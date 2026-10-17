namespace ERP.Common.Infrastructure.Reporting.Models;

public record CompanyReportHeaderModel(
    string CompanyName,
    string? Address,
    string? City,
    string? Phone,
    string? Email,
    string? TaxOrBin,
    string Currency = "BDT"
);
