using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Common.Application.Abstractions.Payroll;

namespace ERP.Web.Api.Controllers;

[ApiController]
[Route("api/pr/reports")]
[Authorize]
public class PayrollReportsController : ControllerBase
{
    private readonly IPayrollReportingService _reportingService;

    public PayrollReportsController(IPayrollReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("payslip/{salarySheetId:int}/{employeeId:int}")]
    public async Task<IActionResult> GetPaySlip(int salarySheetId, int employeeId, CancellationToken cancellationToken)
    {
        var slip = await _reportingService.GetPaySlipAsync(salarySheetId, employeeId, cancellationToken);
        if (slip == null)
        {
            return NotFound(new { success = false, message = $"Pay slip for employee {employeeId} in salary sheet {salarySheetId} was not found." });
        }
        return Ok(new { success = true, data = slip });
    }

    [HttpGet("bank-advice/{salarySheetId:int}")]
    public async Task<IActionResult> GetBankAdvice(int salarySheetId, CancellationToken cancellationToken)
    {
        var advice = await _reportingService.GetBankAdviceAsync(salarySheetId, cancellationToken);
        if (advice == null)
        {
            return NotFound(new { success = false, message = $"Bank advice for salary sheet {salarySheetId} was not found." });
        }
        return Ok(new { success = true, data = advice });
    }
}
