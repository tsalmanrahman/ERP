namespace ERP.Common.Application.Models.Specialized;

// ==============================================================================
// 1. FIXED ASSETS MODELS
// ==============================================================================

public record AssetGroupDto(
    int Id,
    int CompanyId,
    string GroupCode,
    string GroupName,
    decimal DepreciationRate,
    int GlAssetAccountId,
    int GlAccumDeprAccountId,
    int GlDeprExpenseAccountId
);

public record CreateAssetGroupRequest(
    string GroupCode,
    string GroupName,
    decimal DepreciationRate,
    int GlAssetAccountId,
    int GlAccumDeprAccountId,
    int GlDeprExpenseAccountId
);

public record AssetDto(
    int Id,
    int CompanyId,
    string AssetCode,
    string AssetName,
    int AssetGroupId,
    string GroupName,
    DateTime PurchaseDate,
    decimal PurchaseCost,
    decimal CurrentBookValue,
    decimal AccumulatedDepreciation,
    decimal SalvageValue,
    int UsefulLifeMonths,
    string DepreciationMethod,
    bool IsDisposed,
    DateTime? DisposalDate,
    decimal? DisposalAmount,
    decimal? GainLossAmount,
    int? DisposalJournalVoucherId,
    string? DisposalJournalVoucherNumber
);

public record CreateAssetRequest(
    string AssetCode,
    string AssetName,
    int AssetGroupId,
    DateTime PurchaseDate,
    decimal PurchaseCost,
    decimal SalvageValue = 0,
    int UsefulLifeMonths = 60,
    string DepreciationMethod = "StraightLine"
);

public record RunDepreciationRequest(
    DateTime PeriodDate,
    int? FiscalPeriodId = null
);

public record DepreciationEntryDto(
    int AssetId,
    string AssetCode,
    string AssetName,
    decimal DepreciationAmount,
    decimal AccumulatedDepreciationAfter,
    decimal BookValueAfter
);

public record DepreciationRunResultDto(
    int TotalAssetsProcessed,
    decimal TotalDepreciationAmount,
    int JournalVoucherId,
    string JournalVoucherNumber,
    IReadOnlyList<DepreciationEntryDto> Entries
);

public record DisposeAssetRequest(
    int AssetId,
    DateTime DisposalDate,
    decimal DisposalProceeds,
    int? ProceedsDepositAccountId = null,
    string? ReferenceNumber = null
);

public record AssetDisposalResultDto(
    int AssetId,
    string AssetCode,
    string AssetName,
    DateTime DisposalDate,
    decimal PurchaseCost,
    decimal AccumulatedDepreciation,
    decimal BookValueAtDisposal,
    decimal DisposalProceeds,
    decimal GainLossAmount,
    bool IsGain,
    int JournalVoucherId,
    string JournalVoucherNumber
);

public record AssetScheduleItemDto(
    int AssetId,
    string AssetCode,
    string AssetName,
    string GroupName,
    decimal PurchaseCost,
    decimal PriorAccumulatedDepreciation,
    decimal CurrentPeriodDepreciation,
    decimal TotalAccumulatedDepreciation,
    decimal NetBookValue
);

public record AssetScheduleReportDto(
    DateTime AsOfDate,
    decimal TotalPurchaseCost,
    decimal TotalAccumulatedDepreciation,
    decimal TotalNetBookValue,
    IReadOnlyList<AssetScheduleItemDto> Assets
);

// ==============================================================================
// 2. REAL ESTATE & PROPERTY SALES MODELS
// ==============================================================================

public record ProjectDto(
    int Id,
    int CompanyId,
    string ProjectCode,
    string ProjectName,
    string Location,
    int TotalUnits,
    decimal EstimatedCost,
    string Status
);

public record CreateProjectRequest(
    string ProjectCode,
    string ProjectName,
    string Location,
    int TotalUnits,
    decimal EstimatedCost,
    string Status = "UnderConstruction"
);

public record PropertyUnitDto(
    int Id,
    int CompanyId,
    int ProjectId,
    string ProjectName,
    string UnitNumber,
    string UnitType,
    int FloorNumber,
    decimal AreaSqFt,
    decimal RatePerSqFt,
    decimal TotalPrice,
    string Status
);

public record CreatePropertyUnitRequest(
    int ProjectId,
    string UnitNumber,
    string UnitType,
    int FloorNumber,
    decimal AreaSqFt,
    decimal RatePerSqFt
);

public record BookingInstallmentDto(
    int Id,
    int InstallmentNumber,
    DateTime DueDate,
    decimal AmountDue,
    decimal AmountPaid,
    string Status,
    DateTime? PaymentDate,
    int? JournalVoucherId,
    string? Remarks
);

public record PropertyBookingDto(
    int Id,
    int CompanyId,
    int ProjectId,
    string ProjectName,
    int PropertyUnitId,
    string UnitNumber,
    int CustomerId,
    string CustomerName,
    string BookingNumber,
    DateTime BookingDate,
    decimal AgreedPrice,
    decimal DownPayment,
    decimal TotalPaid,
    decimal BalanceDue,
    string Status,
    IReadOnlyList<BookingInstallmentDto> Installments
);

public record CreatePropertyBookingRequest(
    int ProjectId,
    int PropertyUnitId,
    int CustomerId,
    DateTime BookingDate,
    decimal AgreedPrice,
    decimal DownPayment,
    int NumberOfInstallments = 12,
    DateTime? InstallmentsStartDate = null
);

public record CollectInstallmentRequest(
    int BookingId,
    int InstallmentId,
    DateTime PaymentDate,
    decimal AmountPaid,
    int? DepositBankAccountId = null,
    string? ReferenceNumber = null
);

public record InstallmentCollectionResultDto(
    int BookingId,
    string BookingNumber,
    int InstallmentId,
    int InstallmentNumber,
    decimal AmountPaid,
    decimal RemainingBookingBalance,
    int JournalVoucherId,
    string JournalVoucherNumber
);

// ==============================================================================
// 3. MANUFACTURING & BILL OF MATERIALS MODELS
// ==============================================================================

public record BomComponentDto(
    int Id,
    int RawMaterialProductId,
    string RawMaterialProductCode,
    string RawMaterialProductName,
    decimal RequiredQuantity,
    decimal ScrapPercentage,
    decimal UnitCost,
    decimal ExtendedCost
);

public record BillOfMaterialsDto(
    int Id,
    int CompanyId,
    string BomNumber,
    int FinishedGoodProductId,
    string FinishedGoodProductCode,
    string FinishedGoodProductName,
    decimal QuantityProduced,
    string Version,
    bool IsActive,
    decimal TotalDirectMaterialCost,
    IReadOnlyList<BomComponentDto> Components
);

public record CreateBomComponentRequest(
    int RawMaterialProductId,
    decimal RequiredQuantity,
    decimal ScrapPercentage = 0,
    decimal UnitCost = 0
);

public record CreateBomRequest(
    string BomNumber,
    int FinishedGoodProductId,
    decimal QuantityProduced = 1,
    string Version = "1.0",
    IReadOnlyList<CreateBomComponentRequest>? Components = null
);

public record ProductionOrderDto(
    int Id,
    int CompanyId,
    string OrderNumber,
    int BomId,
    string BomNumber,
    int FinishedGoodProductId,
    string FinishedGoodProductCode,
    string FinishedGoodProductName,
    decimal TargetQuantity,
    decimal CompletedQuantity,
    int RawMaterialWarehouseId,
    string RawMaterialWarehouseName,
    int FinishedGoodWarehouseId,
    string FinishedGoodWarehouseName,
    DateTime OrderDate,
    DateTime? StartDate,
    DateTime? CompletionDate,
    decimal TotalMaterialCost,
    string Status,
    int? JournalVoucherId,
    string? JournalVoucherNumber
);

public record CreateProductionOrderRequest(
    int BomId,
    decimal TargetQuantity,
    int RawMaterialWarehouseId,
    int FinishedGoodWarehouseId,
    DateTime? OrderDate = null
);

public record CompleteProductionOrderRequest(
    int ProductionOrderId,
    DateTime? CompletionDate = null,
    decimal? CompletedQuantity = null
);
