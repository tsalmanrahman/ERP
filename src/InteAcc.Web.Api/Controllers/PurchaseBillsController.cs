using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Models.ProcureToPay;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/ap/bills")]
[Authorize]
public class PurchaseBillsController : ControllerBase
{
    private readonly IPurchaseBillService _billService;

    public PurchaseBillsController(IPurchaseBillService billService)
    {
        _billService = billService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPurchaseBills([FromQuery] int? vendorId, CancellationToken cancellationToken)
    {
        var bills = await _billService.GetPurchaseBillsAsync(vendorId, cancellationToken);
        return Ok(new { success = true, data = bills });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPurchaseBillById(int id, CancellationToken cancellationToken)
    {
        var bill = await _billService.GetPurchaseBillByIdAsync(id, cancellationToken);
        if (bill == null)
        {
            return NotFound(new { success = false, message = $"Purchase bill with ID {id} was not found." });
        }
        return Ok(new { success = true, data = bill });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchaseBill([FromBody] CreatePurchaseBillRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var bill = await _billService.CreatePurchaseBillAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPurchaseBillById), new { id = bill.Id }, new { success = true, data = bill });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id:int}/three-way-match")]
    public async Task<IActionResult> PerformThreeWayMatch(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _billService.PerformThreeWayMatchAsync(id, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmAndPostBill(int id, CancellationToken cancellationToken)
    {
        try
        {
            var bill = await _billService.ConfirmAndPostBillAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"Purchase bill {bill.BillNumber} confirmed and posted to General Ledger.", data = bill });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
