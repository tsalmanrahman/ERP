using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.OrderToCash;

public class CustomerReceiptService : ICustomerReceiptService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IJournalVoucherService _journalVoucherService;

    public CustomerReceiptService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<CustomerReceiptDto>> GetCustomerReceiptsAsync(int? customerId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.CustomerReceipts
            .AsNoTracking()
            .Include(r => r.Allocations)
            .Where(r => r.CompanyId == companyId);

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        var receipts = await query.OrderByDescending(r => r.ReceiptDate).ThenByDescending(r => r.Id).ToListAsync(cancellationToken);
        if (!receipts.Any()) return Array.Empty<CustomerReceiptDto>();

        var customerIds = receipts.Select(r => r.CustomerId).Distinct().ToList();
        var invoiceIds = receipts.SelectMany(r => r.Allocations).Select(a => a.InvoiceId).Distinct().ToList();
        var jvIds = receipts.Where(r => r.JournalVoucherId.HasValue).Select(r => r.JournalVoucherId!.Value).Distinct().ToList();

        var customers = await _dbContext.Customers.AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var invoices = await _dbContext.Invoices.AsNoTracking().Where(i => invoiceIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        var jvs = await _dbContext.JournalVouchers.AsNoTracking().Where(j => jvIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, cancellationToken);

        return receipts.Select(r => new CustomerReceiptDto
        {
            Id = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            ReceiptDate = r.ReceiptDate,
            CustomerId = r.CustomerId,
            CustomerCode = customers.TryGetValue(r.CustomerId, out var c) ? c.CustomerCode : string.Empty,
            CustomerName = c != null ? c.CustomerName : string.Empty,
            PaymentMethod = r.PaymentMethod,
            BankAccountId = r.BankAccountId,
            BankAccountTitle = r.PaymentMethod == ReceiptPaymentMethod.Cash ? "Cash in Hand" : "Prime Bank Ltd.",
            CashBookId = r.CashBookId,
            ChequeNumber = r.ChequeNumber,
            ChequeDate = r.ChequeDate,
            Amount = r.Amount,
            Reference = r.Reference,
            Remarks = r.Remarks,
            Status = r.Status,
            JournalVoucherId = r.JournalVoucherId,
            JournalVoucherNumber = r.JournalVoucherId.HasValue && jvs.TryGetValue(r.JournalVoucherId.Value, out var jv) ? jv.VoucherNumber : null,
            Allocations = r.Allocations.Select(a => new CustomerReceiptAllocationDto
            {
                Id = a.Id,
                InvoiceId = a.InvoiceId,
                InvoiceNumber = invoices.TryGetValue(a.InvoiceId, out var inv) ? inv.InvoiceNumber : string.Empty,
                AllocatedAmount = a.AllocatedAmount
            }).ToList()
        }).ToList();
    }

    public async Task<CustomerReceiptDto?> GetCustomerReceiptByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var receipt = await _dbContext.CustomerReceipts
            .AsNoTracking()
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, cancellationToken);

        if (receipt == null) return null;

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == receipt.CustomerId, cancellationToken);

        string? jvNumber = null;
        if (receipt.JournalVoucherId.HasValue)
        {
            var jv = await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(j => j.Id == receipt.JournalVoucherId.Value, cancellationToken);
            jvNumber = jv?.VoucherNumber;
        }

        var invIds = receipt.Allocations.Select(a => a.InvoiceId).Distinct().ToList();
        var invoices = await _dbContext.Invoices.AsNoTracking().Where(i => invIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);

        return new CustomerReceiptDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            ReceiptDate = receipt.ReceiptDate,
            CustomerId = receipt.CustomerId,
            CustomerCode = customer?.CustomerCode ?? string.Empty,
            CustomerName = customer?.CustomerName ?? string.Empty,
            PaymentMethod = receipt.PaymentMethod,
            BankAccountId = receipt.BankAccountId,
            BankAccountTitle = receipt.PaymentMethod == ReceiptPaymentMethod.Cash ? "Cash in Hand" : "Prime Bank Ltd.",
            CashBookId = receipt.CashBookId,
            ChequeNumber = receipt.ChequeNumber,
            ChequeDate = receipt.ChequeDate,
            Amount = receipt.Amount,
            Reference = receipt.Reference,
            Remarks = receipt.Remarks,
            Status = receipt.Status,
            JournalVoucherId = receipt.JournalVoucherId,
            JournalVoucherNumber = jvNumber,
            Allocations = receipt.Allocations.Select(a => new CustomerReceiptAllocationDto
            {
                Id = a.Id,
                InvoiceId = a.InvoiceId,
                InvoiceNumber = invoices.TryGetValue(a.InvoiceId, out var inv) ? inv.InvoiceNumber : string.Empty,
                AllocatedAmount = a.AllocatedAmount
            }).ToList()
        };
    }

    public async Task<CustomerReceiptDto> ProcessCustomerReceiptAsync(CreateCustomerReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Receipt amount must be greater than zero.");
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} was not found.");
        }

        // Validate allocations
        decimal totalAllocated = request.Allocations.Sum(a => a.AllocatedAmount);
        if (totalAllocated > request.Amount)
        {
            throw new InvalidOperationException($"Total allocated amount ({totalAllocated:C}) cannot exceed receipt amount ({request.Amount:C}).");
        }

        // 1. Determine GL Accounts
        // Credit: Customer Control Account (1130 Trade AR)
        int arAccountId = customer.GlControlAccountId;
        if (arAccountId == 0)
        {
            var defaultAr = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1130", cancellationToken);
            arAccountId = defaultAr?.Id ?? 5;
        }

        // Debit: Bank or Cash Account
        int receivingGlAccountId;
        string paymentSourceDesc;

        if (request.PaymentMethod == ReceiptPaymentMethod.Cash)
        {
            var defaultCash = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1111", cancellationToken);
            receivingGlAccountId = defaultCash?.Id ?? 1;
            paymentSourceDesc = "Cash in Hand (1111)";
        }
        else // BankTransfer or Cheque
        {
            var defaultBank = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1120", cancellationToken);
            receivingGlAccountId = defaultBank?.Id ?? 3;
            paymentSourceDesc = "Prime Bank Current Account (1120)";
        }

        // 2. Generate Receipt Number
        var prefix = $"RCPT-{_dateTimeProvider.UtcNow:yyyyMM}-";
        var countThisMonth = await _dbContext.CustomerReceipts
            .CountAsync(r => r.CompanyId == companyId && r.ReceiptNumber.StartsWith(prefix), cancellationToken);
        var receiptNumber = $"{prefix}{(countThisMonth + 1):D4}";

        var receipt = new CustomerReceipt
        {
            CompanyId = companyId,
            ReceiptNumber = receiptNumber,
            ReceiptDate = request.ReceiptDate,
            CustomerId = customer.Id,
            PaymentMethod = request.PaymentMethod,
            BankAccountId = request.BankAccountId ?? (request.PaymentMethod != ReceiptPaymentMethod.Cash ? 1 : null),
            CashBookId = request.CashBookId,
            ChequeNumber = request.ChequeNumber,
            ChequeDate = request.ChequeDate,
            Amount = request.Amount,
            Reference = request.Reference,
            Remarks = request.Remarks,
            Status = CustomerReceiptStatus.Posted
        };

        // 3. Process Allocations against Invoices
        foreach (var allocReq in request.Allocations)
        {
            if (allocReq.AllocatedAmount <= 0) continue;

            var invoice = await _dbContext.Invoices
                .FirstOrDefaultAsync(i => i.Id == allocReq.InvoiceId && i.CompanyId == companyId, cancellationToken);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Invoice with ID {allocReq.InvoiceId} was not found.");
            }

            if (allocReq.AllocatedAmount > invoice.BalanceAmount)
            {
                throw new InvalidOperationException(
                    $"Allocated amount ({allocReq.AllocatedAmount:C}) exceeds outstanding balance ({invoice.BalanceAmount:C}) for invoice {invoice.InvoiceNumber}.");
            }

            invoice.PaidAmount += allocReq.AllocatedAmount;
            invoice.BalanceAmount = invoice.TotalAmount - invoice.PaidAmount;
            invoice.Status = invoice.BalanceAmount <= 0 ? InvoiceStatus.FullyPaid : InvoiceStatus.PartiallyPaid;

            receipt.Allocations.Add(new CustomerReceiptAllocation
            {
                InvoiceId = invoice.Id,
                AllocatedAmount = allocReq.AllocatedAmount
            });
        }

        // 4. Update Customer Balance
        customer.CurrentBalance -= request.Amount;

        // 5. Automated Double-Entry GL Journal Voucher
        // Debit: Bank / Cash Asset Account = request.Amount
        // Credit: Trade Accounts Receivable Control (1130) = request.Amount
        var jvLines = new List<CreateVoucherLineRequest>
        {
            new CreateVoucherLineRequest(
                AccountId: receivingGlAccountId,
                Debit: request.Amount,
                Credit: 0m,
                Particulars: $"Customer collection via {paymentSourceDesc} ({receipt.ReceiptNumber})"
            ),
            new CreateVoucherLineRequest(
                AccountId: arAccountId,
                Debit: 0m,
                Credit: request.Amount,
                Particulars: $"Receivable reduction for {customer.CustomerName} ({receipt.ReceiptNumber})"
            )
        };

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(receipt.ReceiptDate),
            Reference: receipt.ReceiptNumber,
            Description: $"Automated GL Interface: Customer Receipt {receipt.ReceiptNumber} - {customer.CustomerCode}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create GL journal voucher for Customer Receipt: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for Customer Receipt: {postResult.Error}");
        }

        receipt.JournalVoucherId = createResult.Value!.Id;

        _dbContext.CustomerReceipts.Add(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetCustomerReceiptByIdAsync(receipt.Id, cancellationToken))!;
    }
}
