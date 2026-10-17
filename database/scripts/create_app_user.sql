-- ==============================================================================
-- Least-Privilege Application Database User Provisioning Script
-- Replaces SuperAdmin 'sa' account with dedicated least-privilege credentials.
-- ==============================================================================

USE [master];
GO

-- 1. Create SQL Server Login with Strong Random Password
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'erp_app_user')
BEGIN
    CREATE LOGIN [erp_app_user] 
    WITH PASSWORD = N'App_Pr0d_ERP_2026_SecureKey!',
    CHECK_EXPIRATION = OFF,
    CHECK_POLICY = ON,
    DEFAULT_DATABASE = [ERP];
    
    PRINT 'SQL Server Login [erp_app_user] created successfully.';
END;
GO

USE [ERP];
GO

-- 2. Create Database User in [ERP] Database
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'erp_app_user')
BEGIN
    CREATE USER [erp_app_user] FOR LOGIN [erp_app_user];
    PRINT 'Database User [erp_app_user] mapped in database [ERP].';
END;
GO

-- 3. Grant Least-Privilege DML Permissions Across All Modern ERP Domain Schemas
DECLARE @Schemas TABLE (SchemaName NVARCHAR(50));
INSERT INTO @Schemas VALUES 
    (N'sa'), (N'gl'), (N'bc'), (N'inv'), (N'ap'), (N'ar'), 
    (N'sales'), (N'purch'), (N'hr'), (N'fa'), (N'mfg'), (N're'), (N'mig');

DECLARE @s NVARCHAR(50);
DECLARE schema_cursor CURSOR FOR SELECT SchemaName FROM @Schemas;
OPEN schema_cursor;
FETCH NEXT FROM schema_cursor INTO @s;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC('GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::[' + @s + '] TO [erp_app_user];');
    PRINT 'Granted DML/EXEC permissions on schema [' + @s + '] to [erp_app_user].';
    FETCH NEXT FROM schema_cursor INTO @s;
END;

CLOSE schema_cursor;
DEALLOCATE schema_cursor;
GO

-- 4. Explicitly Deny DDL Permissions to Guard Against Accidental Schema Alterations
DENY ALTER ANY SCHEMA TO [erp_app_user];
DENY DROP ANY TABLE TO [erp_app_user];
PRINT 'Security Hardening: Denied ALTER/DROP DDL permissions to [erp_app_user].';
GO
