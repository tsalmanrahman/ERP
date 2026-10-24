using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.Inventory;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Domain.Entities.Procurement;
using ERP.Common.Domain.Entities.Manufacturing;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Inventory;

public class GoodsReceiptService : IGoodsReceiptService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;
    private readonly IDocumentSequenceService? _documentSequenceService;

    public GoodsReceiptService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService,
        IDocumentSequenceService? documentSequenceService = null)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
        _documentSequenceService = documentSequenceService;
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
            GrnType = x.g.GrnType,
            GrnDate = x.g.GrnDate,
            WarehouseId = x.w.Id,
            WarehouseCode = x.w.WarehouseCode,
            WarehouseName = x.w.WarehouseName,
            VendorId = x.g.VendorId,
            VendorName = x.v != null ? x.v.VendorName : null,
            PurchaseOrderId = x.g.PurchaseOrderId,
            ProductionOrderId = x.g.ProductionOrderId,
            ProductionReference = x.g.ProductionReference,
            PoNumber = x.g.PoNumber,
            ChallanNumber = x.g.ChallanNumber,
            ChallanDate = x.g.ChallanDate,
            CarrierName = x.g.CarrierName,
            VehicleNumber = x.g.VehicleNumber,
            GateEntryNumber = x.g.GateEntryNumber,
            QcStatus = x.g.QcStatus,
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
            .Include(g => g.Returns)
                .ThenInclude(r => r.Lines)
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
            GrnType = grn.GrnType,
            GrnDate = grn.GrnDate,
            WarehouseId = grn.WarehouseId,
            WarehouseCode = warehouse?.WarehouseCode ?? string.Empty,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            VendorId = grn.VendorId,
            VendorName = vendor?.VendorName,
            PurchaseOrderId = grn.PurchaseOrderId,
            ProductionOrderId = grn.ProductionOrderId,
            ProductionReference = grn.ProductionReference,
            PoNumber = grn.PoNumber,
            ChallanNumber = grn.ChallanNumber,
            ChallanDate = grn.ChallanDate,
            CarrierName = grn.CarrierName,
            VehicleNumber = grn.VehicleNumber,
            GateEntryNumber = grn.GateEntryNumber,
            QcStatus = grn.QcStatus,
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
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var p) ? p.ProductCode : string.Empty,
                ProductName = p?.ProductName ?? string.Empty,
                UomCode = p != null && uoms.TryGetValue(p.PrimaryUomId, out var u) ? u.UomCode : string.Empty,
                QuantityReceived = l.QuantityReceived,
                AcceptedQuantity = l.AcceptedQuantity,
                RejectedQuantity = l.RejectedQuantity,
                ReturnedQuantity = l.ReturnedQuantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                StorageBinLocation = l.StorageBinLocation,
                BatchNumber = l.BatchNumber,
                ExpiryDate = l.ExpiryDate,
                Remarks = l.Remarks
            }).ToList(),
            Returns = grn.Returns.Where(r => !r.IsDeleted).Select(r => new GrnReturnDto
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                ReturnDate = r.ReturnDate,
                GoodsReceiptNoteId = r.GoodsReceiptNoteId,
                GrnNumber = grn.GrnNumber,
                ReturnType = r.ReturnType,
                WarehouseId = r.WarehouseId,
                WarehouseName = warehouse?.WarehouseName ?? string.Empty,
                VendorId = r.VendorId,
                VendorName = vendor?.VendorName,
                Reason = r.Reason,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                JournalVoucherId = r.JournalVoucherId,
                Remarks = r.Remarks,
                Lines = r.Lines.Where(rl => !rl.IsDeleted).Select(rl => new GrnReturnLineDto
                {
                    Id = rl.Id,
                    LineNumber = rl.LineNumber,
                    GoodsReceiptNoteLineId = rl.GoodsReceiptNoteLineId,
                    ProductId = rl.ProductId,
                    ProductCode = products.TryGetValue(rl.ProductId, out var prod) ? prod.ProductCode : string.Empty,
                    ProductName = prod?.ProductName ?? string.Empty,
                    UomCode = prod != null && uoms.TryGetValue(prod.PrimaryUomId, out var u2) ? u2.UomCode : string.Empty,
                    ReturnedQuantity = rl.ReturnedQuantity,
                    UnitPrice = rl.UnitPrice,
                    LineTotal = rl.LineTotal,
                    ReturnReason = rl.ReturnReason
                }).ToList()
            }).ToList()
        };
    }

    public async Task<List<EligiblePurchaseOrderDto>> GetEligiblePurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var pos = await _dbContext.PurchaseOrders
            .Include(po => po.Lines)
            .Where(po => po.CompanyId == companyId && 
                         (po.Status == PurchaseOrderStatus.Issued || po.Status == PurchaseOrderStatus.PartiallyReceived) &&
                         !po.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var vendorIds = pos.Select(po => po.VendorId).Distinct().ToList();
        var vendors = await _dbContext.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.VendorName, cancellationToken);

        var warehouseIds = pos.Select(po => po.WarehouseId).Distinct().ToList();
        var warehouses = await _dbContext.Warehouses
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.WarehouseName, cancellationToken);

        var allProductIds = pos.SelectMany(po => po.Lines.Select(l => l.ProductId)).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => allProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var uomIds = products.Values.Select(p => p.PrimaryUomId).Distinct().ToList();
        var uoms = await _dbContext.UnitsOfMeasure
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UomCode, cancellationToken);

        var result = new List<EligiblePurchaseOrderDto>();

        foreach (var po in pos)
        {
            var eligibleLines = new List<EligiblePoLineDto>();
            foreach (var line in po.Lines.Where(l => !l.IsDeleted))
            {
                var outstanding = line.OrderedQuantity - line.ReceivedQuantity;
                if (outstanding > 0)
                {
                    products.TryGetValue(line.ProductId, out var prod);
                    var uomCode = prod != null && uoms.TryGetValue(prod.PrimaryUomId, out var u) ? u : string.Empty;

                    eligibleLines.Add(new EligiblePoLineDto
                    {
                        Id = line.Id,
                        ProductId = line.ProductId,
                        ProductCode = prod?.ProductCode ?? string.Empty,
                        ProductName = prod?.ProductName ?? string.Empty,
                        UomCode = uomCode,
                        OrderedQuantity = line.OrderedQuantity,
                        PreviouslyReceivedQuantity = line.ReceivedQuantity,
                        OutstandingQuantity = outstanding,
                        UnitPrice = line.UnitPrice
                    });
                }
            }

            if (eligibleLines.Count > 0)
            {
                result.Add(new EligiblePurchaseOrderDto
                {
                    Id = po.Id,
                    PoNumber = po.PoNumber,
                    OrderDate = po.PoDate,
                    VendorId = po.VendorId,
                    VendorName = vendors.TryGetValue(po.VendorId, out var vn) ? vn : string.Empty,
                    WarehouseId = po.WarehouseId,
                    WarehouseName = warehouses.TryGetValue(po.WarehouseId, out var wn) ? wn : string.Empty,
                    Lines = eligibleLines
                });
            }
        }

        return result;
    }

    public async Task<List<EligibleProductionOrderDto>> GetEligibleProductionOrdersAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var mos = await _dbContext.Set<ProductionOrder>()
            .Where(mo => mo.CompanyId == companyId && 
                         (mo.Status == "Planned" || mo.Status == "InProgress") &&
                         !mo.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var fgIds = mos.Select(m => m.FinishedGoodProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => fgIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var uomIds = products.Values.Select(p => p.PrimaryUomId).Distinct().ToList();
        var uoms = await _dbContext.UnitsOfMeasure
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UomCode, cancellationToken);

        var whIds = mos.Select(m => m.FinishedGoodWarehouseId).Distinct().ToList();
        var warehouses = await _dbContext.Warehouses
            .Where(w => whIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.WarehouseName, cancellationToken);

        var result = new List<EligibleProductionOrderDto>();

        foreach (var mo in mos)
        {
            var outstanding = mo.TargetQuantity - mo.CompletedQuantity;
            if (outstanding > 0)
            {
                products.TryGetValue(mo.FinishedGoodProductId, out var prod);
                var uomCode = prod != null && uoms.TryGetValue(prod.PrimaryUomId, out var u) ? u : string.Empty;
                decimal unitCost = mo.TargetQuantity > 0 ? Math.Round(mo.TotalMaterialCost / mo.TargetQuantity, 4) : (prod?.StandardCost ?? 0m);

                result.Add(new EligibleProductionOrderDto
                {
                    Id = mo.Id,
                    OrderNumber = mo.OrderNumber,
                    OrderDate = mo.OrderDate,
                    FinishedGoodProductId = mo.FinishedGoodProductId,
                    FinishedGoodCode = prod?.ProductCode ?? string.Empty,
                    FinishedGoodName = prod?.ProductName ?? string.Empty,
                    UomCode = uomCode,
                    TargetQuantity = mo.TargetQuantity,
                    PreviouslyCompletedQuantity = mo.CompletedQuantity,
                    OutstandingQuantity = outstanding,
                    EstimatedUnitCost = unitCost > 0 ? unitCost : (prod?.StandardCost ?? 0m),
                    FinishedGoodWarehouseId = mo.FinishedGoodWarehouseId,
                    FinishedGoodWarehouseName = warehouses.TryGetValue(mo.FinishedGoodWarehouseId, out var wn) ? wn : string.Empty
                });
            }
        }

        return result;
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

        // Validation for Vendor GRN vs Production GRN
        if (request.GrnType == GrnType.Vendor)
        {
            if (!request.VendorId.HasValue || request.VendorId.Value <= 0)
            {
                throw new InvalidOperationException("Vendor must be specified for Vendor GRN.");
            }
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

            // Cap QC accepted/rejected quantities to total received
            decimal acceptedQty = reqLine.AcceptedQuantity > 0 ? reqLine.AcceptedQuantity : reqLine.QuantityReceived;
            decimal rejectedQty = reqLine.RejectedQuantity >= 0 ? reqLine.RejectedQuantity : 0m;

            if (acceptedQty + rejectedQty != reqLine.QuantityReceived)
            {
                acceptedQty = reqLine.QuantityReceived - rejectedQty;
            }

            var lineTotal = Math.Round(reqLine.QuantityReceived * reqLine.UnitPrice, 4);
            totalAmount += lineTotal;

            grnLines.Add(new GoodsReceiptNoteLine
            {
                LineNumber = lineNo++,
                PurchaseOrderLineId = reqLine.PurchaseOrderLineId,
                ProductId = reqLine.ProductId,
                QuantityReceived = reqLine.QuantityReceived,
                AcceptedQuantity = acceptedQty,
                RejectedQuantity = rejectedQty,
                ReturnedQuantity = 0m,
                UnitPrice = reqLine.UnitPrice,
                LineTotal = lineTotal,
                StorageBinLocation = reqLine.StorageBinLocation?.Trim(),
                BatchNumber = reqLine.BatchNumber?.Trim(),
                ExpiryDate = reqLine.ExpiryDate,
                Remarks = reqLine.Remarks?.Trim()
            });
        }

        var grn = new GoodsReceiptNote
        {
            CompanyId = companyId,
            GrnNumber = grnNumber,
            GrnType = request.GrnType,
            GrnDate = request.GrnDate.Date,
            WarehouseId = request.WarehouseId,
            VendorId = request.GrnType == GrnType.Vendor ? request.VendorId : null,
            PurchaseOrderId = request.GrnType == GrnType.Vendor ? request.PurchaseOrderId : null,
            ProductionOrderId = request.GrnType == GrnType.Production ? request.ProductionOrderId : null,
            ProductionReference = request.GrnType == GrnType.Production ? request.ProductionReference?.Trim() : null,
            PoNumber = request.PoNumber?.Trim(),
            ChallanNumber = request.ChallanNumber?.Trim(),
            ChallanDate = request.ChallanDate?.Date,
            CarrierName = request.CarrierName?.Trim(),
            VehicleNumber = request.VehicleNumber?.Trim(),
            GateEntryNumber = request.GateEntryNumber?.Trim(),
            QcStatus = string.IsNullOrWhiteSpace(request.QcStatus) ? "Passed" : request.QcStatus.Trim(),
            TotalAmount = totalAmount,
            Status = GrnStatus.Draft,
            Remarks = request.Remarks?.Trim(),
            Lines = grnLines
        };

        _dbContext.GoodsReceiptNotes.Add(grn);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetGoodsReceiptNoteByIdAsync(grn.Id, cancellationToken))!;
    }

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

        // Find Offset Account based on GRN Type
        // Vendor GRN -> Credit Accrued Purchases / GRN Clearing (2140 or fallback 2110)
        // Production GRN -> Credit WIP Inventory / Clearing (1145 or fallback 1140)
        Account? creditOffsetAccount = null;

        if (grn.GrnType == GrnType.Vendor)
        {
            creditOffsetAccount = await _dbContext.Accounts
                .Where(a => a.CompanyId == companyId && (a.AccountCode == "2140" || a.AccountCode == "2110"))
                .OrderBy(a => a.AccountCode == "2140" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);

            if (creditOffsetAccount == null)
            {
                throw new InvalidOperationException("No GRN Clearing (2140) or AP Control (2110) account found for Vendor GRN posting.");
            }
        }
        else // Production GRN
        {
            creditOffsetAccount = await _dbContext.Accounts
                .Where(a => a.CompanyId == companyId && (a.AccountCode == "1145" || a.AccountCode == "1140"))
                .OrderBy(a => a.AccountCode == "1145" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);

            if (creditOffsetAccount == null)
            {
                creditOffsetAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId, cancellationToken);
            }
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
                TransactionType = grn.GrnType == GrnType.Production ? StockTransactionType.ProductionReceipt : StockTransactionType.GoodsReceipt,
                ReferenceNumber = grn.GrnNumber,
                QuantityIn = line.QuantityReceived,
                QuantityOut = 0m,
                UnitCost = line.UnitPrice,
                TotalCost = line.LineTotal,
                BalanceQuantity = stock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(ledgerEntry);
        }

        // 2. Lifecycle Updates for Source Documents (PO or MO)
        if (grn.GrnType == GrnType.Vendor && grn.PurchaseOrderId.HasValue)
        {
            var po = await _dbContext.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == grn.PurchaseOrderId.Value, cancellationToken);

            if (po != null)
            {
                foreach (var line in grn.Lines)
                {
                    if (line.PurchaseOrderLineId.HasValue)
                    {
                        var poLine = po.Lines.FirstOrDefault(pl => pl.Id == line.PurchaseOrderLineId.Value);
                        if (poLine != null)
                        {
                            poLine.ReceivedQuantity += line.QuantityReceived;
                        }
                    }
                    else
                    {
                        // Fallback matching by ProductId
                        var poLine = po.Lines.FirstOrDefault(pl => pl.ProductId == line.ProductId && pl.ReceivedQuantity < pl.OrderedQuantity);
                        if (poLine != null)
                        {
                            poLine.ReceivedQuantity += line.QuantityReceived;
                        }
                    }
                }

                // Check PO completion status
                bool allComplete = po.Lines.All(pl => pl.ReceivedQuantity >= pl.OrderedQuantity);
                bool anyReceived = po.Lines.Any(pl => pl.ReceivedQuantity > 0);

                if (allComplete)
                {
                    po.Status = PurchaseOrderStatus.Completed;
                }
                else if (anyReceived)
                {
                    po.Status = PurchaseOrderStatus.PartiallyReceived;
                }
            }
        }
        else if (grn.GrnType == GrnType.Production && grn.ProductionOrderId.HasValue)
        {
            var mo = await _dbContext.Set<ProductionOrder>()
                .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.Id == grn.ProductionOrderId.Value, cancellationToken);

            if (mo != null)
            {
                decimal totalFgReceived = grn.Lines.Where(l => l.ProductId == mo.FinishedGoodProductId).Sum(l => l.QuantityReceived);
                mo.CompletedQuantity += totalFgReceived;

                if (mo.CompletedQuantity >= mo.TargetQuantity)
                {
                    mo.Status = "Completed";
                    mo.CompletionDate = now;
                }
                else
                {
                    mo.Status = "InProgress";
                    if (!mo.StartDate.HasValue) mo.StartDate = now;
                }
            }
        }

        // 3. Automated Double-Entry GL Journal Generation
        var jvLines = new List<CreateVoucherLineRequest>();

        // Debits: Inventory Asset Accounts (Dr 1140 Finished Goods / Raw Materials)
        foreach (var kvp in inventoryDebitsByAccount)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: kvp.Key,
                Debit: kvp.Value,
                Credit: 0m,
                Particulars: $"GRN Inventory Receipt: {grn.GrnNumber} ({grn.GrnType})"
            ));
        }

        // Credit: GRN Clearing (2140) or WIP Clearing (1145)
        if (creditOffsetAccount != null)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: creditOffsetAccount.Id,
                Debit: 0m,
                Credit: grn.TotalAmount,
                Particulars: grn.GrnType == GrnType.Production 
                    ? $"WIP Finished Goods Receipt offset: {grn.GrnNumber}"
                    : $"Accrued liability for GRN: {grn.GrnNumber}"
            ));
        }

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(grn.GrnDate),
            Reference: grn.GrnNumber,
            Description: $"Automated GL Interface: GRN {grn.GrnNumber} [{grn.GrnType}] - Source: {grn.PoNumber ?? grn.ProductionReference ?? "Direct"}",
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

        // 4. Finalize GRN state
        grn.Status = GrnStatus.Received;
        grn.JournalVoucherId = createResult.Value!.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetGoodsReceiptNoteByIdAsync(grn.Id, cancellationToken))!;
    }

    public async Task<GrnReturnDto> CreateGoodsReceiptReturnAsync(CreateGrnReturnRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var grn = await _dbContext.GoodsReceiptNotes
            .Include(g => g.Lines)
            .FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == request.GoodsReceiptNoteId, cancellationToken);

        if (grn == null)
        {
            throw new InvalidOperationException($"Original GRN with ID {request.GoodsReceiptNoteId} was not found.");
        }

        if (grn.Status != GrnStatus.Received)
        {
            throw new InvalidOperationException($"Returns can only be initiated against posted GRNs (Current Status: {grn.Status}).");
        }

        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new InvalidOperationException("At least one return item line is required.");
        }

        var returnNumber = await GenerateNextReturnNumberAsync(companyId, request.ReturnDate, cancellationToken);
        decimal totalReturnAmount = 0m;
        var returnLines = new List<GoodsReceiptReturnLine>();

        int lineNo = 1;
        foreach (var reqLine in request.Lines)
        {
            var grnLine = grn.Lines.FirstOrDefault(l => l.Id == reqLine.GoodsReceiptNoteLineId);
            if (grnLine == null)
            {
                throw new InvalidOperationException($"GRN line ID {reqLine.GoodsReceiptNoteLineId} does not belong to GRN {grn.GrnNumber}.");
            }

            decimal maxReturnable = grnLine.QuantityReceived - grnLine.ReturnedQuantity;
            if (reqLine.ReturnedQuantity <= 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Returned quantity must be greater than zero.");
            }

            if (reqLine.ReturnedQuantity > maxReturnable)
            {
                throw new InvalidOperationException($"Line {lineNo}: Cannot return {reqLine.ReturnedQuantity}. Maximum returnable quantity is {maxReturnable}.");
            }

            decimal lineTotal = Math.Round(reqLine.ReturnedQuantity * grnLine.UnitPrice, 4);
            totalReturnAmount += lineTotal;

            returnLines.Add(new GoodsReceiptReturnLine
            {
                LineNumber = lineNo++,
                GoodsReceiptNoteLineId = grnLine.Id,
                ProductId = grnLine.ProductId,
                ReturnedQuantity = reqLine.ReturnedQuantity,
                UnitPrice = grnLine.UnitPrice,
                LineTotal = lineTotal,
                ReturnReason = reqLine.ReturnReason?.Trim() ?? request.Reason.Trim()
            });
        }

        var grnReturn = new GoodsReceiptReturn
        {
            CompanyId = companyId,
            ReturnNumber = returnNumber,
            ReturnDate = request.ReturnDate.Date,
            GoodsReceiptNoteId = grn.Id,
            ReturnType = grn.GrnType == GrnType.Production ? GoodsReceiptReturnType.ProductionReturn : GoodsReceiptReturnType.VendorReturn,
            WarehouseId = grn.WarehouseId,
            VendorId = grn.VendorId,
            PurchaseOrderId = grn.PurchaseOrderId,
            ProductionOrderId = grn.ProductionOrderId,
            Reason = request.Reason.Trim(),
            TotalAmount = totalReturnAmount,
            Status = GoodsReceiptReturnStatus.Draft,
            Remarks = request.Remarks?.Trim(),
            Lines = returnLines
        };

        _dbContext.GoodsReceiptReturns.Add(grnReturn);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == grnReturn.WarehouseId, cancellationToken);
        var vendor = grnReturn.VendorId.HasValue ? await _dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == grnReturn.VendorId.Value, cancellationToken) : null;

        return new GrnReturnDto
        {
            Id = grnReturn.Id,
            ReturnNumber = grnReturn.ReturnNumber,
            ReturnDate = grnReturn.ReturnDate,
            GoodsReceiptNoteId = grnReturn.GoodsReceiptNoteId,
            GrnNumber = grn.GrnNumber,
            ReturnType = grnReturn.ReturnType,
            WarehouseId = grnReturn.WarehouseId,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            VendorId = grnReturn.VendorId,
            VendorName = vendor?.VendorName,
            Reason = grnReturn.Reason,
            TotalAmount = grnReturn.TotalAmount,
            Status = grnReturn.Status,
            Remarks = grnReturn.Remarks,
            Lines = grnReturn.Lines.Select(l => new GrnReturnLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                GoodsReceiptNoteLineId = l.GoodsReceiptNoteLineId,
                ProductId = l.ProductId,
                ReturnedQuantity = l.ReturnedQuantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                ReturnReason = l.ReturnReason
            }).ToList()
        };
    }

    public async Task<GrnReturnDto> PostGoodsReceiptReturnAsync(int returnId, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var grnReturn = await _dbContext.GoodsReceiptReturns
            .Include(r => r.Lines)
            .Include(r => r.GoodsReceiptNote)
                .ThenInclude(g => g!.Lines)
            .FirstOrDefaultAsync(r => r.CompanyId == companyId && r.Id == returnId, cancellationToken);

        if (grnReturn == null)
        {
            throw new InvalidOperationException($"GRN Return with ID {returnId} was not found.");
        }

        if (grnReturn.Status != GoodsReceiptReturnStatus.Draft)
        {
            throw new InvalidOperationException($"GRN Return {grnReturn.ReturnNumber} cannot be posted because its current status is {grnReturn.Status}.");
        }

        var productIds = grnReturn.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var classIds = products.Values.Select(p => p.ProductClassId).Distinct().ToList();
        var productClasses = await _dbContext.ProductClasses
            .Where(c => classIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var fallbackInvAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1140", cancellationToken);

        // Find Offset Account based on return type
        Account? debitOffsetAccount = null;
        if (grnReturn.ReturnType == GoodsReceiptReturnType.VendorReturn)
        {
            debitOffsetAccount = await _dbContext.Accounts
                .Where(a => a.CompanyId == companyId && (a.AccountCode == "2140" || a.AccountCode == "2110"))
                .OrderBy(a => a.AccountCode == "2140" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            debitOffsetAccount = await _dbContext.Accounts
                .Where(a => a.CompanyId == companyId && (a.AccountCode == "1145" || a.AccountCode == "1140"))
                .OrderBy(a => a.AccountCode == "1145" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        var inventoryCreditsByAccount = new Dictionary<int, decimal>();

        foreach (var line in grnReturn.Lines)
        {
            var grnLine = grnReturn.GoodsReceiptNote?.Lines.FirstOrDefault(gl => gl.Id == line.GoodsReceiptNoteLineId);
            if (grnLine != null)
            {
                grnLine.ReturnedQuantity += line.ReturnedQuantity;
            }

            int glInvAccountId = fallbackInvAccount?.Id ?? 6;
            if (products.TryGetValue(line.ProductId, out var product) &&
                productClasses.TryGetValue(product.ProductClassId, out var pClass) && 
                pClass.GlInventoryAccountId > 0)
            {
                glInvAccountId = pClass.GlInventoryAccountId;
            }

            if (!inventoryCreditsByAccount.ContainsKey(glInvAccountId))
            {
                inventoryCreditsByAccount[glInvAccountId] = 0m;
            }
            inventoryCreditsByAccount[glInvAccountId] += line.LineTotal;

            // Decrement Stock
            var stock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == grnReturn.WarehouseId && s.ProductId == line.ProductId, cancellationToken);

            if (stock != null)
            {
                stock.QuantityOnHand -= line.ReturnedQuantity;
                stock.LastTransactionDate = now;

                // Log immutable StockLedger entry (Stock Transaction Out)
                var ledgerEntry = new StockLedger
                {
                    CompanyId = companyId,
                    WarehouseId = grnReturn.WarehouseId,
                    ProductId = line.ProductId,
                    TransactionDate = grnReturn.ReturnDate,
                    TransactionType = StockTransactionType.AdjustmentOut,
                    ReferenceNumber = grnReturn.ReturnNumber,
                    QuantityIn = 0m,
                    QuantityOut = line.ReturnedQuantity,
                    UnitCost = line.UnitPrice,
                    TotalCost = line.LineTotal,
                    BalanceQuantity = stock.QuantityOnHand
                };
                _dbContext.StockLedgers.Add(ledgerEntry);
            }
        }

        // Restore PO balances if Vendor Return
        if (grnReturn.ReturnType == GoodsReceiptReturnType.VendorReturn && grnReturn.PurchaseOrderId.HasValue)
        {
            var po = await _dbContext.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == grnReturn.PurchaseOrderId.Value, cancellationToken);

            if (po != null)
            {
                foreach (var line in grnReturn.Lines)
                {
                    var poLine = po.Lines.FirstOrDefault(pl => pl.ProductId == line.ProductId);
                    if (poLine != null)
                    {
                        poLine.ReceivedQuantity = Math.Max(0m, poLine.ReceivedQuantity - line.ReturnedQuantity);
                    }
                }
                po.Status = po.Lines.Any(pl => pl.ReceivedQuantity > 0) ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Issued;
            }
        }

        // Automated GL Reversal Voucher: Dr GRN Clearing / WIP (2140 / 1145), Cr Inventory Asset (1140)
        var jvLines = new List<CreateVoucherLineRequest>();
        if (debitOffsetAccount != null)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: debitOffsetAccount.Id,
                Debit: grnReturn.TotalAmount,
                Credit: 0m,
                Particulars: $"Reversal for GRN Return: {grnReturn.ReturnNumber}"
            ));
        }

        foreach (var kvp in inventoryCreditsByAccount)
        {
            jvLines.Add(new CreateVoucherLineRequest(
                AccountId: kvp.Key,
                Debit: 0m,
                Credit: kvp.Value,
                Particulars: $"Inventory reduction for GRN Return: {grnReturn.ReturnNumber}"
            ));
        }

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(grnReturn.ReturnDate),
            Reference: grnReturn.ReturnNumber,
            Description: $"Automated GL Interface: GRN Return {grnReturn.ReturnNumber} (Original: {grnReturn.GoodsReceiptNote?.GrnNumber})",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (createResult.IsSuccess)
        {
            await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
            grnReturn.JournalVoucherId = createResult.Value.Id;
        }

        grnReturn.Status = GoodsReceiptReturnStatus.Posted;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == grnReturn.WarehouseId, cancellationToken);
        var vendor = grnReturn.VendorId.HasValue ? await _dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == grnReturn.VendorId.Value, cancellationToken) : null;

        return new GrnReturnDto
        {
            Id = grnReturn.Id,
            ReturnNumber = grnReturn.ReturnNumber,
            ReturnDate = grnReturn.ReturnDate,
            GoodsReceiptNoteId = grnReturn.GoodsReceiptNoteId,
            GrnNumber = grnReturn.GoodsReceiptNote?.GrnNumber ?? string.Empty,
            ReturnType = grnReturn.ReturnType,
            WarehouseId = grnReturn.WarehouseId,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            VendorId = grnReturn.VendorId,
            VendorName = vendor?.VendorName,
            Reason = grnReturn.Reason,
            TotalAmount = grnReturn.TotalAmount,
            Status = grnReturn.Status,
            JournalVoucherId = grnReturn.JournalVoucherId,
            Remarks = grnReturn.Remarks
        };
    }

    private async Task<string> GenerateNextGrnNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        if (_documentSequenceService != null)
        {
            try
            {
                return await _documentSequenceService.GetNextNumberAsync(companyId, "GRN", cancellationToken: cancellationToken);
            }
            catch
            {
                // Fallback to internal counter
            }
        }

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

    private async Task<string> GenerateNextReturnNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        if (_documentSequenceService != null)
        {
            try
            {
                return await _documentSequenceService.GetNextNumberAsync(companyId, "GRN-RET", cancellationToken: cancellationToken);
            }
            catch
            {
                // Fallback
            }
        }

        var prefix = $"RET-{date:yyyyMM}-";
        var lastNumber = await _dbContext.GoodsReceiptReturns
            .Where(r => r.CompanyId == companyId && r.ReturnNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReturnNumber)
            .Select(r => r.ReturnNumber)
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
