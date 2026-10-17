using Dapper;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Data;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Domain.Common;

namespace ERP.Common.Infrastructure.Services.Finance;

public class BankingService : IBankingService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public BankingService(
        ISqlConnectionFactory sqlConnectionFactory,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    public async Task<Result<List<BankAccountDto>>> GetBankAccountsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.CompanyId ?? 1;
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                ba.Id,
                b.BankCode,
                b.BankName,
                bb.BranchName,
                ba.AccountNumber,
                ba.AccountTitle,
                ba.CurrencyCode,
                ba.GlAccountId,
                a.AccountCode AS GlAccountCode,
                ba.CurrentBalance,
                ba.IsActive
            FROM bc.BankAccounts ba
            INNER JOIN bc.BankBranches bb ON ba.BranchId = bb.Id
            INNER JOIN bc.Banks b ON bb.BankId = b.Id
            INNER JOIN gl.Accounts a ON ba.GlAccountId = a.Id
            WHERE ba.CompanyId = @CompanyId AND ba.IsDeleted = 0
            ORDER BY b.BankCode, ba.AccountNumber;";

        var list = (await connection.QueryAsync<BankAccountDto>(
            new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken))).ToList();

        return Result.Success(list);
    }

    public async Task<Result<List<CashBookDto>>> GetCashBooksAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.CompanyId ?? 1;
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                cb.Id,
                cb.CashBookCode,
                cb.CashBookName,
                cb.GlAccountId,
                a.AccountCode AS GlAccountCode,
                cb.CurrentBalance,
                cb.IsActive
            FROM bc.CashBooks cb
            INNER JOIN gl.Accounts a ON cb.GlAccountId = a.Id
            WHERE cb.CompanyId = @CompanyId AND cb.IsDeleted = 0
            ORDER BY cb.CashBookCode;";

        var list = (await connection.QueryAsync<CashBookDto>(
            new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken))).ToList();

        return Result.Success(list);
    }

    public async Task<Result<JournalVoucherDto>> ProcessContraTransferAsync(ContraTransferRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserContext.CompanyId ?? 1;

        if (request.Amount <= 0)
        {
            return Result.Failure<JournalVoucherDto>("Transfer amount must be strictly greater than zero.");
        }

        if (request.SourceType == request.DestinationType && request.SourceId == request.DestinationId)
        {
            return Result.Failure<JournalVoucherDto>("Source and destination accounts must be different.");
        }

        using var connection = _sqlConnectionFactory.CreateConnection();

        // 1. Resolve Source GL Account
        int sourceGlAccountId;
        string sourceName;
        if (request.SourceType == 1) // Cash
        {
            var cb = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT GlAccountId, CashBookName FROM bc.CashBooks WHERE Id = @Id AND CompanyId = @CompanyId",
                new { Id = request.SourceId, CompanyId = companyId });
            if (cb == null) return Result.Failure<JournalVoucherDto>("Source cash book not found.");
            sourceGlAccountId = (int)cb.GlAccountId;
            sourceName = (string)cb.CashBookName;
        }
        else // Bank
        {
            var ba = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT GlAccountId, AccountTitle FROM bc.BankAccounts WHERE Id = @Id AND CompanyId = @CompanyId",
                new { Id = request.SourceId, CompanyId = companyId });
            if (ba == null) return Result.Failure<JournalVoucherDto>("Source bank account not found.");
            sourceGlAccountId = (int)ba.GlAccountId;
            sourceName = (string)ba.AccountTitle;
        }

        // 2. Resolve Destination GL Account
        int destGlAccountId;
        string destName;
        if (request.DestinationType == 1) // Cash
        {
            var cb = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT GlAccountId, CashBookName FROM bc.CashBooks WHERE Id = @Id AND CompanyId = @CompanyId",
                new { Id = request.DestinationId, CompanyId = companyId });
            if (cb == null) return Result.Failure<JournalVoucherDto>("Destination cash book not found.");
            destGlAccountId = (int)cb.GlAccountId;
            destName = (string)cb.CashBookName;
        }
        else // Bank
        {
            var ba = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT GlAccountId, AccountTitle FROM bc.BankAccounts WHERE Id = @Id AND CompanyId = @CompanyId",
                new { Id = request.DestinationId, CompanyId = companyId });
            if (ba == null) return Result.Failure<JournalVoucherDto>("Destination bank account not found.");
            destGlAccountId = (int)ba.GlAccountId;
            destName = (string)ba.AccountTitle;
        }

        // 3. Construct Balanced Journal Voucher
        var voucherRequest = new CreateJournalVoucherRequest(
            VoucherDate: request.TransferDate,
            Reference: request.Reference ?? "CONTRA-TRANSFER",
            Description: request.Description ?? $"Contra Transfer from {sourceName} to {destName}",
            FiscalPeriodId: null,
            Lines: new List<CreateVoucherLineRequest>
            {
                // Debit Destination (Receiving account)
                new(
                    AccountId: destGlAccountId,
                    Debit: request.Amount,
                    Credit: 0m,
                    Particulars: $"Received transfer from {sourceName}"
                ),
                // Credit Source (Disbursing account)
                new(
                    AccountId: sourceGlAccountId,
                    Debit: 0m,
                    Credit: request.Amount,
                    Particulars: $"Transferred out to {destName}"
                )
            }
        );

        // 4. Create and Post immediately
        var createResult = await _journalVoucherService.CreateVoucherAsync(voucherRequest, cancellationToken);
        if (createResult.IsFailure)
        {
            return createResult;
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value.Id, cancellationToken);
        return postResult;
    }
}
