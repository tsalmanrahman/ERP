using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.FixedAssets;

public enum DepreciationMethod
{
    StraightLine = 1,
    ReducingBalance = 2
}

public class Asset : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int AssetGroupId { get; set; }
    public AssetGroup? AssetGroup { get; set; }

    public DateTime PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal CurrentBookValue { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal SalvageValue { get; set; }
    public int UsefulLifeMonths { get; set; } = 60;
    public string DepreciationMethod { get; set; } = "StraightLine";

    // Disposal Info
    public bool IsDisposed { get; set; }
    public DateTime? DisposalDate { get; set; }
    public decimal? DisposalAmount { get; set; }
    public decimal? GainLossAmount { get; set; }
    public int? DisposalJournalVoucherId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<DepreciationEntry> DepreciationEntries { get; set; } = new List<DepreciationEntry>();

    public Asset() { }

    public decimal CalculateMonthlyDepreciation(decimal annualRatePercentage)
    {
        if (IsDisposed) return 0m;
        if (CurrentBookValue <= SalvageValue) return 0m;

        decimal amount;
        if (DepreciationMethod.Equals("ReducingBalance", StringComparison.OrdinalIgnoreCase))
        {
            // Reducing Balance: monthly depreciation on current carrying amount
            // Rate per month = (annualRate / 100) / 12
            var monthlyRate = (annualRatePercentage / 100m) / 12m;
            amount = Math.Round(CurrentBookValue * monthlyRate, 2);
        }
        else
        {
            // Straight Line: (Cost - Salvage) / UsefulLifeMonths OR Cost * (annualRate / 12)
            if (UsefulLifeMonths > 0)
            {
                var depreciableBase = Math.Max(0m, PurchaseCost - SalvageValue);
                amount = Math.Round(depreciableBase / UsefulLifeMonths, 2);
            }
            else
            {
                var monthlyRate = (annualRatePercentage / 100m) / 12m;
                amount = Math.Round(PurchaseCost * monthlyRate, 2);
            }
        }

        // Cap so book value doesn't drop below salvage value
        if (CurrentBookValue - amount < SalvageValue)
        {
            amount = Math.Max(0m, CurrentBookValue - SalvageValue);
        }

        return amount;
    }
}
