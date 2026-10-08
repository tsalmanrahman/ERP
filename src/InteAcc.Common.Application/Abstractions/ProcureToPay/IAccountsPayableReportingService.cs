using InteAcc.Common.Application.Models.ProcureToPay;

namespace InteAcc.Common.Application.Abstractions.ProcureToPay;

public interface IAccountsPayableReportingService
{
    Task<VendorAgingReport> GetVendorAgingReportAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
    Task<VendorStatementReport> GetVendorStatementReportAsync(int vendorId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);
}
