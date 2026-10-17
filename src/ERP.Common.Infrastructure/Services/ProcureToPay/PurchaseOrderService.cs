using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.ProcureToPay;
using ERP.Common.Application.Models.ProcureToPay;
using ERP.Common.Domain.Entities.Procurement;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.ProcureToPay;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public PurchaseOrderService(ErpDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(int? vendorId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from po in _dbContext.PurchaseOrders.AsNoTracking()
                    where po.CompanyId == companyId
                    join v in _dbContext.Vendors.AsNoTracking() on po.VendorId equals v.Id
                    join w in _dbContext.Warehouses.AsNoTracking() on po.WarehouseId equals w.Id
                    select new { po, v, w };

        if (vendorId.HasValue)
        {
            query = query.Where(x => x.po.VendorId == vendorId.Value);
        }

        var list = await query.OrderByDescending(x => x.po.PoDate).ThenByDescending(x => x.po.Id).ToListAsync(cancellationToken);

        return list.Select(x => new PurchaseOrderDto
        {
            Id = x.po.Id,
            PoNumber = x.po.PoNumber,
            PoDate = x.po.PoDate,
            VendorId = x.v.Id,
            VendorCode = x.v.VendorCode,
            VendorName = x.v.VendorName,
            WarehouseId = x.w.Id,
            WarehouseName = x.w.WarehouseName,
            TotalAmount = x.po.TotalAmount,
            Status = x.po.Status
        }).ToList();
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, cancellationToken);

        if (po == null) return null;

        var vendor = await _dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == po.VendorId, cancellationToken);
        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == po.WarehouseId, cancellationToken);

        var productIds = po.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return new PurchaseOrderDto
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            PoDate = po.PoDate,
            VendorId = po.VendorId,
            VendorCode = vendor?.VendorCode ?? string.Empty,
            VendorName = vendor?.VendorName ?? string.Empty,
            WarehouseId = po.WarehouseId,
            WarehouseName = warehouse?.WarehouseName ?? string.Empty,
            TotalAmount = po.TotalAmount,
            Status = po.Status,
            Lines = po.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ProductId = l.ProductId,
                ProductCode = products.TryGetValue(l.ProductId, out var p) ? p.ProductCode : string.Empty,
                ProductName = p?.ProductName ?? string.Empty,
                OrderedQuantity = l.OrderedQuantity,
                ReceivedQuantity = l.ReceivedQuantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal
            }).ToList()
        };
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        if (request.Lines == null || request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Purchase order must contain at least one line item.");
        }

        var vendorExists = await _dbContext.Vendors.AnyAsync(v => v.CompanyId == companyId && v.Id == request.VendorId, cancellationToken);
        if (!vendorExists)
        {
            throw new InvalidOperationException($"Vendor with ID {request.VendorId} was not found.");
        }

        var warehouseExists = await _dbContext.Warehouses.AnyAsync(w => w.CompanyId == companyId && w.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
        {
            throw new InvalidOperationException($"Warehouse with ID {request.WarehouseId} was not found.");
        }

        var poNumber = await GenerateNextPoNumberAsync(companyId, request.PoDate, cancellationToken);
        decimal totalAmount = 0m;
        var poLines = new List<PurchaseOrderLine>();

        int lineNo = 1;
        foreach (var reqLine in request.Lines)
        {
            if (reqLine.OrderedQuantity <= 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Ordered quantity must be greater than zero.");
            }
            if (reqLine.UnitPrice < 0)
            {
                throw new InvalidOperationException($"Line {lineNo}: Unit price cannot be negative.");
            }

            var lineTotal = Math.Round(reqLine.OrderedQuantity * reqLine.UnitPrice, 4);
            totalAmount += lineTotal;

            poLines.Add(new PurchaseOrderLine
            {
                LineNumber = lineNo++,
                ProductId = reqLine.ProductId,
                OrderedQuantity = reqLine.OrderedQuantity,
                ReceivedQuantity = 0m,
                UnitPrice = reqLine.UnitPrice,
                LineTotal = lineTotal
            });
        }

        var po = new PurchaseOrder
        {
            CompanyId = companyId,
            PoNumber = poNumber,
            PoDate = request.PoDate.Date,
            VendorId = request.VendorId,
            WarehouseId = request.WarehouseId,
            TotalAmount = totalAmount,
            Status = PurchaseOrderStatus.Draft,
            Lines = poLines
        };

        _dbContext.PurchaseOrders.Add(po);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseOrderByIdAsync(po.Id, cancellationToken))!;
    }

    public async Task<PurchaseOrderDto> IssuePurchaseOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var po = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == id, cancellationToken);
        if (po == null)
        {
            throw new InvalidOperationException($"Purchase order with ID {id} was not found.");
        }

        if (po.Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException($"Only draft purchase orders can be issued. Current status: {po.Status}");
        }

        po.Status = PurchaseOrderStatus.Issued;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseOrderByIdAsync(po.Id, cancellationToken))!;
    }

    private async Task<string> GenerateNextPoNumberAsync(int companyId, DateTime date, CancellationToken cancellationToken)
    {
        var prefix = $"PO-{date:yyyyMM}-";
        var last = await _dbContext.PurchaseOrders
            .Where(p => p.CompanyId == companyId && p.PoNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PoNumber)
            .Select(p => p.PoNumber)
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
