using ERP.Common.Application.Models.Migration;

namespace ERP.Common.Application.Abstractions.Migration;

public interface IFinancialParityAuditService
{
    Task<FinancialParityAuditReport> RunFinancialParityAuditAsync(int companyId, CancellationToken cancellationToken = default);
}
