-- ==============================================================================
-- Migration: 001_Initial_Modern_Schema.sql
-- Project: ERP Modernization (.NET 10 Modular Monolith + MSSQL 2025)
-- Description: Establishes modernized domain schemas (sa, gl, bc, ap, ar, inv, 
--              sales, purch, hr, fa) with enterprise auditing columns, foreign 
--              keys, and optimized indexing.
-- ==============================================================================

USE [ERP];
GO

-- 1. Create Domain Schemas
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'sa') EXEC('CREATE SCHEMA sa;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'gl') EXEC('CREATE SCHEMA gl;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'bc') EXEC('CREATE SCHEMA bc;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ap') EXEC('CREATE SCHEMA ap;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ar') EXEC('CREATE SCHEMA ar;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'inv') EXEC('CREATE SCHEMA inv;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'sales') EXEC('CREATE SCHEMA sales;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'purch') EXEC('CREATE SCHEMA purch;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'hr') EXEC('CREATE SCHEMA hr;');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'fa') EXEC('CREATE SCHEMA fa;');
GO

-- ==============================================================================
-- 2. SYSTEM ADMIN & SECURITY SCHEMA (sa)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.Companies'))
BEGIN
    CREATE TABLE sa.Companies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyCode NVARCHAR(20) NOT NULL CONSTRAINT UQ_sa_Companies_CompanyCode UNIQUE,
        CompanyName NVARCHAR(150) NOT NULL,
        AddressLine1 NVARCHAR(200) NULL,
        AddressLine2 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        PostalCode NVARCHAR(20) NULL,
        CountryCode NVARCHAR(10) NOT NULL DEFAULT 'BGD',
        Phone1 NVARCHAR(50) NULL,
        Phone2 NVARCHAR(50) NULL,
        Email NVARCHAR(100) NULL,
        WebAddress NVARCHAR(150) NULL,
        BaseCurrency NVARCHAR(10) NOT NULL DEFAULT 'BDT',
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.BusinessUnits'))
BEGIN
    CREATE TABLE sa.BusinessUnits (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_sa_BusinessUnits_Company REFERENCES sa.Companies(Id),
        BuCode NVARCHAR(20) NOT NULL,
        BuName NVARCHAR(150) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sa_BusinessUnits_Company_Code UNIQUE (CompanyId, BuCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.Currencies'))
BEGIN
    CREATE TABLE sa.Currencies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CurrencyCode NVARCHAR(10) NOT NULL CONSTRAINT UQ_sa_Currencies_CurrencyCode UNIQUE,
        CurrencyName NVARCHAR(50) NOT NULL,
        Symbol NVARCHAR(10) NULL,
        DecimalPlaces INT NOT NULL DEFAULT 2,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.ExchangeRates'))
BEGIN
    CREATE TABLE sa.ExchangeRates (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_sa_ExchangeRates_Company REFERENCES sa.Companies(Id),
        FromCurrencyCode NVARCHAR(10) NOT NULL,
        ToCurrencyCode NVARCHAR(10) NOT NULL,
        Rate DECIMAL(18, 6) NOT NULL,
        EffectiveDate DATE NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sa_ExchangeRates_Company_Currencies_Date UNIQUE (CompanyId, FromCurrencyCode, ToCurrencyCode, EffectiveDate)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.FiscalYears'))
BEGIN
    CREATE TABLE sa.FiscalYears (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_sa_FiscalYears_Company REFERENCES sa.Companies(Id),
        YearCode NVARCHAR(10) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        IsClosed BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sa_FiscalYears_Company_YearCode UNIQUE (CompanyId, YearCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.FiscalPeriods'))
BEGIN
    CREATE TABLE sa.FiscalPeriods (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FiscalYearId INT NOT NULL CONSTRAINT FK_sa_FiscalPeriods_Year REFERENCES sa.FiscalYears(Id),
        PeriodNumber INT NOT NULL,
        PeriodName NVARCHAR(50) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        IsClosed BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sa_FiscalPeriods_Year_Period UNIQUE (FiscalYearId, PeriodNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.Users'))
BEGIN
    CREATE TABLE sa.Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_sa_Users_UserCode UNIQUE,
        Username NVARCHAR(100) NOT NULL CONSTRAINT UQ_sa_Users_Username UNIQUE,
        Email NVARCHAR(150) NOT NULL CONSTRAINT UQ_sa_Users_Email UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        DefaultCompanyId INT NULL CONSTRAINT FK_sa_Users_DefaultCompany REFERENCES sa.Companies(Id),
        IsActive BIT NOT NULL DEFAULT 1,
        LastLoginAtUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.Roles'))
BEGIN
    CREATE TABLE sa.Roles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_sa_Roles_RoleCode UNIQUE,
        RoleName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        IsSystemRole BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.UserRoles'))
BEGIN
    CREATE TABLE sa.UserRoles (
        UserId INT NOT NULL CONSTRAINT FK_sa_UserRoles_User REFERENCES sa.Users(Id) ON DELETE CASCADE,
        RoleId INT NOT NULL CONSTRAINT FK_sa_UserRoles_Role REFERENCES sa.Roles(Id) ON DELETE CASCADE,
        AssignedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_sa_UserRoles PRIMARY KEY (UserId, RoleId)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sa.UserCompanyAccess'))
BEGIN
    CREATE TABLE sa.UserCompanyAccess (
        UserId INT NOT NULL CONSTRAINT FK_sa_UserCompanyAccess_User REFERENCES sa.Users(Id) ON DELETE CASCADE,
        CompanyId INT NOT NULL CONSTRAINT FK_sa_UserCompanyAccess_Company REFERENCES sa.Companies(Id) ON DELETE CASCADE,
        CONSTRAINT PK_sa_UserCompanyAccess PRIMARY KEY (UserId, CompanyId)
    );
END;
GO

-- ==============================================================================
-- 3. GENERAL LEDGER SCHEMA (gl)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'gl.AccountGroups'))
BEGIN
    CREATE TABLE gl.AccountGroups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_gl_AccountGroups_Company REFERENCES sa.Companies(Id),
        GroupCode NVARCHAR(20) NOT NULL,
        GroupName NVARCHAR(100) NOT NULL,
        AccountCategory INT NOT NULL, -- 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense
        ParentGroupId INT NULL CONSTRAINT FK_gl_AccountGroups_Parent REFERENCES gl.AccountGroups(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_gl_AccountGroups_Company_Code UNIQUE (CompanyId, GroupCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'gl.Accounts'))
BEGIN
    CREATE TABLE gl.Accounts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_gl_Accounts_Company REFERENCES sa.Companies(Id),
        AccountCode NVARCHAR(30) NOT NULL,
        AccountName NVARCHAR(150) NOT NULL,
        AccountGroupId INT NOT NULL CONSTRAINT FK_gl_Accounts_Group REFERENCES gl.AccountGroups(Id),
        AccountType INT NOT NULL DEFAULT 2, -- 1=Header/Summary, 2=Posting
        IsReconcilable BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_gl_Accounts_Company_Code UNIQUE (CompanyId, AccountCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'gl.SubAccounts'))
BEGIN
    CREATE TABLE gl.SubAccounts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_gl_SubAccounts_Company REFERENCES sa.Companies(Id),
        SubAccountCode NVARCHAR(30) NOT NULL,
        SubAccountName NVARCHAR(150) NOT NULL,
        ParentAccountId INT NOT NULL CONSTRAINT FK_gl_SubAccounts_ParentAccount REFERENCES gl.Accounts(Id),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_gl_SubAccounts_Company_Code UNIQUE (CompanyId, SubAccountCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'gl.JournalBatches'))
BEGIN
    CREATE TABLE gl.JournalBatches (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_gl_JournalBatches_Company REFERENCES sa.Companies(Id),
        BatchNumber NVARCHAR(30) NOT NULL,
        BatchDate DATE NOT NULL,
        SourceModule NVARCHAR(10) NOT NULL DEFAULT 'GL',
        Description NVARCHAR(255) NULL,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Posted, 2=Void
        TotalDebit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalCredit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_gl_JournalBatches_Company_BatchNumber UNIQUE (CompanyId, BatchNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'gl.JournalVouchers'))
BEGIN
    CREATE TABLE gl.JournalVouchers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BatchId INT NOT NULL CONSTRAINT FK_gl_JournalVouchers_Batch REFERENCES gl.JournalBatches(Id),
        CompanyId INT NOT NULL CONSTRAINT FK_gl_JournalVouchers_Company REFERENCES sa.Companies(Id),
        VoucherNumber NVARCHAR(30) NOT NULL,
        VoucherDate DATE NOT NULL,
        Reference NVARCHAR(50) NULL,
        Description NVARCHAR(255) NULL,
        FiscalYearId INT NOT NULL CONSTRAINT FK_gl_JournalVouchers_Year REFERENCES sa.FiscalYears(Id),
        FiscalPeriodId INT NOT NULL CONSTRAINT FK_gl_JournalVouchers_Period REFERENCES sa.FiscalPeriods(Id),
        IsPosted BIT NOT NULL DEFAULT 0,
        PostedAtUtc DATETIME2 NULL,
        PostedBy NVARCHAR(50) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_gl_JournalVouchers_Company_VoucherNumber UNIQUE (CompanyId, VoucherNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'gl.JournalVoucherLines'))
BEGIN
    CREATE TABLE gl.JournalVoucherLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VoucherId INT NOT NULL CONSTRAINT FK_gl_JournalVoucherLines_Voucher REFERENCES gl.JournalVouchers(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        AccountId INT NOT NULL CONSTRAINT FK_gl_JournalVoucherLines_Account REFERENCES gl.Accounts(Id),
        SubAccountId INT NULL CONSTRAINT FK_gl_JournalVoucherLines_SubAccount REFERENCES gl.SubAccounts(Id),
        Debit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Credit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'BDT',
        ExchangeRate DECIMAL(18, 6) NOT NULL DEFAULT 1,
        BaseDebit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        BaseCredit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Particulars NVARCHAR(255) NULL,
        BusinessUnitId INT NULL CONSTRAINT FK_gl_JournalVoucherLines_BU REFERENCES sa.BusinessUnits(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_gl_JournalVoucherLines_Voucher_Line UNIQUE (VoucherId, LineNumber)
    );
END;
GO

-- ==============================================================================
-- 4. BANK & CASH SCHEMA (bc)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'bc.Banks'))
BEGIN
    CREATE TABLE bc.Banks (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_bc_Banks_Company REFERENCES sa.Companies(Id),
        BankCode NVARCHAR(20) NOT NULL,
        BankName NVARCHAR(150) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_bc_Banks_Company_Code UNIQUE (CompanyId, BankCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'bc.BankBranches'))
BEGIN
    CREATE TABLE bc.BankBranches (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BankId INT NOT NULL CONSTRAINT FK_bc_BankBranches_Bank REFERENCES bc.Banks(Id),
        BranchCode NVARCHAR(20) NOT NULL,
        BranchName NVARCHAR(150) NOT NULL,
        RoutingNumber NVARCHAR(50) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_bc_BankBranches_Bank_Branch UNIQUE (BankId, BranchCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'bc.BankAccounts'))
BEGIN
    CREATE TABLE bc.BankAccounts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_bc_BankAccounts_Company REFERENCES sa.Companies(Id),
        BranchId INT NOT NULL CONSTRAINT FK_bc_BankAccounts_Branch REFERENCES bc.BankBranches(Id),
        AccountNumber NVARCHAR(50) NOT NULL,
        AccountTitle NVARCHAR(150) NOT NULL,
        CurrencyCode NVARCHAR(10) NOT NULL DEFAULT 'BDT',
        GlAccountId INT NOT NULL CONSTRAINT FK_bc_BankAccounts_GlAccount REFERENCES gl.Accounts(Id),
        CurrentBalance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_bc_BankAccounts_Company_Account UNIQUE (CompanyId, AccountNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'bc.CashBooks'))
BEGIN
    CREATE TABLE bc.CashBooks (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_bc_CashBooks_Company REFERENCES sa.Companies(Id),
        CashBookCode NVARCHAR(20) NOT NULL,
        CashBookName NVARCHAR(150) NOT NULL,
        GlAccountId INT NOT NULL CONSTRAINT FK_bc_CashBooks_GlAccount REFERENCES gl.Accounts(Id),
        CurrentBalance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_bc_CashBooks_Company_Code UNIQUE (CompanyId, CashBookCode)
    );
END;
GO

-- ==============================================================================
-- 5. ACCOUNTS PAYABLE SCHEMA (ap)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ap.VendorGroups'))
BEGIN
    CREATE TABLE ap.VendorGroups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ap_VendorGroups_Company REFERENCES sa.Companies(Id),
        GroupCode NVARCHAR(20) NOT NULL,
        GroupName NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ap_VendorGroups_Company_Code UNIQUE (CompanyId, GroupCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ap.Vendors'))
BEGIN
    CREATE TABLE ap.Vendors (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ap_Vendors_Company REFERENCES sa.Companies(Id),
        VendorCode NVARCHAR(30) NOT NULL,
        VendorName NVARCHAR(150) NOT NULL,
        VendorGroupId INT NOT NULL CONSTRAINT FK_ap_Vendors_Group REFERENCES ap.VendorGroups(Id),
        ContactPerson NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(100) NULL,
        AddressLine1 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        CountryCode NVARCHAR(10) NOT NULL DEFAULT 'BGD',
        TaxId NVARCHAR(50) NULL,
        GlControlAccountId INT NOT NULL CONSTRAINT FK_ap_Vendors_GlControlAccount REFERENCES gl.Accounts(Id),
        CurrentBalance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CreditLimit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CreditPeriodDays INT NOT NULL DEFAULT 30,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ap_Vendors_Company_Code UNIQUE (CompanyId, VendorCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ap.PurchaseBills'))
BEGIN
    CREATE TABLE ap.PurchaseBills (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ap_PurchaseBills_Company REFERENCES sa.Companies(Id),
        BillNumber NVARCHAR(30) NOT NULL,
        BillDate DATE NOT NULL,
        VendorId INT NOT NULL CONSTRAINT FK_ap_PurchaseBills_Vendor REFERENCES ap.Vendors(Id),
        VendorInvoiceNumber NVARCHAR(50) NULL,
        SubTotal DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TaxAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        PaidAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        BalanceAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Approved, 2=PartiallyPaid, 3=FullyPaid
        DueDate DATE NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ap_PurchaseBills_Company_BillNumber UNIQUE (CompanyId, BillNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ap.PurchaseBillLines'))
BEGIN
    CREATE TABLE ap.PurchaseBillLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BillId INT NOT NULL CONSTRAINT FK_ap_PurchaseBillLines_Bill REFERENCES ap.PurchaseBills(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        Description NVARCHAR(255) NOT NULL,
        Quantity DECIMAL(18, 4) NOT NULL,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineAmount DECIMAL(18, 4) NOT NULL,
        TaxAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        ExpenseAccountId INT NOT NULL CONSTRAINT FK_ap_PurchaseBillLines_ExpenseAccount REFERENCES gl.Accounts(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ap_PurchaseBillLines_Bill_Line UNIQUE (BillId, LineNumber)
    );
END;
GO

-- ==============================================================================
-- 6. ACCOUNTS RECEIVABLE SCHEMA (ar)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ar.CustomerGroups'))
BEGIN
    CREATE TABLE ar.CustomerGroups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ar_CustomerGroups_Company REFERENCES sa.Companies(Id),
        GroupCode NVARCHAR(20) NOT NULL,
        GroupName NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ar_CustomerGroups_Company_Code UNIQUE (CompanyId, GroupCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ar.Customers'))
BEGIN
    CREATE TABLE ar.Customers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ar_Customers_Company REFERENCES sa.Companies(Id),
        CustomerCode NVARCHAR(30) NOT NULL,
        CustomerName NVARCHAR(150) NOT NULL,
        CustomerGroupId INT NOT NULL CONSTRAINT FK_ar_Customers_Group REFERENCES ar.CustomerGroups(Id),
        ContactPerson NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL,
        Email NVARCHAR(100) NULL,
        AddressLine1 NVARCHAR(200) NULL,
        City NVARCHAR(100) NULL,
        CountryCode NVARCHAR(10) NOT NULL DEFAULT 'BGD',
        TaxId NVARCHAR(50) NULL,
        GlControlAccountId INT NOT NULL CONSTRAINT FK_ar_Customers_GlControlAccount REFERENCES gl.Accounts(Id),
        CurrentBalance DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CreditLimit DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CreditPeriodDays INT NOT NULL DEFAULT 30,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ar_Customers_Company_Code UNIQUE (CompanyId, CustomerCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ar.Invoices'))
BEGIN
    CREATE TABLE ar.Invoices (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ar_Invoices_Company REFERENCES sa.Companies(Id),
        InvoiceNumber NVARCHAR(30) NOT NULL,
        InvoiceDate DATE NOT NULL,
        CustomerId INT NOT NULL CONSTRAINT FK_ar_Invoices_Customer REFERENCES ar.Customers(Id),
        SubTotal DECIMAL(18, 4) NOT NULL DEFAULT 0,
        DiscountAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TaxAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        PaidAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        BalanceAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Confirmed, 2=PartiallyPaid, 3=FullyPaid
        DueDate DATE NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ar_Invoices_Company_InvoiceNumber UNIQUE (CompanyId, InvoiceNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ar.InvoiceLines'))
BEGIN
    CREATE TABLE ar.InvoiceLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NOT NULL CONSTRAINT FK_ar_InvoiceLines_Invoice REFERENCES ar.Invoices(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        Description NVARCHAR(255) NOT NULL,
        Quantity DECIMAL(18, 4) NOT NULL,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineAmount DECIMAL(18, 4) NOT NULL,
        TaxAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        RevenueAccountId INT NOT NULL CONSTRAINT FK_ar_InvoiceLines_RevenueAccount REFERENCES gl.Accounts(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ar_InvoiceLines_Invoice_Line UNIQUE (InvoiceId, LineNumber)
    );
END;
GO

-- ==============================================================================
-- 7. INVENTORY CONTROL SCHEMA (inv)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.UnitsOfMeasure'))
BEGIN
    CREATE TABLE inv.UnitsOfMeasure (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_UnitsOfMeasure_Company REFERENCES sa.Companies(Id),
        UomCode NVARCHAR(10) NOT NULL,
        UomName NVARCHAR(50) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_inv_UnitsOfMeasure_Company_Code UNIQUE (CompanyId, UomCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.ProductClasses'))
BEGIN
    CREATE TABLE inv.ProductClasses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_ProductClasses_Company REFERENCES sa.Companies(Id),
        ClassCode NVARCHAR(20) NOT NULL,
        ClassName NVARCHAR(100) NOT NULL,
        CategoryType INT NOT NULL, -- 1=FG, 2=RM, 3=SemiFG, 4=Consumable, 5=Service, 6=Asset
        GlInventoryAccountId INT NOT NULL CONSTRAINT FK_inv_ProductClasses_InvAccount REFERENCES gl.Accounts(Id),
        GlCogsAccountId INT NOT NULL CONSTRAINT FK_inv_ProductClasses_CogsAccount REFERENCES gl.Accounts(Id),
        GlRevenueAccountId INT NOT NULL CONSTRAINT FK_inv_ProductClasses_RevAccount REFERENCES gl.Accounts(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_inv_ProductClasses_Company_Code UNIQUE (CompanyId, ClassCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.Warehouses'))
BEGIN
    CREATE TABLE inv.Warehouses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_Warehouses_Company REFERENCES sa.Companies(Id),
        WarehouseCode NVARCHAR(20) NOT NULL,
        WarehouseName NVARCHAR(100) NOT NULL,
        Location NVARCHAR(200) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_inv_Warehouses_Company_Code UNIQUE (CompanyId, WarehouseCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.Products'))
BEGIN
    CREATE TABLE inv.Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_Products_Company REFERENCES sa.Companies(Id),
        ProductCode NVARCHAR(40) NOT NULL,
        ProductName NVARCHAR(150) NOT NULL,
        ProductClassId INT NOT NULL CONSTRAINT FK_inv_Products_Class REFERENCES inv.ProductClasses(Id),
        PrimaryUomId INT NOT NULL CONSTRAINT FK_inv_Products_Uom REFERENCES inv.UnitsOfMeasure(Id),
        PackSize NVARCHAR(50) NULL,
        StandardCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CurrentCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        SellingPrice DECIMAL(18, 4) NOT NULL DEFAULT 0,
        ReorderLevel DECIMAL(18, 4) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_inv_Products_Company_Code UNIQUE (CompanyId, ProductCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'inv.StockLedgers'))
BEGIN
    CREATE TABLE inv.StockLedgers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_inv_StockLedgers_Company REFERENCES sa.Companies(Id),
        WarehouseId INT NOT NULL CONSTRAINT FK_inv_StockLedgers_Warehouse REFERENCES inv.Warehouses(Id),
        ProductId INT NOT NULL CONSTRAINT FK_inv_StockLedgers_Product REFERENCES inv.Products(Id),
        TransactionDate DATETIME2 NOT NULL,
        TransactionType INT NOT NULL, -- 1=GRN, 2=Issue, 3=TransferIn, 4=TransferOut, 5=Adjustment
        ReferenceNumber NVARCHAR(50) NOT NULL,
        QuantityIn DECIMAL(18, 4) NOT NULL DEFAULT 0,
        QuantityOut DECIMAL(18, 4) NOT NULL DEFAULT 0,
        UnitCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        TotalCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        BalanceQuantity DECIMAL(18, 4) NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL
    );
END;
GO

-- ==============================================================================
-- 8. SALES & ORDER ENTRY SCHEMA (sales)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sales.SalesCenters'))
BEGIN
    CREATE TABLE sales.SalesCenters (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_sales_SalesCenters_Company REFERENCES sa.Companies(Id),
        CenterCode NVARCHAR(20) NOT NULL,
        CenterName NVARCHAR(100) NOT NULL,
        WarehouseId INT NOT NULL CONSTRAINT FK_sales_SalesCenters_Warehouse REFERENCES inv.Warehouses(Id),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sales_SalesCenters_Company_Code UNIQUE (CompanyId, CenterCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sales.SalesOrders'))
BEGIN
    CREATE TABLE sales.SalesOrders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_sales_SalesOrders_Company REFERENCES sa.Companies(Id),
        OrderNumber NVARCHAR(30) NOT NULL,
        OrderDate DATE NOT NULL,
        CustomerId INT NOT NULL CONSTRAINT FK_sales_SalesOrders_Customer REFERENCES ar.Customers(Id),
        SalesCenterId INT NOT NULL CONSTRAINT FK_sales_SalesOrders_Center REFERENCES sales.SalesCenters(Id),
        TotalAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        OrderStatus INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Confirmed, 2=Dispatched, 3=Invoiced, 4=Cancelled
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sales_SalesOrders_Company_OrderNumber UNIQUE (CompanyId, OrderNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sales.SalesOrderLines'))
BEGIN
    CREATE TABLE sales.SalesOrderLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SalesOrderId INT NOT NULL CONSTRAINT FK_sales_SalesOrderLines_Order REFERENCES sales.SalesOrders(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_sales_SalesOrderLines_Product REFERENCES inv.Products(Id),
        OrderedQuantity DECIMAL(18, 4) NOT NULL,
        ShippedQuantity DECIMAL(18, 4) NOT NULL DEFAULT 0,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sales_SalesOrderLines_Order_Line UNIQUE (SalesOrderId, LineNumber)
    );
END;
GO

-- ==============================================================================
-- 9. PROCUREMENT SCHEMA (purch)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'purch.PurchaseOrders'))
BEGIN
    CREATE TABLE purch.PurchaseOrders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_purch_PurchaseOrders_Company REFERENCES sa.Companies(Id),
        PoNumber NVARCHAR(30) NOT NULL,
        PoDate DATE NOT NULL,
        VendorId INT NOT NULL CONSTRAINT FK_purch_PurchaseOrders_Vendor REFERENCES ap.Vendors(Id),
        WarehouseId INT NOT NULL CONSTRAINT FK_purch_PurchaseOrders_Warehouse REFERENCES inv.Warehouses(Id),
        TotalAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Issued, 2=PartiallyReceived, 3=Completed, 4=Cancelled
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_purch_PurchaseOrders_Company_PoNumber UNIQUE (CompanyId, PoNumber)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'purch.PurchaseOrderLines'))
BEGIN
    CREATE TABLE purch.PurchaseOrderLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PurchaseOrderId INT NOT NULL CONSTRAINT FK_purch_PurchaseOrderLines_Order REFERENCES purch.PurchaseOrders(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_purch_PurchaseOrderLines_Product REFERENCES inv.Products(Id),
        OrderedQuantity DECIMAL(18, 4) NOT NULL,
        ReceivedQuantity DECIMAL(18, 4) NOT NULL DEFAULT 0,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_purch_PurchaseOrderLines_Order_Line UNIQUE (PurchaseOrderId, LineNumber)
    );
END;
GO

-- ==============================================================================
-- 10. PAYROLL & HR SCHEMA (hr)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.Departments'))
BEGIN
    CREATE TABLE hr.Departments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_Departments_Company REFERENCES sa.Companies(Id),
        DepartmentCode NVARCHAR(20) NOT NULL,
        DepartmentName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_hr_Departments_Company_Code UNIQUE (CompanyId, DepartmentCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.Designations'))
BEGIN
    CREATE TABLE hr.Designations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_Designations_Company REFERENCES sa.Companies(Id),
        DesignationCode NVARCHAR(20) NOT NULL,
        DesignationTitle NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_hr_Designations_Company_Code UNIQUE (CompanyId, DesignationCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'hr.Employees'))
BEGIN
    CREATE TABLE hr.Employees (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_hr_Employees_Company REFERENCES sa.Companies(Id),
        EmployeeCode NVARCHAR(30) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        DepartmentId INT NOT NULL CONSTRAINT FK_hr_Employees_Department REFERENCES hr.Departments(Id),
        DesignationId INT NOT NULL CONSTRAINT FK_hr_Employees_Designation REFERENCES hr.Designations(Id),
        DateOfBirth DATE NULL,
        JoiningDate DATE NOT NULL,
        Email NVARCHAR(100) NULL,
        Phone NVARCHAR(50) NULL,
        GrossSalary DECIMAL(18, 4) NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_hr_Employees_Company_Code UNIQUE (CompanyId, EmployeeCode)
    );
END;
GO

-- ==============================================================================
-- 11. FIXED ASSETS SCHEMA (fa)
-- ==============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'fa.AssetGroups'))
BEGIN
    CREATE TABLE fa.AssetGroups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_fa_AssetGroups_Company REFERENCES sa.Companies(Id),
        GroupCode NVARCHAR(20) NOT NULL,
        GroupName NVARCHAR(100) NOT NULL,
        DepreciationRate DECIMAL(5, 2) NOT NULL DEFAULT 0,
        GlAssetAccountId INT NOT NULL CONSTRAINT FK_fa_AssetGroups_GlAsset REFERENCES gl.Accounts(Id),
        GlAccumDeprAccountId INT NOT NULL CONSTRAINT FK_fa_AssetGroups_GlAccumDepr REFERENCES gl.Accounts(Id),
        GlDeprExpenseAccountId INT NOT NULL CONSTRAINT FK_fa_AssetGroups_GlDeprExpense REFERENCES gl.Accounts(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_fa_AssetGroups_Company_Code UNIQUE (CompanyId, GroupCode)
    );
END;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'fa.Assets'))
BEGIN
    CREATE TABLE fa.Assets (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_fa_Assets_Company REFERENCES sa.Companies(Id),
        AssetCode NVARCHAR(30) NOT NULL,
        AssetName NVARCHAR(150) NOT NULL,
        AssetGroupId INT NOT NULL CONSTRAINT FK_fa_Assets_Group REFERENCES fa.AssetGroups(Id),
        PurchaseDate DATE NOT NULL,
        PurchaseCost DECIMAL(18, 4) NOT NULL,
        CurrentBookValue DECIMAL(18, 4) NOT NULL,
        AccumulatedDepreciation DECIMAL(18, 4) NOT NULL DEFAULT 0,
        IsDisposed BIT NOT NULL DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_fa_Assets_Company_Code UNIQUE (CompanyId, AssetCode)
    );
END;
GO

-- ==============================================================================
-- 12. PERFORMANCE INDEXES
-- ==============================================================================

CREATE NONCLUSTERED INDEX IX_gl_Accounts_Company_Category ON gl.Accounts (CompanyId, AccountGroupId) INCLUDE (AccountCode, AccountName);
CREATE NONCLUSTERED INDEX IX_gl_JournalVouchers_Posting ON gl.JournalVouchers (CompanyId, FiscalYearId, FiscalPeriodId, IsPosted) INCLUDE (VoucherNumber, VoucherDate);
CREATE NONCLUSTERED INDEX IX_gl_JournalVoucherLines_Account ON gl.JournalVoucherLines (AccountId, VoucherId) INCLUDE (Debit, Credit);
CREATE NONCLUSTERED INDEX IX_inv_StockLedgers_Product_Wh ON inv.StockLedgers (ProductId, WarehouseId, TransactionDate) INCLUDE (QuantityIn, QuantityOut, BalanceQuantity);
CREATE NONCLUSTERED INDEX IX_ar_Invoices_Customer_Date ON ar.Invoices (CustomerId, InvoiceDate, Status) INCLUDE (TotalAmount, BalanceAmount);
CREATE NONCLUSTERED INDEX IX_ap_PurchaseBills_Vendor_Date ON ap.PurchaseBills (VendorId, BillDate, Status) INCLUDE (TotalAmount, BalanceAmount);
GO
