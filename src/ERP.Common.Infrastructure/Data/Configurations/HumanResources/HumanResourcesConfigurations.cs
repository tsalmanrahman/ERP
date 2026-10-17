using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.Common.Domain.Entities.HumanResources;

namespace ERP.Common.Infrastructure.Data.Configurations.HumanResources;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "hr");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DepartmentCode).HasMaxLength(20).IsRequired();
        builder.Property(d => d.DepartmentName).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => new { d.CompanyId, d.DepartmentCode }).IsUnique();
        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("Designations", "hr");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DesignationCode).HasMaxLength(20).IsRequired();
        builder.Property(d => d.DesignationTitle).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => new { d.CompanyId, d.DesignationCode }).IsUnique();
        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", "hr");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.BankName).HasMaxLength(100);
        builder.Property(e => e.BankAccountNumber).HasMaxLength(50);

        builder.Property(e => e.GrossSalary).HasPrecision(18, 4);
        builder.Property(e => e.BasicSalary).HasPrecision(18, 4);
        builder.Property(e => e.HouseRent).HasPrecision(18, 4);
        builder.Property(e => e.MedicalAllowance).HasPrecision(18, 4);
        builder.Property(e => e.ConveyanceAllowance).HasPrecision(18, 4);
        builder.Property(e => e.OtherAllowance).HasPrecision(18, 4);
        builder.Property(e => e.ProvidentFundDeduction).HasPrecision(18, 4);
        builder.Property(e => e.TaxDeduction).HasPrecision(18, 4);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Designation)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DesignationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.CompanyId, e.EmployeeCode }).IsUnique();
        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

public class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        builder.ToTable("AttendanceLogs", "hr");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EmployeeCode).HasMaxLength(30).IsRequired();
        builder.Property(a => a.DeviceId).HasMaxLength(50).IsRequired();
        builder.Property(a => a.InOutMode).HasMaxLength(10).IsRequired();
        builder.HasIndex(a => new { a.CompanyId, a.EmployeeCode, a.PunchTime });
    }
}

public class DailyAttendanceConfiguration : IEntityTypeConfiguration<DailyAttendance>
{
    public void Configure(EntityTypeBuilder<DailyAttendance> builder)
    {
        builder.ToTable("DailyAttendance", "hr");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.WorkHours).HasPrecision(5, 2);
        builder.Property(a => a.OvertimeHours).HasPrecision(5, 2);
        builder.Property(a => a.Status).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Remarks).HasMaxLength(250);

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.DailyAttendances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CompanyId, a.EmployeeId, a.AttendanceDate }).IsUnique();
        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods", "hr");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PeriodName).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ClosedBy).HasMaxLength(50);
        builder.HasIndex(p => new { p.CompanyId, p.Year, p.Month }).IsUnique();
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class SalarySheetConfiguration : IEntityTypeConfiguration<SalarySheet>
{
    public void Configure(EntityTypeBuilder<SalarySheet> builder)
    {
        builder.ToTable("SalarySheets", "hr");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SheetNumber).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired();

        builder.Property(s => s.TotalGrossSalary).HasPrecision(18, 4);
        builder.Property(s => s.TotalOvertimeAmount).HasPrecision(18, 4);
        builder.Property(s => s.TotalDeductions).HasPrecision(18, 4);
        builder.Property(s => s.TotalNetSalary).HasPrecision(18, 4);

        builder.HasOne(s => s.PayrollPeriod)
            .WithMany(p => p.SalarySheets)
            .HasForeignKey(s => s.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.SalarySheet)
            .HasForeignKey(l => l.SalarySheetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.CompanyId, s.PayrollPeriodId }).IsUnique();
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class SalarySheetLineConfiguration : IEntityTypeConfiguration<SalarySheetLine>
{
    public void Configure(EntityTypeBuilder<SalarySheetLine> builder)
    {
        builder.ToTable("SalarySheetLines", "hr");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BasicSalary).HasPrecision(18, 4);
        builder.Property(l => l.HouseRent).HasPrecision(18, 4);
        builder.Property(l => l.MedicalAllowance).HasPrecision(18, 4);
        builder.Property(l => l.ConveyanceAllowance).HasPrecision(18, 4);
        builder.Property(l => l.OtherAllowance).HasPrecision(18, 4);
        builder.Property(l => l.GrossSalary).HasPrecision(18, 4);

        builder.Property(l => l.OvertimeHours).HasPrecision(6, 2);
        builder.Property(l => l.OvertimeRate).HasPrecision(18, 4);
        builder.Property(l => l.OvertimeAmount).HasPrecision(18, 4);
        builder.Property(l => l.AbsentDeduction).HasPrecision(18, 4);
        builder.Property(l => l.TaxDeduction).HasPrecision(18, 4);
        builder.Property(l => l.ProvidentFundDeduction).HasPrecision(18, 4);
        builder.Property(l => l.TotalDeductions).HasPrecision(18, 4);
        builder.Property(l => l.NetSalaryPayable).HasPrecision(18, 4);

        builder.Property(l => l.PaymentMethod).HasMaxLength(20).IsRequired();
        builder.Property(l => l.BankName).HasMaxLength(100);
        builder.Property(l => l.BankAccountNumber).HasMaxLength(50);
        builder.Property(l => l.Remarks).HasMaxLength(250);

        builder.HasOne(l => l.Employee)
            .WithMany(e => e.SalarySheetLines)
            .HasForeignKey(l => l.EmployeeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
