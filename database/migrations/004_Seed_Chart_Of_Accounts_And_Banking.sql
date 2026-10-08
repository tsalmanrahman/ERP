-- ==============================================================================
-- Migration: 004_Seed_Chart_Of_Accounts_And_Banking.sql
-- Project: InteAcc ERP Modernization
-- Description: Seeds the standard Enterprise Chart of Accounts (Assets, Liabilities,
--              Equity, Revenue, Expenses) and Bank/Cash Masters for Company 1.
-- ==============================================================================

USE [ERP];
GO

DECLARE @CompId INT = (SELECT Id FROM sa.Companies WHERE CompanyCode = 'COMP-01');

IF @CompId IS NOT NULL
BEGIN
    -- 1. ACCOUNT GROUPS
    IF NOT EXISTS (SELECT 1 FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '1000')
    BEGIN
        INSERT INTO gl.AccountGroups (CompanyId, GroupCode, GroupName, AccountCategory)
        VALUES 
        (@CompId, '1000', N'Current Assets', 1),
        (@CompId, '1200', N'Fixed Assets', 1),
        (@CompId, '2000', N'Current Liabilities', 2),
        (@CompId, '2200', N'Long Term Liabilities', 2),
        (@CompId, '3000', N'Shareholders Equity', 3),
        (@CompId, '4000', N'Sales Revenue', 4),
        (@CompId, '5100', N'Cost of Goods Sold', 5),
        (@CompId, '5200', N'Operating Expenses', 5);
    END;

    DECLARE @GrpCurrentAssets INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '1000');
    DECLARE @GrpFixedAssets INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '1200');
    DECLARE @GrpCurrentLiab INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '2000');
    DECLARE @GrpEquity INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '3000');
    DECLARE @GrpRevenue INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '4000');
    DECLARE @GrpCogs INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '5100');
    DECLARE @GrpOpex INT = (SELECT Id FROM gl.AccountGroups WHERE CompanyId = @CompId AND GroupCode = '5200');

    -- 2. CHART OF ACCOUNTS (gl.Accounts)
    IF NOT EXISTS (SELECT 1 FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1111')
    BEGIN
        INSERT INTO gl.Accounts (CompanyId, AccountCode, AccountName, AccountGroupId, AccountType, IsReconcilable, IsActive)
        VALUES
        -- Cash & Bank
        (@CompId, '1111', N'Cash in Hand (Main Cash)', @GrpCurrentAssets, 2, 1, 1),
        (@CompId, '1112', N'Petty Cash', @GrpCurrentAssets, 2, 1, 1),
        (@CompId, '1120', N'Prime Bank Ltd. (A/C: 110203001)', @GrpCurrentAssets, 2, 1, 1),
        (@CompId, '1121', N'City Bank Ltd. (A/C: 220304002)', @GrpCurrentAssets, 2, 1, 1),
        -- Receivables & Inventory
        (@CompId, '1130', N'Trade Accounts Receivable Control', @GrpCurrentAssets, 2, 1, 1),
        (@CompId, '1140', N'Finished Goods Inventory Control', @GrpCurrentAssets, 2, 1, 1),
        (@CompId, '1150', N'Advance & Security Deposits', @GrpCurrentAssets, 2, 1, 1),
        -- Fixed Assets
        (@CompId, '1210', N'Plant & Factory Machinery', @GrpFixedAssets, 2, 1, 1),
        (@CompId, '1220', N'Office Furniture & Fixtures', @GrpFixedAssets, 2, 1, 1),
        (@CompId, '1290', N'Accumulated Depreciation - Fixed Assets', @GrpFixedAssets, 2, 1, 1),
        -- Liabilities
        (@CompId, '2110', N'Trade Accounts Payable Control', @GrpCurrentLiab, 2, 1, 1),
        (@CompId, '2120', N'Accrued Salaries & Expenses Payable', @GrpCurrentLiab, 2, 1, 1),
        (@CompId, '2130', N'VAT & Tax Withholding Payable', @GrpCurrentLiab, 2, 1, 1),
        -- Equity
        (@CompId, '3100', N'Paid-Up Share Capital', @GrpEquity, 2, 1, 1),
        (@CompId, '3200', N'Retained Earnings', @GrpEquity, 2, 1, 1),
        -- Revenue
        (@CompId, '4100', N'General Sales Revenue', @GrpRevenue, 2, 1, 1),
        (@CompId, '4200', N'Other Operating Income', @GrpRevenue, 2, 1, 1),
        -- Cost of Goods Sold
        (@CompId, '5110', N'Cost of Goods Sold - Manufacturing', @GrpCogs, 2, 1, 1),
        -- Operating Expenses
        (@CompId, '5210', N'Salaries, Wages & Benefits', @GrpOpex, 2, 1, 1),
        (@CompId, '5220', N'Office & Factory Rent', @GrpOpex, 2, 1, 1),
        (@CompId, '5230', N'Electricity & Utility Charges', @GrpOpex, 2, 1, 1),
        (@CompId, '5240', N'Travelling, Fuel & Conveyance', @GrpOpex, 2, 1, 1),
        (@CompId, '5250', N'Depreciation Expense', @GrpOpex, 2, 1, 1),
        (@CompId, '5260', N'Bank Charges & Financial Fees', @GrpOpex, 2, 1, 1);
    END;

    -- 3. BANK & CASH MASTERS (bc)
    IF NOT EXISTS (SELECT 1 FROM bc.Banks WHERE CompanyId = @CompId AND BankCode = 'PRIME')
    BEGIN
        INSERT INTO bc.Banks (CompanyId, BankCode, BankName)
        VALUES 
        (@CompId, 'PRIME', N'Prime Bank PLC'),
        (@CompId, 'CITY', N'City Bank PLC');

        DECLARE @BankPrime INT = (SELECT Id FROM bc.Banks WHERE CompanyId = @CompId AND BankCode = 'PRIME');
        DECLARE @BankCity INT = (SELECT Id FROM bc.Banks WHERE CompanyId = @CompId AND BankCode = 'CITY');

        INSERT INTO bc.BankBranches (BankId, BranchCode, BranchName, RoutingNumber)
        VALUES 
        (@BankPrime, 'GULSHAN', N'Gulshan Corporate Branch', '020261234'),
        (@BankCity, 'MOTIJHEEL', N'Motijheel Principal Branch', '020265678');

        DECLARE @BranchPrime INT = (SELECT Id FROM bc.BankBranches WHERE BankId = @BankPrime AND BranchCode = 'GULSHAN');
        DECLARE @BranchCity INT = (SELECT Id FROM bc.BankBranches WHERE BankId = @BankCity AND BranchCode = 'MOTIJHEEL');

        DECLARE @AcctPrime INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1120');
        DECLARE @AcctCity INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1121');

        INSERT INTO bc.BankAccounts (CompanyId, BranchId, AccountNumber, AccountTitle, CurrencyCode, GlAccountId, CurrentBalance, IsActive)
        VALUES 
        (@CompId, @BranchPrime, '110203001', N'InteAcc Prime Current Account', 'BDT', @AcctPrime, 0, 1),
        (@CompId, @BranchCity, '220304002', N'InteAcc City Operations Account', 'BDT', @AcctCity, 0, 1);
    END;

    -- 4. CASH BOOKS (bc.CashBooks)
    IF NOT EXISTS (SELECT 1 FROM bc.CashBooks WHERE CompanyId = @CompId AND CashBookCode = 'CB-MAIN')
    BEGIN
        DECLARE @AcctCashMain INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1111');
        DECLARE @AcctPettyCash INT = (SELECT Id FROM gl.Accounts WHERE CompanyId = @CompId AND AccountCode = '1112');

        INSERT INTO bc.CashBooks (CompanyId, CashBookCode, CashBookName, GlAccountId, CurrentBalance, IsActive)
        VALUES 
        (@CompId, 'CB-MAIN', N'Corporate Head Office Main Cash Book', @AcctCashMain, 0, 1),
        (@CompId, 'CB-PETTY', N'Factory Petty Cash Imprest Book', @AcctPettyCash, 0, 1);
    END;
END;
GO
