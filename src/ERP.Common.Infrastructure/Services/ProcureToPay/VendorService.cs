using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.ProcureToPay;
using ERP.Common.Application.Models.ProcureToPay;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.ProcureToPay;

public class VendorService : IVendorService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDocumentSequenceService? _sequenceService;

    public VendorService(ErpDbContext dbContext, ICurrentUserContext currentUserContext, IDocumentSequenceService? sequenceService = null)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _sequenceService = sequenceService;
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

    public async Task<VendorDto?> GetVendorByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = from v in _dbContext.Vendors.AsNoTracking()
                    where v.CompanyId == companyId && v.VendorCode == normalizedCode
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

    public async Task<bool> SoftDeleteVendorAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var vendor = await _dbContext.Vendors
            .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == companyId, cancellationToken);
        if (vendor == null) return false;

        var hasBills = await _dbContext.PurchaseBills
            .AnyAsync(b => b.VendorId == id && b.CompanyId == companyId && !b.IsDeleted && b.BalanceAmount > 0, cancellationToken);
        if (hasBills)
        {
            throw new InvalidOperationException($"Cannot soft-delete vendor '{vendor.VendorCode}': Vendor has outstanding unpaid purchase bills.");
        }

        _dbContext.Vendors.Remove(vendor);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var vendorCode = request.VendorCode?.Trim();
        if (string.IsNullOrWhiteSpace(vendorCode) || vendorCode.StartsWith("[Auto-Generated", StringComparison.OrdinalIgnoreCase))
        {
            if (_sequenceService != null)
            {
                vendorCode = await _sequenceService.GenerateCodeAsync(companyId, "Vendor", cancellationToken);
            }
            else
            {
                var count = await _dbContext.Vendors.CountAsync(v => v.CompanyId == companyId, cancellationToken);
                vendorCode = $"VEN-{(count + 1):D6}";
            }
        }
        else
        {
            vendorCode = vendorCode.ToUpperInvariant();
        }

        var exists = await _dbContext.Vendors.AnyAsync(v => v.CompanyId == companyId && v.VendorCode == vendorCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Vendor with code '{vendorCode}' already exists.");
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
            VendorCode = vendorCode,
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
