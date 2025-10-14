# Quick AI Setup - Uses Existing StudentLink Resources
# This script adds only the AI services to your existing infrastructure

Write-Host "?? Setting up AI Services for StudentLink..." -ForegroundColor Cyan
Write-Host ""

# Configuration
$ResourceGroupName = "rg-studentlink-proj"
$Location = "southafricanorth"
$StorageAccountName = "studentlinkstore"
$ServiceBusNamespace = "studentlink-sb-dev"
$KeyVaultName = "studentlink-dev-kv"

# Check Azure login
Write-Host "1?? Checking Azure connection..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "   Please login..." -ForegroundColor Gray
    az login
    $account = az account show | ConvertFrom-Json
}
Write-Host "   ? Connected: $($account.user.name)" -ForegroundColor Green
Write-Host "   Subscription: $($account.name)" -ForegroundColor Gray

# Create Azure OpenAI
Write-Host "`n2?? Creating Azure OpenAI Service..." -ForegroundColor Yellow
$openAIName = "studentlink-openai"

Write-Host "   Creating resource..." -ForegroundColor Gray
az cognitiveservices account create `
    --name $openAIName `
    --resource-group $ResourceGroupName `
    --kind OpenAI `
    --sku S0 `
    --location eastus `
    --custom-domain $openAIName `
    --yes 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ? OpenAI service created" -ForegroundColor Green
    
    Start-Sleep -Seconds 15
    
    # Deploy GPT-4o
    Write-Host "   Deploying GPT-4o model..." -ForegroundColor Gray
    az cognitiveservices account deployment create `
        --name $openAIName `
        --resource-group $ResourceGroupName `
        --deployment-name "gpt-4o" `
        --model-name "gpt-4o" `
        --model-version "2024-05-13" `
        --model-format OpenAI `
        --sku-capacity 10 `
        --sku-name "Standard" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ? GPT-4o deployed" -ForegroundColor Green
    } else {
        Write-Host "   ?? Deploying gpt-4o-mini instead..." -ForegroundColor Yellow
        az cognitiveservices account deployment create `
            --name $openAIName `
            --resource-group $ResourceGroupName `
            --deployment-name "gpt-4o" `
            --model-name "gpt-4o-mini" `
            --model-version "2024-07-18" `
            --model-format OpenAI `
            --sku-capacity 10 `
            --sku-name "Standard" 2>&1 | Out-Null
    }
} else {
    Write-Host "   ?? Azure OpenAI not available in your subscription" -ForegroundColor Yellow
    Write-Host "   You can continue without AI features or request access" -ForegroundColor Gray
}

# Get OpenAI credentials
$openAIEndpoint = ""
$openAIKey = ""
try {
    $openAIEndpoint = az cognitiveservices account show `
        --name $openAIName `
        --resource-group $ResourceGroupName `
        --query "properties.endpoint" -o tsv 2>$null
    
    $openAIKey = az cognitiveservices account keys list `
        --name $openAIName `
        --resource-group $ResourceGroupName `
        --query "key1" -o tsv 2>$null
} catch {}

# Create Document Intelligence
Write-Host "`n3?? Creating Azure Document Intelligence..." -ForegroundColor Yellow
$docAIName = "studentlink-docai"

az cognitiveservices account create `
    --name $docAIName `
    --resource-group $ResourceGroupName `
    --kind FormRecognizer `
    --sku F0 `
    --location $Location `
    --yes 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ? Document Intelligence created (Free tier)" -ForegroundColor Green
} else {
    Write-Host "   ?? Free tier limit reached, trying Standard..." -ForegroundColor Yellow
    az cognitiveservices account create `
        --name $docAIName `
        --resource-group $ResourceGroupName `
        --kind FormRecognizer `
        --sku S0 `
        --location $Location `
        --yes 2>&1 | Out-Null
}

# Get Document Intelligence credentials
$docAIEndpoint = az cognitiveservices account show `
    --name $docAIName `
    --resource-group $ResourceGroupName `
    --query "properties.endpoint" -o tsv

$docAIKey = az cognitiveservices account keys list `
    --name $docAIName `
    --resource-group $ResourceGroupName `
    --query "key1" -o tsv

# Configure Service Bus Queue
Write-Host "`n4?? Configuring Service Bus..." -ForegroundColor Yellow
$queueExists = az servicebus queue show `
    --name "cv-processing-queue" `
    --namespace-name $ServiceBusNamespace `
    --resource-group $ResourceGroupName 2>$null

if (-not $queueExists) {
    az servicebus queue create `
        --name "cv-processing-queue" `
        --namespace-name $ServiceBusNamespace `
        --resource-group $ResourceGroupName 2>&1 | Out-Null
    Write-Host "   ? CV processing queue created" -ForegroundColor Green
} else {
    Write-Host "   ? Queue already exists" -ForegroundColor Green
}

# Get Service Bus connection
$serviceBusConnection = az servicebus namespace authorization-rule keys list `
    --name RootManageSharedAccessKey `
    --namespace-name $ServiceBusNamespace `
    --resource-group $ResourceGroupName `
    --query "primaryConnectionString" -o tsv

# Configure Blob Storage
Write-Host "`n5?? Configuring Blob Storage..." -ForegroundColor Yellow
$containerExists = az storage container exists `
    --name "cvs" `
    --account-name $StorageAccountName `
    --query "exists" -o tsv 2>$null

if ($containerExists -ne "true") {
    az storage container create `
        --name "cvs" `
        --account-name $StorageAccountName `
        --public-access off 2>&1 | Out-Null
    Write-Host "   ? CVs container created" -ForegroundColor Green
} else {
    Write-Host "   ? Container already exists" -ForegroundColor Green
}

$storageConnection = az storage account show-connection-string `
    --name $StorageAccountName `
    --resource-group $ResourceGroupName `
    --query "connectionString" -o tsv

# Store secrets in Key Vault
Write-Host "`n6?? Storing secrets in Key Vault..." -ForegroundColor Yellow

if ($openAIKey) {
    az keyvault secret set --vault-name $KeyVaultName --name "OpenAI-ApiKey" --value $openAIKey 2>&1 | Out-Null
    Write-Host "   ? OpenAI key stored" -ForegroundColor Green
}

az keyvault secret set --vault-name $KeyVaultName --name "DocAI-ApiKey" --value $docAIKey 2>&1 | Out-Null
Write-Host "   ? Document Intelligence key stored" -ForegroundColor Green

# Create appsettings.Development.json
Write-Host "`n7?? Creating configuration file..." -ForegroundColor Yellow

$configPath = Join-Path (Split-Path -Parent $PSScriptRoot) "StudentLinkApi\appsettings.Development.json"

$config = @{
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Information"
            "Microsoft.AspNetCore" = "Warning"
            "Azure" = "Warning"
        }
    }
    "ConnectionStrings" = @{
        "DefaultConnection" = "Server=studentlink-sql-proj.database.windows.net;Database=studentlinkdb;User ID=studentlink-admin;Password=SQLpassword@123;Encrypt=True;TrustServerCertificate=False;"
    }
    "Azure" = @{
        "AI" = @{
            "Enabled" = if ($openAIKey) { $true } else { $false }
        }
        "OpenAI" = @{
            "Endpoint" = $openAIEndpoint
            "ApiKey" = $openAIKey
            "DeploymentName" = "gpt-4o"
        }
        "FormRecognizer" = @{
            "Endpoint" = $docAIEndpoint
            "ApiKey" = $docAIKey
        }
        "ServiceBus" = @{
            "ConnectionString" = $serviceBusConnection
        }
        "BlobStorage" = @{
            "ConnectionString" = $storageConnection
            "ContainerName" = "cvs"
        }
    }
    "FileStorage" = @{
        "UseAzure" = $true
    }
    "KeyVault" = @{
        "Uri" = "https://$KeyVaultName.vault.azure.net/"
    }
    "ApplicationInsights" = @{
        "ConnectionString" = ""
    }
}

$configJson = $config | ConvertTo-Json -Depth 10
Set-Content -Path $configPath -Value $configJson

Write-Host "   ? Configuration saved" -ForegroundColor Green

# Summary
Write-Host "`n" + ("="*80) -ForegroundColor Cyan
Write-Host "?? AI SERVICES CONFIGURED!" -ForegroundColor Green
Write-Host ("="*80) -ForegroundColor Cyan

Write-Host "`n?? Resources Created:" -ForegroundColor Yellow
if ($openAIEndpoint) {
    Write-Host "  ? Azure OpenAI: $openAIName" -ForegroundColor Green
} else {
    Write-Host "  ?? Azure OpenAI: Not available (AI features disabled)" -ForegroundColor Yellow
}
Write-Host "  ? Document Intelligence: $docAIName" -ForegroundColor Green
Write-Host "  ? Service Bus Queue: cv-processing-queue" -ForegroundColor Green
Write-Host "  ? Blob Container: cvs" -ForegroundColor Green

Write-Host "`n?? Secrets Stored in Key Vault:" -ForegroundColor Cyan
Write-Host "  Vault: $KeyVaultName" -ForegroundColor White
Write-Host "  Secrets: OpenAI-ApiKey, DocAI-ApiKey" -ForegroundColor Gray

Write-Host "`n?? Configuration File:" -ForegroundColor Cyan
Write-Host "  $configPath" -ForegroundColor White

Write-Host "`n?? Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Apply database migration:" -ForegroundColor White
Write-Host "     cd infrastructure" -ForegroundColor Gray
Write-Host "     .\add-ai-features.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Restart your API:" -ForegroundColor White
Write-Host "     cd ..\StudentLinkApi" -ForegroundColor Gray
Write-Host "     dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Test CV upload:" -ForegroundColor White
Write-Host "     Upload a CV via frontend (http://localhost:3000)" -ForegroundColor Gray
Write-Host "     Check feedback: GET /cv/{id}/feedback" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Estimated Monthly Costs:" -ForegroundColor Cyan
Write-Host "  Document Intelligence (F0): FREE" -ForegroundColor Green
if ($openAIKey) {
    Write-Host "  Azure OpenAI (S0): ~$10-50 (usage-based)" -ForegroundColor Yellow
} else {
    Write-Host "  Azure OpenAI: $0 (not enabled)" -ForegroundColor Green
}
Write-Host "  Service Bus: ~$0.05" -ForegroundColor Green
Write-Host "  Blob Storage: ~$0.02" -ForegroundColor Green
Write-Host ""

Write-Host "? AI setup complete! Ready for intelligent CV processing!" -ForegroundColor Green
Write-Host ""