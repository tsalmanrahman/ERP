using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.HumanResources;

public enum AttendanceStatus
{
    Present = 1,
    Late = 2,
    Absent = 3,
    HalfDay = 4,
    Leave = 5
}

public class DailyAttendance : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime AttendanceDate { get; set; }
    public TimeSpan? InTime { get; set; }
    public TimeSpan? OutTime { get; set; }
    public decimal WorkHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string Status { get; set; } = "Present"; // Present, Late, Absent, HalfDay, Leave
    public string? Remarks { get; set; }

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DailyAttendance() { }
}
