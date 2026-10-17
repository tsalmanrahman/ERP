using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.OrderToCash;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/sales/orders")]
[Authorize]
public class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public SalesOrdersController(ISalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSalesOrders([FromQuery] int? customerId, CancellationToken cancellationToken)
    {
        var orders = await _salesOrderService.GetSalesOrdersAsync(customerId, cancellationToken);
        return Ok(new { success = true, data = orders });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSalesOrderById(int id, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetSalesOrderByIdAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound(new { success = false, message = $"Sales order with ID {id} was not found." });
        }
        return Ok(new { success = true, data = order });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderService.CreateSalesOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSalesOrderById), new { id = order.Id }, new { success = true, data = order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmSalesOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderService.ConfirmSalesOrderAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"Sales order {order.OrderNumber} confirmed.", data = order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("centers")]
    public async Task<IActionResult> GetSalesCenters(CancellationToken cancellationToken)
    {
        var centers = await _salesOrderService.GetSalesCentersAsync(cancellationToken);
        return Ok(new { success = true, data = centers });
    }

    [HttpPost("centers")]
    public async Task<IActionResult> CreateSalesCenter([FromBody] CreateSalesCenterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var center = await _salesOrderService.CreateSalesCenterAsync(request, cancellationToken);
            return Ok(new { success = true, data = center });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
