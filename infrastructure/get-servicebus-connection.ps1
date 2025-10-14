# Get Service Bus Connection String

Write-Host "?? Getting Service Bus connection string..." -ForegroundColor Cyan

$ResourceGroupName = "rg-studentlink-proj"
$ServiceBusNamespace = "studentlink-sb-dev"

# Login check
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Get connection string
Write-Host "`nRetrieving connection string..." -ForegroundColor Yellow
$connectionString = az servicebus namespace authorization-rule keys list `
    --name RootManageSharedAccessKey `
    --namespace-name $ServiceBusNamespace `
    --resource-group $ResourceGroupName `
    --query "primaryConnectionString" -o tsv

if ($connectionString) {
    Write-Host "`n? Connection String Retrieved!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Connection String:" -ForegroundColor Cyan
    Write-Host $connectionString -ForegroundColor White
    Write-Host ""
    Write-Host "?? Copy this and update appsettings.Development.json:" -ForegroundColor Yellow
    Write-Host '  "ServiceBus": {' -ForegroundColor Gray
    Write-Host '    "ConnectionString": "' -NoNewline -ForegroundColor Gray
    Write-Host $connectionString -NoNewline -ForegroundColor White
    Write-Host '"' -ForegroundColor Gray
    Write-Host '  }' -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "? Failed to retrieve connection string" -ForegroundColor Red
    Write-Host "Please check the Service Bus namespace name and resource group" -ForegroundColor Yellow
}

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")