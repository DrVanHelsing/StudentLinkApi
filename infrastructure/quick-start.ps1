# Quick Start Script - StudentLink Platform
# Automates Phase 1 setup

# Fail fast on errors
$ErrorActionPreference = 'Continue' # Allow some checks to fail gracefully

Write-Host "?? StudentLink Platform - Phase 1 Quick Start" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Configuration
$RESOURCE_GROUP = "rg-studentlink-dev"
$LOCATION = "southafricanorth"
$BASE_NAME = "studentlink"
$SQL_ADMIN_LOGIN = "sqladmin"

Write-Host "`n?? Pre-flight Checklist" -ForegroundColor Yellow

# Check Azure CLI
Write-Host "   Checking Azure CLI..." -NoNewline
try {
    $azVersion = az --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ?" -ForegroundColor Green
    } else {
        throw "Azure CLI not found"
    }
} catch {
    Write-Host " ?" -ForegroundColor Red
    Write-Host "   Please install Azure CLI: https://aka.ms/installazurecli" -ForegroundColor Red
    exit 1
}

# Check .NET 9
Write-Host "   Checking .NET 9 SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion -match '^9\.') {
        Write-Host " ? ($dotnetVersion)" -ForegroundColor Green
    } else {
        Write-Host " ?? (Found: $dotnetVersion, Recommended: 9.x)" -ForegroundColor Yellow
    }
} catch {
    Write-Host " ?" -ForegroundColor Red
    Write-Host "   Please install .NET 9: https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# Check Azure login
Write-Host "   Checking Azure login..." -NoNewline
$account = az account show 2>$null | ConvertFrom-Json
if ($account) {
    Write-Host " ?" -ForegroundColor Green
    Write-Host "      Logged in as: $($account.user.name)" -ForegroundColor Gray
    Write-Host "      Subscription: $($account.name)" -ForegroundColor Gray
} else {
    Write-Host " ?" -ForegroundColor Red
    Write-Host "   Please run: az login" -ForegroundColor Red
    exit 1
}

# Get SQL password
Write-Host ""
Write-Host "?? SQL Server Configuration" -ForegroundColor Yellow
Write-Host "   Requirements: Min 8 chars, uppercase, lowercase, number, special char" -ForegroundColor Gray
$sqlPassword = Read-Host "   Enter SQL admin password" -AsSecureString
$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword))

if ($sqlPasswordPlain.Length -lt 8) {
    Write-Host "   ? Password too short (min 8 characters)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "??? Step 1: Creating Azure Resource Group" -ForegroundColor Cyan
az group create --name $RESOURCE_GROUP --location $LOCATION --only-show-errors --output table

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Failed to create resource group" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "??? Step 2: Deploying Azure Infrastructure" -ForegroundColor Cyan
Write-Host "   This will take approximately 5-10 minutes..." -ForegroundColor Gray
Write-Host "   Creating:" -ForegroundColor Gray
Write-Host "   - Azure SQL Database (Basic tier)" -ForegroundColor Gray
Write-Host "   - Storage Account (LRS)" -ForegroundColor Gray
Write-Host "   - Service Bus (Basic tier)" -ForegroundColor Gray
Write-Host "   - Key Vault" -ForegroundColor Gray
Write-Host "   - Application Insights" -ForegroundColor Gray
Write-Host "   - App Service (Free F1)" -ForegroundColor Gray

$BicepFile = Join-Path $ScriptDir "main.bicep"
$ParametersFile = Join-Path $ScriptDir "main.parameters.json"

Write-Host "   Using files from: $ScriptDir" -ForegroundColor Gray

# Use temp file to avoid stream consumption issues
$tempJson = Join-Path $env:TEMP ("deploy-quickstart-" + [Guid]::NewGuid().ToString() + ".json")
$null = Remove-Item -Path $tempJson -ErrorAction SilentlyContinue

# Build properly quoted argument list for paths with spaces
$deploymentArgs = @(
    'deployment','group','create',
    '--resource-group', $RESOURCE_GROUP,
    '--template-file', "`"$BicepFile`"",
    '--parameters', "`"$ParametersFile`"",
    '--parameters', "baseName=$BASE_NAME",
    '--parameters', "environment=dev",
    '--parameters', "location=$LOCATION",
    '--parameters', "sqlAdminLogin=$SQL_ADMIN_LOGIN",
    '--parameters', "sqlAdminPassword=`"$sqlPasswordPlain`"",
    '--only-show-errors',
    '--output','json'
)

Write-Host "   Starting deployment..." -ForegroundColor Gray

# Use ArgumentList as single string to preserve quoting
$argString = $deploymentArgs -join ' '
$proc = Start-Process -FilePath 'az' -ArgumentList $argString -NoNewWindow -PassThru -Wait -RedirectStandardOutput $tempJson -RedirectStandardError "$tempJson.err"

if ($proc.ExitCode -ne 0) {
    Write-Host "? Deployment failed" -ForegroundColor Red
    if (Test-Path "$tempJson.err") {
        $errContent = Get-Content "$tempJson.err" -Raw
        if ($errContent) { Write-Host $errContent -ForegroundColor Red }
    }
    if (Test-Path $tempJson) {
        $outContent = Get-Content $tempJson -Raw
        if ($outContent) { Write-Host $outContent -ForegroundColor Red }
    }
    exit 1
}

# Read deployment output from temp file
try {
    $deploymentOutput = Get-Content $tempJson -Raw | ConvertFrom-Json
} catch {
    Write-Host "? Failed to parse deployment JSON" -ForegroundColor Red
    if (Test-Path $tempJson) {
        Write-Host (Get-Content $tempJson -Raw) -ForegroundColor DarkRed
    }
    throw
}

Write-Host "? Infrastructure deployed successfully!" -ForegroundColor Green

# Extract outputs
$outputs = $deploymentOutput.properties.outputs
$sqlConnectionString = $outputs.sqlConnectionString.value
$keyVaultName = $outputs.keyVaultName.value
$keyVaultUri = $outputs.keyVaultUri.value
$appInsightsConnectionString = $outputs.appInsightsConnectionString.value
$identityApiName = $outputs.identityApiName.value

Write-Host ""
Write-Host "?? Step 3: Configuring Key Vault Access" -ForegroundColor Cyan
$currentUserId = az ad signed-in-user show --query id -o tsv

az role assignment create `
    --role "Key Vault Secrets Officer" `
    --assignee $currentUserId `
    --scope "/subscriptions/$($account.id)/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$keyVaultName" `
    --only-show-errors `
    --output none

Write-Host "   Waiting for RBAC propagation..." -ForegroundColor Gray
Start-Sleep -Seconds 15

Write-Host "   Storing secrets in Key Vault..." -ForegroundColor Gray
az keyvault secret set --vault-name $keyVaultName --name "SqlConnectionString" --value $sqlConnectionString --only-show-errors --output none
az keyvault secret set --vault-name $keyVaultName --name "AppInsightsConnectionString" --value $appInsightsConnectionString --only-show-errors --output none

Write-Host "? Secrets stored in Key Vault" -ForegroundColor Green

Write-Host ""
Write-Host "??? Step 4: Setting Up Database" -ForegroundColor Cyan
Write-Host "   Updating local appsettings.json..." -ForegroundColor Gray

# Update appsettings.json
$appsettingsPath = Join-Path $RootDir "StudentLinkApi\appsettings.json"

if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $appsettings.ConnectionStrings.DefaultConnection = $sqlConnectionString
        $appsettings.KeyVault.Uri = $keyVaultUri
        $appsettings.ApplicationInsights.ConnectionString = $appInsightsConnectionString
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "   ? appsettings.json updated" -ForegroundColor Green
    } catch {
        Write-Host "   ?? Could not update appsettings.json automatically" -ForegroundColor Yellow
        Write-Host "   Please update manually with these values:" -ForegroundColor Yellow
        Write-Host "   ConnectionString: $sqlConnectionString" -ForegroundColor Gray
    }
} else {
    Write-Host "   ?? appsettings.json not found at $appsettingsPath" -ForegroundColor Yellow
}

Write-Host "   Installing EF Core tools..." -ForegroundColor Gray
dotnet tool install --global dotnet-ef --version 9.* 2>$null | Out-Null

$ApiDir = Join-Path $RootDir "StudentLinkApi"
if (Test-Path $ApiDir) {
    Write-Host "   Creating database migration..." -ForegroundColor Gray
    Push-Location $ApiDir
    
    # Check if migrations already exist
    $MigrationsDir = Join-Path $ApiDir "Data\Migrations"
    if (-not (Test-Path $MigrationsDir)) {
        dotnet ef migrations add InitialCreate --context ApplicationDbContext --output-dir Data/Migrations 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ? Migration created" -ForegroundColor Green
        } else {
            Write-Host "   ?? Migration creation skipped (may already exist)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ?? Migrations already exist" -ForegroundColor Gray
    }

    Write-Host "   Applying migration to database..." -ForegroundColor Gray
    dotnet ef database update --context ApplicationDbContext --connection $sqlConnectionString 2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Database created and migrated" -ForegroundColor Green
    } else {
        Write-Host "?? Database migration may have failed - run manually if needed" -ForegroundColor Yellow
    }

    Pop-Location
} else {
    Write-Host "   ?? StudentLinkApi directory not found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "?? PHASE 1 DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Deployed Resources:" -ForegroundColor Yellow
Write-Host "   Resource Group: $RESOURCE_GROUP" -ForegroundColor White
Write-Host "   Location: $LOCATION" -ForegroundColor White
Write-Host "   Key Vault: $keyVaultName" -ForegroundColor White
Write-Host "   App Service: $identityApiName" -ForegroundColor White
Write-Host ""
Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   1. Set up Azure AD B2C:" -ForegroundColor White
Write-Host "      cd infrastructure" -ForegroundColor Gray
Write-Host "      .\setup-b2c.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. Update appsettings.json with B2C configuration" -ForegroundColor White
Write-Host ""
Write-Host "   3. Test locally:" -ForegroundColor White
Write-Host "      cd StudentLinkApi" -ForegroundColor Gray
Write-Host "      dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "   4. Deploy to Azure:" -ForegroundColor White
Write-Host "      dotnet publish -c Release" -ForegroundColor Gray
Write-Host "      az webapp deployment source config-zip ..." -ForegroundColor Gray
Write-Host ""
Write-Host "?? Full Documentation:" -ForegroundColor Yellow
Write-Host "   See PHASE1-DEPLOYMENT.md for detailed instructions" -ForegroundColor White
Write-Host ""
Write-Host "?? Estimated Monthly Cost: ~\$5-10" -ForegroundColor Green
Write-Host ""
Write-Host "?? Happy Coding!" -ForegroundColor Cyan
Write-Host ""

# Save deployment info
$deploymentInfo = @{
    ResourceGroup = $RESOURCE_GROUP
    Location = $LOCATION
    DeploymentTime = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Resources = @{
        KeyVault = $keyVaultName
        KeyVaultUri = $keyVaultUri
        AppService = $identityApiName
        SqlServer = $outputs.sqlServerName.value
        SqlDatabase = $outputs.sqlDatabaseName.value
    }
}

$deploymentInfoFile = Join-Path $ScriptDir "deployment-info.json"
$deploymentInfo | ConvertTo-Json -Depth 10 | Out-File $deploymentInfoFile
Write-Host "?? Deployment info saved to: $deploymentInfoFile" -ForegroundColor Cyan

# Cleanup temp file
Remove-Item -Path $tempJson -ErrorAction SilentlyContinue
Remove-Item -Path "$tempJson.err" -ErrorAction SilentlyContinue
