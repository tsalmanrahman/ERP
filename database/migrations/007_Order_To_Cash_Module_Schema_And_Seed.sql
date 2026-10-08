-- ==============================================================================
-- Migration: 007_Order_To_Cash_Module_Schema_And_Seed.sql
-- Module: Order-to-Cash (Sales Order Entry 'oe'/'sales' & Accounts Receivable 'ar')
-- Description: Creates DeliveryChallans, CustomerReceipts, modifies Invoices/InvoiceLines,
--              and seeds CustomerGroups, Customers, and SalesCenters.
-- ==============================================================================

SET NOCOUNT ON;
GO

-- 1. Ensure sales.DeliveryChallans exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sales.DeliveryChallans'))
BEGIN
    CREATE TABLE sales.DeliveryChallans (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_sales_DeliveryChallans_Company REFERENCES sa.Companies(Id),
        ChallanNumber NVARCHAR(30) NOT NULL,
        ChallanDate DATE NOT NULL,
        SalesOrderId INT NOT NULL CONSTRAINT FK_sales_DeliveryChallans_SalesOrder REFERENCES sales.SalesOrders(Id),
        CustomerId INT NOT NULL CONSTRAINT FK_sales_DeliveryChallans_Customer REFERENCES ar.Customers(Id),
        WarehouseId INT NOT NULL CONSTRAINT FK_sales_DeliveryChallans_Warehouse REFERENCES inv.Warehouses(Id),
        TotalCogsAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0, -- 0=Draft, 1=Dispatched, 2=Invoiced, 3=Cancelled
        JournalVoucherId INT NULL CONSTRAINT FK_sales_DeliveryChallans_JournalVoucher REFERENCES gl.JournalVouchers(Id),
        DriverName NVARCHAR(100) NULL,
        VehicleNumber NVARCHAR(50) NULL,
        Remarks NVARCHAR(255) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sales_DeliveryChallans_Company_ChallanNumber UNIQUE (CompanyId, ChallanNumber)
    );
END;
GO

-- 2. Ensure sales.DeliveryChallanLines exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'sales.DeliveryChallanLines'))
BEGIN
    CREATE TABLE sales.DeliveryChallanLines (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DeliveryChallanId INT NOT NULL CONSTRAINT FK_sales_DeliveryChallanLines_Challan REFERENCES sales.DeliveryChallans(Id) ON DELETE CASCADE,
        LineNumber INT NOT NULL,
        ProductId INT NOT NULL CONSTRAINT FK_sales_DeliveryChallanLines_Product REFERENCES inv.Products(Id),
        DispatchedQuantity DECIMAL(18, 4) NOT NULL,
        UnitCost DECIMAL(18, 4) NOT NULL DEFAULT 0,
        LineCogsAmount DECIMAL(18, 4) NOT NULL DEFAULT 0,
        BatchNumber NVARCHAR(50) NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_sales_DeliveryChallanLines_Challan_Line UNIQUE (DeliveryChallanId, LineNumber)
    );
END;
GO

-- 3. Enhance ar.Invoices with SalesOrderId, DeliveryChallanId, JournalVoucherId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ar.Invoices') AND name = 'SalesOrderId')
BEGIN
    ALTER TABLE ar.Invoices ADD SalesOrderId INT NULL CONSTRAINT FK_ar_Invoices_SalesOrder REFERENCES sales.SalesOrders(Id);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ar.Invoices') AND name = 'DeliveryChallanId')
BEGIN
    ALTER TABLE ar.Invoices ADD DeliveryChallanId INT NULL CONSTRAINT FK_ar_Invoices_DeliveryChallan REFERENCES sales.DeliveryChallans(Id);
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ar.Invoices') AND name = 'JournalVoucherId')
BEGIN
    ALTER TABLE ar.Invoices ADD JournalVoucherId INT NULL CONSTRAINT FK_ar_Invoices_JournalVoucher REFERENCES gl.JournalVouchers(Id);
END;
GO

-- 4. Enhance ar.InvoiceLines with ProductId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'ar.InvoiceLines') AND name = 'ProductId')
BEGIN
    ALTER TABLE ar.InvoiceLines ADD ProductId INT NULL CONSTRAINT FK_ar_InvoiceLines_Product REFERENCES inv.Products(Id);
END;
GO

-- 5. Create ar.CustomerReceipts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ar.CustomerReceipts'))
BEGIN
    CREATE TABLE ar.CustomerReceipts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL CONSTRAINT FK_ar_CustomerReceipts_Company REFERENCES sa.Companies(Id),
        ReceiptNumber NVARCHAR(30) NOT NULL,
        ReceiptDate DATE NOT NULL,
        CustomerId INT NOT NULL CONSTRAINT FK_ar_CustomerReceipts_Customer REFERENCES ar.Customers(Id),
        PaymentMethod INT NOT NULL DEFAULT 1, -- 1=BankTransfer, 2=Cheque, 3=Cash
        BankAccountId INT NULL CONSTRAINT FK_ar_CustomerReceipts_BankAccount REFERENCES bc.BankAccounts(Id),
        CashBookId INT NULL CONSTRAINT FK_ar_CustomerReceipts_CashBook REFERENCES bc.CashBooks(Id),
        ChequeNumber NVARCHAR(50) NULL,
        ChequeDate DATE NULL,
        Amount DECIMAL(18, 4) NOT NULL,
        Reference NVARCHAR(100) NULL,
        Remarks NVARCHAR(255) NULL,
        Status INT NOT NULL DEFAULT 1, -- 0=Draft, 1=Posted, 2=Voided
        JournalVoucherId INT NULL CONSTRAINT FK_ar_CustomerReceipts_JournalVoucher REFERENCES gl.JournalVouchers(Id),
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UQ_ar_CustomerReceipts_Company_ReceiptNumber UNIQUE (CompanyId, ReceiptNumber)
    );
END;
GO

-- 6. Create ar.CustomerReceiptAllocations
IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'ar.CustomerReceiptAllocations'))
BEGIN
    CREATE TABLE ar.CustomerReceiptAllocations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ReceiptId INT NOT NULL CONSTRAINT FK_ar_CustomerReceiptAllocations_Receipt REFERENCES ar.CustomerReceipts(Id) ON DELETE CASCADE,
        InvoiceId INT NOT NULL CONSTRAINT FK_ar_CustomerReceiptAllocations_Invoice REFERENCES ar.Invoices(Id),
        AllocatedAmount DECIMAL(18, 4) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(50) NOT NULL DEFAULT 'System',
        ModifiedAtUtc DATETIME2 NULL,
        ModifiedBy NVARCHAR(50) NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,
        DeletedBy NVARCHAR(50) NULL,
        RowVersion ROWVERSION NOT NULL
    );
END;
GO

-- 7. Seed Master Data: Customer Groups, Customers, Sales Centers for Company 1
DECLARE @CompanyId INT = 1;
DECLARE @ArAccountId INT = 5; -- Account 1130 Trade Accounts Receivable Control

-- Seed Customer Groups
IF NOT EXISTS (SELECT 1 FROM ar.CustomerGroups WHERE CompanyId = @CompanyId AND GroupCode = 'CG-CORP')
BEGIN
    INSERT INTO ar.CustomerGroups (CompanyId, GroupCode, GroupName, CreatedBy)
    VALUES (@CompanyId, 'CG-CORP', 'Corporate Enterprise Clients', 'System');
END;

IF NOT EXISTS (SELECT 1 FROM ar.CustomerGroups WHERE CompanyId = @CompanyId AND GroupCode = 'CG-RETAIL')
BEGIN
    INSERT INTO ar.CustomerGroups (CompanyId, GroupCode, GroupName, CreatedBy)
    VALUES (@CompanyId, 'CG-RETAIL', 'Retail Distributors & Dealerships', 'System');
END;

-- Seed Customers
DECLARE @CorpGroupId INT = (SELECT Id FROM ar.CustomerGroups WHERE CompanyId = @CompanyId AND GroupCode = 'CG-CORP');
DECLARE @RetailGroupId INT = (SELECT Id FROM ar.CustomerGroups WHERE CompanyId = @CompanyId AND GroupCode = 'CG-RETAIL');

IF NOT EXISTS (SELECT 1 FROM ar.Customers WHERE CompanyId = @CompanyId AND CustomerCode = 'CUST-DHAKA-01')
BEGIN
    INSERT INTO ar.Customers (
        CompanyId, CustomerCode, CustomerName, CustomerGroupId, ContactPerson,
        Phone, Email, AddressLine1, City, CountryCode, TaxId,
        GlControlAccountId, CurrentBalance, CreditLimit, CreditPeriodDays, IsActive, CreatedBy
    )
    VALUES (
        @CompanyId, 'CUST-DHAKA-01', 'Apex Technologies Ltd.', @CorpGroupId, 'Md. Tanvir Ahmed',
        '+8801711223344', 'billing@apextech.bd', 'Plot 45, Gulshan-2 Commercial Area', 'Dhaka', 'BGD', 'TAX-APEX-9901',
        @ArAccountId, 0, 500000.00, 30, 1, 'System'
    );
END;

IF NOT EXISTS (SELECT 1 FROM ar.Customers WHERE CompanyId = @CompanyId AND CustomerCode = 'CUST-CTG-02')
BEGIN
    INSERT INTO ar.Customers (
        CompanyId, CustomerCode, CustomerName, CustomerGroupId, ContactPerson,
        Phone, Email, AddressLine1, City, CountryCode, TaxId,
        GlControlAccountId, CurrentBalance, CreditLimit, CreditPeriodDays, IsActive, CreatedBy
    )
    VALUES (
        @CompanyId, 'CUST-CTG-02', 'Bay Harbor Logistics PLC', @CorpGroupId, 'Rezaul Karim',
        '+8801819887766', 'accounts@bayharbor.com.bd', 'Port Access Road, Agrabad C/A', 'Chittagong', 'BGD', 'TAX-BAY-4412',
        @ArAccountId, 0, 300000.00, 45, 1, 'System'
    );
END;

-- Seed Sales Centers
DECLARE @WhCentralId INT = (SELECT Id FROM inv.Warehouses WHERE CompanyId = @CompanyId AND WarehouseCode = 'WH-CENTRAL');
DECLARE @WhRetailId INT = (SELECT Id FROM inv.Warehouses WHERE CompanyId = @CompanyId AND WarehouseCode = 'WH-RETAIL');

IF NOT EXISTS (SELECT 1 FROM sales.SalesCenters WHERE CompanyId = @CompanyId AND CenterCode = 'SC-DHAKA-01')
BEGIN
    INSERT INTO sales.SalesCenters (CompanyId, CenterCode, CenterName, WarehouseId, IsActive, CreatedBy)
    VALUES (@CompanyId, 'SC-DHAKA-01', 'Dhaka Central Sales Hub', @WhCentralId, 1, 'System');
END;

IF NOT EXISTS (SELECT 1 FROM sales.SalesCenters WHERE CompanyId = @CompanyId AND CenterCode = 'SC-RETAIL-01')
BEGIN
    INSERT INTO sales.SalesCenters (CompanyId, CenterCode, CenterName, WarehouseId, IsActive, CreatedBy)
    VALUES (@CompanyId, 'SC-RETAIL-01', 'Gulshan Flagship Showroom Sales Center', @WhRetailId, 1, 'System');
END;
GO
