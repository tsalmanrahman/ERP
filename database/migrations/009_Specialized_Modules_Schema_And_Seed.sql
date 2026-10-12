-- ==============================================================================
-- 009_Specialized_Modules_Schema_And_Seed.sql
-- Modern ERP Specialized Modules: Fixed Assets (fa), Real Estate (re), Manufacturing (mfg)
-- ==============================================================================

USE ERP;
GO

-- 1. Create Required Schemas
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 're')
BEGIN
    EXEC('CREATE SCHEMA re');
END;
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'mfg')
BEGIN
    EXEC('CREATE SCHEMA mfg');
END;
GO

-- 2. Ensure Specialized GL Accounts exist for Company 1
DECLARE @CompanyId INT = 1;

-- 1145 Raw Materials Inventory Control (Asset, Group 1)
IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '1145')
BEGIN
    INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsActive, CreatedBy)
    VALUES (@CompanyId, '1145', 'Raw Materials Inventory Control', 1, 2, 1, 'System');
END;

-- 2150 Customer Booking Advances & Deposits (Liability, Group 3)
IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '2150')
BEGIN
    INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsActive, CreatedBy)
    VALUES (@CompanyId, '2150', 'Customer Booking Advances & Deposits', 3, 2, 1, 'System');
END;

-- 4110 Real Estate Property Sales Revenue (Revenue, Group 6)
IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '4110')
BEGIN
    INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsActive, CreatedBy)
    VALUES (@CompanyId, '4110', 'Real Estate Property Sales Revenue', 6, 2, 1, 'System');
END;
GO

-- ==============================================================================
-- 3. FIXED ASSETS SCHEMA ENHANCEMENTS (fa)
-- ==============================================================================

-- Enhance fa.Assets
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'SalvageValue')
BEGIN
    ALTER TABLE fa.Assets ADD SalvageValue DECIMAL(18, 4) NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'UsefulLifeMonths')
BEGIN
    ALTER TABLE fa.Assets ADD UsefulLifeMonths INT NOT NULL DEFAULT 60;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'DepreciationMethod')
BEGIN
    ALTER TABLE fa.Assets ADD DepreciationMethod NVARCHAR(20) NOT NULL DEFAULT 'StraightLine';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'DisposalDate')
BEGIN
    ALTER TABLE fa.Assets ADD DisposalDate DATE NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'DisposalAmount')
BEGIN
    ALTER TABLE fa.Assets ADD DisposalAmount DECIMAL(18, 4) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'GainLossAmount')
BEGIN
    ALTER TABLE fa.Assets ADD GainLossAmount DECIMAL(18, 4) NULL;
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'fa.Assets') AND name = 'DisposalJournalVoucherId')
BEGIN
    ALTER TABLE fa.Assets ADD DisposalJournalVoucherId INT NULL CONSTRAINT FK_fa_Assets_DisposalVoucher REFERENCES gl.JournalVouchers(Id);
END;
GO

-- Create fa.DepreciationEntries
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'fa.DepreciationEntries'))
BEGIN
    CREATE TABLE fa.DepreciationEntries (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_fa_Depr_Company REFERENCES sa.Companies(Id),
        AssetId INT NOT NULL CONSTRAINT FK_fa_Depr_Asset REFERENCES fa.Assets(Id),
        ScheduleDate DATE NOT NULL,
        Year INT NOT NULL,
        Month INT NOT NULL,
        DepreciationAmount DECIMAL(18, 4) NOT NULL,
        AccumulatedDepreciationAfter DECIMAL(18, 4) NOT NULL,
        BookValueAfter DECIMAL(18, 4) NOT NULL,
        JournalVoucherId INT NULL CONSTRAINT FK_fa_Depr_Voucher REFERENCES gl.JournalVouchers(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System'
    );

    CREATE NONCLUSTERED INDEX IX_fa_DepreciationEntries_Asset_Date 
    ON fa.DepreciationEntries(AssetId, ScheduleDate);
END;
GO

-- Seed Sample Asset Groups and Asset for Company 1
DECLARE @CompanyId INT = 1;
DECLARE @MachineryGlId INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '1210');
DECLARE @FurnitureGlId INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '1220');
DECLARE @AccumDeprGlId INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '1290');
DECLARE @DeprExpGlId INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompanyId AND AccountCode = '5250');

IF NOT EXISTS (SELECT 1 FROM fa.AssetGroups WHERE CompanyId = @CompanyId AND GroupCode = 'GRP-MACH')
BEGIN
    INSERT INTO fa.AssetGroups (CompanyId, GroupCode, GroupName, DepreciationRate, GlAssetAccountId, GlAccumDeprAccountId, GlDeprExpenseAccountId, CreatedBy)
    VALUES (@CompanyId, 'GRP-MACH', 'Plant & Factory Machinery', 10.00, @MachineryGlId, @AccumDeprGlId, @DeprExpGlId, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM fa.AssetGroups WHERE CompanyId = @CompanyId AND GroupCode = 'GRP-FURN')
BEGIN
    INSERT INTO fa.AssetGroups (CompanyId, GroupCode, GroupName, DepreciationRate, GlAssetAccountId, GlAccumDeprAccountId, GlDeprExpenseAccountId, CreatedBy)
    VALUES (@CompanyId, 'GRP-FURN', 'Office Furniture & Fixtures', 15.00, @FurnitureGlId, @AccumDeprGlId, @DeprExpGlId, 'System');
END;

DECLARE @MachGroupId INT = (SELECT Id FROM fa.AssetGroups WHERE CompanyId = @CompanyId AND GroupCode = 'GRP-MACH');
DECLARE @FurnGroupId INT = (SELECT Id FROM fa.AssetGroups WHERE CompanyId = @CompanyId AND GroupCode = 'GRP-FURN');

IF NOT EXISTS (SELECT 1 FROM fa.Assets WHERE CompanyId = @CompanyId AND AssetCode = 'AST-GEN-01')
BEGIN
    INSERT INTO fa.Assets (
        CompanyId, AssetCode, AssetName, AssetGroupId, PurchaseDate, PurchaseCost,
        CurrentBookValue, AccumulatedDepreciation, SalvageValue, UsefulLifeMonths,
        DepreciationMethod, IsDisposed, CreatedBy
    )
    VALUES (
        @CompanyId, 'AST-GEN-01', 'Perkins 100KVA Diesel Silent Generator', @MachGroupId,
        '2026-01-01', 500000.00, 500000.00, 0.00, 50000.00, 60,
        'StraightLine', 0, 'System'
    );
END;

IF NOT EXISTS (SELECT 1 FROM fa.Assets WHERE CompanyId = @CompanyId AND AssetCode = 'AST-DESK-01')
BEGIN
    INSERT INTO fa.Assets (
        CompanyId, AssetCode, AssetName, AssetGroupId, PurchaseDate, PurchaseCost,
        CurrentBookValue, AccumulatedDepreciation, SalvageValue, UsefulLifeMonths,
        DepreciationMethod, IsDisposed, CreatedBy
    )
    VALUES (
        @CompanyId, 'AST-DESK-01', 'Executive Walnut Conference Table & Chairs', @FurnGroupId,
        '2026-01-01', 120000.00, 120000.00, 0.00, 12000.00, 48,
        'ReducingBalance', 0, 'System'
    );
END;
GO

-- ==============================================================================
-- 4. REAL ESTATE & PROPERTY SALES SCHEMA (re)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N're.Projects'))
BEGIN
    CREATE TABLE re.Projects (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_re_Projects_Company REFERENCES sa.Companies(Id),
        ProjectCode NVARCHAR(30) NOT NULL,
        ProjectName NVARCHAR(150) NOT NULL,
        Location NVARCHAR(200) NOT NULL,
        TotalUnits INT NOT NULL DEFAULT 0,
        EstimatedCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT 'UnderConstruction',
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_re_Projects_Code UNIQUE (CompanyId, ProjectCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N're.PropertyUnits'))
BEGIN
    CREATE TABLE re.PropertyUnits (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_re_Units_Company REFERENCES sa.Companies(Id),
        ProjectId INT NOT NULL CONSTRAINT FK_re_Units_Project REFERENCES re.Projects(Id),
        UnitNumber NVARCHAR(30) NOT NULL,
        UnitType NVARCHAR(30) NOT NULL DEFAULT 'Apartment',
        FloorNumber INT NOT NULL DEFAULT 1,
        AreaSqFt DECIMAL(10, 2) NOT NULL DEFAULT 0,
        RatePerSqFt DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalPrice DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Available',
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_re_PropertyUnits_Number UNIQUE (ProjectId, UnitNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N're.PropertyBookings'))
BEGIN
    CREATE TABLE re.PropertyBookings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_re_Bookings_Company REFERENCES sa.Companies(Id),
        ProjectId INT NOT NULL CONSTRAINT FK_re_Bookings_Project REFERENCES re.Projects(Id),
        PropertyUnitId INT NOT NULL CONSTRAINT FK_re_Bookings_Unit REFERENCES re.PropertyUnits(Id),
        CustomerId INT NOT NULL CONSTRAINT FK_re_Bookings_Customer REFERENCES ar.Customers(Id),
        BookingNumber NVARCHAR(30) NOT NULL,
        BookingDate DATE NOT NULL,
        AgreedPrice DECIMAL(18, 4) NOT NULL,
        DownPayment DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalPaid DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Active',
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_re_Bookings_Number UNIQUE (CompanyId, BookingNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N're.BookingInstallments'))
BEGIN
    CREATE TABLE re.BookingInstallments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BookingId INT NOT NULL CONSTRAINT FK_re_Installments_Booking REFERENCES re.PropertyBookings(Id) ON DELETE CASCADE,
        InstallmentNumber INT NOT NULL,
        DueDate DATE NOT NULL,
        AmountDue DECIMAL(18, 4) NOT NULL,
        AmountPaid DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        PaymentDate DATE NULL,
        JournalVoucherId INT NULL CONSTRAINT FK_re_Installments_Voucher REFERENCES gl.JournalVouchers(Id),
        Remarks NVARCHAR(250) NULL
    );
END;
GO

-- Seed Sample Project & Units
DECLARE @CompanyId INT = 1;

IF NOT EXISTS (SELECT 1 FROM re.Projects WHERE CompanyId = @CompanyId AND ProjectCode = 'PRJ-LAKEVIEW')
BEGIN
    INSERT INTO re.Projects (CompanyId, ProjectCode, ProjectName, Location, TotalUnits, EstimatedCost, Status, CreatedBy)
    VALUES (@CompanyId, 'PRJ-LAKEVIEW', 'Lakeview Horizon Luxury Residencies', 'Plot 42, Road 11, Banani, Dhaka', 12, 180000000.00, 'UnderConstruction', 'System');
END;

DECLARE @PrjId INT = (SELECT Id FROM re.Projects WHERE CompanyId = @CompanyId AND ProjectCode = 'PRJ-LAKEVIEW');

IF NOT EXISTS (SELECT 1 FROM re.PropertyUnits WHERE ProjectId = @PrjId AND UnitNumber = 'APT-101')
BEGIN
    INSERT INTO re.PropertyUnits (CompanyId, ProjectId, UnitNumber, UnitType, FloorNumber, AreaSqFt, RatePerSqFt, TotalPrice, Status, CreatedBy)
    VALUES 
    (@CompanyId, @PrjId, 'APT-101', 'Apartment', 1, 2150.00, 12000.00, 25800000.00, 'Available', 'System'),
    (@CompanyId, @PrjId, 'APT-201', 'Apartment', 2, 2150.00, 12500.00, 26875000.00, 'Available', 'System'),
    (@CompanyId, @PrjId, 'PKG-01', 'Parking', 1, 150.00, 4000.00, 600000.00, 'Available', 'System');
END;
GO

-- ==============================================================================
-- 5. MANUFACTURING & BILL OF MATERIALS SCHEMA (mfg)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'mfg.BillOfMaterials'))
BEGIN
    CREATE TABLE mfg.BillOfMaterials (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_mfg_Bom_Company REFERENCES sa.Companies(Id),
        BomNumber NVARCHAR(30) NOT NULL,
        FinishedGoodProductId INT NOT NULL CONSTRAINT FK_mfg_Bom_Product REFERENCES inv.Products(Id),
        QuantityProduced DECIMAL(18, 4) NOT NULL DEFAULT 1,
        Version NVARCHAR(10) NOT NULL DEFAULT '1.0',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_mfg_Bom_Number UNIQUE (CompanyId, BomNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'mfg.BomComponents'))
BEGIN
    CREATE TABLE mfg.BomComponents (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BomId INT NOT NULL CONSTRAINT FK_mfg_BomComp_Bom REFERENCES mfg.BillOfMaterials(Id) ON DELETE CASCADE,
        RawMaterialProductId INT NOT NULL CONSTRAINT FK_mfg_BomComp_Product REFERENCES inv.Products(Id),
        RequiredQuantity DECIMAL(18, 4) NOT NULL,
        ScrapPercentage DECIMAL(5, 2) NOT NULL DEFAULT 0,
        UnitCost DECIMAL(18, 4) NOT NULL DEFAULT 0
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'mfg.ProductionOrders'))
BEGIN
    CREATE TABLE mfg.ProductionOrders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_mfg_ProdOrder_Company REFERENCES sa.Companies(Id),
        OrderNumber NVARCHAR(30) NOT NULL,
        BomId INT NOT NULL CONSTRAINT FK_mfg_ProdOrder_Bom REFERENCES mfg.BillOfMaterials(Id),
        FinishedGoodProductId INT NOT NULL CONSTRAINT FK_mfg_ProdOrder_FG REFERENCES inv.Products(Id),
        TargetQuantity DECIMAL(18, 4) NOT NULL,
        CompletedQuantity DECIMAL(18, 4) NOT NULL DEFAULT 0,
        RawMaterialWarehouseId INT NOT NULL CONSTRAINT FK_mfg_ProdOrder_RawWh REFERENCES inv.Warehouses(Id),
        FinishedGoodWarehouseId INT NOT NULL CONSTRAINT FK_mfg_ProdOrder_FgWh REFERENCES inv.Warehouses(Id),
        OrderDate DATE NOT NULL,
        StartDate DATE NULL,
        CompletionDate DATE NULL,
        TotalMaterialCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Planned',
        JournalVoucherId INT NULL CONSTRAINT FK_mfg_ProdOrder_Voucher REFERENCES gl.JournalVouchers(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_mfg_ProdOrder_Number UNIQUE (CompanyId, OrderNumber)
    );
END;
GO

-- Seed Sample Bill of Materials for Finished Good (Product 1: 55-Inch TV)
DECLARE @CompanyId INT = 1;
DECLARE @FgProductId INT = (SELECT Id FROM inv.Products WHERE CompanyId = @CompanyId AND ProductCode = 'PROD-LED-55');
DECLARE @RawModId INT = (SELECT Id FROM inv.Products WHERE CompanyId = @CompanyId AND ProductCode = 'PROD-PWR-MOD');
DECLARE @RawCblId INT = (SELECT Id FROM inv.Products WHERE CompanyId = @CompanyId AND ProductCode = 'PROD-CBL-HD');

IF @FgProductId IS NOT NULL AND @RawModId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM mfg.BillOfMaterials WHERE CompanyId = @CompanyId AND BomNumber = 'BOM-LED55-01')
BEGIN
    INSERT INTO mfg.BillOfMaterials (CompanyId, BomNumber, FinishedGoodProductId, QuantityProduced, Version, IsActive, CreatedBy)
    VALUES (@CompanyId, 'BOM-LED55-01', @FgProductId, 1.0000, '1.0', 1, 'System');

    DECLARE @BomId INT = SCOPE_IDENTITY();

    INSERT INTO mfg.BomComponents (BomId, RawMaterialProductId, RequiredQuantity, ScrapPercentage, UnitCost)
    VALUES 
    (@BomId, @RawModId, 1.0000, 0.00, 1200.0000),
    (@BomId, @RawCblId, 10.0000, 2.00, 85.0000);
END;
GO
