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
            IsActive = true
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

    // --- Warehouses ---

    public async Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await _dbContext.Warehouses.AsNoTracking()
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.WarehouseCode)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                WarehouseCode = w.WarehouseCode,
                WarehouseName = w.WarehouseName,
                Location = w.Location,
                IsActive = w.IsActive
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

        var warehouse = new Warehouse
        {
            CompanyId = companyId,
            WarehouseCode = request.WarehouseCode.Trim().ToUpperInvariant(),
            WarehouseName = request.WarehouseName.Trim(),
            Location = request.Location?.Trim(),
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
            IsActive = warehouse.IsActive
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
