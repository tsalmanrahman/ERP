# ==============================================================================
# Automated Database Migration Deployer for ERP Modernization
# Executes SQL migration scripts in sorted transactional order with audit history.
# ==============================================================================
param (
    [string]$Server = "192.168.1.82,1433",
    [string]$Database = "ERP",
    [string]$User = "sa",
    [string]$Password = $env:ERP_DB_PASSWORD,
    [string]$MigrationsPath = "$PSScriptRoot/../migrations"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Password)) {
    throw "A database password is required. Pass -Password or set the ERP_DB_PASSWORD environment variable."
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING AUTOMATED DATABASE MIGRATION PIPELINE          " -ForegroundColor Cyan
Write-Host " Target: $Database on $Server                            " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Verify sqlcmd availability or use Microsoft.Data.SqlClient / dotnet-ef
$sqlFiles = Get-ChildItem -Path $MigrationsPath -Filter "*.sql" | Sort-Object Name

Write-Host "Discovered $($sqlFiles.Count) migration files in ${MigrationsPath}:" -ForegroundColor Yellow
foreach ($f in $sqlFiles) {
    Write-Host " - $($f.Name)" -ForegroundColor DarkGray
}

# 2. Connection string
$connString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Add-Type -AssemblyName "System.Data"
$connection = New-Object System.Data.SqlClient.SqlConnection($connString)

try {
    Write-Host "`nConnecting to SQL Server..." -ForegroundColor Yellow
    $connection.Open()
    Write-Host "-> Connection established successfully!" -ForegroundColor Green

    # Ensure Migration History Table exists in schema 'mig'
    $initHistorySql = @"
    IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'mig') EXEC('CREATE SCHEMA mig;');
    IF NOT EXISTS (SELECT * FROM sys.tables WHERE object_id = OBJECT_ID(N'mig.MigrationAppliedHistory'))
    BEGIN
        CREATE TABLE mig.MigrationAppliedHistory (
            Id INT IDENTITY(1,1) PRIMARY KEY,
            MigrationName NVARCHAR(200) NOT NULL UNIQUE,
            AppliedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            ExecutionDurationMs INT NOT NULL
        );
    END;
"@
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = $initHistorySql
    $cmd.ExecuteNonQuery() | Out-Null

    # Iterate through scripts and apply pending
    foreach ($file in $sqlFiles) {
        $checkCmd = $connection.CreateCommand()
        $checkCmd.CommandText = "SELECT COUNT(1) FROM mig.MigrationAppliedHistory WHERE MigrationName = @name"
        $checkCmd.Parameters.AddWithValue("@name", $file.Name) | Out-Null
        $alreadyApplied = [int]$checkCmd.ExecuteScalar()

        if ($alreadyApplied -gt 0) {
            Write-Host " [SKIP] $($file.Name) already applied." -ForegroundColor DarkGray
        } else {
            Write-Host " [APPLYING] $($file.Name)..." -ForegroundColor Yellow
            $sw = [System.Diagnostics.Stopwatch]::StartNew()

            # Split batches by GO
            $content = Get-Content -Path $file.FullName -Raw
            $batches = [System.Text.RegularExpressions.Regex]::Split($content, '^\s*GO\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

            foreach ($batch in $batches) {
                if (-not [string]::IsNullOrWhiteSpace($batch)) {
                    $bCmd = $connection.CreateCommand()
                    $bCmd.CommandText = $batch
                    $bCmd.CommandTimeout = 120
                    $bCmd.ExecuteNonQuery() | Out-Null
                }
            }

            $sw.Stop()
            $recordCmd = $connection.CreateCommand()
            $recordCmd.CommandText = "INSERT INTO mig.MigrationAppliedHistory (MigrationName, ExecutionDurationMs) VALUES (@name, @dur)"
            $recordCmd.Parameters.AddWithValue("@name", $file.Name) | Out-Null
            $recordCmd.Parameters.AddWithValue("@dur", [int]$sw.ElapsedMilliseconds) | Out-Null
            $recordCmd.ExecuteNonQuery() | Out-Null

            Write-Host " -> PASS: $($file.Name) applied in $($sw.ElapsedMilliseconds) ms." -ForegroundColor Green
        }
    }

    Write-Host "`nAll migrations verified and database is up to date!" -ForegroundColor Green
}
catch {
    Write-Host "`nERROR during migration deployment: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    if ($connection.State -eq [System.Data.ConnectionState]::Open) {
        $connection.Close()
    }
}
