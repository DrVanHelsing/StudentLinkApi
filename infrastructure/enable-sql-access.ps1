# Enable Azure SQL Public Network Access

Write-Host "?? Enabling Azure SQL Public Network Access..." -ForegroundColor Cyan
Write-Host ""

$ResourceGroupName = "rg-studentlink-proj"
$SqlServerName = "studentlink-sql-proj"

# Check login
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

Write-Host "Current SQL Server public access status..." -ForegroundColor Yellow
$currentStatus = az sql server show `
    --name $SqlServerName `
    --resource-group $ResourceGroupName `
    --query "publicNetworkAccess" -o tsv

Write-Host "  Current status: $currentStatus" -ForegroundColor Gray

if ($currentStatus -eq "Disabled") {
    Write-Host "`nEnabling public network access..." -ForegroundColor Yellow
    
    az sql server update `
        --name $SqlServerName `
        --resource-group $ResourceGroupName `
        --enable-public-network true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Public network access ENABLED" -ForegroundColor Green
    } else {
        Write-Host "? Failed to enable public access" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "? Public network access already enabled" -ForegroundColor Green
}

# Add your current IP to firewall
Write-Host "`nAdding your IP to SQL Server firewall..." -ForegroundColor Yellow

$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content.Trim()
Write-Host "  Your IP: $myIp" -ForegroundColor Gray

$ruleName = "AllowMyIP-$(Get-Date -Format 'yyyyMMddHHmmss')"

az sql server firewall-rule create `
    --name $ruleName `
    --resource-group $ResourceGroupName `
    --server $SqlServerName `
    --start-ip-address $myIp `
    --end-ip-address $myIp

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Firewall rule added for your IP" -ForegroundColor Green
} else {
    Write-Host "?? Firewall rule might already exist" -ForegroundColor Yellow
}

Write-Host "`n" + ("="*70) -ForegroundColor Cyan
Write-Host "? SQL SERVER CONFIGURATION COMPLETE" -ForegroundColor Green
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run migration: .\add-ai-features.ps1" -ForegroundColor White
Write-Host "  2. Start API: cd ..\StudentLinkApi; dotnet run" -ForegroundColor White
Write-Host ""