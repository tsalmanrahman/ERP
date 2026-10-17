using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.RealEstate;

public class PropertyUnit : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string UnitNumber { get; set; } = string.Empty;
    public string UnitType { get; set; } = "Apartment"; // Apartment, CommercialSpace, Parking
    public int FloorNumber { get; set; } = 1;
    public decimal AreaSqFt { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Available"; // Available, Booked, Sold, HandedOver

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public PropertyUnit() { }

    public void RecalculatePrice()
    {
        TotalPrice = Math.Round(AreaSqFt * RatePerSqFt, 2);
    }
}
