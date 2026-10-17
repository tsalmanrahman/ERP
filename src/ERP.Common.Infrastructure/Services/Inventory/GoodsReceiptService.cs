using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Inventory;

public class GoodsReceiptService : IGoodsReceiptService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public GoodsReceiptService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<List<GrnDto>> GetGoodsReceiptNotesAsync(int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from g in _dbContext.GoodsReceiptNotes.AsNoTracking()
                    where g.CompanyId == companyId
                    join w in _dbContext.Warehouses.AsNoTracking() on g.WarehouseId equals w.Id
                    join v in _dbContext.Vendors.AsNoTracking() on g.VendorId equals v.Id into vJoin
                    from v in vJoin.DefaultIfEmpty()
                    join jv in _dbContext.JournalVouchers.AsNoTracking() on g.JournalVoucherId equals jv.Id into jvJoin
                    from jv in jvJoin.DefaultIfEmpty()
                    select new { g, w, v, jv };

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.g.WarehouseId == warehouseId.Value);
        }

        var list = await query.OrderByDescending(x => x.g.GrnDate).ThenByDescending(x => x.g.Id).ToListAsync(cancellationToken);

        return list.Select(x => new GrnDto
        {
            Id = x.g.Id,
            GrnNumber = x.g.GrnNumber,
            GrnDate = x.g.GrnDate,
            WarehouseId = x.w.Id,
            WarehouseCode = x.w.WarehouseCode,
            WarehouseName = x.w.WarehouseName,
            VendorId = x.g.VendorId,
            VendorName = x.v != null ? x.v.VendorName : null,
            PoNumber = x.g.PoNumber,
            ChallanNumber = x.g.ChallanNumber,
            ChallanDate = x.g.ChallanDate,
            TotalAmount = x.g.TotalAmount,
            Status = x.g.Status,
            JournalVoucherId = x.g.JournalVoucherId,
            JournalVoucherNumber = x.jv != null ? x.jv.VoucherNumber : null,
            Remarks = x.g.Remarks,
            CreatedAtUtc = x.g.CreatedAtUtc
        }).ToList();
    }

    public async Task<GrnDto?> GetGoodsReceiptNoteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var grn = await _dbContext.GoodsReceiptNotes
            .Include(g => g.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == id, cancellationToken);

        if (grn == null) return null;

        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == grn.WarehouseId, cancellationToken);
        var vendor = grn.VendorId.HasValue 
            ? await _dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == grn.VendorId.Value, cancellationToken)
            : null;

        string? jvNumber = null;
        if (grn.JournalVoucherId.HasValue)
        {
            var jv = await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == grn.JournalVoucherId.Value, cancellationToken);
            jvNumber = jv?.VoucherNumber;
        }

        var productIds = grn.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var uomIds = products.Values.Select(p => p.PrimaryUomId).Distinct().ToList();
        var uoms = await _dbContext.UnitsOfMeasure
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return new GrnDto
        {
            Id = grn.Id,
            GrnNumber = grn.GrnNumber,
            GrnDate = grn.GrnDate,
            WarehouseId = grn.WarehouseId,
            WarehouseCode = warehouse?.WarehouseCode ?? string.Empty,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            VendorId = grn.VendorId,
            VendorName = vendor?.VendorName,
            PoNumber = grn.PoNumber,
            ChallanNumber = grn.ChallanNumber,
            ChallanDate = grn.ChallanDate,
            TotalAmount = grn.TotalAmount,
            Status = grn.Status,
            JournalVoucherId = grn.JournalVoucherId,
            JournalVoucherNumber = jvNumber,
            Remarks = grn.Remarks,
            CreatedAtUtc = grn.CreatedAtUtc,
            Lines = grn.Lines.OrderBy(l => l.LineNumber).Select(l => new GrnLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var p) ? p.ProductCode : string.Empty,
                ProductName = p?.ProductName ?? string.Empty,
                UomCode = p != null && uoms.TryGetValue(p.PrimaryUomId, out var u) ? u.UomCode : string.Empty,
                QuantityReceived = l.QuantityReceived,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                Remarks = l.Remarks
            }).ToList()
        };
    }

    public async Task<GrnDto> CreateGoodsReceiptNoteAsync(CreateGrnRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new InvalidOperationException("A Goods Receipt Note must contain at least one line item.");
        }

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
        {
            throw new InvalidOperationException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        var grnNumber = await GenerateNextGrnNumberAsync(companyId, request.GrnDate, cancellationToken);
        decimal totalAmount = 0m;
        var grnLines = new List<GoodsReceiptNoteLine>();

        int lineNo = 1;
        foreach (var reqLine in request.Lines)
        {
            if (reqLine.QuantityReceived <= 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Received quantity must be greater than zero.");
            }
            if (reqLine.UnitPrice < 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Unit price cannot be negative.");
            }

            var lineTotal = Math.Round(reqLine.QuantityReceived * reqLine.UnitPrice, 4);
            totalAmount += lineTotal;

            grnLines.Add(new GoodsReceiptNoteLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                QuantityReceived = reqLine.QuantityReceived,
                UnitPrice = reqLine.UnitPrice,
                LineTotal = lineTotal,
                BatchNumber = reqLine.BatchNumber?.Trim(),
                ExpiryDate = reqLine.ExpiryDate,
                Remarks = reqLine.Remarks?.Trim()
            });
        }

        var grn = new GoodsReceiptNote
        {
            CompanyId = companyId,
            GrnNumber = grnNumber,
            GrnDate = request.GrnDate.Date,
            WarehouseId = request.WarehouseId,
            VendorId = request.VendorId,
            PoNumber = request.PoNumber?.Trim(),
            ChallanNumber = request.ChallanNumber?.Trim(),
            ChallanDate = request.ChallanDate?.Date,
            TotalAmount = totalAmount,
            Status = GrnStatus.Draft,
            Remarks = request.Remarks?.Trim(),
            Lines = grnLines
        };

        _dbContext.GoodsReceiptNotes.Add(grn);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetGoodsReceiptNoteByIdAsync(grn.Id, cancellationToken))!;
    }

    /// <summary>
    /// Re-engineers legacy stored procedure `icProcessTransactionToGl`:
    /// Updates live warehouse stock, recalculates moving weighted average cost,
    /// logs transaction to stock ledger, and generates a balanced double-entry GL journal voucher.
    /// </summary>
    public async Task<GrnDto> ReceiveAndPostGrnAsync(int grnId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var grn = await _dbContext.GoodsReceiptNotes
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == grnId, cancellationToken);

        if (grn == null)
        {
            throw new InvalidOperationException($"GRN with ID {grnId} was not found.");
        }

        if (grn.Status != GrnStatus.Draft)
        {
            throw new InvalidOperationException($"GRN {grn.GrnNumber} cannot be received because its current status is {grn.Status}.");
        }

        var productIds = grn.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var classIds = products.Values.Select(p => p.ProductClassId).Distinct().ToList();
        var productClasses = await _dbContext.ProductClasses
            .Where(c => classIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        // Find GRN Clearing / Accrued Purchases Account (e.g. 2140 or fallback 2110 Trade AP)
        var grnClearingAccount = await _dbContext.Accounts
            .Where(a => a.CompanyId == companyId && (a.AccountCode == "2140" || a.AccountCode == "2110"))
            .OrderBy(a => a.AccountCode == "2140" ? 0 : 1)
            .FirstOrDefaultAsync(cancellationToken);

        if (grnClearingAccount == null)
        {
            throw new InvalidOperationException("No GRN Clearing (2140) or AP Control (2110) account found for posting.");
        }

        var fallbackInvAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1140", cancellationToken);

        var now = DateTime.UtcNow;

        // 1. Process Stock & Valuation Updates per line
        var inventoryDebitsByAccount = new Dictionary<int, decimal>();

        foreach (var line in grn.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new InvalidOperationException($"Product with ID {line.ProductId} was not found.");
            }

            int glInvAccountId = fallbackInvAccount?.Id ?? 6;
            if (productClasses.TryGetValue(product.ProductClassId, out var pClass) && pClass.GlInventoryAccountId > 0)
            {
                glInvAccountId = pClass.GlInventoryAccountId;
            }

            if (!inventoryDebitsByAccount.ContainsKey(glInvAccountId))
            {
                inventoryDebitsByAccount[glInvAccountId] = 0m;
            }
            inventoryDebitsByAccount[glInvAccountId] += line.LineTotal;

            // Load or initialize live ProductStock
            var stock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == grn.WarehouseId && s.ProductId == line.ProductId, cancellationToken);

            if (stock == null)
            {
                stock = new ProductStock
                {
                    CompanyId = companyId,
                    WarehouseId = grn.WarehouseId,
                    ProductId = line.ProductId,
                    QuantityOnHand = line.QuantityReceived,
                    AllocatedQuantity = 0m,
                    AverageCost = line.UnitPrice,
                    LastTransactionDate = now
                };
                _dbContext.ProductStock.Add(stock);
            }
            else
            {
                // Moving Weighted Average Cost Calculation
                decimal oldQty = stock.QuantityOnHand;
                decimal oldCost = stock.AverageCost;
                decimal newQty = oldQty + line.QuantityReceived;

                decimal newAverageCost = line.UnitPrice;
                if (newQty > 0)
                {
                    newAverageCost = ((oldQty * oldCost) + (line.QuantityReceived * line.UnitPrice)) / newQty;
                }

                stock.QuantityOnHand = newQty;
                stock.AverageCost = Math.Round(newAverageCost, 4);
                stock.LastTransactionDate = now;
            }

            // Sync product current cost
            product.CurrentCost = stock.AverageCost;

            // Log immutable StockLedger entry
            var ledgerEntry = new StockLedger
            {
                CompanyId = companyId,
                WarehouseId = grn.WarehouseId,
                ProductId = line.ProductId,
                TransactionDate = grn.GrnDate,
                TransactionType = StockTransactionType.GoodsReceipt,
                ReferenceNumber = grn.GrnNumber,
                QuantityIn = line.QuantityReceived,
                QuantityOut = 0m,
                UnitCost = line.UnitPrice,
                TotalCost = line.LineTotal,
                BalanceQuantity = stock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(ledgerEntry);
        }

        // 2. Automated Double-Entry GL Journal Generation (Replaces icProcessTransactionToGl)
        var jvLines = new List<CreateVoucherLineRequest>();

        // Debits: Inventory Assets
        foreach (var kvp in inventoryDebitsByAccount)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: kvp.Key,
                Debit: kvp.Value,
                Credit: 0m,
                Particulars: $"GRN Inventory Receipt: {grn.GrnNumber}"
            ));
        }

        // Credit: Accrued Purchases / GRN Clearing
        jvLines.Add(new CreateVoucherLineRequest(
            AccountId: grnClearingAccount.Id,
            Debit: 0m,
            Credit: grn.TotalAmount,
            Particulars: $"Accrued liability for GRN: {grn.GrnNumber}"
        ));

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(grn.GrnDate),
            Reference: grn.GrnNumber,
            Description: $"Automated GL Interface: GRN {grn.GrnNumber} - PO: {grn.PoNumber ?? "Direct"}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to generate GL journal voucher for GRN: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for GRN: {postResult.Error}");
        }

        // 3. Finalize GRN state
        grn.Status = GrnStatus.Received;
        grn.JournalVoucherId = createResult.Value!.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetGoodsReceiptNoteByIdAsync(grn.Id, cancellationToken))!;
    }

    private async Task<string> GenerateNextGrnNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        var prefix = $"GRN-{date:yyyyMM}-";
        var lastNumber = await _dbContext.GoodsReceiptNotes
            .Where(g => g.CompanyId == companyId && g.GrnNumber.StartsWith(prefix))
            .OrderByDescending(g => g.GrnNumber)
            .Select(g => g.GrnNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int seq = 1;
        if (!string.IsNullOrEmpty(lastNumber) && lastNumber.Length >= prefix.Length + 4)
        {
            var seqStr = lastNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out var parsed))
            {
                seq = parsed + 1;
            }
        }

        return $"{prefix}{seq:D4}";
    }
}
