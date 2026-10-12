using Xunit;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Domain.Entities.FixedAssets;
using InteAcc.Common.Domain.Entities.RealEstate;
using InteAcc.Common.Domain.Entities.Manufacturing;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.UnitTests;

public class SpecializedModulesDomainTests
{
    [Fact]
    public void FixedAsset_StraightLineDepreciation_CalculatesMonthlyDepreciationAccurately()
    {
        // Arrange: Cost 500,000, Salvage 50,000, 60 months useful life
        var asset = new Asset
        {
            PurchaseCost = 500000m,
            CurrentBookValue = 500000m,
            AccumulatedDepreciation = 0m,
            SalvageValue = 50000m,
            UsefulLifeMonths = 60,
            DepreciationMethod = "StraightLine"
        };

        // Act
        var monthlyDepr = asset.CalculateMonthlyDepreciation(10m);

        // Assert: (500,000 - 50,000) / 60 = 7,500.00
        Assert.Equal(7500.00m, monthlyDepr);
    }

    [Fact]
    public void FixedAsset_ReducingBalanceDepreciation_CalculatesMonthlyDepreciationAccurately()
    {
        // Arrange: Cost 120,000, 15% annual rate
        var asset = new Asset
        {
            PurchaseCost = 120000m,
            CurrentBookValue = 120000m,
            AccumulatedDepreciation = 0m,
            SalvageValue = 12000m,
            UsefulLifeMonths = 48,
            DepreciationMethod = "ReducingBalance"
        };

        // Act
        var monthlyDepr = asset.CalculateMonthlyDepreciation(15m);

        // Assert: 120,000 * ((15 / 100) / 12) = 120,000 * 0.0125 = 1,500.00
        Assert.Equal(1500.00m, monthlyDepr);
    }

    [Fact]
    public void FixedAsset_Disposal_GainOrLossAndDoubleEntryParity_Maintained()
    {
        // Arrange: Cost 500,000, AccumDepr 100,000, Net Book Value 400,000
        decimal cost = 500000m;
        decimal accumDepr = 100000m;
        decimal bookValue = cost - accumDepr; // 400,000

        // Case 1: Sale with Gain (Proceeds 450,000 -> Gain 50,000)
        decimal proceedsGain = 450000m;
        decimal gain = proceedsGain - bookValue;

        decimal drTotalGain = proceedsGain + accumDepr; // 450,000 + 100,000 = 550,000
        decimal crTotalGain = cost + gain;              // 500,000 + 50,000 = 550,000
        Assert.Equal(0m, Math.Abs(drTotalGain - crTotalGain));

        // Case 2: Sale with Loss (Proceeds 350,000 -> Loss 50,000)
        decimal proceedsLoss = 350000m;
        decimal loss = bookValue - proceedsLoss;

        decimal drTotalLoss = proceedsLoss + accumDepr + loss; // 350,000 + 100,000 + 50,000 = 500,000
        decimal crTotalLoss = cost;                            // 500,000
        Assert.Equal(0m, Math.Abs(drTotalLoss - crTotalLoss));
    }

    [Fact]
    public void RealEstate_PropertyBooking_EqualInstallmentsGeneration()
    {
        // Arrange
        var booking = new PropertyBooking
        {
            AgreedPrice = 24000000m,
            DownPayment = 4000000m
        };

        // Act: 10 installments starting from Nov 1, 2026
        booking.GenerateEqualInstallments(10, new DateTime(2026, 11, 1));

        // Assert: 20,000,000 remaining / 10 = 2,000,000 per installment
        Assert.Equal(10, booking.Installments.Count);
        Assert.All(booking.Installments, i => Assert.Equal(2000000m, i.AmountDue));
        Assert.Equal(20000000m, booking.Installments.Sum(i => i.AmountDue));
    }

    [Fact]
    public void Manufacturing_BomDirectMaterialCost_RollupCalculation()
    {
        // Arrange
        var bom = new BillOfMaterials
        {
            BomNumber = "BOM-TEST-01",
            QuantityProduced = 1m
        };

        // Component 1: 1 x 1,200 (0% scrap) = 1,200
        bom.Components.Add(new BomComponent
        {
            RawMaterialProductId = 3,
            RequiredQuantity = 1m,
            ScrapPercentage = 0m,
            UnitCost = 1200m
        });

        // Component 2: 10 x 85 (2% scrap) = 10 * 1.02 * 85 = 867
        bom.Components.Add(new BomComponent
        {
            RawMaterialProductId = 4,
            RequiredQuantity = 10m,
            ScrapPercentage = 2m,
            UnitCost = 85m
        });

        // Act
        var totalMaterialCost = bom.CalculateTotalDirectMaterialCost();

        // Assert
        Assert.Equal(2067.00m, totalMaterialCost);
    }

    [Fact]
    public void SpecializedModules_EFCoreModelMappings_MatchTargetSchemas()
    {
        var options = new DbContextOptionsBuilder<InteAccDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new InteAccDbContext(options);
        var model = context.Model;

        // Fixed Assets (fa schema)
        var ag = model.FindEntityType(typeof(AssetGroup));
        Assert.NotNull(ag);
        Assert.Equal("AssetGroups", ag.GetTableName());
        Assert.Equal("fa", ag.GetSchema());

        var ast = model.FindEntityType(typeof(Asset));
        Assert.NotNull(ast);
        Assert.Equal("Assets", ast.GetTableName());
        Assert.Equal("fa", ast.GetSchema());

        var depr = model.FindEntityType(typeof(DepreciationEntry));
        Assert.NotNull(depr);
        Assert.Equal("DepreciationEntries", depr.GetTableName());
        Assert.Equal("fa", depr.GetSchema());

        // Real Estate (re schema)
        var proj = model.FindEntityType(typeof(Project));
        Assert.NotNull(proj);
        Assert.Equal("Projects", proj.GetTableName());
        Assert.Equal("re", proj.GetSchema());

        var unit = model.FindEntityType(typeof(PropertyUnit));
        Assert.NotNull(unit);
        Assert.Equal("PropertyUnits", unit.GetTableName());
        Assert.Equal("re", unit.GetSchema());

        var bkg = model.FindEntityType(typeof(PropertyBooking));
        Assert.NotNull(bkg);
        Assert.Equal("PropertyBookings", bkg.GetTableName());
        Assert.Equal("re", bkg.GetSchema());

        var inst = model.FindEntityType(typeof(BookingInstallment));
        Assert.NotNull(inst);
        Assert.Equal("BookingInstallments", inst.GetTableName());
        Assert.Equal("re", inst.GetSchema());

        // Manufacturing (mfg schema)
        var bom = model.FindEntityType(typeof(BillOfMaterials));
        Assert.NotNull(bom);
        Assert.Equal("BillOfMaterials", bom.GetTableName());
        Assert.Equal("mfg", bom.GetSchema());

        var bomComp = model.FindEntityType(typeof(BomComponent));
        Assert.NotNull(bomComp);
        Assert.Equal("BomComponents", bomComp.GetTableName());
        Assert.Equal("mfg", bomComp.GetSchema());

        var po = model.FindEntityType(typeof(ProductionOrder));
        Assert.NotNull(po);
        Assert.Equal("ProductionOrders", po.GetTableName());
        Assert.Equal("mfg", po.GetSchema());
    }
}
