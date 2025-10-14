# Switch to LocalDB for Development
Write-Host "?? Switching to LocalDB for development..." -ForegroundColor Cyan

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ApiDir = Join-Path $RootDir "StudentLinkApi"
$appsettingsPath = Join-Path $ApiDir "appsettings.json"

# Update to LocalDB connection string
Write-Host "?? Updating appsettings.json..." -ForegroundColor Yellow
$appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
$appsettings.ConnectionStrings.DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=StudentLinkDb;Trusted_Connection=true;MultipleActiveResultSets=true"
$appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
Write-Host "? Connection string updated to LocalDB" -ForegroundColor Green

# Create LocalDB database
Write-Host "`n??? Creating LocalDB database..." -ForegroundColor Yellow
Push-Location $ApiDir

Write-Host "   Dropping existing database (if any)..." -ForegroundColor Gray
dotnet ef database drop --force --context ApplicationDbContext 2>$null | Out-Null

Write-Host "   Creating database and applying migrations..." -ForegroundColor Gray
dotnet ef database update --context ApplicationDbContext

if ($LASTEXITCODE -eq 0) {
    Write-Host "? LocalDB database created successfully!" -ForegroundColor Green
} else {
    Write-Host "? Database creation failed" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

Write-Host "`n" + ("="*70) -ForegroundColor Cyan
Write-Host "?? SETUP COMPLETE!" -ForegroundColor Green
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host "`n? Your API is now using LocalDB for development" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Restart your API (Ctrl+C then: cd ..\StudentLinkApi; dotnet run)" -ForegroundColor White
Write-Host "  2. Run tests: cd ..\infrastructure; .\test-api.ps1" -ForegroundColor White
Write-Host ""
Write-Host "?? Benefits of LocalDB:" -ForegroundColor Cyan
Write-Host "  • Free and always available" -ForegroundColor Gray
Write-Host "  • No Azure costs during development" -ForegroundColor Gray
Write-Host "  • Faster for local testing" -ForegroundColor Gray
Write-Host "  • Use Azure SQL when deploying to production" -ForegroundColor Gray
Write-Host ""