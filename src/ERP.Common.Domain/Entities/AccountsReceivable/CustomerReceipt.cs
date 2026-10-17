using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.AccountsReceivable;

public enum ReceiptPaymentMethod
{
    BankTransfer = 1,
    Cheque = 2,
    Cash = 3
}

public enum CustomerReceiptStatus
{
    Draft = 0,
    Posted = 1,
    Voided = 2
}

public class CustomerReceipt : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int CustomerId { get; set; }
    public ReceiptPaymentMethod PaymentMethod { get; set; } = ReceiptPaymentMethod.BankTransfer;
    public int? BankAccountId { get; set; }
    public int? CashBookId { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Remarks { get; set; }
    public CustomerReceiptStatus Status { get; set; } = CustomerReceiptStatus.Posted;
    public int? JournalVoucherId { get; set; }

    public ICollection<CustomerReceiptAllocation> Allocations { get; set; } = new List<CustomerReceiptAllocation>();

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public CustomerReceipt() { }
}

public class CustomerReceiptAllocation : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public int ReceiptId { get; set; }
    public int InvoiceId { get; set; }
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

    public CustomerReceiptAllocation() { }
}
