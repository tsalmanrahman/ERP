using InteAcc.Common.Domain.Entities.SystemAdmin;

namespace InteAcc.Common.Application.Abstractions.Identity;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, int? companyId, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
}
