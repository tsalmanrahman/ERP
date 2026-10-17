-- ==============================================================================
-- Migration: 002_Seed_Initial_Security_Data.sql
-- Project: ERP Modernization
-- Description: Seeds base companies, currencies, business units, fiscal calendar,
--              roles, and initial SuperAdmin user account.
-- ==============================================================================

USE [ERP];
GO

-- 1. Currencies
IF NOT EXISTS (SELECT 1 FROM sa.Currencies WHERE CurrencyCode = 'BDT')
BEGIN
    INSERT INTO sa.Currencies (CurrencyCode, CurrencyName, Symbol, DecimalPlaces, IsActive)
    VALUES 
    ('BDT', N'Bangladeshi Taka', N'৳', 2, 1),
    ('USD', N'US Dollar', N'$', 2, 1),
    ('EUR', N'Euro', N'€', 2, 1);
END;
GO

-- 2. Companies
IF NOT EXISTS (SELECT 1 FROM sa.Companies WHERE CompanyCode = 'COMP-01')
BEGIN
    INSERT INTO sa.Companies (CompanyCode, CompanyName, AddressLine1, City, PostalCode, CountryCode, BaseCurrency, IsActive)
    VALUES 
    ('COMP-01', N'ERP Technologies Ltd.', N'Gulshan Commercial Area', N'Dhaka', N'1212', 'BGD', 'BDT', 1),
    ('COMP-02', N'ERP Global Distribution Inc.', N'Motijheel C/A', N'Dhaka', N'1000', 'BGD', 'USD', 1);
END;
GO

DECLARE @CompId1 INT = (SELECT Id FROM sa.Companies WHERE CompanyCode = 'COMP-01');
DECLARE @CompId2 INT = (SELECT Id FROM sa.Companies WHERE CompanyCode = 'COMP-02');

-- 3. Business Units
IF NOT EXISTS (SELECT 1 FROM sa.BusinessUnits WHERE CompanyId = @CompId1 AND BuCode = 'BU-HQ')
BEGIN
    INSERT INTO sa.BusinessUnits (CompanyId, BuCode, BuName, IsActive)
    VALUES 
    (@CompId1, 'BU-HQ', N'Headquarters & Corporate Office', 1),
    (@CompId1, 'BU-PLANT-01', N'Central Manufacturing Plant', 1);
END;
GO

DECLARE @CompId1 INT = (SELECT Id FROM sa.Companies WHERE CompanyCode = 'COMP-01');

-- 4. Fiscal Calendar (FY2026)
IF NOT EXISTS (SELECT 1 FROM sa.FiscalYears WHERE CompanyId = @CompId1 AND YearCode = 'FY2026')
BEGIN
    INSERT INTO sa.FiscalYears (CompanyId, YearCode, StartDate, EndDate, IsClosed)
    VALUES (@CompId1, 'FY2026', '2026-01-01', '2026-12-31', 0);

    DECLARE @FyId INT = SCOPE_IDENTITY();

    INSERT INTO sa.FiscalPeriods (FiscalYearId, PeriodNumber, PeriodName, StartDate, EndDate, IsClosed)
    VALUES 
    (@FyId, 1, 'Period 01 (Jan 2026)', '2026-01-01', '2026-01-31', 0),
    (@FyId, 2, 'Period 02 (Feb 2026)', '2026-02-01', '2026-02-28', 0),
    (@FyId, 3, 'Period 03 (Mar 2026)', '2026-03-01', '2026-03-31', 0),
    (@FyId, 4, 'Period 04 (Apr 2026)', '2026-04-01', '2026-04-30', 0),
    (@FyId, 5, 'Period 05 (May 2026)', '2026-05-01', '2026-05-31', 0),
    (@FyId, 6, 'Period 06 (Jun 2026)', '2026-06-01', '2026-06-30', 0),
    (@FyId, 7, 'Period 07 (Jul 2026)', '2026-07-01', '2026-07-31', 0),
    (@FyId, 8, 'Period 08 (Aug 2026)', '2026-08-01', '2026-08-31', 0),
    (@FyId, 9, 'Period 09 (Sep 2026)', '2026-09-01', '2026-09-30', 0),
    (@FyId, 10, 'Period 10 (Oct 2026)', '2026-10-01', '2026-10-31', 0),
    (@FyId, 11, 'Period 11 (Nov 2026)', '2026-11-01', '2026-11-30', 0),
    (@FyId, 12, 'Period 12 (Dec 2026)', '2026-12-01', '2026-12-31', 0);
END;
GO

-- 5. Roles
IF NOT EXISTS (SELECT 1 FROM sa.Roles WHERE RoleCode = 'SUPERADMIN')
BEGIN
    INSERT INTO sa.Roles (RoleCode, RoleName, Description, IsSystemRole)
    VALUES 
    ('SUPERADMIN', N'Super Administrator', N'Unrestricted master access across all modules and companies', 1),
    ('FINANCE_MGR', N'Finance Manager', N'Chart of accounts, general ledger posting, and cash management', 0),
    ('SALES_MGR', N'Sales Manager', N'Customer maintenance, price profiles, and sales order processing', 0);
END;
GO

DECLARE @CompId1 INT = (SELECT Id FROM sa.Companies WHERE CompanyCode = 'COMP-01');
DECLARE @CompId2 INT = (SELECT Id FROM sa.Companies WHERE CompanyCode = 'COMP-02');

-- 6. SuperAdmin User (Password: Admin@123)
IF NOT EXISTS (SELECT 1 FROM sa.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO sa.Users (UserCode, Username, Email, PasswordHash, FullName, DefaultCompanyId, IsActive)
    VALUES (
        'USR-001',
        'admin',
        'admin@erp.local',
        'Uf6ZrW2fJhWVPDImTmfV5g==.+6x8u254AvRceiklBbzPdLJhb5qqoglyKvOb1EUckt0=',
        N'System Administrator',
        @CompId1,
        1
    );

    DECLARE @UserId INT = SCOPE_IDENTITY();
    DECLARE @SuperAdminRoleId INT = (SELECT Id FROM sa.Roles WHERE RoleCode = 'SUPERADMIN');

    -- Link SuperAdmin Role
    INSERT INTO sa.UserRoles (UserId, RoleId) VALUES (@UserId, @SuperAdminRoleId);

    -- Grant Company Access
    INSERT INTO sa.UserCompanyAccess (UserId, CompanyId) VALUES (@UserId, @CompId1);
    IF @CompId2 IS NOT NULL
        INSERT INTO sa.UserCompanyAccess (UserId, CompanyId) VALUES (@UserId, @CompId2);
END;
GO
