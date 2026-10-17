using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ERP.UnitTests;

public class DedicatedFullPageScreensTests
{
    private readonly string _wwwrootPath;

    public DedicatedFullPageScreensTests()
    {
        // Traverse up to repository root from test bin directory
        var baseDir = AppContext.BaseDirectory;
        var projectDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../../"));
        _wwwrootPath = Path.Combine(projectDir, "src", "ERP.Web.Api", "wwwroot");
    }

    [Fact]
    public void Verify_AllModules_HaveDedicatedFullPageScreens_InIndexHtml()
    {
        var htmlPath = Path.Combine(_wwwrootPath, "index.html");
        Assert.True(File.Exists(htmlPath), $"index.html must exist at {htmlPath}");

        var htmlContent = File.ReadAllText(htmlPath);

        // All 30 required full-page studios across the entire ERP
        var requiredStudioIds = new[]
        {
            // Core Workspaces & Dashboards
            "studio-dashboard",
            "studio-workspace",
            // Financial & General Ledger
            "studio-jv",
            "studio-trial-balance",
            "studio-entry-account",
            // Cash & Treasury
            "studio-cash-book",
            "studio-entry-contra",
            "studio-entry-bank",
            // Accounts Receivable & Order to Cash
            "studio-ar-aging",
            "studio-receipts",
            "studio-sales-order",
            "studio-entry-customer",
            "studio-entry-challan",
            "studio-entry-invoice",
            // Accounts Payable & Sourcing
            "studio-ap-aging",
            "studio-po",
            "studio-grn",
            "studio-bill-entry",
            "studio-entry-vendor",
            "studio-entry-pr",
            // Inventory & Supply Chain
            "studio-stock",
            "studio-entry-product",
            "studio-entry-warehouse",
            "studio-entry-stock-transfer",
            // HRIS & Payroll
            "studio-entry-employee",
            "studio-payslip",
            "studio-payroll-batch",
            // Specialized Engineering & Assets
            "studio-entry-asset",
            "studio-entry-bom",
            // Governance & System Administration
            "studio-audit-trail"
        };

        foreach (var studioId in requiredStudioIds)
        {
            var marker = $"id=\"{studioId}\"";
            Assert.Contains(marker, htmlContent);
        }
    }

    [Fact]
    public void Verify_AllDedicatedEntryScreens_HaveStandardStructure_AndActionButtons()
    {
        var htmlPath = Path.Combine(_wwwrootPath, "index.html");
        var htmlContent = File.ReadAllText(htmlPath);

        var dedicatedEntryStudios = new[]
        {
            "studio-entry-customer",
            "studio-entry-vendor",
            "studio-entry-product",
            "studio-entry-account",
            "studio-entry-warehouse",
            "studio-entry-employee",
            "studio-entry-contra",
            "studio-entry-challan",
            "studio-entry-invoice",
            "studio-entry-pr",
            "studio-entry-stock-transfer",
            "studio-entry-bank",
            "studio-entry-asset",
            "studio-entry-bom",
            "studio-payroll-batch",
            "studio-audit-trail"
        };

        foreach (var studioId in dedicatedEntryStudios)
        {
            Assert.Contains($"id=\"{studioId}\"", htmlContent);

            // Each studio section must contain responsive cards and back button
            var studioStartIndex = htmlContent.IndexOf($"id=\"{studioId}\"", StringComparison.Ordinal);
            Assert.True(studioStartIndex >= 0, $"Studio {studioId} must exist in HTML");

            // Look inside the next 15,000 characters for standard components
            var sectionSnippet = htmlContent.Substring(studioStartIndex, Math.Min(15000, htmlContent.Length - studioStartIndex));

            Assert.Contains("btn-action-back", sectionSnippet);
            Assert.Contains("glass-card", sectionSnippet);
        }
    }

    [Fact]
    public void Verify_NavigationRouterAndBreadcrumbs_MapAllDedicatedStudios()
    {
        var jsPath = Path.Combine(_wwwrootPath, "js", "erp-studios.js");
        Assert.True(File.Exists(jsPath), $"erp-studios.js must exist at {jsPath}");

        var jsContent = File.ReadAllText(jsPath);

        var requiredViewKeys = new[]
        {
            "dashboard", "workspace", "jv", "stock", "payslip", "sales-order", "po", "grn", 
            "bill-entry", "receipts", "trial-balance", "ar-aging", "ap-aging", "cash-book",
            "entry-customer", "entry-vendor", "entry-product", "entry-account", "entry-warehouse", 
            "entry-employee", "entry-contra",
            "entry-challan", "entry-invoice", "entry-pr", "entry-stock-transfer", "entry-bank",
            "entry-asset", "entry-bom", "payroll-batch", "audit-trail"
        };

        // Assert all views are registered in allViews array
        foreach (var key in requiredViewKeys)
        {
            Assert.Contains($"'{key}'", jsContent);
        }

        // Assert breadcrumbs dictionary contains all 9 new studios
        var newStudioKeys = new[]
        {
            "entry-challan", "entry-invoice", "entry-pr", "entry-stock-transfer", 
            "entry-bank", "entry-asset", "entry-bom", "payroll-batch", "audit-trail"
        };

        foreach (var key in newStudioKeys)
        {
            Assert.Contains($"'{key}':", jsContent);
        }
    }

    [Fact]
    public void Verify_AuditTrail_ConvertedFromModalToDedicatedStudio()
    {
        var htmlPath = Path.Combine(_wwwrootPath, "index.html");
        var htmlContent = File.ReadAllText(htmlPath);
        Assert.Contains("id=\"studio-audit-trail\"", htmlContent);
        Assert.Contains("id=\"audit-logs-tbody\"", htmlContent);
        Assert.Contains("id=\"diff-content-container\"", htmlContent);

        var coreJsPath = Path.Combine(_wwwrootPath, "js", "ERP-core.js");
        if (!File.Exists(coreJsPath))
        {
            coreJsPath = Path.Combine(_wwwrootPath, "js", "erp-core.js");
        }
        if (!File.Exists(coreJsPath))
        {
            coreJsPath = Path.Combine(_wwwrootPath, "js", "pyrix-core.js");
        }
        var coreJsContent = File.ReadAllText(coreJsPath);

        // The audit modal in ERP-core must contain direct navigation to the dedicated full-page studio
        Assert.Contains("navigateToStudio('audit-trail')", coreJsContent);
        Assert.Contains("Open Dedicated Audit Studio", coreJsContent);
    }
}
