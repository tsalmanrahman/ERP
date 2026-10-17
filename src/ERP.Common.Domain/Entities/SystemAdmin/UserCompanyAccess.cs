namespace ERP.Common.Domain.Entities.SystemAdmin;

public class UserCompanyAccess
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public UserCompanyAccess() { }
}
