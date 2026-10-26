namespace ERP.Common.Application.Abstractions.Common;

public interface ICurrentUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    int? CompanyId { get; }
    bool IsAuthenticated { get; }
    bool HasAccessToCompany(int companyId);
    int GetRequiredCompanyId() => CompanyId is > 0
        ? CompanyId.Value
        : throw new InvalidOperationException("An authorized active Company context is required for this operation.");
}
