# ==============================================================================
# InteAcc ERP Modernization - Phase 10: Cutover, Parity & Go-Live E2E Verification
# ==============================================================================
param (
    [string]$BaseUrl = "http://127.0.0.1:5174"
)

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " INTEACC ERP - PHASE 10: CUTOVER, PARITY & GO-LIVE VERIFICATION" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

# 1. Start Web API process
Write-Host "[1/8] Starting InteAcc Web API host on $BaseUrl..." -ForegroundColor Yellow
$proc = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/InteAcc.Web.Api/InteAcc.Web.Api.csproj --urls `"$BaseUrl`"" -PassThru -WindowStyle Hidden

# 2. Wait for /health endpoint
$maxAttempts = 30
$attempt = 0
$healthy = $false

while ($attempt -lt $maxAttempts -and -not $healthy) {
    Start-Sleep -Milliseconds 1000
    $attempt++
    try {
        $healthResp = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
        if ($healthResp -eq "Healthy") {
            $healthy = $true
        }
    } catch {
        Write-Host "   Waiting for API server... ($attempt/$maxAttempts)" -ForegroundColor DarkGray
    }
}

if (-not $healthy) {
    Write-Host "ERROR: API failed to start within timeout." -ForegroundColor Red
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "API Server is Healthy and Listening!" -ForegroundColor Green

try {
    # 3. Test Static Web Portal /index.html
    Write-Host "[2/8] Testing Interactive Enterprise Operations Web Portal UI..." -ForegroundColor Yellow
    $portalResp = Invoke-WebRequest -Uri "$BaseUrl/index.html" -Method Get -UseBasicParsing
    if ($portalResp.StatusCode -eq 200 -and $portalResp.Content.Contains("InteAcc Modern Enterprise ERP")) {
        Write-Host "   PASS: Modern Web Portal (/index.html) successfully served (HTTP 200 OK)." -ForegroundColor Green
    } else {
        throw "Failed to load /index.html properly."
    }

    # 4. Test Financial Parity Audit Engine
    Write-Host "[3/8] Executing Full-Spectrum Financial Parity Audit..." -ForegroundColor Yellow
    $parity = Invoke-RestMethod -Uri "$BaseUrl/api/audit/financial-parity?companyId=1" -Method Get
    
    Write-Host "   General Ledger Total Debits : ৳$($parity.generalLedgerParity.totalDebits)" -ForegroundColor White
    Write-Host "   General Ledger Total Credits: ৳$($parity.generalLedgerParity.totalCredits)" -ForegroundColor White
    Write-Host "   Trial Balance Variance      : ৳$($parity.generalLedgerParity.variance)" -ForegroundColor White
    Write-Host "   Is GL Balanced              : $($parity.generalLedgerParity.isBalanced)" -ForegroundColor Green

    if (-not $parity.generalLedgerParity.isBalanced) {
        throw "General Ledger Trial Balance has non-zero variance!"
    }

    Write-Host "   Reconciling Operational Subledgers:" -ForegroundColor White
    foreach ($sub in $parity.subledgerReconciliations) {
        Write-Host "     - $($sub.subledgerName): Control = ৳$($sub.controlAccountBalance), Subledger = ৳$($sub.subledgerTotal), Variance = ৳$($sub.variance) [Balanced: $($sub.isBalanced)]" -ForegroundColor Gray
        if (-not $sub.isBalanced) {
            throw "Subledger '$($sub.subledgerName)' has non-zero variance!"
        }
    }

    if (-not $parity.isSystemParityAchieved) {
        throw "System parity audit failed!"
    }
    Write-Host "   PASS: 100% Financial Parity Verified across GL and all 6 Subledgers (Zero Variance)." -ForegroundColor Green

    # 5. Test Automated ETL Migration Engine
    Write-Host "[4/8] Executing Automated Delta ETL Data Migration Run..." -ForegroundColor Yellow
    $etlBody = @{
        companyId = 1
        modulesToMigrate = @("gl", "ar", "ap", "inv", "hr", "fa")
        includeOpeningBalances = $true
    } | ConvertTo-Json

    $etlResult = Invoke-RestMethod -Uri "$BaseUrl/api/migration/run-etl" -Method Post -Body $etlBody -ContentType "application/json"
    Write-Host "   ETL Batch Number   : $($etlResult.batchNumber)" -ForegroundColor White
    Write-Host "   Total Records Extracted: $($etlResult.totalExtracted)" -ForegroundColor White
    Write-Host "   Total Records Migrated : $($etlResult.totalMigrated)" -ForegroundColor White
    Write-Host "   Total Records Failed   : $($etlResult.totalFailed)" -ForegroundColor White
    Write-Host "   Batch Duration (ms)    : $($etlResult.totalDurationMs) ms" -ForegroundColor White

    if (-not $etlResult.isSuccess -or $etlResult.totalFailed -ne 0) {
        throw "ETL migration batch reported failures!"
    }
    Write-Host "   PASS: Delta ETL Data Migration executed with 0 failures." -ForegroundColor Green

    # 6. Test Migration History
    Write-Host "[5/8] Validating ETL Audit Logs in sa.MigrationLogs..." -ForegroundColor Yellow
    $history = Invoke-RestMethod -Uri "$BaseUrl/api/migration/history?companyId=1" -Method Get
    Write-Host "   Found $($history.Count) migration log audit entries." -ForegroundColor White
    if ($history.Count -eq 0) {
        throw "No migration history found!"
    }
    Write-Host "   PASS: sa.MigrationLogs audit trail active." -ForegroundColor Green

    # 7. Test Historical Archive
    Write-Host "[6/8] Querying gl.HistoricalLedgerArchive (Legacy 2015-2025)..." -ForegroundColor Yellow
    $archives = Invoke-RestMethod -Uri "$BaseUrl/api/migration/archives?companyId=1" -Method Get
    Write-Host "   Retrieved $($archives.Count) audited legacy journal archive records." -ForegroundColor White
    if ($archives.Count -eq 0) {
        throw "No historical archive records found!"
    }
    Write-Host "   PASS: Historical ledger archives queryable." -ForegroundColor Green

    # 8. Test Pre-Cutover Readiness Checklist
    Write-Host "[7/8] Evaluating Production Cutover Pre-Flight Checklist..." -ForegroundColor Yellow
    $checklist = Invoke-RestMethod -Uri "$BaseUrl/api/cutover/checklist?companyId=1" -Method Get
    foreach ($item in $checklist.checklistItems) {
        $statusStr = if ($item.isPassed) { "PASS" } else { "FAIL" }
        Write-Host "     [$statusStr] $($item.title)" -ForegroundColor $(if ($item.isPassed) { "Green" } else { "Red" })
    }

    if (-not $checklist.overallCutoverReady) {
        throw "Cutover readiness checklist failed one or more verification gates!"
    }
    Write-Host "   PASS: All Cutover Readiness Gates Passed." -ForegroundColor Green

    # 9. Test Official Cutover Sign-Off
    Write-Host "[8/8] Executing Official CFO Cutover Sign-Off..." -ForegroundColor Yellow
    $signoffBody = @{
        companyId = 1
        signedOffBy = "Chief Financial Officer & Enterprise Lead Architect"
        cfoApprovalCode = "CFO-SIGN-OFF-2026-FINAL"
        notes = "Official certification: Full ERP migration complete with exact mathematical parity across all accounts."
    } | ConvertTo-Json

    $signoffResult = Invoke-RestMethod -Uri "$BaseUrl/api/cutover/signoff" -Method Post -Body $signoffBody -ContentType "application/json"
    Write-Host "   Audit ID         : $($signoffResult.auditId)" -ForegroundColor White
    Write-Host "   Signed Off By    : $($signoffResult.signedOffBy)" -ForegroundColor White
    Write-Host "   CFO Approval Code: $($signoffResult.cfoApprovalCode)" -ForegroundColor White
    Write-Host "   Sign-off Result  : $($signoffResult.message)" -ForegroundColor Green

    if (-not $signoffResult.isSuccess) {
        throw "Cutover sign-off failed!"
    }

    Write-Host "==================================================================" -ForegroundColor Green
    Write-Host " ALL PHASE 10 VERIFICATION TESTS PASSED SUCCESSFULLY! (100% OK)   " -ForegroundColor Green
    Write-Host "==================================================================" -ForegroundColor Green

} finally {
    Write-Host "Stopping Web API process (PID: $($proc.Id))..." -ForegroundColor DarkGray
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}
