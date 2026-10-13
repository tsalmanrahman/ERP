using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Data;
using InteAcc.Common.Application.Abstractions.Finance;
using InteAcc.Common.Application.Abstractions.Identity;
using InteAcc.Common.Application.Abstractions.Inventory;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Abstractions.OrderToCash;
using InteAcc.Common.Application.Abstractions.Payroll;
using InteAcc.Common.Application.Abstractions.Specialized;
using InteAcc.Common.Application.Abstractions.Migration;
using InteAcc.Common.Application.Abstractions.SystemAdmin;
using InteAcc.Common.Infrastructure.Data;
using InteAcc.Common.Infrastructure.Services;

namespace InteAcc.Common.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        // Identity & Security
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();

        // General Ledger & Banking
        services.AddScoped<IJournalVoucherService, JournalVoucherService>();
        services.AddScoped<IFinancialReportingService, FinancialReportingService>();
        services.AddScoped<IBankingService, BankingService>();

        // Supply Chain & Inventory Control
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IInventoryReportingService, InventoryReportingService>();

        // Procure-to-Pay & Accounts Payable
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseBillService, PurchaseBillService>();
        services.AddScoped<IVendorPaymentService, VendorPaymentService>();
        services.AddScoped<IAccountsPayableReportingService, AccountsPayableReportingService>();

        // Order-to-Cash & Accounts Receivable
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IDeliveryChallanService, DeliveryChallanService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ICustomerReceiptService, CustomerReceiptService>();
        services.AddScoped<IAccountsReceivableReportingService, AccountsReceivableReportingService>();

        // Human Resources & Payroll
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IPayrollReportingService, PayrollReportingService>();

        // Specialized Modules (Fixed Assets, Real Estate, Manufacturing)
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<IRealEstateService, RealEstateService>();
        services.AddScoped<IManufacturingService, ManufacturingService>();

        // Phase 10: Data Migration, Financial Parity Audit & Cutover
        services.AddScoped<IDataMigrationService, DataMigrationService>();
        services.AddScoped<IFinancialParityAuditService, FinancialParityAuditService>();
        services.AddScoped<ICutoverService, CutoverService>();

        services.AddDbContext<InteAccDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<InteAccDbContext>());

        return services;
    }
}
