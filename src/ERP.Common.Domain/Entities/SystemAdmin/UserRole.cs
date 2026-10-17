namespace ERP.Common.Domain.Entities.SystemAdmin;

public class UserRole
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public UserRole() { }
}
