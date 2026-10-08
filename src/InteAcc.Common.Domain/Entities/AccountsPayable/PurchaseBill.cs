using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.AccountsPayable;

public enum PurchaseBillStatus
{
    Draft = 0,
    Confirmed = 1,
    PartiallyPaid = 2,
    FullyPaid = 3,
    Cancelled = 4
}

public class PurchaseBill : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public int VendorId { get; set; }
    public string? VendorInvoiceNumber { get; set; }
    public int? PoId { get; set; }
    public int? GrnId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public PurchaseBillStatus Status { get; set; } = PurchaseBillStatus.Draft;
    public DateTime? DueDate { get; set; }
    public int? JournalVoucherId { get; set; }

    public ICollection<PurchaseBillLine> Lines { get; set; } = new List<PurchaseBillLine>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public PurchaseBill() { }
}

public class PurchaseBillLine : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int BillId { get; set; }
    public int LineNumber { get; set; }
    public int? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public int ExpenseAccountId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public PurchaseBillLine() { }
}
