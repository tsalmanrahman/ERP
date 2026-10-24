-- =========================================================================
-- Migration 014: Create Depots Table and Link Warehouses with WarehouseType
-- Enterprise Physical Facility (Depot/Plant) vs. Inventory Holding Zone (Warehouse)
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Depots' AND schema_id = SCHEMA_ID('inv'))
BEGIN
    CREATE TABLE [inv].[Depots] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CompanyId] INT NOT NULL,
        [DepotCode] NVARCHAR(20) NOT NULL,
        [DepotName] NVARCHAR(100) NOT NULL,
        [DepotType] INT NOT NULL DEFAULT 1, -- 1=Manufacturing Plant, 2=Regional Logistics Depot, 3=Central Distribution Hub
        [Address] NVARCHAR(250) NULL,
        [City] NVARCHAR(100) NULL,
        [ContactPerson] NVARCHAR(100) NULL,
        [ContactPhone] NVARCHAR(50) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,

        -- Auditing & Concurrency
        [CreatedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifiedAtUtc] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAtUtc] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,

        CONSTRAINT [PK_Depots] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Depots_Companies] FOREIGN KEY ([CompanyId]) REFERENCES [sa].[Companies] ([Id])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Depots_Company_Code]
        ON [inv].[Depots]([CompanyId], [DepotCode])
        WHERE [IsDeleted] = 0;
END;
GO

-- Enhance inv.Warehouses with DepotId and WarehouseType
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('inv.Warehouses') AND name = 'DepotId'
)
BEGIN
    ALTER TABLE [inv].[Warehouses]
    ADD [DepotId] INT NULL;

    ALTER TABLE [inv].[Warehouses]
    ADD CONSTRAINT [FK_Warehouses_Depots]
    FOREIGN KEY ([DepotId]) REFERENCES [inv].[Depots] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('inv.Warehouses') AND name = 'WarehouseType'
)
BEGIN
    ALTER TABLE [inv].[Warehouses]
    ADD [WarehouseType] INT NOT NULL DEFAULT 2; -- 1=Raw Material, 2=Finished Goods, 3=WIP, 4=Spares & Hardware, 5=General/Transit
END;
GO

-- Seed Baseline Physical Depots for Companies 1, 2, 3
IF NOT EXISTS (SELECT 1 FROM [inv].[Depots] WHERE [DepotCode] = 'DEP-TG-01')
BEGIN
    INSERT INTO [inv].[Depots] ([CompanyId], [DepotCode], [DepotName], [DepotType], [Address], [City], [ContactPerson], [ContactPhone], [IsActive], [CreatedAtUtc])
    VALUES 
        (1, 'DEP-TG-01', 'Tongi PEB Structural Fabrication Plant', 1, 'Plot 12-14, Tongi Heavy Industrial Zone', 'Gazipur', 'Engr. M. A. Rashid (Plant Head)', '+880 1711-445566', 1, SYSUTCDATETIME()),
        (1, 'DEP-DHK-01', 'Tejgaon Central Logistics & Distribution Hub', 3, 'Plot 45, Tejgaon Commercial Area', 'Dhaka', 'Tanvir Hossain (Logistics Lead)', '+880 1819-223344', 1, SYSUTCDATETIME()),
        (1, 'DEP-CTG-01', 'Chittagong Maritime Terminal Depot', 2, 'North Patenga Maritime Port Access', 'Chittagong', 'Kamrul Hasan (Depot Manager)', '+880 1912-334455', 1, SYSUTCDATETIME());

    -- Link existing Warehouses to the seeded Depots
    UPDATE w
    SET w.[DepotId] = d.[Id],
        w.[WarehouseType] = CASE 
            WHEN w.[WarehouseCode] LIKE '%RAW%' THEN 1
            WHEN w.[WarehouseCode] LIKE '%FG%' OR w.[WarehouseCode] LIKE '%PLANT%' THEN 2
            WHEN w.[WarehouseCode] LIKE '%SPARE%' THEN 4
            ELSE 5
        END
    FROM [inv].[Warehouses] w
    CROSS JOIN (SELECT TOP 1 [Id] FROM [inv].[Depots] WHERE [DepotCode] = 'DEP-TG-01') d
    WHERE w.[DepotId] IS NULL;
END;
GO
