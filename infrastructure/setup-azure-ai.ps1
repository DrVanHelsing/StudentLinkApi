# Azure AI Resources Setup Script
# Adds AI services to existing StudentLink resource group

param(
    [string]$ResourceGroupName = "rg-studentlink-proj",
    [string]$Location = "southafricanorth",
    [string]$ProjectName = "studentlink"
)

Write-Host "?? Adding Azure AI Services to existing StudentLink resource group..." -ForegroundColor Cyan
Write-Host ""

# Login to Azure
Write-Host "1?? Checking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null
if (-not $account) {
    Write-Host "   Please login to Azure..." -ForegroundColor Gray
    az login
}

$accountInfo = az account show | ConvertFrom-Json
Write-Host "   ? Logged in as: $($accountInfo.user.name)" -ForegroundColor Green
Write-Host "   Subscription: $($accountInfo.name)" -ForegroundColor Gray

# Verify resource group exists
Write-Host "`n2?? Verifying resource group..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName
if ($rgExists -eq "true") {
    Write-Host "   ? Resource group '$ResourceGroupName' found" -ForegroundColor Green
} else {
    Write-Host "   ? Resource group '$ResourceGroupName' not found" -ForegroundColor Red
    Write-Host "   Creating resource group..." -ForegroundColor Yellow
    az group create --name $ResourceGroupName --location $Location
}

# Create Azure OpenAI Service
Write-Host "`n3?? Creating Azure OpenAI Service..." -ForegroundColor Yellow
$openAIName = "$ProjectName-openai-$(Get-Random -Maximum 9999)"
Write-Host "   Name: $openAIName" -ForegroundColor Gray

try {
    az cognitiveservices account create `
        --name $openAIName `
        --resource-group $ResourceGroupName `
        --kind OpenAI `
        --sku S0 `
        --location eastus `
        --custom-domain $openAIName `
        --yes 2>$null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ? Azure OpenAI created" -ForegroundColor Green
        
        # Wait for deployment to complete
        Start-Sleep -Seconds 10
        
        # Deploy GPT-4o model
        Write-Host "   Deploying GPT-4o model..." -ForegroundColor Gray
        az cognitiveservices account deployment create `
            --name $openAIName `
            --resource-group $ResourceGroupName `
            --deployment-name "gpt-4o" `
            --model-name "gpt-4o" `
            --model-version "2024-05-13" `
            --model-format OpenAI `
            --sku-capacity 10 `
            --sku-name "Standard" 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ? GPT-4o model deployed" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "   ?? Azure OpenAI creation skipped (may not be available in your region)" -ForegroundColor Yellow
}

# Get OpenAI endpoint and key
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
} catch {
    Write-Host "   Note: Could not retrieve OpenAI credentials" -ForegroundColor Gray
}

# Create Form Recognizer (Document Intelligence)
Write-Host "`n4?? Creating Azure AI Document Intelligence..." -ForegroundColor Yellow
$formRecognizerName = "$ProjectName-docai-$(Get-Random -Maximum 9999)"
Write-Host "   Name: $formRecognizerName" -ForegroundColor Gray

az cognitiveservices account create `
    --name $formRecognizerName `
    --resource-group $ResourceGroupName `
    --kind FormRecognizer `
    --sku S0 `
    --location $Location `
    --yes 2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ? Document Intelligence created" -ForegroundColor Green
} else {
    Write-Host "   ?? Using Free tier..." -ForegroundColor Yellow
    az cognitiveservices account create `
        --name $formRecognizerName `
        --resource-group $ResourceGroupName `
        --kind FormRecognizer `
        --sku F0 `
        --location $Location `
        --yes
}

# Get Form Recognizer endpoint and key
$formRecognizerEndpoint = az cognitiveservices account show `
    --name $formRecognizerName `
    --resource-group $ResourceGroupName `
    --query "properties.endpoint" -o tsv

$formRecognizerKey = az cognitiveservices account keys list `
    --name $formRecognizerName `
    --resource-group $ResourceGroupName `
    --query "key1" -o tsv

# Use existing Service Bus
Write-Host "`n5?? Checking existing Service Bus..." -ForegroundColor Yellow
$serviceBusName = "studentlink-sb-dev"
$serviceBusExists = az servicebus namespace show --name $serviceBusName --resource-group $ResourceGroupName 2>$null

if ($serviceBusExists) {
    Write-Host "   ? Using existing Service Bus: $serviceBusName" -ForegroundColor Green
} else {
    Write-Host "   Creating new Service Bus namespace..." -ForegroundColor Gray
    az servicebus namespace create `
        --name $serviceBusName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Standard
}

# Create CV processing queue
$queueExists = az servicebus queue show --name "cv-processing-queue" --namespace-name $serviceBusName --resource-group $ResourceGroupName 2>$null
if (-not $queueExists) {
    Write-Host "   Creating CV processing queue..." -ForegroundColor Gray
    az servicebus queue create `
        --name "cv-processing-queue" `
        --namespace-name $serviceBusName `
        --resource-group $ResourceGroupName
    Write-Host "   ? Queue created" -ForegroundColor Green
}

# Get Service Bus connection string
$serviceBusConnection = az servicebus namespace authorization-rule keys list `
    --name RootManageSharedAccessKey `
    --namespace-name $serviceBusName `
    --resource-group $ResourceGroupName `
    --query "primaryConnectionString" -o tsv

# Use existing Storage Account
Write-Host "`n6?? Checking existing Storage Account..." -ForegroundColor Yellow
$storageName = "studentlinkstore"
$storageExists = az storage account show --name $storageName --resource-group $ResourceGroupName 2>$null

if ($storageExists) {
    Write-Host "   ? Using existing Storage: $storageName" -ForegroundColor Green
} else {
    Write-Host "   ? Storage account not found" -ForegroundColor Red
}

# Ensure CVs container exists
Write-Host "   Checking blob container..." -ForegroundColor Gray
$containerExists = az storage container exists --name "cvs" --account-name $storageName --query "exists" -o tsv 2>$null

if ($containerExists -ne "true") {
    az storage container create `
        --name "cvs" `
        --account-name $storageName `
        --public-access off
    Write-Host "   ? CVs container created" -ForegroundColor Green
}

# Get storage connection string
$storageConnection = az storage account show-connection-string `
    --name $storageName `
    --resource-group $ResourceGroupName `
    --query "connectionString" -o tsv

# Display Summary
Write-Host "`n" + ("="*80) -ForegroundColor Cyan
Write-Host "? AZURE AI SERVICES CONFIGURED!" -ForegroundColor Green
Write-Host ("="*80) -ForegroundColor Cyan

Write-Host "`n?? Configuration Summary:" -ForegroundColor Yellow
Write-Host ""

if ($openAIEndpoint) {
    Write-Host "Azure OpenAI:" -ForegroundColor Cyan
    Write-Host "  Resource: $openAIName" -ForegroundColor White
    Write-Host "  Endpoint: $openAIEndpoint" -ForegroundColor White
    Write-Host "  Key: $($openAIKey.Substring(0,10))..." -ForegroundColor White
    Write-Host "  Deployment: gpt-4o" -ForegroundColor White
    Write-Host ""
}

Write-Host "Azure Document Intelligence:" -ForegroundColor Cyan
Write-Host "  Resource: $formRecognizerName" -ForegroundColor White
Write-Host "  Endpoint: $formRecognizerEndpoint" -ForegroundColor White
Write-Host "  Key: $($formRecognizerKey.Substring(0,10))..." -ForegroundColor White
Write-Host ""

Write-Host "Azure Service Bus:" -ForegroundColor Cyan
Write-Host "  Namespace: $serviceBusName" -ForegroundColor White
Write-Host "  Queue: cv-processing-queue" -ForegroundColor White
Write-Host ""

Write-Host "Azure Blob Storage:" -ForegroundColor Cyan
Write-Host "  Account: $storageName" -ForegroundColor White
Write-Host "  Container: cvs" -ForegroundColor White
Write-Host ""

# Save configuration
$configPath = Join-Path (Split-Path -Parent $PSScriptRoot) "StudentLinkApi\appsettings.Development.json"

$config = @{
    "Logging" = @{
        "LogLevel" = @{
            "Default" = "Information"
            "Microsoft.AspNetCore" = "Warning"
        }
    }
    "Azure" = @{
        "AI" = @{
            "Enabled" = $true
        }
        "OpenAI" = @{
            "Endpoint" = $openAIEndpoint
            "ApiKey" = $openAIKey
            "DeploymentName" = "gpt-4o"
        }
        "FormRecognizer" = @{
            "Endpoint" = $formRecognizerEndpoint
            "ApiKey" = $formRecognizerKey
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
}

$configJson = $config | ConvertTo-Json -Depth 10
Set-Content -Path $configPath -Value $configJson

Write-Host "?? Configuration saved to:" -ForegroundColor Green
Write-Host "   $configPath" -ForegroundColor Gray
Write-Host ""

Write-Host "?? SECURITY REMINDER:" -ForegroundColor Red
Write-Host "  • DO NOT commit appsettings.Development.json to Git" -ForegroundColor Yellow
Write-Host "  • Add it to .gitignore" -ForegroundColor Yellow
Write-Host "  • Use Azure Key Vault in production" -ForegroundColor Yellow
Write-Host ""

Write-Host "?? Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Apply database migration:" -ForegroundColor White
Write-Host "     cd infrastructure; .\add-ai-features.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Restart your API:" -ForegroundColor White
Write-Host "     cd ..\StudentLinkApi; dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Test CV upload with AI analysis!" -ForegroundColor White
Write-Host "     Upload a CV and check /cv/{id}/feedback" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Estimated Costs:" -ForegroundColor Cyan
Write-Host "  • Document Intelligence (F0): FREE" -ForegroundColor Green
if ($openAIEndpoint) {
    Write-Host "  • Azure OpenAI (S0): ~$10-50/month (usage-based)" -ForegroundColor Yellow
}
Write-Host "  • Service Bus (Standard): ~$0.05/month" -ForegroundColor Green
Write-Host "  • Blob Storage: ~$0.02/month" -ForegroundColor Green
Write-Host ""

Write-Host "? Your AI-powered CV processing is ready!" -ForegroundColor Green
Write-Host ""