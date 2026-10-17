# Shared helpers for the end-to-end verification scripts.
function Get-ErpAuthHeaders {
    param (
        [Parameter(Mandatory)][string]$BaseUrl,
        [Parameter(Mandatory)][string]$Username,
        [string]$Password
    )

    if ([string]::IsNullOrWhiteSpace($Password)) {
        throw "A password is required. Pass -Password or set the ERP_E2E_PASSWORD environment variable."
    }

    $payload = @{ username = $Username; password = $Password } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/sa/auth/login" -Method Post -Body $payload -ContentType "application/json"
    return @{ "Authorization" = "Bearer $($response.data.accessToken)" }
}
