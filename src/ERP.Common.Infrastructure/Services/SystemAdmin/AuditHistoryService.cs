using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.SystemAdmin;
using ERP.Common.Application.Models.SystemAdmin;
using ERP.Common.Domain.Common;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.SystemAdmin;

public class AuditHistoryService : IAuditHistoryService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditHistoryService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditHistoryByEntityGuidAsync(string entityName, Guid entitySysGuid, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.EntityName == entityName && a.EntitySysGuid == entitySysGuid)
            .OrderByDescending(a => a.TimestampUtc)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                SysGuid = a.SysGuid,
                CompanyId = a.CompanyId,
                EntityName = a.EntityName,
                EntitySysGuid = a.EntitySysGuid,
                RecordIdentifier = a.RecordIdentifier,
                Action = a.Action,
                UserId = a.UserId,
                UserName = a.UserName,
                TimestampUtc = a.TimestampUtc,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                AffectedColumns = a.AffectedColumns,
                Source = a.Source
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAuditLogsAsync(int top = 50, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.TimestampUtc)
            .Take(top)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                SysGuid = a.SysGuid,
                CompanyId = a.CompanyId,
                EntityName = a.EntityName,
                EntitySysGuid = a.EntitySysGuid,
                RecordIdentifier = a.RecordIdentifier,
                Action = a.Action,
                UserId = a.UserId,
                UserName = a.UserName,
                TimestampUtc = a.TimestampUtc,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                AffectedColumns = a.AffectedColumns,
                Source = a.Source
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RestoreEntityAsync(string entityName, Guid entitySysGuid, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.GetRequiredCompanyId();
        var currentUser = _currentUserContext.UserName ?? "System";
        var now = _dateTimeProvider.UtcNow;

        switch (entityName.ToLowerInvariant())
        {
            case "customer":
            case "customers":
                var customer = await _dbContext.Customers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.SysGuid == entitySysGuid && c.CompanyId == companyId, cancellationToken);
                if (customer == null || !customer.IsDeleted) return false;
                customer.IsDeleted = false;
                customer.DeletedAtUtc = null;
                customer.DeletedBy = null;
                customer.ModifiedAtUtc = now;
                customer.ModifiedBy = currentUser;
                break;

            case "vendor":
            case "vendors":
                var vendor = await _dbContext.Vendors
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.SysGuid == entitySysGuid && v.CompanyId == companyId, cancellationToken);
                if (vendor == null || !vendor.IsDeleted) return false;
                vendor.IsDeleted = false;
                vendor.DeletedAtUtc = null;
                vendor.DeletedBy = null;
                vendor.ModifiedAtUtc = now;
                vendor.ModifiedBy = currentUser;
                break;

            case "account":
            case "accounts":
                var account = await _dbContext.Accounts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.SysGuid == entitySysGuid && a.CompanyId == companyId, cancellationToken);
                if (account == null || !account.IsDeleted) return false;
                account.IsDeleted = false;
                account.DeletedAtUtc = null;
                account.DeletedBy = null;
                account.ModifiedAtUtc = now;
                account.ModifiedBy = currentUser;
                break;

            case "product":
            case "products":
                var product = await _dbContext.Products
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.SysGuid == entitySysGuid && p.CompanyId == companyId, cancellationToken);
                if (product == null || !product.IsDeleted) return false;
                product.IsDeleted = false;
                product.DeletedAtUtc = null;
                product.DeletedBy = null;
                product.ModifiedAtUtc = now;
                product.ModifiedBy = currentUser;
                break;

            case "employee":
            case "employees":
                var emp = await _dbContext.Employees
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(e => e.SysGuid == entitySysGuid && e.CompanyId == companyId, cancellationToken);
                if (emp == null || !emp.IsDeleted) return false;
                emp.IsDeleted = false;
                emp.DeletedAtUtc = null;
                emp.DeletedBy = null;
                emp.ModifiedAtUtc = now;
                emp.ModifiedBy = currentUser;
                break;

            case "journalvoucher":
            case "journalvouchers":
                var jv = await _dbContext.JournalVouchers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.SysGuid == entitySysGuid && v.CompanyId == companyId, cancellationToken);
                if (jv == null || !jv.IsDeleted) return false;
                jv.IsDeleted = false;
                jv.DeletedAtUtc = null;
                jv.DeletedBy = null;
                jv.ModifiedAtUtc = now;
                jv.ModifiedBy = currentUser;
                break;

            default:
                return false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
