-- ==============================================================================
-- Migration: 017_Dynamic_Product_Master_And_Reference_Lookups.sql
-- Description: Implements the approved Product Master & Dynamic Master Data Plan:
--              1. Expands inv.Products with Description, BarcodeGtin, SubcategoryId,
--                 BrandId, MaterialGrade, SecondaryUomId, UomConversionFactor,
--                 MinimumRetailPrice, TaxScheduleCode, DefaultStorageBin, QcPolicy,
--                 IsPurchasable, IsSellable, IsManufactured, LifecycleStatus.
--              2. Expands inv.UnitsOfMeasure with UomCategory, DecimalPrecision, IsActive.
--              3. Expands inv.ProductClasses with IsActive.
--              4. Creates inv.MasterLookups (Configurable Reference Data Engine) and seeds
--                 PRODUCT_SUBCATEGORY, MATERIAL_GRADE, PRODUCT_BRAND, PACKAGING_TYPE,
--                 TAX_SCHEDULE, QC_POLICY, and STORAGE_BIN.
--              5. Creates inv.ProductPriceHistory for commercial cost/price audit trail.
-- Target Server: Microsoft SQL Server 2025 / Database: ERP
-- ==============================================================================

USE [ERP];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ==============================================================================
-- 1. EXPAND inv.Products WITH COMPLETE PRODUCT MASTER ATTRIBUTES
-- ==============================================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'Description')
BEGIN
    ALTER TABLE inv.Products ADD Description NVARCHAR(500) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'BarcodeGtin')
BEGIN
    ALTER TABLE inv.Products ADD BarcodeGtin NVARCHAR(50) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'SubcategoryId')
BEGIN
    ALTER TABLE inv.Products ADD SubcategoryId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'BrandId')
BEGIN
    ALTER TABLE inv.Products ADD BrandId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'MaterialGrade')
BEGIN
    ALTER TABLE inv.Products ADD MaterialGrade NVARCHAR(100) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'SecondaryUomId')
BEGIN
    ALTER TABLE inv.Products ADD SecondaryUomId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'UomConversionFactor')
BEGIN
    ALTER TABLE inv.Products ADD UomConversionFactor DECIMAL(18, 6) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'MinimumRetailPrice')
BEGIN
    ALTER TABLE inv.Products ADD MinimumRetailPrice DECIMAL(18, 4) NOT NULL CONSTRAINT DF_Products_MinimumRetailPrice DEFAULT 0.0000;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'TaxScheduleCode')
BEGIN
    ALTER TABLE inv.Products ADD TaxScheduleCode NVARCHAR(30) NULL CONSTRAINT DF_Products_TaxScheduleCode DEFAULT 'VAT_STD_15';
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'DefaultStorageBin')
BEGIN
    ALTER TABLE inv.Products ADD DefaultStorageBin NVARCHAR(50) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'QcPolicy')
BEGIN
    ALTER TABLE inv.Products ADD QcPolicy NVARCHAR(50) NOT NULL CONSTRAINT DF_Products_QcPolicy DEFAULT 'Visual Inspection';
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsPurchasable')
BEGIN
    ALTER TABLE inv.Products ADD IsPurchasable BIT NOT NULL CONSTRAINT DF_Products_IsPurchasable DEFAULT 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsSellable')
BEGIN
    ALTER TABLE inv.Products ADD IsSellable BIT NOT NULL CONSTRAINT DF_Products_IsSellable DEFAULT 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsManufactured')
BEGIN
    ALTER TABLE inv.Products ADD IsManufactured BIT NOT NULL CONSTRAINT DF_Products_IsManufactured DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'LifecycleStatus')
BEGIN
    ALTER TABLE inv.Products ADD LifecycleStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Products_LifecycleStatus DEFAULT 'Active';
END;
GO

-- Backfill MaterialGrade from PackSize where MaterialGrade is null and PackSize holds grade data
UPDATE inv.Products
SET MaterialGrade = ISNULL(MaterialGrade, PackSize),
    MinimumRetailPrice = CASE WHEN MinimumRetailPrice = 0 AND SellingPrice > 0 THEN ROUND(SellingPrice * 1.10, 2) ELSE MinimumRetailPrice END,
    DefaultStorageBin = ISNULL(DefaultStorageBin, 'A-01-R1'),
    QcPolicy = ISNULL(QcPolicy, 'Mandatory QC')
WHERE MaterialGrade IS NULL;
GO

-- ==============================================================================
-- 2. EXPAND inv.UnitsOfMeasure & inv.ProductClasses
-- ==============================================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'UnitsOfMeasure' AND COLUMN_NAME = 'UomCategory')
BEGIN
    ALTER TABLE inv.UnitsOfMeasure ADD UomCategory NVARCHAR(30) NOT NULL CONSTRAINT DF_UnitsOfMeasure_UomCategory DEFAULT 'Unit';
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'UnitsOfMeasure' AND COLUMN_NAME = 'DecimalPrecision')
BEGIN
    ALTER TABLE inv.UnitsOfMeasure ADD DecimalPrecision INT NOT NULL CONSTRAINT DF_UnitsOfMeasure_DecimalPrecision DEFAULT 2;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'UnitsOfMeasure' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE inv.UnitsOfMeasure ADD IsActive BIT NOT NULL CONSTRAINT DF_UnitsOfMeasure_IsActive DEFAULT 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'ProductClasses' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE inv.ProductClasses ADD IsActive BIT NOT NULL CONSTRAINT DF_ProductClasses_IsActive DEFAULT 1;
END;
GO

-- Update existing UOM categories
UPDATE inv.UnitsOfMeasure SET UomCategory = 'Mass', DecimalPrecision = 3 WHERE UomCode IN ('MT', 'KG', 'TON', 'GM');
UPDATE inv.UnitsOfMeasure SET UomCategory = 'Length', DecimalPrecision = 2 WHERE UomCode IN ('M', 'MTR', 'MM', 'FT', 'RFT', 'LENGTH');
UPDATE inv.UnitsOfMeasure SET UomCategory = 'Volume', DecimalPrecision = 3 WHERE UomCode IN ('LTR', 'CBM', 'CFT', 'DRUM');
UPDATE inv.UnitsOfMeasure SET UomCategory = 'Unit', DecimalPrecision = 0 WHERE UomCode IN ('PCS', 'NOS', 'SET', 'BAG', 'BOX', 'COIL', 'SHEET');
GO

-- ==============================================================================
-- 3. CREATE CONFIGURABLE REFERENCE DATA TABLE (inv.MasterLookups)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.MasterLookups'))
BEGIN
    CREATE TABLE inv.MasterLookups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SysGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        CompanyId INT NOT NULL DEFAULT 0, -- 0 = Shared Group-Wide Standard, >0 = Subsidiary-Specific
        LookupType NVARCHAR(50) NOT NULL, -- PRODUCT_SUBCATEGORY, MATERIAL_GRADE, PRODUCT_BRAND, PACKAGING_TYPE, TAX_SCHEDULE, QC_POLICY, STORAGE_BIN
        LookupCode NVARCHAR(50) NOT NULL,
        LookupName NVARCHAR(150) NOT NULL,
        Description NVARCHAR(250) NULL,
        NumericValue DECIMAL(18, 4) NULL, -- e.g. VAT Rate % for TAX_SCHEDULE
        ParentId INT NULL,                -- e.g. ProductClassId for Subcategory, or WarehouseId for StorageBin
        SortOrder INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_inv_MasterLookups_Type_Company_Code UNIQUE (CompanyId, LookupType, LookupCode)
    );
END;
GO

-- Seed standard international & industry reference lookups (idempotent)
IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'MATERIAL_GRADE')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, SortOrder)
    VALUES
        (0, 'MATERIAL_GRADE', 'ASTM-A572-50', 'ASTM A572 Grade 50', 'High-Strength Low-Alloy Structural Steel (345 MPa Yield)', 1),
        (0, 'MATERIAL_GRADE', 'ASTM-A36',     'ASTM A36 Mild Steel', 'Standard Carbon Structural Steel (250 MPa Yield)', 2),
        (0, 'MATERIAL_GRADE', 'JIS-G3101-SS400', 'JIS G3101 SS400', 'General Structure Rolled Steel', 3),
        (0, 'MATERIAL_GRADE', 'ASTM-A653-G90', 'ASTM A653 G90 Galvanized', 'Hot-Dip Galvanized Structural Steel Sheet (275 GSM)', 4),
        (0, 'MATERIAL_GRADE', 'ISO-898-CL8.8', 'ISO 898-1 Class 8.8', 'High-Tensile Structural Bolt Grade 8.8', 5),
        (0, 'MATERIAL_GRADE', 'AWS-A5.1-E7018', 'AWS A5.1 E7018', 'Low-Hydrogen Welding Electrode Standard', 6),
        (0, 'MATERIAL_GRADE', 'HDPE-FILM',    'HDPE Film Extrusion Grade', 'High-Density Polyethylene Virgin Resin', 7),
        (0, 'MATERIAL_GRADE', 'E1-MDF-18MM',  'E1 Grade Moisture-Resistant Board', 'European E1 Formaldehyde Emission Standard Furniture Board', 8),
        (0, 'MATERIAL_GRADE', 'STD-COMMERCIAL', 'Standard Commercial Spec', 'General Commercial Quality Specification', 9);
END;
GO

IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'PACKAGING_TYPE')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, SortOrder)
    VALUES
        (0, 'PACKAGING_TYPE', 'LOOSE-BULK',  'Loose / Unpacked Bulk', 'Unpacked structural members or bulk material', 1),
        (0, 'PACKAGING_TYPE', 'COIL-STD',    'Steel Master Coil (3-5 MT)', 'Strapped steel mother coil', 2),
        (0, 'PACKAGING_TYPE', 'BUNDLE-1MT',  'Steel Bundle (1 MT)', 'Strapped bundle of plates, purlins, or rebars', 3),
        (0, 'PACKAGING_TYPE', 'BAG-25KG',    '25 KG Woven Sack / Bag', 'Standard 25 KG polymer resin or chemical bag', 4),
        (0, 'PACKAGING_TYPE', 'BAG-50KG',    '50 KG Industrial Sack', '50 KG heavy-duty bag', 5),
        (0, 'PACKAGING_TYPE', 'DRUM-200L',   '200 Liter Steel/HDPE Drum', 'Standard 200L liquid/chemical drum', 6),
        (0, 'PACKAGING_TYPE', 'BOX-CARTON',  'Corrugated Master Carton', 'Boxed hardware, fasteners, or knocked-down furniture', 7),
        (0, 'PACKAGING_TYPE', 'PALLET-STD',  'Wooden / Euro Pallet', 'Palletized unit load', 8);
END;
GO

IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'TAX_SCHEDULE')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, NumericValue, SortOrder)
    VALUES
        (0, 'TAX_SCHEDULE', 'VAT_STD_15',    '15.0% — Standard VAT Schedule', 'NBR Standard Value Added Tax (15%)', 15.0000, 1),
        (0, 'TAX_SCHEDULE', 'VAT_TRADE_7_5', '7.5% — Manufacturing / Trade VAT', 'Truncated VAT Schedule (7.5%)', 7.5000, 2),
        (0, 'TAX_SCHEDULE', 'VAT_RED_5',     '5.0% — Reduced Commercial VAT', 'Reduced Rate VAT Schedule (5%)', 5.0000, 3),
        (0, 'TAX_SCHEDULE', 'VAT_ZERO_0',    '0.0% — Zero-Rated Export', 'Zero-Rated Export / Deemed Export Supply (0%)', 0.0000, 4),
        (0, 'TAX_SCHEDULE', 'VAT_EXEMPT',    '0.0% — VAT Exempt Schedule', 'First Schedule Statutory VAT Exempt Item (0%)', 0.0000, 5);
END;
GO

IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'QC_POLICY')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, SortOrder)
    VALUES
        (0, 'QC_POLICY', 'Mandatory QC',      'Mandatory Lab & Dimensional QC', 'Requires full QA/QC inspection and release before stock is unrestricted', 1),
        (0, 'QC_POLICY', 'Mill Cert Check',   'Mill Test Certificate (MTC) Verification', 'Requires EN 10204 3.1 Mill Test Certificate check at GRN', 2),
        (0, 'QC_POLICY', 'Sampling QC',       'AQL Sampling Inspection', 'ISO 2859-1 statistical sampling inspection on receipt', 3),
        (0, 'QC_POLICY', 'Visual Inspection', 'Standard Visual Inspection', 'Gate and storekeeper visual verification of quantity and damage', 4),
        (0, 'QC_POLICY', 'Exempt',            'No QC Required (Direct to Stock)', 'Fast-track direct putaway without QC hold', 5);
END;
GO

IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'PRODUCT_SUBCATEGORY')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, SortOrder)
    VALUES
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-HR-COIL',   'Hot Rolled (HR) Coils & Plates', 'Structural steel coils and plates', 1),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-GI-COIL',   'Galvanized & Color Coated Coils', 'Cold-formed purlin and cladding coils', 2),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-PEB-MEM',   'PEB Built-Up Columns & Rafters', 'Fabricated primary structural steel members', 3),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-FASTENER',  'Structural Bolts, Nuts & Anchor Rods', 'High-tensile fasteners and fixings', 4),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-CONSUM',    'Welding, Paint & Shop Consumables', 'Welding wire, gas, primer, and abrasives', 5),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-POLY-RES',  'Polymer Resins & Masterbatch', 'Virgin and recycled polymer granules', 6),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-FURN-OFF',  'Executive & Modular Office Furniture', 'Desks, workstations, and storage cabinets', 7),
        (0, 'PRODUCT_SUBCATEGORY', 'SUB-SPARES',    'Mechanical & Electrical Spare Parts', 'Plant maintenance spares and tooling', 8);
END;
GO

IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'PRODUCT_BRAND')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, SortOrder)
    VALUES
        (0, 'PRODUCT_BRAND', 'BRD-PEB-STEEL', 'PEB Steel Alliance (In-House)', 'Manufactured in-house by PEB Steel Alliance Ltd.', 1),
        (0, 'PRODUCT_BRAND', 'BRD-HATIM',     'Hatim Furniture (In-House)', 'Manufactured in-house by Hatim Group', 2),
        (0, 'PRODUCT_BRAND', 'BRD-NIPPON',    'Nippon Steel / JFE Japan', 'Imported Japanese structural steel mill', 3),
        (0, 'PRODUCT_BRAND', 'BRD-POSCO',     'POSCO Korea', 'Imported Korean structural steel mill', 4),
        (0, 'PRODUCT_BRAND', 'BRD-BSRM',      'BSRM / AKS Steel', 'Local structural steel manufacturer', 5),
        (0, 'PRODUCT_BRAND', 'BRD-ESAB',      'ESAB / Lincoln Electric', 'Welding consumables & equipment OEM', 6),
        (0, 'PRODUCT_BRAND', 'BRD-SABIC',     'SABIC / ExxonMobil Polymer', 'Petrochemical resin manufacturer', 7),
        (0, 'PRODUCT_BRAND', 'BRD-GENERIC',   'Generic / Multi-Vendor OEM', 'Standard unbranded or multi-source item', 8);
END;
GO

IF NOT EXISTS (SELECT 1 FROM inv.MasterLookups WHERE LookupType = 'STORAGE_BIN')
BEGIN
    INSERT INTO inv.MasterLookups (CompanyId, LookupType, LookupCode, LookupName, Description, SortOrder)
    VALUES
        (0, 'STORAGE_BIN', 'A-01-R1', 'Bay A-01 / Rack R1 (Heavy Coil Yard)', 'Primary structural coil & plate gantry bay', 1),
        (0, 'STORAGE_BIN', 'A-02-R2', 'Bay A-02 / Rack R2 (Section & Purlin Bay)', 'Rolled section and C/Z purlin rack', 2),
        (0, 'STORAGE_BIN', 'B-01-FG', 'Zone B-01 (Finished Goods Dispatch Yard)', 'Fabricated steel & finished goods staging area', 3),
        (0, 'STORAGE_BIN', 'C-01-SILO', 'Silo C-01 / Resin Rack', 'Dry climate-controlled resin and chemical store', 4),
        (0, 'STORAGE_BIN', 'D-01-BIN', 'Store D-01 / Fastener & Spares Bin', 'Small parts, bolts, electrodes, and MRO bin rack', 5),
        (0, 'STORAGE_BIN', 'QC-HOLD-01', 'Quarantine / Inbound QC Hold Bay', 'Dedicated quarantine zone awaiting QA/QC release', 6);
END;
GO

-- ==============================================================================
-- 4. CREATE COMMERCIAL PRICE & COST AUDIT HISTORY TABLE (inv.ProductPriceHistory)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.ProductPriceHistory'))
BEGIN
    CREATE TABLE inv.ProductPriceHistory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_inv_ProductPriceHistory_Product REFERENCES inv.Products(Id),
        OldStandardCost DECIMAL(18, 4) NOT NULL,
        NewStandardCost DECIMAL(18, 4) NOT NULL,
        OldSellingPrice DECIMAL(18, 4) NOT NULL,
        NewSellingPrice DECIMAL(18, 4) NOT NULL,
        OldMinRetailPrice DECIMAL(18, 4) NOT NULL,
        NewMinRetailPrice DECIMAL(18, 4) NOT NULL,
        ChangeReason NVARCHAR(250) NULL,
        EffectiveFromUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ChangedBy NVARCHAR(100) NOT NULL DEFAULT 'System'
    );
END;
GO
