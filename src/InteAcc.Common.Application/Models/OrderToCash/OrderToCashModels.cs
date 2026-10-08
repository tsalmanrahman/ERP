using InteAcc.Common.Domain.Entities.AccountsReceivable;
using InteAcc.Common.Domain.Entities.Sales;

namespace InteAcc.Common.Application.Models.OrderToCash;

// --- Customer DTOs ---

public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerGroupId { get; set; }
    public string? CustomerGroupName { get; set; }
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

public class CreateCustomerRequest
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerGroupId { get; set; }
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

public class CustomerGroupDto
{
    public int Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}

// --- Sales Center DTOs ---

public class SalesCenterDto
{
    public int Id { get; set; }
    public string CenterCode { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateSalesCenterRequest
{
    public string CenterCode { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
}

// --- Sales Order DTOs ---

public class CreateSalesOrderRequest
{
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public int SalesCenterId { get; set; }
    public List<CreateSalesOrderLineRequest> Lines { get; set; } = new();
}

public class CreateSalesOrderLineRequest
{
    public int ProductId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SalesOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int SalesCenterId { get; set; }
    public string SalesCenterName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public SalesOrderStatus OrderStatus { get; set; }
    public List<SalesOrderLineDto> Lines { get; set; } = new();
}

public class SalesOrderLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

// --- Delivery Challan DTOs ---

public class CreateDeliveryChallanRequest
{
    public DateTime ChallanDate { get; set; }
    public int SalesOrderId { get; set; }
    public int WarehouseId { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Remarks { get; set; }
    public List<CreateDeliveryChallanLineRequest> Lines { get; set; } = new();
}

public class CreateDeliveryChallanLineRequest
{
    public int ProductId { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public string? BatchNumber { get; set; }
}

public class DeliveryChallanDto
{
    public int Id { get; set; }
    public string ChallanNumber { get; set; } = string.Empty;
    public DateTime ChallanDate { get; set; }
    public int SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalCogsAmount { get; set; }
    public DeliveryChallanStatus Status { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Remarks { get; set; }
    public List<DeliveryChallanLineDto> Lines { get; set; } = new();
}

public class DeliveryChallanLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal DispatchedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCogsAmount { get; set; }
    public string? BatchNumber { get; set; }
}

// --- Invoice DTOs ---

public class CreateInvoiceRequest
{
    public DateTime InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public int? SalesOrderId { get; set; }
    public int? DeliveryChallanId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public List<CreateInvoiceLineRequest> Lines { get; set; } = new();
}

public class CreateInvoiceLineRequest
{
    public int? ProductId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public int? RevenueAccountId { get; set; }
}

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int? SalesOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public int? DeliveryChallanId { get; set; }
    public string? ChallanNumber { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = new();
}

public class InvoiceLineDto
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
    public int RevenueAccountId { get; set; }
}

// --- Customer Receipt DTOs ---

public class CreateCustomerReceiptRequest
{
    public DateTime ReceiptDate { get; set; }
    public int CustomerId { get; set; }
    public ReceiptPaymentMethod PaymentMethod { get; set; } // 1=Bank, 2=Cheque, 3=Cash
    public int? BankAccountId { get; set; }
    public int? CashBookId { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Remarks { get; set; }
    public List<ReceiptAllocationRequest> Allocations { get; set; } = new();
}

public class ReceiptAllocationRequest
{
    public int InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
}

public class CustomerReceiptDto
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public ReceiptPaymentMethod PaymentMethod { get; set; }
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
    public CustomerReceiptStatus Status { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
    public List<CustomerReceiptAllocationDto> Allocations { get; set; } = new();
}

public class CustomerReceiptAllocationDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
}

// --- AR Reporting DTOs ---

public class CustomerAgingRow
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal CurrentDays { get; set; }      // 0-30 days
    public decimal ThirtyToSixty { get; set; }    // 31-60 days
    public decimal SixtyToNinety { get; set; }    // 61-90 days
    public decimal NinetyToOneTwenty { get; set; }// 91-120 days
    public decimal OverOneTwenty { get; set; }    // >120 days
    public decimal TotalOutstanding { get; set; }
}

public class CustomerAgingReport
{
    public DateOnly AsOfDate { get; set; }
    public decimal GrandTotalOutstanding { get; set; }
    public List<CustomerAgingRow> Customers { get; set; } = new();
}

public class CustomerStatementRow
{
    public DateTime TransactionDate { get; set; }
    public string DocumentType { get; set; } = string.Empty; // INVOICE, RECEIPT
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal ReceiptAmount { get; set; }
    public decimal RunningBalance { get; set; }
}

public class CustomerStatementReport
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalReceipt { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<CustomerStatementRow> Rows { get; set; } = new();
}
