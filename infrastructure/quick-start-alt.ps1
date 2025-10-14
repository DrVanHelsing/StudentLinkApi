<#
.SYNOPSIS
    Quick start deployment script for StudentLink API infrastructure.
.DESCRIPTION
    Deploys the Bicep template into an Azure Resource Group using Azure CLI.
    Works with PowerShell 7+. Handles paths with spaces and avoids JSON stream issues.
#>

param(
    [string]$ResourceGroup = 'rg-studentlink-dev',
    [string]$Location = 'southafricanorth',
    [string]$BaseName = $("studentlink" + (Get-Random -Maximum 9999)),
    [string]$SqlAdminLogin = 'sqladmin',
    [SecureString]$SqlAdminPassword
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host '?? Starting StudentLink infrastructure deployment...' -ForegroundColor Cyan

# Resolve template paths relative to this script
$TemplateFile = Join-Path $PSScriptRoot 'main.bicep'
$ParametersFile = Join-Path $PSScriptRoot 'main.parameters.json'
$DeploymentName = 'studentlink-deploy-' + (Get-Date -Format 'yyyyMMddHHmmss')

# Get SQL password if not provided
if (-not $SqlAdminPassword) {
    Write-Host '?? Enter SQL Admin Password (min 8 chars, mixed case, number, symbol):' -ForegroundColor Yellow
    $SqlAdminPassword = Read-Host -AsSecureString
}

$SqlAdminPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword))

if ($SqlAdminPasswordPlain.Length -lt 8) {
    throw 'SQL password must be at least 8 characters long.'
}

# 1) Azure login check
Write-Host '?? Checking Azure login status...'
try { 
    $account = az account show 2>$null | ConvertFrom-Json 
    Write-Host "   Logged in as: $($account.user.name)" -ForegroundColor Gray
} catch { 
    Write-Host '   Logging in...' -ForegroundColor Gray
    az login | Out-Null 
}

# 2) Resource group
Write-Host "?? Ensuring resource group [$ResourceGroup] in [$Location]..."
if (-not (az group exists --name $ResourceGroup | ConvertFrom-Json)) {
    az group create --name $ResourceGroup --location $Location --only-show-errors --output table
    Write-Host '? Resource group created.'
} else {
    Write-Host '? Resource group exists.'
}

# 3) Try to use Bicep directly (Azure CLI handles compilation automatically)
Write-Host '??? Preparing template for deployment...'
$DeploymentTemplate = $TemplateFile

# Verify template and parameters files exist
if (-not (Test-Path $TemplateFile)) { throw "Template file not found: $TemplateFile" }
if (-not (Test-Path $ParametersFile)) { throw "Parameters file not found: $ParametersFile" }

Write-Host "   Template: $TemplateFile" -ForegroundColor Gray
Write-Host "   Parameters: $ParametersFile" -ForegroundColor Gray

# 4) Deploy template
Write-Host "?? Deploying template to [$ResourceGroup] (deployment: $DeploymentName)..."
Write-Host "   This may take 5-10 minutes..." -ForegroundColor Gray

az deployment group create `
    --name $DeploymentName `
    --resource-group $ResourceGroup `
    --template-file "$DeploymentTemplate" `
    --parameters "@$ParametersFile" `
    --parameters baseName=$BaseName `
    --parameters environment=dev `
    --parameters location=$Location `
    --parameters sqlAdminLogin=$SqlAdminLogin `
    --parameters sqlAdminPassword="$SqlAdminPasswordPlain" `
    --only-show-errors `
    --output none

if ($LASTEXITCODE -ne 0) {
    throw 'Deployment failed. Check the error messages above.'
}

# 5) Fetch status and outputs separately to avoid stream issues
Write-Host '?? Retrieving deployment results...'
$tempOut = Join-Path $env:TEMP ("deploy-outputs-" + [Guid]::NewGuid().ToString() + ".json")

az deployment group show `
    --resource-group $ResourceGroup `
    --name $DeploymentName `
    --query '{status:properties.provisioningState, outputs:properties.outputs}' `
    --output json > $tempOut

if (Test-Path $tempOut) {
    $deploymentResult = Get-Content $tempOut -Raw | ConvertFrom-Json
    Write-Host ("? Deployment complete! Status: {0}" -f $deploymentResult.status) -ForegroundColor Green
    
    # Extract key outputs for user
    if ($deploymentResult.outputs) {
        $outputs = $deploymentResult.outputs
        Write-Host "`n?? Key Resources Created:" -ForegroundColor Yellow
        
        if ($outputs.keyVaultName) { Write-Host "   Key Vault: $($outputs.keyVaultName.value)" -ForegroundColor White }
        if ($outputs.sqlServerName) { Write-Host "   SQL Server: $($outputs.sqlServerName.value)" -ForegroundColor White }
        if ($outputs.identityApiName) { Write-Host "   App Service: $($outputs.identityApiName.value)" -ForegroundColor White }
        if ($outputs.storageBlobEndpoint) { Write-Host "   Storage: $($outputs.storageBlobEndpoint.value)" -ForegroundColor White }
    }
} else {
    Write-Host '?? Could not retrieve deployment outputs.' -ForegroundColor Yellow
}

# 6) List resources
Write-Host "`n?? All resources in [$ResourceGroup]:" -ForegroundColor Cyan
az resource list --resource-group $ResourceGroup --output table

Write-Host "`n? Infrastructure deployment complete!" -ForegroundColor Green
Write-Host "   Base name: $BaseName" -ForegroundColor White
Write-Host "   Resource group: $ResourceGroup" -ForegroundColor White
Write-Host "   Location: $Location" -ForegroundColor White

Write-Host "`n?? Next steps:" -ForegroundColor Yellow
Write-Host "   1. Set up Azure AD B2C: .\setup-b2c.ps1" -ForegroundColor White
Write-Host "   2. Update appsettings.json with B2C configuration" -ForegroundColor White
Write-Host "   3. Test locally: cd ..\StudentLinkApi; dotnet run" -ForegroundColor White

# Cleanup
Remove-Item -Path $tempOut -ErrorAction SilentlyContinue
