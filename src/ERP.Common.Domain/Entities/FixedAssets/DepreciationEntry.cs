using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.FixedAssets;

public class DepreciationEntry : Entity<int>, IMultiTenant
{
    public int CompanyId { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public DateTime ScheduleDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciationAfter { get; set; }
    public decimal BookValueAfter { get; set; }
    public int? JournalVoucherId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";

    public DepreciationEntry() { }
}
