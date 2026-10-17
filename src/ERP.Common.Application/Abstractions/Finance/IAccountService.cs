using ERP.Common.Application.Models.Finance;

namespace ERP.Common.Application.Abstractions.Finance;

/// <summary>
/// Chart-of-accounts maintenance. Business-rule violations raise <see cref="InvalidOperationException"/>;
/// unknown identifiers raise <see cref="KeyNotFoundException"/>.
/// </summary>
public interface IAccountService
{
    Task<List<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<List<AccountGroupDto>> GetAccountGroupsAsync(CancellationToken cancellationToken = default);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<string> DeleteAccountAsync(int id, CancellationToken cancellationToken = default);
    Task<string> RestoreAccountAsync(int id, CancellationToken cancellationToken = default);
}
