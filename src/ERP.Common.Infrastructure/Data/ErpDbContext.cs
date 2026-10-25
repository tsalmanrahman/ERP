using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Domain.Common;
using ERP.Common.Domain.Entities.SystemAdmin;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Domain.Entities.Procurement;
using ERP.Common.Domain.Entities.Sales;
using ERP.Common.Domain.Entities.HumanResources;
using ERP.Common.Domain.Entities.FixedAssets;
using ERP.Common.Domain.Entities.RealEstate;
using ERP.Common.Domain.Entities.Manufacturing;
using ERP.Common.Domain.Entities.Migration;

namespace ERP.Common.Infrastructure.Data;

public class ErpDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly ICurrentUserContext? _currentUserContext;

    public int? CurrentCompanyId => _currentUserContext?.CompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserCompanyAccess> UserCompanyAccess => Set<UserCompanyAccess>();
    public DbSet<AccountGroup> AccountGroups => Set<AccountGroup>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalVoucher> JournalVouchers => Set<JournalVoucher>();
    public DbSet<JournalVoucherLine> JournalVoucherLines => Set<JournalVoucherLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorGroup> VendorGroups => Set<VendorGroup>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ProductClass> ProductClasses => Set<ProductClass>();
    public DbSet<MasterLookup> MasterLookups => Set<MasterLookup>();
    public DbSet<ProductPriceHistory> ProductPriceHistories => Set<ProductPriceHistory>();
    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ProductStock> ProductStock => Set<ProductStock>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes => Set<GoodsReceiptNote>();
    public DbSet<GoodsReceiptNoteLine> GoodsReceiptNoteLines => Set<GoodsReceiptNoteLine>();
    public DbSet<GoodsReceiptReturn> GoodsReceiptReturns => Set<GoodsReceiptReturn>();
    public DbSet<GoodsReceiptReturnLine> GoodsReceiptReturnLines => Set<GoodsReceiptReturnLine>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferLine> StockTransferLines => Set<StockTransferLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseBill> PurchaseBills => Set<PurchaseBill>();
    public DbSet<PurchaseBillLine> PurchaseBillLines => Set<PurchaseBillLine>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();
    public DbSet<VendorPaymentAllocation> VendorPaymentAllocations => Set<VendorPaymentAllocation>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<CustomerReceipt> CustomerReceipts => Set<CustomerReceipt>();
    public DbSet<CustomerReceiptAllocation> CustomerReceiptAllocations => Set<CustomerReceiptAllocation>();
    public DbSet<SalesCenter> SalesCenters => Set<SalesCenter>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<DeliveryChallan> DeliveryChallans => Set<DeliveryChallan>();
    public DbSet<DeliveryChallanLine> DeliveryChallanLines => Set<DeliveryChallanLine>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<DailyAttendance> DailyAttendance => Set<DailyAttendance>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<SalarySheet> SalarySheets => Set<SalarySheet>();
    public DbSet<SalarySheetLine> SalarySheetLines => Set<SalarySheetLine>();

    // Fixed Assets
    public DbSet<AssetGroup> AssetGroups => Set<AssetGroup>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();

    // Real Estate
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
    public DbSet<PropertyBooking> PropertyBookings => Set<PropertyBooking>();
    public DbSet<BookingInstallment> BookingInstallments => Set<BookingInstallment>();

    // Manufacturing
    public DbSet<BillOfMaterials> BillsOfMaterials => Set<BillOfMaterials>();
    public DbSet<BomComponent> BomComponents => Set<BomComponent>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();

    // System Admin & Governance
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Data Migration & Cutover
    public DbSet<MigrationLog> MigrationLogs => Set<MigrationLog>();
    public DbSet<CutoverAudit> CutoverAudits => Set<CutoverAudit>();
    public DbSet<HistoricalLedgerArchive> HistoricalLedgerArchives => Set<HistoricalLedgerArchive>();

    public ErpDbContext(
        DbContextOptions<ErpDbContext> options,
        IDateTimeProvider? dateTimeProvider = null,
        ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndTenantInformation();
        var auditLogs = CollectAuditLogs();
        var result = await base.SaveChangesAsync(cancellationToken);
        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    public override int SaveChanges()
    {
        ApplyAuditAndTenantInformation();
        var auditLogs = CollectAuditLogs();
        var result = base.SaveChanges();
        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
            base.SaveChanges();
        }
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ApplyGlobalFilters(modelBuilder, entityType.ClrType);

            if (typeof(IIdentifiableWithGuid).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IIdentifiableWithGuid.SysGuid))
                    .HasDefaultValueSql("NEWSEQUENTIALID()");
            }
        }
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder, Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "e");
        Expression? filter = null;

        if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
        {
            var isDeletedProp = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(isDeletedProp);
            filter = notDeleted;
        }

        if (typeof(IMultiTenant).IsAssignableFrom(clrType))
        {
            var companyIdProp = Expression.Property(parameter, nameof(IMultiTenant.CompanyId));
            var contextExpr = Expression.Constant(this);
            var currentCompanyIdProp = Expression.Property(contextExpr, nameof(CurrentCompanyId));

            // currentCompanyId == null || e.CompanyId == currentCompanyId
            var isCompanyNull = Expression.Equal(currentCompanyIdProp, Expression.Constant(null, typeof(int?)));
            var nullableCompanyId = Expression.Convert(companyIdProp, typeof(int?));
            var isSameCompany = Expression.Equal(nullableCompanyId, currentCompanyIdProp);
            var tenantCondition = Expression.OrElse(isCompanyNull, isSameCompany);

            filter = filter == null ? tenantCondition : Expression.AndAlso(filter, tenantCondition);
        }

        if (filter != null)
        {
            var lambda = Expression.Lambda(filter, parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }

    private void ApplyAuditAndTenantInformation()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var currentUser = _currentUserContext?.UserName ?? "System";
        var currentCompany = _currentUserContext?.CompanyId;

        foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CompanyId == 0 && currentCompany.HasValue)
            {
                entry.Entity.CompanyId = currentCompany.Value;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IIdentifiableWithGuid>())
        {
            if (entry.State == EntityState.Added && entry.Entity.SysGuid == Guid.Empty)
            {
                entry.Entity.SysGuid = Guid.NewGuid();
            }
        }

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = currentUser;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
                entry.Entity.ModifiedBy = currentUser;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedBy = currentUser;
            }
        }
    }

    private List<AuditLog> CollectAuditLogs()
    {
        var auditLogs = new List<AuditLog>();
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var currentUser = _currentUserContext?.UserName ?? "System";
        var currentCompany = _currentUserContext?.CompanyId ?? 1;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var entityType = entry.Entity.GetType();
            var entityName = entityType.Name;
            var sysGuid = (entry.Entity as IIdentifiableWithGuid)?.SysGuid ?? Guid.Empty;

            string action;
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var affectedColumns = new List<string>();

            if (entry.State == EntityState.Added)
            {
                action = "Create";
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey()) continue;
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                action = "Delete";
                foreach (var prop in entry.Properties)
                {
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                }
            }
            else // Modified
            {
                var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
                if (isDeletedProp != null && isDeletedProp.IsModified)
                {
                    if (true.Equals(isDeletedProp.CurrentValue) && !true.Equals(isDeletedProp.OriginalValue))
                    {
                        action = "Delete";
                    }
                    else if (!true.Equals(isDeletedProp.CurrentValue) && true.Equals(isDeletedProp.OriginalValue))
                    {
                        action = "Restore";
                    }
                    else
                    {
                        action = "Update";
                    }
                }
                else
                {
                    action = "Update";
                }

                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified)
                    {
                        affectedColumns.Add(prop.Metadata.Name);
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }
            }

            string? secondaryId = null;
            var codeProp = entityType.GetProperties()
                .FirstOrDefault(p => p.Name.Contains("Code") || p.Name.Contains("Number") || p.Name == "VoucherNumber");
            if (codeProp != null)
            {
                secondaryId = codeProp.GetValue(entry.Entity)?.ToString();
            }

            int entityCompanyId = currentCompany;
            if (entry.Entity is IMultiTenant tenantEntity && tenantEntity.CompanyId != 0)
            {
                entityCompanyId = tenantEntity.CompanyId;
            }

            auditLogs.Add(new AuditLog
            {
                CompanyId = entityCompanyId,
                EntityName = entityName,
                EntitySysGuid = sysGuid,
                RecordIdentifier = secondaryId,
                Action = action,
                UserId = _currentUserContext?.UserId,
                UserName = currentUser,
                TimestampUtc = now,
                OldValues = oldValues.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(newValues) : null,
                AffectedColumns = affectedColumns.Count > 0 ? string.Join(", ", affectedColumns) : null,
                Source = "Web API"
            });
        }

        return auditLogs;
    }
}
