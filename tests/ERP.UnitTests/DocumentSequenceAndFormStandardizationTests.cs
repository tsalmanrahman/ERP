using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Application.Models.ProcureToPay;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Common;
using ERP.Common.Infrastructure.Services.Inventory;
using ERP.Common.Infrastructure.Services.OrderToCash;
using ERP.Common.Infrastructure.Services.ProcureToPay;
using Xunit;

namespace ERP.UnitTests;

public class DocumentSequenceAndFormStandardizationTests
{
    private sealed class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "tester@erp.local";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc);
    }

    private static (ErpDbContext Db, FakeUserContext User, DocumentSequenceService SequenceService) CreateTestContext(string dbName)
    {
        var user = new FakeUserContext();
        var clock = new FixedClock();
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ErpDbContext(options, clock, user);
        var sequenceService = new DocumentSequenceService(db);
        db.ProductClasses.Add(new ProductClass { CompanyId = 1, ClassCode = "GEN", ClassName = "General", IsActive = true });
        db.UnitsOfMeasure.Add(new UnitOfMeasure { CompanyId = 1, UomCode = "PCS", UomName = "Pieces", IsActive = true });
        db.CustomerGroups.Add(new CustomerGroup { CompanyId = 1, GroupCode = "CORP", GroupName = "Corporate" });
        db.Set<VendorGroup>().Add(new VendorGroup { CompanyId = 1, GroupCode = "DOM", GroupName = "Domestic" });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1130", AccountName = "AR Control", AccountType = 2, IsActive = true });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "2110", AccountName = "AP Control", AccountType = 2, IsActive = true });
        db.SaveChanges();
        return (db, user, sequenceService);
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
    // 1. BACKEND DOCUMENT SEQUENCE GENERATOR & IMMUTABILITY TESTS
    // =========================================================================

    [Fact]
    public async Task GenerateCodeAsync_DefaultProductSequence_GeneratesSequentialPaddedCodes()
    {
        var (_, _, sequenceService) = CreateTestContext(nameof(GenerateCodeAsync_DefaultProductSequence_GeneratesSequentialPaddedCodes));

        var code1 = await sequenceService.GenerateCodeAsync(1, "Product");
        var code2 = await sequenceService.GenerateCodeAsync(1, "Product");
        var code3 = await sequenceService.GenerateCodeAsync(1, "Product");

        Assert.Equal("PRD-000001", code1);
        Assert.Equal("PRD-000002", code2);
        Assert.Equal("PRD-000003", code3);
    }

    [Fact]
    public async Task GenerateCodeAsync_CustomerAndVendorSequences_GenerateStandardCodes()
    {
        var (_, _, sequenceService) = CreateTestContext(nameof(GenerateCodeAsync_CustomerAndVendorSequences_GenerateStandardCodes));

        var customerCode = await sequenceService.GenerateCodeAsync(1, "Customer");
        var vendorCode = await sequenceService.GenerateCodeAsync(1, "Vendor");

        Assert.Equal("CUS-000001", customerCode);
        Assert.Equal("VEN-000001", vendorCode);
    }

    [Fact]
    public async Task SetConfigurationAsync_CustomPrefixAndLength_UpdatesSequenceOutput()
    {
        var (_, _, sequenceService) = CreateTestContext(nameof(SetConfigurationAsync_CustomPrefixAndLength_UpdatesSequenceOutput));

        await sequenceService.SetConfigurationAsync(1, "Product", "SKU-", 5, "GLOBAL", true);

        var generatedCode = await sequenceService.GenerateCodeAsync(1, "Product");

        Assert.Equal("SKU-00001", generatedCode);
    }

    [Fact]
    public async Task InventoryService_CreateProduct_AutoGeneratesCode_WhenPlaceholderOrEmpty()
    {
        var (db, user, sequenceService) = CreateTestContext(nameof(InventoryService_CreateProduct_AutoGeneratesCode_WhenPlaceholderOrEmpty));
        var inventoryService = new InventoryService(db, user, sequenceService);

        // 1. Create with placeholder "[Auto-Generated on Save]"
        var product1 = await inventoryService.CreateProductAsync(new CreateProductRequest
        {
            ProductCode = "[Auto-Generated on Save]",
            ProductName = "Precision Polymer Pipe 110mm",
            ProductClassId = 1,
            PrimaryUomId = 1,
            StandardCost = 150m,
            SellingPrice = 220m
        });

        // 2. Create with null code
        var product2 = await inventoryService.CreateProductAsync(new CreateProductRequest
        {
            ProductCode = null,
            ProductName = "Structural Steel I-Beam 200mm",
            ProductClassId = 1,
            PrimaryUomId = 1,
            StandardCost = 5000m,
            SellingPrice = 7200m
        });

        Assert.Equal("PRD-000001", product1.ProductCode);
        Assert.Equal("PRD-000002", product2.ProductCode);

        // Verify stored in DB
        var storedInDb = await db.Products.OrderBy(p => p.Id).ToListAsync();
        Assert.Equal(2, storedInDb.Count);
        Assert.Equal("PRD-000001", storedInDb[0].ProductCode);
        Assert.Equal("PRD-000002", storedInDb[1].ProductCode);
    }

    [Fact]
    public async Task InventoryService_UpdateProduct_PreservesExistingCode_AndPreventsTampering()
    {
        var (db, user, sequenceService) = CreateTestContext(nameof(InventoryService_UpdateProduct_PreservesExistingCode_AndPreventsTampering));
        var inventoryService = new InventoryService(db, user, sequenceService);

        var created = await inventoryService.CreateProductAsync(new CreateProductRequest
        {
            ProductCode = null,
            ProductName = "High-Tensile Fastener M16",
            ProductClassId = 1,
            PrimaryUomId = 1,
            StandardCost = 25m,
            SellingPrice = 40m
        });

        Assert.Equal("PRD-000001", created.ProductCode);

        // Attempt update with modified name and pricing
        var updated = await inventoryService.UpdateProductAsync(created.Id, new UpdateProductRequest
        {
            ProductName = "High-Tensile Fastener M16 (Galvanized)",
            ProductClassId = 1,
            PrimaryUomId = 1,
            StandardCost = 28m,
            SellingPrice = 45m
        });

        Assert.NotNull(updated);
        // Original code must be preserved and immutable
        Assert.Equal("PRD-000001", updated!.ProductCode);
        Assert.Equal("High-Tensile Fastener M16 (Galvanized)", updated.ProductName);
    }

    [Fact]
    public async Task CustomerService_CreateCustomer_AutoGeneratesCode_WhenPlaceholderOrEmpty()
    {
        var (db, user, sequenceService) = CreateTestContext(nameof(CustomerService_CreateCustomer_AutoGeneratesCode_WhenPlaceholderOrEmpty));
        var customerService = new CustomerService(db, user, sequenceService);

        var customer = await customerService.CreateCustomerAsync(new CreateCustomerRequest
        {
            CustomerCode = "[Auto-Generated on Save]",
            CustomerName = "Apex Polymer Industries Ltd",
            CustomerGroupId = 1
        });

        Assert.Equal("CUS-000001", customer.CustomerCode);
    }

    [Fact]
    public async Task VendorService_CreateVendor_AutoGeneratesCode_WhenPlaceholderOrEmpty()
    {
        var (db, user, sequenceService) = CreateTestContext(nameof(VendorService_CreateVendor_AutoGeneratesCode_WhenPlaceholderOrEmpty));
        var vendorService = new VendorService(db, user, sequenceService);

        var vendor = await vendorService.CreateVendorAsync(new CreateVendorRequest
        {
            VendorCode = "[Auto-Generated on Save]",
            VendorName = "Global Heavy Steel Supplies",
            VendorGroupId = 1
        });

        Assert.Equal("VEN-000001", vendor.VendorCode);
    }

    // =========================================================================
    // 2. FRONTEND TITLE BADGES & SECTION HEADINGS STANDARDIZATION TESTS
    // =========================================================================

    [Fact]
    public void IndexHtml_NoRedundantMetadataBadges_BesidePageTitles()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        // Redundant technical metadata codes that were previously displayed next to page titles
        var redundantCodes = new[]
        {
            "GL-COA-LIST", "AR-CUST-LIST", "AP-VEND-LIST", "IC-PROD-LIST",
            "IC-WH-LIST", "HR-EMP-LIST", "BC-BANK-LIST", "FA-ASSET-LIST", "MFG-BOM-LIST",
            "AR-CUST-NEW", "AP-VEND-NEW", "IC-PRD-NEW", "GL-COA-NEW",
            "IC-WH-NEW", "HR-EMP-NEW", "BC-CONTRA-NEW", "SD-DC-NEW", "AR-INV-NEW",
            "PO-PR-NEW", "IC-IWT-NEW", "BC-BANK-NEW", "FA-AST-NEW", "MFG-BOM-NEW",
            "HR-PAY-BATCH", "SA-AUDIT-LOG"
        };

        foreach (var code in redundantCodes)
        {
            // Verify none of these appear inside a title badge <span>
            Assert.DoesNotMatch(new Regex($@"<span[^>]*>[^<]*\b{Regex.Escape(code)}\b[^<]*</span>", RegexOptions.IgnoreCase), html);
        }
    }

    [Fact]
    public void IndexHtml_PreservesRequiredBusinessAndTransactionIdentifiers()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        // Business identifiers and transaction numbers that must NOT be removed
        Assert.Contains("PO-2026-0419", html);
        Assert.Contains("BILL-2026-0182", html);
    }

    [Fact]
    public void IndexHtml_FormSectionHeadings_MustNotUseUppercaseClass()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        // No h3 section heading should have the uppercase class
        var uppercaseHeadingRegex = new Regex(@"<h3[^>]*\bclass=""[^""]*\buppercase\b[^""]*""[^>]*>", RegexOptions.IgnoreCase);
        var matches = uppercaseHeadingRegex.Matches(html);

        Assert.True(matches.Count == 0,
            $"Found {matches.Count} section headings still using 'uppercase' class. Expected normal title text.");
    }

    // =========================================================================
    // 3. FRONTEND BUSINESS CODE IMMUTABILITY TESTS
    // =========================================================================

    [Fact]
    public void IndexHtml_MasterCodeInputs_MustBeReadOnly_WithAutoGeneratedIndicator()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        var codeInputIds = new[]
        {
            "prod-code",
            "cust-code",
            "vend-code",
            "wh-code",
            "emp-code",
            "ast-tag-no",
            "bom-code"
        };

        foreach (var inputId in codeInputIds)
        {
            var pattern = new Regex($@"<input[^>]*\bid=""{inputId}""[^>]*>", RegexOptions.IgnoreCase);
            var match = pattern.Match(html);

            Assert.True(match.Success, $"Expected input #{inputId} to exist in index.html");
            var inputTag = match.Value;

            Assert.Contains("readonly", inputTag, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("[Auto-Generated on Save]", inputTag, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("cursor-not-allowed", inputTag, StringComparison.OrdinalIgnoreCase);
        }
    }

    // =========================================================================
    // 4. FRONTEND LIST TABLES COMPACT STYLING & ADAPTIVE PAGINATION TESTS
    // =========================================================================

    [Fact]
    public void IndexHtml_AllMasterListTables_HaveCompactClass_AndPaginationContainer()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));

        var listEntities = new[]
        {
            "accounts", "customers", "vendors", "products", "warehouses", "employees", "banks", "assets", "boms"
        };

        foreach (var entity in listEntities)
        {
            // Table must have id="table-list-{entity}" and class containing erp-table-compact
            var tableRegex = new Regex($@"<table\b[^>]*\bid=""table-list-{entity}""[^>]*>", RegexOptions.IgnoreCase);
            var match = tableRegex.Match(html);

            Assert.True(match.Success, $"Expected table #table-list-{entity} to exist");
            Assert.Contains("erp-table-compact", match.Value, StringComparison.OrdinalIgnoreCase);

            // Container for pagination must exist
            var pagContainerId = $"pagination-list-{entity}";
            Assert.Contains($"id=\"{pagContainerId}\"", html);
            Assert.Contains("erp-pagination-bar", html);
        }
    }

    [Fact]
    public void ErpStudiosJs_AllListTableRenderers_InstantiateAdaptivePagination()
    {
        var js = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "js", "erp-studios.js"));

        var expectedTbodies = new[]
        {
            "tbody-list-accounts",
            "tbody-list-customers",
            "tbody-list-vendors",
            "tbody-list-products",
            "tbody-list-warehouses",
            "tbody-list-employees",
            "tbody-list-banks",
            "tbody-list-assets",
            "tbody-list-boms"
        };

        foreach (var tbodyId in expectedTbodies)
        {
            Assert.Contains($"window.ERP_PAGINATION['{tbodyId}']", js);
            Assert.Contains("new ErpAdaptiveTablePagination", js);
        }
    }
}
