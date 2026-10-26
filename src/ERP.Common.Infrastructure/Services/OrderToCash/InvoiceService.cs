using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Domain.Entities.AccountsReceivable;
using ERP.Common.Domain.Entities.Sales;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.OrderToCash;

public class InvoiceService : IInvoiceService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IJournalVoucherService _journalVoucherService;

    public InvoiceService(
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

    private int GetCompanyId() => _currentUserContext.GetRequiredCompanyId();

    public async Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(int? customerId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.CompanyId == companyId);

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync(cancellationToken);
        if (!invoices.Any()) return Array.Empty<InvoiceDto>();

        var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
        var soIds = invoices.Where(i => i.SalesOrderId.HasValue).Select(i => i.SalesOrderId!.Value).Distinct().ToList();
        var dcIds = invoices.Where(i => i.DeliveryChallanId.HasValue).Select(i => i.DeliveryChallanId!.Value).Distinct().ToList();
        var productIds = invoices.SelectMany(i => i.Lines).Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var jvIds = invoices.Where(i => i.JournalVoucherId.HasValue).Select(i => i.JournalVoucherId!.Value).Distinct().ToList();

        var customers = await _dbContext.Customers.AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var orders = await _dbContext.SalesOrders.AsNoTracking().Where(o => soIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken);
        var challans = await _dbContext.DeliveryChallans.AsNoTracking().Where(d => dcIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, cancellationToken);
        var products = await _dbContext.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var jvs = await _dbContext.JournalVouchers.AsNoTracking().Where(j => jvIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, cancellationToken);

        return invoices.Select(i => new InvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            InvoiceDate = i.InvoiceDate,
            CustomerId = i.CustomerId,
            CustomerCode = customers.TryGetValue(i.CustomerId, out var c) ? c.CustomerCode : string.Empty,
            CustomerName = c != null ? c.CustomerName : string.Empty,
            SalesOrderId = i.SalesOrderId,
            OrderNumber = i.SalesOrderId.HasValue && orders.TryGetValue(i.SalesOrderId.Value, out var so) ? so.OrderNumber : null,
            DeliveryChallanId = i.DeliveryChallanId,
            ChallanNumber = i.DeliveryChallanId.HasValue && challans.TryGetValue(i.DeliveryChallanId.Value, out var dc) ? dc.ChallanNumber : null,
            SubTotal = i.SubTotal,
            DiscountAmount = i.DiscountAmount,
            TaxAmount = i.TaxAmount,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            BalanceAmount = i.BalanceAmount,
            Status = i.Status,
            DueDate = i.DueDate,
            JournalVoucherId = i.JournalVoucherId,
            JournalVoucherNumber = i.JournalVoucherId.HasValue && jvs.TryGetValue(i.JournalVoucherId.Value, out var jv) ? jv.VoucherNumber : null,
            Lines = i.Lines.OrderBy(l => l.LineNumber).Select(l => new InvoiceLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var prod) ? prod.ProductCode : null,
                ProductName = l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var prod2) ? prod2.ProductName : null,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                TaxAmount = l.TaxAmount,
                RevenueAccountId = l.RevenueAccountId
            }).ToList()
        }).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId, cancellationToken);

        if (invoice == null) return null;

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, cancellationToken);

        string? soNumber = null;
        if (invoice.SalesOrderId.HasValue)
        {
            var so = await _dbContext.SalesOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == invoice.SalesOrderId.Value, cancellationToken);
            soNumber = so?.OrderNumber;
        }

        string? dcNumber = null;
        if (invoice.DeliveryChallanId.HasValue)
        {
            var dc = await _dbContext.DeliveryChallans.AsNoTracking().FirstOrDefaultAsync(d => d.Id == invoice.DeliveryChallanId.Value, cancellationToken);
            dcNumber = dc?.ChallanNumber;
        }

        string? jvNumber = null;
        if (invoice.JournalVoucherId.HasValue)
        {
            var jv = await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(j => j.Id == invoice.JournalVoucherId.Value, cancellationToken);
            jvNumber = jv?.VoucherNumber;
        }

        var productIds = invoice.Lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            CustomerCode = customer?.CustomerCode ?? string.Empty,
            CustomerName = customer?.CustomerName ?? string.Empty,
            SalesOrderId = invoice.SalesOrderId,
            OrderNumber = soNumber,
            DeliveryChallanId = invoice.DeliveryChallanId,
            ChallanNumber = dcNumber,
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            BalanceAmount = invoice.BalanceAmount,
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            JournalVoucherId = invoice.JournalVoucherId,
            JournalVoucherNumber = jvNumber,
            Lines = invoice.Lines.OrderBy(l => l.LineNumber).Select(l => new InvoiceLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var prod) ? prod.ProductCode : null,
                ProductName = l.ProductId.HasValue && products.TryGetValue(l.ProductId.Value, out var prod2) ? prod2.ProductName : null,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount,
                TaxAmount = l.TaxAmount,
                RevenueAccountId = l.RevenueAccountId
            }).ToList()
        };
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} was not found.");
        }

        if (request.Lines == null || !request.Lines.Any())
        {
            throw new InvalidOperationException("At least one line item is required for a Sales Invoice.");
        }

        if (request.SalesOrderId.HasValue && request.SalesOrderId.Value > 0)
        {
            var soExists = await _dbContext.SalesOrders.AnyAsync(o => o.CompanyId == companyId && o.Id == request.SalesOrderId.Value, cancellationToken);
            if (!soExists)
            {
                throw new InvalidOperationException($"Sales Order with ID {request.SalesOrderId.Value} was not found in the active company.");
            }
        }

        if (request.DeliveryChallanId.HasValue && request.DeliveryChallanId.Value > 0)
        {
            var dcExists = await _dbContext.DeliveryChallans.AnyAsync(d => d.CompanyId == companyId && d.Id == request.DeliveryChallanId.Value, cancellationToken);
            if (!dcExists)
            {
                throw new InvalidOperationException($"Delivery Challan with ID {request.DeliveryChallanId.Value} was not found in the active company.");
            }
        }

        // Default Revenue account: 4100 General Sales Revenue
        var defaultRevenueAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "4100", cancellationToken)
            ?? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Sales Revenue Account (4100) was not found for the active company.");
        int defaultRevenueAccountId = defaultRevenueAccount.Id;

        var prefix = $"INV-{_dateTimeProvider.UtcNow:yyyyMM}-";
        var countThisMonth = await _dbContext.Invoices
            .CountAsync(i => i.CompanyId == companyId && i.InvoiceNumber.StartsWith(prefix), cancellationToken);
        var invoiceNumber = $"{prefix}{(countThisMonth + 1):D4}";

        var invoice = new Invoice
        {
            CompanyId = companyId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate,
            CustomerId = customer.Id,
            SalesOrderId = request.SalesOrderId,
            DeliveryChallanId = request.DeliveryChallanId,
            DiscountAmount = request.DiscountAmount,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Draft
        };

        decimal subTotal = 0m;
        decimal totalTax = 0m;
        int lineNo = 1;

        foreach (var reqLine in request.Lines)
        {
            decimal lineAmt = Math.Round(reqLine.Quantity * reqLine.UnitPrice, 4);
            subTotal += lineAmt;
            totalTax += reqLine.TaxAmount;

            int revAccId = reqLine.RevenueAccountId ?? defaultRevenueAccountId;

            invoice.Lines.Add(new InvoiceLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                Description = string.IsNullOrWhiteSpace(reqLine.Description) ? $"Line {lineNo - 1}" : reqLine.Description,
                Quantity = reqLine.Quantity,
                UnitPrice = reqLine.UnitPrice,
                LineAmount = lineAmt,
                TaxAmount = reqLine.TaxAmount,
                RevenueAccountId = revAccId
            });
        }

        invoice.SubTotal = subTotal;
        invoice.TaxAmount = totalTax;
        invoice.TotalAmount = (subTotal - request.DiscountAmount) + totalTax;
        invoice.PaidAmount = 0m;
        invoice.BalanceAmount = invoice.TotalAmount;

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }

    public async Task<InvoiceDto> ConfirmAndPostToGlAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var invoice = await _dbContext.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId, cancellationToken);

        if (invoice == null)
        {
            throw new InvalidOperationException($"Sales Invoice with ID {id} was not found.");
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Sales Invoice {invoice.InvoiceNumber} is currently '{invoice.Status}' and cannot be confirmed.");
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == invoice.CustomerId && c.CompanyId == companyId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {invoice.CustomerId} was not found.");
        }

        // 1. Determine Accounts
        // Debit: Customer Control Account (e.g. 1130 Trade AR)
        var arAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == customer.GlControlAccountId, cancellationToken)
            ?? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1130", cancellationToken)
            ?? throw new InvalidOperationException("Trade Accounts Receivable Control account (1130) was not found for the active company.");
        var arAccountId = arAccount.Id;

        // Credit: Revenue Account(s)
        var defaultRevenue = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "4100", cancellationToken)
            ?? throw new InvalidOperationException("Sales Revenue account (4100) was not found for the active company.");
        int defaultRevenueAccountId = defaultRevenue.Id;

        // Credit: Tax Account (2130 VAT & Tax Withholding Payable)
        var taxAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2130", cancellationToken);
        int taxAccountId = taxAccount?.Id ?? defaultRevenueAccountId;

        // Group revenue credits by RevenueAccountId
        var revenueCredits = invoice.Lines
            .GroupBy(l => l.RevenueAccountId > 0 ? l.RevenueAccountId : defaultRevenueAccountId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.LineAmount));

        // Adjust for overall invoice discount proportionally if any
        decimal netRevenue = invoice.SubTotal - invoice.DiscountAmount;

        var jvLines = new List<CreateVoucherLineRequest>();

        // Debit: Trade Accounts Receivable Control = TotalAmount
        jvLines.Add(new CreateVoucherLineRequest(
            AccountId: arAccountId,
            Debit: invoice.TotalAmount,
            Credit: 0m,
            Particulars: $"Receivable for Sales Invoice: {invoice.InvoiceNumber} ({customer.CustomerName})"
        ));

        // Credit: Sales Revenue
        foreach (var kvp in revenueCredits)
        {
            decimal lineRev = kvp.Value;
            if (invoice.SubTotal > 0 && invoice.DiscountAmount > 0)
            {
                lineRev = Math.Round(kvp.Value * (netRevenue / invoice.SubTotal), 4);
            }

            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: kvp.Key,
                Debit: 0m,
                Credit: lineRev,
                Particulars: $"Sales Revenue for Invoice: {invoice.InvoiceNumber}"
            ));
        }

        // Credit: Tax / VAT Payable if applicable
        if (invoice.TaxAmount > 0)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: taxAccountId,
                Debit: 0m,
                Credit: invoice.TaxAmount,
                Particulars: $"VAT Payable for Sales Invoice: {invoice.InvoiceNumber}"
            ));
        }

        // 2. Post Journal Voucher
        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(invoice.InvoiceDate),
            Reference: invoice.InvoiceNumber,
            Description: $"Automated GL Interface: Sales Invoice {invoice.InvoiceNumber} - Customer: {customer.CustomerCode}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create GL journal voucher for Sales Invoice: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for Sales Invoice: {postResult.Error}");
        }

        // 3. Update Customer Current Balance
        customer.CurrentBalance += invoice.TotalAmount;

        // 4. Update Sales Order status to Invoiced if linked
        if (invoice.SalesOrderId.HasValue)
        {
            var order = await _dbContext.SalesOrders
                .FirstOrDefaultAsync(o => o.Id == invoice.SalesOrderId.Value && o.CompanyId == companyId, cancellationToken);
            if (order != null)
            {
                order.OrderStatus = SalesOrderStatus.Invoiced;
            }
        }

        invoice.Status = InvoiceStatus.Confirmed;
        invoice.JournalVoucherId = createResult.Value!.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetInvoiceByIdAsync(invoice.Id, cancellationToken))!;
    }
}
