using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.HumanResources;

public enum SalarySheetStatus
{
    Draft = 0,
    Approved = 1,
    Disbursed = 2,
    Cancelled = 3
}

public class SalarySheet : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public int PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }
    public string SheetNumber { get; set; } = string.Empty;
    public DateTime ProcessingDate { get; set; }
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalOvertimeAmount { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Approved, Disbursed, Cancelled
    public int? AccrualJournalVoucherId { get; set; }
    public int? DisbursementJournalVoucherId { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<SalarySheetLine> Lines { get; set; } = new List<SalarySheetLine>();

    public SalarySheet() { }

    public void RecalculateTotals()
    {
        TotalGrossSalary = Lines.Sum(l => l.GrossSalary);
        TotalOvertimeAmount = Lines.Sum(l => l.OvertimeAmount);
        TotalDeductions = Lines.Sum(l => l.TotalDeductions);
        TotalNetSalary = Lines.Sum(l => l.NetSalaryPayable);
    }
}

public class SalarySheetLine : Entity<int>
{
    public int SalarySheetId { get; set; }
    public SalarySheet? SalarySheet { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal HouseRent { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
    public decimal GrossSalary { get; set; }

    public int WorkingDays { get; set; } = 26;
    public int PresentDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }

    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal OvertimeAmount { get; set; }

    public decimal AbsentDeduction { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal ProvidentFundDeduction { get; set; }
    public decimal TotalDeductions { get; set; }

    public decimal NetSalaryPayable { get; set; }

    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Remarks { get; set; }

    public SalarySheetLine() { }

    public void Calculate()
    {
        GrossSalary = BasicSalary + HouseRent + MedicalAllowance + ConveyanceAllowance + OtherAllowance;

        if (WorkingDays > 0)
        {
            // Bangladesh Labor Law formula for overtime rate: (BasicSalary / (WorkingDays * 8)) * 2
            OvertimeRate = Math.Round((BasicSalary / (WorkingDays * 8m)) * 2m, 4);
            OvertimeAmount = Math.Round(OvertimeHours * OvertimeRate, 2);

            // Absent deduction: (GrossSalary / WorkingDays) * AbsentDays
            AbsentDeduction = Math.Round((GrossSalary / WorkingDays) * AbsentDays, 2);
        }
        else
        {
            OvertimeRate = 0;
            OvertimeAmount = 0;
            AbsentDeduction = 0;
        }

        TotalDeductions = AbsentDeduction + TaxDeduction + ProvidentFundDeduction;
        NetSalaryPayable = (GrossSalary + OvertimeAmount) - TotalDeductions;
        if (NetSalaryPayable < 0) NetSalaryPayable = 0;
    }
}
