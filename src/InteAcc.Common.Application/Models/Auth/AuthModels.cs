namespace InteAcc.Common.Application.Models.Auth;

public record LoginRequest(string Username, string Password, int? CompanyId = null);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes,
    UserProfileDto User
);

public record UserProfileDto(
    int Id,
    string UserCode,
    string Username,
    string Email,
    string FullName,
    int? ActiveCompanyId,
    List<string> Roles,
    List<string> Permissions,
    List<CompanyAccessDto> AccessibleCompanies
);

public record CompanyAccessDto(
    int Id,
    string CompanyCode,
    string CompanyName,
    string BaseCurrency,
    bool IsDefault
);

public record SwitchCompanyRequest(int TargetCompanyId);
