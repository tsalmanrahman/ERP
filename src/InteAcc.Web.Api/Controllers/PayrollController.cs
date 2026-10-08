using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Payroll;
using InteAcc.Common.Application.Models.Payroll;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/pr/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods(CancellationToken cancellationToken)
    {
        var periods = await _payrollService.GetPayrollPeriodsAsync(cancellationToken);
        return Ok(new { success = true, data = periods });
    }

    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] CreatePayrollPeriodRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var period = await _payrollService.CreatePayrollPeriodAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Payroll period '{period.PeriodName}' created successfully.", data = period });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateSalarySheet([FromBody] GenerateSalarySheetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sheet = await _payrollService.GenerateSalarySheetAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Salary sheet {sheet.SheetNumber} generated successfully.", data = sheet });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("sheets/{id:int}")]
    public async Task<IActionResult> GetSalarySheetById(int id, CancellationToken cancellationToken)
    {
        var sheet = await _payrollService.GetSalarySheetByIdAsync(id, cancellationToken);
        if (sheet == null)
        {
            return NotFound(new { success = false, message = $"Salary sheet with ID {id} was not found." });
        }
        return Ok(new { success = true, data = sheet });
    }

    [HttpPost("sheets/{id:int}/approve")]
    public async Task<IActionResult> ApproveSalarySheet(int id, CancellationToken cancellationToken)
    {
        try
        {
            var sheet = await _payrollService.ApproveSalarySheetAsync(id, cancellationToken);
            return Ok(new { success = true, message = $"Salary sheet {sheet.SheetNumber} approved and GL accrual posted successfully.", data = sheet });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("sheets/disburse")]
    public async Task<IActionResult> DisburseSalarySheet([FromBody] DisburseSalaryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var sheet = await _payrollService.DisburseSalarySheetAsync(request, cancellationToken);
            return Ok(new { success = true, message = $"Salary sheet {sheet.SheetNumber} disbursed via bank and GL payment posted successfully.", data = sheet });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
