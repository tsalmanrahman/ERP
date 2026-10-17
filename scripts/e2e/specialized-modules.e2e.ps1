# test_specialized_modules_e2e.ps1
# End-to-End Test for Phase 9: Specialized Modules (Fixed Assets fa, Real Estate re/pss, Manufacturing mfg)

param (
    [string]$BaseUrl = "http://localhost:5174",
    [string]$Username = "admin",
    [string]$Password = $env:ERP_E2E_PASSWORD
)
. "$PSScriptRoot/_common.ps1"
$baseUrl = $BaseUrl
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING E2E VERIFICATION FOR PHASE 9: SPECIALIZED MODULES" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Authenticate
Write-Host "`n1. Authenticating as admin..." -ForegroundColor Yellow
$headers = Get-ErpAuthHeaders -BaseUrl $baseUrl -Username $Username -Password $Password
Write-Host "-> Successfully authenticated! Token acquired." -ForegroundColor Green

# ==============================================================================
# 2. FIXED ASSETS VERIFICATION
# ==============================================================================
Write-Host "`n2. Verifying Fixed Assets Module (fa)..." -ForegroundColor Yellow

# Query Asset Groups
$groupsResp = Invoke-RestMethod -Uri "$baseUrl/api/fa/groups" -Method Get -Headers $headers
Write-Host "-> Asset Groups Count: $($groupsResp.data.Count)" -ForegroundColor Gray
foreach ($g in $groupsResp.data) {
    Write-Host "   - $($g.groupCode): $($g.groupName) | Depreciation Rate: $($g.depreciationRate)%" -ForegroundColor DarkGray
}

# Query Seeded Assets
$assetsResp = Invoke-RestMethod -Uri "$baseUrl/api/fa/assets" -Method Get -Headers $headers
Write-Host "-> Registered Assets Count: $($assetsResp.data.Count)" -ForegroundColor Gray
foreach ($a in $assetsResp.data) {
    Write-Host "   - $($a.assetCode): $($a.assetName) | Cost: ৳$($a.purchaseCost.ToString('N2')) | Method: $($a.depreciationMethod) | Net Book: ৳$($a.currentBookValue.ToString('N2'))" -ForegroundColor DarkGray
}

# Run Monthly Depreciation
Write-Host "`n   Running Monthly Depreciation for October 2026..." -ForegroundColor Yellow
$deprRunPayload = @{
    periodDate = "2026-10-31"
} | ConvertTo-Json

$deprResult = Invoke-RestMethod -Uri "$baseUrl/api/fa/depreciation/run" -Method Post -Headers $headers -Body $deprRunPayload -ContentType "application/json"
Write-Host "-> $($deprResult.message)" -ForegroundColor Green
Write-Host "   Processed Assets: $($deprResult.data.totalAssetsProcessed) | Total Depreciation: ৳$($deprResult.data.totalDepreciationAmount.ToString('N2'))" -ForegroundColor White
Write-Host "   GL Journal Voucher: $($deprResult.data.journalVoucherNumber) (ID: $($deprResult.data.journalVoucherId))" -ForegroundColor Cyan

foreach ($entry in $deprResult.data.entries) {
    Write-Host "   - $($entry.assetCode): Depreciation ৳$($entry.depreciationAmount.ToString('N2')) | New Book Value: ৳$($entry.bookValueAfter.ToString('N2'))" -ForegroundColor DarkGray
}

# Query Asset Schedule Report
$schedReport = Invoke-RestMethod -Uri "$baseUrl/api/fa/reports/schedule?asOfDate=2026-10-31" -Method Get -Headers $headers
Write-Host "`n-> Fixed Asset Register Schedule as of $($schedReport.data.asOfDate):" -ForegroundColor Gray
Write-Host "   Total Cost: ৳$($schedReport.data.totalPurchaseCost.ToString('N2')) | Total Accum Depr: ৳$($schedReport.data.totalAccumulatedDepreciation.ToString('N2')) | Net Book Value: ৳$($schedReport.data.totalNetBookValue.ToString('N2'))" -ForegroundColor Cyan

# Dispose an Asset (AST-DESK-01: Cost 120,000, Book value 118,500, Sold for 125,000 with Gain 6,500)
$deskAsset = $assetsResp.data | Where-Object { $_.assetCode -eq "AST-DESK-01" }
Write-Host "`n   Disposing Asset $($deskAsset.assetCode) ($($deskAsset.assetName))..." -ForegroundColor Yellow
$disposePayload = @{
    assetId = $deskAsset.id
    disposalDate = "2026-10-31"
    disposalProceeds = 125000.00
    referenceNumber = "DISP-DESK-OCT2026"
} | ConvertTo-Json

$disposeResult = Invoke-RestMethod -Uri "$baseUrl/api/fa/assets/dispose" -Method Post -Headers $headers -Body $disposePayload -ContentType "application/json"
Write-Host "-> $($disposeResult.message)" -ForegroundColor Green
Write-Host "   Original Cost:    ৳$($disposeResult.data.purchaseCost.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   Accum Depr:       ৳$($disposeResult.data.accumulatedDepreciation.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   Book Value:       ৳$($disposeResult.data.bookValueAtDisposal.ToString('N2'))" -ForegroundColor DarkGray
Write-Host "   Sale Proceeds:    ৳$($disposeResult.data.disposalProceeds.ToString('N2'))" -ForegroundColor White
Write-Host "   Gain on Disposal: ৳$($disposeResult.data.gainLossAmount.ToString('N2')) (IsGain: $($disposeResult.data.isGain))" -ForegroundColor Green
Write-Host "   GL Disposal JV:   $($disposeResult.data.journalVoucherNumber) (ID: $($disposeResult.data.journalVoucherId))" -ForegroundColor Cyan

# ==============================================================================
# 3. REAL ESTATE & PROPERTY SALES VERIFICATION
# ==============================================================================
Write-Host "`n3. Verifying Real Estate & Property Sales Module (re/pss)..." -ForegroundColor Yellow

$projectsResp = Invoke-RestMethod -Uri "$baseUrl/api/re/projects" -Method Get -Headers $headers
$project = $projectsResp.data[0]
Write-Host "-> Project: $($project.projectCode) - $($project.projectName) (Location: $($project.location))" -ForegroundColor Gray

$unitsResp = Invoke-RestMethod -Uri "$baseUrl/api/re/units?projectId=$($project.id)" -Method Get -Headers $headers
Write-Host "-> Property Units Count: $($unitsResp.data.Count)" -ForegroundColor Gray
foreach ($u in $unitsResp.data) {
    Write-Host "   - $($u.unitNumber): $($u.unitType) ($($u.areaSqFt) sqft @ ৳$($u.ratePerSqFt)/sqft = ৳$($u.totalPrice.ToString('N2'))) | Status: $($u.status)" -ForegroundColor DarkGray
}

# Book Apartment APT-101 for Customer 1 (Bengal Trading)
$aptUnit = $unitsResp.data | Where-Object { $_.unitNumber -eq "APT-101" }
Write-Host "`n   Creating Booking for Unit $($aptUnit.unitNumber)..." -ForegroundColor Yellow
$bookingPayload = @{
    projectId = $project.id
    propertyUnitId = $aptUnit.id
    customerId = 1
    bookingDate = "2026-10-31"
    agreedPrice = $aptUnit.totalPrice
    downPayment = 5800000.00
    numberOfInstallments = 10
    installmentsStartDate = "2026-11-01"
} | ConvertTo-Json

$bookingResult = Invoke-RestMethod -Uri "$baseUrl/api/re/bookings" -Method Post -Headers $headers -Body $bookingPayload -ContentType "application/json"
$booking = $bookingResult.data
Write-Host "-> Booking Created: $($booking.bookingNumber)" -ForegroundColor Green
Write-Host "   Agreed Price: ৳$($booking.agreedPrice.ToString('N2')) | Down Payment: ৳$($booking.downPayment.ToString('N2')) | Balance: ৳$($booking.balanceDue.ToString('N2'))" -ForegroundColor White
Write-Host "   Total Installments: $($booking.installments.Count)" -ForegroundColor DarkGray

# Collect Installment #1
$inst1 = $booking.installments[0]
Write-Host "`n   Collecting Installment #$($inst1.installmentNumber) (Due: ৳$($inst1.amountDue.ToString('N2')))..." -ForegroundColor Yellow
$collectPayload = @{
    bookingId = $booking.id
    installmentId = $inst1.id
    paymentDate = "2026-10-31"
    amountPaid = $inst1.amountDue
    referenceNumber = "PAY-INST1-$($booking.bookingNumber)"
} | ConvertTo-Json

$collectResult = Invoke-RestMethod -Uri "$baseUrl/api/re/installments/collect" -Method Post -Headers $headers -Body $collectPayload -ContentType "application/json"
Write-Host "-> $($collectResult.message)" -ForegroundColor Green
Write-Host "   Amount Paid: ৳$($collectResult.data.amountPaid.ToString('N2')) | Remaining Booking Balance: ৳$($collectResult.data.remainingBookingBalance.ToString('N2'))" -ForegroundColor White
Write-Host "   GL Installment JV: $($collectResult.data.journalVoucherNumber) (ID: $($collectResult.data.journalVoucherId))" -ForegroundColor Cyan

# ==============================================================================
# 4. MANUFACTURING & BILL OF MATERIALS VERIFICATION
# ==============================================================================
Write-Host "`n4. Verifying Manufacturing & Bill of Materials Module (mfg/mp)..." -ForegroundColor Yellow

$bomsResp = Invoke-RestMethod -Uri "$baseUrl/api/mfg/boms" -Method Get -Headers $headers
$bom = $bomsResp.data[0]
Write-Host "-> Bill of Materials: $($bom.bomNumber) for Finished Good: $($bom.finishedGoodProductName) ($($bom.finishedGoodProductCode))" -ForegroundColor Gray
Write-Host "   Direct Material Cost per Unit: ৳$($bom.totalDirectMaterialCost.ToString('N2'))" -ForegroundColor Cyan
foreach ($comp in $bom.components) {
    Write-Host "   - Raw Component: $($comp.rawMaterialProductCode) ($($comp.rawMaterialProductName)) | Qty: $($comp.requiredQuantity) @ ৳$($comp.unitCost) | ExtCost: ৳$($comp.extendedCost.ToString('N2'))" -ForegroundColor DarkGray
}

# Create Production Order for 10 Finished Goods (Target: 10 units)
Write-Host "`n   Creating Manufacturing Production Order for 10 Units..." -ForegroundColor Yellow
$orderPayload = @{
    bomId = $bom.id
    targetQuantity = 10.0
    rawMaterialWarehouseId = 2 # WH-PLANT
    finishedGoodWarehouseId = 1 # WH-CENTRAL
    orderDate = "2026-10-31"
} | ConvertTo-Json

$orderResult = Invoke-RestMethod -Uri "$baseUrl/api/mfg/production-orders" -Method Post -Headers $headers -Body $orderPayload -ContentType "application/json"
$prodOrder = $orderResult.data
Write-Host "-> Created Production Order: $($prodOrder.orderNumber)" -ForegroundColor Green
Write-Host "   Target Qty: $($prodOrder.targetQuantity) | Est Material Cost: ৳$($prodOrder.totalMaterialCost.ToString('N2')) | Status: $($prodOrder.status)" -ForegroundColor White

# Complete Production Order
Write-Host "`n   Completing Production Order $($prodOrder.orderNumber)..." -ForegroundColor Yellow
$completePayload = @{
    productionOrderId = $prodOrder.id
    completionDate = "2026-10-31"
    completedQuantity = 10.0
} | ConvertTo-Json

$completeResult = Invoke-RestMethod -Uri "$baseUrl/api/mfg/production-orders/complete" -Method Post -Headers $headers -Body $completePayload -ContentType "application/json"
Write-Host "-> $($completeResult.message)" -ForegroundColor Green
Write-Host "   Produced: $($completeResult.data.completedQuantity) units of $($completeResult.data.finishedGoodProductName)" -ForegroundColor White
Write-Host "   Total Actual Material Cost Capitalized: ৳$($completeResult.data.totalMaterialCost.ToString('N2'))" -ForegroundColor White
Write-Host "   GL Capitalization JV: $($completeResult.data.journalVoucherNumber) (ID: $($completeResult.data.journalVoucherId))" -ForegroundColor Cyan

# ==============================================================================
# 5. GENERAL LEDGER TRIAL BALANCE PARITY CHECK
# ==============================================================================
Write-Host "`n5. Verifying General Ledger Trial Balance Parity..." -ForegroundColor Yellow
$tbResponse = Invoke-RestMethod -Uri "$baseUrl/api/gl/reports/trial-balance?asOfDate=2026-10-31" -Method Get -Headers $headers
$tb = $tbResponse.data

Write-Host "-> Trial Balance Results as of $($tb.asOfDate):" -ForegroundColor Cyan
Write-Host "   Total Debits:  ৳$($tb.totalDebit.ToString('N2'))" -ForegroundColor White
Write-Host "   Total Credits: ৳$($tb.totalCredit.ToString('N2'))" -ForegroundColor White
Write-Host "   Difference:    ৳$($tb.difference.ToString('N4'))" -ForegroundColor White
Write-Host "   Is Balanced:   $($tb.isBalanced)" -ForegroundColor $(if ($tb.isBalanced) { "Green" } else { "Red" })

if ($tb.isBalanced -and $tb.difference -eq 0) {
    Write-Host "`n>>> [SUCCESS] PHASE 9 E2E VERIFICATION COMPLETED WITH ZERO GL VARIANCE! <<<" -ForegroundColor Green
} else {
    Write-Host "`n>>> [FAILURE] TRIAL BALANCE HAS DISCREPANCY! <<<" -ForegroundColor Red
    exit 1
}
