# Verify AI Configuration and Setup

Write-Host "?? StudentLink AI Configuration Verification" -ForegroundColor Cyan
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host ""

# Check appsettings.Development.json
$configPath = Join-Path (Split-Path -Parent $PSScriptRoot) "StudentLinkApi\appsettings.Development.json"

Write-Host "1?? Checking configuration file..." -ForegroundColor Yellow
if (Test-Path $configPath) {
    Write-Host "   ? appsettings.Development.json found" -ForegroundColor Green
    
    $config = Get-Content $configPath | ConvertFrom-Json
    
    # Check Azure AI settings
    Write-Host "`n2?? Verifying Azure AI settings..." -ForegroundColor Yellow
    
    if ($config.Azure.AI.Enabled) {
        Write-Host "   ? AI Features: ENABLED" -ForegroundColor Green
    } else {
        Write-Host "   ?? AI Features: DISABLED" -ForegroundColor Yellow
    }
    
    # Check OpenAI
    if ($config.Azure.OpenAI.Endpoint -and $config.Azure.OpenAI.ApiKey) {
        Write-Host "   ? Azure OpenAI: Configured" -ForegroundColor Green
        Write-Host "      Endpoint: $($config.Azure.OpenAI.Endpoint)" -ForegroundColor Gray
        Write-Host "      Deployment: $($config.Azure.OpenAI.DeploymentName)" -ForegroundColor Gray
    } else {
        Write-Host "   ? Azure OpenAI: NOT configured" -ForegroundColor Red
    }
    
    # Check Form Recognizer
    if ($config.Azure.FormRecognizer.Endpoint -and $config.Azure.FormRecognizer.ApiKey) {
        Write-Host "   ? Document Intelligence: Configured" -ForegroundColor Green
        Write-Host "      Endpoint: $($config.Azure.FormRecognizer.Endpoint)" -ForegroundColor Gray
    } else {
        Write-Host "   ? Document Intelligence: NOT configured" -ForegroundColor Red
    }
    
    # Check Service Bus
    if ($config.Azure.ServiceBus.ConnectionString) {
        Write-Host "   ? Service Bus: Configured" -ForegroundColor Green
    } else {
        Write-Host "   ?? Service Bus: NOT configured (will process synchronously)" -ForegroundColor Yellow
    }
    
    # Check Blob Storage
    if ($config.Azure.BlobStorage.ConnectionString) {
        Write-Host "   ? Blob Storage: Configured" -ForegroundColor Green
        Write-Host "      Container: $($config.Azure.BlobStorage.ContainerName)" -ForegroundColor Gray
    } else {
        Write-Host "   ? Blob Storage: NOT configured" -ForegroundColor Red
    }
    
    # Check File Storage mode
    Write-Host "`n3?? File Storage Configuration..." -ForegroundColor Yellow
    if ($config.FileStorage.UseAzure) {
        Write-Host "   ? Using Azure Blob Storage" -ForegroundColor Green
    } else {
        Write-Host "   ?? Using Local Storage: $($config.FileStorage.LocalPath)" -ForegroundColor Gray
    }
    
} else {
    Write-Host "   ? Configuration file not found!" -ForegroundColor Red
    Write-Host "   Expected: $configPath" -ForegroundColor Gray
}

# Check database
Write-Host "`n4?? Checking database..." -ForegroundColor Yellow
$apiDir = Join-Path (Split-Path -Parent $PSScriptRoot) "StudentLinkApi"
Push-Location $apiDir

try {
    $migrations = dotnet ef migrations list --no-build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ? Database migrations ready" -ForegroundColor Green
        
        # Check if AI migrations exist
        if ($migrations -match "AddAIFeatures") {
            Write-Host "   ? AI features migration found" -ForegroundColor Green
        } else {
            Write-Host "   ?? AI features migration NOT found" -ForegroundColor Yellow
            Write-Host "      Run: .\add-ai-features.ps1" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ?? Could not verify migrations" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ?? Could not check database" -ForegroundColor Yellow
}

Pop-Location

# Summary
Write-Host "`n" + ("="*70) -ForegroundColor Cyan
Write-Host "?? CONFIGURATION SUMMARY" -ForegroundColor Green
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host ""

Write-Host "? Configured Resources:" -ForegroundColor Green
Write-Host "  • Azure OpenAI (gpt-5-mini)" -ForegroundColor White
Write-Host "  • Document Intelligence" -ForegroundColor White
Write-Host "  • Blob Storage (cvs container)" -ForegroundColor White

if ($config.Azure.ServiceBus.ConnectionString) {
    Write-Host "  • Service Bus Queue" -ForegroundColor White
} else {
    Write-Host "`n?? Missing Configuration:" -ForegroundColor Yellow
    Write-Host "  • Service Bus connection string" -ForegroundColor White
    Write-Host "    Run: .\get-servicebus-connection.ps1" -ForegroundColor Gray
}

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host ""

if (-not $config.Azure.ServiceBus.ConnectionString) {
    Write-Host "  1. Get Service Bus connection:" -ForegroundColor White
    Write-Host "     cd infrastructure" -ForegroundColor Gray
    Write-Host "     .\get-servicebus-connection.ps1" -ForegroundColor Gray
    Write-Host ""
}

if ($migrations -notmatch "AddAIFeatures") {
    Write-Host "  2. Apply AI database migration:" -ForegroundColor White
    Write-Host "     cd infrastructure" -ForegroundColor Gray
    Write-Host "     .\add-ai-features.ps1" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "  3. Build the project:" -ForegroundColor White
Write-Host "     cd StudentLinkApi" -ForegroundColor Gray
Write-Host "     dotnet build" -ForegroundColor Gray
Write-Host ""

Write-Host "  4. Start the API:" -ForegroundColor White
Write-Host "     dotnet run" -ForegroundColor Gray
Write-Host ""

Write-Host "  5. Test CV upload with AI!" -ForegroundColor White
Write-Host "     Upload a CV via http://localhost:3000" -ForegroundColor Gray
Write-Host ""

Write-Host "?? Documentation:" -ForegroundColor Cyan
Write-Host "  • FINAL-IMPLEMENTATION-SUMMARY.md - Complete guide" -ForegroundColor Gray
Write-Host "  • AI-CV-PROCESSING-GUIDE.md - AI implementation details" -ForegroundColor Gray
Write-Host ""