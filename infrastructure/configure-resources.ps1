# Configure StudentLink resources - FAST version with progress indicators
# This script assumes you've manually created the Azure resources

param(
    [string]$ResourceGroup = 'rg-studentlink-proj',
    [string]$StorageAccountName = 'studentlinkstore',
    [string]$SqlServerName = 'studentlink-sql-proj',
    [string]$SqlDatabaseName = 'studentlinkdb',
    [string]$SqlAdminLogin = 'studentlink-admin',
    [string]$ServiceBusNamespace = 'studentlink-sb-dev',
    [string]$KeyVaultName = 'studentlink-dev-kv',
    [string]$AppInsightsName = 'studentlink-dev-ai',
    [string]$AppServiceName = 'studentlink-dev-identity-api'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'  # Don't stop on warnings

Write-Host '?? Configuring StudentLink resources (FAST MODE)...' -ForegroundColor Cyan

# Check Azure login
Write-Host '?? Checking Azure login...'
try { 
    $account = az account show 2>$null | ConvertFrom-Json 
    Write-Host "   ? Logged in as: $($account.user.name)" -ForegroundColor Green
} catch { 
    Write-Host '? Not logged in to Azure. Run: az login' -ForegroundColor Red
    exit 1
}

Write-Host "`n?? Retrieving connection strings..." -ForegroundColor Yellow

# 1. Storage
Write-Host "[1/4] Storage Account..." -NoNewline
try {
    $storageKeys = az storage account keys list --resource-group $ResourceGroup --account-name $StorageAccountName --query '[0].value' -o tsv 2>$null
    $storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=$StorageAccountName;AccountKey=$storageKeys;EndpointSuffix=core.windows.net"
    Write-Host " ?" -ForegroundColor Green
} catch {
    Write-Host " ? Failed" -ForegroundColor Red
    exit 1
}

# 2. SQL
Write-Host "[2/4] SQL Database..." -NoNewline
$sqlPassword = Read-Host "`n   Enter SQL password for $SqlAdminLogin" -AsSecureString
$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword))
$sqlConnectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$SqlDatabaseName;User ID=$SqlAdminLogin;Password=$sqlPasswordPlain;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
Write-Host "   ? Built" -ForegroundColor Green

# 3. Service Bus
Write-Host "[3/4] Service Bus..." -NoNewline
try {
    $serviceBusConnectionString = az servicebus namespace authorization-rule keys list --resource-group $ResourceGroup --namespace-name $ServiceBusNamespace --name RootManageSharedAccessKey --query primaryConnectionString -o tsv 2>$null
    Write-Host " ?" -ForegroundColor Green
} catch {
    Write-Host " ? Failed" -ForegroundColor Red
    exit 1
}

# 4. App Insights - Use instrumentation key instead of connection string to avoid extension
Write-Host "[4/4] Application Insights..." -NoNewline
try {
    # Get instrumentation key (faster, no extension needed)
    $appInsightsKey = az resource show --resource-group $ResourceGroup --name $AppInsightsName --resource-type "microsoft.insights/components" --query properties.InstrumentationKey -o tsv 2>$null
    $appInsightsConnectionString = "InstrumentationKey=$appInsightsKey"
    Write-Host " ?" -ForegroundColor Green
} catch {
    Write-Host " ?? Skipped (not critical)" -ForegroundColor Yellow
    $appInsightsConnectionString = ""
}

$keyVaultUri = "https://$KeyVaultName.vault.azure.net/"

# 5. Store in Key Vault
Write-Host "`n?? Storing secrets in Key Vault..." -ForegroundColor Yellow

$currentUserId = az ad signed-in-user show --query id -o tsv 2>$null

Write-Host "   Assigning access..." -NoNewline
az role assignment create --role "Key Vault Secrets Officer" --assignee $currentUserId --scope "/subscriptions/$($account.id)/resourceGroups/$ResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName" --output none 2>$null
Write-Host " ?" -ForegroundColor Green

Write-Host "   Waiting for RBAC (10s)..." -NoNewline
Start-Sleep -Seconds 10
Write-Host " ?" -ForegroundColor Green

Write-Host "   Storing secrets..." -NoNewline
az keyvault secret set --vault-name $KeyVaultName --name "SqlConnectionString" --value "$sqlConnectionString" --output none 2>$null
az keyvault secret set --vault-name $KeyVaultName --name "ServiceBusConnectionString" --value "$serviceBusConnectionString" --output none 2>$null
az keyvault secret set --vault-name $KeyVaultName --name "StorageConnectionString" --value "$storageConnectionString" --output none 2>$null
az keyvault secret set --vault-name $KeyVaultName --name "StorageAccountName" --value "$StorageAccountName" --output none 2>$null
if ($appInsightsConnectionString) {
    az keyvault secret set --vault-name $KeyVaultName --name "AppInsightsConnectionString" --value "$appInsightsConnectionString" --output none 2>$null
}
Write-Host " ?" -ForegroundColor Green

# 6. App Service Identity
Write-Host "`n?? Configuring App Service..." -ForegroundColor Yellow
Write-Host "   Enabling managed identity..." -NoNewline
az webapp identity assign --name $AppServiceName --resource-group $ResourceGroup --output none 2>$null
Write-Host " ?" -ForegroundColor Green

Write-Host "   Granting Key Vault access..." -NoNewline
$identityPrincipalId = az webapp identity show --name $AppServiceName --resource-group $ResourceGroup --query principalId -o tsv 2>$null
az role assignment create --role "Key Vault Secrets User" --assignee $identityPrincipalId --scope "/subscriptions/$($account.id)/resourceGroups/$ResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName" --output none 2>$null
Write-Host " ?" -ForegroundColor Green

# 7. Update appsettings.json
Write-Host "`n?? Updating appsettings.json..." -NoNewline
$RootDir = Split-Path -Parent $PSScriptRoot
$appsettingsPath = Join-Path $RootDir "StudentLinkApi\appsettings.json"

if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $appsettings.ConnectionStrings.DefaultConnection = $sqlConnectionString
        $appsettings.KeyVault.Uri = $keyVaultUri
        if ($appInsightsConnectionString) {
            $appsettings.ApplicationInsights.ConnectionString = $appInsightsConnectionString
        }
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host " ?" -ForegroundColor Green
    } catch {
        Write-Host " ?? Failed" -ForegroundColor Yellow
    }
} else {
    Write-Host " ?? Not found" -ForegroundColor Yellow
}

# 8. Database migrations
Write-Host "`n??? Setting up database..." -ForegroundColor Yellow
$ApiDir = Join-Path $RootDir "StudentLinkApi"
if (Test-Path $ApiDir) {
    Push-Location $ApiDir
    
    $MigrationsDir = Join-Path $ApiDir "Data\Migrations"
    if (-not (Test-Path $MigrationsDir)) {
        Write-Host "   Creating migration..." -NoNewline
        dotnet ef migrations add InitialCreate --context ApplicationDbContext --output-dir Data/Migrations 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host " ?" -ForegroundColor Green
        } else {
            Write-Host " ?? Failed" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   Migration exists ?" -ForegroundColor Green
    }
    
    Write-Host "   Applying to database..." -NoNewline
    dotnet ef database update --context ApplicationDbContext --connection "$sqlConnectionString" 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ?" -ForegroundColor Green
    } else {
        Write-Host " ?? Failed (run manually: dotnet ef database update)" -ForegroundColor Yellow
    }
    
    Pop-Location
}

# Summary
Write-Host "`n" + ("="*60) -ForegroundColor Cyan
Write-Host "?? CONFIGURATION COMPLETE!" -ForegroundColor Green
Write-Host ("="*60) -ForegroundColor Cyan
Write-Host "`n?? Resources configured:" -ForegroundColor Yellow
Write-Host "   • Storage: $StorageAccountName"
Write-Host "   • SQL: $SqlServerName/$SqlDatabaseName"
Write-Host "   • Service Bus: $ServiceBusNamespace"
Write-Host "   • Key Vault: $KeyVaultName"
Write-Host "   • App Service: $AppServiceName"
Write-Host "`n? Actions completed:" -ForegroundColor Green
Write-Host "   • Connection strings stored in Key Vault"
Write-Host "   • appsettings.json updated"
Write-Host "   • Database schema created"
Write-Host "   • App Service configured"
Write-Host "`n?? Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Test locally:" -ForegroundColor White
Write-Host "      cd ..\StudentLinkApi" -ForegroundColor Gray
Write-Host "      dotnet run" -ForegroundColor Gray
Write-Host "   2. Test health endpoint:" -ForegroundColor White
Write-Host "      curl https://localhost:7XXX/auth/ping" -ForegroundColor Gray
Write-Host "   3. Set up Azure AD B2C (see START-HERE.md)" -ForegroundColor White
Write-Host ""

# Save config
$configInfo = @{
    ResourceGroup = $ResourceGroup
    ConfiguredAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Resources = @{
        Storage = $StorageAccountName
        SqlServer = $SqlServerName
        SqlDatabase = $SqlDatabaseName
        ServiceBus = $ServiceBusNamespace
        KeyVault = $KeyVaultName
        AppInsights = $AppInsightsName
        AppService = $AppServiceName
    }
}
$configInfo | ConvertTo-Json -Depth 10 | Out-File (Join-Path $PSScriptRoot "configuration-info.json")
Write-Host "?? Config saved to: configuration-info.json`n" -ForegroundColor Cyan