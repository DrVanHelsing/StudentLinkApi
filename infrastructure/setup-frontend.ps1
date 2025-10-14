# StudentLink Frontend Setup Script

Write-Host "?? Setting up StudentLink Frontend..." -ForegroundColor Cyan

$FrontendDir = Join-Path (Split-Path -Parent $PSScriptRoot) "studentlink-frontend"

# Check if Node.js is installed
Write-Host "`n1?? Checking prerequisites..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "   ? Node.js installed: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "   ? Node.js not found. Please install from: https://nodejs.org/" -ForegroundColor Red
    exit 1
}

try {
    $npmVersion = npm --version
    Write-Host "   ? npm installed: v$npmVersion" -ForegroundColor Green
} catch {
    Write-Host "   ? npm not found" -ForegroundColor Red
    exit 1
}

# Navigate to frontend directory
if (-not (Test-Path $FrontendDir)) {
    Write-Host "`n? Frontend directory not found at: $FrontendDir" -ForegroundColor Red
    exit 1
}

Push-Location $FrontendDir

# Install dependencies
Write-Host "`n2?? Installing dependencies..." -ForegroundColor Yellow
Write-Host "   This may take a few minutes..." -ForegroundColor Gray
npm install

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ? Failed to install dependencies" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "   ? Dependencies installed" -ForegroundColor Green

# Display next steps
Write-Host "`n" + ("="*70) -ForegroundColor Cyan
Write-Host "?? FRONTEND SETUP COMPLETE!" -ForegroundColor Green
Write-Host ("="*70) -ForegroundColor Cyan
Write-Host "`n?? Next Steps:" -ForegroundColor Yellow
Write-Host "`n1?? Start your API (if not running):" -ForegroundColor White
Write-Host "   cd ..\StudentLinkApi" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host "`n2?? Start the frontend:" -ForegroundColor White
Write-Host "   cd studentlink-frontend" -ForegroundColor Gray
Write-Host "   npm start" -ForegroundColor Gray
Write-Host "`n3?? Open your browser:" -ForegroundColor White
Write-Host "   http://localhost:3000" -ForegroundColor Gray
Write-Host "`n?? Features:" -ForegroundColor Cyan
Write-Host "   • User Registration & Login" -ForegroundColor Gray
Write-Host "   • Role-based Dashboards (Student/Recruiter/Admin)" -ForegroundColor Gray
Write-Host "   • Profile Management" -ForegroundColor Gray
Write-Host "   • Protected Routes" -ForegroundColor Gray
Write-Host "   • Modern Responsive UI" -ForegroundColor Gray
Write-Host "`n?? Test Accounts:" -ForegroundColor Cyan
Write-Host "   Student: student355154426@test.com / TestPassword123!" -ForegroundColor Gray
Write-Host "   Recruiter: recruiter1517664712@test.com / Password123!" -ForegroundColor Gray
Write-Host "   Admin: admin2123524763@test.com / Password123!" -ForegroundColor Gray
Write-Host ""

Pop-Location