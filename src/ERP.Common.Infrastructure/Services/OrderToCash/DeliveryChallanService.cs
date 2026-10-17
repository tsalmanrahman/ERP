using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.OrderToCash;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.OrderToCash;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Domain.Entities.Sales;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.OrderToCash;

public class DeliveryChallanService : IDeliveryChallanService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IJournalVoucherService _journalVoucherService;

    public DeliveryChallanService(
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

    public async Task<IReadOnlyList<DeliveryChallanDto>> GetDeliveryChallansAsync(int? salesOrderId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.DeliveryChallans
            .AsNoTracking()
            .Include(d => d.Lines)
            .Where(d => d.CompanyId == companyId);

        if (salesOrderId.HasValue)
        {
            query = query.Where(d => d.SalesOrderId == salesOrderId.Value);
        }

        var challans = await query.OrderByDescending(d => d.ChallanDate).ThenByDescending(d => d.Id).ToListAsync(cancellationToken);
        if (!challans.Any()) return Array.Empty<DeliveryChallanDto>();

        var soIds = challans.Select(d => d.SalesOrderId).Distinct().ToList();
        var customerIds = challans.Select(d => d.CustomerId).Distinct().ToList();
        var warehouseIds = challans.Select(d => d.WarehouseId).Distinct().ToList();
        var productIds = challans.SelectMany(d => d.Lines).Select(l => l.ProductId).Distinct().ToList();
        var jvIds = challans.Where(d => d.JournalVoucherId.HasValue).Select(d => d.JournalVoucherId!.Value).Distinct().ToList();

        var orders = await _dbContext.SalesOrders.AsNoTracking().Where(o => soIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, cancellationToken);
        var customers = await _dbContext.Customers.AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var warehouses = await _dbContext.Warehouses.AsNoTracking().Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);
        var products = await _dbContext.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var jvs = await _dbContext.JournalVouchers.AsNoTracking().Where(j => jvIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, cancellationToken);

        return challans.Select(d => new DeliveryChallanDto
        {
            Id = d.Id,
            ChallanNumber = d.ChallanNumber,
            ChallanDate = d.ChallanDate,
            SalesOrderId = d.SalesOrderId,
            OrderNumber = orders.TryGetValue(d.SalesOrderId, out var so) ? so.OrderNumber : string.Empty,
            CustomerId = d.CustomerId,
            CustomerName = customers.TryGetValue(d.CustomerId, out var c) ? c.CustomerName : string.Empty,
            WarehouseId = d.WarehouseId,
            WarehouseName = warehouses.TryGetValue(d.WarehouseId, out var w) ? w.WarehouseName : string.Empty,
            TotalCogsAmount = d.TotalCogsAmount,
            Status = d.Status,
            JournalVoucherId = d.JournalVoucherId,
            JournalVoucherNumber = d.JournalVoucherId.HasValue && jvs.TryGetValue(d.JournalVoucherId.Value, out var jv) ? jv.VoucherNumber : null,
            DriverName = d.DriverName,
            VehicleNumber = d.VehicleNumber,
            Remarks = d.Remarks,
            Lines = d.Lines.OrderBy(l => l.LineNumber).Select(l => new DeliveryChallanLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var prod) ? prod.ProductCode : string.Empty,
                ProductName = prod != null ? prod.ProductName : string.Empty,
                DispatchedQuantity = l.DispatchedQuantity,
                UnitCost = l.UnitCost,
                LineCogsAmount = l.LineCogsAmount,
                BatchNumber = l.BatchNumber
            }).ToList()
        }).ToList();
    }

    public async Task<DeliveryChallanDto?> GetDeliveryChallanByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var challan = await _dbContext.DeliveryChallans
            .AsNoTracking()
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);

        if (challan == null) return null;

        var order = await _dbContext.SalesOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == challan.SalesOrderId, cancellationToken);
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == challan.CustomerId, cancellationToken);
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == challan.WarehouseId, cancellationToken);

        var productIds = challan.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        string? jvNumber = null;
        if (challan.JournalVoucherId.HasValue)
        {
            var jv = await _dbContext.JournalVouchers.AsNoTracking().FirstOrDefaultAsync(j => j.Id == challan.JournalVoucherId.Value, cancellationToken);
            jvNumber = jv?.VoucherNumber;
        }

        return new DeliveryChallanDto
        {
            Id = challan.Id,
            ChallanNumber = challan.ChallanNumber,
            ChallanDate = challan.ChallanDate,
            SalesOrderId = challan.SalesOrderId,
            OrderNumber = order?.OrderNumber ?? string.Empty,
            CustomerId = challan.CustomerId,
            CustomerName = customer?.CustomerName ?? string.Empty,
            WarehouseId = challan.WarehouseId,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            TotalCogsAmount = challan.TotalCogsAmount,
            Status = challan.Status,
            JournalVoucherId = challan.JournalVoucherId,
            JournalVoucherNumber = jvNumber,
            DriverName = challan.DriverName,
            VehicleNumber = challan.VehicleNumber,
            Remarks = challan.Remarks,
            Lines = challan.Lines.OrderBy(l => l.LineNumber).Select(l => new DeliveryChallanLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var prod) ? prod.ProductCode : string.Empty,
                ProductName = prod != null ? prod.ProductName : string.Empty,
                DispatchedQuantity = l.DispatchedQuantity,
                UnitCost = l.UnitCost,
                LineCogsAmount = l.LineCogsAmount,
                BatchNumber = l.BatchNumber
            }).ToList()
        };
    }

    public async Task<DeliveryChallanDto> CreateDeliveryChallanAsync(CreateDeliveryChallanRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var order = await _dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.SalesOrderId && o.CompanyId == companyId, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Sales Order with ID {request.SalesOrderId} was not found.");
        }

        if (order.OrderStatus != SalesOrderStatus.Confirmed && order.OrderStatus != SalesOrderStatus.Dispatched)
        {
            throw new InvalidOperationException($"Sales Order {order.OrderNumber} is currently '{order.OrderStatus}' and must be Confirmed before dispatching.");
        }

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == companyId, cancellationToken);
        if (warehouse == null)
        {
            throw new InvalidOperationException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        if (request.Lines == null || !request.Lines.Any())
        {
            throw new InvalidOperationException("At least one line item is required for a Delivery Challan.");
        }

        var prefix = $"DC-{_dateTimeProvider.UtcNow:yyyyMM}-";
        var countThisMonth = await _dbContext.DeliveryChallans
            .CountAsync(d => d.CompanyId == companyId && d.ChallanNumber.StartsWith(prefix), cancellationToken);
        var challanNumber = $"{prefix}{(countThisMonth + 1):D4}";

        var challan = new DeliveryChallan
        {
            CompanyId = companyId,
            ChallanNumber = challanNumber,
            ChallanDate = request.ChallanDate,
            SalesOrderId = order.Id,
            CustomerId = order.CustomerId,
            WarehouseId = request.WarehouseId,
            TotalCogsAmount = 0m,
            Status = DeliveryChallanStatus.Draft,
            DriverName = request.DriverName,
            VehicleNumber = request.VehicleNumber,
            Remarks = request.Remarks
        };

        decimal totalCogs = 0m;
        int lineNo = 1;

        foreach (var reqLine in request.Lines)
        {
            var soLine = order.Lines.FirstOrDefault(l => l.ProductId == reqLine.ProductId);
            if (soLine == null)
            {
                throw new InvalidOperationException($"Product ID {reqLine.ProductId} does not exist in Sales Order {order.OrderNumber}.");
            }

            decimal remainingToShip = soLine.OrderedQuantity - soLine.ShippedQuantity;
            if (reqLine.DispatchedQuantity > remainingToShip)
            {
                throw new InvalidOperationException($"Line {lineNo}: Dispatched quantity ({reqLine.DispatchedQuantity}) exceeds unshipped quantity ({remainingToShip}) for product ID {reqLine.ProductId}.");
            }

            // Retrieve live stock moving average cost
            var stock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(ps => ps.CompanyId == companyId && ps.WarehouseId == request.WarehouseId && ps.ProductId == reqLine.ProductId, cancellationToken);

            if (stock == null || stock.QuantityOnHand < reqLine.DispatchedQuantity)
            {
                decimal available = stock?.QuantityOnHand ?? 0m;
                throw new InvalidOperationException($"Insufficient stock in warehouse '{warehouse.WarehouseName}' for Product ID {reqLine.ProductId}. Requested: {reqLine.DispatchedQuantity}, Available: {available}.");
            }

            decimal unitCost = stock.AverageCost;
            decimal lineCogs = Math.Round(reqLine.DispatchedQuantity * unitCost, 4);
            totalCogs += lineCogs;

            challan.Lines.Add(new DeliveryChallanLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                DispatchedQuantity = reqLine.DispatchedQuantity,
                UnitCost = unitCost,
                LineCogsAmount = lineCogs,
                BatchNumber = reqLine.BatchNumber
            });
        }

        challan.TotalCogsAmount = totalCogs;

        _dbContext.DeliveryChallans.Add(challan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetDeliveryChallanByIdAsync(challan.Id, cancellationToken))!;
    }

    public async Task<DeliveryChallanDto> DispatchAndPostCogsAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var challan = await _dbContext.DeliveryChallans
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);

        if (challan == null)
        {
            throw new InvalidOperationException($"Delivery Challan with ID {id} was not found.");
        }

        if (challan.Status != DeliveryChallanStatus.Draft)
        {
            throw new InvalidOperationException($"Delivery Challan {challan.ChallanNumber} is currently '{challan.Status}' and cannot be dispatched.");
        }

        var order = await _dbContext.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == challan.SalesOrderId && o.CompanyId == companyId, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Associated Sales Order {challan.SalesOrderId} not found.");
        }

        // 1. Find COGS (5110) and Finished Goods Inventory (1140) accounts
        var cogsAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "5110", cancellationToken);
        if (cogsAccount == null)
        {
            throw new InvalidOperationException("Cost of Goods Sold GL account (5110) was not found.");
        }

        var inventoryAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1140", cancellationToken);
        if (inventoryAccount == null)
        {
            throw new InvalidOperationException("Finished Goods Inventory GL account (1140) was not found.");
        }

        var now = _dateTimeProvider.UtcNow;

        // 2. Decrement warehouse stock, insert StockLedger entries, update SalesOrderLine shipped quantities
        foreach (var line in challan.Lines)
        {
            var stock = await _dbContext.ProductStock
                .FirstOrDefaultAsync(ps => ps.CompanyId == companyId && ps.WarehouseId == challan.WarehouseId && ps.ProductId == line.ProductId, cancellationToken);

            if (stock == null || stock.QuantityOnHand < line.DispatchedQuantity)
            {
                decimal avail = stock?.QuantityOnHand ?? 0m;
                throw new InvalidOperationException($"Insufficient inventory stock for Product ID {line.ProductId}. Available: {avail}, Dispatched: {line.DispatchedQuantity}.");
            }

            stock.QuantityOnHand -= line.DispatchedQuantity;
            stock.LastTransactionDate = now;

            // Log immutable StockLedger
            var ledger = new StockLedger
            {
                CompanyId = companyId,
                WarehouseId = challan.WarehouseId,
                ProductId = line.ProductId,
                TransactionDate = challan.ChallanDate,
                TransactionType = StockTransactionType.GoodsIssue,
                ReferenceNumber = challan.ChallanNumber,
                QuantityIn = 0m,
                QuantityOut = line.DispatchedQuantity,
                UnitCost = line.UnitCost,
                TotalCost = line.LineCogsAmount,
                BalanceQuantity = stock.QuantityOnHand
            };
            _dbContext.StockLedgers.Add(ledger);

            // Update SO line shipped quantity
            var soLine = order.Lines.FirstOrDefault(l => l.ProductId == line.ProductId);
            if (soLine != null)
            {
                soLine.ShippedQuantity += line.DispatchedQuantity;
            }
        }

        // Check if Sales Order is now fully shipped
        if (order.Lines.All(l => l.ShippedQuantity >= l.OrderedQuantity))
        {
            order.OrderStatus = SalesOrderStatus.Dispatched;
        }

        // 3. Automated Double-Entry GL Journal Voucher for COGS
        // Debit: 5110 Cost of Goods Sold - Manufacturing = TotalCogsAmount
        // Credit: 1140 Finished Goods Inventory Control = TotalCogsAmount
        var jvLines = new List<CreateVoucherLineRequest>
        {
            new CreateVoucherLineRequest(
                AccountId: cogsAccount.Id,
                Debit: challan.TotalCogsAmount,
                Credit: 0m,
                Particulars: $"COGS for Delivery Challan {challan.ChallanNumber} (SO: {order.OrderNumber})"
            ),
            new CreateVoucherLineRequest(
                AccountId: inventoryAccount.Id,
                Debit: 0m,
                Credit: challan.TotalCogsAmount,
                Particulars: $"Inventory relief for Delivery Challan {challan.ChallanNumber}"
            )
        };

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(challan.ChallanDate),
            Reference: challan.ChallanNumber,
            Description: $"Automated GL Interface: COGS for Challan {challan.ChallanNumber} (SO: {order.OrderNumber})",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create GL journal voucher for COGS: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL journal voucher for COGS: {postResult.Error}");
        }

        challan.Status = DeliveryChallanStatus.Dispatched;
        challan.JournalVoucherId = createResult.Value!.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetDeliveryChallanByIdAsync(challan.Id, cancellationToken))!;
    }
}
