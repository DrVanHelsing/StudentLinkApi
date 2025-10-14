# Add CV Upload Feature - Database Migration

Write-Host "?? Adding CV Upload feature..." -ForegroundColor Cyan

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

Write-Host "`n??? Creating migration..." -ForegroundColor Yellow
dotnet ef migrations add AddCVSupport --context ApplicationDbContext --output-dir Data/Migrations

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

Write-Host "`n" + ("="*70) -ForegroundColor Cyan
Write-Host "?? CV UPLOAD FEATURE ADDED!" -ForegroundColor Green
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host "`n? Backend Complete:" -ForegroundColor Green
Write-Host "  • CV model created" -ForegroundColor White
Write-Host "  • File storage service added" -ForegroundColor White
Write-Host "  • CV controller with upload/download" -ForegroundColor White
Write-Host "  • Database migration applied" -ForegroundColor White
Write-Host "`n?? New API Endpoints:" -ForegroundColor Cyan
Write-Host "  POST   /cv/upload        - Upload CV" -ForegroundColor Gray
Write-Host "  GET    /cv/current       - Get current CV" -ForegroundColor Gray
Write-Host "  GET    /cv/history       - Get CV history" -ForegroundColor Gray
Write-Host "  GET    /cv/download/{id} - Download CV" -ForegroundColor Gray
Write-Host "  DELETE /cv/{id}          - Delete CV" -ForegroundColor Gray
Write-Host "`n?? Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Restart API: cd ..\StudentLinkApi; dotnet run" -ForegroundColor White
Write-Host "  2. Update frontend with CV upload component" -ForegroundColor White
Write-Host "  3. Test CV upload in Swagger: https://localhost:7068/swagger" -ForegroundColor White
Write-Host ""