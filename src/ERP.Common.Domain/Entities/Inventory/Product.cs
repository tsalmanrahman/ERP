using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public class Product : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ProductClassId { get; set; }
    public int PrimaryUomId { get; set; }
    public string? PackSize { get; set; }
    public decimal StandardCost { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;

    // International Standards & Specifications (UNSPSC, GS1, ISO 80000, IAS 2)
    public string? Description { get; set; }
    public string? BarcodeGtin { get; set; }
    public int? SubcategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? MaterialGrade { get; set; }
    public int? SecondaryUomId { get; set; }
    public decimal? UomConversionFactor { get; set; }
    public decimal MinimumRetailPrice { get; set; }
    public string? TaxScheduleCode { get; set; } = "VAT_STD_15";
    public string? DefaultStorageBin { get; set; }
    public string QcPolicy { get; set; } = "Visual Inspection";
    public bool IsPurchasable { get; set; } = true;
    public bool IsSellable { get; set; } = true;
    public bool IsManufactured { get; set; } = false;
    public string LifecycleStatus { get; set; } = "Active";

    public string? HsCode { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public string? WeightUom { get; set; } = "KG";
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUom { get; set; } = "MM";
    public decimal? VolumeCbm { get; set; }
    public int TrackingPolicy { get; set; } = 1; // 1 = Standard, 2 = Batch/Heat No, 3 = Serialized
    public decimal SafetyStock { get; set; } = 0m;
    public int LeadTimeDays { get; set; } = 7;
    public string CostingMethod { get; set; } = "MovingAverage";
    public decimal TaxRatePercent { get; set; } = 15.00m;
    public int? DefaultWarehouseId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Product() { }
}
