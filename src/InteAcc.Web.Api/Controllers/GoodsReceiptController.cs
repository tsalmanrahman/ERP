using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Inventory;
using InteAcc.Common.Application.Models.Inventory;

namespace InteAcc.Web.Api.Controllers;

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
}
