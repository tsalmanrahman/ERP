using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/gl/[controller]")]
[Authorize]
public class JournalVouchersController : ControllerBase
{
    private readonly IJournalVoucherService _journalVoucherService;

    public JournalVouchersController(IJournalVoucherService journalVoucherService)
    {
        _journalVoucherService = journalVoucherService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateVoucher([FromBody] CreateJournalVoucherRequest request, CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.CreateVoucherAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return CreatedAtAction(nameof(GetVoucherById), new { id = result.Value.Id }, new { success = true, data = result.Value });
    }

    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> PostVoucher(int id, CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.PostVoucherAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetVouchers(
        [FromQuery] int? fiscalPeriodId,
        [FromQuery] bool? isPosted,
        CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.GetVouchersAsync(fiscalPeriodId, isPosted, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVoucherById(int id, CancellationToken cancellationToken)
    {
        var result = await _journalVoucherService.GetVoucherByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }
}
