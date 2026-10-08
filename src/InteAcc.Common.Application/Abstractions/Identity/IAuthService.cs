using InteAcc.Common.Application.Models.Auth;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Application.Abstractions.Identity;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> GetCurrentUserProfileAsync(int userId, int? activeCompanyId, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> SwitchCompanyAsync(int userId, int targetCompanyId, CancellationToken cancellationToken = default);
}
