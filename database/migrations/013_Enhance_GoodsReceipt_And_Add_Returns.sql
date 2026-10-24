-- ============================================================================
-- Migration: 013_Enhance_GoodsReceipt_And_Add_Returns.sql
-- Description: Extends GoodsReceiptNotes and GoodsReceiptNoteLines for dual-stream
--              intake (Vendor GRN vs Production GRN), adds PO Line tracking, and
--              creates GoodsReceiptReturns and GoodsReceiptReturnLines tables.
-- ============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

PRINT 'Starting Migration 013_Enhance_GoodsReceipt_And_Add_Returns...';

-- 1. Enhance GoodsReceiptNotes Table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'GrnType')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD GrnType INT NOT NULL CONSTRAINT DF_GoodsReceiptNotes_GrnType DEFAULT 1; -- 1 = Vendor, 2 = Production
    PRINT 'Added GrnType column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'PurchaseOrderId')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD PurchaseOrderId INT NULL;
    PRINT 'Added PurchaseOrderId column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'ProductionOrderId')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD ProductionOrderId INT NULL;
    PRINT 'Added ProductionOrderId column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'ProductionReference')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD ProductionReference NVARCHAR(100) NULL;
    PRINT 'Added ProductionReference column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'CarrierName')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD CarrierName NVARCHAR(100) NULL;
    PRINT 'Added CarrierName column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'VehicleNumber')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD VehicleNumber NVARCHAR(50) NULL;
    PRINT 'Added VehicleNumber column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'GateEntryNumber')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD GateEntryNumber NVARCHAR(50) NULL;
    PRINT 'Added GateEntryNumber column to GoodsReceiptNotes.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNotes') AND name = 'QcStatus')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNotes ADD QcStatus NVARCHAR(50) NULL CONSTRAINT DF_GoodsReceiptNotes_QcStatus DEFAULT 'Passed';
    PRINT 'Added QcStatus column to GoodsReceiptNotes.';
END;

-- 2. Enhance GoodsReceiptNoteLines Table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNoteLines') AND name = 'PurchaseOrderLineId')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNoteLines ADD PurchaseOrderLineId INT NULL;
    PRINT 'Added PurchaseOrderLineId column to GoodsReceiptNoteLines.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNoteLines') AND name = 'AcceptedQuantity')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNoteLines ADD AcceptedQuantity DECIMAL(18, 4) NOT NULL CONSTRAINT DF_GoodsReceiptNoteLines_AcceptedQuantity DEFAULT 0;
    PRINT 'Added AcceptedQuantity column to GoodsReceiptNoteLines.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNoteLines') AND name = 'RejectedQuantity')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNoteLines ADD RejectedQuantity DECIMAL(18, 4) NOT NULL CONSTRAINT DF_GoodsReceiptNoteLines_RejectedQuantity DEFAULT 0;
    PRINT 'Added RejectedQuantity column to GoodsReceiptNoteLines.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNoteLines') AND name = 'ReturnedQuantity')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNoteLines ADD ReturnedQuantity DECIMAL(18, 4) NOT NULL CONSTRAINT DF_GoodsReceiptNoteLines_ReturnedQuantity DEFAULT 0;
    PRINT 'Added ReturnedQuantity column to GoodsReceiptNoteLines.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GoodsReceiptNoteLines') AND name = 'StorageBinLocation')
BEGIN
    ALTER TABLE dbo.GoodsReceiptNoteLines ADD StorageBinLocation NVARCHAR(50) NULL;
    PRINT 'Added StorageBinLocation column to GoodsReceiptNoteLines.';
END;

-- 3. Create GoodsReceiptReturns Table
IF OBJECT_ID('dbo.GoodsReceiptReturns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoodsReceiptReturns (
        Id INT IDENTITY(1,1) NOT NULL,
        CompanyId INT NOT NULL,
        ReturnNumber NVARCHAR(50) NOT NULL,
        ReturnDate DATETIME2 NOT NULL,
        GoodsReceiptNoteId INT NOT NULL,
        ReturnType INT NOT NULL CONSTRAINT DF_GoodsReceiptReturns_ReturnType DEFAULT 1, -- 1=VendorReturn, 2=ProductionReturn
        WarehouseId INT NOT NULL,
        VendorId INT NULL,
        PurchaseOrderId INT NULL,
        ProductionOrderId INT NULL,
        Reason NVARCHAR(250) NOT NULL,
        TotalAmount DECIMAL(18, 4) NOT NULL CONSTRAINT DF_GoodsReceiptReturns_TotalAmount DEFAULT 0,
        Status INT NOT NULL CONSTRAINT DF_GoodsReceiptReturns_Status DEFAULT 0, -- 0=Draft, 1=Posted, 2=Cancelled
        JournalVoucherId INT NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_GoodsReceiptReturns_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(100) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_GoodsReceiptReturns_IsDeleted DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(100) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_GoodsReceiptReturns PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_GoodsReceiptReturns_GoodsReceiptNotes FOREIGN KEY (GoodsReceiptNoteId) REFERENCES dbo.GoodsReceiptNotes (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_GoodsReceiptReturns_Company_ReturnNumber 
        ON dbo.GoodsReceiptReturns (CompanyId, ReturnNumber) 
        WHERE IsDeleted = 0;

    CREATE NONCLUSTERED INDEX IX_GoodsReceiptReturns_GrnId 
        ON dbo.GoodsReceiptReturns (GoodsReceiptNoteId);

    PRINT 'Created GoodsReceiptReturns table and indices.';
END;

-- 4. Create GoodsReceiptReturnLines Table
IF OBJECT_ID('dbo.GoodsReceiptReturnLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoodsReceiptReturnLines (
        Id INT IDENTITY(1,1) NOT NULL,
        ReturnId INT NOT NULL,
        LineNumber INT NOT NULL,
        GoodsReceiptNoteLineId INT NOT NULL,
        ProductId INT NOT NULL,
        ReturnedQuantity DECIMAL(18, 4) NOT NULL,
        UnitPrice DECIMAL(18, 4) NOT NULL,
        LineTotal DECIMAL(18, 4) NOT NULL,
        ReturnReason NVARCHAR(250) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_GoodsReceiptReturnLines_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(100) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_GoodsReceiptReturnLines_IsDeleted DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(100) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT PK_GoodsReceiptReturnLines PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_GoodsReceiptReturnLines_GoodsReceiptReturns FOREIGN KEY (ReturnId) REFERENCES dbo.GoodsReceiptReturns (Id) ON DELETE CASCADE,
        CONSTRAINT FK_GoodsReceiptReturnLines_GoodsReceiptNoteLines FOREIGN KEY (GoodsReceiptNoteLineId) REFERENCES dbo.GoodsReceiptNoteLines (Id)
    );

    CREATE NONCLUSTERED INDEX IX_GoodsReceiptReturnLines_ReturnId 
        ON dbo.GoodsReceiptReturnLines (ReturnId);

    PRINT 'Created GoodsReceiptReturnLines table and indices.';
END;

PRINT 'Migration 013_Enhance_GoodsReceipt_And_Add_Returns completed successfully.';
