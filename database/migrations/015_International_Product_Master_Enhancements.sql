-- ==============================================================================
-- Migration: 015_International_Product_Master_Enhancements.sql
-- Description: Upgrades inv.Products with International Standard attributes:
--              1. HS / Tariff Code (WCO 6-8 digits)
--              2. Weights: Net Weight, Gross Weight, Weight UOM (kg, MT, lbs)
--              3. Dimensions: Length, Width, Height, Dimension UOM (mm, cm, m), CBM Volume
--              4. Traceability: Tracking Policy (Standard, Batch, Serialized)
--              5. Supply Chain Planning: Safety Stock, Lead Time Days
--              6. Tax / Valuation: Costing Method, Tax Rate / VAT Rate
-- Target Server: Microsoft SQL Server 2025 / Database: ERP
-- ==============================================================================

USE ERP;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. ADD INTERNATIONAL STANDARD COLUMNS TO inv.Products
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'HsCode')
BEGIN
    ALTER TABLE inv.Products ADD HsCode NVARCHAR(20) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'NetWeight')
BEGIN
    ALTER TABLE inv.Products ADD NetWeight DECIMAL(18, 4) NULL CONSTRAINT DF_Products_NetWeight DEFAULT 0.0000;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'GrossWeight')
BEGIN
    ALTER TABLE inv.Products ADD GrossWeight DECIMAL(18, 4) NULL CONSTRAINT DF_Products_GrossWeight DEFAULT 0.0000;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'WeightUom')
BEGIN
    ALTER TABLE inv.Products ADD WeightUom NVARCHAR(20) NULL CONSTRAINT DF_Products_WeightUom DEFAULT 'KG';
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'Length')
BEGIN
    ALTER TABLE inv.Products ADD Length DECIMAL(18, 4) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'Width')
BEGIN
    ALTER TABLE inv.Products ADD Width DECIMAL(18, 4) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'Height')
BEGIN
    ALTER TABLE inv.Products ADD Height DECIMAL(18, 4) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'DimensionUom')
BEGIN
    ALTER TABLE inv.Products ADD DimensionUom NVARCHAR(20) NULL CONSTRAINT DF_Products_DimensionUom DEFAULT 'MM';
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'VolumeCbm')
BEGIN
    ALTER TABLE inv.Products ADD VolumeCbm DECIMAL(18, 4) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'TrackingPolicy')
BEGIN
    -- 1 = Standard / None, 2 = Batch / Heat Number Tracked, 3 = Serial Number Tracked
    ALTER TABLE inv.Products ADD TrackingPolicy INT NOT NULL CONSTRAINT DF_Products_TrackingPolicy DEFAULT 1;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'SafetyStock')
BEGIN
    ALTER TABLE inv.Products ADD SafetyStock DECIMAL(18, 4) NOT NULL CONSTRAINT DF_Products_SafetyStock DEFAULT 0.0000;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'LeadTimeDays')
BEGIN
    ALTER TABLE inv.Products ADD LeadTimeDays INT NOT NULL CONSTRAINT DF_Products_LeadTimeDays DEFAULT 7;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'CostingMethod')
BEGIN
    -- MovingAverage, StandardCost, FIFO
    ALTER TABLE inv.Products ADD CostingMethod NVARCHAR(30) NOT NULL CONSTRAINT DF_Products_CostingMethod DEFAULT 'MovingAverage';
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'TaxRatePercent')
BEGIN
    ALTER TABLE inv.Products ADD TaxRatePercent DECIMAL(5, 2) NOT NULL CONSTRAINT DF_Products_TaxRatePercent DEFAULT 15.00;
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'DefaultWarehouseId')
BEGIN
    ALTER TABLE inv.Products ADD DefaultWarehouseId INT NULL;
END;
GO

-- 2. SEED DEFAULT SAMPLE SPECS FOR EXISTING PRODUCTS IF MISSING
UPDATE inv.Products 
SET HsCode = '7214.20.00',
    NetWeight = 250.0000,
    GrossWeight = 252.5000,
    WeightUom = 'KG',
    Length = 12000.0000,
    Width = 200.0000,
    Height = 150.0000,
    DimensionUom = 'MM',
    VolumeCbm = 0.3600,
    TrackingPolicy = 2, -- Batch / Heat Number Tracked
    SafetyStock = 20.0000,
    LeadTimeDays = 14,
    CostingMethod = 'MovingAverage',
    TaxRatePercent = 15.00
WHERE HsCode IS NULL;
GO
