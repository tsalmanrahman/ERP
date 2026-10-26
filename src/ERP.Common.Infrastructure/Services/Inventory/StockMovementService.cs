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

public class StockMovementService : IStockMovementService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public StockMovementService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.GetRequiredCompanyId();

    // --- Stock Adjustments ---

    public async Task<List<StockAdjustmentDto>> GetStockAdjustmentsAsync(int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from a in _dbContext.StockAdjustments.AsNoTracking()
                    where a.CompanyId == companyId
                    join w in _dbContext.Warehouses.AsNoTracking() on a.WarehouseId equals w.Id
                    select new { a, w };

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.a.WarehouseId == warehouseId.Value);
        }

        var list = await query.OrderByDescending(x => x.a.AdjustmentDate).ThenByDescending(x => x.a.Id).ToListAsync(cancellationToken);

        return list.Select(x => new StockAdjustmentDto
        {
            Id = x.a.Id,
            AdjustmentNumber = x.a.AdjustmentNumber,
            AdjustmentDate = x.a.AdjustmentDate,
            WarehouseId = x.w.Id,
            WarehouseName = x.w.WarehouseName,
            AdjustmentType = x.a.AdjustmentType,
            Reason = x.a.Reason,
            TotalAmount = x.a.TotalAmount,
            Status = x.a.Status,
            JournalVoucherId = x.a.JournalVoucherId
        }).ToList();
    }

    public async Task<StockAdjustmentDto?> GetStockAdjustmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var adj = await _dbContext.StockAdjustments
            .Include(a => a.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == id, cancellationToken);

        if (adj == null) return null;

        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == adj.WarehouseId, cancellationToken);
        var productIds = adj.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new StockAdjustmentDto
        {
            Id = adj.Id,
            AdjustmentNumber = adj.AdjustmentNumber,
            AdjustmentDate = adj.AdjustmentDate,
            WarehouseId = adj.WarehouseId,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            AdjustmentType = adj.AdjustmentType,
            Reason = adj.Reason,
            TotalAmount = adj.TotalAmount,
            Status = adj.Status,
            JournalVoucherId = adj.JournalVoucherId,
            Lines = adj.Lines.OrderBy(l => l.LineNumber).Select(l => new StockAdjustmentLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var p) ? p.ProductCode : string.Empty,
                ProductName = p?.ProductName ?? string.Empty,
                AdjustmentQuantity = l.AdjustmentQuantity,
                UnitCost = l.UnitCost,
                LineTotal = l.LineTotal,
                Notes = l.Notes
            }).ToList()
        };
    }

    public async Task<StockAdjustmentDto> ProcessStockAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Stock adjustment must contain at least one line item.");
        }

        var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == request.WarehouseId, cancellationToken);
        if (warehouse == null)
        {
            throw new InvalidOperationException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        var varianceAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "5120", cancellationToken)
            ?? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "5110", cancellationToken);

        if (varianceAccount == null)
        {
            throw new InvalidOperationException("No Inventory Adjustment Variance account (5120/5110) found.");
        }

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var classIds = products.Values.Select(p => p.ProductClassId).Distinct().ToList();
        var productClasses = await _dbContext.ProductClasses
            .Where(c => classIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var fallbackInvAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1140", cancellationToken);

        var adjNumber = await GenerateNextAdjustmentNumberAsync(companyId, request.AdjustmentDate, cancellationToken);
        var now = DateTime.UtcNow;

        decimal totalNetCost = 0m;
        var adjLines = new List<StockAdjustmentLine>();
        var jvLines = new List<CreateVoucherLineRequest>();

        int lineNo = 1;
        foreach (var reqLine in request.Lines)
        {
            if (reqLine.AdjustmentQuantity == 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Adjustment quantity cannot be zero.");
            }

            if (!products.TryGetValue(reqLine.ProductId, out var product))
            {
                throw new InvalidOperationException($"Line {lineNo}: Product with ID {reqLine.ProductId} was not found.");
            }

            var stock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == request.WarehouseId && s.ProductId == reqLine.ProductId, cancellationToken);

            decimal unitCost = reqLine.UnitCost > 0 ? reqLine.UnitCost : (stock != null && stock.AverageCost > 0 ? stock.AverageCost : product.CurrentCost);
            decimal lineAmount = Math.Abs(reqLine.AdjustmentQuantity) * unitCost;

            if (stock == null)
            {
                if (reqLine.AdjustmentQuantity < 0)
                {
                    throw new InvalidOperationException($"Cannot adjust negative quantity for product '{product.ProductCode}' with zero stock.");
                }
                stock = new ProductStock
                {
                    CompanyId = companyId,
                    WarehouseId = request.WarehouseId,
                    ProductId = reqLine.ProductId,
                    QuantityOnHand = reqLine.AdjustmentQuantity,
                    AllocatedQuantity = 0m,
                    AverageCost = unitCost,
                    LastTransactionDate = now
                };
                _dbContext.ProductStock.Add(stock);
            }
            else
            {
                if (stock.QuantityOnHand + reqLine.AdjustmentQuantity < 0)
                {
                    throw new InvalidOperationException($"Insufficient stock for product '{product.ProductCode}'. Current on hand: {stock.QuantityOnHand}, Adjustment: {reqLine.AdjustmentQuantity}.");
                }
                stock.QuantityOnHand += reqLine.AdjustmentQuantity;
                stock.LastTransactionDate = now;
            }

            // Append Stock Ledger entry
            var ledgerEntry = new StockLedger
            {
                CompanyId = companyId,
                WarehouseId = request.WarehouseId,
                ProductId = reqLine.ProductId,
                TransactionDate = request.AdjustmentDate,
                TransactionType = StockTransactionType.Adjustment,
                ReferenceNumber = adjNumber,
                QuantityIn = reqLine.AdjustmentQuantity > 0 ? reqLine.AdjustmentQuantity : 0m,
                QuantityOut = reqLine.AdjustmentQuantity < 0 ? Math.Abs(reqLine.AdjustmentQuantity) : 0m,
                UnitCost = unitCost,
                TotalCost = lineAmount,
                BalanceQuantity = stock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(ledgerEntry);

            // Track adjustment line
            adjLines.Add(new StockAdjustmentLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                AdjustmentQuantity = reqLine.AdjustmentQuantity,
                UnitCost = unitCost,
                LineTotal = lineAmount,
                Notes = reqLine.Notes?.Trim()
            });

            totalNetCost += lineAmount;

            // GL Accounting:
            // If Stock Gain (AdjustmentQuantity > 0): Debit Inventory Account, Credit Variance
            // If Stock Loss (AdjustmentQuantity < 0): Debit Variance, Credit Inventory Account
            int glInvAccountId = fallbackInvAccount?.Id ?? 0;
            if (productClasses.TryGetValue(product.ProductClassId, out var pClass) && pClass.GlInventoryAccountId > 0)
            {
                glInvAccountId = pClass.GlInventoryAccountId;
            }
            if (glInvAccountId <= 0)
            {
                throw new InvalidOperationException($"No GL Inventory Account configured for product '{product.ProductCode}' in the active company.");
            }

            if (reqLine.AdjustmentQuantity > 0)
            {
                // Gain: Debit Inventory Asset, Credit Variance Account
                jvLines.Add(new CreateVoucherLineRequest(
                    AccountId: glInvAccountId,
                    Debit: lineAmount,
                    Credit: 0m,
                    Particulars: $"Stock Gain: {product.ProductCode} ({reqLine.AdjustmentQuantity} units)"
                ));
                jvLines.Add(new CreateVoucherLineRequest(
                    AccountId: varianceAccount.Id,
                    Debit: 0m,
                    Credit: lineAmount,
                    Particulars: $"Inventory Gain: {product.ProductCode} ({adjNumber})"
                ));
            }
            else
            {
                // Loss: Debit Variance Account, Credit Inventory Asset
                jvLines.Add(new CreateVoucherLineRequest(
                    AccountId: varianceAccount.Id,
                    Debit: lineAmount,
                    Credit: 0m,
                    Particulars: $"Inventory Loss / Shrinkage: {product.ProductCode} ({adjNumber})"
                ));
                jvLines.Add(new CreateVoucherLineRequest(
                    AccountId: glInvAccountId,
                    Debit: 0m,
                    Credit: lineAmount,
                    Particulars: $"Stock Write-off: {product.ProductCode} ({Math.Abs(reqLine.AdjustmentQuantity)} units)"
                ));
            }
        }

        // Create and post balanced GL Journal Voucher
        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(request.AdjustmentDate),
            Reference: adjNumber,
            Description: $"Inventory Adjustment: {adjNumber} - Reason: {request.Reason}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to generate GL journal voucher for stock adjustment: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for stock adjustment: {postResult.Error}");
        }

        var adjustment = new StockAdjustment
        {
            CompanyId = companyId,
            AdjustmentNumber = adjNumber,
            AdjustmentDate = request.AdjustmentDate.Date,
            WarehouseId = request.WarehouseId,
            AdjustmentType = request.AdjustmentType,
            Reason = request.Reason.Trim(),
            TotalAmount = totalNetCost,
            Status = StockAdjustmentStatus.Approved,
            JournalVoucherId = createResult.Value!.Id,
            Lines = adjLines
        };

        _dbContext.StockAdjustments.Add(adjustment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetStockAdjustmentByIdAsync(adjustment.Id, cancellationToken))!;
    }

    // --- Stock Transfers ---

    public async Task<List<StockTransferDto>> GetStockTransfersAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from t in _dbContext.StockTransfers.AsNoTracking()
                    where t.CompanyId == companyId
                    join fw in _dbContext.Warehouses.AsNoTracking() on t.FromWarehouseId equals fw.Id
                    join tw in _dbContext.Warehouses.AsNoTracking() on t.ToWarehouseId equals tw.Id
                    select new { t, fw, tw };

        var list = await query.OrderByDescending(x => x.t.TransferDate).ThenByDescending(x => x.t.Id).ToListAsync(cancellationToken);

        return list.Select(x => new StockTransferDto
        {
            Id = x.t.Id,
            TransferNumber = x.t.TransferNumber,
            TransferDate = x.t.TransferDate,
            FromWarehouseId = x.fw.Id,
            FromWarehouseName = x.fw.WarehouseName,
            ToWarehouseId = x.tw.Id,
            ToWarehouseName = x.tw.WarehouseName,
            Status = x.t.Status,
            Remarks = x.t.Remarks
        }).ToList();
    }

    public async Task<StockTransferDto?> GetStockTransferByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var transfer = await _dbContext.StockTransfers
            .Include(t => t.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.Id == id, cancellationToken);

        if (transfer == null) return null;

        var fw = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == transfer.FromWarehouseId, cancellationToken);
        var tw = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == transfer.ToWarehouseId, cancellationToken);

        var productIds = transfer.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new StockTransferDto
        {
            Id = transfer.Id,
            TransferNumber = transfer.TransferNumber,
            TransferDate = transfer.TransferDate,
            FromWarehouseId = transfer.FromWarehouseId,
            FromWarehouseName = fw?.WarehouseName ?? string.Empty,
            ToWarehouseId = transfer.ToWarehouseId,
            ToWarehouseName = tw?.WarehouseName ?? string.Empty,
            Status = transfer.Status,
            Remarks = transfer.Remarks,
            Lines = transfer.Lines.OrderBy(l => l.LineNumber).Select(l => new StockTransferLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var p) ? p.ProductCode : string.Empty,
                ProductName = p?.ProductName ?? string.Empty,
                TransferQuantity = l.TransferQuantity,
                UnitCost = l.UnitCost,
                LineTotal = l.LineTotal
            }).ToList()
        };
    }

    public async Task<StockTransferDto> ProcessStockTransferAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            throw new InvalidOperationException("Source and destination warehouses cannot be the same.");
        }

        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Stock transfer must contain at least one line item.");
        }

        var fw = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == request.FromWarehouseId, cancellationToken);
        var tw = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Id == request.ToWarehouseId, cancellationToken);

        if (fw == null || tw == null)
        {
            throw new InvalidOperationException("One or both specified warehouses were not found.");
        }

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var transferNumber = await GenerateNextTransferNumberAsync(companyId, request.TransferDate, cancellationToken);
        var now = DateTime.UtcNow;

        var transferLines = new List<StockTransferLine>();
        int lineNo = 1;

        foreach (var reqLine in request.Lines)
        {
            if (reqLine.TransferQuantity <= 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Transfer quantity must be greater than zero.");
            }

            if (!products.TryGetValue(reqLine.ProductId, out var product))
            {
                throw new InvalidOperationException($"Line {lineNo}: Product with ID {reqLine.ProductId} was not found.");
            }

            // Check source stock
            var fromStock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == request.FromWarehouseId && s.ProductId == reqLine.ProductId, cancellationToken);

            if (fromStock == null || fromStock.QuantityOnHand < reqLine.TransferQuantity)
            {
                decimal available = fromStock?.QuantityOnHand ?? 0m;
                throw new InvalidOperationException($"Insufficient stock for '{product.ProductCode}' in warehouse '{fw.WarehouseName}'. Available: {available}, Requested: {reqLine.TransferQuantity}.");
            }

            decimal unitCost = fromStock.AverageCost > 0 ? fromStock.AverageCost : product.CurrentCost;
            decimal lineTotal = Math.Round(reqLine.TransferQuantity * unitCost, 4);

            // Deduct from Source Warehouse
            fromStock.QuantityOnHand -= reqLine.TransferQuantity;
            fromStock.LastTransactionDate = now;

            var outLedger = new StockLedger
            {
                CompanyId = companyId,
                WarehouseId = request.FromWarehouseId,
                ProductId = reqLine.ProductId,
                TransactionDate = request.TransferDate,
                TransactionType = StockTransactionType.TransferOut,
                ReferenceNumber = transferNumber,
                QuantityIn = 0m,
                QuantityOut = reqLine.TransferQuantity,
                UnitCost = unitCost,
                TotalCost = lineTotal,
                BalanceQuantity = fromStock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(outLedger);

            // Add to Destination Warehouse
            var toStock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == request.ToWarehouseId && s.ProductId == reqLine.ProductId, cancellationToken);

            if (toStock == null)
            {
                toStock = new ProductStock
                {
                    CompanyId = companyId,
                    WarehouseId = request.ToWarehouseId,
                    ProductId = reqLine.ProductId,
                    QuantityOnHand = reqLine.TransferQuantity,
                    AllocatedQuantity = 0m,
                    AverageCost = unitCost,
                    LastTransactionDate = now
                };
                _dbContext.ProductStock.Add(toStock);
            }
            else
            {
                decimal oldQty = toStock.QuantityOnHand;
                decimal oldCost = toStock.AverageCost;
                decimal newQty = oldQty + reqLine.TransferQuantity;
                decimal newCost = newQty > 0 ? ((oldQty * oldCost) + (reqLine.TransferQuantity * unitCost)) / newQty : unitCost;

                toStock.QuantityOnHand = newQty;
                toStock.AverageCost = Math.Round(newCost, 4);
                toStock.LastTransactionDate = now;
            }

            var inLedger = new StockLedger
            {
                CompanyId = companyId,
                WarehouseId = request.ToWarehouseId,
                ProductId = reqLine.ProductId,
                TransactionDate = request.TransferDate,
                TransactionType = StockTransactionType.TransferIn,
                ReferenceNumber = transferNumber,
                QuantityIn = reqLine.TransferQuantity,
                QuantityOut = 0m,
                UnitCost = unitCost,
                TotalCost = lineTotal,
                BalanceQuantity = toStock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(inLedger);

            transferLines.Add(new StockTransferLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                TransferQuantity = reqLine.TransferQuantity,
                UnitCost = unitCost,
                LineTotal = lineTotal
            });
        }

        var transfer = new StockTransfer
        {
            CompanyId = companyId,
            TransferNumber = transferNumber,
            TransferDate = request.TransferDate.Date,
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            Status = StockTransferStatus.Completed,
            Remarks = request.Remarks?.Trim(),
            Lines = transferLines
        };

        _dbContext.StockTransfers.Add(transfer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetStockTransferByIdAsync(transfer.Id, cancellationToken))!;
    }

    private async Task<string> GenerateNextAdjustmentNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        var prefix = $"ADJ-{date:yyyyMM}-";
        var last = await _dbContext.StockAdjustments
            .Where(a => a.CompanyId == companyId && a.AdjustmentNumber.StartsWith(prefix))
            .OrderByDescending(a => a.AdjustmentNumber)
            .Select(a => a.AdjustmentNumber)
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

    private async Task<string> GenerateNextTransferNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        var prefix = $"ST-{date:yyyyMM}-";
        var last = await _dbContext.StockTransfers
            .Where(t => t.CompanyId == companyId && t.TransferNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TransferNumber)
            .Select(t => t.TransferNumber)
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
