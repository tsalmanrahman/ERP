-- ==============================================================================
-- ERP Database Operational Procedures: Backup, Integrity & Disaster Recovery
-- Target Database: [ERP] on Microsoft SQL Server 2025 / 2022
-- ==============================================================================

USE [master];
GO

-- ------------------------------------------------------------------------------
-- 1. DATABASE INTEGRITY CHECK (DBCC CHECKDB)
-- ------------------------------------------------------------------------------
PRINT 'Checking physical and logical integrity of database [ERP]...';
DBCC CHECKDB (N'ERP') WITH NO_INFOMSGS, ALL_ERRORMSGS, DATA_PURITY;
GO

-- ------------------------------------------------------------------------------
-- 2. FULL DATABASE BACKUP (DAILY AT 00:00 UTC)
-- ------------------------------------------------------------------------------
DECLARE @BackupDate NVARCHAR(50) = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(19), GETUTCDATE(), 120), '-', ''), ':', ''), ' ', '_');
DECLARE @FullBackupPath NVARCHAR(500) = N'/var/opt/mssql/data/Backup_ERP_Full_' + @BackupDate + N'.bak';

PRINT 'Initiating FULL database backup to: ' + @FullBackupPath;

BACKUP DATABASE [ERP]
TO DISK = @FullBackupPath
WITH 
    FORMAT,
    INIT,
    NAME = N'ERP-Full Database Backup',
    SKIP,
    NOREWIND,
    NOUNLOAD,
    COMPRESSION,
    CHECKSUM,
    STATS = 10;
GO

-- ------------------------------------------------------------------------------
-- 3. DIFFERENTIAL DATABASE BACKUP (EVERY 6 HOURS)
-- ------------------------------------------------------------------------------
DECLARE @BackupDateDiff NVARCHAR(50) = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(19), GETUTCDATE(), 120), '-', ''), ':', ''), ' ', '_');
DECLARE @DiffBackupPath NVARCHAR(500) = N'/var/opt/mssql/data/Backup_ERP_Diff_' + @BackupDateDiff + N'.bak';

PRINT 'Initiating DIFFERENTIAL database backup to: ' + @DiffBackupPath;

BACKUP DATABASE [ERP]
TO DISK = @DiffBackupPath
WITH 
    DIFFERENTIAL,
    FORMAT,
    INIT,
    NAME = N'ERP-Differential Database Backup',
    SKIP,
    NOREWIND,
    NOUNLOAD,
    COMPRESSION,
    CHECKSUM,
    STATS = 25;
GO

-- ------------------------------------------------------------------------------
-- 4. TRANSACTION LOG BACKUP (EVERY 15 MINUTES - POINT-IN-TIME RECOVERY)
-- ------------------------------------------------------------------------------
DECLARE @BackupDateLog NVARCHAR(50) = REPLACE(REPLACE(REPLACE(CONVERT(NVARCHAR(19), GETUTCDATE(), 120), '-', ''), ':', ''), ' ', '_');
DECLARE @LogBackupPath NVARCHAR(500) = N'/var/opt/mssql/data/Backup_ERP_Log_' + @BackupDateLog + N'.trn';

PRINT 'Initiating TRANSACTION LOG backup to: ' + @LogBackupPath;

BACKUP LOG [ERP]
TO DISK = @LogBackupPath
WITH 
    FORMAT,
    INIT,
    NAME = N'ERP-Transaction Log Backup',
    SKIP,
    NOREWIND,
    NOUNLOAD,
    COMPRESSION,
    CHECKSUM,
    STATS = 25;
GO

-- ------------------------------------------------------------------------------
-- 5. DISASTER RECOVERY RESTORE PROCEDURE (EXAMPLE POINT-IN-TIME RESTORE)
-- ------------------------------------------------------------------------------
/*
-- Step 5a: Restore Full Backup (with NORECOVERY)
RESTORE DATABASE [ERP_Restored]
FROM DISK = N'/var/opt/mssql/data/Backup_ERP_Full_20261013_000000.bak'
WITH 
    MOVE N'ERP' TO N'/var/opt/mssql/data/ERP_Restored.mdf',
    MOVE N'ERP_log' TO N'/var/opt/mssql/data/ERP_Restored_log.ldf',
    NORECOVERY,
    REPLACE,
    STATS = 10;

-- Step 5b: Restore Most Recent Differential Backup (with NORECOVERY)
RESTORE DATABASE [ERP_Restored]
FROM DISK = N'/var/opt/mssql/data/Backup_ERP_Diff_20261013_060000.bak'
WITH 
    NORECOVERY,
    STATS = 10;

-- Step 5c: Restore Transaction Log up to Point in Time
RESTORE LOG [ERP_Restored]
FROM DISK = N'/var/opt/mssql/data/Backup_ERP_Log_20261013_061500.trn'
WITH 
    STOPAT = '2026-10-13 06:12:00',
    RECOVERY,
    STATS = 10;
*/
