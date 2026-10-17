using ERP.Common.Application.Models.Auth;
using ERP.Common.Domain.Common;

namespace ERP.Common.Application.Abstractions.Identity;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> GetCurrentUserProfileAsync(int userId, int? activeCompanyId, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> SwitchCompanyAsync(int userId, int targetCompanyId, CancellationToken cancellationToken = default);
}
