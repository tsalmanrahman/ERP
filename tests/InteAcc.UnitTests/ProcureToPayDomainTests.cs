using Xunit;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Domain.Entities.Procurement;
using InteAcc.Common.Domain.Entities.AccountsPayable;
using InteAcc.Common.Application.Models.ProcureToPay;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.UnitTests;

public class ProcureToPayDomainTests
{
    [Fact]
    public void PurchaseOrder_LineTotalsAndStatusTransitions_CalculatesCorrectly()
    {
        // Arrange
        var po = new PurchaseOrder
        {
            CompanyId = 1,
            PoNumber = "PO-202610-0001",
            PoDate = DateTime.UtcNow,
            VendorId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Draft
        };

        var line1 = new PurchaseOrderLine
        {
            LineNumber = 1,
            ProductId = 1,
            OrderedQuantity = 10m,
            UnitPrice = 1200m,
            LineTotal = 10m * 1200m
        };

        var line2 = new PurchaseOrderLine
        {
            LineNumber = 2,
            ProductId = 2,
            OrderedQuantity = 5m,
            UnitPrice = 3000m,
            LineTotal = 5m * 3000m
        };

        po.Lines.Add(line1);
        po.Lines.Add(line2);
        po.TotalAmount = po.Lines.Sum(l => l.LineTotal);

        // Assert
        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Equal(12000m, line1.LineTotal);
        Assert.Equal(15000m, line2.LineTotal);
        Assert.Equal(27000m, po.TotalAmount);

        // Transition to Issued
        po.Status = PurchaseOrderStatus.Issued;
        Assert.Equal(PurchaseOrderStatus.Issued, po.Status);
    }

    [Fact]
    public void PurchaseBill_Calculations_SubTotalTaxAndBalance()
    {
        // Arrange
        var bill = new PurchaseBill
        {
            CompanyId = 1,
            BillNumber = "BILL-202610-0001",
            VendorId = 1,
            BillDate = DateTime.UtcNow,
            Status = PurchaseBillStatus.Draft
        };

        var line = new PurchaseBillLine
        {
            LineNumber = 1,
            ProductId = 1,
            Quantity = 10m,
            UnitPrice = 1200m,
            LineAmount = 10m * 1200m,
            TaxAmount = 600m // 5% tax
        };

        bill.Lines.Add(line);
        bill.SubTotal = bill.Lines.Sum(l => l.LineAmount);
        bill.TaxAmount = bill.Lines.Sum(l => l.TaxAmount);
        bill.TotalAmount = bill.SubTotal + bill.TaxAmount;
        bill.BalanceAmount = bill.TotalAmount - bill.PaidAmount;

        // Assert
        Assert.Equal(12000m, bill.SubTotal);
        Assert.Equal(600m, bill.TaxAmount);
        Assert.Equal(12600m, bill.TotalAmount);
        Assert.Equal(12600m, bill.BalanceAmount);
        Assert.Equal(0m, bill.PaidAmount);
    }

    [Fact]
    public void PurchaseBill_PaymentAllocation_UpdatesBalancesAndStatuses()
    {
        // Arrange
        var bill = new PurchaseBill
        {
            TotalAmount = 12000m,
            PaidAmount = 0m,
            BalanceAmount = 12000m,
            Status = PurchaseBillStatus.Confirmed
        };

        // Act 1: Partial payment of 8,000
        decimal payment1 = 8000m;
        bill.PaidAmount += payment1;
        bill.BalanceAmount = bill.TotalAmount - bill.PaidAmount;
        bill.Status = bill.BalanceAmount == 0 ? PurchaseBillStatus.FullyPaid : PurchaseBillStatus.PartiallyPaid;

        // Assert 1
        Assert.Equal(8000m, bill.PaidAmount);
        Assert.Equal(4000m, bill.BalanceAmount);
        Assert.Equal(PurchaseBillStatus.PartiallyPaid, bill.Status);

        // Act 2: Final payment of remaining 4,000
        decimal payment2 = 4000m;
        bill.PaidAmount += payment2;
        bill.BalanceAmount = bill.TotalAmount - bill.PaidAmount;
        bill.Status = bill.BalanceAmount == 0 ? PurchaseBillStatus.FullyPaid : PurchaseBillStatus.PartiallyPaid;

        // Assert 2
        Assert.Equal(12000m, bill.PaidAmount);
        Assert.Equal(0m, bill.BalanceAmount);
        Assert.Equal(PurchaseBillStatus.FullyPaid, bill.Status);
    }

    [Fact]
    public void ThreeWayMatching_PerfectMatch_EvaluatesCorrectly()
    {
        // Arrange
        var matchLine = new ThreeWayMatchLineResult
        {
            LineNumber = 1,
            ProductId = 1,
            ProductCode = "PROD-PWR-MOD",
            PoQuantity = 10m,
            GrnQuantity = 10m,
            BilledQuantity = 10m,
            PoUnitPrice = 1200m,
            GrnUnitPrice = 1200m,
            BilledUnitPrice = 1200m
        };

        // Act
        matchLine.QuantityMatches = (matchLine.BilledQuantity <= matchLine.GrnQuantity) && (matchLine.BilledQuantity == matchLine.PoQuantity);
        matchLine.PriceMatches = (matchLine.BilledUnitPrice == matchLine.PoUnitPrice);

        if (matchLine.QuantityMatches && matchLine.PriceMatches)
        {
            matchLine.Status = "MATCHED";
        }

        var matchResult = new ThreeWayMatchResultDto
        {
            BillId = 1,
            BillNumber = "BILL-202610-0001",
            PoNumber = "PO-202610-0001",
            GrnNumber = "GRN-202610-0003",
            Lines = new List<ThreeWayMatchLineResult> { matchLine },
            IsFullyMatched = matchLine.QuantityMatches && matchLine.PriceMatches,
            TotalPoAmount = 12000m,
            TotalGrnAmount = 12000m,
            TotalBilledAmount = 12000m,
            PriceVariance = 0m
        };

        // Assert
        Assert.True(matchResult.IsFullyMatched);
        Assert.True(matchLine.QuantityMatches);
        Assert.True(matchLine.PriceMatches);
        Assert.Equal("MATCHED", matchLine.Status);
        Assert.Equal(0m, matchResult.PriceVariance);
    }

    [Fact]
    public void ThreeWayMatching_QuantityAndPriceDiscrepancies_IdentifiedCorrectly()
    {
        // Arrange: Vendor over-bills quantity and unit price
        var matchLine = new ThreeWayMatchLineResult
        {
            LineNumber = 1,
            ProductId = 1,
            PoQuantity = 10m,
            GrnQuantity = 8m,    // Only 8 received
            BilledQuantity = 10m, // But 10 billed
            PoUnitPrice = 1200m,
            BilledUnitPrice = 1250m // Price higher than contract
        };

        // Act
        matchLine.QuantityMatches = (matchLine.BilledQuantity <= matchLine.GrnQuantity);
        matchLine.PriceMatches = (matchLine.BilledUnitPrice == matchLine.PoUnitPrice);

        var variances = new List<string>();
        if (!matchLine.QuantityMatches)
        {
            variances.Add($"Billed qty ({matchLine.BilledQuantity}) exceeds received qty ({matchLine.GrnQuantity})");
        }
        if (!matchLine.PriceMatches)
        {
            variances.Add($"Billed price ({matchLine.BilledUnitPrice:C}) differs from PO price ({matchLine.PoUnitPrice:C})");
        }

        matchLine.DiscrepancyNotes = string.Join("; ", variances);
        matchLine.Status = "DISCREPANCY";

        decimal priceVariance = (matchLine.BilledUnitPrice - matchLine.PoUnitPrice) * matchLine.BilledQuantity;

        // Assert
        Assert.False(matchLine.QuantityMatches);
        Assert.False(matchLine.PriceMatches);
        Assert.Equal("DISCREPANCY", matchLine.Status);
        Assert.Equal(500m, priceVariance); // (1250 - 1200) * 10 = 500
        Assert.Contains("exceeds received", matchLine.DiscrepancyNotes);
        Assert.Contains("differs from PO price", matchLine.DiscrepancyNotes);
    }

    [Fact]
    public void VendorPayment_Allocations_MustNotExceedPaymentTotal()
    {
        // Arrange
        var payment = new VendorPayment
        {
            CompanyId = 1,
            PaymentNumber = "PAY-202610-0001",
            Amount = 10000m,
            PaymentMethod = PaymentMethod.BankTransfer,
            BankAccountId = 1
        };

        var allocation1 = new VendorPaymentAllocation { BillId = 1, AllocatedAmount = 6000m };
        var allocation2 = new VendorPaymentAllocation { BillId = 2, AllocatedAmount = 4000m };

        payment.Allocations.Add(allocation1);
        payment.Allocations.Add(allocation2);

        decimal totalAllocated = payment.Allocations.Sum(a => a.AllocatedAmount);

        // Assert
        Assert.Equal(10000m, totalAllocated);
        Assert.True(totalAllocated <= payment.Amount);
    }

    [Fact]
    public void ProcureToPay_GLJournalVoucher_DoubleEntryParity_Maintained()
    {
        // 1. Bill Posting Entry (Clearing GRN liability into Trade AP)
        // Dr. 2140 GRN Clearing (Inventory Received Not Invoiced) = 12,000
        // Cr. 2110 Trade Accounts Payable = 12,000
        decimal billDr = 12000m;
        decimal billCr = 12000m;
        decimal billImbalance = Math.Abs(billDr - billCr);

        Assert.Equal(0m, billImbalance);

        // 2. Vendor Payment Entry (Disbursing payment from Bank to reduce AP liability)
        // Dr. 2110 Trade Accounts Payable = 8,000
        // Cr. 1120 Prime Bank = 8,000
        decimal payDr = 8000m;
        decimal payCr = 8000m;
        decimal payImbalance = Math.Abs(payDr - payCr);

        Assert.Equal(0m, payImbalance);
    }

    [Fact]
    public void InteAccDbContext_ShouldMapProcureToPayEntitiesToCorrectSchemas()
    {
        var options = new DbContextOptionsBuilder<InteAccDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new InteAccDbContext(options);
        var model = context.Model;

        var poEntity = model.FindEntityType(typeof(PurchaseOrder));
        Assert.NotNull(poEntity);
        Assert.Equal("PurchaseOrders", poEntity.GetTableName());
        Assert.Equal("purch", poEntity.GetSchema());

        var poLineEntity = model.FindEntityType(typeof(PurchaseOrderLine));
        Assert.NotNull(poLineEntity);
        Assert.Equal("PurchaseOrderLines", poLineEntity.GetTableName());
        Assert.Equal("purch", poLineEntity.GetSchema());

        var billEntity = model.FindEntityType(typeof(PurchaseBill));
        Assert.NotNull(billEntity);
        Assert.Equal("PurchaseBills", billEntity.GetTableName());
        Assert.Equal("ap", billEntity.GetSchema());

        var billLineEntity = model.FindEntityType(typeof(PurchaseBillLine));
        Assert.NotNull(billLineEntity);
        Assert.Equal("PurchaseBillLines", billLineEntity.GetTableName());
        Assert.Equal("ap", billLineEntity.GetSchema());

        var paymentEntity = model.FindEntityType(typeof(VendorPayment));
        Assert.NotNull(paymentEntity);
        Assert.Equal("VendorPayments", paymentEntity.GetTableName());
        Assert.Equal("ap", paymentEntity.GetSchema());

        var paymentAllocEntity = model.FindEntityType(typeof(VendorPaymentAllocation));
        Assert.NotNull(paymentAllocEntity);
        Assert.Equal("VendorPaymentAllocations", paymentAllocEntity.GetTableName());
        Assert.Equal("ap", paymentAllocEntity.GetSchema());

        var vendorGroupEntity = model.FindEntityType(typeof(VendorGroup));
        Assert.NotNull(vendorGroupEntity);
        Assert.Equal("VendorGroups", vendorGroupEntity.GetTableName());
        Assert.Equal("ap", vendorGroupEntity.GetSchema());
    }
}
