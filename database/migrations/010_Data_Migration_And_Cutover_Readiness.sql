-- ==============================================================================
-- InteAcc ERP Migration - Phase 10: Data Migration, Financial Parity Audit & Cutover Readiness
-- Migration: 010_Data_Migration_And_Cutover_Readiness.sql
-- ==============================================================================

USE [ERP];
GO

-- 1. Create sa.MigrationLogs
IF NOT EXISTS (SELECT * FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'sa' AND t.name = 'MigrationLogs')
BEGIN
    CREATE TABLE sa.MigrationLogs (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId INT NOT NULL,
        BatchNumber NVARCHAR(50) NOT NULL,
        SourceModule NVARCHAR(50) NOT NULL,
        TargetEntity NVARCHAR(100) NOT NULL,
        RecordsExtracted INT NOT NULL DEFAULT 0,
        RecordsMigrated INT NOT NULL DEFAULT 0,
        RecordsFailed INT NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Completed', -- Completed, InProgress, Failed
        DurationMs BIGINT NOT NULL DEFAULT 0,
        Details NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_MigrationLogs_Company FOREIGN KEY (CompanyId) REFERENCES sa.Companies(Id)
    );

    CREATE NONCLUSTERED INDEX IX_MigrationLogs_BatchNumber ON sa.MigrationLogs(BatchNumber);
    CREATE NONCLUSTERED INDEX IX_MigrationLogs_CompanyId ON sa.MigrationLogs(CompanyId);
END
GO

-- 2. Create sa.CutoverAudits
IF NOT EXISTS (SELECT * FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'sa' AND t.name = 'CutoverAudits')
BEGIN
    CREATE TABLE sa.CutoverAudits (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId INT NOT NULL,
        CutoverDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        SignedOffBy NVARCHAR(100) NOT NULL,
        CfoApprovalCode NVARCHAR(50) NOT NULL,
        TrialBalanceVariance DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        ArSubledgerVariance DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        ApSubledgerVariance DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        InventorySubledgerVariance DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        BankSubledgerVariance DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        IsParityVerified BIT NOT NULL DEFAULT 0,
        CutoverStatus NVARCHAR(30) NOT NULL DEFAULT 'Approved', -- Pending, Approved, GoLiveCompleted
        Notes NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_CutoverAudits_Company FOREIGN KEY (CompanyId) REFERENCES sa.Companies(Id)
    );

    CREATE NONCLUSTERED INDEX IX_CutoverAudits_CompanyId ON sa.CutoverAudits(CompanyId);
END
GO

-- 3. Create gl.HistoricalLedgerArchive
IF NOT EXISTS (SELECT * FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'gl' AND t.name = 'HistoricalLedgerArchive')
BEGIN
    CREATE TABLE gl.HistoricalLedgerArchive (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId INT NOT NULL,
        FiscalYear NVARCHAR(10) NOT NULL,
        PeriodMonth INT NOT NULL,
        LegacyVoucherNumber NVARCHAR(50) NOT NULL,
        VoucherDate DATE NOT NULL,
        AccountCode NVARCHAR(30) NOT NULL,
        AccountName NVARCHAR(200) NOT NULL,
        Particulars NVARCHAR(500) NULL,
        Debit DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        Credit DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
        CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'BDT',
        LegacySourceModule NVARCHAR(20) NOT NULL DEFAULT 'GL',
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT FK_HistoricalLedgerArchive_Company FOREIGN KEY (CompanyId) REFERENCES sa.Companies(Id)
    );

    CREATE NONCLUSTERED INDEX IX_HistoricalArchive_Company_FY ON gl.HistoricalLedgerArchive(CompanyId, FiscalYear, PeriodMonth);
    CREATE NONCLUSTERED INDEX IX_HistoricalArchive_AccountCode ON gl.HistoricalLedgerArchive(AccountCode);
    CREATE NONCLUSTERED INDEX IX_HistoricalArchive_LegacyVoucher ON gl.HistoricalLedgerArchive(LegacyVoucherNumber);
END
GO

-- 4. Seed Initial Migration Baseline Data & Historical Ledger Records
IF NOT EXISTS (SELECT * FROM sa.MigrationLogs WHERE BatchNumber = 'BATCH-INITIAL-MIGRATION-2026')
BEGIN
    INSERT INTO sa.MigrationLogs (CompanyId, BatchNumber, SourceModule, TargetEntity, RecordsExtracted, RecordsMigrated, RecordsFailed, Status, DurationMs, Details)
    VALUES 
    (1, 'BATCH-INITIAL-MIGRATION-2026', 'gl', 'ChartOfAccounts', 29, 29, 0, 'Completed', 142, 'Chart of accounts hierarchy successfully migrated from legacy GlAcct'),
    (1, 'BATCH-INITIAL-MIGRATION-2026', 'ar', 'Customers', 2, 2, 0, 'Completed', 85, 'Customer master profiles and credit limits mapped from legacy ArCustomer'),
    (1, 'BATCH-INITIAL-MIGRATION-2026', 'ap', 'Vendors', 2, 2, 0, 'Completed', 78, 'Vendor master profiles and payment terms mapped from legacy ApVendor'),
    (1, 'BATCH-INITIAL-MIGRATION-2026', 'inv', 'ProductsAndWarehouses', 4, 4, 0, 'Completed', 115, 'Product items and warehouse storage nodes mapped from legacy IcProduct and IcWh'),
    (1, 'BATCH-INITIAL-MIGRATION-2026', 'hr', 'Employees', 3, 3, 0, 'Completed', 92, 'Employee profiles, designations, and pay structures mapped from legacy PrEmployee'),
    (1, 'BATCH-INITIAL-MIGRATION-2026', 'fa', 'FixedAssets', 2, 2, 0, 'Completed', 64, 'Fixed asset schedules and depreciation policies mapped from legacy FaAssetProfile');
END
GO

-- Seed representative historical archived transactions for audited 2025 financial history
IF NOT EXISTS (SELECT * FROM gl.HistoricalLedgerArchive WHERE FiscalYear = '2025-2026')
BEGIN
    INSERT INTO gl.HistoricalLedgerArchive (CompanyId, FiscalYear, PeriodMonth, LegacyVoucherNumber, VoucherDate, AccountCode, AccountName, Particulars, Debit, Credit, CurrencyCode, LegacySourceModule)
    VALUES
    (1, '2025-2026', 12, 'LEGACY-JV-2025-001', '2025-12-31', '1120', 'Prime Bank Ltd. (A/C: 110203001)', 'Opening Cash & Bank Balance Transferred', 5000000.0000, 0.0000, 'BDT', 'BC'),
    (1, '2025-2026', 12, 'LEGACY-JV-2025-001', '2025-12-31', '3100', 'Retained Earnings', 'Opening Cash & Bank Balance Transferred', 0.0000, 5000000.0000, 'BDT', 'GL'),
    (1, '2025-2026', 12, 'LEGACY-JV-2025-002', '2025-12-31', '1130', 'Trade Accounts Receivable Control', 'Opening AR Control Balance Transferred', 1500000.0000, 0.0000, 'BDT', 'AR'),
    (1, '2025-2026', 12, 'LEGACY-JV-2025-002', '2025-12-31', '3100', 'Retained Earnings', 'Opening AR Control Balance Transferred', 0.0000, 1500000.0000, 'BDT', 'GL'),
    (1, '2025-2026', 12, 'LEGACY-JV-2025-003', '2025-12-31', '3100', 'Retained Earnings', 'Opening AP Control Balance Transferred', 800000.0000, 0.0000, 'BDT', 'GL'),
    (1, '2025-2026', 12, 'LEGACY-JV-2025-003', '2025-12-31', '2110', 'Trade Accounts Payable Control', 'Opening AP Control Balance Transferred', 0.0000, 800000.0000, 'BDT', 'AP');
END
GO
