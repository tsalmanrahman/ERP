-- ==============================================================================
-- Migration: 018_Multi_Company_Foundational_Provisioning_And_Isolation.sql
-- Project: ERP Modernization (.NET 10 Modular Monolith + MSSQL 2025)
-- Description:
--   1. Reconciles GoodsReceiptReturns / GoodsReceiptReturnLines & GRN columns in inv schema.
--   2. Adds missing foreign keys and composite multi-tenant indexes on CompanyId.
--   3. Ensures Admin user (Id=1) has access to all 4 Hatim Group companies (HSF=1, HSS=2, HIL=3, HPL=4).
--   4. Provisions complete foundational master data for Companies 2 (HSS), 3 (HIL), and 4 (HPL)
--      (and any missing items for Company 1):
--      - Business Units (sa.BusinessUnits)
--      - Fiscal Calendar FY2026 & 12 Periods (sa.FiscalYears, sa.FiscalPeriods)
--      - Chart of Accounts (gl.AccountGroups, gl.Accounts)
--      - Banks, Branches, Bank Accounts & Cash Books (bc.Banks, bc.BankBranches, bc.BankAccounts, bc.CashBooks)
--      - Units of Measure (inv.UnitsOfMeasure)
--      - Product Classes with GL links (inv.ProductClasses)
--      - Depots & Warehouses (inv.Depots, inv.Warehouses)
--      - Subsidiary-distinct Products & Stock (inv.Products, inv.ProductStock)
--      - Vendor Groups & Subsidiary Vendors (ap.VendorGroups, ap.Vendors)
--      - Customer Groups, Subsidiary Customers & Sales Centers (ar.CustomerGroups, ar.Customers, sales.SalesCenters)
--      - HR Departments, Designations & Subsidiary Employees (hr.Departments, hr.Designations, hr.Employees)
--      - Fixed Asset Groups & Subsidiary Assets (fa.AssetGroups, fa.Assets)
-- ==============================================================================

USE [ERP];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT 'Starting Migration 018_Multi_Company_Foundational_Provisioning_And_Isolation...';

-- ==============================================================================
-- 1. SCHEMA RECONCILIATION FOR inv.GoodsReceiptNotes & inv.GoodsReceiptReturns
-- ==============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'GrnType')
    ALTER TABLE inv.GoodsReceiptNotes ADD GrnType INT NOT NULL CONSTRAINT DF_inv_GRN_GrnType DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'PurchaseOrderId')
    ALTER TABLE inv.GoodsReceiptNotes ADD PurchaseOrderId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'ProductionOrderId')
    ALTER TABLE inv.GoodsReceiptNotes ADD ProductionOrderId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'ProductionReference')
    ALTER TABLE inv.GoodsReceiptNotes ADD ProductionReference NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'CarrierName')
    ALTER TABLE inv.GoodsReceiptNotes ADD CarrierName NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'VehicleNumber')
    ALTER TABLE inv.GoodsReceiptNotes ADD VehicleNumber NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'GateEntryNumber')
    ALTER TABLE inv.GoodsReceiptNotes ADD GateEntryNumber NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNotes') AND name = 'QcStatus')
    ALTER TABLE inv.GoodsReceiptNotes ADD QcStatus NVARCHAR(50) NULL CONSTRAINT DF_inv_GRN_QcStatus DEFAULT 'Passed';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNoteLines') AND name = 'PurchaseOrderLineId')
    ALTER TABLE inv.GoodsReceiptNoteLines ADD PurchaseOrderLineId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNoteLines') AND name = 'AcceptedQuantity')
    ALTER TABLE inv.GoodsReceiptNoteLines ADD AcceptedQuantity DECIMAL(18, 4) NOT NULL CONSTRAINT DF_inv_GRNLines_AcceptedQty DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNoteLines') AND name = 'RejectedQuantity')
    ALTER TABLE inv.GoodsReceiptNoteLines ADD RejectedQuantity DECIMAL(18, 4) NOT NULL CONSTRAINT DF_inv_GRNLines_RejectedQty DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNoteLines') AND name = 'ReturnedQuantity')
    ALTER TABLE inv.GoodsReceiptNoteLines ADD ReturnedQuantity DECIMAL(18, 4) NOT NULL CONSTRAINT DF_inv_GRNLines_ReturnedQty DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('inv.GoodsReceiptNoteLines') AND name = 'StorageBinLocation')
    ALTER TABLE inv.GoodsReceiptNoteLines ADD StorageBinLocation NVARCHAR(50) NULL;
GO

IF OBJECT_ID('inv.GoodsReceiptReturns', 'U') IS NULL
BEGIN
    CREATE TABLE inv.GoodsReceiptReturns (
        Id INT IDENTITY(1,1) NOT NULL,
        SysGuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturns_SysGuid DEFAULT NEWSEQUENTIALID(),
        CompanyId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptReturns_Company REFERENCES sa.Companies(Id),
        ReturnNumber NVARCHAR(50) NOT NULL,
        ReturnDate DATETIME2 NOT NULL,
        GoodsReceiptNoteId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptReturns_GRN REFERENCES inv.GoodsReceiptNotes(Id),
        ReturnType INT NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturns_ReturnType DEFAULT 1,
        WarehouseId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptReturns_Warehouse REFERENCES inv.Warehouses(Id),
        VendorId INT NULL,
        PurchaseOrderId INT NULL,
        ProductionOrderId INT NULL,
        Reason NVARCHAR(250) NOT NULL,
        TotalAmount DECIMAL(18, 4) NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturns_TotalAmount DEFAULT 0,
        Status INT NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturns_Status DEFAULT 0,
        JournalVoucherId INT NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturns_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(100) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturns_IsDeleted DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(100) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_inv_GoodsReceiptReturns PRIMARY KEY CLUSTERED (Id ASC)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_inv_GoodsReceiptReturns_Company_ReturnNumber
        ON inv.GoodsReceiptReturns (CompanyId, ReturnNumber)
        WHERE IsDeleted = 0;
END;
GO

IF OBJECT_ID('inv.GoodsReceiptReturnLines', 'U') IS NULL
BEGIN
    CREATE TABLE inv.GoodsReceiptReturnLines (
        Id INT IDENTITY(1,1) NOT NULL,
        SysGuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturnLines_SysGuid DEFAULT NEWSEQUENTIALID(),
        ReturnId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptReturnLines_Return REFERENCES inv.GoodsReceiptReturns(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        GoodsReceiptNoteLineId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptReturnLines_GrnLine REFERENCES inv.GoodsReceiptNoteLines(Id),
        ProductId INT NOT NULL CONSTRAINT FK_inv_GoodsReceiptReturnLines_Product REFERENCES inv.Products(Id),
        ReturnedQuantity DECIMAL(18, 4) NOT NULL,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        ReturnReason NVARCHAR(250) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturnLines_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(100) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_inv_GoodsReceiptReturnLines_IsDeleted DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(100) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_inv_GoodsReceiptReturnLines PRIMARY KEY CLUSTERED (Id ASC)
    );
END;
GO

-- Add FK on inv.ProductPriceHistory.CompanyId if missing
IF OBJECT_ID('inv.ProductPriceHistory', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_inv_ProductPriceHistory_Company')
BEGIN
    ALTER TABLE inv.ProductPriceHistory
    ADD CONSTRAINT FK_inv_ProductPriceHistory_Company FOREIGN KEY (CompanyId) REFERENCES sa.Companies(Id);
END;
GO

-- Add composite index on inv.ProductPriceHistory(CompanyId, ProductId, EffectiveFromUtc DESC)
IF OBJECT_ID('inv.ProductPriceHistory', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('inv.ProductPriceHistory') AND name = 'IX_inv_ProductPriceHistory_Company_Product')
BEGIN
    CREATE NONCLUSTERED INDEX IX_inv_ProductPriceHistory_Company_Product
        ON inv.ProductPriceHistory (CompanyId, ProductId, EffectiveFromUtc DESC);
END;
GO

-- ==============================================================================
-- 2. ENSURE ALL 4 HATIM GROUP SUBSIDIARIES & ADMIN ACCESS EXIST
-- ==============================================================================

IF EXISTS (SELECT 1 FROM sa.Companies WHERE Id = 1)
    UPDATE sa.Companies SET CompanyCode = 'HSF', CompanyName = 'HATIM STEEL STRUCTURE LTD (HSF)', BaseCurrency = 'BDT', IsActive = 1 WHERE Id = 1;

IF EXISTS (SELECT 1 FROM sa.Companies WHERE Id = 2)
    UPDATE sa.Companies SET CompanyCode = 'HSS', CompanyName = 'HATIM STEEL STRUCTURE LTD', BaseCurrency = 'BDT', IsActive = 1 WHERE Id = 2;

SET IDENTITY_INSERT sa.Companies ON;

IF NOT EXISTS (SELECT 1 FROM sa.Companies WHERE Id = 3)
BEGIN
    INSERT INTO sa.Companies (Id, CompanyCode, CompanyName, AddressLine1, City, CountryCode, BaseCurrency, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (3, 'HIL', 'Hatim Industries Ltd', 'Corporate Headquarters, Tejgaon Industrial Area', 'Dhaka', 'BGD', 'BDT', 1, SYSUTCDATETIME(), 'SYSTEM', 0);
END;

IF NOT EXISTS (SELECT 1 FROM sa.Companies WHERE Id = 4)
BEGIN
    INSERT INTO sa.Companies (Id, CompanyCode, CompanyName, AddressLine1, City, CountryCode, BaseCurrency, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (4, 'HPL', 'Hatim Polymer Ltd', 'Hatim Polymer Plant, BSCIC Industrial Estate', 'Narayanganj', 'BGD', 'BDT', 1, SYSUTCDATETIME(), 'SYSTEM', 0);
END;

SET IDENTITY_INSERT sa.Companies OFF;
GO

-- Grant all active users access to all 4 companies if not already granted
INSERT INTO sa.UserCompanyAccess (UserId, CompanyId)
SELECT u.Id, c.Id
FROM sa.Users u
CROSS JOIN sa.Companies c
WHERE c.Id IN (1, 2, 3, 4)
  AND NOT EXISTS (
      SELECT 1 FROM sa.UserCompanyAccess uca
      WHERE uca.UserId = u.Id AND uca.CompanyId = c.Id
  );
GO

-- ==============================================================================
-- 3. PROVISION FOUNDATIONAL MASTER DATA FOR ALL 4 COMPANIES (1=HSF, 2=HSS, 3=HIL, 4=HPL)
-- ==============================================================================

DECLARE @CompId INT = 1;

WHILE @CompId <= 4
BEGIN
    IF EXISTS (SELECT 1 FROM sa.Companies WHERE Id = @CompId)
    BEGIN
        DECLARE @CompPrefix NVARCHAR(10) = CASE @CompId
            WHEN 1 THEN 'HSF'
            WHEN 2 THEN 'HSS'
            WHEN 3 THEN 'HIL'
            WHEN 4 THEN 'HPL'
        END;

        -- 3.1 Business Units
        IF NOT EXISTS (SELECT 1 FROM sa.BusinessUnits WHERE CompanyId = @CompId AND BuCode = 'BU-HQ')
        BEGIN
            INSERT INTO sa.BusinessUnits (CompanyId, BuCode, BuName, IsActive, CreatedBy)
            VALUES
                (@CompId, 'BU-HQ', @CompPrefix + N' Corporate Headquarters', 1, 'System'),
                (@CompId, 'BU-PLANT-01', @CompPrefix + N' Manufacturing & Operations Division', 1, 'System');
        END;

        -- 3.2 Fiscal Year FY2026 & 12 Fiscal Periods
        IF NOT EXISTS (SELECT 1 FROM sa.FiscalYears WHERE CompanyId = @CompId AND YearCode = 'FY2026')
        BEGIN
            INSERT INTO sa.FiscalYears (CompanyId, YearCode, StartDate, EndDate, IsClosed, CreatedBy)
            VALUES (@CompId, 'FY2026', '2026-01-01', '2026-12-31', 0, 'System');

            DECLARE @FyId INT = SCOPE_IDENTITY();

            INSERT INTO sa.FiscalPeriods (FiscalYearId, PeriodNumber, PeriodName, StartDate, EndDate, IsClosed, CreatedBy)
            VALUES
                (@FyId, 1,  'Period 01 (Jan 2026)', '2026-01-01', '2026-01-31', 0, 'System'),
                (@FyId, 2,  'Period 02 (Feb 2026)', '2026-02-01', '2026-02-28', 0, 'System'),
                (@FyId, 3,  'Period 03 (Mar 2026)', '2026-03-01', '2026-03-31', 0, 'System'),
                (@FyId, 4,  'Period 04 (Apr 2026)', '2026-04-01', '2026-04-30', 0, 'System'),
                (@FyId, 5,  'Period 05 (May 2026)', '2026-05-01', '2026-05-31', 0, 'System'),
                (@FyId, 6,  'Period 06 (Jun 2026)', '2026-06-01', '2026-06-30', 0, 'System'),
                (@FyId, 7,  'Period 07 (Jul 2026)', '2026-07-01', '2026-07-31', 0, 'System'),
                (@FyId, 8,  'Period 08 (Aug 2026)', '2026-08-01', '2026-08-31', 0, 'System'),
                (@FyId, 9,  'Period 09 (Sep 2026)', '2026-09-01', '2026-09-30', 0, 'System'),
                (@FyId, 10, 'Period 10 (Oct 2026)', '2026-10-01', '2026-10-31', 0, 'System'),
                (@FyId, 11, 'Period 11 (Nov 2026)', '2026-11-01', '2026-11-30', 0, 'System'),
                (@FyId, 12, 'Period 12 (Dec 2026)', '2026-12-01', '2026-12-31', 0, 'System');
        END;

        -- 3.3 GL Account Groups
        IF NOT EXISTS (SELECT 1 FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '1000')
        BEGIN
            INSERT INTO gl.AccountGroups (CompanyId, GroupCode, GroupName, AccountCategory, CreatedBy)
            VALUES
                (@CompId, '1000', N'Current Assets', 1, 'System'),
                (@CompId, '1200', N'Fixed Assets', 1, 'System'),
                (@CompId, '2000', N'Current Liabilities', 2, 'System'),
                (@CompId, '2200', N'Long Term Liabilities', 2, 'System'),
                (@CompId, '3000', N'Shareholders Equity', 3, 'System'),
                (@CompId, '4000', N'Sales Revenue', 4, 'System'),
                (@CompId, '5100', N'Cost of Goods Sold', 5, 'System'),
                (@CompId, '5200', N'Operating Expenses', 5, 'System');
        END;

        DECLARE @GrpCurrentAssets INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '1000');
        DECLARE @GrpFixedAssets   INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '1200');
        DECLARE @GrpCurrentLiab   INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '2000');
        DECLARE @GrpEquity        INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '3000');
        DECLARE @GrpRevenue       INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '4000');
        DECLARE @GrpCogs          INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '5100');
        DECLARE @GrpOpex          INT = (SELECT TOP 1 Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '5200');

        -- 3.4 Chart of Accounts (gl.Accounts)
        IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1111')
        BEGIN
            INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsReconcilable, IsActive, CreatedBy)
            VALUES
                (@CompId, '1111', N'Cash in Hand (Main Cash)', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1112', N'Petty Cash', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1120', N'Prime Bank Ltd. - Operations Account', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1121', N'City Bank Ltd. - Corporate Account', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1130', N'Trade Accounts Receivable Control', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1140', N'Finished Goods Inventory Control', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1145', N'Raw Materials Inventory Control', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1150', N'Advance & Security Deposits', @GrpCurrentAssets, 2, 1, 1, 'System'),
                (@CompId, '1210', N'Plant & Factory Machinery', @GrpFixedAssets, 2, 1, 1, 'System'),
                (@CompId, '1220', N'Office Furniture & Fixtures', @GrpFixedAssets, 2, 1, 1, 'System'),
                (@CompId, '1290', N'Accumulated Depreciation - Fixed Assets', @GrpFixedAssets, 2, 1, 1, 'System'),
                (@CompId, '2110', N'Trade Accounts Payable Control', @GrpCurrentLiab, 2, 1, 1, 'System'),
                (@CompId, '2120', N'Accrued Salaries & Expenses Payable', @GrpCurrentLiab, 2, 1, 1, 'System'),
                (@CompId, '2130', N'VAT & Tax Withholding Payable', @GrpCurrentLiab, 2, 1, 1, 'System'),
                (@CompId, '2140', N'Accrued Inventory Purchases / GRN Clearing', @GrpCurrentLiab, 2, 1, 1, 'System'),
                (@CompId, '2150', N'Customer Booking Advances & Deposits', @GrpCurrentLiab, 2, 1, 1, 'System'),
                (@CompId, '3100', N'Paid-Up Share Capital', @GrpEquity, 2, 1, 1, 'System'),
                (@CompId, '3200', N'Retained Earnings', @GrpEquity, 2, 1, 1, 'System'),
                (@CompId, '4100', N'General Sales Revenue', @GrpRevenue, 2, 1, 1, 'System'),
                (@CompId, '4110', N'Real Estate Property Sales Revenue', @GrpRevenue, 2, 1, 1, 'System'),
                (@CompId, '4200', N'Other Operating Income', @GrpRevenue, 2, 1, 1, 'System'),
                (@CompId, '5110', N'Cost of Goods Sold - Manufacturing', @GrpCogs, 2, 1, 1, 'System'),
                (@CompId, '5120', N'Inventory Adjustment Variance (Gain/Loss)', @GrpCogs, 2, 0, 1, 'System'),
                (@CompId, '5210', N'Salaries, Wages & Benefits', @GrpOpex, 2, 1, 1, 'System'),
                (@CompId, '5220', N'Office & Factory Rent', @GrpOpex, 2, 1, 1, 'System'),
                (@CompId, '5230', N'Electricity & Utility Charges', @GrpOpex, 2, 1, 1, 'System'),
                (@CompId, '5240', N'Travelling, Fuel & Conveyance', @GrpOpex, 2, 1, 1, 'System'),
                (@CompId, '5250', N'Depreciation Expense', @GrpOpex, 2, 1, 1, 'System'),
                (@CompId, '5260', N'Bank Charges & Financial Fees', @GrpOpex, 2, 1, 1, 'System');
        END;

        -- 3.5 Banks, Branches, Bank Accounts & Cash Books
        IF NOT EXISTS (SELECT 1 FROM bc.Banks WHERE CompanyId = @CompId AND BankCode = 'PRIME')
        BEGIN
            INSERT INTO bc.Banks (CompanyId, BankCode, BankName, CreatedBy)
            VALUES
                (@CompId, 'PRIME', N'Prime Bank PLC', 'System'),
                (@CompId, 'CITY',  N'City Bank PLC', 'System');

            DECLARE @BankPrime INT = (SELECT TOP 1 Id FROM bc.Banks WHERE CompanyId = @CompId AND BankCode = 'PRIME');
            DECLARE @BankCity  INT = (SELECT TOP 1 Id FROM bc.Banks WHERE CompanyId = @CompId AND BankCode = 'CITY');

            INSERT INTO bc.BankBranches (BankId, BranchCode, BranchName, RoutingNumber, CreatedBy)
            VALUES
                (@BankPrime, 'GULSHAN',   N'Gulshan Corporate Branch', '020261234', 'System'),
                (@BankCity,  'MOTIJHEEL', N'Motijheel Principal Branch', '020265678', 'System');

            DECLARE @BranchPrime INT = (SELECT TOP 1 Id FROM bc.BankBranches WHERE BankId = @BankPrime AND BranchCode = 'GULSHAN');
            DECLARE @BranchCity  INT = (SELECT TOP 1 Id FROM bc.BankBranches WHERE BankId = @BankCity  AND BranchCode = 'MOTIJHEEL');

            DECLARE @AcctPrime INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1120');
            DECLARE @AcctCity  INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1121');

            INSERT INTO bc.BankAccounts (CompanyId, BranchId, AccountNumber, AccountTitle, CurrencyCode, GlAccountId, CurrentBalance, IsActive, CreatedBy)
            VALUES
                (@CompId, @BranchPrime, '11020300' + CAST(@CompId AS NVARCHAR(5)), @CompPrefix + N' Prime Current Account', 'BDT', @AcctPrime, 0, 1, 'System'),
                (@CompId, @BranchCity,  '22030400' + CAST(@CompId AS NVARCHAR(5)), @CompPrefix + N' City Operations Account', 'BDT', @AcctCity, 0, 1, 'System');
        END;

        IF NOT EXISTS (SELECT 1 FROM bc.CashBooks WHERE CompanyId = @CompId AND CashBookCode = 'CB-MAIN')
        BEGIN
            DECLARE @AcctCashMain  INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1111');
            DECLARE @AcctPettyCash INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1112');

            INSERT INTO bc.CashBooks (CompanyId, CashBookCode, CashBookName, GlAccountId, CurrentBalance, IsActive, CreatedBy)
            VALUES
                (@CompId, 'CB-MAIN',  @CompPrefix + N' Corporate Main Cash Book', @AcctCashMain, 0, 1, 'System'),
                (@CompId, 'CB-PETTY', @CompPrefix + N' Factory Petty Cash Imprest Book', @AcctPettyCash, 0, 1, 'System');
        END;

        -- 3.6 Units of Measure (inv.UnitsOfMeasure)
        INSERT INTO inv.UnitsOfMeasure (CompanyId, UomCode, UomName, UomCategory, DecimalPrecision, IsActive, CreatedBy)
        SELECT @CompId, v.UomCode, v.UomName, v.UomCategory, v.DecimalPrecision, 1, 'System'
        FROM (VALUES
            ('PCS', 'Pieces',           'Unit',   0),
            ('KG',  'Kilograms',        'Mass',   3),
            ('MT',  'Metric Tons',      'Mass',   3),
            ('BOX', 'Boxes / Cartons',  'Unit',   0),
            ('BAG', 'Bags / Sacks',     'Unit',   0),
            ('SET', 'Complete Sets',    'Unit',   0),
            ('LTR', 'Liters',           'Volume', 3),
            ('MTR', 'Meters',           'Length', 2)
        ) AS v(UomCode, UomName, UomCategory, DecimalPrecision)
        WHERE NOT EXISTS (
            SELECT 1 FROM inv.UnitsOfMeasure u
            WHERE u.CompanyId = @CompId AND u.UomCode = v.UomCode
        );

        -- 3.7 Product Classes (inv.ProductClasses)
        DECLARE @GlInvFg  INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1140');
        DECLARE @GlInvRm  INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1145');
        DECLARE @GlCogs   INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '5110');
        DECLARE @GlRev    INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '4100');

        IF NOT EXISTS (SELECT 1 FROM inv.ProductClasses WHERE CompanyId = @CompId)
        BEGIN
            IF @CompId = 2 -- HSS (Hatim Steel Structure Ltd)
            BEGIN
                INSERT INTO inv.ProductClasses (CompanyId, ClassCode, ClassName, CategoryType, GlInventoryAccountId, GlCogsAccountId, GlRevenueAccountId, IsActive, CreatedBy)
                VALUES
                    (@CompId, 'FG-STEEL', 'Finished Structural Steel Members', 1, @GlInvFg, @GlCogs, @GlRev, 1, 'System'),
                    (@CompId, 'RM-COIL',  'Structural HR/GI Steel Coils & Plates', 2, ISNULL(@GlInvRm, @GlInvFg), @GlCogs, @GlRev, 1, 'System'),
                    (@CompId, 'CONS-WELD','Welding & Fabrication Consumables', 4, ISNULL(@GlInvRm, @GlInvFg), @GlCogs, @GlRev, 1, 'System');
            END
            ELSE IF @CompId = 3 -- HIL (Hatim Industries Ltd)
            BEGIN
                INSERT INTO inv.ProductClasses (CompanyId, ClassCode, ClassName, CategoryType, GlInventoryAccountId, GlCogsAccountId, GlRevenueAccountId, IsActive, CreatedBy)
                VALUES
                    (@CompId, 'FG-FURN',  'Finished Modular & Executive Furniture', 1, @GlInvFg, @GlCogs, @GlRev, 1, 'System'),
                    (@CompId, 'RM-WOOD',  'Engineered Wood Boards, Timber & Hardware', 2, ISNULL(@GlInvRm, @GlInvFg), @GlCogs, @GlRev, 1, 'System'),
                    (@CompId, 'CONS-FIN', 'Lacquer, Adhesives & Upholstery Consumables', 4, ISNULL(@GlInvRm, @GlInvFg), @GlCogs, @GlRev, 1, 'System');
            END
            ELSE IF @CompId = 4 -- HPL (Hatim Polymer Ltd)
            BEGIN
                INSERT INTO inv.ProductClasses (CompanyId, ClassCode, ClassName, CategoryType, GlInventoryAccountId, GlCogsAccountId, GlRevenueAccountId, IsActive, CreatedBy)
                VALUES
                    (@CompId, 'FG-POLY',  'Finished uPVC Pipes, Fittings & Molded Goods', 1, @GlInvFg, @GlCogs, @GlRev, 1, 'System'),
                    (@CompId, 'RM-RESIN', 'Virgin Polymer Resins & Masterbatch Granules', 2, ISNULL(@GlInvRm, @GlInvFg), @GlCogs, @GlRev, 1, 'System'),
                    (@CompId, 'CONS-CHEM','Stabilizers, Pigments & Extrusion Additives', 4, ISNULL(@GlInvRm, @GlInvFg), @GlCogs, @GlRev, 1, 'System');
            END;
        END;

        -- 3.8 Depots & Warehouses (inv.Depots, inv.Warehouses)
        IF NOT EXISTS (SELECT 1 FROM inv.Depots WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO inv.Depots (CompanyId, DepotCode, DepotName, DepotType, Address, City, ContactPerson, ContactPhone, IsActive, CreatedBy)
            VALUES
                (@CompId, 'DEP-' + @CompPrefix + '-01', @CompPrefix + N' Primary Manufacturing Plant', 1, 'Industrial Zone, Sector 4', 'Gazipur', 'Plant Manager (' + @CompPrefix + ')', '+880 1711-00000' + CAST(@CompId AS NVARCHAR(2)), 1, 'System'),
                (@CompId, 'DEP-' + @CompPrefix + '-02', @CompPrefix + N' Central Distribution Hub', 3, 'Tejgaon Industrial Area', 'Dhaka', 'Logistics Head (' + @CompPrefix + ')', '+880 1819-00000' + CAST(@CompId AS NVARCHAR(2)), 1, 'System');
        END;

        DECLARE @PrimaryDepotId INT = (SELECT TOP 1 Id FROM inv.Depots WHERE CompanyId = @CompId ORDER BY Id);
        DECLARE @DistDepotId    INT = (SELECT TOP 1 Id FROM inv.Depots WHERE CompanyId = @CompId ORDER BY Id DESC);

        IF NOT EXISTS (SELECT 1 FROM inv.Warehouses WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO inv.Warehouses (CompanyId, DepotId, WarehouseCode, WarehouseName, WarehouseType, Location, IsActive, CreatedBy)
            VALUES
                (@CompId, @PrimaryDepotId, 'WH-' + @CompPrefix + '-RM',   @CompPrefix + N' Raw Material Store', 1, 'Plant Gate A, Gazipur', 1, 'System'),
                (@CompId, @PrimaryDepotId, 'WH-' + @CompPrefix + '-FG',   @CompPrefix + N' Finished Goods Warehouse', 2, 'Plant Dispatch Bay, Gazipur', 1, 'System'),
                (@CompId, @DistDepotId,    'WH-' + @CompPrefix + '-DIST', @CompPrefix + N' Central Distribution Center', 2, 'Tejgaon C/A, Dhaka', 1, 'System');
        END;

        -- 3.9 Subsidiary-Distinct Products & Opening Stock (inv.Products, inv.ProductStock)
        DECLARE @FgClassId INT = (SELECT TOP 1 Id FROM inv.ProductClasses WHERE CompanyId = @CompId AND CategoryType = 1 ORDER BY Id);
        DECLARE @RmClassId INT = (SELECT TOP 1 Id FROM inv.ProductClasses WHERE CompanyId = @CompId AND CategoryType = 2 ORDER BY Id);
        DECLARE @UomPcs    INT = (SELECT TOP 1 Id FROM inv.UnitsOfMeasure WHERE CompanyId = @CompId AND UomCode = 'PCS');
        DECLARE @UomKg     INT = (SELECT TOP 1 Id FROM inv.UnitsOfMeasure WHERE CompanyId = @CompId AND UomCode = 'KG');
        DECLARE @UomMt     INT = (SELECT TOP 1 Id FROM inv.UnitsOfMeasure WHERE CompanyId = @CompId AND UomCode = 'MT');
        DECLARE @DefFgWh   INT = (SELECT TOP 1 Id FROM inv.Warehouses WHERE CompanyId = @CompId AND WarehouseType = 2 ORDER BY Id);
        DECLARE @DefRmWh   INT = (SELECT TOP 1 Id FROM inv.Warehouses WHERE CompanyId = @CompId AND WarehouseType = 1 ORDER BY Id);

        IF NOT EXISTS (SELECT 1 FROM inv.Products WHERE CompanyId = @CompId)
        BEGIN
            IF @CompId = 2 -- HSS (Hatim Steel Structure Ltd)
            BEGIN
                INSERT INTO inv.Products (
                    CompanyId, ProductCode, ProductName, Description, ProductClassId, PrimaryUomId,
                    PackSize, MaterialGrade, StandardCost, CurrentCost, SellingPrice, MinimumRetailPrice,
                    ReorderLevel, SafetyStock, LeadTimeDays, DefaultWarehouseId, DefaultStorageBin,
                    HsCode, NetWeight, GrossWeight, WeightUom, TrackingPolicy, CostingMethod,
                    TaxScheduleCode, TaxRatePercent, QcPolicy, IsPurchasable, IsSellable, IsManufactured,
                    LifecycleStatus, IsActive, CreatedBy
                )
                VALUES
                    (@CompId, 'HSS-PEB-COL-300', 'Built-Up PEB Tapered H-Column (300x200mm)', 'Primary Structural Steel Column Member', @FgClassId, ISNULL(@UomMt, @UomPcs),
                     'Bundle (1 MT)', 'ASTM A572 Grade 50', 115000.00, 115000.00, 142000.00, 148000.00,
                     10, 5, 14, @DefFgWh, 'B-01-FG', '7308.90.00', 1000.00, 1005.00, 'KG', 2, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Mandatory QC', 0, 1, 1, 'Active', 1, 'System'),
                    (@CompId, 'HSS-HRC-8MM', 'Hot Rolled Steel Coil (8.0mm x 1250mm)', 'Structural Mother Coil for Web/Flange Fabrication', @RmClassId, ISNULL(@UomMt, @UomPcs),
                     'Steel Master Coil (3-5 MT)', 'ASTM A572 Grade 50', 92000.00, 92000.00, 105000.00, 110000.00,
                     25, 15, 30, ISNULL(@DefRmWh, @DefFgWh), 'A-01-R1', '7208.37.00', 1000.00, 1002.00, 'KG', 2, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Mill Cert Check', 1, 0, 0, 'Active', 1, 'System');
            END
            ELSE IF @CompId = 3 -- HIL (Hatim Industries Ltd)
            BEGIN
                INSERT INTO inv.Products (
                    CompanyId, ProductCode, ProductName, Description, ProductClassId, PrimaryUomId,
                    PackSize, MaterialGrade, StandardCost, CurrentCost, SellingPrice, MinimumRetailPrice,
                    ReorderLevel, SafetyStock, LeadTimeDays, DefaultWarehouseId, DefaultStorageBin,
                    HsCode, NetWeight, GrossWeight, WeightUom, TrackingPolicy, CostingMethod,
                    TaxScheduleCode, TaxRatePercent, QcPolicy, IsPurchasable, IsSellable, IsManufactured,
                    LifecycleStatus, IsActive, CreatedBy
                )
                VALUES
                    (@CompId, 'HIL-EXEC-DESK-01', 'Hatim Direktor Executive Office Desk (2100mm)', 'Premium Melamine & Veneer Executive Workstation', @FgClassId, @UomPcs,
                     'Corrugated Master Carton', 'E1 Grade Moisture-Resistant Board', 28500.00, 28500.00, 39500.00, 42000.00,
                     15, 8, 7, @DefFgWh, 'B-01-FG', '9403.30.00', 68.00, 72.50, 'KG', 1, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Mandatory QC', 0, 1, 1, 'Active', 1, 'System'),
                    (@CompId, 'HIL-WORK-4P', 'Modular 4-Person Corporate Cluster Workstation', 'Open-Plan Aluminum Partition & E1 Board Workstation', @FgClassId, @UomPcs,
                     'Corrugated Master Carton', 'E1 Grade Moisture-Resistant Board', 52000.00, 52000.00, 74000.00, 78000.00,
                     8, 4, 10, @DefFgWh, 'B-01-FG', '9403.30.00', 145.00, 152.00, 'KG', 1, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Mandatory QC', 0, 1, 1, 'Active', 1, 'System'),
                    (@CompId, 'HIL-MDF-18MM', 'E1 Grade Melamine Faced MDF Board (18mm, 4x8 ft)', 'Imported Moisture-Resistant Furniture Core Board', @RmClassId, @UomPcs,
                     'Wooden / Euro Pallet', 'E1 Grade Moisture-Resistant Board', 2450.00, 2450.00, 3100.00, 3300.00,
                     100, 50, 14, ISNULL(@DefRmWh, @DefFgWh), 'A-02-R2', '4411.14.00', 38.00, 38.50, 'KG', 2, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Visual Inspection', 1, 0, 0, 'Active', 1, 'System');
            END
            ELSE IF @CompId = 4 -- HPL (Hatim Polymer Ltd)
            BEGIN
                INSERT INTO inv.Products (
                    CompanyId, ProductCode, ProductName, Description, ProductClassId, PrimaryUomId,
                    PackSize, MaterialGrade, StandardCost, CurrentCost, SellingPrice, MinimumRetailPrice,
                    ReorderLevel, SafetyStock, LeadTimeDays, DefaultWarehouseId, DefaultStorageBin,
                    HsCode, NetWeight, GrossWeight, WeightUom, TrackingPolicy, CostingMethod,
                    TaxScheduleCode, TaxRatePercent, QcPolicy, IsPurchasable, IsSellable, IsManufactured,
                    LifecycleStatus, IsActive, CreatedBy
                )
                VALUES
                    (@CompId, 'HPL-UPVC-110MM', 'Hatim High-Pressure uPVC Pipe (110mm x 6m, PN10)', 'Industrial & Water Supply Heavy-Duty uPVC Pipe', @FgClassId, @UomPcs,
                     'Loose / Unpacked Bulk', 'Standard Commercial Spec', 1650.00, 1650.00, 2250.00, 2400.00,
                     120, 60, 5, @DefFgWh, 'B-01-FG', '3917.23.00', 14.20, 14.20, 'KG', 2, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Mandatory QC', 0, 1, 1, 'Active', 1, 'System'),
                    (@CompId, 'HPL-RESIN-HDPE', 'Virgin HDPE Film & Extrusion Grade Resin (SABIC)', 'Imported Petrochemical Polymer Granules', @RmClassId, ISNULL(@UomKg, @UomPcs),
                     '25 KG Woven Sack / Bag', 'HDPE Film Extrusion Grade', 148.00, 148.00, 175.00, 185.00,
                     5000, 2000, 21, ISNULL(@DefRmWh, @DefFgWh), 'C-01-SILO', '3901.20.00', 1.00, 1.01, 'KG', 2, 'MovingAverage',
                     'VAT_STD_15', 15.00, 'Sampling QC', 1, 0, 0, 'Active', 1, 'System');
            END;

            -- Seed initial stock balances for newly created products so Inventory Stock Status works immediately
            INSERT INTO inv.ProductStock (CompanyId, WarehouseId, ProductId, QuantityOnHand, AllocatedQuantity, AverageCost, LastTransactionDate, CreatedBy)
            SELECT
                p.CompanyId,
                ISNULL(p.DefaultWarehouseId, @DefFgWh),
                p.Id,
                CASE WHEN p.IsSellable = 1 THEN 50.0000 ELSE 200.0000 END,
                0.0000,
                p.StandardCost,
                SYSUTCDATETIME(),
                'System'
            FROM inv.Products p
            WHERE p.CompanyId = @CompId
              AND NOT EXISTS (
                  SELECT 1 FROM inv.ProductStock ps
                  WHERE ps.CompanyId = p.CompanyId AND ps.ProductId = p.Id
              );
        END;

        -- 3.10 Vendor Groups & Vendors (ap.VendorGroups, ap.Vendors)
        DECLARE @ApCtrlId INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '2110');

        IF NOT EXISTS (SELECT 1 FROM ap.VendorGroups WHERE CompanyId = @CompId AND GroupCode = 'VG-DOMESTIC')
        BEGIN
            INSERT INTO ap.VendorGroups (CompanyId, GroupCode, GroupName, CreatedBy)
            VALUES
                (@CompId, 'VG-DOMESTIC', @CompPrefix + N' Domestic Raw Material Suppliers', 'System'),
                (@CompId, 'VG-IMPORT',   @CompPrefix + N' International / Import Suppliers', 'System');
        END;

        DECLARE @VgDomId INT = (SELECT TOP 1 Id FROM ap.VendorGroups WHERE CompanyId = @CompId AND GroupCode = 'VG-DOMESTIC');

        IF NOT EXISTS (SELECT 1 FROM ap.Vendors WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO ap.Vendors (
                CompanyId, VendorCode, VendorName, VendorGroupId, ContactPerson, Phone, Email,
                AddressLine1, City, CountryCode, GlControlAccountId, CurrentBalance, CreditLimit, CreditPeriodDays, IsActive, CreatedBy
            )
            VALUES (
                @CompId, 'VEND-' + @CompPrefix + '-01',
                CASE @CompId
                    WHEN 2 THEN N'BSRM Steels & Alloys Supply Ltd.'
                    WHEN 3 THEN N'Meridian Timber & Hardware Impex Ltd.'
                    ELSE N'Gulf Petrochemical Resins Trading LLC'
                END,
                @VgDomId, 'Commercial Manager', '+880171188000' + CAST(@CompId AS NVARCHAR(2)),
                'supply@' + LOWER(@CompPrefix) + '-vendor.com',
                'Commercial Zone, Plot 18', 'Dhaka', 'BGD', @ApCtrlId, 0, 2500000.00, 45, 1, 'System'
            );
        END;

        -- 3.11 Customer Groups, Customers & Sales Centers (ar.CustomerGroups, ar.Customers, sales.SalesCenters)
        DECLARE @ArCtrlId INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1130');

        IF NOT EXISTS (SELECT 1 FROM ar.CustomerGroups WHERE CompanyId = @CompId AND GroupCode = 'CG-CORP')
        BEGIN
            INSERT INTO ar.CustomerGroups (CompanyId, GroupCode, GroupName, CreatedBy)
            VALUES
                (@CompId, 'CG-CORP',   @CompPrefix + N' Corporate & Institutional Clients', 'System'),
                (@CompId, 'CG-DEALER', @CompPrefix + N' Authorized Dealers & Distributors', 'System');
        END;

        DECLARE @CgCorpId INT = (SELECT TOP 1 Id FROM ar.CustomerGroups WHERE CompanyId = @CompId AND GroupCode = 'CG-CORP');

        IF NOT EXISTS (SELECT 1 FROM ar.Customers WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO ar.Customers (
                CompanyId, CustomerCode, CustomerName, CustomerGroupId, ContactPerson,
                Phone, Email, AddressLine1, City, CountryCode, TaxId,
                GlControlAccountId, CurrentBalance, CreditLimit, CreditPeriodDays, IsActive, CreatedBy
            )
            VALUES (
                @CompId, 'CUST-' + @CompPrefix + '-01',
                CASE @CompId
                    WHEN 2 THEN N'Meghna Industrial Economic Zone Ltd.'
                    WHEN 3 THEN N'Brac Corporate Interiors & Workspaces'
                    ELSE N'WASA Infrastructure & Pipeline Project'
                END,
                @CgCorpId, 'Head of Procurement', '+880181177000' + CAST(@CompId AS NVARCHAR(2)),
                'procurement@' + LOWER(@CompPrefix) + '-client.com',
                'Gulshan Avenue, Circle 2', 'Dhaka', 'BGD', 'BIN-' + @CompPrefix + '-0091',
                @ArCtrlId, 0, 1500000.00, 30, 1, 'System'
            );
        END;

        IF NOT EXISTS (SELECT 1 FROM sales.SalesCenters WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO sales.SalesCenters (CompanyId, CenterCode, CenterName, WarehouseId, IsActive, CreatedBy)
            VALUES (@CompId, 'SC-' + @CompPrefix + '-01', @CompPrefix + N' Central Sales & Distribution Center', @DefFgWh, 1, 'System');
        END;

        -- 3.12 HR Departments, Designations & Employees (hr.Departments, hr.Designations, hr.Employees)
        IF NOT EXISTS (SELECT 1 FROM hr.Departments WHERE CompanyId = @CompId AND DepartmentCode = 'OPS')
        BEGIN
            INSERT INTO hr.Departments (CompanyId, DepartmentCode, DepartmentName, IsActive, CreatedBy)
            VALUES
                (@CompId, 'OPS',  @CompPrefix + N' Plant Operations & Production', 1, 'System'),
                (@CompId, 'FIN',  @CompPrefix + N' Finance & Accounts', 1, 'System'),
                (@CompId, 'HR',   @CompPrefix + N' Human Resources & Admin', 1, 'System');
        END;

        IF NOT EXISTS (SELECT 1 FROM hr.Designations WHERE CompanyId = @CompId AND DesignationCode = 'MGR-OPS')
        BEGIN
            INSERT INTO hr.Designations (CompanyId, DesignationCode, DesignationTitle, IsActive, CreatedBy)
            VALUES
                (@CompId, 'MGR-OPS', N'Operations Manager', 1, 'System'),
                (@CompId, 'SR-ACC',  N'Senior Accountant', 1, 'System');
        END;

        DECLARE @DeptOpsId  INT = (SELECT TOP 1 Id FROM hr.Departments WHERE CompanyId = @CompId AND DepartmentCode = 'OPS');
        DECLARE @DesigOpsId INT = (SELECT TOP 1 Id FROM hr.Designations WHERE CompanyId = @CompId AND DesignationCode = 'MGR-OPS');

        IF NOT EXISTS (SELECT 1 FROM hr.Employees WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO hr.Employees (
                CompanyId, EmployeeCode, FirstName, LastName, DepartmentId, DesignationId,
                DateOfBirth, JoiningDate, Email, Phone, GrossSalary, BasicSalary, HouseRent,
                MedicalAllowance, ConveyanceAllowance, OtherAllowance, ProvidentFundDeduction,
                TaxDeduction, BankName, BankAccountNumber, IsActive, CreatedBy
            )
            VALUES (
                @CompId, 'EMP-' + @CompPrefix + '-001',
                CASE @CompId WHEN 2 THEN 'Kamrul' WHEN 3 THEN 'Nadia' ELSE 'Shafiqul' END,
                CASE @CompId WHEN 2 THEN 'Hasan'  WHEN 3 THEN 'Islam' ELSE 'Alam'     END,
                @DeptOpsId, @DesigOpsId,
                '1989-04-12', '2023-01-01',
                'ops.mgr@' + LOWER(@CompPrefix) + '.hatimgroup.com',
                '+880171155000' + CAST(@CompId AS NVARCHAR(2)),
                95000.00, 50000.00, 25000.00, 10000.00, 10000.00, 0.00, 2500.00,
                3000.00, 'Prime Bank PLC', '11020300' + CAST(@CompId AS NVARCHAR(5)) + '-001', 1, 'System'
            );
        END;

        -- 3.13 Fixed Asset Groups & Assets (fa.AssetGroups, fa.Assets)
        DECLARE @MachGlId      INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1210');
        DECLARE @AccumDeprGlId INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1290');
        DECLARE @DeprExpGlId   INT = (SELECT TOP 1 Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '5250');

        IF NOT EXISTS (SELECT 1 FROM fa.AssetGroups WHERE CompanyId = @CompId AND GroupCode = 'GRP-MACH')
        BEGIN
            INSERT INTO fa.AssetGroups (CompanyId, GroupCode, GroupName, DepreciationRate, GlAssetAccountId, GlAccumDeprAccountId, GlDeprExpenseAccountId, CreatedBy)
            VALUES (@CompId, 'GRP-MACH', @CompPrefix + N' Plant & Industrial Machinery', 10.00, @MachGlId, @AccumDeprGlId, @DeprExpGlId, 'System');
        END;

        DECLARE @FaGrpId INT = (SELECT TOP 1 Id FROM fa.AssetGroups WHERE CompanyId = @CompId AND GroupCode = 'GRP-MACH');

        IF NOT EXISTS (SELECT 1 FROM fa.Assets WHERE CompanyId = @CompId)
        BEGIN
            INSERT INTO fa.Assets (
                CompanyId, AssetCode, AssetName, AssetGroupId, PurchaseDate, PurchaseCost,
                CurrentBookValue, AccumulatedDepreciation, SalvageValue, UsefulLifeMonths,
                DepreciationMethod, IsDisposed, CreatedBy
            )
            VALUES (
                @CompId, 'AST-' + @CompPrefix + '-01',
                CASE @CompId
                    WHEN 2 THEN N'CNC Plasma Plate Cutting & Drilling Line'
                    WHEN 3 THEN N'Homag Automatic Edge Banding & Panel Saw Line'
                    ELSE N'KraussMaffei Twin-Screw uPVC Pipe Extruder'
                END,
                @FaGrpId, '2026-01-15', 1850000.00, 1850000.00, 0.00, 150000.00, 120,
                'StraightLine', 0, 'System'
            );
        END;
    END;

    SET @CompId = @CompId + 1;
END;
GO

PRINT 'Migration 018_Multi_Company_Foundational_Provisioning_And_Isolation completed successfully.';
GO
