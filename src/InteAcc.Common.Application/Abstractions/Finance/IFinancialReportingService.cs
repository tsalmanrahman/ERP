using InteAcc.Common.Application.Models.Finance;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Application.Abstractions.Finance;

public interface IFinancialReportingService
{
    Task<Result<TrialBalanceReportDto>> GetTrialBalanceAsync(DateOnly? asOfDate, CancellationToken cancellationToken = default);
    Task<Result<AccountStatementReportDto>> GetAccountStatementAsync(int accountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
