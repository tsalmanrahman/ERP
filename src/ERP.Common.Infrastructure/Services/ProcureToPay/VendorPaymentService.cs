using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.ProcureToPay;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.ProcureToPay;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.ProcureToPay;

public class VendorPaymentService : IVendorPaymentService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public VendorPaymentService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<List<VendorPaymentDto>> GetVendorPaymentsAsync(int? vendorId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from p in _dbContext.VendorPayments.AsNoTracking()
                    where p.CompanyId == companyId
                    join v in _dbContext.Vendors.AsNoTracking() on p.VendorId equals v.Id
                    join jv in _dbContext.JournalVouchers.AsNoTracking() on p.JournalVoucherId equals jv.Id into jvJoin
                    from jv in jvJoin.DefaultIfEmpty()
                    select new { p, v, jv };

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.p.VendorId == vendorId.Value);
        }

        var list = await query.OrderByDescending(x => x.p.PaymentDate).ThenByDescending(x => x.p.Id).ToListAsync(cancellationToken);

        return list.Select(x => new VendorPaymentDto
        {
            Id = x.p.Id,
            PaymentNumber = x.p.PaymentNumber,
            PaymentDate = x.p.PaymentDate,
            VendorId = x.v.Id,
            VendorName = x.v.VendorName,
            PaymentMethod = x.p.PaymentMethod,
            BankAccountId = x.p.BankAccountId,
            CashBookId = x.p.CashBookId,
            ChequeNumber = x.p.ChequeNumber,
            ChequeDate = x.p.ChequeDate,
            Amount = x.p.Amount,
            Reference = x.p.Reference,
            Remarks = x.p.Remarks,
            Status = x.p.Status,
            JournalVoucherId = x.p.JournalVoucherId,
            JournalVoucherNumber = x.jv != null ? x.jv.VoucherNumber : null
        }).ToList();
    }

    public async Task<VendorPaymentDto?> GetVendorPaymentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var payment = await _dbContext.VendorPayments
            .Include(p => p.Allocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, cancellationToken);

        if (payment == null) return null;

        var vendor = await _dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == payment.VendorId, cancellationToken);
        var jv = payment.JournalVoucherId.HasValue ? await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == payment.JournalVoucherId.Value, cancellationToken) : null;

        var billIds = payment.Allocations.Select(a => a.BillId).Distinct().ToList();
        var bills = await _dbContext.PurchaseBills
            .Where(b => billIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        return new VendorPaymentDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            PaymentDate = payment.PaymentDate,
            VendorId = payment.VendorId,
            VendorName = vendor?.VendorName ?? string.Empty,
            PaymentMethod = payment.PaymentMethod,
            BankAccountId = payment.BankAccountId,
            CashBookId = payment.CashBookId,
            ChequeNumber = payment.ChequeNumber,
            ChequeDate = payment.ChequeDate,
            Amount = payment.Amount,
            Reference = payment.Reference,
            Remarks = payment.Remarks,
            Status = payment.Status,
            JournalVoucherId = payment.JournalVoucherId,
            JournalVoucherNumber = jv?.VoucherNumber,
            Allocations = payment.Allocations.Select(a => new VendorPaymentAllocationDto
            {
                Id = a.Id,
                BillId = a.BillId,
                BillNumber = bills.TryGetValue(a.BillId, out var b) ? b.BillNumber : string.Empty,
                AllocatedAmount = a.AllocatedAmount
            }).ToList()
        };
    }

    public async Task<VendorPaymentDto> ProcessVendorPaymentAsync(CreateVendorPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Payment amount must be greater than zero.");
        }

        var vendor = await _dbContext.Vendors.FirstOrDefaultAsync(v => v.CompanyId == companyId && v.Id == request.VendorId, cancellationToken);
        if (vendor == null)
        {
            throw new InvalidOperationException($"Vendor with ID {request.VendorId} was not found.");
        }

        // 1. Resolve Disbursing Source GL Account
        int disbursingGlAccountId;
        string sourceName;

        if (request.PaymentMethod == PaymentMethod.Cash)
        {
            var defaultCash = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1111", cancellationToken);
            disbursingGlAccountId = defaultCash?.Id ?? 1;
            sourceName = "Cash in Hand";
        }
        else // BankTransfer or Cheque
        {
            int baId = request.BankAccountId ?? 1;
            var bankAccount = await _dbContext.Set<ERP.Common.Domain.Entities.GeneralLedger.Account>()
                .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1120", cancellationToken);
            
            disbursingGlAccountId = bankAccount?.Id ?? 3;
            sourceName = "Prime Bank Current Account";
        }

        // 2. Resolve Vendor AP Control Account
        var apAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == vendor.GlControlAccountId, cancellationToken)
            ?? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2110", cancellationToken);

        if (apAccount == null)
        {
            throw new InvalidOperationException("Trade Accounts Payable Control account (2110) was not found.");
        }

        var paymentNumber = await GenerateNextPaymentNumberAsync(companyId, request.PaymentDate, cancellationToken);

        // 3. Automated Double-Entry GL Journal Voucher
        // Debit: Accounts Payable (2110)
        // Credit: Bank / Cash (1120 / 1111)
        var jvLines = new List<CreateVoucherLineRequest>
        {
            new(
                AccountId: apAccount.Id,
                Debit: request.Amount,
                Credit: 0m,
                Particulars: $"Vendor payment to {vendor.VendorName} ({paymentNumber})"
            ),
            new(
                AccountId: disbursingGlAccountId,
                Debit: 0m,
                Credit: request.Amount,
                Particulars: $"Disbursement from {sourceName} ({paymentNumber})"
            )
        };

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(request.PaymentDate),
            Reference: paymentNumber,
            Description: $"Payment Disbursement: {paymentNumber} to {vendor.VendorName} via {request.PaymentMethod}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to generate GL journal voucher for payment: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for payment: {postResult.Error}");
        }

        // 4. Update Bill Allocations
        var allocations = new List<VendorPaymentAllocation>();
        if (request.Allocations != null && request.Allocations.Count > 0)
        {
            var billIds = request.Allocations.Select(a => a.BillId).ToList();
            var bills = await _dbContext.PurchaseBills
                .Where(b => b.CompanyId == companyId && billIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            foreach (var allocReq in request.Allocations)
            {
                var bill = bills.FirstOrDefault(b => b.Id == allocReq.BillId);
                if (bill != null && allocReq.AllocatedAmount > 0)
                {
                    bill.PaidAmount += allocReq.AllocatedAmount;
                    bill.BalanceAmount = Math.Max(0m, bill.BalanceAmount - allocReq.AllocatedAmount);
                    bill.Status = bill.BalanceAmount <= 0 ? PurchaseBillStatus.FullyPaid : PurchaseBillStatus.PartiallyPaid;

                    allocations.Add(new VendorPaymentAllocation
                    {
                        BillId = bill.Id,
                        AllocatedAmount = allocReq.AllocatedAmount
                    });
                }
            }
        }

        // 5. Deduct Vendor Outstanding Balance
        vendor.CurrentBalance = Math.Max(0m, vendor.CurrentBalance - request.Amount);

        // 6. Record Payment
        var payment = new VendorPayment
        {
            CompanyId = companyId,
            PaymentNumber = paymentNumber,
            PaymentDate = request.PaymentDate.Date,
            VendorId = request.VendorId,
            PaymentMethod = request.PaymentMethod,
            BankAccountId = request.BankAccountId,
            CashBookId = request.CashBookId,
            ChequeNumber = request.ChequeNumber?.Trim(),
            ChequeDate = request.ChequeDate?.Date,
            Amount = request.Amount,
            Reference = request.Reference?.Trim(),
            Remarks = request.Remarks?.Trim(),
            Status = PaymentStatus.Posted,
            JournalVoucherId = createResult.Value!.Id,
            Allocations = allocations
        };

        _dbContext.VendorPayments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetVendorPaymentByIdAsync(payment.Id, cancellationToken))!;
    }

    private async Task<string> GenerateNextPaymentNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        var prefix = $"PAY-{date:yyyyMM}-";
        var last = await _dbContext.VendorPayments
            .Where(p => p.CompanyId == companyId && p.PaymentNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PaymentNumber)
            .Select(p => p.PaymentNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int seq = 1;
        if (!string.IsNullOrEmpty(last) && last.Length >= prefix.Length + 4)
        {
            if (int.TryParse(last.Substring(prefix.Length), out var parsed))
            {
                seq = parsed + 1;
            }
        }
        return $"{prefix}{seq:D4}";
    }
}
