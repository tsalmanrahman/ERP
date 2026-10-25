using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Inventory;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/inv/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public ProductsController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _inventoryService.GetProductsAsync(cancellationToken);
        return Ok(new { success = true, data = products });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var product = await _inventoryService.GetProductByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound(new { success = false, message = $"Product with ID {id} was not found." });
        }
        return Ok(new { success = true, data = product });
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetProductByCode(string code, CancellationToken cancellationToken)
    {
        var product = await _inventoryService.GetProductByCodeAsync(code, cancellationToken);
        if (product == null)
        {
            return NotFound(new { success = false, message = $"Product with code '{code}' was not found." });
        }
        return Ok(new { success = true, data = product });
    }

    [HttpGet("{id:int}/price-history")]
    public async Task<IActionResult> GetProductPriceHistory(int id, CancellationToken cancellationToken)
    {
        var history = await _inventoryService.GetProductPriceHistoryAsync(id, cancellationToken);
        return Ok(new { success = true, data = history });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.SoftDeleteProductAsync(id, cancellationToken);
            if (!result)
            {
                return NotFound(new { success = false, message = $"Product with ID {id} was not found." });
            }
            return Ok(new { success = true, message = "Product soft-deleted successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _inventoryService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, new { success = true, data = product });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _inventoryService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = product });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // --- Product Classes ---

    [HttpGet("classes")]
    public async Task<IActionResult> GetProductClasses(CancellationToken cancellationToken)
    {
        var classes = await _inventoryService.GetProductClassesAsync(cancellationToken);
        return Ok(new { success = true, data = classes });
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateProductClass([FromBody] CreateProductClassRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateProductClassAsync(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("classes/{id:int}")]
    public async Task<IActionResult> UpdateProductClass(int id, [FromBody] CreateProductClassRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.UpdateProductClassAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("classes/{id:int}")]
    public async Task<IActionResult> DeleteProductClass(int id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.DeleteProductClassAsync(id, cancellationToken);
        if (!result) return NotFound(new { success = false, message = $"Product class with ID {id} was not found." });
        return Ok(new { success = true, message = "Product class updated/deactivated successfully." });
    }

    // --- Units of Measure ---

    [HttpGet("uoms")]
    public async Task<IActionResult> GetUnitsOfMeasure(CancellationToken cancellationToken)
    {
        var uoms = await _inventoryService.GetUnitsOfMeasureAsync(cancellationToken);
        return Ok(new { success = true, data = uoms });
    }

    [HttpPost("uoms")]
    public async Task<IActionResult> CreateUnitOfMeasure([FromBody] CreateUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateUnitOfMeasureAsync(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("uoms/{id:int}")]
    public async Task<IActionResult> UpdateUnitOfMeasure(int id, [FromBody] CreateUnitOfMeasureRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.UpdateUnitOfMeasureAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("uoms/{id:int}")]
    public async Task<IActionResult> DeleteUnitOfMeasure(int id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.DeleteUnitOfMeasureAsync(id, cancellationToken);
        if (!result) return NotFound(new { success = false, message = $"Unit of measure with ID {id} was not found." });
        return Ok(new { success = true, message = "Unit of measure deactivated/deleted successfully." });
    }

    // --- Configurable Master Lookups ---

    [HttpGet("lookups")]
    public async Task<IActionResult> GetMasterLookups(
        [FromQuery] string? type,
        [FromQuery] int? parentId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var lookups = await _inventoryService.GetMasterLookupsAsync(type, parentId, includeInactive, cancellationToken);
        return Ok(new { success = true, data = lookups });
    }

    [HttpPost("lookups")]
    public async Task<IActionResult> CreateMasterLookup([FromBody] SaveMasterLookupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateMasterLookupAsync(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("lookups/{id:int}")]
    public async Task<IActionResult> UpdateMasterLookup(int id, [FromBody] SaveMasterLookupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.UpdateMasterLookupAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("lookups/{id:int}")]
    public async Task<IActionResult> DeleteMasterLookup(int id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.DeleteMasterLookupAsync(id, cancellationToken);
        if (!result) return NotFound(new { success = false, message = $"Master lookup with ID {id} was not found." });
        return Ok(new { success = true, message = "Master lookup deactivated successfully." });
    }

    // --- Depots & Warehouses ---

    [HttpGet("depots")]
    public async Task<IActionResult> GetDepots([FromQuery] int? type, CancellationToken cancellationToken)
    {
        var depots = await _inventoryService.GetDepotsAsync(type, cancellationToken);
        return Ok(new { success = true, data = depots });
    }

    [HttpPost("depots")]
    public async Task<IActionResult> CreateDepot([FromBody] CreateDepotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateDepotAsync(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken cancellationToken)
    {
        var warehouses = await _inventoryService.GetWarehousesAsync(cancellationToken);
        return Ok(new { success = true, data = warehouses });
    }

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.CreateWarehouseAsync(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock([FromQuery] int? warehouseId, [FromQuery] int? productId, CancellationToken cancellationToken)
    {
        var stocks = await _inventoryService.GetProductStocksAsync(warehouseId, productId, cancellationToken);
        return Ok(new { success = true, data = stocks });
    }
}
