using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Data;
using InteAcc.Common.Domain.Common;
using InteAcc.Common.Domain.Entities.SystemAdmin;
using InteAcc.Common.Domain.Entities.GeneralLedger;
using InteAcc.Common.Domain.Entities.AccountsReceivable;
using InteAcc.Common.Domain.Entities.AccountsPayable;
using InteAcc.Common.Domain.Entities.Inventory;
using InteAcc.Common.Domain.Entities.Procurement;
using InteAcc.Common.Domain.Entities.Sales;
using InteAcc.Common.Domain.Entities.HumanResources;
using InteAcc.Common.Domain.Entities.FixedAssets;
using InteAcc.Common.Domain.Entities.RealEstate;
using InteAcc.Common.Domain.Entities.Manufacturing;
using InteAcc.Common.Domain.Entities.Migration;

namespace InteAcc.Common.Infrastructure.Data;

public class InteAccDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly ICurrentUserContext? _currentUserContext;

    public int? CurrentCompanyId => _currentUserContext?.CompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
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
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ProductStock> ProductStock => Set<ProductStock>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes => Set<GoodsReceiptNote>();
    public DbSet<GoodsReceiptNoteLine> GoodsReceiptNoteLines => Set<GoodsReceiptNoteLine>();
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

    // Data Migration & Cutover
    public DbSet<MigrationLog> MigrationLogs => Set<MigrationLog>();
    public DbSet<CutoverAudit> CutoverAudits => Set<CutoverAudit>();
    public DbSet<HistoricalLedgerArchive> HistoricalLedgerArchives => Set<HistoricalLedgerArchive>();

    public InteAccDbContext(
        DbContextOptions<InteAccDbContext> options,
        IDateTimeProvider? dateTimeProvider = null,
        ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUserContext = currentUserContext;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndTenantInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndTenantInformation();
        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InteAccDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ApplyGlobalFilters(modelBuilder, entityType.ClrType);
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
}
