-- =============================================================================
-- Migration 011: Configure Hatim Group Corporate Entities
-- 1. HSF - HATIM STEEL STRUCTURE LTD (HSF)
-- 2. HSS - HATIM STEEL STRUCTURE LTD
-- 3. HIL - Hatim Industries Ltd
-- 4. HPL - Hatim Polymer Ltd
-- =============================================================================

UPDATE sa.Companies 
SET CompanyCode = 'HSF', 
    CompanyName = 'HATIM STEEL STRUCTURE LTD (HSF)', 
    AddressLine1 = 'Hatim Complex, Dhaka-Chittagong Highway', 
    City = 'Dhaka', 
    BaseCurrency = 'BDT', 
    IsActive = 1 
WHERE Id = 1;

UPDATE sa.Companies 
SET CompanyCode = 'HSS', 
    CompanyName = 'HATIM STEEL STRUCTURE LTD', 
    AddressLine1 = 'Plot 42, Heavy Industrial Area', 
    City = 'Gazipur', 
    BaseCurrency = 'BDT', 
    IsActive = 1 
WHERE Id = 2;

SET IDENTITY_INSERT sa.Companies ON;

IF NOT EXISTS (SELECT 1 FROM sa.Companies WHERE Id = 3)
BEGIN
    INSERT INTO sa.Companies (Id, CompanyCode, CompanyName, AddressLine1, City, CountryCode, BaseCurrency, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (3, 'HIL', 'Hatim Industries Ltd', 'Corporate Headquarters, Tejgaon Industrial Area', 'Dhaka', 'BGD', 'BDT', 1, SYSUTCDATETIME(), 'SYSTEM', 0);
END
ELSE
BEGIN
    UPDATE sa.Companies SET CompanyCode = 'HIL', CompanyName = 'Hatim Industries Ltd', BaseCurrency = 'BDT', IsActive = 1 WHERE Id = 3;
END;

IF NOT EXISTS (SELECT 1 FROM sa.Companies WHERE Id = 4)
BEGIN
    INSERT INTO sa.Companies (Id, CompanyCode, CompanyName, AddressLine1, City, CountryCode, BaseCurrency, IsActive, CreatedAtUtc, CreatedBy, IsDeleted)
    VALUES (4, 'HPL', 'Hatim Polymer Ltd', 'Hatim Polymer Plant, BSCIC Industrial Estate', 'Narayanganj', 'BGD', 'BDT', 1, SYSUTCDATETIME(), 'SYSTEM', 0);
END
ELSE
BEGIN
    UPDATE sa.Companies SET CompanyCode = 'HPL', CompanyName = 'Hatim Polymer Ltd', BaseCurrency = 'BDT', IsActive = 1 WHERE Id = 4;
END;

SET IDENTITY_INSERT sa.Companies OFF;

-- Grant access to default admin (UserId = 1) across all 4 Hatim Group entities
IF NOT EXISTS (SELECT 1 FROM sa.UserCompanyAccess WHERE UserId = 1 AND CompanyId = 1)
    INSERT INTO sa.UserCompanyAccess (UserId, CompanyId) VALUES (1, 1);

IF NOT EXISTS (SELECT 1 FROM sa.UserCompanyAccess WHERE UserId = 1 AND CompanyId = 2)
    INSERT INTO sa.UserCompanyAccess (UserId, CompanyId) VALUES (1, 2);

IF NOT EXISTS (SELECT 1 FROM sa.UserCompanyAccess WHERE UserId = 1 AND CompanyId = 3)
    INSERT INTO sa.UserCompanyAccess (UserId, CompanyId) VALUES (1, 3);

IF NOT EXISTS (SELECT 1 FROM sa.UserCompanyAccess WHERE UserId = 1 AND CompanyId = 4)
    INSERT INTO sa.UserCompanyAccess (UserId, CompanyId) VALUES (1, 4);
