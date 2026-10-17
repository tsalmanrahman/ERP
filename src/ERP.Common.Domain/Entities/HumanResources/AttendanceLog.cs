using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.HumanResources;

public class AttendanceLog : Entity<long>, IMultiTenant
{
    public int CompanyId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime PunchTime { get; set; }
    public string DeviceId { get; set; } = "TERMINAL-01";
    public string InOutMode { get; set; } = "PUNCH"; // IN, OUT, PUNCH
    public bool IsProcessed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public AttendanceLog() { }
}
