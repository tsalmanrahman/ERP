using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.OrderToCash;
using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/ar/receipts")]
[Authorize]
public class CustomerReceiptsController : ControllerBase
{
    private readonly ICustomerReceiptService _receiptService;

    public CustomerReceiptsController(ICustomerReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReceipts([FromQuery] int? customerId, CancellationToken cancellationToken)
    {
        var receipts = await _receiptService.GetCustomerReceiptsAsync(customerId, cancellationToken);
        return Ok(new { success = true, data = receipts });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReceiptById(int id, CancellationToken cancellationToken)
    {
        var receipt = await _receiptService.GetCustomerReceiptByIdAsync(id, cancellationToken);
        if (receipt == null)
        {
            return NotFound(new { success = false, message = $"Customer receipt with ID {id} was not found." });
        }
        return Ok(new { success = true, data = receipt });
    }

    [HttpPost]
    public async Task<IActionResult> ProcessReceipt([FromBody] CreateCustomerReceiptRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await _receiptService.ProcessCustomerReceiptAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Customer receipt {receipt.ReceiptNumber} processed and posted to General Ledger.", data = receipt });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
