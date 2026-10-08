using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/gl/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly InteAccDbContext _dbContext;

    public AccountsController(InteAccDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _dbContext.Accounts
            .Include(a => a.AccountGroup)
            .OrderBy(a => a.AccountCode)
            .Select(a => new
            {
                a.Id,
                a.AccountCode,
                a.AccountName,
                a.AccountGroupId,
                GroupName = a.AccountGroup != null ? a.AccountGroup.GroupName : string.Empty,
                Category = a.AccountGroup != null ? a.AccountGroup.AccountCategory.ToString() : string.Empty,
                a.AccountType,
                a.IsReconcilable,
                a.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = accounts });
    }
}
