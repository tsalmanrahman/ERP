using ERP.Common.Domain.Entities.Inventory;

namespace ERP.Common.Application.Models.Inventory;

// --- Masters DTOs ---

public class ProductDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BarcodeGtin { get; set; }
    public int ProductClassId { get; set; }
    public string? ClassCode { get; set; }
    public string? ClassName { get; set; }
    public int? CategoryType { get; set; }
    public int? SubcategoryId { get; set; }
    public string? SubcategoryName { get; set; }
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? MaterialGrade { get; set; }
    public int PrimaryUomId { get; set; }
    public string? UomCode { get; set; }
    public int? SecondaryUomId { get; set; }
    public decimal? UomConversionFactor { get; set; }
    public string? PackSize { get; set; }
    public decimal StandardCost { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MinimumRetailPrice { get; set; }
    public string? TaxScheduleCode { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal TotalQuantityOnHand { get; set; }
    public bool IsActive { get; set; }
    public string LifecycleStatus { get; set; } = "Active";
    public bool IsPurchasable { get; set; } = true;
    public bool IsSellable { get; set; } = true;
    public bool IsManufactured { get; set; } = false;

    // International Standards & Specifications
    public string? HsCode { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public string? WeightUom { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUom { get; set; }
    public decimal? VolumeCbm { get; set; }
    public int TrackingPolicy { get; set; }
    public decimal SafetyStock { get; set; }
    public int LeadTimeDays { get; set; }
    public string CostingMethod { get; set; } = "MovingAverage";
    public decimal TaxRatePercent { get; set; }
    public int? DefaultWarehouseId { get; set; }
    public string? DefaultWarehouseName { get; set; }
    public string? DefaultStorageBin { get; set; }
    public string QcPolicy { get; set; } = "Visual Inspection";
    public bool HasTransactions { get; set; }
}

public class CreateProductRequest
{
    public string? ProductCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BarcodeGtin { get; set; }
    public int ProductClassId { get; set; }
    public int? SubcategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? MaterialGrade { get; set; }
    public int PrimaryUomId { get; set; }
    public int? SecondaryUomId { get; set; }
    public decimal? UomConversionFactor { get; set; }
    public string? PackSize { get; set; }
    public decimal StandardCost { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MinimumRetailPrice { get; set; }
    public string? TaxScheduleCode { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? LifecycleStatus { get; set; }
    public bool? IsPurchasable { get; set; }
    public bool? IsSellable { get; set; }
    public bool? IsManufactured { get; set; }

    // International Standards
    public string? HsCode { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public string? WeightUom { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUom { get; set; }
    public decimal? VolumeCbm { get; set; }
    public int? TrackingPolicy { get; set; }
    public decimal? SafetyStock { get; set; }
    public int? LeadTimeDays { get; set; }
    public string? CostingMethod { get; set; }
    public decimal? TaxRatePercent { get; set; }
    public int? DefaultWarehouseId { get; set; }
    public string? DefaultStorageBin { get; set; }
    public string? QcPolicy { get; set; }
}

public class UpdateProductRequest
{
    // Business code is intentionally immutable: client-supplied codes are never accepted for modification
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BarcodeGtin { get; set; }
    public int ProductClassId { get; set; }
    public int? SubcategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? MaterialGrade { get; set; }
    public int PrimaryUomId { get; set; }
    public int? SecondaryUomId { get; set; }
    public decimal? UomConversionFactor { get; set; }
    public string? PackSize { get; set; }
    public decimal StandardCost { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MinimumRetailPrice { get; set; }
    public string? TaxScheduleCode { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LifecycleStatus { get; set; }
    public bool? IsPurchasable { get; set; }
    public bool? IsSellable { get; set; }
    public bool? IsManufactured { get; set; }

    // International Standards
    public string? HsCode { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public string? WeightUom { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? DimensionUom { get; set; }
    public decimal? VolumeCbm { get; set; }
    public int? TrackingPolicy { get; set; }
    public decimal? SafetyStock { get; set; }
    public int? LeadTimeDays { get; set; }
    public string? CostingMethod { get; set; }
    public decimal? TaxRatePercent { get; set; }
    public int? DefaultWarehouseId { get; set; }
    public string? DefaultStorageBin { get; set; }
    public string? QcPolicy { get; set; }
}

public class ProductClassDto
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public ProductCategoryType CategoryType { get; set; }
    public string CategoryTypeName { get; set; } = string.Empty;
    public int GlInventoryAccountId { get; set; }
    public string? GlInventoryAccountCode { get; set; }
    public int GlCogsAccountId { get; set; }
    public string? GlCogsAccountCode { get; set; }
    public int GlRevenueAccountId { get; set; }
    public string? GlRevenueAccountCode { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateProductClassRequest
{
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public ProductCategoryType CategoryType { get; set; }
    public int GlInventoryAccountId { get; set; }
    public int GlCogsAccountId { get; set; }
    public int GlRevenueAccountId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UnitOfMeasureDto
{
    public int Id { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public string UomCategory { get; set; } = "Unit";
    public int DecimalPrecision { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}

public class CreateUnitOfMeasureRequest
{
    public string UomCode { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public string? UomCategory { get; set; }
    public int? DecimalPrecision { get; set; }
    public bool? IsActive { get; set; }
}

public class MasterLookupDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string LookupType { get; set; } = string.Empty;
    public string LookupCode { get; set; } = string.Empty;
    public string LookupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? NumericValue { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class SaveMasterLookupRequest
{
    public string LookupType { get; set; } = string.Empty;
    public string LookupCode { get; set; } = string.Empty;
    public string LookupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? NumericValue { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsGlobal { get; set; } = true;
}

public class ProductPriceHistoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal OldStandardCost { get; set; }
    public decimal NewStandardCost { get; set; }
    public decimal OldSellingPrice { get; set; }
    public decimal NewSellingPrice { get; set; }
    public decimal OldMinRetailPrice { get; set; }
    public decimal NewMinRetailPrice { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}

// --- Depot & Facility DTOs ---

public class DepotDto
{
    public int Id { get; set; }
    public string DepotCode { get; set; } = string.Empty;
    public string DepotName { get; set; } = string.Empty;
    public int DepotType { get; set; }
    public string DepotTypeName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public int WarehouseCount { get; set; }
}

public class CreateDepotRequest
{
    public string DepotCode { get; set; } = string.Empty;
    public string DepotName { get; set; } = string.Empty;
    public int DepotType { get; set; } = 1; // 1=Manufacturing Plant, 2=Regional Logistics Depot, 3=Central Distribution Hub
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
}

public class WarehouseDto
{
    public int Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public int? DepotId { get; set; }
    public string? DepotName { get; set; }
    public int WarehouseType { get; set; }
    public string WarehouseTypeName { get; set; } = string.Empty;
}

public class CreateWarehouseRequest
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int? DepotId { get; set; }
    public int WarehouseType { get; set; } = 2; // 1=Raw Material, 2=Finished Goods, 3=WIP, 4=Spares, 5=General/Transit
}

// --- Stock Balance DTOs ---

public class ProductStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValuation { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}

// --- Goods Receipt Note (GRN) DTOs ---

public class CreateGrnRequest
{
    public GrnType GrnType { get; set; } = GrnType.Vendor;
    public DateTime GrnDate { get; set; }
    public int WarehouseId { get; set; }
    public int? VendorId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? ProductionOrderId { get; set; }
    public string? ProductionReference { get; set; }
    public string? PoNumber { get; set; }
    public string? ChallanNumber { get; set; }
    public DateTime? ChallanDate { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? GateEntryNumber { get; set; }
    public string? QcStatus { get; set; } = "Passed";
    public string? Remarks { get; set; }
    public List<CreateGrnLineRequest> Lines { get; set; } = new();
}

public class CreateGrnLineRequest
{
    public int? PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? StorageBinLocation { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
}

public class GrnDto
{
    public int Id { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public GrnType GrnType { get; set; } = GrnType.Vendor;
    public DateTime GrnDate { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? ProductionOrderId { get; set; }
    public string? ProductionReference { get; set; }
    public string? PoNumber { get; set; }
    public string? ChallanNumber { get; set; }
    public DateTime? ChallanDate { get; set; }
    public string? CarrierName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? GateEntryNumber { get; set; }
    public string? QcStatus { get; set; } = "Passed";
    public decimal TotalAmount { get; set; }
    public GrnStatus Status { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? JournalVoucherNumber { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<GrnLineDto> Lines { get; set; } = new();
    public List<GrnReturnDto> Returns { get; set; } = new();
}

public class GrnLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int? PurchaseOrderLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? StorageBinLocation { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
}

// --- Eligible PO / MO DTOs for GRN Intake ---

public class EligiblePurchaseOrderDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public List<EligiblePoLineDto> Lines { get; set; } = new();
}

public class EligiblePoLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PreviouslyReceivedQuantity { get; set; }
    public decimal OutstandingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class EligibleProductionOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int FinishedGoodProductId { get; set; }
    public string FinishedGoodCode { get; set; } = string.Empty;
    public string FinishedGoodName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal TargetQuantity { get; set; }
    public decimal PreviouslyCompletedQuantity { get; set; }
    public decimal OutstandingQuantity { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public int FinishedGoodWarehouseId { get; set; }
    public string FinishedGoodWarehouseName { get; set; } = string.Empty;
}

// --- Goods Receipt Return DTOs ---

public class CreateGrnReturnRequest
{
    public int GoodsReceiptNoteId { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public List<CreateGrnReturnLineRequest> Lines { get; set; } = new();
}

public class CreateGrnReturnLineRequest
{
    public int GoodsReceiptNoteLineId { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public string? ReturnReason { get; set; }
}

public class GrnReturnDto
{
    public int Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int GoodsReceiptNoteId { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public GoodsReceiptReturnType ReturnType { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public GoodsReceiptReturnStatus Status { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? Remarks { get; set; }
    public List<GrnReturnLineDto> Lines { get; set; } = new();
}

public class GrnReturnLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int GoodsReceiptNoteLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal ReturnedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ReturnReason { get; set; }
}

// --- Stock Adjustment DTOs ---

public class CreateStockAdjustmentRequest
{
    public DateTime AdjustmentDate { get; set; }
    public int WarehouseId { get; set; }
    public StockAdjustmentType AdjustmentType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<CreateStockAdjustmentLineRequest> Lines { get; set; } = new();
}

public class CreateStockAdjustmentLineRequest
{
    public int ProductId { get; set; }
    public decimal AdjustmentQuantity { get; set; } // Positive for gain, Negative for loss
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

public class StockAdjustmentDto
{
    public int Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public DateTime AdjustmentDate { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public StockAdjustmentType AdjustmentType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public StockAdjustmentStatus Status { get; set; }
    public int? JournalVoucherId { get; set; }
    public List<StockAdjustmentLineDto> Lines { get; set; } = new();
}

public class StockAdjustmentLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal AdjustmentQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

// --- Stock Transfer DTOs ---

public class CreateStockTransferRequest
{
    public DateTime TransferDate { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public string? Remarks { get; set; }
    public List<CreateStockTransferLineRequest> Lines { get; set; } = new();
}

public class CreateStockTransferLineRequest
{
    public int ProductId { get; set; }
    public decimal TransferQuantity { get; set; }
}

public class StockTransferDto
{
    public int Id { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public int FromWarehouseId { get; set; }
    public string FromWarehouseName { get; set; } = string.Empty;
    public int ToWarehouseId { get; set; }
    public string ToWarehouseName { get; set; } = string.Empty;
    public StockTransferStatus Status { get; set; }
    public string? Remarks { get; set; }
    public List<StockTransferLineDto> Lines { get; set; } = new();
}

public class StockTransferLineDto
{
    public int Id { get; set; }
    public int LineNumber { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal TransferQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

// --- Reports DTOs ---

public class StockLedgerReportRow
{
    public DateTime TransactionDate { get; set; }
    public StockTransactionType TransactionType { get; set; }
    public string TransactionTypeName => TransactionType.ToString();
    public string ReferenceNumber { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal BalanceQuantity { get; set; }
}

public class StockLedgerReport
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<StockLedgerReportRow> Rows { get; set; } = new();
}

public class StockValuationSummaryRow
{
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string UomCode { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValuation { get; set; }
}

public class StockValuationReport
{
    public DateTime AsOfDate { get; set; }
    public decimal TotalInventoryValuation { get; set; }
    public int TotalItemCount { get; set; }
    public List<StockValuationSummaryRow> Items { get; set; } = new();
}
