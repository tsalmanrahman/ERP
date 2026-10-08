using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InteAcc.Common.Application.Abstractions.Payroll;
using InteAcc.Common.Application.Models.Payroll;

namespace InteAcc.Web.Api.Controllers;

[ApiController]
[Route("api/hr/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("punch")]
    public async Task<IActionResult> LogPunch([FromBody] AttendancePunchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _attendanceService.LogPunchAsync(request, cancellationToken);
            return Ok(new { success, message = $"Punch registered for employee {request.EmployeeCode} at {request.PunchTime:yyyy-MM-dd HH:mm:ss}." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("import-biometric")]
    public async Task<IActionResult> ImportBiometric([FromBody] BiometricImportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.ImportBiometricPunchesAsync(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("reconcile")]
    public async Task<IActionResult> ReconcileAttendance([FromBody] ReconcileAttendanceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendanceService.ReconcileAttendanceAsync(request, cancellationToken);
            return Ok(new { success = true, message = "Attendance reconciliation completed.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyAttendance(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? employeeId,
        CancellationToken cancellationToken)
    {
        var records = await _attendanceService.GetDailyAttendanceAsync(startDate, endDate, employeeId, cancellationToken);
        return Ok(new { success = true, data = records });
    }
}
