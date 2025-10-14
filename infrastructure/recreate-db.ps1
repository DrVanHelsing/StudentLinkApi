# Force Database Recreation Script
Write-Host "?? Force recreating LocalDB database..." -ForegroundColor Cyan

$ApiDir = "C:\MAUI Applications\StudentLinkApi_Sln\StudentLinkApi"
Push-Location $ApiDir

Write-Host "`n1?? Dropping existing database..." -ForegroundColor Yellow
dotnet ef database drop --force --context ApplicationDbContext

Write-Host "`n2?? Removing migrations..." -ForegroundColor Yellow
dotnet ef migrations remove --force --context ApplicationDbContext 2>$null

Write-Host "`n3?? Creating fresh migration..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate --context ApplicationDbContext --output-dir Data/Migrations

Write-Host "`n4?? Applying migration..." -ForegroundColor Yellow
dotnet ef database update --context ApplicationDbContext --verbose

Pop-Location

Write-Host "`n" + ("="*70) -ForegroundColor Cyan
Write-Host "? Database recreated successfully!" -ForegroundColor Green
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Restart your API (Ctrl+C then: dotnet run)" -ForegroundColor White
Write-Host "  2. Run tests: cd ..\infrastructure; .\test-api.ps1" -ForegroundColor White
Write-Host ""