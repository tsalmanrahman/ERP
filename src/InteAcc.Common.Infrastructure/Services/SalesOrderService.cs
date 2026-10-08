using Microsoft.EntityFrameworkCore;
using InteAcc.Common.Application.Abstractions.Common;
using InteAcc.Common.Application.Abstractions.OrderToCash;
using InteAcc.Common.Application.Models.OrderToCash;
using InteAcc.Common.Domain.Entities.Sales;
using InteAcc.Common.Infrastructure.Data;

namespace InteAcc.Common.Infrastructure.Services;

public class SalesOrderService : ISalesOrderService
{
    private readonly InteAccDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SalesOrderService(
        InteAccDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<SalesOrderDto>> GetSalesOrdersAsync(int? customerId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = _dbContext.SalesOrders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.CompanyId == companyId);

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        var orders = await query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id).ToListAsync(cancellationToken);
        if (!orders.Any()) return Array.Empty<SalesOrderDto>();

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var centerIds = orders.Select(o => o.SalesCenterId).Distinct().ToList();
        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();

        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var centers = await _dbContext.SalesCenters.AsNoTracking()
            .Where(sc => centerIds.Contains(sc.Id))
            .ToDictionaryAsync(sc => sc.Id, cancellationToken);

        var products = await _dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return orders.Select(o => new SalesOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            OrderDate = o.OrderDate,
            CustomerId = o.CustomerId,
            CustomerCode = customers.TryGetValue(o.CustomerId, out var cust) ? cust.CustomerCode : string.Empty,
            CustomerName = cust != null ? cust.CustomerName : string.Empty,
            SalesCenterId = o.SalesCenterId,
            SalesCenterName = centers.TryGetValue(o.SalesCenterId, out var ctr) ? ctr.CenterName : string.Empty,
            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus,
            Lines = o.Lines.OrderBy(l => l.LineNumber).Select(l => new SalesOrderLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var prod) ? prod.ProductCode : string.Empty,
                ProductName = prod != null ? prod.ProductName : string.Empty,
                OrderedQuantity = l.OrderedQuantity,
                ShippedQuantity = l.ShippedQuantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal
            }).ToList()
        }).ToList();
    }

    public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var order = await _dbContext.SalesOrders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken);

        if (order == null) return null;

        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == order.CustomerId, cancellationToken);
        var center = await _dbContext.SalesCenters.AsNoTracking().FirstOrDefaultAsync(sc => sc.Id == order.SalesCenterId, cancellationToken);

        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new SalesOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            CustomerId = order.CustomerId,
            CustomerCode = customer?.CustomerCode ?? string.Empty,
            CustomerName = customer?.CustomerName ?? string.Empty,
            SalesCenterId = order.SalesCenterId,
            SalesCenterName = center?.CenterName ?? string.Empty,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            Lines = order.Lines.OrderBy(l => l.LineNumber).Select(l => new SalesOrderLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var prod) ? prod.ProductCode : string.Empty,
                ProductName = prod != null ? prod.ProductName : string.Empty,
                OrderedQuantity = l.OrderedQuantity,
                ShippedQuantity = l.ShippedQuantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal
            }).ToList()
        };
    }

    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} was not found.");
        }

        var center = await _dbContext.SalesCenters
            .FirstOrDefaultAsync(sc => sc.Id == request.SalesCenterId && sc.CompanyId == companyId, cancellationToken);
        if (center == null)
        {
            throw new InvalidOperationException($"Sales Center with ID {request.SalesCenterId} was not found.");
        }

        if (request.Lines == null || !request.Lines.Any())
        {
            throw new InvalidOperationException("At least one line item is required for a Sales Order.");
        }

        // Calculate order total
        decimal totalAmount = request.Lines.Sum(l => l.OrderedQuantity * l.UnitPrice);

        // Credit limit validation
        if (customer.CreditLimit > 0)
        {
            decimal potentialExposure = customer.CurrentBalance + totalAmount;
            if (potentialExposure > customer.CreditLimit)
            {
                throw new InvalidOperationException(
                    $"Credit limit exceeded for customer '{customer.CustomerName}'. Credit Limit: {customer.CreditLimit:C}, Current Balance: {customer.CurrentBalance:C}, Order Total: {totalAmount:C}. Total exposure would be {potentialExposure:C}.");
            }
        }

        // Generate Order Number: SO-yyyyMM-XXXX
        var prefix = $"SO-{_dateTimeProvider.UtcNow:yyyyMM}-";
        var countThisMonth = await _dbContext.SalesOrders
            .CountAsync(o => o.CompanyId == companyId && o.OrderNumber.StartsWith(prefix), cancellationToken);
        var orderNumber = $"{prefix}{(countThisMonth + 1):D4}";

        var salesOrder = new SalesOrder
        {
            CompanyId = companyId,
            OrderNumber = orderNumber,
            OrderDate = request.OrderDate,
            CustomerId = request.CustomerId,
            SalesCenterId = request.SalesCenterId,
            TotalAmount = totalAmount,
            OrderStatus = SalesOrderStatus.Draft
        };

        int lineNo = 1;
        foreach (var l in request.Lines)
        {
            if (l.OrderedQuantity <= 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Ordered quantity must be greater than zero.");
            }
            if (l.UnitPrice < 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Unit price cannot be negative.");
            }

            salesOrder.Lines.Add(new SalesOrderLine
            {
                LineNumber = lineNo++,
                ProductId = l.ProductId,
                OrderedQuantity = l.OrderedQuantity,
                ShippedQuantity = 0,
                UnitPrice = l.UnitPrice,
                LineTotal = l.OrderedQuantity * l.UnitPrice
            });
        }

        _dbContext.SalesOrders.Add(salesOrder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSalesOrderByIdAsync(salesOrder.Id, cancellationToken))!;
    }

    public async Task<SalesOrderDto> ConfirmSalesOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var order = await _dbContext.SalesOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Sales Order with ID {id} was not found.");
        }

        if (order.OrderStatus != SalesOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Sales Order {order.OrderNumber} is currently '{order.OrderStatus}' and cannot be confirmed.");
        }

        order.OrderStatus = SalesOrderStatus.Confirmed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSalesOrderByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<SalesCenterDto>> GetSalesCentersAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from sc in _dbContext.SalesCenters.AsNoTracking()
                    where sc.CompanyId == companyId
                    join w in _dbContext.Warehouses.AsNoTracking() on sc.WarehouseId equals w.Id into wJoin
                    from w in wJoin.DefaultIfEmpty()
                    select new SalesCenterDto
                    {
                        Id = sc.Id,
                        CenterCode = sc.CenterCode,
                        CenterName = sc.CenterName,
                        WarehouseId = sc.WarehouseId,
                        WarehouseName = w != null ? w.WarehouseName : string.Empty,
                        IsActive = sc.IsActive
                    };

        return await query.OrderBy(sc => sc.CenterCode).ToListAsync(cancellationToken);
    }

    public async Task<SalesCenterDto> CreateSalesCenterAsync(CreateSalesCenterRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();

        var existing = await _dbContext.SalesCenters
            .AnyAsync(sc => sc.CompanyId == companyId && sc.CenterCode == request.CenterCode, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"Sales center with code '{request.CenterCode}' already exists.");
        }

        var warehouse = await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == companyId, cancellationToken);
        if (warehouse == null)
        {
            throw new InvalidOperationException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        var center = new SalesCenter
        {
            CompanyId = companyId,
            CenterCode = request.CenterCode.Trim(),
            CenterName = request.CenterName.Trim(),
            WarehouseId = request.WarehouseId,
            IsActive = true
        };

        _dbContext.SalesCenters.Add(center);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SalesCenterDto
        {
            Id = center.Id,
            CenterCode = center.CenterCode,
            CenterName = center.CenterName,
            WarehouseId = center.WarehouseId,
            WarehouseName = warehouse.WarehouseName,
            IsActive = center.IsActive
        };
    }
}
