namespace ERP.Common.Application.Models.Finance;

public record CreateJournalVoucherRequest(
    DateOnly VoucherDate,
    string? Reference,
    string? Description,
    int? FiscalPeriodId,
    List<CreateVoucherLineRequest> Lines
);

public record CreateVoucherLineRequest(
    int AccountId,
    decimal Debit,
    decimal Credit,
    string? Particulars
);

public record JournalVoucherDto(
    int Id,
    int CompanyId,
    string VoucherNumber,
    DateOnly VoucherDate,
    string? Reference,
    string? Description,
    int FiscalYearId,
    int FiscalPeriodId,
    bool IsPosted,
    DateTime? PostedAtUtc,
    string? PostedBy,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced,
    List<JournalVoucherLineDto> Lines
);

public record JournalVoucherLineDto(
    int Id,
    int LineNumber,
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? Particulars
);

public record JournalVoucherSummaryDto(
    int Id,
    string VoucherNumber,
    DateOnly VoucherDate,
    string? Reference,
    string? Description,
    bool IsPosted,
    decimal TotalDebit,
    decimal TotalCredit,
    int LineCount
);

public record TrialBalanceReportDto(
    DateOnly AsOfDate,
    int CompanyId,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    bool IsBalanced,
    List<TrialBalanceLineDto> Lines
);

public record TrialBalanceLineDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    string GroupName,
    string CategoryName,
    decimal Debit,
    decimal Credit
);

public record AccountStatementReportDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    List<AccountStatementLineDto> Transactions
);

public record AccountStatementLineDto(
    DateOnly Date,
    string VoucherNumber,
    string? Reference,
    string? Particulars,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance
);

public record BankAccountDto(
    int Id,
    string BankCode,
    string BankName,
    string BranchName,
    string AccountNumber,
    string AccountTitle,
    string CurrencyCode,
    int GlAccountId,
    string GlAccountCode,
    decimal CurrentBalance,
    bool IsActive
);

public record CashBookDto(
    int Id,
    string CashBookCode,
    string CashBookName,
    int GlAccountId,
    string GlAccountCode,
    decimal CurrentBalance,
    bool IsActive
);

public record ContraTransferRequest(
    int SourceType, // 1 = Cash, 2 = Bank
    int SourceId,   // CashBookId or BankAccountId
    int DestinationType, // 1 = Cash, 2 = Bank
    int DestinationId,
    decimal Amount,
    DateOnly TransferDate,
    string? Reference,
    string? Description
);
