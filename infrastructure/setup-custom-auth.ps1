# Custom JWT Authentication Migration Script

Write-Host "?? Updating database for custom JWT authentication..." -ForegroundColor Cyan

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ApiDir = Join-Path $RootDir "StudentLinkApi"

# Navigate to API directory
Push-Location $ApiDir

Write-Host "?? Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`n??? Building project..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed. Fix errors first." -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "`n??? Removing old migrations..." -ForegroundColor Yellow
dotnet ef migrations remove --context ApplicationDbContext --force 2>$null

Write-Host "??? Creating new migration for custom JWT auth..." -ForegroundColor Yellow
dotnet ef migrations add CustomJwtAuth --context ApplicationDbContext --output-dir Data/Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Migration created successfully" -ForegroundColor Green
    
    Write-Host "`n??? Applying migration to database..." -ForegroundColor Yellow
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

Write-Host "`n" + ("="*60) -ForegroundColor Cyan
Write-Host "?? CUSTOM AUTHENTICATION READY!" -ForegroundColor Green
Write-Host ("="*60) -ForegroundColor Cyan
Write-Host "`nYou can now:" -ForegroundColor Yellow
Write-Host "  1. Run the API: cd StudentLinkApi; dotnet run" -ForegroundColor White
Write-Host "  2. Register users: POST /auth/register" -ForegroundColor White
Write-Host "  3. Login: POST /auth/login" -ForegroundColor White
Write-Host "  4. Access protected endpoints with JWT token" -ForegroundColor White
Write-Host "`n?? See CUSTOM-AUTH-TESTING.md for testing guide" -ForegroundColor Cyan
Write-Host ""