-- ==============================================================================
-- Migration: 012_Enterprise_Guid_DualId_Audit_And_Performance.sql
-- Project: ERP Modernization (.NET 10 Modular Monolith + MSSQL 2025)
-- Description: 
--   1. Adds SysGuid (UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID()) with 
--      unique indexes across all 72 domain tables for universal internal GUID identity.
--   2. Ensures 100% coverage of standard audit columns (IsDeleted, CreatedAtUtc, 
--      CreatedBy, ModifiedAtUtc, ModifiedBy, DeletedAtUtc, DeletedBy) across all tables.
--   3. Establishes sa.DocumentSequences table & atomic concurrency-safe stored procedure
--      sa.usp_GetNextDocumentNumber for user-friendly secondary IDs.
--   4. Creates enterprise audit history repository sa.AuditLogs for forensic tracking.
--   5. Applies filtered unique indexes (WHERE IsDeleted = 0) and foreign key covering indexes.
-- ==============================================================================

USE [ERP];
GO

-- ------------------------------------------------------------------------------
-- 1. ADD SysGuid (UNIQUEIDENTIFIER) WITH UNIQUE INDEXES ACROSS ALL DOMAIN TABLES
-- ------------------------------------------------------------------------------
DECLARE @sqlGuid NVARCHAR(MAX) = N'';

SELECT @sqlGuid += N'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''SysGuid'')
BEGIN
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' 
    ADD SysGuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + s.name + '_' + t.name + '_SysGuid') + ' DEFAULT NEWSEQUENTIALID();
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''' + 'UX_' + s.name + '_' + t.name + '_SysGuid'')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX ' + QUOTENAME('UX_' + s.name + '_' + t.name + '_SysGuid') + ' 
    ON ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' (SysGuid);
END;
'
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name IN ('sa', 'gl', 'bc', 'ap', 'ar', 'inv', 'sales', 'purch', 'hr', 'fa', 're', 'mfg');

EXEC sp_executesql @sqlGuid;
GO

-- ------------------------------------------------------------------------------
-- 2. ENSURE 100% COVERAGE OF STANDARD AUDIT & SOFT-DELETE COLUMNS
-- ------------------------------------------------------------------------------
DECLARE @sqlAudit NVARCHAR(MAX) = N'';

SELECT @sqlAudit += N'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''IsDeleted'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD IsDeleted BIT NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + s.name + '_' + t.name + '_IsDeleted') + ' DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''CreatedAtUtc'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + s.name + '_' + t.name + '_CreatedAtUtc') + ' DEFAULT SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''CreatedBy'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD CreatedBy NVARCHAR(50) NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + s.name + '_' + t.name + '_CreatedBy') + ' DEFAULT ''System'';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''ModifiedAtUtc'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD ModifiedAtUtc DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''ModifiedBy'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD ModifiedBy NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''DeletedAtUtc'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD DeletedAtUtc DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''DeletedBy'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD DeletedBy NVARCHAR(50) NULL;
'
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name IN ('sa', 'gl', 'bc', 'ap', 'ar', 'inv', 'sales', 'purch', 'hr', 'fa', 're', 'mfg');

EXEC sp_executesql @sqlAudit;
GO

-- ------------------------------------------------------------------------------
-- 3. CONCURRENCY-SAFE DOCUMENT SEQUENCING INFRASTRUCTURE
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.DocumentSequences'))
BEGIN
    CREATE TABLE sa.DocumentSequences (
        Id INT IDENTITY(1,1) PRIMARY KEY CLUSTERED,
        SysGuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_sa_DocumentSequences_SysGuid DEFAULT NEWSEQUENTIALID(),
        CompanyId INT NOT NULL CONSTRAINT FK_sa_DocumentSequences_Company REFERENCES sa.Companies(Id),
        DocumentType NVARCHAR(50) NOT NULL, -- 'JV', 'SO', 'PO', 'INV', 'BILL', 'GRN', 'PAY', 'REC', 'DC', 'EMP', 'CUST', 'VEND', 'PRD', 'AST', 'MO'
        Prefix NVARCHAR(20) NOT NULL,
        FiscalYear NVARCHAR(10) NOT NULL DEFAULT 'GLOBAL',
        CurrentNumber BIGINT NOT NULL DEFAULT 0,
        NumberLength INT NOT NULL DEFAULT 4,
        Suffix NVARCHAR(10) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_sa_DocumentSequences_Scope UNIQUE (CompanyId, DocumentType, Prefix, FiscalYear)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_sa_DocumentSequences_SysGuid ON sa.DocumentSequences(SysGuid);
END;
GO

-- Stored procedure for transaction-safe document numbering using atomic UPDLOCK & ROWLOCK
IF OBJECT_ID('sa.usp_GetNextDocumentNumber', 'P') IS NOT NULL
    DROP PROCEDURE sa.usp_GetNextDocumentNumber;
GO

CREATE PROCEDURE sa.usp_GetNextDocumentNumber
    @CompanyId INT,
    @DocumentType NVARCHAR(50),
    @Prefix NVARCHAR(20) = NULL,
    @FiscalYear NVARCHAR(10) = NULL,
    @NumberLength INT = 4,
    @GeneratedNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Defaults
    IF @Prefix IS NULL OR @Prefix = ''
    BEGIN
        SET @Prefix = CASE @DocumentType
            WHEN 'JV' THEN 'JV-'
            WHEN 'SO' THEN 'SO-'
            WHEN 'PO' THEN 'PO-'
            WHEN 'INV' THEN 'INV-'
            WHEN 'BILL' THEN 'PB-'
            WHEN 'GRN' THEN 'GRN-'
            WHEN 'PAY' THEN 'PAY-'
            WHEN 'REC' THEN 'REC-'
            WHEN 'DC' THEN 'DC-'
            WHEN 'ADJ' THEN 'ADJ-'
            WHEN 'TRN' THEN 'TRN-'
            WHEN 'EMP' THEN 'EMP-'
            WHEN 'CUST' THEN 'CUST-'
            WHEN 'VEND' THEN 'VEND-'
            WHEN 'PRD' THEN 'PRD-'
            WHEN 'AST' THEN 'AST-'
            WHEN 'MO' THEN 'MO-'
            ELSE UPPER(@DocumentType) + '-'
        END;
    END;

    IF @FiscalYear IS NULL OR @FiscalYear = ''
    BEGIN
        SET @FiscalYear = 'GLOBAL';
    END;

    DECLARE @NextVal BIGINT;

    -- Atomic lock and increment
    UPDATE sa.DocumentSequences WITH (UPDLOCK, ROWLOCK)
    SET @NextVal = CurrentNumber = CurrentNumber + 1,
        ModifiedAtUtc = SYSUTCDATETIME(),
        ModifiedBy = 'System'
    WHERE CompanyId = @CompanyId 
      AND DocumentType = @DocumentType 
      AND Prefix = @Prefix 
      AND FiscalYear = @FiscalYear;

    IF @@ROWCOUNT = 0
    BEGIN
        -- If sequence record doesn't exist, insert atomically
        BEGIN TRY
            INSERT INTO sa.DocumentSequences (CompanyId, DocumentType, Prefix, FiscalYear, CurrentNumber, NumberLength)
            VALUES (@CompanyId, @DocumentType, @Prefix, @FiscalYear, 1, @NumberLength);
            
            SET @NextVal = 1;
        END TRY
        BEGIN CATCH
            -- Retry update on concurrent race insert
            UPDATE sa.DocumentSequences WITH (UPDLOCK, ROWLOCK)
            SET @NextVal = CurrentNumber = CurrentNumber + 1,
                ModifiedAtUtc = SYSUTCDATETIME(),
                ModifiedBy = 'System'
            WHERE CompanyId = @CompanyId 
              AND DocumentType = @DocumentType 
              AND Prefix = @Prefix 
              AND FiscalYear = @FiscalYear;
        END CATCH
    END;

    -- Format final output (e.g. JV-202610-0001 or EMP-000105)
    IF @FiscalYear <> 'GLOBAL'
        SET @GeneratedNumber = @Prefix + @FiscalYear + '-' + RIGHT(REPLICATE('0', @NumberLength) + CAST(@NextVal AS NVARCHAR(20)), @NumberLength);
    ELSE
        SET @GeneratedNumber = @Prefix + RIGHT(REPLICATE('0', @NumberLength) + CAST(@NextVal AS NVARCHAR(20)), @NumberLength);

    RETURN 0;
END;
GO

-- ------------------------------------------------------------------------------
-- 4. ENTERPRISE AUDIT TRAIL & HISTORY LOG TABLE (sa.AuditLogs)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.AuditLogs'))
BEGIN
    CREATE TABLE sa.AuditLogs (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY CLUSTERED,
        SysGuid UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_sa_AuditLogs_SysGuid DEFAULT NEWSEQUENTIALID(),
        CompanyId INT NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        EntitySysGuid UNIQUEIDENTIFIER NOT NULL,
        RecordIdentifier NVARCHAR(100) NULL,
        Action NVARCHAR(20) NOT NULL, -- 'Create', 'Update', 'Delete', 'Restore'
        UserId NVARCHAR(50) NULL,
        UserName NVARCHAR(100) NOT NULL DEFAULT 'System',
        TimestampUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        OldValues NVARCHAR(MAX) NULL, -- JSON formatted snapshot
        NewValues NVARCHAR(MAX) NULL, -- JSON formatted snapshot
        AffectedColumns NVARCHAR(MAX) NULL,
        Source NVARCHAR(100) NOT NULL DEFAULT 'Web API',
        CONSTRAINT FK_sa_AuditLogs_Company FOREIGN KEY (CompanyId) REFERENCES sa.Companies(Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_sa_AuditLogs_SysGuid ON sa.AuditLogs(SysGuid);
    CREATE NONCLUSTERED INDEX IX_sa_AuditLogs_Entity ON sa.AuditLogs(EntityName, EntitySysGuid);
    CREATE NONCLUSTERED INDEX IX_sa_AuditLogs_Company_Time ON sa.AuditLogs(CompanyId, TimestampUtc DESC);
    CREATE NONCLUSTERED INDEX IX_sa_AuditLogs_RecordIdentifier ON sa.AuditLogs(RecordIdentifier) WHERE RecordIdentifier IS NOT NULL;
END;
GO

-- ------------------------------------------------------------------------------
-- 5. FILTERED UNIQUE INDEXES (WHERE IsDeleted = 0) FOR ACTIVE ENTITY UNIQUENESS
-- ------------------------------------------------------------------------------

-- GL Accounts: Active AccountCode uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('gl.Accounts') AND name = 'UX_gl_Accounts_Company_Code_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_gl_Accounts_Company_Code_Active 
    ON gl.Accounts (CompanyId, AccountCode) 
    WHERE IsDeleted = 0;
END;
GO

-- GL Journal Vouchers: Active VoucherNumber uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('gl.JournalVouchers') AND name = 'UX_gl_JournalVouchers_Number_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_gl_JournalVouchers_Number_Active 
    ON gl.JournalVouchers (CompanyId, VoucherNumber) 
    WHERE IsDeleted = 0;
END;
GO

-- AR Customers: Active CustomerCode uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ar.Customers') AND name = 'UX_ar_Customers_Code_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ar_Customers_Code_Active 
    ON ar.Customers (CompanyId, CustomerCode) 
    WHERE IsDeleted = 0;
END;
GO

-- AP Vendors: Active VendorCode uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ap.Vendors') AND name = 'UX_ap_Vendors_Code_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ap_Vendors_Code_Active 
    ON ap.Vendors (CompanyId, VendorCode) 
    WHERE IsDeleted = 0;
END;
GO

-- Inventory Products: Active ProductCode uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('inv.Products') AND name = 'UX_inv_Products_Code_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_inv_Products_Code_Active 
    ON inv.Products (CompanyId, ProductCode) 
    WHERE IsDeleted = 0;
END;
GO

-- Sales Orders: Active OrderNumber uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('sales.SalesOrders') AND name = 'UX_sales_SalesOrders_Number_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_sales_SalesOrders_Number_Active 
    ON sales.SalesOrders (CompanyId, OrderNumber) 
    WHERE IsDeleted = 0;
END;
GO

-- Purchase Orders: Active PoNumber uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('purch.PurchaseOrders') AND name = 'UX_purch_PurchaseOrders_Number_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_purch_PurchaseOrders_Number_Active 
    ON purch.PurchaseOrders (CompanyId, PoNumber) 
    WHERE IsDeleted = 0;
END;
GO

-- HR Employees: Active EmployeeCode uniqueness per Company
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('hr.Employees') AND name = 'UX_hr_Employees_Code_Active')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_hr_Employees_Code_Active 
    ON hr.Employees (CompanyId, EmployeeCode) 
    WHERE IsDeleted = 0;
END;
GO

-- ------------------------------------------------------------------------------
-- 6. PERFORMANCE INDEXES ON CRITICAL FOREIGN KEYS AND TRANSACTION LOOKUPS
-- ------------------------------------------------------------------------------

-- GL Lines by AccountId & VoucherId with filtered active records
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('gl.JournalVoucherLines') AND name = 'IX_gl_JvLines_AccountId_Active')
BEGIN
    CREATE NONCLUSTERED INDEX IX_gl_JvLines_AccountId_Active 
    ON gl.JournalVoucherLines (AccountId, VoucherId) 
    INCLUDE (Debit, Credit) 
    WHERE IsDeleted = 0;
END;
GO

-- Inventory StockLedger lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('inv.StockLedgers') AND name = 'IX_inv_StockLedgers_Lookup_Active')
BEGIN
    CREATE NONCLUSTERED INDEX IX_inv_StockLedgers_Lookup_Active 
    ON inv.StockLedgers (CompanyId, WarehouseId, ProductId, TransactionDate) 
    INCLUDE (QuantityIn, QuantityOut, BalanceQuantity, UnitCost)
    WHERE IsDeleted = 0;
END;
GO

-- AR Invoices customer lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ar.Invoices') AND name = 'IX_ar_Invoices_Customer_Status')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ar_Invoices_Customer_Status 
    ON ar.Invoices (CompanyId, CustomerId, [Status], InvoiceDate) 
    INCLUDE (TotalAmount, PaidAmount, BalanceAmount) 
    WHERE IsDeleted = 0;
END;
GO

-- AP PurchaseBills vendor lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ap.PurchaseBills') AND name = 'IX_ap_PurchaseBills_Vendor_Status')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ap_PurchaseBills_Vendor_Status 
    ON ap.PurchaseBills (CompanyId, VendorId, [Status], BillDate) 
    INCLUDE (TotalAmount, PaidAmount, BalanceAmount) 
    WHERE IsDeleted = 0;
END;
GO

PRINT 'Migration 012_Enterprise_Guid_DualId_Audit_And_Performance completed successfully.';
GO
