using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Models.ProcureToPay;
using InteAcc.Common.Domain.Entities.AccountsPayable;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class VendorService : IVendorService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public VendorService(InteAccDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<List<VendorDto>> GetVendorsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from v in _dbContext.Vendors.AsNoTracking()
                    where v.CompanyId == companyId
                    join vg in _dbContext.Set<VendorGroup>().AsNoTracking() on v.VendorGroupId equals vg.Id into vgJoin
                    from vg in vgJoin.DefaultIfEmpty()
                    join a in _dbContext.Accounts.AsNoTracking() on v.GlControlAccountId equals a.Id into aJoin
                    from a in aJoin.DefaultIfEmpty()
                    select new VendorDto
                    {
                        Id = v.Id,
                        VendorCode = v.VendorCode,
                        VendorName = v.VendorName,
                        VendorGroupId = v.VendorGroupId,
                        VendorGroupName = vg != null ? vg.GroupName : null,
                        ContactPerson = v.ContactPerson,
                        Phone = v.Phone,
                        Email = v.Email,
                        AddressLine1 = v.AddressLine1,
                        City = v.City,
                        CountryCode = v.CountryCode,
                        TaxId = v.TaxId,
                        GlControlAccountId = v.GlControlAccountId,
                        GlControlAccountCode = a != null ? a.AccountCode : null,
                        CurrentBalance = v.CurrentBalance,
                        CreditLimit = v.CreditLimit,
                        CreditPeriodDays = v.CreditPeriodDays,
                        IsActive = v.IsActive
                    };

        return await query.OrderBy(v => v.VendorCode).ToListAsync(cancellationToken);
    }

    public async Task<VendorDto?> GetVendorByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from v in _dbContext.Vendors.AsNoTracking()
                    where v.CompanyId == companyId && v.Id == id
                    join vg in _dbContext.Set<VendorGroup>().AsNoTracking() on v.VendorGroupId equals vg.Id into vgJoin
                    from vg in vgJoin.DefaultIfEmpty()
                    join a in _dbContext.Accounts.AsNoTracking() on v.GlControlAccountId equals a.Id into aJoin
                    from a in aJoin.DefaultIfEmpty()
                    select new VendorDto
                    {
                        Id = v.Id,
                        VendorCode = v.VendorCode,
                        VendorName = v.VendorName,
                        VendorGroupId = v.VendorGroupId,
                        VendorGroupName = vg != null ? vg.GroupName : null,
                        ContactPerson = v.ContactPerson,
                        Phone = v.Phone,
                        Email = v.Email,
                        AddressLine1 = v.AddressLine1,
                        City = v.City,
                        CountryCode = v.CountryCode,
                        TaxId = v.TaxId,
                        GlControlAccountId = v.GlControlAccountId,
                        GlControlAccountCode = a != null ? a.AccountCode : null,
                        CurrentBalance = v.CurrentBalance,
                        CreditLimit = v.CreditLimit,
                        CreditPeriodDays = v.CreditPeriodDays,
                        IsActive = v.IsActive
                    };

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var exists = await _dbContext.Vendors.AnyAsync(v => v.CompanyId == companyId && v.VendorCode == request.VendorCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Vendor with code '{request.VendorCode}' already exists.");
        }

        int glAccountId = request.GlControlAccountId ?? 0;
        if (glAccountId == 0)
        {
            var defaultAp = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2110", cancellationToken);
            glAccountId = defaultAp?.Id ?? 11;
        }

        var vendor = new Vendor
        {
            CompanyId = companyId,
            VendorCode = request.VendorCode.Trim().ToUpperInvariant(),
            VendorName = request.VendorName.Trim(),
            VendorGroupId = request.VendorGroupId,
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            AddressLine1 = request.AddressLine1?.Trim(),
            City = request.City?.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "BGD" : request.CountryCode.Trim().ToUpperInvariant(),
            TaxId = request.TaxId?.Trim(),
            GlControlAccountId = glAccountId,
            CurrentBalance = 0m,
            CreditLimit = request.CreditLimit,
            CreditPeriodDays = request.CreditPeriodDays > 0 ? request.CreditPeriodDays : 30,
            IsActive = true
        };

        _dbContext.Vendors.Add(vendor);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetVendorByIdAsync(vendor.Id, cancellationToken))!;
    }

    public async Task<List<VendorGroupDto>> GetVendorGroupsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        return await _dbContext.Set<VendorGroup>().AsNoTracking()
            .Where(vg => vg.CompanyId == companyId)
            .OrderBy(vg => vg.GroupCode)
            .Select(vg => new VendorGroupDto
            {
                Id = vg.Id,
                GroupCode = vg.GroupCode,
                GroupName = vg.GroupName
            })
            .ToListAsync(cancellationToken);
    }
}
