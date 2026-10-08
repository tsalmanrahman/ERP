using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Finance;
using InteAcc.Common.Application.Abstractions.ProcureToPay;
using InteAcc.Common.Application.Models.Finance;
using InteAcc.Common.Application.Models.ProcureToPay;
using InteAcc.Common.Domain.Entities.AccountsPayable;
using InteAcc.Common.Domain.Entities.Inventory;
using InteAcc.Common.Domain.Entities.Procurement;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class PurchaseBillService : IPurchaseBillService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public PurchaseBillService(
        InteAccDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<List<PurchaseBillDto>> GetPurchaseBillsAsync(int? vendorId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from b in _dbContext.PurchaseBills.AsNoTracking()
                    where b.CompanyId == companyId
                    join v in _dbContext.Vendors.AsNoTracking() on b.VendorId equals v.Id
                    join po in _dbContext.PurchaseOrders.AsNoTracking() on b.PoId equals po.Id into poJoin
                    from po in poJoin.DefaultIfEmpty()
                    join grn in _dbContext.GoodsReceiptNotes.AsNoTracking() on b.GrnId equals grn.Id into grnJoin
                    from grn in grnJoin.DefaultIfEmpty()
                    join jv in _dbContext.JournalVouchers.AsNoTracking() on b.JournalVoucherId equals jv.Id into jvJoin
                    from jv in jvJoin.DefaultIfEmpty()
                    select new { b, v, po, grn, jv };

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.b.VendorId == vendorId.Value);
        }

        var list = await query.OrderByDescending(x => x.b.BillDate).ThenByDescending(x => x.b.Id).ToListAsync(cancellationToken);

        return list.Select(x => new PurchaseBillDto
        {
            Id = x.b.Id,
            BillNumber = x.b.BillNumber,
            BillDate = x.b.BillDate,
            VendorId = x.v.Id,
            VendorName = x.v.VendorName,
            VendorInvoiceNumber = x.b.VendorInvoiceNumber,
            PoId = x.b.PoId,
            PoNumber = x.po != null ? x.po.PoNumber : null,
            GrnId = x.b.GrnId,
            GrnNumber = x.grn != null ? x.grn.GrnNumber : null,
            SubTotal = x.b.SubTotal,
            TaxAmount = x.b.TaxAmount,
            TotalAmount = x.b.TotalAmount,
            PaidAmount = x.b.PaidAmount,
            BalanceAmount = x.b.BalanceAmount,
            Status = x.b.Status,
            DueDate = x.b.DueDate,
            JournalVoucherId = x.b.JournalVoucherId,
            JournalVoucherNumber = x.jv != null ? x.jv.VoucherNumber : null
        }).ToList();
    }

    public async Task<PurchaseBillDto?> GetPurchaseBillByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var bill = await _dbContext.PurchaseBills
            .Include(b => b.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == id, cancellationToken);

        if (bill == null) return null;

        var vendor = await _dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == bill.VendorId, cancellationToken);
        var po = bill.PoId.HasValue ? await _dbContext.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(p => p.Id == bill.PoId.Value, cancellationToken) : null;
        var grn = bill.GrnId.HasValue ? await _dbContext.GoodsReceiptNotes.AsNoTracking().FirstOrDefaultAsync(g => g.Id == bill.GrnId.Value, cancellationToken) : null;
        var jv = bill.JournalVoucherId.HasValue ? await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == bill.JournalVoucherId.Value, cancellationToken) : null;

        var productIds = bill.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new PurchaseBillDto
        {
            Id = bill.Id,
            BillNumber = bill.BillNumber,
            BillDate = bill.BillDate,
            VendorId = bill.VendorId,
            VendorName = vendor?.VendorName ?? string.Empty,
            VendorInvoiceNumber = bill.VendorInvoiceNumber,
            PoId = bill.PoId,
            PoNumber = po?.PoNumber,
            GrnId = bill.GrnId,
            GrnNumber = grn?.GrnNumber,
            SubTotal = bill.SubTotal,
            TaxAmount = bill.TaxAmount,
            TotalAmount = bill.TotalAmount,
            PaidAmount = bill.PaidAmount,
            BalanceAmount = bill.BalanceAmount,
            Status = bill.Status,
            DueDate = bill.DueDate,
            JournalVoucherId = bill.JournalVoucherId,
            JournalVoucherNumber = jv?.VoucherNumber,
            Lines = bill.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseBillLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var p) ? p.ProductCode : null,
                ProductName = l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var pr) ? pr.ProductName : null,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                TaxAmount = l.TaxAmount,
                ExpenseAccountId = l.ExpenseAccountId
            }).ToList()
        };
    }

    public async Task<PurchaseBillDto> CreatePurchaseBillAsync(CreatePurchaseBillRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new InvalidOperationException("A vendor purchase bill must contain at least one line item.");
        }

        var vendor = await _dbContext.Vendors.FirstOrDefaultAsync(v => v.CompanyId == companyId && v.Id == request.VendorId, cancellationToken);
        if (vendor == null)
        {
            throw new InvalidOperationException($"Vendor with ID {request.VendorId} was not found.");
        }

        var defaultExpAcct = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "5110", cancellationToken);
        int fallbackExpId = defaultExpAcct?.Id ?? 18;

        var billNumber = await GenerateNextBillNumberAsync(companyId, request.BillDate, cancellationToken);
        decimal subTotal = 0m;
        decimal totalTax = 0m;
        var billLines = new List<PurchaseBillLine>();

        int lineNo = 1;
        foreach (var reqLine in request.Lines)
        {
            if (reqLine.Quantity <= 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Billed quantity must be greater than zero.");
            }
            if (reqLine.UnitPrice < 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Unit price cannot be negative.");
            }

            var lineAmount = Math.Round(reqLine.Quantity * reqLine.UnitPrice, 4);
            subTotal += lineAmount;
            totalTax += reqLine.TaxAmount;

            billLines.Add(new PurchaseBillLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                Description = string.IsNullOrWhiteSpace(reqLine.Description) ? $"Line item {lineNo - 1}" : reqLine.Description.Trim(),
                Quantity = reqLine.Quantity,
                UnitPrice = reqLine.UnitPrice,
                LineAmount = lineAmount,
                TaxAmount = reqLine.TaxAmount,
                ExpenseAccountId = reqLine.ExpenseAccountId ?? fallbackExpId
            });
        }

        decimal totalAmount = subTotal + totalTax;
        var dueDate = request.DueDate ?? request.BillDate.AddDays(vendor.CreditPeriodDays > 0 ? vendor.CreditPeriodDays : 30);

        var bill = new PurchaseBill
        {
            CompanyId = companyId,
            BillNumber = billNumber,
            BillDate = request.BillDate.Date,
            VendorId = request.VendorId,
            VendorInvoiceNumber = request.VendorInvoiceNumber?.Trim(),
            PoId = request.PoId,
            GrnId = request.GrnId,
            SubTotal = subTotal,
            TaxAmount = totalTax,
            TotalAmount = totalAmount,
            PaidAmount = 0m,
            BalanceAmount = totalAmount,
            Status = PurchaseBillStatus.Draft,
            DueDate = dueDate.Date,
            Lines = billLines
        };

        _dbContext.PurchaseBills.Add(bill);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseBillByIdAsync(bill.Id, cancellationToken))!;
    }

    public async Task<ThreeWayMatchResultDto> PerformThreeWayMatchAsync(int billId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var bill = await _dbContext.PurchaseBills
            .Include(b => b.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == billId, cancellationToken);

        if (bill == null)
        {
            throw new InvalidOperationException($"Bill with ID {billId} was not found.");
        }

        PurchaseOrder? po = null;
        if (bill.PoId.HasValue)
        {
            po = await _dbContext.PurchaseOrders
                .Include(p => p.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == bill.PoId.Value, cancellationToken);
        }

        GoodsReceiptNote? grn = null;
        if (bill.GrnId.HasValue)
        {
            grn = await _dbContext.GoodsReceiptNotes
                .Include(g => g.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == bill.GrnId.Value, cancellationToken);
        }

        var productIds = bill.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var lineResults = new List<ThreeWayMatchLineResult>();
        bool isFullyMatched = true;
        decimal totalPoAmt = po?.TotalAmount ?? 0m;
        decimal totalGrnAmt = grn?.TotalAmount ?? 0m;

        foreach (var bLine in bill.Lines)
        {
            var pCode = bLine.ProductId.HasValue && products.TryGetValue(bLine.ProductId.Value, out var prod) ? prod.ProductCode : "N/A";
            
            decimal poQty = 0m;
            decimal poPrice = 0m;
            if (po != null && bLine.ProductId.HasValue)
            {
                var poLine = po.Lines.FirstOrDefault(pl => pl.ProductId == bLine.ProductId.Value);
                if (poLine != null)
                {
                    poQty = poLine.OrderedQuantity;
                    poPrice = poLine.UnitPrice;
                }
            }

            decimal grnQty = 0m;
            decimal grnPrice = 0m;
            if (grn != null && bLine.ProductId.HasValue)
            {
                var grnLine = grn.Lines.FirstOrDefault(gl => gl.ProductId == bLine.ProductId.Value);
                if (grnLine != null)
                {
                    grnQty = grnLine.QuantityReceived;
                    grnPrice = grnLine.UnitPrice;
                }
            }

            bool qtyOk = grn != null ? (bLine.Quantity <= grnQty) : (po != null ? (bLine.Quantity <= poQty) : true);
            bool priceOk = po != null ? (bLine.UnitPrice == poPrice) : (grn != null ? (bLine.UnitPrice == grnPrice) : true);

            string status = "MATCHED";
            string? notes = null;

            if (!qtyOk && !priceOk)
            {
                status = "VARIANCE_BOTH";
                notes = $"Quantity exceeds received ({bLine.Quantity} > {grnQty}) and price differs ({bLine.UnitPrice} != {poPrice})";
                isFullyMatched = false;
            }
            else if (!qtyOk)
            {
                status = "QUANTITY_VARIANCE";
                notes = $"Billed quantity ({bLine.Quantity}) exceeds physical received quantity ({grnQty})";
                isFullyMatched = false;
            }
            else if (!priceOk)
            {
                status = "PRICE_VARIANCE";
                notes = $"Billed unit price ({bLine.UnitPrice}) differs from Purchase Order ({poPrice})";
                isFullyMatched = false;
            }

            lineResults.Add(new ThreeWayMatchLineResult
            {
                LineNumber = bLine.LineNumber,
                ProductId = bLine.ProductId,
                ProductCode = pCode,
                PoQuantity = poQty,
                GrnQuantity = grnQty,
                BilledQuantity = bLine.Quantity,
                PoUnitPrice = poPrice,
                GrnUnitPrice = grnPrice,
                BilledUnitPrice = bLine.UnitPrice,
                QuantityMatches = qtyOk,
                PriceMatches = priceOk,
                Status = status,
                DiscrepancyNotes = notes
            });
        }

        return new ThreeWayMatchResultDto
        {
            BillId = bill.Id,
            BillNumber = bill.BillNumber,
            PoNumber = po?.PoNumber,
            GrnNumber = grn?.GrnNumber,
            IsFullyMatched = isFullyMatched,
            TotalPoAmount = totalPoAmt,
            TotalGrnAmount = totalGrnAmt,
            TotalBilledAmount = bill.TotalAmount,
            PriceVariance = bill.TotalAmount - totalGrnAmt,
            Lines = lineResults
        };
    }

    public async Task<PurchaseBillDto> ConfirmAndPostBillAsync(int billId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var bill = await _dbContext.PurchaseBills
            .Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == billId, cancellationToken);

        if (bill == null)
        {
            throw new InvalidOperationException($"Bill with ID {billId} was not found.");
        }

        if (bill.Status != PurchaseBillStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft bills can be confirmed. Current status: {bill.Status}");
        }

        var vendor = await _dbContext.Vendors.FirstOrDefaultAsync(v => v.CompanyId == companyId && v.Id == bill.VendorId, cancellationToken);
        if (vendor == null)
        {
            throw new InvalidOperationException($"Vendor with ID {bill.VendorId} was not found.");
        }

        // Resolving GL Accounts:
        // AP Control Account (2110)
        var apAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == vendor.GlControlAccountId, cancellationToken)
            ?? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2110", cancellationToken);

        if (apAccount == null)
        {
            throw new InvalidOperationException("AP Control Account (2110) was not found.");
        }

        // GRN Clearing Account (2140) if GRN linked, else standard expense account
        var grnClearingAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2140", cancellationToken);

        var taxAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2130", cancellationToken);

        var jvLines = new List<CreateVoucherLineRequest>();

        if (bill.GrnId.HasValue && grnClearingAccount != null)
        {
            // Debiting GRN Clearing account (clearing the accrued liability!)
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: grnClearingAccount.Id,
                Debit: bill.SubTotal,
                Credit: 0m,
                Particulars: $"Clearing accrued GRN liability for bill {bill.BillNumber}"
            ));
        }
        else
        {
            // Direct bill: Debit Expense Account(s)
            foreach (var line in bill.Lines)
            {
                jvLines.Add(new CreateVoucherLineRequest(
                    AccountId: line.ExpenseAccountId,
                    Debit: line.LineAmount,
                    Credit: 0m,
                    Particulars: $"{line.Description} (Bill {bill.BillNumber})"
                ));
            }
        }

        // Debiting Tax/VAT if present
        if (bill.TaxAmount > 0 && taxAccount != null)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: taxAccount.Id,
                Debit: bill.TaxAmount,
                Credit: 0m,
                Particulars: $"Recoverable input VAT for bill {bill.BillNumber}"
            ));
        }

        // Crediting Trade Accounts Payable (Vendor liability)
        jvLines.Add(new CreateVoucherLineRequest(
            AccountId: apAccount.Id,
            Debit: 0m,
            Credit: bill.TotalAmount,
            Particulars: $"Vendor liability to {vendor.VendorName} ({bill.BillNumber})"
        ));

        // Create and post balanced GL Journal Voucher
        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(bill.BillDate),
            Reference: bill.BillNumber,
            Description: $"Vendor Bill: {bill.BillNumber} - {vendor.VendorName} (Inv: {bill.VendorInvoiceNumber ?? "N/A"})",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to generate GL journal voucher for bill: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for bill: {postResult.Error}");
        }

        // Update Bill state and Vendor outstanding balance
        bill.Status = PurchaseBillStatus.Confirmed;
        bill.BalanceAmount = bill.TotalAmount;
        bill.JournalVoucherId = createResult.Value!.Id;

        vendor.CurrentBalance += bill.TotalAmount;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseBillByIdAsync(bill.Id, cancellationToken))!;
    }

    private async Task<string> GenerateNextBillNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        var prefix = $"BILL-{date:yyyyMM}-";
        var last = await _dbContext.PurchaseBills
            .Where(b => b.CompanyId == companyId && b.BillNumber.StartsWith(prefix))
            .OrderByDescending(b => b.BillNumber)
            .Select(b => b.BillNumber)
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
