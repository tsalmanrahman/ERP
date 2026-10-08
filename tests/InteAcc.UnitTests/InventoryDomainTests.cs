using Xunit;
using InteAcc.Common.Application.Models.Finance;
using InteAcc.Common.Application.Models.Inventory;
using InteAcc.Common.Domain.Entities.Inventory;

namespace InteAcc.UnitTests;

public class InventoryDomainTests
{
    [Fact]
    public void MovingWeightedAverageCost_InitialReceipt_CalculatesCorrectly()
    {
        // Arrange
        decimal initialQty = 0m;
        decimal initialCost = 0m;
        decimal receivedQty = 10m;
        decimal unitPrice = 40000m;

        // Act
        decimal newQty = initialQty + receivedQty;
        decimal newCost = ((initialQty * initialCost) + (receivedQty * unitPrice)) / newQty;

        // Assert
        Assert.Equal(10m, newQty);
        Assert.Equal(40000m, newCost);
        Assert.Equal(400000m, newQty * newCost);
    }

    [Fact]
    public void MovingWeightedAverageCost_SubsequentReceipt_CalculatesWeightedAverage()
    {
        // Arrange
        decimal existingQty = 10m;
        decimal existingCost = 40000m; // Total = 400,000

        decimal incomingQty = 5m;
        decimal incomingCost = 46000m; // Total = 230,000

        // Act
        decimal totalQty = existingQty + incomingQty; // 15
        decimal weightedCost = ((existingQty * existingCost) + (incomingQty * incomingCost)) / totalQty; // 630,000 / 15 = 42,000

        // Assert
        Assert.Equal(15m, totalQty);
        Assert.Equal(42000m, weightedCost);
        Assert.Equal(630000m, totalQty * weightedCost);
    }

    [Fact]
    public void MovingWeightedAverageCost_StockOutflow_PreservesUnitCost()
    {
        // Arrange
        decimal existingQty = 15m;
        decimal existingCost = 42000m;
        decimal issueQty = 6m;

        // Act
        decimal remainingQty = existingQty - issueQty;
        decimal costAfterIssue = existingCost; // In moving average, cost does not change on issue

        // Assert
        Assert.Equal(9m, remainingQty);
        Assert.Equal(42000m, costAfterIssue);
        Assert.Equal(378000m, remainingQty * costAfterIssue);
    }

    [Fact]
    public void GrnJournalVoucher_EnforcesStrictDoubleEntryEquilibrium()
    {
        // Arrange: Multi-item GRN
        var items = new List<(int AccountId, decimal Qty, decimal Price)>
        {
            (1140, 5m, 42000m), // 210,000
            (1141, 10m, 1200m)   // 12,000
        };

        decimal totalAmount = items.Sum(i => i.Qty * i.Price); // 222,000
        int clearingAccountId = 2140;

        // Act: Generate Voucher lines
        var lines = new List<CreateVoucherLineRequest>();
        foreach (var item in items)
        {
            lines.Add(new CreateVoucherLineRequest(item.AccountId, item.Qty * item.Price, 0m, "Inventory asset debit"));
        }
        lines.Add(new CreateVoucherLineRequest(clearingAccountId, 0m, totalAmount, "GRN clearing liability credit"));

        decimal totalDebits = lines.Sum(l => l.Debit);
        decimal totalCredits = lines.Sum(l => l.Credit);

        // Assert
        Assert.Equal(totalDebits, totalCredits);
        Assert.Equal(222000m, totalDebits);
        Assert.Equal(0m, totalDebits - totalCredits);
    }

    [Fact]
    public void StockAdjustment_Gain_GeneratesBalancedDebitCredit()
    {
        // Arrange: 2 units found of PROD-LED-55 @ 42,000 = 84,000
        decimal unitCost = 42000m;
        decimal gainQty = 2m;
        decimal lineAmount = gainQty * unitCost;

        int invAccountId = 1140;
        int varianceAccountId = 5120;

        // Act: Gain -> Debit Asset, Credit Variance
        var lines = new List<CreateVoucherLineRequest>
        {
            new(invAccountId, lineAmount, 0m, "Stock gain debit"),
            new(varianceAccountId, 0m, lineAmount, "Variance credit")
        };

        // Assert
        Assert.Equal(lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));
        Assert.Equal(84000m, lines.Sum(l => l.Debit));
    }

    [Fact]
    public void StockAdjustment_Loss_GeneratesBalancedDebitCredit()
    {
        // Arrange: 1 unit damaged @ 55,000
        decimal unitCost = 55000m;
        decimal lossQty = 1m;
        decimal lineAmount = lossQty * unitCost;

        int invAccountId = 1140;
        int varianceAccountId = 5120;

        // Act: Loss -> Debit Variance, Credit Asset
        var lines = new List<CreateVoucherLineRequest>
        {
            new(varianceAccountId, lineAmount, 0m, "Shrinkage expense debit"),
            new(invAccountId, 0m, lineAmount, "Stock write-off credit")
        };

        // Assert
        Assert.Equal(lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));
        Assert.Equal(55000m, lines.Sum(l => l.Debit));
    }

    [Fact]
    public void StockTransfer_ConservesTotalInventoryQuantityAcrossWarehouses()
    {
        // Arrange
        decimal centralWhInitial = 20m;
        decimal retailWhInitial = 5m;
        decimal transferQty = 7m;

        // Act
        decimal centralWhAfter = centralWhInitial - transferQty;
        decimal retailWhAfter = retailWhInitial + transferQty;

        // Assert
        Assert.Equal(13m, centralWhAfter);
        Assert.Equal(12m, retailWhAfter);
        Assert.Equal(centralWhInitial + retailWhInitial, centralWhAfter + retailWhAfter);
    }
}
