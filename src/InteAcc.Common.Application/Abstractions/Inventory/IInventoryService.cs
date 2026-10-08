using InteAcc.Common.Application.Models.Inventory;

namespace InteAcc.Common.Application.Abstractions.Inventory;

public interface IInventoryService
{
    // Product Master
    Task<List<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    // Product Classes
    Task<List<ProductClassDto>> GetProductClassesAsync(CancellationToken cancellationToken = default);
    Task<ProductClassDto> CreateProductClassAsync(CreateProductClassRequest request, CancellationToken cancellationToken = default);

    // Units of Measure
    Task<List<UnitOfMeasureDto>> GetUnitsOfMeasureAsync(CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto> CreateUnitOfMeasureAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default);

    // Warehouses
    Task<List<WarehouseDto>> GetWarehousesAsync(CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);

    // Stock Level Lookups
    Task<List<ProductStockDto>> GetProductStocksAsync(int? warehouseId = null, int? productId = null, CancellationToken cancellationToken = default);
}
