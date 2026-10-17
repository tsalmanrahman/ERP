using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.OrderToCash;

public class CustomerService : ICustomerService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDocumentSequenceService? _sequenceService;

    public CustomerService(ErpDbContext dbContext, ICurrentUserContext currentUserContext, IDocumentSequenceService? sequenceService = null)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _sequenceService = sequenceService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from c in _dbContext.Customers.AsNoTracking()
                    where c.CompanyId == companyId
                    join cg in _dbContext.CustomerGroups.AsNoTracking() on c.CustomerGroupId equals cg.Id into cgJoin
                    from cg in cgJoin.DefaultIfEmpty()
                    join a in _dbContext.Accounts.AsNoTracking() on c.GlControlAccountId equals a.Id into aJoin
                    from a in aJoin.DefaultIfEmpty()
                    select new CustomerDto
                    {
                        Id = c.Id,
                        SysGuid = c.SysGuid,
                        CustomerCode = c.CustomerCode,
                        CustomerName = c.CustomerName,
                        CustomerGroupId = c.CustomerGroupId,
                        CustomerGroupName = cg != null ? cg.GroupName : null,
                        ContactPerson = c.ContactPerson,
                        Phone = c.Phone,
                        Email = c.Email,
                        AddressLine1 = c.AddressLine1,
                        City = c.City,
                        CountryCode = c.CountryCode,
                        TaxId = c.TaxId,
                        GlControlAccountId = c.GlControlAccountId,
                        GlControlAccountCode = a != null ? a.AccountCode : null,
                        CurrentBalance = c.CurrentBalance,
                        CreditLimit = c.CreditLimit,
                        CreditPeriodDays = c.CreditPeriodDays,
                        IsActive = c.IsActive
                    };

        return await query.OrderBy(c => c.CustomerCode).ToListAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from c in _dbContext.Customers.AsNoTracking()
                    where c.Id == id && c.CompanyId == companyId
                    join cg in _dbContext.CustomerGroups.AsNoTracking() on c.CustomerGroupId equals cg.Id into cgJoin
                    from cg in cgJoin.DefaultIfEmpty()
                    join a in _dbContext.Accounts.AsNoTracking() on c.GlControlAccountId equals a.Id into aJoin
                    from a in aJoin.DefaultIfEmpty()
                    select new CustomerDto
                    {
                        Id = c.Id,
                        SysGuid = c.SysGuid,
                        CustomerCode = c.CustomerCode,
                        CustomerName = c.CustomerName,
                        CustomerGroupId = c.CustomerGroupId,
                        CustomerGroupName = cg != null ? cg.GroupName : null,
                        ContactPerson = c.ContactPerson,
                        Phone = c.Phone,
                        Email = c.Email,
                        AddressLine1 = c.AddressLine1,
                        City = c.City,
                        CountryCode = c.CountryCode,
                        TaxId = c.TaxId,
                        GlControlAccountId = c.GlControlAccountId,
                        GlControlAccountCode = a != null ? a.AccountCode : null,
                        CurrentBalance = c.CurrentBalance,
                        CreditLimit = c.CreditLimit,
                        CreditPeriodDays = c.CreditPeriodDays,
                        IsActive = c.IsActive
                    };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetCustomerByGuidAsync(Guid sysGuid, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from c in _dbContext.Customers.AsNoTracking()
                    where c.SysGuid == sysGuid && c.CompanyId == companyId
                    join cg in _dbContext.CustomerGroups.AsNoTracking() on c.CustomerGroupId equals cg.Id into cgJoin
                    from cg in cgJoin.DefaultIfEmpty()
                    join a in _dbContext.Accounts.AsNoTracking() on c.GlControlAccountId equals a.Id into aJoin
                    from a in aJoin.DefaultIfEmpty()
                    select new CustomerDto
                    {
                        Id = c.Id,
                        SysGuid = c.SysGuid,
                        CustomerCode = c.CustomerCode,
                        CustomerName = c.CustomerName,
                        CustomerGroupId = c.CustomerGroupId,
                        CustomerGroupName = cg != null ? cg.GroupName : null,
                        ContactPerson = c.ContactPerson,
                        Phone = c.Phone,
                        Email = c.Email,
                        AddressLine1 = c.AddressLine1,
                        City = c.City,
                        CountryCode = c.CountryCode,
                        TaxId = c.TaxId,
                        GlControlAccountId = c.GlControlAccountId,
                        GlControlAccountCode = a != null ? a.AccountCode : null,
                        CurrentBalance = c.CurrentBalance,
                        CreditLimit = c.CreditLimit,
                        CreditPeriodDays = c.CreditPeriodDays,
                        IsActive = c.IsActive
                    };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetCustomerByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = from c in _dbContext.Customers.AsNoTracking()
                    where c.CustomerCode == normalizedCode && c.CompanyId == companyId
                    join cg in _dbContext.CustomerGroups.AsNoTracking() on c.CustomerGroupId equals cg.Id into cgJoin
                    from cg in cgJoin.DefaultIfEmpty()
                    join a in _dbContext.Accounts.AsNoTracking() on c.GlControlAccountId equals a.Id into aJoin
                    from a in aJoin.DefaultIfEmpty()
                    select new CustomerDto
                    {
                        Id = c.Id,
                        SysGuid = c.SysGuid,
                        CustomerCode = c.CustomerCode,
                        CustomerName = c.CustomerName,
                        CustomerGroupId = c.CustomerGroupId,
                        CustomerGroupName = cg != null ? cg.GroupName : null,
                        ContactPerson = c.ContactPerson,
                        Phone = c.Phone,
                        Email = c.Email,
                        AddressLine1 = c.AddressLine1,
                        City = c.City,
                        CountryCode = c.CountryCode,
                        TaxId = c.TaxId,
                        GlControlAccountId = c.GlControlAccountId,
                        GlControlAccountCode = a != null ? a.AccountCode : null,
                        CurrentBalance = c.CurrentBalance,
                        CreditLimit = c.CreditLimit,
                        CreditPeriodDays = c.CreditPeriodDays,
                        IsActive = c.IsActive
                    };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteCustomerAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId, cancellationToken);
        if (customer == null) return false;

        // Invariant guard: check for unpaid invoices or active sales orders
        var hasInvoices = await _dbContext.Invoices
            .AnyAsync(i => i.CustomerId == id && !i.IsDeleted && i.BalanceAmount > 0, cancellationToken);
        if (hasInvoices)
        {
            throw new InvalidOperationException($"Cannot soft-delete customer '{customer.CustomerCode}': Customer has outstanding unpaid invoices.");
        }

        _dbContext.Customers.Remove(customer); // Triggers EF Core ChangeTracker soft-delete
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreCustomerAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var customer = await _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId, cancellationToken);
        if (customer == null || !customer.IsDeleted) return false;

        customer.IsDeleted = false;
        customer.DeletedAtUtc = null;
        customer.DeletedBy = null;
        customer.ModifiedAtUtc = DateTime.UtcNow;
        customer.ModifiedBy = _currentUserContext.UserName ?? "System";

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var customerCode = request.CustomerCode?.Trim();
        if (string.IsNullOrWhiteSpace(customerCode) || customerCode.StartsWith("[Auto-Generated", StringComparison.OrdinalIgnoreCase))
        {
            if (_sequenceService != null)
            {
                customerCode = await _sequenceService.GenerateCodeAsync(companyId, "Customer", cancellationToken);
            }
            else
            {
                var count = await _dbContext.Customers.CountAsync(c => c.CompanyId == companyId, cancellationToken);
                customerCode = $"CUS-{(count + 1):D6}";
            }
        }

        var existing = await _dbContext.Customers
            .AnyAsync(c => c.CompanyId == companyId && c.CustomerCode == customerCode, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"Customer with code '{customerCode}' already exists.");
        }

        int glAccountId = request.GlControlAccountId ?? 0;
        if (glAccountId == 0)
        {
            var defaultArAccount = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1130", cancellationToken);
            glAccountId = defaultArAccount?.Id ?? 5;
        }

        var customer = new Customer
        {
            CompanyId = companyId,
            CustomerCode = customerCode,
            CustomerName = request.CustomerName.Trim(),
            CustomerGroupId = request.CustomerGroupId,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            CountryCode = request.CountryCode ?? "BGD",
            TaxId = request.TaxId,
            GlControlAccountId = glAccountId,
            CurrentBalance = 0,
            CreditLimit = request.CreditLimit,
            CreditPeriodDays = request.CreditPeriodDays,
            IsActive = true
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetCustomerByIdAsync(customer.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<CustomerGroupDto>> GetCustomerGroupsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await _dbContext.CustomerGroups
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId)
            .OrderBy(g => g.GroupCode)
            .Select(g => new CustomerGroupDto
            {
                Id = g.Id,
                GroupCode = g.GroupCode,
                GroupName = g.GroupName
            })
            .ToListAsync(cancellationToken);
    }
}
