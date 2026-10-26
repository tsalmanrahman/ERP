using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDocumentSequenceService _documentSequenceService;

    public InventoryService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDocumentSequenceService documentSequenceService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _documentSequenceService = documentSequenceService;
    }

    private int GetCompanyId() => _currentUserContext.GetRequiredCompanyId();

    private static string GetCategoryTypeName(ProductCategoryType type) => type switch
    {
        ProductCategoryType.FinishedGoods => "Finished Goods",
        ProductCategoryType.RawMaterials => "Raw Materials",
        ProductCategoryType.SemiFinishedGoods => "Semi-Finished / WIP",
        ProductCategoryType.Consumables => "Consumables & Spares",
        ProductCategoryType.Service => "Service / Non-Stock",
        ProductCategoryType.FixedAsset => "Fixed Asset Item",
        _ => "Finished Goods"
    };

    private IQueryable<ProductDto> BuildProductDtoQuery(int companyId)
    {
        return from p in _dbContext.Products.AsNoTracking()
               where p.CompanyId == companyId
               join pc in _dbContext.ProductClasses.AsNoTracking() on p.ProductClassId equals pc.Id into pcJoin
               from pc in pcJoin.DefaultIfEmpty()
               join uom in _dbContext.UnitsOfMeasure.AsNoTracking() on p.PrimaryUomId equals uom.Id into uomJoin
               from uom in uomJoin.DefaultIfEmpty()
               join wh in _dbContext.Warehouses.AsNoTracking() on p.DefaultWarehouseId equals wh.Id into whJoin
               from wh in whJoin.DefaultIfEmpty()
               join sub in _dbContext.MasterLookups.AsNoTracking() on p.SubcategoryId equals sub.Id into subJoin
               from sub in subJoin.DefaultIfEmpty()
               join brd in _dbContext.MasterLookups.AsNoTracking() on p.BrandId equals brd.Id into brdJoin
               from brd in brdJoin.DefaultIfEmpty()
               select new ProductDto
               {
                   Id = p.Id,
                   ProductCode = p.ProductCode,
                   ProductName = p.ProductName,
                   Description = p.Description,
                   BarcodeGtin = p.BarcodeGtin,
                   ProductClassId = p.ProductClassId,
                   ClassCode = pc != null ? pc.ClassCode : null,
                   ClassName = pc != null ? pc.ClassName : null,
                   CategoryType = pc != null ? (int?)pc.CategoryType : null,
                   SubcategoryId = p.SubcategoryId,
                   SubcategoryName = sub != null ? sub.LookupName : null,
                   BrandId = p.BrandId,
                   BrandName = brd != null ? brd.LookupName : null,
                   MaterialGrade = p.MaterialGrade,
                   PrimaryUomId = p.PrimaryUomId,
                   UomCode = uom != null ? uom.UomCode : null,
                   SecondaryUomId = p.SecondaryUomId,
                   UomConversionFactor = p.UomConversionFactor,
                   PackSize = p.PackSize,
                   StandardCost = p.StandardCost,
                   CurrentCost = p.CurrentCost,
                   SellingPrice = p.SellingPrice,
                   MinimumRetailPrice = p.MinimumRetailPrice,
                   TaxScheduleCode = p.TaxScheduleCode,
                   ReorderLevel = p.ReorderLevel,
                   IsActive = p.IsActive,
                   LifecycleStatus = p.LifecycleStatus,
                   IsPurchasable = p.IsPurchasable,
                   IsSellable = p.IsSellable,
                   IsManufactured = p.IsManufactured,
                   HsCode = p.HsCode,
                   NetWeight = p.NetWeight,
                   GrossWeight = p.GrossWeight,
                   WeightUom = p.WeightUom,
                   Length = p.Length,
                   Width = p.Width,
                   Height = p.Height,
                   DimensionUom = p.DimensionUom,
                   VolumeCbm = p.VolumeCbm,
                   TrackingPolicy = p.TrackingPolicy,
                   SafetyStock = p.SafetyStock,
                   LeadTimeDays = p.LeadTimeDays,
                   CostingMethod = p.CostingMethod,
                   TaxRatePercent = p.TaxRatePercent,
                   DefaultWarehouseId = p.DefaultWarehouseId,
                   DefaultWarehouseName = wh != null ? wh.WarehouseName : null,
                   DefaultStorageBin = p.DefaultStorageBin,
                   QcPolicy = p.QcPolicy,
                   TotalQuantityOnHand = _dbContext.ProductStock
                       .Where(s => s.CompanyId == companyId && s.ProductId == p.Id && !s.IsDeleted)
                       .Sum(s => (decimal?)s.QuantityOnHand) ?? 0m,
                   HasTransactions = _dbContext.StockLedgers
                       .Any(sm => sm.CompanyId == companyId && sm.ProductId == p.Id && !sm.IsDeleted)
               };
    }

    // --- Product Master ---

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await BuildProductDtoQuery(companyId)
            .OrderBy(p => p.ProductCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await BuildProductDtoQuery(companyId)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await BuildProductDtoQuery(companyId)
            .FirstOrDefaultAsync(p => p.ProductCode == normalizedCode, cancellationToken);
    }

    public async Task<List<ProductPriceHistoryDto>> GetProductPriceHistoryAsync(int productId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await _dbContext.ProductPriceHistories.AsNoTracking()
            .Where(h => h.CompanyId == companyId && h.ProductId == productId)
            .OrderByDescending(h => h.EffectiveFromUtc)
            .Select(h => new ProductPriceHistoryDto
            {
                Id = h.Id,
                ProductId = h.ProductId,
                OldStandardCost = h.OldStandardCost,
                NewStandardCost = h.NewStandardCost,
                OldSellingPrice = h.OldSellingPrice,
                NewSellingPrice = h.NewSellingPrice,
                OldMinRetailPrice = h.OldMinRetailPrice,
                NewMinRetailPrice = h.NewMinRetailPrice,
                ChangeReason = h.ChangeReason,
                EffectiveFromUtc = h.EffectiveFromUtc,
                ChangedBy = h.ChangedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);
        if (product == null) return false;

        var hasStockMovements = await _dbContext.StockLedgers
            .AnyAsync(sm => sm.ProductId == id && sm.CompanyId == companyId && !sm.IsDeleted, cancellationToken);
        if (hasStockMovements)
        {
            throw new InvalidOperationException($"Cannot soft-delete product '{product.ProductCode}': Product has recorded stock movements.");
        }

        var hasStock = await _dbContext.ProductStock
            .AnyAsync(s => s.ProductId == id && s.CompanyId == companyId && !s.IsDeleted && s.QuantityOnHand > 0, cancellationToken);
        if (hasStock)
        {
            throw new InvalidOperationException($"Cannot soft-delete product '{product.ProductCode}': Product has remaining inventory on hand.");
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        // System-generated business code enforcement:
        string code;
        if (string.IsNullOrWhiteSpace(request.ProductCode) ||
            request.ProductCode.Contains("Auto", StringComparison.OrdinalIgnoreCase) ||
            request.ProductCode.StartsWith("["))
        {
            code = await _documentSequenceService.GenerateCodeAsync(companyId, "Product", cancellationToken);
        }
        else
        {
            var rawCode = request.ProductCode.Trim().ToUpperInvariant();
            var exists = await _dbContext.Products.AnyAsync(p => p.CompanyId == companyId && p.ProductCode == rawCode, cancellationToken);
            if (exists)
            {
                code = await _documentSequenceService.GenerateCodeAsync(companyId, "Product", cancellationToken);
            }
            else
            {
                code = rawCode;
            }
        }

        int resolvedClassId;
        if (request.ProductClassId > 0)
        {
            var pClass = await _dbContext.ProductClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ProductClassId && c.CompanyId == companyId, cancellationToken)
                ?? throw new InvalidOperationException("The selected Product Class does not belong to the active company.");
            resolvedClassId = pClass.Id;
        }
        else
        {
            var defaultClass = await _dbContext.ProductClasses
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.IsActive)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No active Product Class exists for the active company.");
            resolvedClassId = defaultClass.Id;
        }

        int resolvedUomId;
        if (request.PrimaryUomId > 0)
        {
            var uom = await _dbContext.UnitsOfMeasure
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.PrimaryUomId && u.CompanyId == companyId, cancellationToken)
                ?? throw new InvalidOperationException("The selected Base UOM does not belong to the active company.");
            resolvedUomId = uom.Id;
        }
        else
        {
            var defaultUom = await _dbContext.UnitsOfMeasure
                .AsNoTracking()
                .Where(u => u.CompanyId == companyId && u.IsActive)
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("No active Unit of Measure exists for the active company.");
            resolvedUomId = defaultUom.Id;
        }

        if (request.DefaultWarehouseId is > 0)
        {
            var whExists = await _dbContext.Warehouses
                .AnyAsync(w => w.Id == request.DefaultWarehouseId.Value && w.CompanyId == companyId, cancellationToken);
            if (!whExists)
            {
                throw new InvalidOperationException("The selected Default Warehouse does not belong to the active company.");
            }
        }

        var lifecycleStatus = !string.IsNullOrWhiteSpace(request.LifecycleStatus) ? request.LifecycleStatus.Trim() : "Active";
        var isActive = !string.Equals(lifecycleStatus, "Inactive", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(lifecycleStatus, "Discontinued", StringComparison.OrdinalIgnoreCase);

        var product = new Product
        {
            CompanyId = companyId,
            ProductCode = code,
            ProductName = request.ProductName.Trim(),
            Description = request.Description?.Trim(),
            BarcodeGtin = request.BarcodeGtin?.Trim(),
            ProductClassId = resolvedClassId,
            SubcategoryId = request.SubcategoryId,
            BrandId = request.BrandId,
            MaterialGrade = request.MaterialGrade?.Trim(),
            PrimaryUomId = resolvedUomId,
            SecondaryUomId = request.SecondaryUomId,
            UomConversionFactor = request.UomConversionFactor,
            PackSize = request.PackSize?.Trim(),
            StandardCost = request.StandardCost,
            CurrentCost = request.CurrentCost > 0 ? request.CurrentCost : request.StandardCost,
            SellingPrice = request.SellingPrice,
            MinimumRetailPrice = request.MinimumRetailPrice > 0 ? request.MinimumRetailPrice : request.SellingPrice,
            TaxScheduleCode = !string.IsNullOrWhiteSpace(request.TaxScheduleCode) ? request.TaxScheduleCode.Trim() : "VAT_STD_15",
            ReorderLevel = request.ReorderLevel,
            IsActive = isActive,
            LifecycleStatus = lifecycleStatus,
            IsPurchasable = request.IsPurchasable ?? true,
            IsSellable = request.IsSellable ?? true,
            IsManufactured = request.IsManufactured ?? false,
            HsCode = request.HsCode?.Trim(),
            NetWeight = request.NetWeight ?? 0m,
            GrossWeight = request.GrossWeight ?? request.NetWeight ?? 0m,
            WeightUom = !string.IsNullOrWhiteSpace(request.WeightUom) ? request.WeightUom.Trim() : "KG",
            Length = request.Length,
            Width = request.Width,
            Height = request.Height,
            DimensionUom = !string.IsNullOrWhiteSpace(request.DimensionUom) ? request.DimensionUom.Trim() : "MM",
            VolumeCbm = request.VolumeCbm ?? (request.Length.HasValue && request.Width.HasValue && request.Height.HasValue ? (request.Length.Value * request.Width.Value * request.Height.Value) / 1000000000m : null),
            TrackingPolicy = request.TrackingPolicy ?? 1,
            SafetyStock = request.SafetyStock ?? 0m,
            LeadTimeDays = request.LeadTimeDays ?? 7,
            CostingMethod = !string.IsNullOrWhiteSpace(request.CostingMethod) ? request.CostingMethod.Trim() : "MovingAverage",
            TaxRatePercent = request.TaxRatePercent ?? 15.00m,
            DefaultWarehouseId = request.DefaultWarehouseId,
            DefaultStorageBin = request.DefaultStorageBin?.Trim(),
            QcPolicy = !string.IsNullOrWhiteSpace(request.QcPolicy) ? request.QcPolicy.Trim() : "Visual Inspection"
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.ProductPriceHistories.Add(new ProductPriceHistory
        {
            CompanyId = companyId,
            ProductId = product.Id,
            OldStandardCost = 0m,
            NewStandardCost = product.StandardCost,
            OldSellingPrice = 0m,
            NewSellingPrice = product.SellingPrice,
            OldMinRetailPrice = 0m,
            NewMinRetailPrice = product.MinimumRetailPrice,
            ChangeReason = "Initial Product Creation Baseline",
            EffectiveFromUtc = DateTime.UtcNow,
            ChangedBy = _currentUserContext.UserName ?? "System"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetProductByIdAsync(product.Id, cancellationToken))!;
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {id} was not found.");
        }

        var hasStockMovements = await _dbContext.StockLedgers
            .AnyAsync(sm => sm.ProductId == id && sm.CompanyId == companyId && !sm.IsDeleted, cancellationToken);

        // Enforce transaction lock on structural fields if stock movements exist
        if (hasStockMovements)
        {
            if (request.PrimaryUomId > 0 && request.PrimaryUomId != product.PrimaryUomId)
            {
                throw new InvalidOperationException($"Cannot change Base UOM for product '{product.ProductCode}' because stock transactions already exist.");
            }
            if (!string.IsNullOrWhiteSpace(request.CostingMethod) &&
                !string.Equals(request.CostingMethod.Trim(), product.CostingMethod, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Cannot change Costing Method for product '{product.ProductCode}' because stock transactions already exist.");
            }
        }

        // Record Price/Cost Audit History if commercial values changed
        var newMinRetail = request.MinimumRetailPrice > 0 ? request.MinimumRetailPrice : product.MinimumRetailPrice;
        if (product.StandardCost != request.StandardCost ||
            product.SellingPrice != request.SellingPrice ||
            product.MinimumRetailPrice != newMinRetail)
        {
            _dbContext.ProductPriceHistories.Add(new ProductPriceHistory
            {
                CompanyId = companyId,
                ProductId = product.Id,
                OldStandardCost = product.StandardCost,
                NewStandardCost = request.StandardCost,
                OldSellingPrice = product.SellingPrice,
                NewSellingPrice = request.SellingPrice,
                OldMinRetailPrice = product.MinimumRetailPrice,
                NewMinRetailPrice = newMinRetail,
                ChangeReason = "Updated via Product Specification Master",
                EffectiveFromUtc = DateTime.UtcNow,
                ChangedBy = _currentUserContext.UserName ?? "System"
            });
        }

        // Code immutability rule: product.ProductCode remains strictly unmodified!
        if (request.ProductClassId > 0 && request.ProductClassId != product.ProductClassId)
        {
            var validClass = await _dbContext.ProductClasses
                .AnyAsync(c => c.Id == request.ProductClassId && c.CompanyId == companyId, cancellationToken);
            if (!validClass)
            {
                throw new InvalidOperationException("The selected Product Class does not belong to the active company.");
            }
            product.ProductClassId = request.ProductClassId;
        }
        if (request.PrimaryUomId > 0 && request.PrimaryUomId != product.PrimaryUomId)
        {
            var validUom = await _dbContext.UnitsOfMeasure
                .AnyAsync(u => u.Id == request.PrimaryUomId && u.CompanyId == companyId, cancellationToken);
            if (!validUom)
            {
                throw new InvalidOperationException("The selected Base UOM does not belong to the active company.");
            }
            product.PrimaryUomId = request.PrimaryUomId;
        }
        if (request.DefaultWarehouseId is > 0 && request.DefaultWarehouseId != product.DefaultWarehouseId)
        {
            var validWh = await _dbContext.Warehouses
                .AnyAsync(w => w.Id == request.DefaultWarehouseId.Value && w.CompanyId == companyId, cancellationToken);
            if (!validWh)
            {
                throw new InvalidOperationException("The selected Default Warehouse does not belong to the active company.");
            }
        }

        product.ProductName = request.ProductName.Trim();
        product.Description = request.Description?.Trim();
        product.BarcodeGtin = request.BarcodeGtin?.Trim();
        product.SubcategoryId = request.SubcategoryId;
        product.BrandId = request.BrandId;
        if (request.MaterialGrade != null) product.MaterialGrade = request.MaterialGrade.Trim();
        product.SecondaryUomId = request.SecondaryUomId;
        product.UomConversionFactor = request.UomConversionFactor;
        product.PackSize = request.PackSize?.Trim();
        product.StandardCost = request.StandardCost;

        // Protect CurrentCost (Moving Average) if stock movements exist; only sync CurrentCost with StandardCost when no transactions exist
        if (!hasStockMovements)
        {
            product.CurrentCost = request.CurrentCost > 0 ? request.CurrentCost : request.StandardCost;
        }

        product.SellingPrice = request.SellingPrice;
        if (request.MinimumRetailPrice >= 0) product.MinimumRetailPrice = request.MinimumRetailPrice;
        if (!string.IsNullOrWhiteSpace(request.TaxScheduleCode)) product.TaxScheduleCode = request.TaxScheduleCode.Trim();
        product.ReorderLevel = request.ReorderLevel;

        if (!string.IsNullOrWhiteSpace(request.LifecycleStatus))
        {
            product.LifecycleStatus = request.LifecycleStatus.Trim();
            product.IsActive = !string.Equals(product.LifecycleStatus, "Inactive", StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(product.LifecycleStatus, "Discontinued", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            product.IsActive = request.IsActive;
            product.LifecycleStatus = request.IsActive ? "Active" : "Inactive";
        }

        if (request.IsPurchasable.HasValue) product.IsPurchasable = request.IsPurchasable.Value;
        if (request.IsSellable.HasValue) product.IsSellable = request.IsSellable.Value;
        if (request.IsManufactured.HasValue) product.IsManufactured = request.IsManufactured.Value;

        // International Standards
        product.HsCode = request.HsCode?.Trim();
        if (request.NetWeight.HasValue) product.NetWeight = request.NetWeight.Value;
        if (request.GrossWeight.HasValue) product.GrossWeight = request.GrossWeight.Value;
        if (!string.IsNullOrWhiteSpace(request.WeightUom)) product.WeightUom = request.WeightUom.Trim();
        product.Length = request.Length;
        product.Width = request.Width;
        product.Height = request.Height;
        if (!string.IsNullOrWhiteSpace(request.DimensionUom)) product.DimensionUom = request.DimensionUom.Trim();
        product.VolumeCbm = request.VolumeCbm ?? (request.Length.HasValue && request.Width.HasValue && request.Height.HasValue ? (request.Length.Value * request.Width.Value * request.Height.Value) / 1000000000m : product.VolumeCbm);
        if (request.TrackingPolicy.HasValue) product.TrackingPolicy = request.TrackingPolicy.Value;
        if (request.SafetyStock.HasValue) product.SafetyStock = request.SafetyStock.Value;
        if (request.LeadTimeDays.HasValue) product.LeadTimeDays = request.LeadTimeDays.Value;
        if (!string.IsNullOrWhiteSpace(request.CostingMethod)) product.CostingMethod = request.CostingMethod.Trim();
        if (request.TaxRatePercent.HasValue) product.TaxRatePercent = request.TaxRatePercent.Value;
        product.DefaultWarehouseId = request.DefaultWarehouseId;
        if (request.DefaultStorageBin != null) product.DefaultStorageBin = request.DefaultStorageBin.Trim();
        if (!string.IsNullOrWhiteSpace(request.QcPolicy)) product.QcPolicy = request.QcPolicy.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetProductByIdAsync(product.Id, cancellationToken))!;
    }

    // --- Product Classes ---

    public async Task<List<ProductClassDto>> GetProductClassesAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from pc in _dbContext.ProductClasses.AsNoTracking()
                    where pc.CompanyId == companyId
                    join invAcct in _dbContext.Accounts.AsNoTracking() on pc.GlInventoryAccountId equals invAcct.Id into invJoin
                    from invAcct in invJoin.DefaultIfEmpty()
                    join cogsAcct in _dbContext.Accounts.AsNoTracking() on pc.GlCogsAccountId equals cogsAcct.Id into cogsJoin
                    from cogsAcct in cogsJoin.DefaultIfEmpty()
                    join revAcct in _dbContext.Accounts.AsNoTracking() on pc.GlRevenueAccountId equals revAcct.Id into revJoin
                    from revAcct in revJoin.DefaultIfEmpty()
                    orderby pc.ClassCode
                    select new ProductClassDto
                    {
                        Id = pc.Id,
                        ClassCode = pc.ClassCode,
                        ClassName = pc.ClassName,
                        CategoryType = pc.CategoryType,
                        CategoryTypeName = pc.CategoryType == ProductCategoryType.FinishedGoods ? "Finished Goods" :
                                           pc.CategoryType == ProductCategoryType.RawMaterials ? "Raw Materials" :
                                           pc.CategoryType == ProductCategoryType.SemiFinishedGoods ? "Semi-Finished / WIP" :
                                           pc.CategoryType == ProductCategoryType.Consumables ? "Consumables & Spares" :
                                           pc.CategoryType == ProductCategoryType.Service ? "Service / Non-Stock" : "Fixed Asset Item",
                        GlInventoryAccountId = pc.GlInventoryAccountId,
                        GlInventoryAccountCode = invAcct != null ? invAcct.AccountCode : null,
                        GlCogsAccountId = pc.GlCogsAccountId,
                        GlCogsAccountCode = cogsAcct != null ? cogsAcct.AccountCode : null,
                        GlRevenueAccountId = pc.GlRevenueAccountId,
                        GlRevenueAccountCode = revAcct != null ? revAcct.AccountCode : null,
                        IsActive = pc.IsActive
                    };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ProductClassDto> CreateProductClassAsync(CreateProductClassRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.ClassCode.Trim().ToUpperInvariant();
        var exists = await _dbContext.ProductClasses.AnyAsync(c => c.CompanyId == companyId && c.ClassCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Product class with code '{code}' already exists.");
        }

        // Resolve fallback GL accounts if caller passes 0 (e.g., from Quick-Add modal)
        int invAccId = request.GlInventoryAccountId;
        int cogsAccId = request.GlCogsAccountId;
        int revAccId = request.GlRevenueAccountId;
        if (invAccId <= 0 || cogsAccId <= 0 || revAccId <= 0)
        {
            var existingClass = await _dbContext.ProductClasses.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId, cancellationToken);
            if (existingClass != null)
            {
                if (invAccId <= 0) invAccId = existingClass.GlInventoryAccountId;
                if (cogsAccId <= 0) cogsAccId = existingClass.GlCogsAccountId;
                if (revAccId <= 0) revAccId = existingClass.GlRevenueAccountId;
            }
            else
            {
                var firstAccId = await _dbContext.Accounts.AsNoTracking().Where(a => a.CompanyId == companyId).Select(a => a.Id).FirstOrDefaultAsync(cancellationToken);
                if (invAccId <= 0) invAccId = firstAccId > 0 ? firstAccId : 1;
                if (cogsAccId <= 0) cogsAccId = firstAccId > 0 ? firstAccId : 1;
                if (revAccId <= 0) revAccId = firstAccId > 0 ? firstAccId : 1;
            }
        }

        var productClass = new ProductClass
        {
            CompanyId = companyId,
            ClassCode = code,
            ClassName = request.ClassName.Trim(),
            CategoryType = Enum.IsDefined(typeof(ProductCategoryType), request.CategoryType) ? request.CategoryType : ProductCategoryType.FinishedGoods,
            GlInventoryAccountId = invAccId,
            GlCogsAccountId = cogsAccId,
            GlRevenueAccountId = revAccId,
            IsActive = request.IsActive
        };

        _dbContext.ProductClasses.Add(productClass);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetProductClassesAsync(cancellationToken)).First(c => c.Id == productClass.Id);
    }

    public async Task<ProductClassDto> UpdateProductClassAsync(int id, CreateProductClassRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var productClass = await _dbContext.ProductClasses.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId, cancellationToken);
        if (productClass == null)
        {
            throw new KeyNotFoundException($"Product class with ID {id} was not found.");
        }

        productClass.ClassName = request.ClassName.Trim();
        if (Enum.IsDefined(typeof(ProductCategoryType), request.CategoryType))
        {
            productClass.CategoryType = request.CategoryType;
        }
        if (request.GlInventoryAccountId > 0) productClass.GlInventoryAccountId = request.GlInventoryAccountId;
        if (request.GlCogsAccountId > 0) productClass.GlCogsAccountId = request.GlCogsAccountId;
        if (request.GlRevenueAccountId > 0) productClass.GlRevenueAccountId = request.GlRevenueAccountId;
        productClass.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetProductClassesAsync(cancellationToken)).First(c => c.Id == productClass.Id);
    }

    public async Task<bool> DeleteProductClassAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var productClass = await _dbContext.ProductClasses.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId, cancellationToken);
        if (productClass == null) return false;

        var inUse = await _dbContext.Products.AnyAsync(p => p.CompanyId == companyId && p.ProductClassId == id && !p.IsDeleted, cancellationToken);
        if (inUse)
        {
            productClass.IsActive = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        _dbContext.ProductClasses.Remove(productClass);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- Units of Measure ---

    public async Task<List<UnitOfMeasureDto>> GetUnitsOfMeasureAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await _dbContext.UnitsOfMeasure.AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.UomCode)
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                UomCode = u.UomCode,
                UomName = u.UomName,
                UomCategory = u.UomCategory,
                DecimalPrecision = u.DecimalPrecision,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UnitOfMeasureDto> CreateUnitOfMeasureAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.UomCode.Trim().ToUpperInvariant();
        var exists = await _dbContext.UnitsOfMeasure.AnyAsync(u => u.CompanyId == companyId && u.UomCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Unit of measure with code '{code}' already exists.");
        }

        var uom = new UnitOfMeasure
        {
            CompanyId = companyId,
            UomCode = code,
            UomName = request.UomName.Trim(),
            UomCategory = !string.IsNullOrWhiteSpace(request.UomCategory) ? request.UomCategory.Trim() : "Unit",
            DecimalPrecision = request.DecimalPrecision ?? 2,
            IsActive = request.IsActive ?? true
        };

        _dbContext.UnitsOfMeasure.Add(uom);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UnitOfMeasureDto
        {
            Id = uom.Id,
            UomCode = uom.UomCode,
            UomName = uom.UomName,
            UomCategory = uom.UomCategory,
            DecimalPrecision = uom.DecimalPrecision,
            IsActive = uom.IsActive
        };
    }

    public async Task<UnitOfMeasureDto> UpdateUnitOfMeasureAsync(int id, CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var uom = await _dbContext.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId, cancellationToken);
        if (uom == null)
        {
            throw new KeyNotFoundException($"Unit of measure with ID {id} was not found.");
        }

        uom.UomName = request.UomName.Trim();
        if (!string.IsNullOrWhiteSpace(request.UomCategory)) uom.UomCategory = request.UomCategory.Trim();
        if (request.DecimalPrecision.HasValue) uom.DecimalPrecision = request.DecimalPrecision.Value;
        if (request.IsActive.HasValue) uom.IsActive = request.IsActive.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UnitOfMeasureDto
        {
            Id = uom.Id,
            UomCode = uom.UomCode,
            UomName = uom.UomName,
            UomCategory = uom.UomCategory,
            DecimalPrecision = uom.DecimalPrecision,
            IsActive = uom.IsActive
        };
    }

    public async Task<bool> DeleteUnitOfMeasureAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var uom = await _dbContext.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId, cancellationToken);
        if (uom == null) return false;

        var inUse = await _dbContext.Products.AnyAsync(p => p.CompanyId == companyId && p.PrimaryUomId == id && !p.IsDeleted, cancellationToken);
        if (inUse)
        {
            uom.IsActive = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        _dbContext.UnitsOfMeasure.Remove(uom);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- Configurable Master Lookups ---

    public async Task<List<MasterLookupDto>> GetMasterLookupsAsync(
        string? lookupType = null,
        int? parentId = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.MasterLookups.AsNoTracking()
            .Where(m => !m.IsDeleted && (m.CompanyId == 0 || m.CompanyId == companyId));

        if (!string.IsNullOrWhiteSpace(lookupType))
        {
            var normalizedType = lookupType.Trim().ToUpperInvariant();
            query = query.Where(m => m.LookupType == normalizedType);
        }

        if (parentId.HasValue)
        {
            query = query.Where(m => m.ParentId == null || m.ParentId == parentId.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderBy(m => m.LookupType)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.LookupName)
            .Select(m => new MasterLookupDto
            {
                Id = m.Id,
                CompanyId = m.CompanyId,
                LookupType = m.LookupType,
                LookupCode = m.LookupCode,
                LookupName = m.LookupName,
                Description = m.Description,
                NumericValue = m.NumericValue,
                ParentId = m.ParentId,
                SortOrder = m.SortOrder,
                IsActive = m.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<MasterLookupDto> CreateMasterLookupAsync(SaveMasterLookupRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LookupType) || string.IsNullOrWhiteSpace(request.LookupName))
        {
            throw new InvalidOperationException("Lookup Type and Lookup Name are required.");
        }

        var companyId = request.IsGlobal ? 0 : GetCompanyId();
        var normalizedType = request.LookupType.Trim().ToUpperInvariant();
        var normalizedName = request.LookupName.Trim();
        var normalizedCode = !string.IsNullOrWhiteSpace(request.LookupCode)
            ? request.LookupCode.Trim()
            : normalizedName.ToUpperInvariant().Replace(" ", "-");

        var duplicate = await _dbContext.MasterLookups.AnyAsync(
            m => !m.IsDeleted &&
                 (m.CompanyId == 0 || m.CompanyId == companyId) &&
                 m.LookupType == normalizedType &&
                 (m.LookupCode == normalizedCode || m.LookupName == normalizedName),
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException($"A '{normalizedType}' lookup entry with code '{normalizedCode}' or name '{normalizedName}' already exists.");
        }

        var lookup = new MasterLookup
        {
            CompanyId = companyId,
            LookupType = normalizedType,
            LookupCode = normalizedCode,
            LookupName = normalizedName,
            Description = request.Description?.Trim(),
            NumericValue = request.NumericValue,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        _dbContext.MasterLookups.Add(lookup);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MasterLookupDto
        {
            Id = lookup.Id,
            CompanyId = lookup.CompanyId,
            LookupType = lookup.LookupType,
            LookupCode = lookup.LookupCode,
            LookupName = lookup.LookupName,
            Description = lookup.Description,
            NumericValue = lookup.NumericValue,
            ParentId = lookup.ParentId,
            SortOrder = lookup.SortOrder,
            IsActive = lookup.IsActive
        };
    }

        public async Task<MasterLookupDto> UpdateMasterLookupAsync(int id, SaveMasterLookupRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var lookup = await _dbContext.MasterLookups.FirstOrDefaultAsync(
            m => m.Id == id && !m.IsDeleted && m.CompanyId == companyId,
            cancellationToken);

        if (lookup == null)
        {
            throw new KeyNotFoundException($"Master lookup with ID {id} was not found or is a protected group-wide standard.");
        }

        if (!string.IsNullOrWhiteSpace(request.LookupName)) lookup.LookupName = request.LookupName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LookupCode)) lookup.LookupCode = request.LookupCode.Trim();
        lookup.Description = request.Description?.Trim();
        lookup.NumericValue = request.NumericValue;
        lookup.ParentId = request.ParentId;
        lookup.SortOrder = request.SortOrder;
        lookup.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MasterLookupDto
        {
            Id = lookup.Id,
            CompanyId = lookup.CompanyId,
            LookupType = lookup.LookupType,
            LookupCode = lookup.LookupCode,
            LookupName = lookup.LookupName,
            Description = lookup.Description,
            NumericValue = lookup.NumericValue,
            ParentId = lookup.ParentId,
            SortOrder = lookup.SortOrder,
            IsActive = lookup.IsActive
        };
    }

    public async Task<bool> DeleteMasterLookupAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var lookup = await _dbContext.MasterLookups.FirstOrDefaultAsync(
            m => m.Id == id && !m.IsDeleted && (m.CompanyId == companyId || m.CompanyId == 0),
            cancellationToken);

        if (lookup == null) return false;

        lookup.IsActive = false;
        _dbContext.MasterLookups.Remove(lookup);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- Depots & Facilities ---

    public async Task<List<DepotDto>> GetDepotsAsync(int? depotType = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.Depots.AsNoTracking().Where(d => d.CompanyId == companyId);
        if (depotType.HasValue)
        {
            query = query.Where(d => (int)d.DepotType == depotType.Value);
        }
        return await query
            .OrderBy(d => d.DepotCode)
            .Select(d => new DepotDto
            {
                Id = d.Id,
                DepotCode = d.DepotCode,
                DepotName = d.DepotName,
                DepotType = (int)d.DepotType,
                DepotTypeName = d.DepotType == DepotType.ManufacturingPlant ? "Manufacturing Plant" :
                                d.DepotType == DepotType.RegionalDepot ? "Regional Logistics Depot" : "Central Distribution Hub",
                Address = d.Address,
                City = d.City,
                ContactPerson = d.ContactPerson,
                ContactPhone = d.ContactPhone,
                IsActive = d.IsActive,
                WarehouseCount = d.Warehouses.Count(w => !w.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DepotDto> CreateDepotAsync(CreateDepotRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var exists = await _dbContext.Depots.AnyAsync(d => d.CompanyId == companyId && d.DepotCode == request.DepotCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Depot with code '{request.DepotCode}' already exists.");
        }

        var depot = new Depot
        {
            CompanyId = companyId,
            DepotCode = request.DepotCode.Trim().ToUpperInvariant(),
            DepotName = request.DepotName.Trim(),
            DepotType = Enum.IsDefined(typeof(DepotType), request.DepotType) ? (DepotType)request.DepotType : DepotType.ManufacturingPlant,
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            IsActive = true
        };

        _dbContext.Depots.Add(depot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DepotDto
        {
            Id = depot.Id,
            DepotCode = depot.DepotCode,
            DepotName = depot.DepotName,
            DepotType = (int)depot.DepotType,
            DepotTypeName = depot.DepotType == DepotType.ManufacturingPlant ? "Manufacturing Plant" :
                            depot.DepotType == DepotType.RegionalDepot ? "Regional Logistics Depot" : "Central Distribution Hub",
            Address = depot.Address,
            City = depot.City,
            ContactPerson = depot.ContactPerson,
            ContactPhone = depot.ContactPhone,
            IsActive = depot.IsActive,
            WarehouseCount = 0
        };
    }

    // --- Warehouses ---

    public async Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await _dbContext.Warehouses.AsNoTracking()
            .Include(w => w.Depot)
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.WarehouseCode)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                WarehouseCode = w.WarehouseCode,
                WarehouseName = w.WarehouseName,
                Location = w.Location,
                IsActive = w.IsActive,
                DepotId = w.DepotId,
                DepotName = w.Depot != null ? w.Depot.DepotName : null,
                WarehouseType = (int)w.WarehouseType,
                WarehouseTypeName = w.WarehouseType == WarehouseType.RawMaterial ? "Raw Materials" :
                                   w.WarehouseType == WarehouseType.FinishedGoods ? "Finished Goods" :
                                   w.WarehouseType == WarehouseType.WorkInProgress ? "Work In Progress" :
                                   w.WarehouseType == WarehouseType.SparesAndHardware ? "Spares & Fasteners" : "General Transit"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var exists = await _dbContext.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.WarehouseCode == request.WarehouseCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Warehouse with code '{request.WarehouseCode}' already exists.");
        }

        string? depotName = null;
        if (request.DepotId.HasValue)
        {
            var depot = await _dbContext.Depots.FirstOrDefaultAsync(d => d.Id == request.DepotId.Value && d.CompanyId == companyId, cancellationToken);
            if (depot == null)
            {
                throw new InvalidOperationException($"Depot with ID '{request.DepotId.Value}' does not exist.");
            }
            depotName = depot.DepotName;
        }

        var warehouse = new Warehouse
        {
            CompanyId = companyId,
            WarehouseCode = request.WarehouseCode.Trim().ToUpperInvariant(),
            WarehouseName = request.WarehouseName.Trim(),
            Location = request.Location?.Trim(),
            DepotId = request.DepotId,
            WarehouseType = Enum.IsDefined(typeof(WarehouseType), request.WarehouseType) ? (WarehouseType)request.WarehouseType : WarehouseType.FinishedGoods,
            IsActive = true
        };

        _dbContext.Warehouses.Add(warehouse);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new WarehouseDto
        {
            Id = warehouse.Id,
            WarehouseCode = warehouse.WarehouseCode,
            WarehouseName = warehouse.WarehouseName,
            Location = warehouse.Location,
            IsActive = warehouse.IsActive,
            DepotId = warehouse.DepotId,
            DepotName = depotName,
            WarehouseType = (int)warehouse.WarehouseType,
            WarehouseTypeName = warehouse.WarehouseType == WarehouseType.RawMaterial ? "Raw Materials" :
                               warehouse.WarehouseType == WarehouseType.FinishedGoods ? "Finished Goods" :
                               warehouse.WarehouseType == WarehouseType.WorkInProgress ? "Work In Progress" :
                               warehouse.WarehouseType == WarehouseType.SparesAndHardware ? "Spares & Fasteners" : "General Transit"
        };
    }

    // --- Stock Balances ---

    public async Task<List<ProductStockDto>> GetProductStocksAsync(int? warehouseId = null, int? productId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from s in _dbContext.ProductStock.AsNoTracking()
                    where s.CompanyId == companyId
                    join w in _dbContext.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id
                    join p in _dbContext.Products.AsNoTracking() on s.ProductId equals p.Id
                    join u in _dbContext.UnitsOfMeasure.AsNoTracking() on p.PrimaryUomId equals u.Id into uomJoin
                    from u in uomJoin.DefaultIfEmpty()
                    select new { s, w, p, u };

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.s.WarehouseId == warehouseId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.s.ProductId == productId.Value);
        }

        return await query.Select(x => new ProductStockDto
        {
            WarehouseId = x.w.Id,
            WarehouseCode = x.w.WarehouseCode,
            WarehouseName = x.w.WarehouseName,
            ProductId = x.p.Id,
            ProductCode = x.p.ProductCode,
            ProductName = x.p.ProductName,
            UomCode = x.u != null ? x.u.UomCode : string.Empty,
            QuantityOnHand = x.s.QuantityOnHand,
            AllocatedQuantity = x.s.AllocatedQuantity,
            AvailableQuantity = x.s.QuantityOnHand - x.s.AllocatedQuantity,
            AverageCost = x.s.AverageCost,
            TotalValuation = x.s.QuantityOnHand * x.s.AverageCost,
            LastTransactionDate = x.s.LastTransactionDate
        }).ToListAsync(cancellationToken);
    }
}
