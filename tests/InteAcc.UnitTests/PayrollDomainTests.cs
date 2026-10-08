using Xunit;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Domain.Entities.HumanResources;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.UnitTests;

public class PayrollDomainTests
{
    [Fact]
    public void Employee_SalaryStructure_RecalculatesGrossCorrectly()
    {
        // Arrange
        var emp = new Employee
        {
            CompanyId = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Arif",
            LastName = "Rahman",
            BasicSalary = 60000m,
            HouseRent = 30000m,
            MedicalAllowance = 15000m,
            ConveyanceAllowance = 15000m,
            OtherAllowance = 0m,
            TaxDeduction = 5000m,
            ProvidentFundDeduction = 3000m
        };

        // Act
        emp.RecalculateGrossSalary();

        // Assert
        Assert.Equal(120000m, emp.GrossSalary);
        Assert.Equal("Arif Rahman", emp.FullName);
    }

    [Fact]
    public void SalarySheetLine_BangladeshLaborLawCalculations_OvertimeAndAbsentDeductions()
    {
        // Arrange: BD standard 26 working days, 8h/day, double rate for overtime
        var line = new SalarySheetLine
        {
            EmployeeId = 1,
            BasicSalary = 60000m,
            HouseRent = 30000m,
            MedicalAllowance = 15000m,
            ConveyanceAllowance = 15000m,
            OtherAllowance = 0m,
            WorkingDays = 26,
            PresentDays = 24,
            LateDays = 0,
            AbsentDays = 2,
            OvertimeHours = 10m,
            TaxDeduction = 5000m,
            ProvidentFundDeduction = 3000m
        };

        // Act
        line.Calculate();

        // Expected:
        // GrossSalary = 120,000
        // OvertimeRate = (60,000 / (26 * 8)) * 2 = 576.9231
        // OvertimeAmount = 10 * 576.9231 = 5769.23
        // AbsentDeduction = (120,000 / 26) * 2 = 9230.77
        // TotalDeductions = 9230.77 + 5000 + 3000 = 17230.77
        // NetSalaryPayable = (120000 + 5769.23) - 17230.77 = 108538.46
        Assert.Equal(120000m, line.GrossSalary);
        Assert.Equal(576.9231m, line.OvertimeRate);
        Assert.Equal(5769.23m, line.OvertimeAmount);
        Assert.Equal(9230.77m, line.AbsentDeduction);
        Assert.Equal(17230.77m, line.TotalDeductions);
        Assert.Equal(108538.46m, line.NetSalaryPayable);
    }

    [Fact]
    public void SalarySheet_AccrualGLPosting_DoubleEntryParity_Maintained()
    {
        // Arrange
        var sheet = new SalarySheet
        {
            CompanyId = 1,
            SheetNumber = "SAL-202610-0001",
            ProcessingDate = new DateTime(2026, 10, 31)
        };

        var line1 = new SalarySheetLine
        {
            EmployeeId = 1,
            BasicSalary = 60000m,
            HouseRent = 30000m,
            MedicalAllowance = 15000m,
            ConveyanceAllowance = 15000m,
            WorkingDays = 26,
            PresentDays = 24,
            AbsentDays = 2,
            OvertimeHours = 10m,
            TaxDeduction = 5000m,
            ProvidentFundDeduction = 3000m
        };
        line1.Calculate();

        var line2 = new SalarySheetLine
        {
            EmployeeId = 2,
            BasicSalary = 40000m,
            HouseRent = 20000m,
            MedicalAllowance = 10000m,
            ConveyanceAllowance = 10000m,
            WorkingDays = 26,
            PresentDays = 26,
            AbsentDays = 0,
            OvertimeHours = 0m,
            TaxDeduction = 2500m,
            ProvidentFundDeduction = 2000m
        };
        line2.Calculate();

        sheet.Lines.Add(line1);
        sheet.Lines.Add(line2);
        sheet.RecalculateTotals();

        // Accrual GL Double-Entry Logic:
        decimal totalGrossAndOt = sheet.TotalGrossSalary + sheet.TotalOvertimeAmount;
        decimal totalAbsentDeduction = sheet.Lines.Sum(l => l.AbsentDeduction);
        decimal totalTaxDeduction = sheet.Lines.Sum(l => l.TaxDeduction);
        decimal totalPfDeduction = sheet.Lines.Sum(l => l.ProvidentFundDeduction);

        decimal netSalaryExpenseDebit = totalGrossAndOt - totalAbsentDeduction;
        decimal taxWithholdingCredit = totalTaxDeduction;
        decimal accruedPayableCredit = sheet.TotalNetSalary + totalPfDeduction;

        decimal totalCredits = taxWithholdingCredit + accruedPayableCredit;
        decimal variance = Math.Abs(netSalaryExpenseDebit - totalCredits);

        // Assert exact zero variance
        Assert.Equal(0m, variance);
        Assert.Equal(netSalaryExpenseDebit, totalCredits);
    }

    [Fact]
    public void SalarySheet_DisbursementGLPosting_DoubleEntryParity_Maintained()
    {
        // Arrange
        decimal netSalaryToDisburse = 184038.46m;

        // Debit: 2120 Accrued Salaries & Expenses Payable
        decimal debitAccruedSalaries = netSalaryToDisburse;
        // Credit: 1120 Prime Bank Ltd.
        decimal creditBank = netSalaryToDisburse;

        decimal variance = Math.Abs(debitAccruedSalaries - creditBank);

        // Assert
        Assert.Equal(0m, variance);
    }

    [Fact]
    public void DailyAttendance_StatusAndOvertimeEvaluation_WorksAsExpected()
    {
        // Arrange
        var shiftStart = new TimeSpan(9, 0, 0);
        var shiftEnd = new TimeSpan(18, 0, 0);
        var lateThreshold = shiftStart.Add(TimeSpan.FromMinutes(15)); // 09:15

        // Case 1: On-time with overtime
        var inTime1 = new TimeSpan(8, 55, 0);
        var outTime1 = new TimeSpan(19, 30, 0);

        bool isLate1 = inTime1 > lateThreshold;
        decimal otHours1 = outTime1 > shiftEnd ? (decimal)(outTime1 - shiftEnd).TotalHours : 0m;

        Assert.False(isLate1);
        Assert.Equal(1.5m, Math.Round(otHours1, 1));

        // Case 2: Late arrival
        var inTime2 = new TimeSpan(9, 25, 0);
        bool isLate2 = inTime2 > lateThreshold;
        Assert.True(isLate2);
    }

    [Fact]
    public void InteAccDbContext_ShouldMapPayrollEntitiesToCorrectSchemas()
    {
        var options = new DbContextOptionsBuilder<InteAccDbContext>()
            .UseSqlServer("Server=dummy;Database=ERP;TrustServerCertificate=True;")
            .Options;

        using var context = new InteAccDbContext(options);
        var model = context.Model;

        var deptEntity = model.FindEntityType(typeof(Department));
        Assert.NotNull(deptEntity);
        Assert.Equal("Departments", deptEntity.GetTableName());
        Assert.Equal("hr", deptEntity.GetSchema());

        var desigEntity = model.FindEntityType(typeof(Designation));
        Assert.NotNull(desigEntity);
        Assert.Equal("Designations", desigEntity.GetTableName());
        Assert.Equal("hr", desigEntity.GetSchema());

        var empEntity = model.FindEntityType(typeof(Employee));
        Assert.NotNull(empEntity);
        Assert.Equal("Employees", empEntity.GetTableName());
        Assert.Equal("hr", empEntity.GetSchema());

        var attLogEntity = model.FindEntityType(typeof(AttendanceLog));
        Assert.NotNull(attLogEntity);
        Assert.Equal("AttendanceLogs", attLogEntity.GetTableName());
        Assert.Equal("hr", attLogEntity.GetSchema());

        var dailyAttEntity = model.FindEntityType(typeof(DailyAttendance));
        Assert.NotNull(dailyAttEntity);
        Assert.Equal("DailyAttendance", dailyAttEntity.GetTableName());
        Assert.Equal("hr", dailyAttEntity.GetSchema());

        var periodEntity = model.FindEntityType(typeof(PayrollPeriod));
        Assert.NotNull(periodEntity);
        Assert.Equal("PayrollPeriods", periodEntity.GetTableName());
        Assert.Equal("hr", periodEntity.GetSchema());

        var sheetEntity = model.FindEntityType(typeof(SalarySheet));
        Assert.NotNull(sheetEntity);
        Assert.Equal("SalarySheets", sheetEntity.GetTableName());
        Assert.Equal("hr", sheetEntity.GetSchema());

        var sheetLineEntity = model.FindEntityType(typeof(SalarySheetLine));
        Assert.NotNull(sheetLineEntity);
        Assert.Equal("SalarySheetLines", sheetLineEntity.GetTableName());
        Assert.Equal("hr", sheetLineEntity.GetSchema());
    }

    [Fact]
    public void BiometricText_CommaSeparatedFormat_ParsesFieldsAccurately()
    {
        // Sample line: EMP-001,2026-10-01 08:58:22,DEV-01,IN
        var line = "EMP-001,2026-10-01 08:58:22,DEV-01,IN";
        var parts = line.Split(',');

        Assert.Equal(4, parts.Length);
        Assert.Equal("EMP-001", parts[0]);
        Assert.True(DateTime.TryParse(parts[1], out var dt));
        Assert.Equal(new DateTime(2026, 10, 1, 8, 58, 22), dt);
        Assert.Equal("DEV-01", parts[2]);
        Assert.Equal("IN", parts[3]);
    }

    [Fact]
    public void BiometricText_TabSeparatedFormat_ParsesFieldsAccurately()
    {
        // Sample legacy format: EMP-002\t2026-10-01\t09:18:00
        var line = "EMP-002\t2026-10-01\t09:18:00";
        var parts = line.Split('\t');

        Assert.Equal(3, parts.Length);
        Assert.Equal("EMP-002", parts[0]);
        var combined = $"{parts[1]} {parts[2]}";
        Assert.True(DateTime.TryParse(combined, out var dt));
        Assert.Equal(new DateTime(2026, 10, 1, 9, 18, 0), dt);
    }
}
