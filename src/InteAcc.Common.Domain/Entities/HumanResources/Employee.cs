using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.HumanResources;

public class Employee : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int DesignationId { get; set; }
    public Designation? Designation { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime JoiningDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Salary Structure
    public decimal GrossSalary { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HouseRent { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal ProvidentFundDeduction { get; set; }
    public decimal TaxDeduction { get; set; }

    // Banking Details
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }

    public bool IsActive { get; set; } = true;

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<DailyAttendance> DailyAttendances { get; set; } = new List<DailyAttendance>();
    public ICollection<SalarySheetLine> SalarySheetLines { get; set; } = new List<SalarySheetLine>();

    public Employee() { }

    public void RecalculateGrossSalary()
    {
        GrossSalary = BasicSalary + HouseRent + MedicalAllowance + ConveyanceAllowance + OtherAllowance;
    }
}
