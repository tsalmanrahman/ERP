using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Data;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly IDatabaseHealthService _databaseHealthService;

    public DiagnosticsController(IDatabaseHealthService databaseHealthService)
    {
        _databaseHealthService = databaseHealthService;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            status = "Online",
            timestamp = DateTime.UtcNow,
            framework = ".NET 10 (C# 13)",
            system = "InteAcc Modern ERP"
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase(CancellationToken cancellationToken)
    {
        var result = await _databaseHealthService.CheckDatabaseHealthAsync(cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(503, new
            {
                success = false,
                error = result.Error
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Value
        });
    }
}
