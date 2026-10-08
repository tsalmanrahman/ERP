-- ==============================================================================
-- Migration: 006_Procure_To_Pay_Module_Schema_And_Seed.sql
-- Description: Procure-to-Pay Schema enhancements (PO links, Vendor Payments, Allocations)
--              and Vendor Seed Data.
-- Target Server: Microsoft SQL Server 2025 / Database: ERP
-- ==============================================================================

USE ERP;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------------------------
-- 1. ENHANCE ap.PurchaseBills TABLE
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ap.PurchaseBills') AND name = 'JournalVoucherId')
BEGIN
    ALTER TABLE ap.PurchaseBills
    ADD JournalVoucherId INT NULL CONSTRAINT FK_ap_PurchaseBills_JV REFERENCES gl.JournalVouchers(Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ap.PurchaseBills') AND name = 'PoId')
BEGIN
    ALTER TABLE ap.PurchaseBills
    ADD PoId INT NULL CONSTRAINT FK_ap_PurchaseBills_PO REFERENCES purch.PurchaseOrders(Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ap.PurchaseBills') AND name = 'GrnId')
BEGIN
    ALTER TABLE ap.PurchaseBills
    ADD GrnId INT NULL CONSTRAINT FK_ap_PurchaseBills_GRN REFERENCES inv.GoodsReceiptNotes(Id);
END;
GO

-- ------------------------------------------------------------------------------
-- 2. ENHANCE ap.PurchaseBillLines TABLE
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ap.PurchaseBillLines') AND name = 'ProductId')
BEGIN
    ALTER TABLE ap.PurchaseBillLines
    ADD ProductId INT NULL CONSTRAINT FK_ap_PurchaseBillLines_Product REFERENCES inv.Products(Id);
END;
GO

-- ------------------------------------------------------------------------------
-- 3. CREATE VENDOR PAYMENTS TABLE (ap.VendorPayments)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ap.VendorPayments'))
BEGIN
    CREATE TABLE ap.VendorPayments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ap_VendorPayments_Company REFERENCES sa.Companies(Id),
        PaymentNumber NVARCHAR(30) NOT NULL,
        PaymentDate DATE NOT NULL,
        VendorId INT NOT NULL CONSTRAINT FK_ap_VendorPayments_Vendor REFERENCES ap.Vendors(Id),
        PaymentMethod INT NOT NULL, -- 1=BankTransfer, 2=Cheque, 3=Cash
        BankAccountId INT NULL CONSTRAINT FK_ap_VendorPayments_BankAccount REFERENCES bc.BankAccounts(Id),
        CashBookId INT NULL CONSTRAINT FK_ap_VendorPayments_CashBook REFERENCES bc.CashBooks(Id),
        ChequeNumber NVARCHAR(50) NULL,
        ChequeDate DATE NULL,
        Amount DECIMAL(18, 4) NOT NULL,
        Reference NVARCHAR(100) NULL,
        Remarks NVARCHAR(255) NULL,
        Status INT NOT NULL DEFAULT 1, -- 0=Draft, 1=Posted/Cleared, 2=Voided
        JournalVoucherId INT NULL CONSTRAINT FK_ap_VendorPayments_JV REFERENCES gl.JournalVouchers(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        CONSTRAINT UQ_ap_VendorPayments_Company_Number UNIQUE (CompanyId, PaymentNumber)
    );
END;
GO

-- ------------------------------------------------------------------------------
-- 4. CREATE VENDOR PAYMENT ALLOCATIONS (ap.VendorPaymentAllocations)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ap.VendorPaymentAllocations'))
BEGIN
    CREATE TABLE ap.VendorPaymentAllocations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PaymentId INT NOT NULL CONSTRAINT FK_ap_VendorPaymentAllocations_Payment REFERENCES ap.VendorPayments(Id) ON DELETE CASCADE,
        BillId INT NOT NULL CONSTRAINT FK_ap_VendorPaymentAllocations_Bill REFERENCES ap.PurchaseBills(Id),
        AllocatedAmount DECIMAL(18, 4) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        RowVersion ROWVERSION NOT NULL,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL
    );
END;
GO

-- ------------------------------------------------------------------------------
-- 5. SEED INTERNATIONAL VENDOR & GROUPS
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM ap.VendorGroups WHERE GroupCode = 'VG-IMPORT')
BEGIN
    INSERT INTO ap.VendorGroups (CompanyId, GroupCode, GroupName, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (1, 'VG-IMPORT', 'Foreign / Import Component Manufacturers', SYSUTCDATETIME(), 'SeedMigration', 0);
END;

DECLARE @ImportGroupId INT = (SELECT Id FROM ap.VendorGroups WHERE GroupCode = 'VG-IMPORT');
DECLARE @ApAccountId INT = (SELECT Id FROM gl.Accounts WHERE AccountCode = '2110');

IF NOT EXISTS (SELECT 1 FROM ap.Vendors WHERE VendorCode = 'VEND-GLOBAL-01')
BEGIN
    INSERT INTO ap.Vendors (CompanyId, VendorCode, VendorName, VendorGroupId, ContactPerson, Phone, Email, AddressLine1, City, CountryCode, GlControlAccountId, CurrentBalance, CreditLimit, CreditPeriodDays, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (1, 'VEND-GLOBAL-01', 'Global Micro-Components Pte Ltd.', @ImportGroupId, 'David Tan', '+65-6889-1122', 'accounts@globalmicro.sg', '8 Marina View, Asia Square Tower 1', 'Singapore', 'SGP', @ApAccountId, 0, 5000000.00, 60, 1, SYSUTCDATETIME(), 'SeedMigration', 0);
END;

PRINT 'Migration 006 completed successfully: Procure-to-Pay Schema & Seed deployed.';
GO
