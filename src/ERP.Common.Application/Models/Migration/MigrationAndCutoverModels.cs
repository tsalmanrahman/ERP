namespace ERP.Common.Application.Models.Migration;

public class EtlMigrationRequest
{
    public int CompanyId { get; set; } = 1;
    public string? BatchNumber { get; set; }
    public List<string> ModulesToMigrate { get; set; } = new() { "gl", "ar", "ap", "inv", "hr", "fa" };
    public bool IncludeOpeningBalances { get; set; } = true;
    public DateTime? HistoricalCutoffDate { get; set; }
}

public class ModuleMigrationSummaryDto
{
    public string SourceModule { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public int RecordsExtracted { get; set; }
    public int RecordsMigrated { get; set; }
    public int RecordsFailed { get; set; }
    public string Status { get; set; } = "Completed";
    public long DurationMs { get; set; }
    public string? Details { get; set; }
}

public class EtlMigrationSummaryDto
{
    public string BatchNumber { get; set; } = string.Empty;
    public int TotalExtracted { get; set; }
    public int TotalMigrated { get; set; }
    public int TotalFailed { get; set; }
    public long TotalDurationMs { get; set; }
    public bool IsSuccess => TotalFailed == 0;
    public List<ModuleMigrationSummaryDto> ModuleSummaries { get; set; } = new();
}

public class SubledgerParityItem
{
    public string SubledgerName { get; set; } = string.Empty;
    public string ControlAccountCodes { get; set; } = string.Empty;
    public decimal ControlAccountBalance { get; set; }
    public decimal SubledgerTotal { get; set; }
    public decimal Variance { get; set; }
    public bool IsBalanced => Math.Abs(Variance) < 0.0001m;
    public string Remarks { get; set; } = string.Empty;
}

public class GeneralLedgerParityDto
{
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal Variance { get; set; }
    public bool IsBalanced => Math.Abs(Variance) < 0.0001m;
    public int TotalPostedVouchers { get; set; }
    public int TotalVoucherLines { get; set; }
}

public class FinancialParityAuditReport
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime AuditTimestamp { get; set; } = DateTime.UtcNow;
    public GeneralLedgerParityDto GeneralLedgerParity { get; set; } = new();
    public List<SubledgerParityItem> SubledgerReconciliations { get; set; } = new();
    public bool IsSystemParityAchieved => GeneralLedgerParity.IsBalanced && SubledgerReconciliations.All(s => s.IsBalanced);
    public int CriticalDiscrepanciesCount => (GeneralLedgerParity.IsBalanced ? 0 : 1) + SubledgerReconciliations.Count(s => !s.IsBalanced);
    public string AuditSummaryMessage { get; set; } = string.Empty;
}

public class ChecklistItem
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public string Details { get; set; } = string.Empty;
}

public class CutoverChecklistResponse
{
    public int CompanyId { get; set; }
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool DatabaseConnectivityVerified { get; set; }
    public bool FiscalYearAndPeriodOpen { get; set; }
    public bool TrialBalanceVarianceZero { get; set; }
    public bool AllSubledgersReconciled { get; set; }
    public bool NoUnpostedDraftVouchers { get; set; }
    public bool RbacAndSecurityConfigured { get; set; }
    public bool BackupSystemReady { get; set; }
    public bool OverallCutoverReady => DatabaseConnectivityVerified && FiscalYearAndPeriodOpen &&
                                       TrialBalanceVarianceZero && AllSubledgersReconciled &&
                                       NoUnpostedDraftVouchers && RbacAndSecurityConfigured && BackupSystemReady;
    public List<ChecklistItem> ChecklistItems { get; set; } = new();
}

public class CutoverSignoffRequest
{
    public int CompanyId { get; set; } = 1;
    public string SignedOffBy { get; set; } = string.Empty;
    public string CfoApprovalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CutoverSignoffResult
{
    public int AuditId { get; set; }
    public DateTime CutoverDate { get; set; }
    public string SignedOffBy { get; set; } = string.Empty;
    public string CfoApprovalCode { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class HistoricalArchiveDto
{
    public long Id { get; set; }
    public string FiscalYear { get; set; } = string.Empty;
    public int PeriodMonth { get; set; }
    public string LegacyVoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Particulars { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string CurrencyCode { get; set; } = "BDT";
    public string LegacySourceModule { get; set; } = "GL";
}
