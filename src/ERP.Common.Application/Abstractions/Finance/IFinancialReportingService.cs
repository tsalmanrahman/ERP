using ERP.Common.Application.Models.Finance;
using ERP.Common.Domain.Common;

namespace ERP.Common.Application.Abstractions.Finance;

public interface IFinancialReportingService
{
    Task<Result<TrialBalanceReportDto>> GetTrialBalanceAsync(DateOnly? asOfDate, CancellationToken cancellationToken = default);
    Task<Result<AccountStatementReportDto>> GetAccountStatementAsync(int accountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
