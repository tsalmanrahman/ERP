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

    public Task<List<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        var query = from a in _dbContext.Accounts.AsNoTracking()
                    where a.CompanyId == companyId
                    join g in _dbContext.AccountGroups.AsNoTracking() on a.AccountGroupId equals g.Id into gJoin
                    from g in gJoin.DefaultIfEmpty()
                    orderby a.AccountCode
                    select new AccountDto(
                        a.Id,
                        a.AccountCode,
                        a.AccountName,
                        a.AccountGroupId,
                        g != null ? g.GroupName : string.Empty,
                        g != null ? g.AccountCategory.ToString() : string.Empty,
                        a.AccountType,
                        a.IsReconcilable,
                        a.IsActive);

        return query.ToListAsync(cancellationToken);
    }

    public Task<List<AccountGroupDto>> GetAccountGroupsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        return _dbContext.AccountGroups
            .Where(g => g.CompanyId == companyId)
            .OrderBy(g => g.GroupCode)
            .Select(g => new AccountGroupDto(g.Id, g.GroupCode, g.GroupName, g.AccountCategory.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccountCode) || string.IsNullOrWhiteSpace(request.AccountName))
        {
            throw new InvalidOperationException("Account Code and Account Name are required.");
        }

        var companyId = _currentUserContext.GetRequiredCompanyId();

        var code = request.AccountCode.Trim();
        var exists = await _dbContext.Accounts
            .AnyAsync(a => a.CompanyId == companyId && a.AccountCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Account with code '{request.AccountCode}' already exists.");
        }

        int resolvedGroupId;
        if (request.AccountGroupId > 0)
        {
            var group = await _dbContext.AccountGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == request.AccountGroupId && g.CompanyId == companyId, cancellationToken)
                ?? throw new InvalidOperationException("The selected Account Group does not belong to the active company.");
            resolvedGroupId = group.Id;
        }
        else
        {
            var defaultGroup = await _dbContext.AccountGroups
                .AsNoTracking()
                .Where(g => g.CompanyId == companyId)
                .OrderBy(g => g.GroupCode)
                .FirstOrDefaultAsync(cancellationToken);
            resolvedGroupId = defaultGroup?.Id ?? 0;
        }

        var account = new Account
        {
            CompanyId = companyId,
            AccountCode = code,
            AccountName = request.AccountName.Trim(),
            AccountGroupId = resolvedGroupId,
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
        var companyId = _currentUserContext.GetRequiredCompanyId();
        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, cancellationToken)
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
        var companyId = _currentUserContext.GetRequiredCompanyId();
        var account = await _dbContext.Accounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, cancellationToken);
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
