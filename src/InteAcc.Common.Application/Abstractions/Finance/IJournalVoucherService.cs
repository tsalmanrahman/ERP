using InteAcc.Common.Application.Models.Finance;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Application.Abstractions.Finance;

public interface IJournalVoucherService
{
    Task<Result<JournalVoucherDto>> CreateVoucherAsync(CreateJournalVoucherRequest request, CancellationToken cancellationToken = default);
    Task<Result<JournalVoucherDto>> PostVoucherAsync(int voucherId, CancellationToken cancellationToken = default);
    Task<Result<List<JournalVoucherSummaryDto>>> GetVouchersAsync(int? fiscalPeriodId, bool? isPosted, CancellationToken cancellationToken = default);
    Task<Result<JournalVoucherDto>> GetVoucherByIdAsync(int voucherId, CancellationToken cancellationToken = default);
}
