using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.Migration;

public class HistoricalLedgerArchive : Entity<long>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
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

    // Audit and Soft Delete
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; } = "SYSTEM";
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
