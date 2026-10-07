namespace InteAcc.Common.Application.Abstractions.Common;

public interface ICurrentUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    int? CompanyId { get; }
    bool IsAuthenticated { get; }
}
