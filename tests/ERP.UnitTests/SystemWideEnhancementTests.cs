using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Application.Models.ProcureToPay;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Common;
using ERP.Common.Infrastructure.Services.Inventory;
using ERP.Common.Infrastructure.Services.OrderToCash;
using ERP.Common.Infrastructure.Services.ProcureToPay;
using Xunit;

namespace ERP.UnitTests;

/// <summary>
/// End-to-end tests for ERP system-wide enhancements:
/// - Clean list pages without redundant count badges
/// - Clickable primary business identifiers resolving to dedicated View pages
/// - Modern 3-action row controller (View, Edit, Delete) with soft-deletion and referential integrity protection
/// - Clickable hierarchical breadcrumbs
/// - Visual form validation with shake animations and field auto-focus
/// - Company context locking on View and Edit pages
/// </summary>
public class SystemWideEnhancementTests
{
    private sealed class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "admin@erp.local";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
    }

    private static (ErpDbContext Db, FakeUserContext User, DocumentSequenceService SequenceService) CreateDb(string dbName)
    {
        var user = new FakeUserContext();
        var clock = new FixedClock();
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ErpDbContext(options, clock, user);
        var seq = new DocumentSequenceService(db);
        return (db, user, seq);
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ERP.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Repository root containing ERP.sln not found.");
    }

    private static string ReadFrontendFile(string relativePath)
    {
        var fullPath = Path.Combine(GetRepoRoot(), relativePath);
        Assert.True(File.Exists(fullPath), $"Expected file to exist: {fullPath}");
        return File.ReadAllText(fullPath);
    }

    // =========================================================================
    // 1. BACKEND: PRODUCT SERVICE TESTS (GET BY CODE & SOFT DELETE INTEGRITY)
    // =========================================================================

    [Fact]
    public async Task InventoryService_GetProductByCode_ReturnsCorrectProductScopedToCompany()
    {
        var (db, user, seq) = CreateDb(nameof(InventoryService_GetProductByCode_ReturnsCorrectProductScopedToCompany));
        var service = new InventoryService(db, user, seq);

        var pclass = new ProductClass { CompanyId = 1, ClassCode = "RAW", ClassName = "Raw Materials" };
        var uom = new UnitOfMeasure { CompanyId = 1, UomCode = "MT", UomName = "Metric Ton" };
        db.ProductClasses.Add(pclass);
        db.UnitsOfMeasure.Add(uom);
        await db.SaveChangesAsync();

        var prod = new Product
        {
            CompanyId = 1,
            ProductCode = "PRD-101",
            ProductName = "Structural Steel Beam HEB-300",
            ProductClassId = pclass.Id,
            PrimaryUomId = uom.Id,
            StandardCost = 85000m,
            SellingPrice = 105000m
        };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var result = await service.GetProductByCodeAsync("PRD-101");
        Assert.NotNull(result);
        Assert.Equal("PRD-101", result.ProductCode);
        Assert.Equal("Structural Steel Beam HEB-300", result.ProductName);
    }

    [Fact]
    public async Task InventoryService_SoftDeleteProduct_SucceedsWhenNoStockAndNoMovements()
    {
        var (db, user, seq) = CreateDb(nameof(InventoryService_SoftDeleteProduct_SucceedsWhenNoStockAndNoMovements));
        var service = new InventoryService(db, user, seq);

        var pclass = new ProductClass { CompanyId = 1, ClassCode = "PKG", ClassName = "Packaging" };
        var uom = new UnitOfMeasure { CompanyId = 1, UomCode = "PCS", UomName = "Pieces" };
        db.ProductClasses.Add(pclass);
        db.UnitsOfMeasure.Add(uom);
        await db.SaveChangesAsync();

        var prod = new Product
        {
            CompanyId = 1,
            ProductCode = "PRD-202",
            ProductName = "Wooden Pallet Standard 1200x1000",
            ProductClassId = pclass.Id,
            PrimaryUomId = uom.Id
        };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        var success = await service.SoftDeleteProductAsync(prod.Id);
        Assert.True(success);

        // Verify soft-deleted state
        var deletedProd = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == prod.Id);
        Assert.NotNull(deletedProd);
        Assert.True(deletedProd.IsDeleted);
        Assert.NotNull(deletedProd.DeletedAtUtc);
    }

    [Fact]
    public async Task InventoryService_SoftDeleteProduct_FailsWhenOnHandStockExists()
    {
        var (db, user, seq) = CreateDb(nameof(InventoryService_SoftDeleteProduct_FailsWhenOnHandStockExists));
        var service = new InventoryService(db, user, seq);

        var pclass = new ProductClass { CompanyId = 1, ClassCode = "STL", ClassName = "Raw Steel" };
        var uom = new UnitOfMeasure { CompanyId = 1, UomCode = "MT", UomName = "Metric Ton" };
        var wh = new Warehouse { CompanyId = 1, WarehouseCode = "WH-01", WarehouseName = "Central Yard" };
        db.ProductClasses.Add(pclass);
        db.UnitsOfMeasure.Add(uom);
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync();

        var prod = new Product
        {
            CompanyId = 1,
            ProductCode = "PRD-303",
            ProductName = "Mild Steel Plate 12mm",
            ProductClassId = pclass.Id,
            PrimaryUomId = uom.Id
        };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        db.ProductStock.Add(new ProductStock
        {
            CompanyId = 1,
            ProductId = prod.Id,
            WarehouseId = wh.Id,
            QuantityOnHand = 25m
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SoftDeleteProductAsync(prod.Id));

        Assert.Contains("remaining inventory on hand", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InventoryService_SoftDeleteProduct_FailsWhenStockLedgerTransactionsExist()
    {
        var (db, user, seq) = CreateDb(nameof(InventoryService_SoftDeleteProduct_FailsWhenStockLedgerTransactionsExist));
        var service = new InventoryService(db, user, seq);

        var pclass = new ProductClass { CompanyId = 1, ClassCode = "HDW", ClassName = "Hardware" };
        var uom = new UnitOfMeasure { CompanyId = 1, UomCode = "NOS", UomName = "Numbers" };
        var wh = new Warehouse { CompanyId = 1, WarehouseCode = "WH-02", WarehouseName = "Fittings Depot" };
        db.ProductClasses.Add(pclass);
        db.UnitsOfMeasure.Add(uom);
        db.Warehouses.Add(wh);
        await db.SaveChangesAsync();

        var prod = new Product
        {
            CompanyId = 1,
            ProductCode = "PRD-404",
            ProductName = "High-Tensile Anchor Bolt M24",
            ProductClassId = pclass.Id,
            PrimaryUomId = uom.Id
        };
        db.Products.Add(prod);
        await db.SaveChangesAsync();

        // Stock quantity is zero, but historical movements exist
        db.StockLedgers.Add(new StockLedger
        {
            CompanyId = 1,
            ProductId = prod.Id,
            WarehouseId = wh.Id,
            TransactionDate = DateTime.UtcNow.Date,
            TransactionType = StockTransactionType.GoodsReceipt,
            ReferenceNumber = "GRN-001",
            QuantityIn = 100m,
            UnitCost = 450m,
            TotalCost = 45000m,
            BalanceQuantity = 100m
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SoftDeleteProductAsync(prod.Id));

        Assert.Contains("recorded stock movements", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 2. BACKEND: VENDOR SERVICE TESTS (GET BY CODE & SOFT DELETE INTEGRITY)
    // =========================================================================

    [Fact]
    public async Task VendorService_GetVendorByCode_ReturnsCorrectVendor()
    {
        var (db, user, seq) = CreateDb(nameof(VendorService_GetVendorByCode_ReturnsCorrectVendor));
        var service = new VendorService(db, user, seq);

        var vendor = new Vendor
        {
            CompanyId = 1,
            VendorCode = "VEND-101",
            VendorName = "BSRM Steels Limited",
            Phone = "+880 1819-876543",
            AddressLine1 = "Chittagong Heavy Industrial Area"
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var result = await service.GetVendorByCodeAsync("VEND-101");
        Assert.NotNull(result);
        Assert.Equal("VEND-101", result.VendorCode);
        Assert.Equal("BSRM Steels Limited", result.VendorName);
    }

    [Fact]
    public async Task VendorService_SoftDeleteVendor_FailsWhenUnpaidPurchaseBillsExist()
    {
        var (db, user, seq) = CreateDb(nameof(VendorService_SoftDeleteVendor_FailsWhenUnpaidPurchaseBillsExist));
        var service = new VendorService(db, user, seq);

        var vendor = new Vendor
        {
            CompanyId = 1,
            VendorCode = "VEND-202",
            VendorName = "PHP Steel Mills Limited"
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        db.PurchaseBills.Add(new PurchaseBill
        {
            CompanyId = 1,
            VendorId = vendor.Id,
            BillNumber = "BILL-2026-0099",
            BillDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(30),
            TotalAmount = 500000m,
            BalanceAmount = 500000m, // Unpaid balance
            Status = PurchaseBillStatus.Confirmed
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SoftDeleteVendorAsync(vendor.Id));

        Assert.Contains("outstanding unpaid purchase bills", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 3. BACKEND: CUSTOMER SERVICE TESTS (GET BY CODE)
    // =========================================================================

    [Fact]
    public async Task CustomerService_GetCustomerByCode_ReturnsCorrectCustomer()
    {
        var (db, user, seq) = CreateDb(nameof(CustomerService_GetCustomerByCode_ReturnsCorrectCustomer));
        var service = new CustomerService(db, user, seq);

        var cust = new Customer
        {
            CompanyId = 1,
            CustomerCode = "CUST-303",
            CustomerName = "Bashundhara Engineering Ltd",
            City = "Dhaka",
            CreditLimit = 10000000m
        };
        db.Customers.Add(cust);
        await db.SaveChangesAsync();

        var result = await service.GetCustomerByCodeAsync("CUST-303");
        Assert.NotNull(result);
        Assert.Equal("CUST-303", result.CustomerCode);
        Assert.Equal("Bashundhara Engineering Ltd", result.CustomerName);
    }

    // =========================================================================
    // 4. FRONTEND: UI MARKUP & DESIGN SYSTEM ENHANCEMENTS
    // =========================================================================

    [Fact]
    public void IndexHtml_NoRedundantCountBadgesNextToTitles()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        // All 9 title badge counts were removed from the <h2> headers
        Assert.DoesNotContain("id=\"badge-count-", html, StringComparison.Ordinal);
    }

    [Fact]
    public void IndexHtml_ViewRecordStudio_ExistsWithRequiredActions()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        Assert.Contains("id=\"studio-view-record\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"view-record-fields-grid\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"btn-view-to-edit\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"btn-view-delete\"", html, StringComparison.Ordinal);
        Assert.Contains("navigateBackFromView()", html, StringComparison.Ordinal);
        Assert.Contains("editRecordFromView()", html, StringComparison.Ordinal);
        Assert.Contains("deleteRecordFromView()", html, StringComparison.Ordinal);
    }

    [Fact]
    public void IndexHtml_SoftDeleteModal_ExistsWithConfirmActions()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        Assert.Contains("id=\"erp-delete-modal\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"del-record-code\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"del-error-alert\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"btn-confirm-delete\"", html, StringComparison.Ordinal);
        Assert.Contains("executePendingDelete()", html, StringComparison.Ordinal);
        Assert.Contains("closeDeleteModal()", html, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystemCss_FormShakeAndCompanyLockStylesDefined()
    {
        var css = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "css", "ERP-design-system.css"));

        Assert.Contains("@keyframes erpShake", css, StringComparison.Ordinal);
        Assert.Contains(".erp-shake", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", css, StringComparison.Ordinal);
        Assert.Contains(".company-switcher-locked", css, StringComparison.Ordinal);
        Assert.Contains(".clickable-code", css, StringComparison.Ordinal);
        Assert.Contains(".breadcrumb-item-link", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ErpStudiosJs_All9ListTables_IncludeClickableCodesAndViewEditDeleteActions()
    {
        var js = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "js", "erp-studios.js"));

        // Clickable codes for entities
        Assert.Contains("viewRecord('account'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('customer'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('vendor'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('product'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('warehouse'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('employee'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('bank'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('asset'", js, StringComparison.Ordinal);
        Assert.Contains("viewRecord('bom'", js, StringComparison.Ordinal);

        // Soft delete confirmation triggers
        Assert.Contains("confirmSoftDelete('account'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('customer'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('vendor'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('product'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('warehouse'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('employee'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('bank'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('asset'", js, StringComparison.Ordinal);
        Assert.Contains("confirmSoftDelete('bom'", js, StringComparison.Ordinal);

        // Company locking on view and edit studios
        Assert.Contains("isViewOrEdit", js, StringComparison.Ordinal);
        Assert.Contains("lockCompanySwitcher", js, StringComparison.Ordinal);
    }
}
