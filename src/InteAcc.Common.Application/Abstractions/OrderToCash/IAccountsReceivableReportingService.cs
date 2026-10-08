using InteAcc.Common.Application.Models.OrderToCash;

namespace InteAcc.Common.Application.Abstractions.OrderToCash;

public interface IAccountsReceivableReportingService
{
    Task<CustomerAgingReport> GetCustomerAgingReportAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<CustomerStatementReport> GetCustomerStatementReportAsync(int customerId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
}
