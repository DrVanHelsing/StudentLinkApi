# Add AI Features - Database Migration

Write-Host "?? Adding AI-powered CV processing features..." -ForegroundColor Cyan

$ApiDir = "C:\MAUI Applications\StudentLinkApi_Sln\StudentLinkApi"
Push-Location $ApiDir

Write-Host "`n?? Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`n??? Building project..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "`n??? Creating migration for AI features..." -ForegroundColor Yellow
dotnet ef migrations add AddAIFeatures --context ApplicationDbContext --output-dir Data/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Migration created successfully" -ForegroundColor Green
    
    Write-Host "`n??? Applying migration..." -ForegroundColor Yellow
    dotnet ef database update --context ApplicationDbContext
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Database updated successfully!" -ForegroundColor Green
    } else {
        Write-Host "? Database update failed" -ForegroundColor Red
        Pop-Location
        exit 1
    }
} else {
    Write-Host "? Migration creation failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

Write-Host "`n" + ("="*80) -ForegroundColor Cyan
Write-Host "?? AI FEATURES ADDED!" -ForegroundColor Green
Write-Host ("="*80) -ForegroundColor Cyan

Write-Host "`n? New Database Tables:" -ForegroundColor Green
Write-Host "  • CVFeedbacks - Stores AI-generated feedback" -ForegroundColor White
Write-Host "  • CVAnalysisResults - Stores extracted CV data" -ForegroundColor White
Write-Host "  • Jobs - Stores job postings" -ForegroundColor White
Write-Host "  • JobMatches - Stores AI-powered job matches" -ForegroundColor White

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Run: .\setup-azure-ai.ps1 (optional)" -ForegroundColor White
Write-Host "  2. Restart API" -ForegroundColor White
Write-Host "  3. Test CV upload with AI!" -ForegroundColor White
Write-Host ""