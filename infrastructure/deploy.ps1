# Azure Infrastructure Deployment Script
# Phase 1: Core Infrastructure Setup

# Fail fast on errors
$ErrorActionPreference = 'Stop'

# Configuration
$RESOURCE_GROUP = "rg-studentlink-dev"
$LOCATION = "southafricanorth"
$SQL_ADMIN_PASSWORD = "YourStrongP@ssw0rd123!" # Change this!

Write-Host "?? Starting Azure Infrastructure Deployment for StudentLink Platform" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan

# Check if logged in to Azure
Write-Host "`n?? Step 1: Checking Azure CLI login status..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "? Not logged in to Azure. Please run: az login" -ForegroundColor Red
    exit 1
}
Write-Host "? Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "   Subscription: $($account.name) ($($account.id))" -ForegroundColor Green

# Create Resource Group
Write-Host "`n?? Step 2: Creating Resource Group..." -ForegroundColor Yellow
az group create `
    --name $RESOURCE_GROUP `
    --location $LOCATION `
    --only-show-errors `
    --output table

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Failed to create resource group" -ForegroundColor Red
    exit 1
}
Write-Host "? Resource group created: $RESOURCE_GROUP" -ForegroundColor Green

# Deploy Bicep template
Write-Host "`n?? Step 3: Deploying Azure resources (this may take 5-10 minutes)..." -ForegroundColor Yellow
Write-Host "   - Storage Account (Standard_LRS)" -ForegroundColor Gray
Write-Host "   - Azure SQL Database (Basic tier)" -ForegroundColor Gray
Write-Host "   - Service Bus (Basic tier)" -ForegroundColor Gray
Write-Host "   - Key Vault (Standard tier)" -ForegroundColor Gray
Write-Host "   - Application Insights" -ForegroundColor Gray
Write-Host "   - App Service (Free F1 tier)" -ForegroundColor Gray

# Get the script directory and construct proper paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BicepFile = Join-Path $ScriptDir "main.bicep"
$ParametersFile = Join-Path $ScriptDir "main.parameters.json"

Write-Host "   Using Bicep file: $BicepFile" -ForegroundColor Gray
Write-Host "   Using parameters file: $ParametersFile" -ForegroundColor Gray

# Capture JSON to a temp file to avoid stream consumption issues
$tempJson = Join-Path $env:TEMP ("deploy-" + [Guid]::NewGuid().ToString() + ".json")
$null = Remove-Item -Path $tempJson -ErrorAction SilentlyContinue

$deploymentCmd = @(
    'deployment','group','create',
    '--resource-group', $RESOURCE_GROUP,
    '--template-file', $BicepFile,
    '--parameters', $ParametersFile,
    '--parameters', "sqlAdminPassword=$SQL_ADMIN_PASSWORD",
    '--only-show-errors',
    '--output','json'
)

# Run and redirect output to temp file
$cmdLine = 'az ' + ($deploymentCmd -join ' ')
Write-Host "   Running: $cmdLine" -ForegroundColor DarkGray
$proc = Start-Process -FilePath 'az' -ArgumentList $deploymentCmd -NoNewWindow -PassThru -Wait -RedirectStandardOutput $tempJson -RedirectStandardError "$tempJson.err"

if ($proc.ExitCode -ne 0) {
    Write-Host "? Deployment failed" -ForegroundColor Red
    if (Test-Path "$tempJson.err") {
        Write-Host (Get-Content "$tempJson.err" -Raw) -ForegroundColor Red
    }
    if (Test-Path $tempJson) {
        Write-Host (Get-Content $tempJson -Raw) -ForegroundColor Red
    }
    exit 1
}

# Read JSON safely
try {
    $deploymentOutput = Get-Content $tempJson -Raw | ConvertFrom-Json
} catch {
    Write-Host "? Failed to parse deployment output JSON" -ForegroundColor Red
    Write-Host (Get-Content $tempJson -Raw) -ForegroundColor DarkRed
    throw
}

Write-Host "? Infrastructure deployed successfully!" -ForegroundColor Green

# Extract outputs
$outputs = $deploymentOutput.properties.outputs

Write-Host "`n?? Step 4: Extracting deployment outputs..." -ForegroundColor Yellow
$storageAccountName = $outputs.storageAccountName.value
$sqlServerName = $outputs.sqlServerName.value
$sqlDatabaseName = $outputs.sqlDatabaseName.value
$sqlConnectionString = $outputs.sqlConnectionString.value
$serviceBusConnectionString = $outputs.serviceBusConnectionString.value
$keyVaultName = $outputs.keyVaultName.value
$appInsightsConnectionString = $outputs.appInsightsConnectionString.value
$identityApiName = $outputs.identityApiName.value

Write-Host "? Outputs extracted" -ForegroundColor Green

# Store secrets in Key Vault
Write-Host "`n?? Step 5: Storing secrets in Key Vault..." -ForegroundColor Yellow

# Assign Key Vault Secrets Officer role to current user
$currentUserId = az ad signed-in-user show --query id -o tsv
az role assignment create `
    --role "Key Vault Secrets Officer" `
    --assignee $currentUserId `
    --scope "/subscriptions/$($account.id)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$keyVaultName" `
    --only-show-errors `
    --output none

Start-Sleep -Seconds 10 # Wait for RBAC propagation

az keyvault secret set --vault-name $keyVaultName --name "SqlConnectionString" --value $sqlConnectionString --only-show-errors --output none
az keyvault secret set --vault-name $keyVaultName --name "ServiceBusConnectionString" --value $serviceBusConnectionString --only-show-errors --output none
az keyvault secret set --vault-name $keyVaultName --name "AppInsightsConnectionString" --value $appInsightsConnectionString --only-show-errors --output none
az keyvault secret set --vault-name $keyVaultName --name "StorageAccountName" --value $storageAccountName --only-show-errors --output none

Write-Host "? Secrets stored in Key Vault" -ForegroundColor Green

# Grant App Service access to Key Vault
Write-Host "`n?? Step 6: Configuring App Service managed identity..." -ForegroundColor Yellow
$identityPrincipalId = az webapp identity show --name $identityApiName --resource-group $RESOURCE_GROUP --query principalId -o tsv

az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee $identityPrincipalId `
    --scope "/subscriptions/$($account.id)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$keyVaultName" `
    --only-show-errors `
    --output none

Write-Host "? App Service can now access Key Vault secrets" -ForegroundColor Green

# Summary
Write-Host "`n=====================================================================" -ForegroundColor Cyan
Write-Host "?? DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Resource Group: $RESOURCE_GROUP" -ForegroundColor White
Write-Host "?? Location: $LOCATION" -ForegroundColor White
Write-Host ""
Write-Host "?? Key Resources:" -ForegroundColor Yellow
Write-Host "   Storage Account: $storageAccountName" -ForegroundColor White
Write-Host "   SQL Server: $sqlServerName" -ForegroundColor White
Write-Host "   SQL Database: $sqlDatabaseName" -ForegroundColor White
Write-Host "   Key Vault: $keyVaultName" -ForegroundColor White
Write-Host "   App Service: $identityApiName" -ForegroundColor White
Write-Host ""
Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Update appsettings.json with connection strings" -ForegroundColor White
Write-Host "   2. Run database migrations" -ForegroundColor White
Write-Host "   3. Set up Azure AD B2C tenant" -ForegroundColor White
Write-Host "   4. Deploy your API to App Service" -ForegroundColor White
Write-Host ""
Write-Host "?? Cost Estimate: ~\$5-10/month (Basic tier SQL + Free tier App Service)" -ForegroundColor Green
Write-Host ""
Write-Host "?? Connection strings saved to Key Vault: $keyVaultName" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan

# Save outputs to file
$outputFile = Join-Path $ScriptDir "deployment-outputs.json"
$deploymentOutput | ConvertTo-Json -Depth 10 | Out-File $outputFile
Write-Host "`n?? Full deployment details saved to: $outputFile" -ForegroundColor Cyan
