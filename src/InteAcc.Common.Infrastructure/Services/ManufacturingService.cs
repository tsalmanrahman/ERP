using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.Finance;
using InteAcc.Common.Application.Abstractions.Specialized;
using InteAcc.Common.Application.Models.Finance;
using InteAcc.Common.Application.Models.Specialized;
using InteAcc.Common.Domain.Entities.Inventory;
using InteAcc.Common.Domain.Entities.Manufacturing;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class ManufacturingService : IManufacturingService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public ManufacturingService(
        InteAccDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<BillOfMaterialsDto>> GetBomsAsync(int? finishedGoodProductId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.BillsOfMaterials
            .Include(b => b.FinishedGoodProduct)
            .Include(b => b.Components)
                .ThenInclude(c => c.RawMaterialProduct)
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId);

        if (finishedGoodProductId.HasValue)
        {
            query = query.Where(b => b.FinishedGoodProductId == finishedGoodProductId.Value);
        }

        var list = await query.OrderBy(b => b.BomNumber).ToListAsync(cancellationToken);

        return list.Select(MapToBomDto).ToList();
    }

    public async Task<BillOfMaterialsDto?> GetBomByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var bom = await _dbContext.BillsOfMaterials
            .Include(b => b.FinishedGoodProduct)
            .Include(b => b.Components)
                .ThenInclude(c => c.RawMaterialProduct)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == id, cancellationToken);

        return bom == null ? null : MapToBomDto(bom);
    }

    public async Task<BillOfMaterialsDto> CreateBomAsync(CreateBomRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var number = request.BomNumber.Trim().ToUpperInvariant();

        var exists = await _dbContext.BillsOfMaterials.AnyAsync(b => b.CompanyId == companyId && b.BomNumber == number, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"BOM with number '{number}' already exists.");
        }

        var fg = await _dbContext.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == request.FinishedGoodProductId, cancellationToken);
        if (fg == null)
        {
            throw new InvalidOperationException($"Finished Good Product with ID {request.FinishedGoodProductId} was not found.");
        }

        var bom = new BillOfMaterials
        {
            CompanyId = companyId,
            BomNumber = number,
            FinishedGoodProductId = request.FinishedGoodProductId,
            QuantityProduced = request.QuantityProduced > 0 ? request.QuantityProduced : 1m,
            Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0" : request.Version.Trim(),
            IsActive = true
        };

        if (request.Components != null)
        {
            foreach (var c in request.Components)
            {
                var raw = await _dbContext.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == c.RawMaterialProductId, cancellationToken);
                if (raw == null)
                {
                    throw new InvalidOperationException($"Raw Material Product with ID {c.RawMaterialProductId} was not found.");
                }

                bom.Components.Add(new BomComponent
                {
                    RawMaterialProductId = c.RawMaterialProductId,
                    RequiredQuantity = c.RequiredQuantity,
                    ScrapPercentage = c.ScrapPercentage,
                    UnitCost = c.UnitCost > 0 ? c.UnitCost : raw.CurrentCost
                });
            }
        }

        _dbContext.BillsOfMaterials.Add(bom);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetBomByIdAsync(bom.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<ProductionOrderDto>> GetProductionOrdersAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from o in _dbContext.ProductionOrders.AsNoTracking()
                    where o.CompanyId == companyId
                    join b in _dbContext.BillsOfMaterials.AsNoTracking() on o.BomId equals b.Id
                    join fg in _dbContext.Products.AsNoTracking() on o.FinishedGoodProductId equals fg.Id
                    join rw in _dbContext.Warehouses.AsNoTracking() on o.RawMaterialWarehouseId equals rw.Id
                    join fw in _dbContext.Warehouses.AsNoTracking() on o.FinishedGoodWarehouseId equals fw.Id
                    join jv in _dbContext.JournalVouchers.AsNoTracking() on o.JournalVoucherId equals jv.Id into jvJoin
                    from jv in jvJoin.DefaultIfEmpty()
                    select new
                    {
                        o,
                        b.BomNumber,
                        fg.ProductCode,
                        fg.ProductName,
                        RawWarehouseName = rw.WarehouseName,
                        FgWarehouseName = fw.WarehouseName,
                        JvNumber = jv != null ? jv.VoucherNumber : null
                    };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.o.Status == status);
        }

        var list = await query.OrderByDescending(x => x.o.OrderDate).ToListAsync(cancellationToken);

        return list.Select(x => new ProductionOrderDto(
            x.o.Id,
            x.o.CompanyId,
            x.o.OrderNumber,
            x.o.BomId,
            x.BomNumber,
            x.o.FinishedGoodProductId,
            x.ProductCode,
            x.ProductName,
            x.o.TargetQuantity,
            x.o.CompletedQuantity,
            x.o.RawMaterialWarehouseId,
            x.RawWarehouseName,
            x.o.FinishedGoodWarehouseId,
            x.FgWarehouseName,
            x.o.OrderDate,
            x.o.StartDate,
            x.o.CompletionDate,
            x.o.TotalMaterialCost,
            x.o.Status,
            x.o.JournalVoucherId,
            x.JvNumber
        )).ToList();
    }

    public async Task<ProductionOrderDto?> GetProductionOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var item = await (from o in _dbContext.ProductionOrders.AsNoTracking()
                          where o.CompanyId == companyId && o.Id == id
                          join b in _dbContext.BillsOfMaterials.AsNoTracking() on o.BomId equals b.Id
                          join fg in _dbContext.Products.AsNoTracking() on o.FinishedGoodProductId equals fg.Id
                          join rw in _dbContext.Warehouses.AsNoTracking() on o.RawMaterialWarehouseId equals rw.Id
                          join fw in _dbContext.Warehouses.AsNoTracking() on o.FinishedGoodWarehouseId equals fw.Id
                          join jv in _dbContext.JournalVouchers.AsNoTracking() on o.JournalVoucherId equals jv.Id into jvJoin
                          from jv in jvJoin.DefaultIfEmpty()
                          select new
                          {
                              o,
                              b.BomNumber,
                              fg.ProductCode,
                              fg.ProductName,
                              RawWarehouseName = rw.WarehouseName,
                              FgWarehouseName = fw.WarehouseName,
                              JvNumber = jv != null ? jv.VoucherNumber : null
                          }).FirstOrDefaultAsync(cancellationToken);

        if (item == null) return null;

        return new ProductionOrderDto(
            item.o.Id,
            item.o.CompanyId,
            item.o.OrderNumber,
            item.o.BomId,
            item.BomNumber,
            item.o.FinishedGoodProductId,
            item.ProductCode,
            item.ProductName,
            item.o.TargetQuantity,
            item.o.CompletedQuantity,
            item.o.RawMaterialWarehouseId,
            item.RawWarehouseName,
            item.o.FinishedGoodWarehouseId,
            item.FgWarehouseName,
            item.o.OrderDate,
            item.o.StartDate,
            item.o.CompletionDate,
            item.o.TotalMaterialCost,
            item.o.Status,
            item.o.JournalVoucherId,
            item.JvNumber
        );
    }

    public async Task<ProductionOrderDto> CreateProductionOrderAsync(CreateProductionOrderRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var bom = await _dbContext.BillsOfMaterials
            .Include(b => b.Components)
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == request.BomId, cancellationToken);

        if (bom == null)
        {
            throw new InvalidOperationException($"BOM with ID {request.BomId} was not found.");
        }

        var orderDate = request.OrderDate?.Date ?? DateTime.UtcNow.Date;
        var orderNumber = $"MO-{orderDate:yyyyMM}-{DateTime.UtcNow.Ticks % 10000:D4}";

        var totalDirectCost = bom.CalculateTotalDirectMaterialCost();
        var totalCost = Math.Round(totalDirectCost * request.TargetQuantity, 2);

        var order = new ProductionOrder
        {
            CompanyId = companyId,
            OrderNumber = orderNumber,
            BomId = bom.Id,
            FinishedGoodProductId = bom.FinishedGoodProductId,
            TargetQuantity = request.TargetQuantity,
            CompletedQuantity = 0m,
            RawMaterialWarehouseId = request.RawMaterialWarehouseId,
            FinishedGoodWarehouseId = request.FinishedGoodWarehouseId,
            OrderDate = orderDate,
            StartDate = orderDate,
            TotalMaterialCost = totalCost,
            Status = "InProgress"
        };

        _dbContext.ProductionOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetProductionOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<ProductionOrderDto> CompleteProductionOrderAsync(CompleteProductionOrderRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var order = await _dbContext.ProductionOrders
            .Include(o => o.Bom)
                .ThenInclude(b => b!.Components)
            .FirstOrDefaultAsync(o => o.CompanyId == companyId && o.Id == request.ProductionOrderId, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Production Order with ID {request.ProductionOrderId} was not found.");
        }

        if (order.Status == "Completed")
        {
            throw new InvalidOperationException($"Production Order {order.OrderNumber} is already completed.");
        }

        var qtyToComplete = request.CompletedQuantity ?? order.TargetQuantity;
        var completionDate = request.CompletionDate?.Date ?? DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var bom = order.Bom!;
        decimal actualMaterialCost = 0m;

        // 1. Consume Raw Materials from RawMaterialWarehouse
        foreach (var comp in bom.Components)
        {
            var requiredQty = comp.RequiredQuantity * (1m + (comp.ScrapPercentage / 100m)) * qtyToComplete;
            var lineCost = Math.Round(requiredQty * comp.UnitCost, 2);
            actualMaterialCost += lineCost;

            var rawStock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == order.RawMaterialWarehouseId && s.ProductId == comp.RawMaterialProductId, cancellationToken);

            if (rawStock == null)
            {
                rawStock = new ProductStock
                {
                    CompanyId = companyId,
                    WarehouseId = order.RawMaterialWarehouseId,
                    ProductId = comp.RawMaterialProductId,
                    QuantityOnHand = 0m,
                    AverageCost = comp.UnitCost,
                    LastTransactionDate = now
                };
                _dbContext.ProductStock.Add(rawStock);
            }

            rawStock.QuantityOnHand -= requiredQty;
            rawStock.LastTransactionDate = now;

            var rawLedger = new StockLedger
            {
                CompanyId = companyId,
                WarehouseId = order.RawMaterialWarehouseId,
                ProductId = comp.RawMaterialProductId,
                TransactionDate = completionDate,
                TransactionType = StockTransactionType.GoodsIssue,
                ReferenceNumber = order.OrderNumber,
                QuantityIn = 0m,
                QuantityOut = requiredQty,
                UnitCost = comp.UnitCost,
                TotalCost = lineCost,
                BalanceQuantity = rawStock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(rawLedger);
        }

        // 2. Produce Finished Goods into FinishedGoodWarehouse
        var fgStock = await _dbContext.ProductStock
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.WarehouseId == order.FinishedGoodWarehouseId && s.ProductId == order.FinishedGoodProductId, cancellationToken);

        var fgUnitCost = qtyToComplete > 0 ? Math.Round(actualMaterialCost / qtyToComplete, 4) : 0m;

        if (fgStock == null)
        {
            fgStock = new ProductStock
            {
                CompanyId = companyId,
                WarehouseId = order.FinishedGoodWarehouseId,
                ProductId = order.FinishedGoodProductId,
                QuantityOnHand = qtyToComplete,
                AverageCost = fgUnitCost,
                LastTransactionDate = now
            };
            _dbContext.ProductStock.Add(fgStock);
        }
        else
        {
            var oldQty = fgStock.QuantityOnHand;
            var oldCost = fgStock.AverageCost;
            var newQty = oldQty + qtyToComplete;
            var newAvg = newQty > 0 ? ((oldQty * oldCost) + actualMaterialCost) / newQty : oldCost;

            fgStock.QuantityOnHand = newQty;
            fgStock.AverageCost = Math.Round(newAvg, 4);
            fgStock.LastTransactionDate = now;
        }

        var fgLedger = new StockLedger
        {
            CompanyId = companyId,
            WarehouseId = order.FinishedGoodWarehouseId,
            ProductId = order.FinishedGoodProductId,
            TransactionDate = completionDate,
            TransactionType = StockTransactionType.GoodsReceipt,
            ReferenceNumber = order.OrderNumber,
            QuantityIn = qtyToComplete,
            QuantityOut = 0m,
            UnitCost = fgUnitCost,
            TotalCost = actualMaterialCost,
            BalanceQuantity = fgStock.QuantityOnHand
        };
        _dbContext.StockLedgers.Add(fgLedger);

        // 3. Automated Double-Entry GL Journal Voucher:
        // Dr: 1140 Finished Goods Inventory Control = actualMaterialCost
        // Cr: 1145 Raw Materials Inventory Control = actualMaterialCost
        var fgAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1140", cancellationToken);
        var rawAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1145", cancellationToken)
            ?? fgAccount;

        if (fgAccount != null && rawAccount != null && actualMaterialCost > 0)
        {
            var jvLines = new List<CreateVoucherLineRequest>
            {
                new(
                    AccountId: fgAccount.Id,
                    Debit: actualMaterialCost,
                    Credit: 0m,
                    Particulars: $"Finished Goods capitalization for {order.OrderNumber} (Qty: {qtyToComplete})"
                ),
                new(
                    AccountId: rawAccount.Id,
                    Debit: 0m,
                    Credit: actualMaterialCost,
                    Particulars: $"Raw materials consumed for {order.OrderNumber}"
                )
            };

            var jvRequest = new CreateJournalVoucherRequest(
                VoucherDate: DateOnly.FromDateTime(completionDate),
                Reference: order.OrderNumber,
                Description: $"Production Order Completion: {order.OrderNumber} (Produced: {qtyToComplete})",
                FiscalPeriodId: null,
                Lines: jvLines
            );

            var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
            if (createResult.IsSuccess)
            {
                await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
                order.JournalVoucherId = createResult.Value!.Id;
            }
        }

        order.CompletedQuantity = qtyToComplete;
        order.CompletionDate = completionDate;
        order.TotalMaterialCost = actualMaterialCost;
        order.Status = "Completed";

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetProductionOrderByIdAsync(order.Id, cancellationToken))!;
    }

    private static BillOfMaterialsDto MapToBomDto(BillOfMaterials b)
    {
        var compDtos = b.Components.Select(c => new BomComponentDto(
            c.Id,
            c.RawMaterialProductId,
            c.RawMaterialProduct?.ProductCode ?? string.Empty,
            c.RawMaterialProduct?.ProductName ?? string.Empty,
            c.RequiredQuantity,
            c.ScrapPercentage,
            c.UnitCost,
            Math.Round(c.RequiredQuantity * (1m + (c.ScrapPercentage / 100m)) * c.UnitCost, 2)
        )).ToList();

        return new BillOfMaterialsDto(
            b.Id,
            b.CompanyId,
            b.BomNumber,
            b.FinishedGoodProductId,
            b.FinishedGoodProduct?.ProductCode ?? string.Empty,
            b.FinishedGoodProduct?.ProductName ?? string.Empty,
            b.QuantityProduced,
            b.Version,
            b.IsActive,
            b.CalculateTotalDirectMaterialCost(),
            compDtos
        );
    }
}
