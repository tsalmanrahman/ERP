namespace ERP.Common.Application.Models.Payroll;

public record DepartmentDto(
    int Id,
    int CompanyId,
    string DepartmentCode,
    string DepartmentName,
    bool IsActive
);

public record CreateDepartmentRequest(
    string DepartmentCode,
    string DepartmentName
);

public record DesignationDto(
    int Id,
    int CompanyId,
    string DesignationCode,
    string DesignationTitle,
    bool IsActive
);

public record CreateDesignationRequest(
    string DesignationCode,
    string DesignationTitle
);

public record EmployeeDto(
    int Id,
    int CompanyId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    int DepartmentId,
    string DepartmentName,
    int DesignationId,
    string DesignationTitle,
    DateTime? DateOfBirth,
    DateTime JoiningDate,
    string? Email,
    string? Phone,
    decimal GrossSalary,
    decimal BasicSalary,
    decimal HouseRent,
    decimal MedicalAllowance,
    decimal ConveyanceAllowance,
    decimal OtherAllowance,
    decimal ProvidentFundDeduction,
    decimal TaxDeduction,
    string? BankName,
    string? BankAccountNumber,
    bool IsActive
);

public record CreateEmployeeRequest(
    string EmployeeCode,
    string FirstName,
    string LastName,
    int DepartmentId,
    int DesignationId,
    DateTime? DateOfBirth,
    DateTime JoiningDate,
    string? Email,
    string? Phone,
    decimal BasicSalary,
    decimal HouseRent,
    decimal MedicalAllowance,
    decimal ConveyanceAllowance,
    decimal OtherAllowance = 0,
    decimal ProvidentFundDeduction = 0,
    decimal TaxDeduction = 0,
    string? BankName = null,
    string? BankAccountNumber = null
);

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    int DepartmentId,
    int DesignationId,
    DateTime? DateOfBirth,
    DateTime JoiningDate,
    string? Email,
    string? Phone,
    decimal BasicSalary,
    decimal HouseRent,
    decimal MedicalAllowance,
    decimal ConveyanceAllowance,
    decimal OtherAllowance,
    decimal ProvidentFundDeduction,
    decimal TaxDeduction,
    string? BankName,
    string? BankAccountNumber,
    bool IsActive
);

public record AttendancePunchRequest(
    string EmployeeCode,
    DateTime PunchTime,
    string DeviceId = "TERMINAL-01",
    string InOutMode = "PUNCH"
);

public record BiometricImportRequest(
    string RawPunchData,
    string DeviceId = "TERMINAL-01"
);

public record BiometricImportResultDto(
    int TotalPunchesParsed,
    int PunchesImported,
    int DuplicateOrInvalidCount,
    string Message
);

public record DailyAttendanceDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DepartmentName,
    DateTime AttendanceDate,
    TimeSpan? InTime,
    TimeSpan? OutTime,
    decimal WorkHours,
    decimal OvertimeHours,
    string Status,
    string? Remarks
);

public record ReconcileAttendanceRequest(
    DateTime StartDate,
    DateTime EndDate,
    TimeSpan? StandardShiftStart = null,
    TimeSpan? StandardShiftEnd = null,
    int GracePeriodMinutes = 15
);

public record AttendanceReconciliationResultDto(
    int TotalEmployeesProcessed,
    int TotalDaysReconciled,
    int TotalPresent,
    int TotalLate,
    int TotalAbsent
);

public record PayrollPeriodDto(
    int Id,
    int CompanyId,
    string PeriodName,
    int Year,
    int Month,
    DateTime StartDate,
    DateTime EndDate,
    int WorkingDays,
    bool IsClosed
);

public record CreatePayrollPeriodRequest(
    string PeriodName,
    int Year,
    int Month,
    DateTime StartDate,
    DateTime EndDate,
    int WorkingDays = 26
);

public record SalarySheetDto(
    int Id,
    int CompanyId,
    int PayrollPeriodId,
    string PeriodName,
    string SheetNumber,
    DateTime ProcessingDate,
    decimal TotalGrossSalary,
    decimal TotalOvertimeAmount,
    decimal TotalDeductions,
    decimal TotalNetSalary,
    string Status,
    int? AccrualJournalVoucherId,
    string? AccrualJournalVoucherNumber,
    int? DisbursementJournalVoucherId,
    string? DisbursementJournalVoucherNumber,
    IReadOnlyList<SalarySheetLineDto> Lines
);

public record SalarySheetLineDto(
    int Id,
    int SalarySheetId,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DesignationTitle,
    string DepartmentName,
    decimal BasicSalary,
    decimal HouseRent,
    decimal MedicalAllowance,
    decimal ConveyanceAllowance,
    decimal OtherAllowance,
    decimal GrossSalary,
    int WorkingDays,
    int PresentDays,
    int LateDays,
    int AbsentDays,
    decimal OvertimeHours,
    decimal OvertimeRate,
    decimal OvertimeAmount,
    decimal AbsentDeduction,
    decimal TaxDeduction,
    decimal ProvidentFundDeduction,
    decimal TotalDeductions,
    decimal NetSalaryPayable,
    string PaymentMethod,
    string? BankName,
    string? BankAccountNumber,
    string? Remarks
);

public record GenerateSalarySheetRequest(
    int PayrollPeriodId,
    DateTime? ProcessingDate = null
);

public record DisburseSalaryRequest(
    int SalarySheetId,
    int? BankAccountId = null,
    DateTime? DisbursementDate = null,
    string? ChequeOrReferenceNumber = null
);

public record PaySlipDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string DesignationTitle,
    string DepartmentName,
    DateTime JoiningDate,
    string? BankName,
    string? BankAccountNumber,
    string PeriodName,
    int Year,
    int Month,
    DateTime ProcessingDate,
    int WorkingDays,
    int PresentDays,
    int LateDays,
    int AbsentDays,
    decimal OvertimeHours,
    decimal BasicSalary,
    decimal HouseRent,
    decimal MedicalAllowance,
    decimal ConveyanceAllowance,
    decimal OtherAllowance,
    decimal OvertimeRate,
    decimal OvertimeAmount,
    decimal TotalGrossEarnings,
    decimal AbsentDeduction,
    decimal TaxDeduction,
    decimal ProvidentFundDeduction,
    decimal TotalDeductions,
    decimal NetSalaryPayable
);

public record BankAdviceItemDto(
    string EmployeeCode,
    string EmployeeName,
    string? BankName,
    string? BankAccountNumber,
    decimal NetSalaryPayable
);

public record BankAdviceDto(
    string PeriodName,
    DateTime ProcessingDate,
    string SheetNumber,
    decimal TotalAmount,
    int TotalEmployees,
    IReadOnlyList<BankAdviceItemDto> Items
);
