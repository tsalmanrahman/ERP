using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Domain.Entities.Procurement;

namespace ERP.Common.Application.Models.ProcureToPay;

// --- Vendor DTOs ---

public class VendorDto
{
    public int Id { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int VendorGroupId { get; set; }
    public string? VendorGroupName { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string CountryCode { get; set; } = "BGD";
    public string? TaxId { get; set; }
    public int GlControlAccountId { get; set; }
    public string? GlControlAccountCode { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditPeriodDays { get; set; }
    public bool IsActive { get; set; }
}

public class CreateVendorRequest
{
    public string? VendorCode { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorGroupId { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string CountryCode { get; set; } = "BGD";
    public string? TaxId { get; set; }
    public int? GlControlAccountId { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditPeriodDays { get; set; } = 30;
}

public class VendorGroupDto
{
    public int Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}

// --- Purchase Order DTOs ---

public class CreatePurchaseOrderRequest
{
    public DateTime PoDate { get; set; }
    public int VendorId { get; set; }
    public int WarehouseId { get; set; }
    public List<CreatePurchaseOrderLineRequest> Lines { get; set; } = new();
}

public class CreatePurchaseOrderLineRequest
{
    public int ProductId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public DateTime PoDate { get; set; }
    public int VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class PurchaseOrderLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

// --- Purchase Bill DTOs ---

public class CreatePurchaseBillRequest
{
    public DateTime BillDate { get; set; }
    public int VendorId { get; set; }
    public string? VendorInvoiceNumber { get; set; }
    public int? PoId { get; set; }
    public int? GrnId { get; set; }
    public DateTime? DueDate { get; set; }
    public List<CreatePurchaseBillLineRequest> Lines { get; set; } = new();
}

public class CreatePurchaseBillLineRequest
{
    public int? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public int? ExpenseAccountId { get; set; }
}

public class PurchaseBillDto
{
    public int Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? VendorInvoiceNumber { get; set; }
    public int? PoId { get; set; }
    public string? PoNumber { get; set; }
    public int? GrnId { get; set; }
    public string? GrnNumber { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public PurchaseBillStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
    public List<PurchaseBillLineDto> Lines { get; set; } = new();
}

public class PurchaseBillLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public int ExpenseAccountId { get; set; }
}

// --- 3-Way Matching DTO ---

public class ThreeWayMatchLineResult
{
    public int LineNumber { get; set; }
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public decimal PoQuantity { get; set; }
    public decimal GrnQuantity { get; set; }
    public decimal BilledQuantity { get; set; }
    public decimal PoUnitPrice { get; set; }
    public decimal GrnUnitPrice { get; set; }
    public decimal BilledUnitPrice { get; set; }
    public bool QuantityMatches { get; set; }
    public bool PriceMatches { get; set; }
    public string Status { get; set; } = "MATCHED"; // MATCHED, QUANTITY_VARIANCE, PRICE_VARIANCE
    public string? DiscrepancyNotes { get; set; }
}

public class ThreeWayMatchResultDto
{
    public int BillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string? PoNumber { get; set; }
    public string? GrnNumber { get; set; }
    public bool IsFullyMatched { get; set; }
    public decimal TotalPoAmount { get; set; }
    public decimal TotalGrnAmount { get; set; }
    public decimal TotalBilledAmount { get; set; }
    public decimal PriceVariance { get; set; }
    public List<ThreeWayMatchLineResult> Lines { get; set; } = new();
}

// --- Vendor Payment DTOs ---

public class CreateVendorPaymentRequest
{
    public DateTime PaymentDate { get; set; }
    public int VendorId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } // 1=Bank, 2=Cheque, 3=Cash
    public int? BankAccountId { get; set; }
    public int? CashBookId { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Remarks { get; set; }
    public List<PaymentAllocationRequest> Allocations { get; set; } = new();
}

public class PaymentAllocationRequest
{
    public int BillId { get; set; }
    public decimal AllocatedAmount { get; set; }
}

public class VendorPaymentDto
{
    public int Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public int? BankAccountId { get; set; }
    public string? BankAccountTitle { get; set; }
    public int? CashBookId { get; set; }
    public string? CashBookName { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Remarks { get; set; }
    public PaymentStatus Status { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
    public List<VendorPaymentAllocationDto> Allocations { get; set; } = new();
}

public class VendorPaymentAllocationDto
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
}

// --- Reports DTOs ---

public class VendorAgingRow
{
    public int VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal CurrentDays { get; set; }      // 0-30 days
    public decimal ThirtyToSixty { get; set; }    // 31-60 days
    public decimal SixtyToNinety { get; set; }    // 61-90 days
    public decimal NinetyToOneTwenty { get; set; }// 91-120 days
    public decimal OverOneTwenty { get; set; }    // >120 days
    public decimal TotalOutstanding { get; set; }
}

public class VendorAgingReport
{
    public DateOnly AsOfDate { get; set; }
    public decimal GrandTotalOutstanding { get; set; }
    public List<VendorAgingRow> Vendors { get; set; } = new();
}

public class VendorStatementRow
{
    public DateTime TransactionDate { get; set; }
    public string DocumentType { get; set; } = string.Empty; // BILL, PAYMENT
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal BillAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RunningBalance { get; set; }
}

public class VendorStatementReport
{
    public int VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<VendorStatementRow> Rows { get; set; } = new();
}
