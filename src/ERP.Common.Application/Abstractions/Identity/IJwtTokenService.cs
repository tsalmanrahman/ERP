using ERP.Common.Domain.Entities.SystemAdmin;

namespace ERP.Common.Application.Abstractions.Identity;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, int? companyId, IEnumerable<string> roles, IEnumerable<string> permissions, IEnumerable<int>? accessibleCompanyIds = null);
    string GenerateRefreshToken();
}
