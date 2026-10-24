using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Models.Inventory;
using ERP.Common.Domain.Entities.Inventory;
using ERP.Common.Infrastructure.Data;
using ERP.Common.Infrastructure.Services.Common;
using ERP.Common.Infrastructure.Services.Inventory;

namespace ERP.UnitTests;

public class DepotAndWarehouseSeparationTests
{
    private sealed class FakeUserContext : ICurrentUserContext
    {
        public string? UserId { get; set; } = "1";
        public string? UserName { get; set; } = "tester@erp.local";
        public int? CompanyId { get; set; } = 1;
        public bool IsAuthenticated => true;
        public bool HasAccessToCompany(int companyId) => CompanyId == companyId;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
    }

    private static (InventoryService service, ErpDbContext dbContext) CreateTestSetup(string dbName)
    {
        var user = new FakeUserContext();
        var clock = new FixedClock();
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var db = new ErpDbContext(options, clock, user);
        var seq = new DocumentSequenceService(db);
        var service = new InventoryService(db, user, seq);
        return (service, db);
    }

    [Fact]
    public async Task Depot_CanBeCreated_WithMultipleChildWarehousesOfDifferentTypes()
    {
        var (service, db) = CreateTestSetup(nameof(Depot_CanBeCreated_WithMultipleChildWarehousesOfDifferentTypes));

        // 1. Create a Depot (Factory / Manufacturing Plant)
        var depotRequest = new CreateDepotRequest
        {
            DepotCode = "DEP-TG-01",
            DepotName = "Tongi Structural PEB Factory",
            DepotType = (int)DepotType.ManufacturingPlant,
            City = "Gazipur",
            Address = "Plot 12-14, Tongi Heavy Industrial Zone",
            ContactPerson = "Engr. M. A. Rashid (Plant Head)",
            ContactPhone = "+880 1711-445566"
        };
        var depotDto = await service.CreateDepotAsync(depotRequest);
        Assert.NotNull(depotDto);
        Assert.Equal("DEP-TG-01", depotDto.DepotCode);
        Assert.Equal("Manufacturing Plant", depotDto.DepotTypeName);

        // 2. Create Warehouse 1 under this Depot (Raw Materials)
        var whRaw = await service.CreateWarehouseAsync(new CreateWarehouseRequest
        {
            WarehouseCode = "WH-RAW-01",
            WarehouseName = "Heavy Steel Plates & Coils Yard",
            Location = "Bay A & B",
            DepotId = depotDto.Id,
            WarehouseType = (int)WarehouseType.RawMaterial
        });

        // 3. Create Warehouse 2 under this Depot (Finished Goods)
        var whFg = await service.CreateWarehouseAsync(new CreateWarehouseRequest
        {
            WarehouseCode = "WH-FG-01",
            WarehouseName = "Fabricated PEB Trusses Store",
            Location = "Bay C (Holding Area)",
            DepotId = depotDto.Id,
            WarehouseType = (int)WarehouseType.FinishedGoods
        });

        // 4. Create Warehouse 3 under this Depot (Manufacturing Spares)
        var whSpares = await service.CreateWarehouseAsync(new CreateWarehouseRequest
        {
            WarehouseCode = "WH-SPR-01",
            WarehouseName = "Fasteners, Bolts & Welding Consumables",
            Location = "Store Room 2",
            DepotId = depotDto.Id,
            WarehouseType = (int)WarehouseType.SparesAndHardware
        });

        // 5. Query warehouses and verify parent link & types
        var allWarehouses = await service.GetWarehousesAsync();
        Assert.Equal(3, allWarehouses.Count);

        var rawResult = allWarehouses.First(w => w.WarehouseCode == "WH-RAW-01");
        Assert.Equal("Raw Materials", rawResult.WarehouseTypeName);
        Assert.Equal("Tongi Structural PEB Factory", rawResult.DepotName);

        var fgResult = allWarehouses.First(w => w.WarehouseCode == "WH-FG-01");
        Assert.Equal("Finished Goods", fgResult.WarehouseTypeName);
        Assert.Equal("Tongi Structural PEB Factory", fgResult.DepotName);

        var sparesResult = allWarehouses.First(w => w.WarehouseCode == "WH-SPR-01");
        Assert.Equal("Spares & Fasteners", sparesResult.WarehouseTypeName);

        // 6. Query depots and verify child warehouse count
        var allDepots = await service.GetDepotsAsync();
        Assert.Single(allDepots);
        Assert.Equal(3, allDepots[0].WarehouseCount);
    }

    [Fact]
    public async Task Warehouse_CanExistAsStandalone_WithoutParentDepot()
    {
        var (service, db) = CreateTestSetup(nameof(Warehouse_CanExistAsStandalone_WithoutParentDepot));

        var wh = await service.CreateWarehouseAsync(new CreateWarehouseRequest
        {
            WarehouseCode = "WH-TRANSIT",
            WarehouseName = "Floating Inland Barge Transit Store",
            Location = "Narayanganj River Port",
            DepotId = null,
            WarehouseType = (int)WarehouseType.GeneralTransit
        });

        Assert.NotNull(wh);
        Assert.Null(wh.DepotId);
        Assert.Null(wh.DepotName);
        Assert.Equal("General Transit", wh.WarehouseTypeName);
    }
}
