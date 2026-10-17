using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/bc/[controller]")]
[Authorize]
public class BankingController : ControllerBase
{
    private readonly IBankingService _bankingService;

    public BankingController(IBankingService bankingService)
    {
        _bankingService = bankingService;
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts(CancellationToken cancellationToken)
    {
        var result = await _bankingService.GetBankAccountsAsync(cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpGet("cash-books")]
    public async Task<IActionResult> GetCashBooks(CancellationToken cancellationToken)
    {
        var result = await _bankingService.GetCashBooksAsync(cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost("contra-transfer")]
    public async Task<IActionResult> ProcessContraTransfer([FromBody] ContraTransferRequest request, CancellationToken cancellationToken)
    {
        var result = await _bankingService.ProcessContraTransferAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new { success = false, message = result.Error });
        }

        return Ok(new { success = true, data = result.Value });
    }
}
