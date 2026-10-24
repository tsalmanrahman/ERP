using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Inventory;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/inv/[controller]")]
[Authorize]
public class GoodsReceiptController : ControllerBase
{
    private readonly IGoodsReceiptService _goodsReceiptService;

    public GoodsReceiptController(IGoodsReceiptService goodsReceiptService)
    {
        _goodsReceiptService = goodsReceiptService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGrns([FromQuery] int? warehouseId, CancellationToken cancellationToken)
    {
        var grns = await _goodsReceiptService.GetGoodsReceiptNotesAsync(warehouseId, cancellationToken);
        return Ok(new { success = true, data = grns });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetGrnById(int id, CancellationToken cancellationToken)
    {
        var grn = await _goodsReceiptService.GetGoodsReceiptNoteByIdAsync(id, cancellationToken);
        if (grn == null)
        {
            return NotFound(new { success = false, message = $"GRN with ID {id} was not found." });
        }
        return Ok(new { success = true, data = grn });
    }

    [HttpGet("eligible-pos")]
    public async Task<IActionResult> GetEligiblePurchaseOrders(CancellationToken cancellationToken)
    {
        var pos = await _goodsReceiptService.GetEligiblePurchaseOrdersAsync(cancellationToken);
        return Ok(new { success = true, data = pos });
    }

    [HttpGet("eligible-mos")]
    public async Task<IActionResult> GetEligibleProductionOrders(CancellationToken cancellationToken)
    {
        var mos = await _goodsReceiptService.GetEligibleProductionOrdersAsync(cancellationToken);
        return Ok(new { success = true, data = mos });
    }

    [HttpPost]
    public async Task<IActionResult> CreateGrn([FromBody] CreateGrnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptService.CreateGoodsReceiptNoteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetGrnById), new { id = grn.Id }, new { success = true, data = grn });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/receive")]
    public async Task<IActionResult> ReceiveAndPostGrn(int id, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _goodsReceiptService.ReceiveAndPostGrnAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"GRN {grn.GrnNumber} successfully received and posted to General Ledger.", data = grn });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("returns")]
    public async Task<IActionResult> CreateGrnReturn([FromBody] CreateGrnReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var grnReturn = await _goodsReceiptService.CreateGoodsReceiptReturnAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"GRN Return {grnReturn.ReturnNumber} created successfully.", data = grnReturn });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("returns/{id:int}/post")]
    public async Task<IActionResult> PostGrnReturn(int id, CancellationToken cancellationToken)
    {
        try
        {
            var grnReturn = await _goodsReceiptService.PostGoodsReceiptReturnAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"GRN Return {grnReturn.ReturnNumber} successfully posted and inventory updated.", data = grnReturn });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
