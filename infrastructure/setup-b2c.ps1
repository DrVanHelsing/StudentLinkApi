# Azure AD B2C Setup Script
# Creates B2C tenant, app registrations, user flows, and custom policies

Write-Host "?? Azure AD B2C Setup for StudentLink Platform" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

Write-Host "`n??  IMPORTANT NOTES:" -ForegroundColor Yellow
Write-Host "   1. Azure AD B2C cannot be fully automated via CLI" -ForegroundColor White
Write-Host "   2. You must create the B2C tenant manually in Azure Portal" -ForegroundColor White
Write-Host "   3. This script will guide you through the process" -ForegroundColor White
Write-Host ""

Write-Host "?? Manual Steps Required:" -ForegroundColor Yellow
Write-Host ""
Write-Host "STEP 1: Create Azure AD B2C Tenant" -ForegroundColor Cyan
Write-Host "   1. Go to: https://portal.azure.com" -ForegroundColor White
Write-Host "   2. Search for 'Azure AD B2C'" -ForegroundColor White
Write-Host "   3. Click 'Create a new B2C Tenant'" -ForegroundColor White
Write-Host "   4. Organization name: StudentLink" -ForegroundColor White
Write-Host "   5. Initial domain name: studentlink (or available name)" -ForegroundColor White
Write-Host "   6. Location: South Africa North" -ForegroundColor White
Write-Host "   7. Click 'Create' and wait 2-3 minutes" -ForegroundColor White
Write-Host ""

$tenantName = Read-Host "Enter your B2C tenant name (e.g., studentlink)"
$tenantDomain = "$tenantName.onmicrosoft.com"

Write-Host ""
Write-Host "STEP 2: Create User Flow (Sign-up and Sign-in)" -ForegroundColor Cyan
Write-Host "   1. In B2C tenant, go to 'User flows'" -ForegroundColor White
Write-Host "   2. Click '+ New user flow'" -ForegroundColor White
Write-Host "   3. Select 'Sign up and sign in'" -ForegroundColor White
Write-Host "   4. Version: Recommended" -ForegroundColor White
Write-Host "   5. Name: B2C_1_signupsignin" -ForegroundColor White
Write-Host "   6. Identity providers: Email signup" -ForegroundColor White
Write-Host "   7. User attributes: Email Address, Display Name, Given Name, Surname" -ForegroundColor White
Write-Host "   8. Application claims: Check all selected attributes + 'User's Object ID'" -ForegroundColor White
Write-Host "   9. Click 'Create'" -ForegroundColor White
Write-Host ""

Write-Host "STEP 3: Create App Registration (API)" -ForegroundColor Cyan
Write-Host "   1. In B2C tenant, go to 'App registrations'" -ForegroundColor White
Write-Host "   2. Click '+ New registration'" -ForegroundColor White
Write-Host "   3. Name: StudentLink API" -ForegroundColor White
Write-Host "   4. Supported account types: Accounts in this organizational directory only" -ForegroundColor White
Write-Host "   5. Redirect URI: Leave blank for now" -ForegroundColor White
Write-Host "   6. Click 'Register'" -ForegroundColor White
Write-Host "   7. Copy the 'Application (client) ID' - this is your ClientId" -ForegroundColor White
Write-Host ""

$apiClientId = Read-Host "Enter the API Application (client) ID"

Write-Host ""
Write-Host "STEP 4: Expose API Scope" -ForegroundColor Cyan
Write-Host "   1. In the API app registration, go to 'Expose an API'" -ForegroundColor White
Write-Host "   2. Click 'Add a scope'" -ForegroundColor White
Write-Host "   3. Accept the default Application ID URI" -ForegroundColor White
Write-Host "   4. Scope name: user_impersonation" -ForegroundColor White
Write-Host "   5. Who can consent: Admins and users" -ForegroundColor White
Write-Host "   6. Display names and descriptions: 'Access StudentLink API'" -ForegroundColor White
Write-Host "   7. State: Enabled" -ForegroundColor White
Write-Host "   8. Click 'Add scope'" -ForegroundColor White
Write-Host ""

Write-Host "STEP 5: Create App Registration (Student Portal - Optional)" -ForegroundColor Cyan
Write-Host "   1. Go to 'App registrations' > '+ New registration'" -ForegroundColor White
Write-Host "   2. Name: StudentLink Student Portal" -ForegroundColor White
Write-Host "   3. Redirect URI: Single-page application (SPA) - http://localhost:3000" -ForegroundColor White
Write-Host "   4. Click 'Register'" -ForegroundColor White
Write-Host "   5. Go to 'API permissions' > 'Add a permission'" -ForegroundColor White
Write-Host "   6. Select 'My APIs' > 'StudentLink API'" -ForegroundColor White
Write-Host "   7. Select 'user_impersonation'" -ForegroundColor White
Write-Host "   8. Click 'Add permissions'" -ForegroundColor White
Write-Host ""

Write-Host "STEP 6: Add Custom Roles (App Roles)" -ForegroundColor Cyan
Write-Host "   1. In the API app registration, go to 'App roles'" -ForegroundColor White
Write-Host "   2. Click '+ Create app role'" -ForegroundColor White
Write-Host ""
Write-Host "   Create 3 roles:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Role 1: Student" -ForegroundColor White
Write-Host "      Display name: Student" -ForegroundColor Gray
Write-Host "      Value: Student" -ForegroundColor Gray
Write-Host "      Description: Student user with profile access" -ForegroundColor Gray
Write-Host "      Allowed member types: Users/Groups" -ForegroundColor Gray
Write-Host ""
Write-Host "   Role 2: Recruiter" -ForegroundColor White
Write-Host "      Display name: Recruiter" -ForegroundColor Gray
Write-Host "      Value: Recruiter" -ForegroundColor Gray
Write-Host "      Description: Recruiter with candidate search access" -ForegroundColor Gray
Write-Host "      Allowed member types: Users/Groups" -ForegroundColor Gray
Write-Host ""
Write-Host "   Role 3: Admin" -ForegroundColor White
Write-Host "      Display name: Admin" -ForegroundColor Gray
Write-Host "      Value: Admin" -ForegroundColor Gray
Write-Host "      Description: System administrator" -ForegroundColor Gray
Write-Host "      Allowed member types: Users/Groups" -ForegroundColor Gray
Write-Host ""

Write-Host "STEP 7: Update Token Configuration" -ForegroundColor Cyan
Write-Host "   1. In API app registration, go to 'Token configuration'" -ForegroundColor White
Write-Host "   2. Click '+ Add optional claim'" -ForegroundColor White
Write-Host "   3. Token type: ID, Access" -ForegroundColor White
Write-Host "   4. Select: email, family_name, given_name" -ForegroundColor White
Write-Host "   5. Click 'Add'" -ForegroundColor White
Write-Host "   6. Click '+ Add groups claim'" -ForegroundColor White
Write-Host "   7. Select 'Security groups' and 'Group ID'" -ForegroundColor White
Write-Host "   8. Click 'Add'" -ForegroundColor White
Write-Host ""

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "? Manual Setup Complete!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Generate appsettings
$b2cInstance = "https://$tenantName.b2clogin.com"
$b2cDomain = $tenantDomain

Write-Host "?? Your B2C Configuration:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Instance: $b2cInstance" -ForegroundColor White
Write-Host "Domain: $b2cDomain" -ForegroundColor White
Write-Host "ClientId (API): $apiClientId" -ForegroundColor White
Write-Host "SignUpSignInPolicyId: B2C_1_signupsignin" -ForegroundColor White
Write-Host ""

# Create appsettings template
$appsettingsB2C = @"
{
  "AzureAdB2C": {
    "Instance": "$b2cInstance",
    "Domain": "$b2cDomain",
    "TenantId": "<GET_FROM_B2C_TENANT_OVERVIEW>",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ClientId": "$apiClientId"
  }
}
"@

$appsettingsB2C | Out-File "infrastructure/b2c-config.json"
Write-Host "?? B2C configuration saved to: infrastructure/b2c-config.json" -ForegroundColor Cyan
Write-Host ""

Write-Host "?? Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Get TenantId from B2C tenant Overview page" -ForegroundColor White
Write-Host "   2. Update StudentLinkApi/appsettings.json with these values" -ForegroundColor White
Write-Host "   3. Test authentication with a test user" -ForegroundColor White
Write-Host ""
Write-Host "?? Test User Creation:" -ForegroundColor Yellow
Write-Host "   - Go to B2C tenant > Users > + New user" -ForegroundColor White
Write-Host "   - Create test users with Student/Recruiter/Admin roles" -ForegroundColor White
Write-Host "   - Assign app roles via Enterprise Applications" -ForegroundColor White
Write-Host ""
