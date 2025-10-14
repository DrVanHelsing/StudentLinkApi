# Fix Connection String Script
Write-Host "?? Updating appsettings.json with Azure SQL connection..." -ForegroundColor Cyan

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$appsettingsPath = Join-Path $RootDir "StudentLinkApi\appsettings.json"

# Azure SQL connection details (from your manual setup)
$sqlServer = "studentlink-sql-proj"
$sqlDatabase = "studentlinkdb"
$sqlUser = "studentlink-admin"

Write-Host "Please enter your SQL password: " -NoNewline
$sqlPassword = Read-Host -AsSecureString
$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword))

$connectionString = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$sqlDatabase;User ID=$sqlUser;Password=$sqlPasswordPlain;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Update appsettings.json
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $appsettings.ConnectionStrings.DefaultConnection = $connectionString
    $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
    
    Write-Host "? Connection string updated successfully!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "  1. Restart your API (Ctrl+C then dotnet run)" -ForegroundColor White
    Write-Host "  2. Run the test script again: .\test-api.ps1" -ForegroundColor White
} else {
    Write-Host "? appsettings.json not found at $appsettingsPath" -ForegroundColor Red
}