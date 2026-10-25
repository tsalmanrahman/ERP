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

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    // --- Product Master ---

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from p in _dbContext.Products.AsNoTracking()
                    where p.CompanyId == companyId
                    join pc in _dbContext.ProductClasses.AsNoTracking() on p.ProductClassId equals pc.Id into pcJoin
                    from pc in pcJoin.DefaultIfEmpty()
                    join uom in _dbContext.UnitsOfMeasure.AsNoTracking() on p.PrimaryUomId equals uom.Id into uomJoin
                    from uom in uomJoin.DefaultIfEmpty()
                    join wh in _dbContext.Warehouses.AsNoTracking() on p.DefaultWarehouseId equals wh.Id into whJoin
                    from wh in whJoin.DefaultIfEmpty()
                    select new ProductDto
                    {
                        Id = p.Id,
                        ProductCode = p.ProductCode,
                        ProductName = p.ProductName,
                        ProductClassId = p.ProductClassId,
                        ClassCode = pc != null ? pc.ClassCode : null,
                        ClassName = pc != null ? pc.ClassName : null,
                        PrimaryUomId = p.PrimaryUomId,
                        UomCode = uom != null ? uom.UomCode : null,
                        PackSize = p.PackSize,
                        StandardCost = p.StandardCost,
                        CurrentCost = p.CurrentCost,
                        SellingPrice = p.SellingPrice,
                        ReorderLevel = p.ReorderLevel,
                        IsActive = p.IsActive,
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
                        TotalQuantityOnHand = _dbContext.ProductStock
                            .Where(s => s.CompanyId == companyId && s.ProductId == p.Id && !s.IsDeleted)
                            .Sum(s => (decimal?)s.QuantityOnHand) ?? 0m
                    };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from p in _dbContext.Products.AsNoTracking()
                    where p.CompanyId == companyId && p.Id == id
                    join pc in _dbContext.ProductClasses.AsNoTracking() on p.ProductClassId equals pc.Id into pcJoin
                    from pc in pcJoin.DefaultIfEmpty()
                    join uom in _dbContext.UnitsOfMeasure.AsNoTracking() on p.PrimaryUomId equals uom.Id into uomJoin
                    from uom in uomJoin.DefaultIfEmpty()
                    join wh in _dbContext.Warehouses.AsNoTracking() on p.DefaultWarehouseId equals wh.Id into whJoin
                    from wh in whJoin.DefaultIfEmpty()
                    select new ProductDto
                    {
                        Id = p.Id,
                        ProductCode = p.ProductCode,
                        ProductName = p.ProductName,
                        ProductClassId = p.ProductClassId,
                        ClassCode = pc != null ? pc.ClassCode : null,
                        ClassName = pc != null ? pc.ClassName : null,
                        PrimaryUomId = p.PrimaryUomId,
                        UomCode = uom != null ? uom.UomCode : null,
                        PackSize = p.PackSize,
                        StandardCost = p.StandardCost,
                        CurrentCost = p.CurrentCost,
                        SellingPrice = p.SellingPrice,
                        ReorderLevel = p.ReorderLevel,
                        IsActive = p.IsActive,
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
                        TotalQuantityOnHand = _dbContext.ProductStock
                            .Where(s => s.CompanyId == companyId && s.ProductId == p.Id && !s.IsDeleted)
                            .Sum(s => (decimal?)s.QuantityOnHand) ?? 0m
                    };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = from p in _dbContext.Products.AsNoTracking()
                    where p.CompanyId == companyId && p.ProductCode == normalizedCode
                    join pc in _dbContext.ProductClasses.AsNoTracking() on p.ProductClassId equals pc.Id into pcJoin
                    from pc in pcJoin.DefaultIfEmpty()
                    join uom in _dbContext.UnitsOfMeasure.AsNoTracking() on p.PrimaryUomId equals uom.Id into uomJoin
                    from uom in uomJoin.DefaultIfEmpty()
                    join wh in _dbContext.Warehouses.AsNoTracking() on p.DefaultWarehouseId equals wh.Id into whJoin
                    from wh in whJoin.DefaultIfEmpty()
                    select new ProductDto
                    {
                        Id = p.Id,
                        ProductCode = p.ProductCode,
                        ProductName = p.ProductName,
                        ProductClassId = p.ProductClassId,
                        ClassCode = pc != null ? pc.ClassCode : null,
                        ClassName = pc != null ? pc.ClassName : null,
                        PrimaryUomId = p.PrimaryUomId,
                        UomCode = uom != null ? uom.UomCode : null,
                        PackSize = p.PackSize,
                        StandardCost = p.StandardCost,
                        CurrentCost = p.CurrentCost,
                        SellingPrice = p.SellingPrice,
                        ReorderLevel = p.ReorderLevel,
                        IsActive = p.IsActive,
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
                        TotalQuantityOnHand = _dbContext.ProductStock
                            .Where(s => s.CompanyId == companyId && s.ProductId == p.Id && !s.IsDeleted)
                            .Sum(s => (decimal?)s.QuantityOnHand) ?? 0m
                    };

        return await query.FirstOrDefaultAsync(cancellationToken);
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
        // If client passes null, empty, auto-placeholder, or a manual code, generate/ensure a safe unique code
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
                // Concurrency & duplicate safety: generate through sequence
                code = await _documentSequenceService.GenerateCodeAsync(companyId, "Product", cancellationToken);
            }
            else
            {
                code = rawCode;
            }
        }

        var product = new Product
        {
            CompanyId = companyId,
            ProductCode = code,
            ProductName = request.ProductName.Trim(),
            ProductClassId = request.ProductClassId > 0 ? request.ProductClassId : 1,
            PrimaryUomId = request.PrimaryUomId > 0 ? request.PrimaryUomId : 1,
            PackSize = request.PackSize?.Trim(),
            StandardCost = request.StandardCost,
            CurrentCost = request.CurrentCost > 0 ? request.CurrentCost : request.StandardCost,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = true,
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
            DefaultWarehouseId = request.DefaultWarehouseId
        };

        _dbContext.Products.Add(product);
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

        // Code immutability rule: product.ProductCode remains strictly unmodified!
        product.ProductName = request.ProductName.Trim();
        if (request.ProductClassId > 0) product.ProductClassId = request.ProductClassId;
        if (request.PrimaryUomId > 0) product.PrimaryUomId = request.PrimaryUomId;
        product.PackSize = request.PackSize?.Trim();
        product.StandardCost = request.StandardCost;
        product.CurrentCost = request.CurrentCost;
        product.SellingPrice = request.SellingPrice;
        product.ReorderLevel = request.ReorderLevel;
        product.IsActive = request.IsActive;

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
                    select new ProductClassDto
                    {
                        Id = pc.Id,
                        ClassCode = pc.ClassCode,
                        ClassName = pc.ClassName,
                        CategoryType = pc.CategoryType,
                        GlInventoryAccountId = pc.GlInventoryAccountId,
                        GlInventoryAccountCode = invAcct != null ? invAcct.AccountCode : null,
                        GlCogsAccountId = pc.GlCogsAccountId,
                        GlCogsAccountCode = cogsAcct != null ? cogsAcct.AccountCode : null,
                        GlRevenueAccountId = pc.GlRevenueAccountId,
                        GlRevenueAccountCode = revAcct != null ? revAcct.AccountCode : null
                    };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ProductClassDto> CreateProductClassAsync(CreateProductClassRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var exists = await _dbContext.ProductClasses.AnyAsync(c => c.CompanyId == companyId && c.ClassCode == request.ClassCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Product class with code '{request.ClassCode}' already exists.");
        }

        var productClass = new ProductClass
        {
            CompanyId = companyId,
            ClassCode = request.ClassCode.Trim().ToUpperInvariant(),
            ClassName = request.ClassName.Trim(),
            CategoryType = request.CategoryType,
            GlInventoryAccountId = request.GlInventoryAccountId,
            GlCogsAccountId = request.GlCogsAccountId,
            GlRevenueAccountId = request.GlRevenueAccountId
        };

        _dbContext.ProductClasses.Add(productClass);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetProductClassesAsync(cancellationToken)).First(c => c.Id == productClass.Id);
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
                UomName = u.UomName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UnitOfMeasureDto> CreateUnitOfMeasureAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var exists = await _dbContext.UnitsOfMeasure.AnyAsync(u => u.CompanyId == companyId && u.UomCode == request.UomCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Unit of measure with code '{request.UomCode}' already exists.");
        }

        var uom = new UnitOfMeasure
        {
            CompanyId = companyId,
            UomCode = request.UomCode.Trim().ToUpperInvariant(),
            UomName = request.UomName.Trim()
        };

        _dbContext.UnitsOfMeasure.Add(uom);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UnitOfMeasureDto { Id = uom.Id, UomCode = uom.UomCode, UomName = uom.UomName };
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
