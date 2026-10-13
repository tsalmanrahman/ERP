using InteAcc.Common.Application.Models.Migration;

namespace InteAcc.Common.Application.Abstractions.Migration;

public interface IFinancialParityAuditService
{
    Task<FinancialParityAuditReport> RunFinancialParityAuditAsync(int companyId, CancellationToken cancellationToken = default);
}
