using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Finance;

public class AccountService : IAccountService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AccountService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<List<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Accounts
            .OrderBy(a => a.AccountCode)
            .Select(a => new AccountDto(
                a.Id,
                a.AccountCode,
                a.AccountName,
                a.AccountGroupId,
                a.AccountGroup != null ? a.AccountGroup.GroupName : string.Empty,
                a.AccountGroup != null ? a.AccountGroup.AccountCategory.ToString() : string.Empty,
                a.AccountType,
                a.IsReconcilable,
                a.IsActive))
            .ToListAsync(cancellationToken);

    public Task<List<AccountGroupDto>> GetAccountGroupsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.AccountGroups
            .OrderBy(g => g.GroupCode)
            .Select(g => new AccountGroupDto(g.Id, g.GroupCode, g.GroupName, g.AccountCategory.ToString()))
            .ToListAsync(cancellationToken);

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccountCode) || string.IsNullOrWhiteSpace(request.AccountName))
        {
            throw new InvalidOperationException("Account Code and Account Name are required.");
        }

        var companyId = _currentUserContext.CompanyId
            ?? throw new InvalidOperationException("An active company context is required to create an account.");

        var code = request.AccountCode.Trim();
        var exists = await _dbContext.Accounts
            .AnyAsync(a => a.CompanyId == companyId && a.AccountCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Account with code '{request.AccountCode}' already exists.");
        }

        var account = new Account
        {
            CompanyId = companyId,
            AccountCode = code,
            AccountName = request.AccountName.Trim(),
            AccountGroupId = request.AccountGroupId > 0 ? request.AccountGroupId : 1,
            AccountType = request.AccountType > 0 ? request.AccountType : 2,
            IsReconcilable = request.IsReconcilable,
            IsActive = request.IsActive
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AccountDto(
            account.Id,
            account.AccountCode,
            account.AccountName,
            account.AccountGroupId,
            string.Empty,
            string.Empty,
            account.AccountType,
            account.IsReconcilable,
            account.IsActive);
    }

    public async Task<string> DeleteAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Account with ID {id} not found.");

        // Invariant guard: account referenced by journal voucher lines must keep its audit trail.
        var hasJournalEntries = await _dbContext.JournalVoucherLines
            .AnyAsync(l => l.AccountId == id && !l.IsDeleted, cancellationToken);
        if (hasJournalEntries)
        {
            throw new InvalidOperationException(
                $"Cannot delete Account '{account.AccountCode}': Account is referenced in active journal vouchers. Soft deletion is restricted to preserve financial audit trail.");
        }

        // Sub-ledger control account guard.
        var isCustomerControl = await _dbContext.Customers.AnyAsync(c => c.GlControlAccountId == id && !c.IsDeleted, cancellationToken);
        var isVendorControl = await _dbContext.Vendors.AnyAsync(v => v.GlControlAccountId == id && !v.IsDeleted, cancellationToken);
        if (isCustomerControl || isVendorControl)
        {
            throw new InvalidOperationException(
                $"Cannot delete Account '{account.AccountCode}': Account is assigned as a subledger control account.");
        }

        _dbContext.Accounts.Remove(account); // ChangeTracker converts Remove into a soft delete
        await _dbContext.SaveChangesAsync(cancellationToken);

        return $"Account '{account.AccountCode}' successfully soft-deleted.";
    }

    public async Task<string> RestoreAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (account == null || !account.IsDeleted)
        {
            throw new KeyNotFoundException($"Soft-deleted account with ID {id} not found.");
        }

        account.IsDeleted = false;
        account.DeletedAtUtc = null;
        account.DeletedBy = null;
        account.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        account.ModifiedBy = _currentUserContext.UserName ?? "system";

        await _dbContext.SaveChangesAsync(cancellationToken);
        return $"Account '{account.AccountCode}' successfully restored.";
    }
}
