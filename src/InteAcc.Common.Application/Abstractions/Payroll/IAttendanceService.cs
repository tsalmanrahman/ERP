using InteAcc.Common.Application.Models.Payroll;

namespace InteAcc.Common.Application.Abstractions.Payroll;

public interface IAttendanceService
{
    Task<bool> LogPunchAsync(AttendancePunchRequest request, CancellationToken cancellationToken = default);
    Task<BiometricImportResultDto> ImportBiometricPunchesAsync(BiometricImportRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceReconciliationResultDto> ReconcileAttendanceAsync(ReconcileAttendanceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyAttendanceDto>> GetDailyAttendanceAsync(DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null, CancellationToken cancellationToken = default);
}
