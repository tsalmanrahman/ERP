using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Application.Models.Payroll;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.Sales;
using ERP.Common.Infrastructure.Reporting.Documents;
using ERP.Common.Infrastructure.Reporting.Models;
using Xunit;

namespace ERP.UnitTests;

public class PdfReportingEngineTests
{
    private readonly CompanyReportHeaderModel _companyHeader;

    public PdfReportingEngineTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        _companyHeader = new CompanyReportHeaderModel(
            CompanyName: "ERP Global Enterprise Ltd.",
            Address: "Gulshan Commercial Area",
            City: "Dhaka",
            Phone: "+880-2-9876543",
            Email: "finance@erp.local",
            TaxOrBin: "BIN-002918274-0101",
            Currency: "BDT"
        );
    }

    [Fact]
    public void JournalVoucherDocument_ShouldGenerateValidPdf()
    {
        var voucher = new JournalVoucherDto(
            Id: 101,
            CompanyId: 1,
            VoucherNumber: "JV-2026-0001",
            VoucherDate: new DateOnly(2026, 10, 1),
            Reference: "REF-OCT-PAYMENT",
            Description: "Settlement of supplier accounts and inventory replenishment",
            FiscalYearId: 1,
            FiscalPeriodId: 10,
            IsPosted: true,
            PostedAtUtc: DateTime.UtcNow,
            PostedBy: "chief.accountant",
            TotalDebit: 55000.00m,
            TotalCredit: 55000.00m,
            IsBalanced: true,
            Lines: new List<JournalVoucherLineDto>
            {
                new(1, 1, 10, "1010", "Cash in Hand", 0m, 55000.00m, "Payment disbursement"),
                new(2, 2, 20, "2010", "Accounts Payable - Vendors", 55000.00m, 0m, "Settlement of vendor bills")
            }
        );

        var doc = new JournalVoucherDocument(voucher, _companyHeader);
        var bytes = doc.GeneratePdf();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "Generated Journal Voucher PDF must be larger than 1KB.");
        // Verify PDF Magic Header %PDF-
        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }

    [Fact]
    public void SalesInvoiceDocument_ShouldGenerateValidPdf()
    {
        var invoice = new InvoiceDto
        {
            Id = 201,
            InvoiceNumber = "INV-2026-0089",
            InvoiceDate = DateTime.UtcNow,
            CustomerId = 5,
            CustomerCode = "CUST-005",
            CustomerName = "Apex Retail Solutions Ltd.",
            OrderNumber = "SO-2026-0012",
            ChallanNumber = "DC-2026-0012",
            SubTotal = 100000.00m,
            DiscountAmount = 5000.00m,
            TaxAmount = 4750.00m,
            TotalAmount = 99750.00m,
            PaidAmount = 0m,
            BalanceAmount = 99750.00m,
            Status = InvoiceStatus.Confirmed,
            DueDate = DateTime.UtcNow.AddDays(30),
            Lines = new List<InvoiceLineDto>
            {
                new()
                {
                    Id = 1,
                    LineNumber = 1,
                    ProductCode = "PRD-STEEL-01",
                    ProductName = "Reinforced Steel Rods 12mm",
                    Quantity = 100,
                    UnitPrice = 1000.00m,
                    LineAmount = 100000.00m,
                    TaxAmount = 4750.00m
                }
            }
        };

        var doc = new SalesInvoiceDocument(invoice, _companyHeader);
        var bytes = doc.GeneratePdf();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "Generated Sales Invoice PDF must be larger than 1KB.");
        Assert.Equal(0x25, bytes[0]);
    }

    [Fact]
    public void DeliveryChallanDocument_ShouldGenerateValidPdf()
    {
        var challan = new DeliveryChallanDto
        {
            Id = 301,
            ChallanNumber = "DC-2026-0045",
            ChallanDate = DateTime.UtcNow,
            SalesOrderId = 12,
            OrderNumber = "SO-2026-0012",
            CustomerId = 5,
            CustomerName = "Apex Retail Solutions Ltd.",
            WarehouseId = 1,
            WarehouseName = "Central Finished Goods Warehouse",
            TotalCogsAmount = 75000.00m,
            Status = DeliveryChallanStatus.Dispatched,
            VehicleNumber = "DM-TA-8921",
            DriverName = "Rahim Mia",
            Remarks = "Dispatched via refrigerated transport",
            Lines = new List<DeliveryChallanLineDto>
            {
                new()
                {
                    Id = 1,
                    LineNumber = 1,
                    ProductCode = "PRD-STEEL-01",
                    ProductName = "Reinforced Steel Rods 12mm",
                    DispatchedQuantity = 100,
                    UnitCost = 750.00m,
                    LineCogsAmount = 75000.00m,
                    BatchNumber = "BATCH-2026-Q3-01"
                }
            }
        };

        var doc = new DeliveryChallanDocument(challan, _companyHeader);
        var bytes = doc.GeneratePdf();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "Generated Delivery Challan PDF must be larger than 1KB.");
        Assert.Equal(0x25, bytes[0]);
    }

    [Fact]
    public void PaySlipDocument_ShouldGenerateValidPdf()
    {
        var slip = new PaySlipDto(
            EmployeeId: 1,
            EmployeeCode: "EMP-001",
            EmployeeName: "Engr. Tanvir Ahmed",
            DesignationTitle: "Chief Technology Officer",
            DepartmentName: "Engineering & IT",
            JoiningDate: new DateTime(2022, 1, 15),
            BankName: "Standard Chartered Bank",
            BankAccountNumber: "01-9876543-01",
            PeriodName: "October 2026",
            Year: 2026,
            Month: 10,
            ProcessingDate: new DateTime(2026, 10, 31),
            WorkingDays: 26,
            PresentDays: 25,
            LateDays: 1,
            AbsentDays: 0,
            OvertimeHours: 4.5m,
            BasicSalary: 75000.00m,
            HouseRent: 37500.00m,
            MedicalAllowance: 7500.00m,
            ConveyanceAllowance: 5000.00m,
            OtherAllowance: 0m,
            OvertimeRate: 360.50m,
            OvertimeAmount: 1622.25m,
            TotalGrossEarnings: 126622.25m,
            AbsentDeduction: 0m,
            TaxDeduction: 8500.00m,
            ProvidentFundDeduction: 7500.00m,
            TotalDeductions: 16000.00m,
            NetSalaryPayable: 110622.25m
        );

        var doc = new PaySlipDocument(slip, _companyHeader);
        var bytes = doc.GeneratePdf();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "Generated Pay Slip PDF must be larger than 1KB.");
        Assert.Equal(0x25, bytes[0]);
    }
}
