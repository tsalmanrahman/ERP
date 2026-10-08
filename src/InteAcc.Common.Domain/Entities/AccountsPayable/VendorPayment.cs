using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.AccountsPayable;

public enum PaymentMethod
{
    BankTransfer = 1,
    Cheque = 2,
    Cash = 3
}

public enum PaymentStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public class VendorPayment : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public int VendorId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public int? BankAccountId { get; set; }
    public int? CashBookId { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Remarks { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Posted;
    public int? JournalVoucherId { get; set; }

    public ICollection<VendorPaymentAllocation> Allocations { get; set; } = new List<VendorPaymentAllocation>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public VendorPayment() { }
}

public class VendorPaymentAllocation : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int PaymentId { get; set; }
    public int BillId { get; set; }
    public decimal AllocatedAmount { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public VendorPaymentAllocation() { }
}
