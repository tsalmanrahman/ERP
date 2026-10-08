using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.OrderToCash;
using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/ar/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices([FromQuery] int? customerId, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(customerId, cancellationToken);
        return Ok(new { success = true, data = invoices });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInvoiceById(int id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);
        if (invoice == null)
        {
            return NotFound(new { success = false, message = $"Sales invoice with ID {id} was not found." });
        }
        return Ok(new { success = true, data = invoice });
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, new { success = true, data = invoice });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmInvoice(int id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceService.ConfirmAndPostToGlAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"Sales invoice {invoice.InvoiceNumber} confirmed and posted to General Ledger.", data = invoice });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
