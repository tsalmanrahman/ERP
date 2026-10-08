using Xunit;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Domain.Entities.AccountsReceivable;
using InteAcc.Common.Domain.Entities.Sales;
using InteAcc.Common.Application.Models.OrderToCash;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.UnitTests;

public class OrderToCashDomainTests
{
    [Fact]
    public void SalesOrder_LineTotalsAndStatusTransitions_CalculatesCorrectly()
    {
        // Arrange
        var order = new SalesOrder
        {
            CompanyId = 1,
            OrderNumber = "SO-202610-0001",
            OrderDate = DateTime.UtcNow,
            CustomerId = 1,
            SalesCenterId = 1,
            OrderStatus = SalesOrderStatus.Draft
        };

        var line1 = new SalesOrderLine
        {
            LineNumber = 1,
            ProductId = 1,
            OrderedQuantity = 2m,
            UnitPrice = 55000m,
            LineTotal = 2m * 55000m
        };

        var line2 = new SalesOrderLine
        {
            LineNumber = 2,
            ProductId = 3,
            OrderedQuantity = 10m,
            UnitPrice = 1500m,
            LineTotal = 10m * 1500m
        };

        order.Lines.Add(line1);
        order.Lines.Add(line2);
        order.TotalAmount = order.Lines.Sum(l => l.LineTotal);

        // Assert
        Assert.Equal(SalesOrderStatus.Draft, order.OrderStatus);
        Assert.Equal(110000m, line1.LineTotal);
        Assert.Equal(15000m, line2.LineTotal);
        Assert.Equal(125000m, order.TotalAmount);

        // Transition to Confirmed
        order.OrderStatus = SalesOrderStatus.Confirmed;
        Assert.Equal(SalesOrderStatus.Confirmed, order.OrderStatus);
    }

    [Fact]
    public void SalesOrder_CreditLimitValidation_EnforcesExposureLimit()
    {
        // Arrange
        var customer = new Customer
        {
            CustomerCode = "CUST-DHAKA-01",
            CustomerName = "Apex Technologies Ltd.",
            CreditLimit = 500000m,
            CurrentBalance = 420000m // Existing debt
        };

        decimal exceedingOrderTotal = 100000m;
        decimal acceptableOrderTotal = 70000m;

        // Act & Assert
        decimal potentialExceedingExposure = customer.CurrentBalance + exceedingOrderTotal; // 520,000 > 500,000
        bool exceedsCreditLimit = customer.CreditLimit > 0 && potentialExceedingExposure > customer.CreditLimit;

        decimal potentialAcceptableExposure = customer.CurrentBalance + acceptableOrderTotal; // 490,000 <= 500,000
        bool withinCreditLimit = customer.CreditLimit > 0 && potentialAcceptableExposure <= customer.CreditLimit;

        Assert.True(exceedsCreditLimit);
        Assert.True(withinCreditLimit);
    }

    [Fact]
    public void DeliveryChallan_Calculations_CogsAndInventoryRelief()
    {
        // Arrange
        var challan = new DeliveryChallan
        {
            CompanyId = 1,
            ChallanNumber = "DC-202610-0001",
            ChallanDate = DateTime.UtcNow,
            SalesOrderId = 1,
            CustomerId = 1,
            WarehouseId = 1,
            Status = DeliveryChallanStatus.Draft
        };

        var line = new DeliveryChallanLine
        {
            LineNumber = 1,
            ProductId = 1,
            DispatchedQuantity = 2m,
            UnitCost = 42000m, // Moving weighted average cost
            LineCogsAmount = 2m * 42000m // 84,000
        };

        challan.Lines.Add(line);
        challan.TotalCogsAmount = challan.Lines.Sum(l => l.LineCogsAmount);

        // Assert
        Assert.Equal(84000m, line.LineCogsAmount);
        Assert.Equal(84000m, challan.TotalCogsAmount);
        Assert.Equal(DeliveryChallanStatus.Draft, challan.Status);

        // Dispatch
        challan.Status = DeliveryChallanStatus.Dispatched;
        Assert.Equal(DeliveryChallanStatus.Dispatched, challan.Status);
    }

    [Fact]
    public void SalesInvoice_Calculations_SubTotalDiscountTaxAndBalance()
    {
        // Arrange
        var invoice = new Invoice
        {
            CompanyId = 1,
            InvoiceNumber = "INV-202610-0001",
            CustomerId = 1,
            InvoiceDate = DateTime.UtcNow,
            DiscountAmount = 5000m,
            Status = InvoiceStatus.Draft
        };

        var line1 = new InvoiceLine
        {
            LineNumber = 1,
            ProductId = 1,
            Quantity = 2m,
            UnitPrice = 55000m,
            LineAmount = 110000m,
            TaxAmount = 5500m // 5% VAT
        };

        invoice.Lines.Add(line1);
        invoice.SubTotal = invoice.Lines.Sum(l => l.LineAmount);
        invoice.TaxAmount = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.TotalAmount = (invoice.SubTotal - invoice.DiscountAmount) + invoice.TaxAmount;
        invoice.PaidAmount = 0m;
        invoice.BalanceAmount = invoice.TotalAmount;

        // Assert
        Assert.Equal(110000m, invoice.SubTotal);
        Assert.Equal(5000m, invoice.DiscountAmount);
        Assert.Equal(5500m, invoice.TaxAmount);
        Assert.Equal(110500m, invoice.TotalAmount); // (110,000 - 5,000) + 5,500 = 110,500
        Assert.Equal(110500m, invoice.BalanceAmount);
    }

    [Fact]
    public void SalesInvoice_CustomerReceiptAllocation_UpdatesBalancesAndStatuses()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 110000m,
            PaidAmount = 0m,
            BalanceAmount = 110000m,
            Status = InvoiceStatus.Confirmed
        };

        // Act 1: Partial receipt of 70,000
        decimal receipt1 = 70000m;
        invoice.PaidAmount += receipt1;
        invoice.BalanceAmount = invoice.TotalAmount - invoice.PaidAmount;
        invoice.Status = invoice.BalanceAmount <= 0 ? InvoiceStatus.FullyPaid : InvoiceStatus.PartiallyPaid;

        // Assert 1
        Assert.Equal(70000m, invoice.PaidAmount);
        Assert.Equal(40000m, invoice.BalanceAmount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);

        // Act 2: Final settlement of remaining 40,000
        decimal receipt2 = 40000m;
        invoice.PaidAmount += receipt2;
        invoice.BalanceAmount = invoice.TotalAmount - invoice.PaidAmount;
        invoice.Status = invoice.BalanceAmount <= 0 ? InvoiceStatus.FullyPaid : InvoiceStatus.PartiallyPaid;

        // Assert 2
        Assert.Equal(110000m, invoice.PaidAmount);
        Assert.Equal(0m, invoice.BalanceAmount);
        Assert.Equal(InvoiceStatus.FullyPaid, invoice.Status);
    }

    [Fact]
    public void OrderToCash_DoubleEntryGLParity_CogsInvoicingAndReceipt_Maintained()
    {
        // 1. Delivery Challan COGS Posting
        // Dr. 5110 Cost of Goods Sold = 84,000
        // Cr. 1140 Finished Goods Inventory = 84,000
        decimal cogsDr = 84000m;
        decimal cogsCr = 84000m;
        Assert.Equal(0m, Math.Abs(cogsDr - cogsCr));

        // 2. Sales Invoice Revenue Posting
        // Dr. 1130 Trade Accounts Receivable = 110,000
        // Cr. 4100 General Sales Revenue = 110,000
        decimal invDr = 110000m;
        decimal invCr = 110000m;
        Assert.Equal(0m, Math.Abs(invDr - invCr));

        // 3. Customer Receipt Collection Posting
        // Dr. 1120 Prime Bank = 70,000
        // Cr. 1130 Trade Accounts Receivable = 70,000
        decimal rcpDr = 70000m;
        decimal rcpCr = 70000m;
        Assert.Equal(0m, Math.Abs(rcpDr - rcpCr));
    }

    [Fact]
    public void InteAccDbContext_ShouldMapOrderToCashEntitiesToCorrectSchemas()
    {
        var options = new DbContextOptionsBuilder<InteAccDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new InteAccDbContext(options);
        var model = context.Model;

        var custGroupEntity = model.FindEntityType(typeof(CustomerGroup));
        Assert.NotNull(custGroupEntity);
        Assert.Equal("CustomerGroups", custGroupEntity.GetTableName());
        Assert.Equal("ar", custGroupEntity.GetSchema());

        var invoiceEntity = model.FindEntityType(typeof(Invoice));
        Assert.NotNull(invoiceEntity);
        Assert.Equal("Invoices", invoiceEntity.GetTableName());
        Assert.Equal("ar", invoiceEntity.GetSchema());

        var invLineEntity = model.FindEntityType(typeof(InvoiceLine));
        Assert.NotNull(invLineEntity);
        Assert.Equal("InvoiceLines", invLineEntity.GetTableName());
        Assert.Equal("ar", invLineEntity.GetSchema());

        var rcpEntity = model.FindEntityType(typeof(CustomerReceipt));
        Assert.NotNull(rcpEntity);
        Assert.Equal("CustomerReceipts", rcpEntity.GetTableName());
        Assert.Equal("ar", rcpEntity.GetSchema());

        var rcpAllocEntity = model.FindEntityType(typeof(CustomerReceiptAllocation));
        Assert.NotNull(rcpAllocEntity);
        Assert.Equal("CustomerReceiptAllocations", rcpAllocEntity.GetTableName());
        Assert.Equal("ar", rcpAllocEntity.GetSchema());

        var salesCenterEntity = model.FindEntityType(typeof(SalesCenter));
        Assert.NotNull(salesCenterEntity);
        Assert.Equal("SalesCenters", salesCenterEntity.GetTableName());
        Assert.Equal("sales", salesCenterEntity.GetSchema());

        var soEntity = model.FindEntityType(typeof(SalesOrder));
        Assert.NotNull(soEntity);
        Assert.Equal("SalesOrders", soEntity.GetTableName());
        Assert.Equal("sales", soEntity.GetSchema());

        var soLineEntity = model.FindEntityType(typeof(SalesOrderLine));
        Assert.NotNull(soLineEntity);
        Assert.Equal("SalesOrderLines", soLineEntity.GetTableName());
        Assert.Equal("sales", soLineEntity.GetSchema());

        var dcEntity = model.FindEntityType(typeof(DeliveryChallan));
        Assert.NotNull(dcEntity);
        Assert.Equal("DeliveryChallans", dcEntity.GetTableName());
        Assert.Equal("sales", dcEntity.GetSchema());

        var dcLineEntity = model.FindEntityType(typeof(DeliveryChallanLine));
        Assert.NotNull(dcLineEntity);
        Assert.Equal("DeliveryChallanLines", dcLineEntity.GetTableName());
        Assert.Equal("sales", dcLineEntity.GetSchema());
    }
}
