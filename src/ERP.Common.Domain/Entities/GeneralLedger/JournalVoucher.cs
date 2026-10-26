using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.GeneralLedger;

public class JournalVoucher : AggregateRoot<int>, IAuditableEntity, ISoftDeletable, IMultiTenant
{
    public int BatchId { get; set; }
    public int CompanyId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateOnly VoucherDate { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public int FiscalYearId { get; set; }
    public int FiscalPeriodId { get; set; }
    public bool IsPosted { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public string? PostedBy { get; set; }

    public List<JournalVoucherLine> Lines { get; set; } = new();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public JournalVoucher() { }

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit;
}

public class JournalVoucherLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int VoucherId { get; set; }
    public int LineNumber { get; set; }
    public int AccountId { get; set; }
    public int? SubAccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string CurrencyCode { get; set; } = "BDT";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal BaseDebit { get; set; }
    public decimal BaseCredit { get; set; }
    public string? Particulars { get; set; }
    public int? BusinessUnitId { get; set; }

    // Navigation
    public JournalVoucher? Voucher { get; set; }
    public Account? Account { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public JournalVoucherLine() { }
}
