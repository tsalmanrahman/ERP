using InteAcc.Common.Application.Models.Finance;
using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Application.Abstractions.Finance;

public interface IBankingService
{
    Task<Result<List<BankAccountDto>>> GetBankAccountsAsync(CancellationToken cancellationToken = default);
    Task<Result<List<CashBookDto>>> GetCashBooksAsync(CancellationToken cancellationToken = default);
    Task<Result<JournalVoucherDto>> ProcessContraTransferAsync(ContraTransferRequest request, CancellationToken cancellationToken = default);
}
