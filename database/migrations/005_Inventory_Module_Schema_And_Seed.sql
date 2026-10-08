-- ==============================================================================
-- Migration: 005_Inventory_Module_Schema_And_Seed.sql
-- Description: Inventory Module Schema (ProductStock, GRN, Adjustments, Transfers)
--              and Master Seed Data (UOMs, Product Classes, Warehouses, Products, Vendors)
-- Target Server: Microsoft SQL Server 2025 / Database: ERP
-- ==============================================================================

USE ERP;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------------------------
-- 1. GL CLEARING & ADJUSTMENT ACCOUNTS (For Automated Inventory Postings)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE AccountCode = '2140')
BEGIN
    INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsReconcilable, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (1, '2140', 'Accrued Inventory Purchases / GRN Clearing', 3, 2, 1, 1, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE AccountCode = '5120')
BEGIN
    INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsReconcilable, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (1, '5120', 'Inventory Adjustment Variance (Gain/Loss)', 7, 2, 0, 1, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

-- ------------------------------------------------------------------------------
-- 2. VENDOR GROUPS & VENDORS BASELINE (For GRN Receiving)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM ap.VendorGroups WHERE GroupCode = 'VG-DOMESTIC')
BEGIN
    INSERT INTO ap.VendorGroups (CompanyId, GroupCode, GroupName, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (1, 'VG-DOMESTIC', 'Domestic Raw Material & FG Suppliers', SYSUTCDATETIME(), 'SeedMigration', 0);
END;

DECLARE @VendorGroupId INT = (SELECT Id FROM ap.VendorGroups WHERE GroupCode = 'VG-DOMESTIC');
DECLARE @ApAccountId INT = (SELECT Id FROM gl.Accounts WHERE AccountCode = '2110');

IF NOT EXISTS (SELECT 1 FROM ap.Vendors WHERE VendorCode = 'VEND-TECH-01')
BEGIN
    INSERT INTO ap.Vendors (CompanyId, VendorCode, VendorName, VendorGroupId, ContactPerson, Phone, Email, AddressLine1, City, CountryCode, GlControlAccountId, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (1, 'VEND-TECH-01', 'Summit Electronics Components Ltd.', @VendorGroupId, 'Engr. Mahbub Alam', '+8801711998877', 'sales@summit-elec.com', 'Plot 45, Sector 7, Uttara Commercial Area', 'Dhaka', 'BGD', @ApAccountId, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

-- ------------------------------------------------------------------------------
-- 3. INVENTORY LIVE BALANCE AGGREGATE (inv.ProductStock)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.ProductStock'))
BEGIN
    CREATE TABLE inv.ProductStock (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_ProductStock_Company REFERENCES sa.Companies(Id),
        WarehouseId INT NOT NULL CONSTRAINT FK_inv_ProductStock_Warehouse REFERENCES inv.Warehouses(Id),
        ProductId INT NOT NULL CONSTRAINT FK_inv_ProductStock_Product REFERENCES inv.Products(Id),
        QuantityOnHand DECIMAL(18, 4) NOT NULL DEFAULT 0,
        AllocatedQuantity DECIMAL(18, 4) NOT NULL DEFAULT 0,
        AvailableQuantity AS (QuantityOnHand - AllocatedQuantity),
        AverageCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalValuation AS (QuantityOnHand * AverageCost),
        LastTransactionDate DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_ProductStock_Warehouse_Product UNIQUE (CompanyId, WarehouseId, ProductId)
    );
END;
GO

-- ------------------------------------------------------------------------------
-- 4. GOODS RECEIPT NOTES (inv.GoodsReceiptNotes & inv.GoodsReceiptNoteLines)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.GoodsReceiptNotes'))
BEGIN
    CREATE TABLE inv.GoodsReceiptNotes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptNotes_Company REFERENCES sa.Companies(Id),
        GrnNumber NVARCHAR(30) NOT NULL,
        GrnDate DATE NOT NULL,
        WarehouseId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptNotes_Warehouse REFERENCES inv.Warehouses(Id),
        VendorId INT NULL CONSTRAINT FK_inv_GoodsReceiptNotes_Vendor REFERENCES ap.Vendors(Id),
        PoNumber NVARCHAR(50) NULL,
        ChallanNumber NVARCHAR(50) NULL,
        ChallanDate DATE NULL,
        TotalAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Received/Posted, 2=Cancelled
        JournalVoucherId INT NULL CONSTRAINT FK_inv_GoodsReceiptNotes_JV REFERENCES gl.JournalVouchers(Id),
        Remarks NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_GoodsReceiptNotes_Company_Number UNIQUE (CompanyId, GrnNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.GoodsReceiptNoteLines'))
BEGIN
    CREATE TABLE inv.GoodsReceiptNoteLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        GrnId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptNoteLines_Grn REFERENCES inv.GoodsReceiptNotes(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptNoteLines_Product REFERENCES inv.Products(Id),
        QuantityReceived DECIMAL(18, 4) NOT NULL,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        BatchNumber NVARCHAR(50) NULL,
        ExpiryDate DATE NULL,
        Remarks NVARCHAR(250) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_GoodsReceiptNoteLines_Grn_Line UNIQUE (GrnId, LineNumber)
    );
END;
GO

-- ------------------------------------------------------------------------------
-- 5. STOCK ADJUSTMENTS (inv.StockAdjustments & inv.StockAdjustmentLines)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.StockAdjustments'))
BEGIN
    CREATE TABLE inv.StockAdjustments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_StockAdjustments_Company REFERENCES sa.Companies(Id),
        AdjustmentNumber NVARCHAR(30) NOT NULL,
        AdjustmentDate DATE NOT NULL,
        WarehouseId INT NOT NULL CONSTRAINT FK_inv_StockAdjustments_Warehouse REFERENCES inv.Warehouses(Id),
        AdjustmentType INT NOT NULL, -- 1=PhysicalCountVariance, 2=DamagedScrap, 3=OpeningBalance, 4=Expired
        Reason NVARCHAR(255) NOT NULL,
        TotalAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Approved/Posted, 2=Cancelled
        JournalVoucherId INT NULL CONSTRAINT FK_inv_StockAdjustments_JV REFERENCES gl.JournalVouchers(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_StockAdjustments_Company_Number UNIQUE (CompanyId, AdjustmentNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.StockAdjustmentLines'))
BEGIN
    CREATE TABLE inv.StockAdjustmentLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AdjustmentId INT NOT NULL CONSTRAINT FK_inv_StockAdjustmentLines_Adjustment REFERENCES inv.StockAdjustments(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_inv_StockAdjustmentLines_Product REFERENCES inv.Products(Id),
        AdjustmentQuantity DECIMAL(18, 4) NOT NULL, -- Positive for increase, Negative for decrease
        UnitCost DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        Notes NVARCHAR(250) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_StockAdjustmentLines_Adjustment_Line UNIQUE (AdjustmentId, LineNumber)
    );
END;
GO

-- ------------------------------------------------------------------------------
-- 6. INTER-WAREHOUSE STOCK TRANSFERS (inv.StockTransfers & inv.StockTransferLines)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.StockTransfers'))
BEGIN
    CREATE TABLE inv.StockTransfers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_StockTransfers_Company REFERENCES sa.Companies(Id),
        TransferNumber NVARCHAR(30) NOT NULL,
        TransferDate DATE NOT NULL,
        FromWarehouseId INT NOT NULL CONSTRAINT FK_inv_StockTransfers_FromWh REFERENCES inv.Warehouses(Id),
        ToWarehouseId INT NOT NULL CONSTRAINT FK_inv_StockTransfers_ToWh REFERENCES inv.Warehouses(Id),
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=InTransit, 2=Completed, 3=Cancelled
        Remarks NVARCHAR(250) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_StockTransfers_Company_Number UNIQUE (CompanyId, TransferNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.StockTransferLines'))
BEGIN
    CREATE TABLE inv.StockTransferLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TransferId INT NOT NULL CONSTRAINT FK_inv_StockTransferLines_Transfer REFERENCES inv.StockTransfers(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_inv_StockTransferLines_Product REFERENCES inv.Products(Id),
        TransferQuantity DECIMAL(18, 4) NOT NULL,
        UnitCost DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_inv_StockTransferLines_Transfer_Line UNIQUE (TransferId, LineNumber)
    );
END;
GO

-- ------------------------------------------------------------------------------
-- 7. SEED UNITS OF MEASURE (inv.UnitsOfMeasure)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM inv.UnitsOfMeasure WHERE UomCode = 'PCS')
BEGIN
    INSERT INTO inv.UnitsOfMeasure (CompanyId, UomCode, UomName, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES 
        (1, 'PCS', 'Pieces', SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'KG', 'Kilograms', SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'BOX', 'Boxes / Cartons', SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'LTR', 'Liters', SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'MTR', 'Meters', SYSUTCDATETIME(), 'SeedMigration', 0);
END;

-- ------------------------------------------------------------------------------
-- 8. SEED PRODUCT CLASSES (inv.ProductClasses)
-- ------------------------------------------------------------------------------
DECLARE @GlInvAcct INT = (SELECT Id FROM gl.Accounts WHERE AccountCode = '1140');
DECLARE @GlCogsAcct INT = (SELECT Id FROM gl.Accounts WHERE AccountCode = '5110');
DECLARE @GlRevAcct INT = (SELECT Id FROM gl.Accounts WHERE AccountCode = '4100');

IF NOT EXISTS (SELECT 1 FROM inv.ProductClasses WHERE ClassCode = 'FG-ELEC')
BEGIN
    INSERT INTO inv.ProductClasses (CompanyId, ClassCode, ClassName, CategoryType, GlInventoryAccountId, GlCogsAccountId, GlRevenueAccountId, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES 
        (1, 'FG-ELEC', 'Finished Goods - Consumer Electronics', 1, @GlInvAcct, @GlCogsAcct, @GlRevAcct, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'RM-PARTS', 'Raw Materials & Electrical Components', 2, @GlInvAcct, @GlCogsAcct, @GlRevAcct, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'CONS-OFF', 'Consumables & Operational Supplies', 4, @GlInvAcct, @GlCogsAcct, @GlRevAcct, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

-- ------------------------------------------------------------------------------
-- 9. SEED WAREHOUSES (inv.Warehouses)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM inv.Warehouses WHERE WarehouseCode = 'WH-CENTRAL')
BEGIN
    INSERT INTO inv.Warehouses (CompanyId, WarehouseCode, WarehouseName, Location, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES 
        (1, 'WH-CENTRAL', 'Central Distribution Hub', 'Plot 12, Tejgaon I/A, Dhaka', 1, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'WH-PLANT', 'Manufacturing Plant Warehouse', 'Tongi Industrial Estate, Gazipur', 1, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'WH-RETAIL', 'Gulshan Flagship Showroom Store', 'Road 11, Banani/Gulshan, Dhaka', 1, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

-- ------------------------------------------------------------------------------
-- 10. SEED PRODUCT CATALOG (inv.Products)
-- ------------------------------------------------------------------------------
DECLARE @ClassFgId INT = (SELECT Id FROM inv.ProductClasses WHERE ClassCode = 'FG-ELEC');
DECLARE @ClassRmId INT = (SELECT Id FROM inv.ProductClasses WHERE ClassCode = 'RM-PARTS');
DECLARE @UomPcsId INT = (SELECT Id FROM inv.UnitsOfMeasure WHERE UomCode = 'PCS');
DECLARE @UomMtrId INT = (SELECT Id FROM inv.UnitsOfMeasure WHERE UomCode = 'MTR');

IF NOT EXISTS (SELECT 1 FROM inv.Products WHERE ProductCode = 'PROD-LED-55')
BEGIN
    INSERT INTO inv.Products (CompanyId, ProductCode, ProductName, ProductClassId, PrimaryUomId, PackSize, StandardCost, CurrentCost, SellingPrice, ReorderLevel, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES 
        (1, 'PROD-LED-55', '55-Inch 4K UHD Smart Television', @ClassFgId, @UomPcsId, '1 Unit/Box', 42000.00, 42000.00, 58000.00, 10, 1, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'PROD-AC-1.5T', '1.5 Ton Inverter Dual Split Air Conditioner', @ClassFgId, @UomPcsId, '1 Unit/Box', 55000.00, 55000.00, 72000.00, 5, 1, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'PROD-PWR-MOD', 'Switch Mode Power Supply 24V 10A Module', @ClassRmId, @UomPcsId, '10 Pcs/Box', 1200.00, 1200.00, 1800.00, 50, 1, SYSUTCDATETIME(), 'SeedMigration', 0),
        (1, 'PROD-CBL-HD', 'High Density Shielded Copper Cable', @ClassRmId, @UomMtrId, '100m Roll', 85.00, 85.00, 130.00, 200, 1, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

PRINT 'Migration 005 completed successfully: Schema & Master Seed data established.';
GO
