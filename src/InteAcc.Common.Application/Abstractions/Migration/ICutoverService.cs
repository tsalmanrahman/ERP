using InteAcc.Common.Application.Models.Migration;

namespace InteAcc.Common.Application.Abstractions.Migration;

public interface ICutoverService
{
    Task<CutoverChecklistResponse> EvaluateCutoverChecklistAsync(int companyId, CancellationToken cancellationToken = default);
    Task<CutoverSignoffResult> ExecuteCutoverSignoffAsync(CutoverSignoffRequest request, CancellationToken cancellationToken = default);
}
