using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.SystemAdmin;

public class Company : Entity<int>, IAuditableEntity, ISoftDeletable
{
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "BGD";
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public string? WebAddress { get; set; }
    public string BaseCurrency { get; set; } = "BDT";
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

    public Company() { }

    public Company(int id, string companyCode, string companyName, string baseCurrency = "BDT")
        : base(id)
    {
        CompanyCode = companyCode;
        CompanyName = companyName;
        BaseCurrency = baseCurrency;
    }
}
