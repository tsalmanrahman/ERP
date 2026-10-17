using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.Identity;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Abstractions.ProcureToPay;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Abstractions.Payroll;
using ERP.Common.Application.Abstractions.Specialized;
using ERP.Common.Application.Abstractions.Migration;
using ERP.Common.Application.Abstractions.Reports;
using ERP.Common.Application.Abstractions.SystemAdmin;
using ERP.Common.Application.Security;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Common;
using ERP.Common.Infrastructure.Services.Data;
using ERP.Common.Infrastructure.Services.Finance;
using ERP.Common.Infrastructure.Services.Identity;
using ERP.Common.Infrastructure.Services.Inventory;
using ERP.Common.Infrastructure.Services.Migration;
using ERP.Common.Infrastructure.Services.OrderToCash;
using ERP.Common.Infrastructure.Services.Payroll;
using ERP.Common.Infrastructure.Services.ProcureToPay;
using ERP.Common.Infrastructure.Services.Reports;
using ERP.Common.Infrastructure.Services.Specialized;
using ERP.Common.Infrastructure.Services.SystemAdmin;

namespace ERP.Common.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommonInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        // Identity, Governance & Sequences
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IDocumentSequenceService, DocumentSequenceService>();
        services.AddScoped<IAuditHistoryService, AuditHistoryService>();

        // General Ledger & Banking
        services.AddScoped<IAccountService, AccountService>();
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

        // QuestPDF Reporting Engine
        services.AddScoped<IPdfReportingService, PdfReportingService>();

        services.AddDbContext<ErpDbContext>(options =>
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

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ErpDbContext>());

        return services;
    }
}
