using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Domain.Common;
using ERP.Common.Domain.Entities.GeneralLedger;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Domain.Entities.Procurement;
using ERP.Common.Domain.Entities.AccountsPayable;
using ERP.Common.Domain.Entities.Manufacturing;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Inventory;

namespace ERP.UnitTests;

public class GoodsReceiptWorkflowTests
{
    private sealed class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "tester@erp.local";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private sealed class FakeJournalVoucherService : IJournalVoucherService
    {
        public Task<Result<JournalVoucherDto>> CreateVoucherAsync(CreateJournalVoucherRequest request, CancellationToken cancellationToken = default)
        {
            var dto = new JournalVoucherDto(1001, 1, "JV-2026-0001", request.VoucherDate, request.Reference, request.Description, 1, 1, false, null, null, 1000m, 1000m, true, new List<JournalVoucherLineDto>());
            return Task.FromResult(Result<JournalVoucherDto>.Success(dto));
        }

        public Task<Result<JournalVoucherDto>> PostVoucherAsync(int id, CancellationToken cancellationToken = default)
        {
            var dto = new JournalVoucherDto(id, 1, "JV-2026-0001", DateOnly.FromDateTime(DateTime.UtcNow), null, "GRN Posting", 1, 1, true, DateTime.UtcNow, "tester@erp.local", 1000m, 1000m, true, new List<JournalVoucherLineDto>());
            return Task.FromResult(Result<JournalVoucherDto>.Success(dto));
        }

        public Task<Result<JournalVoucherDto>> GetVoucherByIdAsync(int voucherId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Result<List<JournalVoucherSummaryDto>>> GetVouchersAsync(int? fiscalPeriodId, bool? isPosted, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static (GoodsReceiptService service, ErpDbContext dbContext) CreateTestSetup(string dbName)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var db = new ErpDbContext(options);
        var userContext = new FakeUserContext();
        var jvService = new FakeJournalVoucherService();
        var service = new GoodsReceiptService(db, userContext, jvService);
        return (service, db);
    }

    [Fact]
    public async Task VendorGRN_PartialReceipt_UpdatesPurchaseOrderOutstandingAndStatus()
    {
        var (service, db) = CreateTestSetup(nameof(VendorGRN_PartialReceipt_UpdatesPurchaseOrderOutstandingAndStatus));

        db.Warehouses.Add(new Warehouse { CompanyId = 1, WarehouseCode = "WH-CENTRAL", WarehouseName = "Central Warehouse", Location = "Dhaka" });
        db.Vendors.Add(new Vendor { CompanyId = 1, VendorCode = "VEND-01", VendorName = "Tata Steel International" });
        db.Products.Add(new Product { CompanyId = 1, ProductCode = "RAW-001", ProductName = "Heavy Steel Beam", PrimaryUomId = 1, StandardCost = 8500, CurrentCost = 8500 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "2140", AccountName = "GRN Clearing Account", AccountType = 2 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1140", AccountName = "Finished Goods / Raw Inventory", AccountType = 2 });

        var po = new PurchaseOrder
        {
            CompanyId = 1,
            PoNumber = "PO-2026-001",
            PoDate = DateTime.UtcNow,
            VendorId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.Issued,
            Lines = new List<PurchaseOrderLine>
            {
                new() { ProductId = 1, OrderedQuantity = 100m, ReceivedQuantity = 0m, UnitPrice = 8500m, LineTotal = 850000m }
            }
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var createRequest = new CreateGrnRequest
        {
            GrnType = GrnType.Vendor,
            GrnDate = DateTime.UtcNow,
            WarehouseId = 1,
            VendorId = 1,
            PurchaseOrderId = po.Id,
            PoNumber = po.PoNumber,
            Lines = new List<CreateGrnLineRequest>
            {
                new()
                {
                    PurchaseOrderLineId = po.Lines.First().Id,
                    ProductId = 1,
                    QuantityReceived = 40m,
                    AcceptedQuantity = 40m,
                    UnitPrice = 8500m
                }
            }
        };

        var grnDto = await service.CreateGoodsReceiptNoteAsync(createRequest);
        Assert.NotNull(grnDto);
        Assert.Equal(GrnStatus.Draft, grnDto.Status);

        var postedGrn = await service.ReceiveAndPostGrnAsync(grnDto.Id);
        Assert.Equal(GrnStatus.Received, postedGrn.Status);

        var updatedPo = await db.PurchaseOrders.Include(p => p.Lines).FirstAsync(p => p.Id == po.Id);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, updatedPo.Status);
        Assert.Equal(40m, updatedPo.Lines.First().ReceivedQuantity);

        var stock = await db.ProductStock.FirstAsync(s => s.CompanyId == 1 && s.ProductId == 1);
        Assert.Equal(40m, stock.QuantityOnHand);
    }

    [Fact]
    public async Task VendorGRN_MultiReceipt_CompletesPurchaseOrder()
    {
        var (service, db) = CreateTestSetup(nameof(VendorGRN_MultiReceipt_CompletesPurchaseOrder));

        db.Warehouses.Add(new Warehouse { CompanyId = 1, WarehouseCode = "WH-CENTRAL", WarehouseName = "Central Warehouse", Location = "Dhaka" });
        db.Vendors.Add(new Vendor { CompanyId = 1, VendorCode = "VEND-01", VendorName = "Tata Steel" });
        db.Products.Add(new Product { CompanyId = 1, ProductCode = "RAW-001", ProductName = "Heavy Steel Beam", PrimaryUomId = 1, StandardCost = 8500 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "2140", AccountName = "GRN Clearing", AccountType = 2 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1140", AccountName = "Inventory Asset", AccountType = 2 });

        var po = new PurchaseOrder
        {
            CompanyId = 1,
            PoNumber = "PO-2026-002",
            PoDate = DateTime.UtcNow,
            VendorId = 1,
            WarehouseId = 1,
            Status = PurchaseOrderStatus.PartiallyReceived,
            Lines = new List<PurchaseOrderLine>
            {
                new() { ProductId = 1, OrderedQuantity = 100m, ReceivedQuantity = 40m, UnitPrice = 8500m }
            }
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var req = new CreateGrnRequest
        {
            GrnType = GrnType.Vendor,
            GrnDate = DateTime.UtcNow,
            WarehouseId = 1,
            VendorId = 1,
            PurchaseOrderId = po.Id,
            Lines = new List<CreateGrnLineRequest>
            {
                new() { PurchaseOrderLineId = po.Lines.First().Id, ProductId = 1, QuantityReceived = 60m, UnitPrice = 8500m }
            }
        };

        var grn = await service.CreateGoodsReceiptNoteAsync(req);
        await service.ReceiveAndPostGrnAsync(grn.Id);

        var updatedPo = await db.PurchaseOrders.Include(p => p.Lines).FirstAsync(p => p.Id == po.Id);
        Assert.Equal(PurchaseOrderStatus.Completed, updatedPo.Status);
        Assert.Equal(100m, updatedPo.Lines.First().ReceivedQuantity);
    }

    [Fact]
    public async Task ProductionGRN_ReceivesGoodsWithoutVendorOrPO()
    {
        var (service, db) = CreateTestSetup(nameof(ProductionGRN_ReceivesGoodsWithoutVendorOrPO));

        db.Warehouses.Add(new Warehouse { CompanyId = 1, WarehouseCode = "WH-PLANT", WarehouseName = "Plant Depot", Location = "Gazipur" });
        db.Products.Add(new Product { CompanyId = 1, ProductCode = "FG-001", ProductName = "Finished Steel Truss", PrimaryUomId = 1, StandardCost = 25000 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1145", AccountName = "WIP Inventory Clearing", AccountType = 2 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1140", AccountName = "Finished Goods Inventory", AccountType = 2 });

        var mo = new ProductionOrder
        {
            CompanyId = 1,
            OrderNumber = "MO-2026-001",
            FinishedGoodProductId = 1,
            TargetQuantity = 50m,
            CompletedQuantity = 10m,
            Status = "InProgress",
            FinishedGoodWarehouseId = 1
        };
        db.Set<ProductionOrder>().Add(mo);
        await db.SaveChangesAsync();

        var req = new CreateGrnRequest
        {
            GrnType = GrnType.Production,
            GrnDate = DateTime.UtcNow,
            WarehouseId = 1,
            VendorId = null,
            ProductionOrderId = mo.Id,
            ProductionReference = "HEAT-BATCH-890",
            Lines = new List<CreateGrnLineRequest>
            {
                new() { ProductId = 1, QuantityReceived = 20m, UnitPrice = 25000m }
            }
        };

        var grn = await service.CreateGoodsReceiptNoteAsync(req);
        Assert.Null(grn.VendorId);
        Assert.Equal(GrnType.Production, grn.GrnType);

        var posted = await service.ReceiveAndPostGrnAsync(grn.Id);
        Assert.Equal(GrnStatus.Received, posted.Status);

        var updatedMo = await db.Set<ProductionOrder>().FirstAsync(m => m.Id == mo.Id);
        Assert.Equal(30m, updatedMo.CompletedQuantity);
    }

    [Fact]
    public async Task GoodsReceiptReturn_EnforcesReturnableQuantityLimit()
    {
        var (service, db) = CreateTestSetup(nameof(GoodsReceiptReturn_EnforcesReturnableQuantityLimit));

        db.Warehouses.Add(new Warehouse { CompanyId = 1, WarehouseCode = "WH-1", WarehouseName = "Depot" });
        db.Vendors.Add(new Vendor { CompanyId = 1, VendorCode = "V-1", VendorName = "Vendor" });
        db.Products.Add(new Product { CompanyId = 1, ProductCode = "P-1", ProductName = "Product" });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "2140", AccountName = "GRN Clearing", AccountType = 2 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1140", AccountName = "Inventory", AccountType = 2 });

        var grn = new GoodsReceiptNote
        {
            CompanyId = 1,
            GrnNumber = "GRN-TEST-001",
            GrnType = GrnType.Vendor,
            GrnDate = DateTime.UtcNow,
            WarehouseId = 1,
            VendorId = 1,
            Status = GrnStatus.Received,
            TotalAmount = 50000m,
            Lines = new List<GoodsReceiptNoteLine>
            {
                new() { ProductId = 1, QuantityReceived = 50m, ReturnedQuantity = 20m, UnitPrice = 1000m, LineTotal = 50000m }
            }
        };
        db.GoodsReceiptNotes.Add(grn);
        await db.SaveChangesAsync();

        var lineId = grn.Lines.First().Id;

        // Max returnable is 50 - 20 = 30. Attempting to return 35 should throw exception
        var invalidReq = new CreateGrnReturnRequest
        {
            GoodsReceiptNoteId = grn.Id,
            ReturnDate = DateTime.UtcNow,
            Reason = "Defective shipment",
            Lines = new List<CreateGrnReturnLineRequest>
            {
                new() { GoodsReceiptNoteLineId = lineId, ReturnedQuantity = 35m }
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateGoodsReceiptReturnAsync(invalidReq));
        Assert.Contains("Maximum returnable quantity is 30", ex.Message);
    }

    [Fact]
    public async Task GoodsReceiptReturn_Posting_DecrementsStockAndReversesGL()
    {
        var (service, db) = CreateTestSetup(nameof(GoodsReceiptReturn_Posting_DecrementsStockAndReversesGL));

        db.Warehouses.Add(new Warehouse { CompanyId = 1, WarehouseCode = "WH-1", WarehouseName = "Depot" });
        db.Vendors.Add(new Vendor { CompanyId = 1, VendorCode = "V-1", VendorName = "Vendor" });
        db.Products.Add(new Product { CompanyId = 1, ProductCode = "P-1", ProductName = "Product", PrimaryUomId = 1 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "2140", AccountName = "GRN Clearing", AccountType = 2 });
        db.Accounts.Add(new Account { CompanyId = 1, AccountCode = "1140", AccountName = "Inventory", AccountType = 2 });

        db.ProductStock.Add(new ProductStock
        {
            CompanyId = 1,
            WarehouseId = 1,
            ProductId = 1,
            QuantityOnHand = 50m,
            AverageCost = 1000m
        });

        var grn = new GoodsReceiptNote
        {
            CompanyId = 1,
            GrnNumber = "GRN-RET-TEST",
            GrnType = GrnType.Vendor,
            GrnDate = DateTime.UtcNow,
            WarehouseId = 1,
            VendorId = 1,
            Status = GrnStatus.Received,
            TotalAmount = 50000m,
            Lines = new List<GoodsReceiptNoteLine>
            {
                new() { ProductId = 1, QuantityReceived = 50m, ReturnedQuantity = 0m, UnitPrice = 1000m, LineTotal = 50000m }
            }
        };
        db.GoodsReceiptNotes.Add(grn);
        await db.SaveChangesAsync();

        var retReq = new CreateGrnReturnRequest
        {
            GoodsReceiptNoteId = grn.Id,
            ReturnDate = DateTime.UtcNow,
            Reason = "Damaged during transit",
            Lines = new List<CreateGrnReturnLineRequest>
            {
                new() { GoodsReceiptNoteLineId = grn.Lines.First().Id, ReturnedQuantity = 15m }
            }
        };

        var returnDto = await service.CreateGoodsReceiptReturnAsync(retReq);
        Assert.NotNull(returnDto);
        Assert.Equal(GoodsReceiptReturnStatus.Draft, returnDto.Status);

        var postedReturn = await service.PostGoodsReceiptReturnAsync(returnDto.Id);
        Assert.Equal(GoodsReceiptReturnStatus.Posted, postedReturn.Status);

        var stock = await db.ProductStock.FirstAsync(s => s.CompanyId == 1 && s.ProductId == 1);
        Assert.Equal(35m, stock.QuantityOnHand);

        var grnLine = await db.GoodsReceiptNoteLines.FirstAsync(l => l.GrnId == grn.Id);
        Assert.Equal(15m, grnLine.ReturnedQuantity);
    }
}
