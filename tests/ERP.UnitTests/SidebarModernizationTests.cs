using Xunit;

namespace ERP.UnitTests;

/// <summary>
/// Unit and architecture verification tests for the Sidebar Modernization & Navigation Cleanup:
/// 
/// Validates:
/// 1. 100% Structural Invariance & Hierarchy:
///    - All 11 workspace parent modules preserved in exact chronological order:
///      GL -> AP -> AR -> Cash & Banking -> Sourcing -> Inventory -> Sales -> HRIS -> Fixed Assets -> Manufacturing -> System Admin.
///    - All operational studios, registers, entry forms, and master lists exist and are organized under their parent module workspaces.
/// 2. Clean List Pages & Removal of Sidebar Numeric Badges:
///    - Numeric badges ('14', 'Matrix', 'OK', '236', '5-Suite') removed from sidebar menu items and submenus.
/// 3. Single Sidebar Toggle Button:
///    - Duplicate collapse button removed from sidebar header.
///    - Green-marked top-bar sidebar toggle button (#sidebar-toggle-btn) preserved and active.
/// 4. Top Navigation Bar Search Cleanup:
///    - Yellow-marked top-bar search button and sliding search drawer removed (#header-search-container).
///    - In-sidebar quick search (Ctrl+K) preserved.
/// 5. Expandable Accordion Submenus:
///    - All 11 parent modules are expandable accordion nodes with vertical tree connector groups.
///    - JavaScript controllers support auto-expansion and active state synchronization.
/// </summary>
public class SidebarModernizationTests
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
    public void Verify_Sidebar_11_Workspaces_In_Exact_Chronological_Order()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        // 1. Home Overview
        Assert.Contains("data-studio=\"dashboard\"", indexHtml);
        Assert.Contains("Home Overview", indexHtml);

        // 2. Enterprise Workspaces in EXACT chronological order
        var expectedWorkspaces = new[]
        {
            "general-ledger",
            "accounts-payable",
            "accounts-receivable",
            "cash-book",
            "sourcing",
            "inventory",
            "sales",
            "hris",
            "fixed-assets",
            "manufacturing",
            "system-admin"
        };

        var lastIndex = 0;
        foreach (var ws in expectedWorkspaces)
        {
            var marker = $"data-module=\"{ws}\"";
            var idx = indexHtml.IndexOf(marker, lastIndex, StringComparison.Ordinal);
            Assert.True(idx > -1, $"Workspace '{ws}' must exist in the sidebar.");
            Assert.True(idx >= lastIndex, $"Workspace '{ws}' must preserve exact chronological sequence.");
            lastIndex = idx;
        }
    }

    [Fact]
    public void Verify_All_11_Modules_Have_Expandable_Accordion_Nodes_With_Tree_SubItems()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        var expectedModules = new[]
        {
            "general-ledger",
            "accounts-payable",
            "accounts-receivable",
            "cash-book",
            "sourcing",
            "inventory",
            "sales",
            "hris",
            "fixed-assets",
            "manufacturing",
            "system-admin"
        };

        foreach (var slug in expectedModules)
        {
            Assert.Contains($"data-accordion-module=\"{slug}\"", indexHtml);
            Assert.Contains($"id=\"accordion-content-{slug}\"", indexHtml.Replace("id=\"gl-accordion-content\"", "id=\"accordion-content-general-ledger\""));
            Assert.Contains($"toggleModuleAccordion('{slug}')", indexHtml);
        }

        // Verify sub-items exist under parent modules (simplified list-first pattern)
        Assert.Contains("data-list=\"accounts\"", indexHtml);
        Assert.Contains("data-studio=\"jv\"", indexHtml);
        Assert.Contains("data-studio=\"trial-balance\"", indexHtml);

        Assert.Contains("data-list=\"vendors\"", indexHtml);
        Assert.Contains("data-studio=\"bill-entry\"", indexHtml);
        Assert.Contains("data-studio=\"ap-aging\"", indexHtml);

        Assert.Contains("data-list=\"customers\"", indexHtml);
        Assert.Contains("data-studio=\"receipts\"", indexHtml);
        Assert.Contains("data-studio=\"ar-aging\"", indexHtml);

        Assert.Contains("data-studio=\"cash-book\"", indexHtml);
        Assert.Contains("data-list=\"banks\"", indexHtml);
        Assert.Contains("data-studio=\"entry-contra\"", indexHtml);

        Assert.Contains("data-studio=\"po\"", indexHtml);
        Assert.Contains("data-studio=\"entry-pr\"", indexHtml);
        Assert.Contains("data-studio=\"grn\"", indexHtml);

        Assert.Contains("data-list=\"products\"", indexHtml);
        Assert.Contains("data-list=\"depots\"", indexHtml);
        Assert.Contains("data-list=\"warehouses\"", indexHtml);
        Assert.Contains("data-list=\"transfers\"", indexHtml);
        Assert.Contains("data-list=\"adjustments\"", indexHtml);
        Assert.Contains("data-list=\"grn\"", indexHtml);
        Assert.Contains("data-studio=\"stock\"", indexHtml);
        Assert.Contains("data-list=\"uoms\"", indexHtml);
        Assert.Contains("data-studio=\"entry-challan\"", indexHtml);

        Assert.Contains("data-studio=\"sales-order\"", indexHtml);
        Assert.Contains("data-studio=\"entry-invoice\"", indexHtml);

        Assert.Contains("data-list=\"employees\"", indexHtml);
        Assert.Contains("data-studio=\"payslip\"", indexHtml);
        Assert.Contains("data-studio=\"payroll-batch\"", indexHtml);

        Assert.Contains("data-list=\"assets\"", indexHtml);

        Assert.Contains("data-list=\"boms\"", indexHtml);

        Assert.Contains("data-studio=\"audit-trail\"", indexHtml);
        Assert.Contains("/scalar/v1", indexHtml);
        Assert.Contains("/health/ready", indexHtml);
    }

    [Fact]
    public void Verify_Sidebar_Cleanup_Removes_Numeric_Badges_Duplicate_Toggle_And_TopBar_Search()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        // Extract sidebar content only
        var sidebarStart = indexHtml.IndexOf("<aside", StringComparison.Ordinal);
        var sidebarEnd = indexHtml.IndexOf("</aside>", StringComparison.Ordinal);
        Assert.True(sidebarStart > -1 && sidebarEnd > sidebarStart);
        var sidebarContent = indexHtml.Substring(sidebarStart, sidebarEnd - sidebarStart);

        // 1. Numeric badges must be completely removed from sidebar menu items
        Assert.DoesNotContain(">14<", sidebarContent);
        Assert.DoesNotContain(">Matrix<", sidebarContent);
        Assert.DoesNotContain(">OK<", sidebarContent);
        Assert.DoesNotContain(">236<", sidebarContent);
        Assert.DoesNotContain(">5-Suite<", sidebarContent);

        // 2. Duplicate sidebar toggle in sidebar header must be removed
        Assert.DoesNotContain("title=\"Collapse to Slim Rail (56px)\"", sidebarContent);

        // 3. Top-bar sidebar toggle must be present and active
        Assert.Contains("id=\"sidebar-toggle-btn\"", indexHtml);

        // 4. Top-bar search button and drawer must be removed
        Assert.DoesNotContain("id=\"header-search-container\"", indexHtml);
        Assert.DoesNotContain("id=\"search-toggle-btn\"", indexHtml);
    }

    [Fact]
    public void Verify_OptionD_Monaco_Visual_Elements_Present()
    {
        var root = GetRepoRoot();
        var indexHtml = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "index.html"));

        // Double-chevron subsidiary context switcher in sidebar header
        Assert.Contains("data-lucide=\"chevrons-up-down\"", indexHtml);
        Assert.Contains("openCompanyModal()", indexHtml);

        // Sidebar quick search input with keyboard shortcut support (badge removed for clean UI)
        Assert.Contains("id=\"sidebar-quick-search\"", indexHtml);
        Assert.DoesNotContain("⌘ K", indexHtml);
        Assert.DoesNotContain("Ctrl + K", indexHtml);

        // NAVIGATE uppercase section header
        Assert.Contains("NAVIGATE", indexHtml);

        // Vertical tree connector group
        Assert.Contains("tree-connector-group", indexHtml);

        // Tooltip attributes for collapsed rail mode
        Assert.Contains("data-tooltip=\"General Ledger\"", indexHtml);
        Assert.Contains("data-tooltip=\"Accounts Payable\"", indexHtml);
        Assert.Contains("data-tooltip=\"Sourcing &amp; Procurement\"", indexHtml);
    }

    [Fact]
    public void Verify_OptionD_CSS_Rules_In_DesignSystem()
    {
        var root = GetRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "css", "ERP-design-system.css"));

        // Floating sidebar geometry
        Assert.Contains(".floating-sidebar", css);
        Assert.Contains("border-radius: 18px", css);

        // Monaco 58px collapsed slim rail
        Assert.Contains(".sidebar-collapsed .floating-sidebar", css);
        Assert.Contains("width: 58px", css);
        Assert.Contains(".sidebar-collapsed .main-wrapper", css);

        // Floating hover tooltips in collapsed mode
        Assert.Contains(".sidebar-collapsed .nav-item[data-tooltip]::after", css);

        // Unified primary blue active pill styling (replacing dark charcoal #18181B / #1E2330)
        Assert.Contains(".nav-item.active", css);
        Assert.Contains("#2563EB", css);
        Assert.DoesNotContain("#18181B", css);
        Assert.DoesNotContain("#1E2330", css);

        // Tree connector line
        Assert.Contains(".tree-connector-group", css);
        Assert.Contains(".tree-sub-item", css);
        Assert.Contains(".tree-sub-item.active", css);
        Assert.Contains(".accordion-content", css);
    }

    [Fact]
    public void Verify_OptionD_JavaScript_Controllers()
    {
        var root = GetRepoRoot();
        var coreJs = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "js", "ERP-core.js"));
        var studiosJs = File.ReadAllText(Path.Combine(root, "src", "ERP.Web.Api", "wwwroot", "js", "erp-studios.js"));

        // Accordion controllers
        Assert.Contains("function toggleModuleAccordion", coreJs);
        Assert.Contains("function toggleGlTreeAccordion", coreJs);

        // Quick search filter
        Assert.Contains("function filterSidebarItems", coreJs);
        Assert.Contains("function initSidebarQuickSearch", coreJs);

        // Studios parent module resolution & sync
        Assert.Contains("function getParentModuleForStudio", studiosJs);
        Assert.Contains("document.querySelectorAll('.tree-sub-item')", studiosJs);
        Assert.Contains("toggleModuleAccordion(parentModule, true)", studiosJs);
    }
}
