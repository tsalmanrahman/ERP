param (
    [string]$CertPath = "$PSScriptRoot/erp.crt",
    [string]$KeyPath = "$PSScriptRoot/erp.key"
)

$cert = New-SelfSignedCertificate -DnsName "erp.local", "localhost" -CertStoreLocation "cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(2)
$certBase64 = [System.Convert]::ToBase64String($cert.RawData, [System.Base64FormattingOptions]::InsertLineBreaks)
$certPem = "-----BEGIN CERTIFICATE-----`r`n$certBase64`r`n-----END CERTIFICATE-----`r`n"
Set-Content -Path $CertPath -Value $certPem

# Key dummy placeholder or exported key for container configuration
$keyPem = "-----BEGIN PRIVATE KEY-----`r`nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC7Vj2y57`r`n-----END PRIVATE KEY-----`r`n"
Set-Content -Path $KeyPath -Value $keyPem
Write-Host "Dev certificate and key written to $CertPath and $KeyPath." -ForegroundColor Green
