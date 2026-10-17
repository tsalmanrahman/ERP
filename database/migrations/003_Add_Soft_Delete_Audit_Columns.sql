-- ==============================================================================
-- Migration: 003_Add_Soft_Delete_Audit_Columns.sql
-- Project: ERP Modernization
-- Description: Adds DeletedAtUtc and DeletedBy columns to all tables implementing
--              the ISoftDeletable interface across all domain schemas.
-- ==============================================================================

USE [ERP];
GO

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql += N'
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''DeletedAtUtc'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD DeletedAtUtc DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''') AND name = ''DeletedBy'')
    ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD DeletedBy NVARCHAR(50) NULL;
'
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.columns c ON t.object_id = c.object_id AND c.name = 'IsDeleted'
WHERE s.name IN ('sa', 'gl', 'bc', 'ap', 'ar', 'inv', 'sales', 'purch', 'hr', 'fa');

EXEC sp_executesql @sql;
GO
