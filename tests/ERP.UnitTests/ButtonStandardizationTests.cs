using System.Text.RegularExpressions;
using Xunit;

namespace ERP.UnitTests;

/// <summary>
/// Automated system-wide verification tests enforcing the mandatory
/// "Icon + One-Word Label" button standard across all frontend files.
/// </summary>
public class ButtonStandardizationTests
{
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

    private static readonly Regex ButtonRegex = new(
        @"<button\b(?<attrs>[^>]*)>(?<body>[\s\S]*?)</button>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    [Fact]
    public void IndexHtml_AllButtons_MustFollowIconPlusOneWordStandard_OrDocumentedException()
    {
        var html = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "index.html"));
        var matches = ButtonRegex.Matches(html);
        Assert.NotEmpty(matches);

        var violations = new List<string>();

        foreach (Match match in matches)
        {
            var attrs = match.Groups["attrs"].Value;
            var body = match.Groups["body"].Value;
            var fullButton = match.Value;

            // Special-purpose context selector pills in the top header bar
            if (attrs.Contains("id=\"header-company-switcher-btn\"", StringComparison.Ordinal) ||
                attrs.Contains("id=\"user-profile-btn\"", StringComparison.Ordinal))
            {
                if (!attrs.Contains("aria-label=", StringComparison.Ordinal) &&
                    !attrs.Contains("title=", StringComparison.Ordinal))
                {
                    violations.Add($"Context selector button missing accessible label: {fullButton}");
                }
                continue;
            }

            // Every button must have at least one Lucide icon
            bool hasIcon = body.Contains("data-lucide=", StringComparison.Ordinal);
            if (!hasIcon)
            {
                violations.Add($"Button is missing a Lucide icon (data-lucide): {CollapseWhitespace(fullButton)}");
                continue;
            }

            // Extract visible text by stripping all HTML tags
            var visibleText = CollapseWhitespace(HtmlTagRegex.Replace(body, " ")).Trim();

            // Icon-only button exception check
            if (string.IsNullOrEmpty(visibleText))
            {
                bool isJustifiedIconOnly =
                    attrs.Contains("id=\"sidebar-toggle-btn\"", StringComparison.Ordinal) ||
                    attrs.Contains("id=\"search-clear-btn\"", StringComparison.Ordinal) ||
                    attrs.Contains("id=\"search-toggle-btn\"", StringComparison.Ordinal) ||
                    attrs.Contains("id=\"theme-toggle-btn\"", StringComparison.Ordinal) ||
                    attrs.Contains("id=\"notification-bell-btn\"", StringComparison.Ordinal) ||
                    attrs.Contains("removeJvLine(this)", StringComparison.Ordinal) ||
                    attrs.Contains("this.closest('tr').remove()", StringComparison.Ordinal) ||
                    attrs.Contains("Minimum 1 item required on delivery challan", StringComparison.Ordinal) ||
                    attrs.Contains("closeCompanyModal()", StringComparison.Ordinal) ||
                    attrs.Contains("title=\"Previous Page\"", StringComparison.Ordinal) ||
                    attrs.Contains("title=\"Next Page\"", StringComparison.Ordinal);

                if (!isJustifiedIconOnly)
                {
                    violations.Add($"Unjustified icon-only button found: {CollapseWhitespace(fullButton)}");
                    continue;
                }

                bool hasAccessibleName =
                    attrs.Contains("title=", StringComparison.Ordinal) ||
                    attrs.Contains("aria-label=", StringComparison.Ordinal);

                if (!hasAccessibleName)
                {
                    violations.Add($"Icon-only button is missing title/aria-label: {CollapseWhitespace(fullButton)}");
                }

                continue;
            }

            // Standard action button: must have exactly ONE visible word
            var words = visibleText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length != 1)
            {
                violations.Add($"Button must have exactly 1 visible word (found {words.Length}: \"{visibleText}\"): {CollapseWhitespace(fullButton)}");
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} button standardization violation(s) in index.html:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void JsFiles_AllDynamicButtonTemplates_MustFollowIconPlusOneWordStandard()
    {
        var jsFiles = new[]
        {
            Path.Combine("src", "ERP.Web.Api", "wwwroot", "js", "ERP-core.js"),
            Path.Combine("src", "ERP.Web.Api", "wwwroot", "js", "erp-studios.js")
        };

        var violations = new List<string>();

        foreach (var relativePath in jsFiles)
        {
            var content = ReadFrontendFile(relativePath);
            var matches = ButtonRegex.Matches(content);

            foreach (Match match in matches)
            {
                var attrs = match.Groups["attrs"].Value;
                var body = match.Groups["body"].Value;
                var fullButton = match.Value;

                // Skip the generic templates inside renderErpButton() helper which interpolate ${iconName} / ${titleAttr}
                if (attrs.Contains("${titleAttr}", StringComparison.Ordinal) &&
                    body.Contains("${iconName}", StringComparison.Ordinal))
                {
                    continue;
                }

                bool hasIcon = body.Contains("data-lucide=", StringComparison.Ordinal);
                if (!hasIcon)
                {
                    violations.Add($"[{relativePath}] Button missing Lucide icon: {CollapseWhitespace(fullButton)}");
                    continue;
                }

                var visibleText = CollapseWhitespace(HtmlTagRegex.Replace(body, " ")).Trim();

                // Icon-only buttons in JS (e.g. closeAuditHistoryModal, removeJvLine, this.closest('tr').remove())
                if (string.IsNullOrEmpty(visibleText))
                {
                    bool hasAccessibleName =
                        attrs.Contains("title=", StringComparison.Ordinal) ||
                        attrs.Contains("aria-label=", StringComparison.Ordinal);

                    if (!hasAccessibleName)
                    {
                        violations.Add($"[{relativePath}] Icon-only button missing title/aria-label: {CollapseWhitespace(fullButton)}");
                    }
                    continue;
                }

                var words = visibleText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length != 1)
                {
                    violations.Add($"[{relativePath}] Button must have 1 visible word (found {words.Length}: \"{visibleText}\"): {CollapseWhitespace(fullButton)}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} button standardization violation(s) in JS files:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void ErpStudiosJs_DynamicButtonTextMutations_MustUseSingleWordLabels()
    {
        var studiosJs = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "js", "erp-studios.js"));

        // Match assignments to button text variables (txt.textContent = '...' or btnText.textContent = '...')
        var textAssignmentRegex = new Regex(
            @"\b(?:txt|btnText)\.textContent\s*=\s*['""](?<label>[^'""]+)['""]",
            RegexOptions.Compiled);

        var matches = textAssignmentRegex.Matches(studiosJs);
        Assert.NotEmpty(matches);

        foreach (Match match in matches)
        {
            var label = match.Groups["label"].Value.Trim();
            var words = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(words.Length == 1,
                $"Dynamic button label assignment must be a single word, but found \"{label}\" in: {match.Value}");
        }
    }

    [Fact]
    public void ReusableButtonSystem_AndDocumentation_MustExist()
    {
        var coreJs = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "js", "ERP-core.js"));
        Assert.Contains("ERP_BUTTON_STANDARDS", coreJs);
        Assert.Contains("function renderErpButton(", coreJs);
        Assert.Contains("function setErpButtonLoading(", coreJs);
        Assert.Contains("function hydrateErpButtons(", coreJs);

        var css = ReadFrontendFile(Path.Combine("src", "ERP.Web.Api", "wwwroot", "css", "ERP-design-system.css"));
        Assert.Contains(".erp-btn", css);
        Assert.Contains(".erp-btn-sm", css);
        Assert.Contains(".erp-btn-xs", css);
        Assert.Contains(".erp-btn-icon", css);
        Assert.Contains(".btn-action-cancel", css);

        var doc = ReadFrontendFile(Path.Combine("docs", "button-standard.md"));
        Assert.Contains("Icon + One-Word Label", doc);
        Assert.Contains("ERP_BUTTON_STANDARDS", doc);
    }

    private static string CollapseWhitespace(string input)
    {
        return Regex.Replace(input, @"\s+", " ");
    }
}
