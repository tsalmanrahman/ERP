using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Inventory;
using InteAcc.Common.Application.Models.Inventory;

namespace InteAcc.Web.Api.Controllers;

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
