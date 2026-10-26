using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Application.Models.ProcureToPay;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Common;
using ERP.Common.Infrastructure.Services.Finance;
using ERP.Common.Infrastructure.Services.Inventory;
using ERP.Common.Infrastructure.Services.OrderToCash;
using ERP.Common.Infrastructure.Services.ProcureToPay;
using ERP.Web.Api.Middleware;
using Xunit;

namespace ERP.UnitTests;

public class MultiCompanyIsolationTests
{
    private sealed class MutableCompanyUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "admin@hatim.local";
        public int? CompanyId { get; set; }
        public HashSet<int> AccessibleCompanies { get; } = new() { 1, 2, 3, 4 };
        public bool IsAuthenticated { get; set; } = true;
        public bool HasAccessToCompany(int companyId) => IsAuthenticated && AccessibleCompanies.Contains(companyId);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeSequenceService : IDocumentSequenceService
    {
        private int _counter;

        public Task<string> GetNextNumberAsync(int companyId, string documentType, string? prefix = null, string? fiscalYear = null, int numberLength = 4, CancellationToken cancellationToken = default)
            => Task.FromResult($"{prefix ?? documentType}-{companyId}-{Interlocked.Increment(ref _counter):D4}");

        public Task<string> GenerateCodeAsync(int companyId, string entityType, CancellationToken cancellationToken = default)
            => Task.FromResult($"{entityType.ToUpperInvariant()[..3]}-{companyId}-{Interlocked.Increment(ref _counter):D4}");

        public Task<DocumentSequenceConfigDto> GetConfigurationAsync(int companyId, string entityType, CancellationToken cancellationToken = default)
            => Task.FromResult(new DocumentSequenceConfigDto { EntityType = entityType, Prefix = "SEQ", NumberLength = 6 });

        public Task<IReadOnlyList<DocumentSequenceConfigDto>> GetAllConfigurationsAsync(int companyId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DocumentSequenceConfigDto>>(Array.Empty<DocumentSequenceConfigDto>());

        public Task SetConfigurationAsync(int companyId, string entityType, string prefix, int numberLength, string? fiscalYear = "GLOBAL", bool isActive = true, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private static DbContextOptions<ErpDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(dbName + "_" + Guid.NewGuid())
            .Options;

    private static async Task SeedCompanyFoundationsAsync(ErpDbContext db, int companyId, string suffix)
    {
        db.UnitsOfMeasure.Add(new UnitOfMeasure { CompanyId = companyId, UomCode = $"PCS-{suffix}", UomName = $"Pieces ({suffix})", IsActive = true });
        db.ProductClasses.Add(new ProductClass { CompanyId = companyId, ClassCode = $"CLS-{suffix}", ClassName = $"Default Class ({suffix})", IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ScenarioA_ListIsolation_ProductsCustomersVendorsAccounts_StrictSeparationBetweenHSFAndHIL()
    {
        var options = CreateOptions(nameof(ScenarioA_ListIsolation_ProductsCustomersVendorsAccounts_StrictSeparationBetweenHSFAndHIL));
        var clock = new FixedClock();
        var seq = new FakeSequenceService();

        // 1. Operate in HSF (CompanyId = 1)
        var hsfUser = new MutableCompanyUserContext { CompanyId = 1 };
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            await SeedCompanyFoundationsAsync(hsfDb, 1, "HSF");
            var accountService = new AccountService(hsfDb, hsfUser, clock);
            var customerService = new CustomerService(hsfDb, hsfUser);
            var vendorService = new VendorService(hsfDb, hsfUser);
            var inventoryService = new InventoryService(hsfDb, hsfUser, seq);

            await accountService.CreateAccountAsync(new CreateAccountRequest { AccountCode = "1110-HSF", AccountName = "HSF Cash" });
            await customerService.CreateCustomerAsync(new CreateCustomerRequest { CustomerCode = "CUST-HSF-01", CustomerName = "HSF Furniture Buyer" });
            await vendorService.CreateVendorAsync(new CreateVendorRequest { VendorCode = "VEND-HSF-01", VendorName = "HSF Timber Supplier" });
            await inventoryService.CreateProductAsync(new CreateProductRequest { ProductCode = "PROD-HSF-01", ProductName = "HSF Executive Desk", StandardCost = 1000m, SellingPrice = 1500m });
        }

        // 2. Operate in HIL (CompanyId = 3)
        var hilUser = new MutableCompanyUserContext { CompanyId = 3 };
        await using (var hilDb = new ErpDbContext(options, clock, hilUser))
        {
            var accountService = new AccountService(hilDb, hilUser, clock);
            var customerService = new CustomerService(hilDb, hilUser);
            var vendorService = new VendorService(hilDb, hilUser);
            var inventoryService = new InventoryService(hilDb, hilUser, seq);

            // Before creating HIL records, HIL lists must be completely empty (zero HSF leakage)
            Assert.Empty(await accountService.GetAccountsAsync());
            Assert.Empty(await customerService.GetCustomersAsync());
            Assert.Empty(await vendorService.GetVendorsAsync());
            Assert.Empty(await inventoryService.GetProductsAsync());

            await SeedCompanyFoundationsAsync(hilDb, 3, "HIL");
            await accountService.CreateAccountAsync(new CreateAccountRequest { AccountCode = "1110-HIL", AccountName = "HIL Cash" });
            await customerService.CreateCustomerAsync(new CreateCustomerRequest { CustomerCode = "CUST-HIL-01", CustomerName = "HIL Industrial Buyer" });
            await vendorService.CreateVendorAsync(new CreateVendorRequest { VendorCode = "VEND-HIL-01", VendorName = "HIL Polymer Supplier" });
            await inventoryService.CreateProductAsync(new CreateProductRequest { ProductCode = "PROD-HIL-01", ProductName = "HIL UPVC Pipe", StandardCost = 500m, SellingPrice = 750m });

            var hilAccounts = (await accountService.GetAccountsAsync()).ToList();
            var hilCustomers = (await customerService.GetCustomersAsync()).ToList();
            var hilVendors = (await vendorService.GetVendorsAsync()).ToList();
            var hilProducts = (await inventoryService.GetProductsAsync()).ToList();

            Assert.Single(hilAccounts);
            Assert.Equal("1110-HIL", hilAccounts[0].AccountCode);

            Assert.Single(hilCustomers);
            Assert.Equal("CUST-HIL-01", hilCustomers[0].CustomerCode);

            Assert.Single(hilVendors);
            Assert.Equal("VEND-HIL-01", hilVendors[0].VendorCode);

            Assert.Single(hilProducts);
            Assert.Equal("PROD-HIL-01", hilProducts[0].ProductCode);
        }

        // 3. Re-verify HSF (CompanyId = 1) still sees only HSF records
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            var inventoryService = new InventoryService(hsfDb, hsfUser, seq);
            var hsfProducts = (await inventoryService.GetProductsAsync()).ToList();
            Assert.Single(hsfProducts);
            Assert.Equal("PROD-HSF-01", hsfProducts[0].ProductCode);
        }
    }

    [Fact]
    public async Task ScenarioB_CreateCompanyAssignment_EnforcesActiveCompanyAndAllowsSameCodeInDifferentCompanies()
    {
        var options = CreateOptions(nameof(ScenarioB_CreateCompanyAssignment_EnforcesActiveCompanyAndAllowsSameCodeInDifferentCompanies));
        var clock = new FixedClock();
        var seq = new FakeSequenceService();

        var hsfUser = new MutableCompanyUserContext { CompanyId = 1 };
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            await SeedCompanyFoundationsAsync(hsfDb, 1, "HSF");
            var inventoryService = new InventoryService(hsfDb, hsfUser, seq);
            await inventoryService.CreateProductAsync(new CreateProductRequest
            {
                ProductCode = "SHARED-SKU-001",
                ProductName = "HSF Version",
                StandardCost = 100m,
                SellingPrice = 150m
            });
        }

        // Creating the exact same ProductCode in HIL (CompanyId = 3) succeeds and assigns CompanyId = 3
        var hilUser = new MutableCompanyUserContext { CompanyId = 3 };
        await using (var hilDb = new ErpDbContext(options, clock, hilUser))
        {
            await SeedCompanyFoundationsAsync(hilDb, 3, "HIL");
            var inventoryService = new InventoryService(hilDb, hilUser, seq);
            var created = await inventoryService.CreateProductAsync(new CreateProductRequest
            {
                ProductCode = "SHARED-SKU-001",
                ProductName = "HIL Version",
                StandardCost = 200m,
                SellingPrice = 300m
            });

            var storedHil = await hilDb.Products.SingleAsync(p => p.Id == created.Id);
            Assert.Equal(3, storedHil.CompanyId);
            Assert.Equal("HIL Version", storedHil.ProductName);
        }
    }

    [Fact]
    public async Task ScenarioC_CrossCompanyReadUpdateDeleteRestore_AreBlockedBetweenHSFAndHIL()
    {
        var options = CreateOptions(nameof(ScenarioC_CrossCompanyReadUpdateDeleteRestore_AreBlockedBetweenHSFAndHIL));
        var clock = new FixedClock();
        var seq = new FakeSequenceService();

        int hsfProductId;
        int hsfCustomerId;
        int hsfVendorId;

        var hsfUser = new MutableCompanyUserContext { CompanyId = 1 };
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            await SeedCompanyFoundationsAsync(hsfDb, 1, "HSF");
            var accountService = new AccountService(hsfDb, hsfUser, clock);
            await accountService.CreateAccountAsync(new CreateAccountRequest { AccountCode = "1130", AccountName = "HSF AR Control" });

            var inventoryService = new InventoryService(hsfDb, hsfUser, seq);
            var customerService = new CustomerService(hsfDb, hsfUser);
            var vendorService = new VendorService(hsfDb, hsfUser);

            var prod = await inventoryService.CreateProductAsync(new CreateProductRequest
            {
                ProductCode = "HSF-LOCKED",
                ProductName = "HSF Locked Product",
                StandardCost = 50m,
                SellingPrice = 80m
            });
            hsfProductId = prod.Id;

            var cust = await customerService.CreateCustomerAsync(new CreateCustomerRequest
            {
                CustomerCode = "HSF-CUST-LOCKED",
                CustomerName = "HSF Locked Customer"
            });
            hsfCustomerId = cust.Id;

            var vend = await vendorService.CreateVendorAsync(new CreateVendorRequest
            {
                VendorCode = "HSF-VEND-LOCKED",
                VendorName = "HSF Locked Vendor"
            });
            hsfVendorId = vend.Id;
        }

        // Switch to HIL (CompanyId = 3) and attempt to read, update, or delete HSF records by ID
        var hilUser = new MutableCompanyUserContext { CompanyId = 3 };
        await using (var hilDb = new ErpDbContext(options, clock, hilUser))
        {
            var inventoryService = new InventoryService(hilDb, hilUser, seq);
            var customerService = new CustomerService(hilDb, hilUser);
            var vendorService = new VendorService(hilDb, hilUser);

            Assert.Null(await inventoryService.GetProductByIdAsync(hsfProductId));
            Assert.Null(await inventoryService.GetProductByCodeAsync("HSF-LOCKED"));
            Assert.Null(await customerService.GetCustomerByIdAsync(hsfCustomerId));
            Assert.Null(await vendorService.GetVendorByIdAsync(hsfVendorId));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                inventoryService.UpdateProductAsync(hsfProductId, new UpdateProductRequest
                {
                    ProductName = "Tampered by HIL",
                    StandardCost = 1m,
                    SellingPrice = 1m
                }));

            Assert.False(await inventoryService.SoftDeleteProductAsync(hsfProductId));
            Assert.False(await customerService.SoftDeleteCustomerAsync(hsfCustomerId));
            Assert.False(await vendorService.SoftDeleteVendorAsync(hsfVendorId));
        }

        // Verify HSF records remain intact in HSF (CompanyId = 1)
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            var inventoryService = new InventoryService(hsfDb, hsfUser, seq);
            var intact = await inventoryService.GetProductByIdAsync(hsfProductId);
            Assert.NotNull(intact);
            Assert.Equal("HSF Locked Product", intact!.ProductName);
        }
    }

    [Fact]
    public async Task ScenarioD_CrossCompanyForeignKeyReference_IsRejectedOnWrite()
    {
        var options = CreateOptions(nameof(ScenarioD_CrossCompanyForeignKeyReference_IsRejectedOnWrite));
        var clock = new FixedClock();
        var seq = new FakeSequenceService();

        int hsfUomId;
        int hsfClassId;
        int hsfAccountId;

        var hsfUser = new MutableCompanyUserContext { CompanyId = 1 };
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            var uom = new UnitOfMeasure { CompanyId = 1, UomCode = "PCS", UomName = "Pieces", IsActive = true };
            var pClass = new ProductClass { CompanyId = 1, ClassCode = "FURN", ClassName = "Furniture", IsActive = true };
            var acc = new Account { CompanyId = 1, AccountCode = "1210", AccountName = "HSF AR Control", AccountType = 2, IsActive = true };

            hsfDb.UnitsOfMeasure.Add(uom);
            hsfDb.ProductClasses.Add(pClass);
            hsfDb.Accounts.Add(acc);
            await hsfDb.SaveChangesAsync();

            hsfUomId = uom.Id;
            hsfClassId = pClass.Id;
            hsfAccountId = acc.Id;
        }

        // Attempt in HIL (CompanyId = 3) to create a Product referencing HSF's UOM or ProductClass, or a Customer referencing HSF's GL Account
        var hilUser = new MutableCompanyUserContext { CompanyId = 3 };
        await using (var hilDb = new ErpDbContext(options, clock, hilUser))
        {
            await SeedCompanyFoundationsAsync(hilDb, 3, "HIL");
            var inventoryService = new InventoryService(hilDb, hilUser, seq);
            var customerService = new CustomerService(hilDb, hilUser);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                inventoryService.CreateProductAsync(new CreateProductRequest
                {
                    ProductCode = "HIL-BAD-UOM",
                    ProductName = "Product With HSF UOM",
                    PrimaryUomId = hsfUomId
                }));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                inventoryService.CreateProductAsync(new CreateProductRequest
                {
                    ProductCode = "HIL-BAD-CLASS",
                    ProductName = "Product With HSF Class",
                    ProductClassId = hsfClassId
                }));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                customerService.CreateCustomerAsync(new CreateCustomerRequest
                {
                    CustomerCode = "HIL-BAD-GL",
                    CustomerName = "Customer With HSF GL Account",
                    GlControlAccountId = hsfAccountId
                }));
        }
    }

    [Fact]
    public async Task ScenarioE_CompanyContextEnforcementMiddleware_RejectsUnauthorizedCompanyHeaderWith403()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/inv/products";
        httpContext.Request.Headers["X-Company-Id"] = "4"; // HPL (Company 4), not granted to user
        httpContext.Response.Body = new MemoryStream();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "25"),
            new Claim("unique_name", "hil_operator"),
            new Claim("company_id", "3"),
            new Claim("accessible_company", "3"),
            new Claim(ClaimTypes.Role, "Operator")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserContext = new CurrentUserContext(accessor);

        var nextCalled = false;
        var middleware = new CompanyContextEnforcementMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<CompanyContextEnforcementMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, currentUserContext);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ScenarioF_DbContextSaveChangesGuard_BlocksCrossCompanyMutationEvenIfLoadedIgnoringFilters()
    {
        var options = CreateOptions(nameof(ScenarioF_DbContextSaveChangesGuard_BlocksCrossCompanyMutationEvenIfLoadedIgnoringFilters));
        var clock = new FixedClock();

        int hsfAccountId;
        var hsfUser = new MutableCompanyUserContext { CompanyId = 1 };
        await using (var hsfDb = new ErpDbContext(options, clock, hsfUser))
        {
            var acc = new Account { CompanyId = 1, AccountCode = "1000", AccountName = "HSF Main Account", AccountType = 2, IsActive = true };
            hsfDb.Accounts.Add(acc);
            await hsfDb.SaveChangesAsync();
            hsfAccountId = acc.Id;
        }

        var hilUser = new MutableCompanyUserContext { CompanyId = 3 };
        await using (var hilDb = new ErpDbContext(options, clock, hilUser))
        {
            // Even if a developer mistakenly uses IgnoreQueryFilters() and mutates an HSF entity while in HIL context,
            // ErpDbContext.ApplyAuditAndTenantInformation blocks the cross-company write at SaveChangesAsync!
            var crossCompanyEntity = await hilDb.Accounts.IgnoreQueryFilters().SingleAsync(a => a.Id == hsfAccountId);
            crossCompanyEntity.AccountName = "Unauthorized Cross-Tenant Edit";

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => hilDb.SaveChangesAsync());
        }
    }
}
