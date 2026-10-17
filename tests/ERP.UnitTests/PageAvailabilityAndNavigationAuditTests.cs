using System;
using System.IO;
using Xunit;

namespace ERP.UnitTests;

/// <summary>
/// Validates the System-Wide Page Availability Audit and the List-First Navigation Pattern.
/// Ensures all 11 modules provide full visibility of existing records before opening data entry forms.
/// </summary>
public class PageAvailabilityAndNavigationAuditTests
{
    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ERP.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not find ERP.sln repository root.");
    }

    [Fact]
    public void Verify_SalesOrderListCanvas_ExistsWithCompleteGridControls()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        // 1. Dedicated List Section
        Assert.Contains("id=\"studio-list-sales-orders\"", indexHtml);
        Assert.Contains("Commercial Sales Orders Registry", indexHtml);

        // 2. Data Table Canvas & Body
        Assert.Contains("id=\"table-list-sales-orders\"", indexHtml);
        Assert.Contains("id=\"tbody-list-sales-orders\"", indexHtml);
        Assert.Contains("id=\"pagination-list-sales-orders\"", indexHtml);
        Assert.Contains("id=\"empty-list-sales-orders\"", indexHtml);

        // 3. Search and Multi-Criteria Filtering Controls
        Assert.Contains("id=\"search-list-sales-orders\"", indexHtml);
        Assert.Contains("oninput=\"filterSalesOrderListTable()\"", indexHtml);
        Assert.Contains("id=\"filter-status-sales-orders\"", indexHtml);
        Assert.Contains("onchange=\"filterSalesOrderListTable()\"", indexHtml);
        Assert.Contains("id=\"filter-customer-sales-orders\"", indexHtml);

        // 4. Action Controls: New Sales Order & Refresh
        Assert.Contains("onclick=\"openNewSalesOrderForm()\"", indexHtml);
        Assert.Contains("onclick=\"loadSalesOrderListData()\"", indexHtml);
        Assert.Contains("onclick=\"navigateBackFrom('list-sales-orders')\"", indexHtml);
    }

    [Fact]
    public void Verify_ListFirstNavigationPattern_EnforcedForSalesOrders()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        // Sidebar link MUST route to list-first canvas rather than directly entering studio form
        Assert.Contains("href=\"#list-sales-orders\"", indexHtml);
        Assert.Contains("onclick=\"navigateToList('sales-orders')\"", indexHtml);
        Assert.Contains("data-list=\"sales-orders\"", indexHtml);

        // Inside the 6-tab studio-sales-order, back button must return to list registry
        Assert.Contains("onclick=\"navigateBackFrom('sales-order')\"", indexHtml);
        Assert.Contains("Return to Sales Orders Registry", indexHtml);
    }

    [Fact]
    public void Verify_ErpStudiosRouter_RegistersSalesOrderListAndState()
    {
        var root = GetRepoRoot();
        var js = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "js", "erp-studios.js"));

        // Registered in views array
        Assert.Contains("'list-sales-orders'", js);

        // Hierarchy and Parent View Mapping
        Assert.Contains("'list-sales-orders': 'module-sales'", js);
        Assert.Contains("'sales-order': 'list-sales-orders'", js);

        // Module Map
        Assert.Contains("'sales-orders': 'sales'", js);
        Assert.Contains("'list-sales-orders': 'sales'", js);

        // Breadcrumb and Entity View Metadata
        Assert.Contains("listStudio: 'list-sales-orders'", js);

        // Hash Routing
        Assert.Contains("rawHash === 'list-sales-orders'", js);

        // Section 17.10 controller functions
        Assert.Contains("function loadSalesOrderListData", js);
        Assert.Contains("function renderSalesOrderListTable", js);
        Assert.Contains("function filterSalesOrderListTable", js);
        Assert.Contains("function openNewSalesOrderForm", js);
        Assert.Contains("function openSalesOrderInStudio", js);
        Assert.Contains("function confirmSalesOrderRecord", js);
        Assert.Contains("function exportSalesOrdersCsv", js);

        // Global exports on window
        Assert.Contains("window.loadSalesOrderListData = loadSalesOrderListData;", js);
        Assert.Contains("window.renderSalesOrderListTable = renderSalesOrderListTable;", js);
        Assert.Contains("window.openSalesOrderInStudio = openSalesOrderInStudio;", js);
        Assert.Contains("window.exportSalesOrdersCsv = exportSalesOrdersCsv;", js);
    }

    [Fact]
    public void Verify_All10Entities_HaveListAndEntryPair_WithListFirstNavigation()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        var entityPairs = new[]
        {
            new { Name = "Chart of Accounts", List = "studio-list-accounts", Entry = "studio-entry-account" },
            new { Name = "Customer Directory", List = "studio-list-customers", Entry = "studio-entry-customer" },
            new { Name = "Vendor Directory", List = "studio-list-vendors", Entry = "studio-entry-vendor" },
            new { Name = "Product Master Catalog", List = "studio-list-products", Entry = "studio-entry-product" },
            new { Name = "Warehouses & Depots", List = "studio-list-warehouses", Entry = "studio-entry-warehouse" },
            new { Name = "Employee Directory", List = "studio-list-employees", Entry = "studio-entry-employee" },
            new { Name = "Bank Accounts", List = "studio-list-banks", Entry = "studio-entry-bank" },
            new { Name = "Fixed Assets", List = "studio-list-assets", Entry = "studio-entry-asset" },
            new { Name = "Bill of Materials (BOM)", List = "studio-list-boms", Entry = "studio-entry-bom" },
            new { Name = "Commercial Sales Orders", List = "studio-list-sales-orders", Entry = "studio-sales-order" }
        };

        foreach (var pair in entityPairs)
        {
            Assert.True(indexHtml.Contains($"id=\"{pair.List}\""), $"List screen {pair.List} for {pair.Name} must exist.");
            Assert.True(indexHtml.Contains($"id=\"{pair.Entry}\""), $"Entry screen {pair.Entry} for {pair.Name} must exist.");
        }
    }
}
