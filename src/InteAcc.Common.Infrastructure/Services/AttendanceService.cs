using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Payroll;
using InteAcc.Common.Application.Models.Payroll;
using InteAcc.Common.Domain.Entities.HumanResources;
using InteAcc.Common.Infrastructure.Data;
using System.Globalization;

namespace InteAcc.Common.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AttendanceService(InteAccDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<bool> LogPunchAsync(AttendancePunchRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.EmployeeCode.Trim();

        var emp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.EmployeeCode == code, cancellationToken);
        if (emp == null)
        {
            throw new InvalidOperationException($"Employee with code '{code}' does not exist.");
        }

        var punch = new AttendanceLog
        {
            CompanyId = companyId,
            EmployeeCode = code,
            PunchTime = request.PunchTime,
            DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? "TERMINAL-01" : request.DeviceId.Trim(),
            InOutMode = string.IsNullOrWhiteSpace(request.InOutMode) ? "PUNCH" : request.InOutMode.Trim().ToUpperInvariant(),
            IsProcessed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AttendanceLogs.Add(punch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BiometricImportResultDto> ImportBiometricPunchesAsync(BiometricImportRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (string.IsNullOrWhiteSpace(request.RawPunchData))
        {
            return new BiometricImportResultDto(0, 0, 0, "Raw punch data is empty.");
        }

        var lines = request.RawPunchData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        int parsedCount = 0;
        int importedCount = 0;
        int duplicateOrInvalid = 0;

        var existingEmployees = await _dbContext.Employees
            .Where(e => e.CompanyId == companyId)
            .Select(e => e.EmployeeCode)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);

        var existingPunches = await _dbContext.AttendanceLogs
            .Where(a => a.CompanyId == companyId)
            .Select(a => new { a.EmployeeCode, a.PunchTime })
            .ToHashSetAsync(cancellationToken);

        var newPunches = new List<AttendanceLog>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skip common headers
            if (line.StartsWith("Employee", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("EmpCode", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("UserID", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("CardNo", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parsedCount++;

            string empCode = string.Empty;
            DateTime punchTime = DateTime.MinValue;
            string deviceId = request.DeviceId ?? "TERMINAL-01";
            string inOutMode = "PUNCH";

            // Delimiter detection: comma or tab or space
            if (line.Contains(','))
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    empCode = parts[0].Trim();
                    if (DateTime.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                        DateTime.TryParse(parts[1].Trim(), out dt))
                    {
                        punchTime = dt;
                    }
                    if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                    {
                        deviceId = parts[2].Trim();
                    }
                    if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
                    {
                        inOutMode = parts[3].Trim().ToUpperInvariant();
                    }
                }
            }
            else if (line.Contains('\t'))
            {
                var parts = line.Split('\t');
                if (parts.Length >= 2)
                {
                    empCode = parts[0].Trim();
                    if (parts.Length >= 3 && parts[1].Contains('-') && parts[2].Contains(':'))
                    {
                        // format: EMP-001 \t 2026-10-01 \t 08:55:00
                        var combined = $"{parts[1].Trim()} {parts[2].Trim()}";
                        if (DateTime.TryParse(combined, out var dt)) punchTime = dt;
                    }
                    else if (DateTime.TryParse(parts[1].Trim(), out var dt))
                    {
                        punchTime = dt;
                    }
                }
            }
            else
            {
                // Space-separated: EMP-001 2026-10-01 08:55:00
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    empCode = parts[0].Trim();
                    var combined = $"{parts[1]} {parts[2]}";
                    if (DateTime.TryParse(combined, out var dt)) punchTime = dt;
                }
            }

            if (string.IsNullOrWhiteSpace(empCode) || punchTime == DateTime.MinValue)
            {
                duplicateOrInvalid++;
                continue;
            }

            if (!existingEmployees.Contains(empCode))
            {
                duplicateOrInvalid++;
                continue;
            }

            var punchKey = new { EmployeeCode = empCode, PunchTime = punchTime };
            if (existingPunches.Contains(punchKey) || newPunches.Any(p => p.EmployeeCode.Equals(empCode, StringComparison.OrdinalIgnoreCase) && p.PunchTime == punchTime))
            {
                duplicateOrInvalid++;
                continue;
            }

            newPunches.Add(new AttendanceLog
            {
                CompanyId = companyId,
                EmployeeCode = empCode,
                PunchTime = punchTime,
                DeviceId = deviceId,
                InOutMode = inOutMode,
                IsProcessed = false,
                CreatedAtUtc = DateTime.UtcNow
            });

            importedCount++;
        }

        if (newPunches.Count > 0)
        {
            _dbContext.AttendanceLogs.AddRange(newPunches);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BiometricImportResultDto(
            TotalPunchesParsed: parsedCount,
            PunchesImported: importedCount,
            DuplicateOrInvalidCount: duplicateOrInvalid,
            Message: $"Successfully imported {importedCount} biometric punch logs ({duplicateOrInvalid} invalid/duplicate)."
        );
    }

    public async Task<AttendanceReconciliationResultDto> ReconcileAttendanceAsync(ReconcileAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var shiftStart = request.StandardShiftStart ?? new TimeSpan(9, 0, 0); // 09:00:00
        var shiftEnd = request.StandardShiftEnd ?? new TimeSpan(18, 0, 0);   // 18:00:00
        var lateThreshold = shiftStart.Add(TimeSpan.FromMinutes(request.GracePeriodMinutes)); // e.g. 09:15:00

        var employees = await _dbContext.Employees
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .ToListAsync(cancellationToken);

        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;

        var punches = await _dbContext.AttendanceLogs
            .Where(a => a.CompanyId == companyId && a.PunchTime >= startDate && a.PunchTime < endDate.AddDays(1))
            .OrderBy(a => a.PunchTime)
            .ToListAsync(cancellationToken);

        var existingDaily = await _dbContext.DailyAttendance
            .Where(d => d.CompanyId == companyId && d.AttendanceDate >= startDate && d.AttendanceDate <= endDate)
            .ToListAsync(cancellationToken);

        int totalEmployees = employees.Count;
        int totalDaysReconciled = 0;
        int totalPresent = 0;
        int totalLate = 0;
        int totalAbsent = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            totalDaysReconciled++;
            var nextDate = date.AddDays(1);

            foreach (var emp in employees)
            {
                var empPunches = punches
                    .Where(p => p.EmployeeCode.Equals(emp.EmployeeCode, StringComparison.OrdinalIgnoreCase)
                                && p.PunchTime >= date && p.PunchTime < nextDate)
                    .OrderBy(p => p.PunchTime)
                    .ToList();

                var existing = existingDaily.FirstOrDefault(d => d.EmployeeId == emp.Id && d.AttendanceDate == date);

                if (empPunches.Count == 0)
                {
                    // Absent
                    totalAbsent++;
                    if (existing != null)
                    {
                        existing.Status = "Absent";
                        existing.InTime = null;
                        existing.OutTime = null;
                        existing.WorkHours = 0m;
                        existing.OvertimeHours = 0m;
                        existing.Remarks = "No punch recorded";
                    }
                    else
                    {
                        var daily = new DailyAttendance
                        {
                            CompanyId = companyId,
                            EmployeeId = emp.Id,
                            AttendanceDate = date,
                            InTime = null,
                            OutTime = null,
                            WorkHours = 0m,
                            OvertimeHours = 0m,
                            Status = "Absent",
                            Remarks = "No punch recorded"
                        };
                        _dbContext.DailyAttendance.Add(daily);
                    }
                }
                else
                {
                    var firstPunch = empPunches.First().PunchTime.TimeOfDay;
                    var lastPunch = empPunches.Last().PunchTime.TimeOfDay;

                    TimeSpan inTime = firstPunch;
                    TimeSpan outTime = lastPunch;
                    decimal workHours = 0m;
                    decimal overtimeHours = 0m;
                    string status = "Present";

                    if (empPunches.Count > 1 && outTime > inTime)
                    {
                        workHours = Math.Round((decimal)(outTime - inTime).TotalHours, 2);
                    }
                    else
                    {
                        // Single punch: standard 8 hours
                        workHours = 8.0m;
                        outTime = inTime.Add(TimeSpan.FromHours(8));
                    }

                    // Check late
                    if (inTime > lateThreshold)
                    {
                        status = "Late";
                        totalLate++;
                    }
                    else
                    {
                        status = "Present";
                        totalPresent++;
                    }

                    // Check overtime (beyond 18:00)
                    if (outTime > shiftEnd)
                    {
                        overtimeHours = Math.Max(0m, Math.Round((decimal)(outTime - shiftEnd).TotalHours, 2));
                    }

                    if (existing != null)
                    {
                        existing.Status = status;
                        existing.InTime = inTime;
                        existing.OutTime = outTime;
                        existing.WorkHours = workHours;
                        existing.OvertimeHours = overtimeHours;
                        existing.Remarks = $"Punches: {empPunches.Count}";
                    }
                    else
                    {
                        var daily = new DailyAttendance
                        {
                            CompanyId = companyId,
                            EmployeeId = emp.Id,
                            AttendanceDate = date,
                            InTime = inTime,
                            OutTime = outTime,
                            WorkHours = workHours,
                            OvertimeHours = overtimeHours,
                            Status = status,
                            Remarks = $"Punches: {empPunches.Count}"
                        };
                        _dbContext.DailyAttendance.Add(daily);
                    }
                }
            }
        }

        // Mark punches as processed
        foreach (var p in punches)
        {
            p.IsProcessed = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AttendanceReconciliationResultDto(
            TotalEmployeesProcessed: totalEmployees,
            TotalDaysReconciled: totalDaysReconciled,
            TotalPresent: totalPresent,
            TotalLate: totalLate,
            TotalAbsent: totalAbsent
        );
    }

    public async Task<IReadOnlyList<DailyAttendanceDto>> GetDailyAttendanceAsync(DateTime? startDate = null, DateTime? endDate = null, int? employeeId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from d in _dbContext.DailyAttendance.AsNoTracking()
                    where d.CompanyId == companyId
                    join e in _dbContext.Employees.AsNoTracking() on d.EmployeeId equals e.Id
                    join dept in _dbContext.Departments.AsNoTracking() on e.DepartmentId equals dept.Id into deptJoin
                    from dept in deptJoin.DefaultIfEmpty()
                    select new { d, e, DeptName = dept != null ? dept.DepartmentName : string.Empty };

        if (startDate.HasValue)
        {
            var s = startDate.Value.Date;
            query = query.Where(x => x.d.AttendanceDate >= s);
        }

        if (endDate.HasValue)
        {
            var e = endDate.Value.Date;
            query = query.Where(x => x.d.AttendanceDate <= e);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(x => x.d.EmployeeId == employeeId.Value);
        }

        var list = await query.OrderByDescending(x => x.d.AttendanceDate).ThenBy(x => x.e.EmployeeCode).ToListAsync(cancellationToken);

        return list.Select(x => new DailyAttendanceDto(
            x.d.Id,
            x.d.EmployeeId,
            x.e.EmployeeCode,
            x.e.FullName,
            x.DeptName,
            x.d.AttendanceDate,
            x.d.InTime,
            x.d.OutTime,
            x.d.WorkHours,
            x.d.OvertimeHours,
            x.d.Status,
            x.d.Remarks
        )).ToList();
    }
}
