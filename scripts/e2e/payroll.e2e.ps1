# test_payroll_e2e.ps1
# End-to-End Test for Phase 8: HRIS, Biometric Attendance Ingestion & Payroll

param (
    [string]$BaseUrl = "http://localhost:5174",
    [string]$Username = "admin",
    [string]$Password = $env:ERP_E2E_PASSWORD
)
. "$PSScriptRoot/_common.ps1"
$baseUrl = $BaseUrl
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING E2E VERIFICATION FOR PHASE 8: HR & PAYROLL      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Authenticate
Write-Host "`n1. Authenticating as admin..." -ForegroundColor Yellow
$headers = Get-ErpAuthHeaders -BaseUrl $baseUrl -Username $Username -Password $Password
Write-Host "-> Successfully authenticated! Token acquired." -ForegroundColor Green

# 2. Verify HR Master Data (Departments, Designations, Employees)
Write-Host "`n2. Verifying HR Master Data..." -ForegroundColor Yellow
$deptsResponse = Invoke-RestMethod -Uri "$baseUrl/api/hr/departments" -Method Get -Headers $headers
Write-Host "-> Departments count: $($deptsResponse.data.Count)" -ForegroundColor Gray
foreach ($d in $deptsResponse.data) {
    Write-Host "   - Dept: $($d.departmentCode) : $($d.departmentName)" -ForegroundColor DarkGray
}

$desigsResponse = Invoke-RestMethod -Uri "$baseUrl/api/hr/designations" -Method Get -Headers $headers
Write-Host "-> Designations count: $($desigsResponse.data.Count)" -ForegroundColor Gray
foreach ($dg in $desigsResponse.data) {
    Write-Host "   - Desig: $($dg.designationCode) : $($dg.designationTitle)" -ForegroundColor DarkGray
}

$empsResponse = Invoke-RestMethod -Uri "$baseUrl/api/hr/employees" -Method Get -Headers $headers
Write-Host "-> Seeded Employees count: $($empsResponse.data.Count)" -ForegroundColor Gray
foreach ($e in $empsResponse.data) {
    Write-Host "   - Employee: $($e.employeeCode) - $($e.fullName) | Gross: $($e.grossSalary) BDT (Basic: $($e.basicSalary)) | Bank: $($e.bankName) ($($e.bankAccountNumber))" -ForegroundColor DarkGray
}

# 3. Biometric Attendance Ingestion
Write-Host "`n3. Ingesting Biometric Attendance Logs (CSV / Tab text format)..." -ForegroundColor Yellow
$rawBiometricLogs = @"
EMP-001,2026-10-01 08:52:10,DEVICE-01,IN
EMP-001,2026-10-01 19:30:00,DEVICE-01,OUT
EMP-002,2026-10-01 09:28:45,DEVICE-01,IN
EMP-002,2026-10-01 18:05:00,DEVICE-01,OUT
EMP-003,2026-10-01 08:58:00,DEVICE-02,IN
EMP-003,2026-10-01 18:00:00,DEVICE-02,OUT
EMP-001,2026-10-02 08:55:00,DEVICE-01,IN
EMP-001,2026-10-02 20:00:00,DEVICE-01,OUT
EMP-002,2026-10-02 09:00:00,DEVICE-01,IN
EMP-002,2026-10-02 18:00:00,DEVICE-01,OUT
EMP-003,2026-10-02 08:50:00,DEVICE-02,IN
EMP-003,2026-10-02 18:00:00,DEVICE-02,OUT
"@

$importPayload = @{
    rawPunchData = $rawBiometricLogs
    deviceId = "DEVICE-01"
} | ConvertTo-Json

$importResponse = Invoke-RestMethod -Uri "$baseUrl/api/hr/attendance/import-biometric" -Method Post -Headers $headers -Body $importPayload -ContentType "application/json"
Write-Host "-> Biometric Import Result: $($importResponse.data.message) (Imported: $($importResponse.data.punchesImported))" -ForegroundColor Green

# Also test single punch registration
$singlePunch = @{
    employeeCode = "EMP-001"
    punchTime = "2026-10-03T08:55:00Z"
    deviceId = "MANUAL-APP"
    inOutMode = "IN"
} | ConvertTo-Json

$punchResp = Invoke-RestMethod -Uri "$baseUrl/api/hr/attendance/punch" -Method Post -Headers $headers -Body $singlePunch -ContentType "application/json"
Write-Host "-> Manual Punch Log: $($punchResp.message)" -ForegroundColor Green

# 4. Attendance Reconciliation
Write-Host "`n4. Reconciling Daily Attendance (Oct 1 to Oct 3)..." -ForegroundColor Yellow
$reconcilePayload = @{
    startDate = "2026-10-01"
    endDate = "2026-10-03"
    gracePeriodMinutes = 15
} | ConvertTo-Json

$reconcileResp = Invoke-RestMethod -Uri "$baseUrl/api/hr/attendance/reconcile" -Method Post -Headers $headers -Body $reconcilePayload -ContentType "application/json"
Write-Host "-> Reconciliation Summary:" -ForegroundColor Green
Write-Host "   Employees: $($reconcileResp.data.totalEmployeesProcessed) | Present: $($reconcileResp.data.totalPresent) | Late: $($reconcileResp.data.totalLate) | Absent: $($reconcileResp.data.totalAbsent)" -ForegroundColor DarkGreen

# Query Daily Attendance
$dailyAtt = Invoke-RestMethod -Uri "$baseUrl/api/hr/attendance/daily?startDate=2026-10-01&endDate=2026-10-03" -Method Get -Headers $headers
Write-Host "-> Reconciled Daily Records count: $($dailyAtt.data.Count)" -ForegroundColor Gray
foreach ($rec in $dailyAtt.data) {
    Write-Host "   $([DateTime]::Parse($rec.attendanceDate).ToString('yyyy-MM-dd')) | $($rec.employeeCode) - $($rec.employeeName) | Status: $($rec.status) | WorkHours: $($rec.workHours)h | Overtime: $($rec.overtimeHours)h" -ForegroundColor DarkGray
}

# 5. Create Payroll Period & Generate Salary Sheet
Write-Host "`n5. Creating Payroll Period & Generating Salary Sheet for October 2026..." -ForegroundColor Yellow
$periodPayload = @{
    periodName = "October 2026"
    year = 2026
    month = 10
    startDate = "2026-10-01"
    endDate = "2026-10-31"
    workingDays = 26
} | ConvertTo-Json

$periodResp = $null
try {
    $periodResp = Invoke-RestMethod -Uri "$baseUrl/api/pr/payroll/periods" -Method Post -Headers $headers -Body $periodPayload -ContentType "application/json"
    $periodId = $periodResp.data.id
    Write-Host "-> Created Period: $($periodResp.data.periodName) (ID: $periodId)" -ForegroundColor Green
} catch {
    # If already created, query existing
    $periods = Invoke-RestMethod -Uri "$baseUrl/api/pr/payroll/periods" -Method Get -Headers $headers
    $periodId = $periods.data[0].id
    Write-Host "-> Using Existing Period: $($periods.data[0].periodName) (ID: $periodId)" -ForegroundColor Green
}

# Generate Monthly Salary Sheet
$genPayload = @{
    payrollPeriodId = $periodId
    processingDate = "2026-10-31"
} | ConvertTo-Json

$sheetResp = Invoke-RestMethod -Uri "$baseUrl/api/pr/payroll/generate" -Method Post -Headers $headers -Body $genPayload -ContentType "application/json"
$sheetId = $sheetResp.data.id
Write-Host "-> Generated Salary Sheet: $($sheetResp.data.sheetNumber) (ID: $sheetId)" -ForegroundColor Green
Write-Host "   Total Gross Salary:    ৳$($sheetResp.data.totalGrossSalary.ToString('N2'))" -ForegroundColor White
Write-Host "   Total Overtime Amount: ৳$($sheetResp.data.totalOvertimeAmount.ToString('N2'))" -ForegroundColor White
Write-Host "   Total Deductions:      ৳$($sheetResp.data.totalDeductions.ToString('N2'))" -ForegroundColor White
Write-Host "   Total Net Salary:      ৳$($sheetResp.data.totalNetSalary.ToString('N2'))" -ForegroundColor Yellow

foreach ($l in $sheetResp.data.lines) {
    Write-Host "   - $($l.employeeCode): $($l.employeeName) ($($l.designationTitle)) | Gross: ৳$($l.grossSalary) | OT: $($l.overtimeHours)h (৳$($l.overtimeAmount)) | AbsentDed: ৳$($l.absentDeduction) | TaxDed: ৳$($l.taxDeduction) | PFDed: ৳$($l.providentFundDeduction) | NetPay: ৳$($l.netSalaryPayable)" -ForegroundColor DarkGray
}

# 6. Approve Salary Sheet -> Automated Double-Entry Accrual GL Voucher
Write-Host "`n6. Approving Salary Sheet & Posting Accrual GL Voucher..." -ForegroundColor Yellow
$approveResp = Invoke-RestMethod -Uri "$baseUrl/api/pr/payroll/sheets/$sheetId/approve" -Method Post -Headers $headers
Write-Host "-> $($approveResp.message)" -ForegroundColor Green
Write-Host "   Accrual JV Number: $($approveResp.data.accrualJournalVoucherNumber) (ID: $($approveResp.data.accrualJournalVoucherId))" -ForegroundColor Cyan

# 7. High-Performance Reporting (Dapper)
Write-Host "`n7. Generating Pay Slip and Bank Advice Statements (Dapper)..." -ForegroundColor Yellow
$emp1 = $empsResponse.data | Where-Object { $_.employeeCode -eq "EMP-001" }
$payslip = Invoke-RestMethod -Uri "$baseUrl/api/pr/reports/payslip/$sheetId/$($emp1.id)" -Method Get -Headers $headers
Write-Host "-> Pay Slip for $($payslip.data.employeeName) ($($payslip.data.employeeCode)):" -ForegroundColor Green
Write-Host "   Designation:    $($payslip.data.designationTitle) | Department: $($payslip.data.departmentName)" -ForegroundColor DarkGray
Write-Host "   Period:         $($payslip.data.periodName) | Working Days: $($payslip.data.workingDays)" -ForegroundColor DarkGray
Write-Host "   Basic Salary:   ৳$($payslip.data.basicSalary.ToString('N2')) | HRA: ৳$($payslip.data.houseRent.ToString('N2')) | Med: ৳$($payslip.data.medicalAllowance.ToString('N2')) | Conv: ৳$($payslip.data.conveyanceAllowance.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   Overtime Hours: $($payslip.data.overtimeHours)h @ ৳$($payslip.data.overtimeRate.ToString('N2'))/hr = ৳$($payslip.data.overtimeAmount.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   Gross Earnings: ৳$($payslip.data.totalGrossEarnings.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   Deductions:     Absent: ৳$($payslip.data.absentDeduction.ToString('N2')) | Tax: ৳$($payslip.data.taxDeduction.ToString('N2')) | PF: ৳$($payslip.data.providentFundDeduction.ToString('N2')) | Total: ৳$($payslip.data.totalDeductions.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   NET PAYABLE:    ৳$($payslip.data.netSalaryPayable.ToString('N2'))" -ForegroundColor Yellow

$bankAdvice = Invoke-RestMethod -Uri "$baseUrl/api/pr/reports/bank-advice/$sheetId" -Method Get -Headers $headers
Write-Host "`n-> Bank Advice Statement for $($bankAdvice.data.periodName):" -ForegroundColor Green
Write-Host "   Total Employees: $($bankAdvice.data.totalEmployees) | Total Advice Amount: ৳$($bankAdvice.data.totalAmount.ToString('N2'))" -ForegroundColor Cyan
foreach ($item in $bankAdvice.data.items) {
    Write-Host "   - $($item.employeeCode): $($item.employeeName) | A/C: $($item.bankAccountNumber) ($($item.bankName)) | Payable: ৳$($item.netSalaryPayable.ToString('N2'))" -ForegroundColor DarkGray
}

# 8. Disburse Salaries via Bank -> Automated Double-Entry Disbursement GL Voucher
Write-Host "`n8. Disbursing Salaries via Prime Bank Ltd. & Posting Disbursement GL Voucher..." -ForegroundColor Yellow
$disbursePayload = @{
    salarySheetId = $sheetId
    disbursementDate = "2026-10-31"
    chequeOrReferenceNumber = "SAL-TXN-OCT2026"
} | ConvertTo-Json

$disburseResp = Invoke-RestMethod -Uri "$baseUrl/api/pr/payroll/sheets/disburse" -Method Post -Headers $headers -Body $disbursePayload -ContentType "application/json"
Write-Host "-> $($disburseResp.message)" -ForegroundColor Green
Write-Host "   Disbursement JV Number: $($disburseResp.data.disbursementJournalVoucherNumber) (ID: $($disburseResp.data.disbursementJournalVoucherId))" -ForegroundColor Cyan

# 9. Verify General Ledger Parity (Trial Balance)
Write-Host "`n9. Verifying General Ledger Trial Balance Parity..." -ForegroundColor Yellow
$tbResponse = Invoke-RestMethod -Uri "$baseUrl/api/gl/reports/trial-balance?asOfDate=2026-10-31" -Method Get -Headers $headers
$tb = $tbResponse.data

Write-Host "-> Trial Balance Results as of $($tb.asOfDate):" -ForegroundColor Cyan
Write-Host "   Total Debits:  ৳$($tb.totalDebit.ToString('N2'))" -ForegroundColor White
Write-Host "   Total Credits: ৳$($tb.totalCredit.ToString('N2'))" -ForegroundColor White
Write-Host "   Difference:    ৳$($tb.difference.ToString('N4'))" -ForegroundColor White
Write-Host "   Is Balanced:   $($tb.isBalanced)" -ForegroundColor $(if ($tb.isBalanced) { "Green" } else { "Red" })

if ($tb.isBalanced -and $tb.difference -eq 0) {
    Write-Host "`n>>> [SUCCESS] PHASE 8 E2E VERIFICATION COMPLETED WITH ZERO GL VARIANCE! <<<" -ForegroundColor Green
} else {
    Write-Host "`n>>> [FAILURE] TRIAL BALANCE HAS DISCREPANCY! <<<" -ForegroundColor Red
    exit 1
}
