using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/gl/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAccountsAsync(cancellationToken);
        return Ok(new { success = true, data = accounts });
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetAccountGroups(CancellationToken cancellationToken)
    {
        var groups = await _accountService.GetAccountGroupsAsync(cancellationToken);
        return Ok(new { success = true, data = groups });
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var account = await _accountService.CreateAccountAsync(request, cancellationToken);
            return Ok(new
            {
                success = true,
                message = $"Account '{account.AccountCode} - {account.AccountName}' created successfully.",
                data = account
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAccount(int id, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _accountService.DeleteAccountAsync(id, cancellationToken);
            return Ok(new { success = true, message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreAccount(int id, CancellationToken cancellationToken)
    {
        try
        {
            var message = await _accountService.RestoreAccountAsync(id, cancellationToken);
            return Ok(new { success = true, message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
