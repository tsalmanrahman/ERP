using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.OrderToCash;
using InteAcc.Common.Application.Models.OrderToCash;
using InteAcc.Common.Domain.Entities.AccountsReceivable;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CustomerService(InteAccDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
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

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var existing = await _dbContext.Customers
            .AnyAsync(c => c.CompanyId == companyId && c.CustomerCode == request.CustomerCode, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"Customer with code '{request.CustomerCode}' already exists.");
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
            CustomerCode = request.CustomerCode.Trim(),
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
