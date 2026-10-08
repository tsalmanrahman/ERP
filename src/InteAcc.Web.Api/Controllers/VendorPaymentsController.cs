using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Models.ProcureToPay;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/ap/payments")]
[Authorize]
public class VendorPaymentsController : ControllerBase
{
    private readonly IVendorPaymentService _paymentService;

    public VendorPaymentsController(IVendorPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] int? vendorId, CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetVendorPaymentsAsync(vendorId, cancellationToken);
        return Ok(new { success = true, data = payments });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPaymentById(int id, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetVendorPaymentByIdAsync(id, cancellationToken);
        if (payment == null)
        {
            return NotFound(new { success = false, message = $"Vendor payment with ID {id} was not found." });
        }
        return Ok(new { success = true, data = payment });
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] CreateVendorPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _paymentService.ProcessVendorPaymentAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Vendor payment {payment.PaymentNumber} successfully disbursed and posted to General Ledger.", data = payment });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
