# ?? YOUR NEXT STEPS - Phase 1 Deployment

## ? What's Already Done (Code Complete)

All the code for Phase 1 is complete and ready to deploy! Here's what we've built:

- ? Complete Azure infrastructure templates (Bicep)
- ? Automated deployment scripts
- ? JWT authentication with role-based access
- ? User management API (8 endpoints)
- ? Database models with EF Core 9
- ? Security hardening (Key Vault, HTTPS, JWT)
- ? Comprehensive documentation

**Build Status**: ? Successful

---

## ?? What You Need to Do Now (Manual Steps)

### Step 1: Deploy to Azure (15-20 minutes)

**Option A: Automated (Recommended)**
```powershell
# 1. Open PowerShell in the project directory
cd "C:\MAUI Applications\StudentLinkApi_Sln"

# 2. Login to Azure
az login

# 3. Run the quick start script
cd infrastructure
.\quick-start.ps1

# Follow the prompts:
# - It will check prerequisites
# - Ask for SQL admin password (min 8 chars, uppercase, lowercase, number, special char)
# - Deploy all Azure resources (~5-10 minutes)
# - Configure Key Vault
# - Run database migrations
```

**Option B: Manual Deployment**
```powershell
# If quick-start.ps1 has issues, run step by step:
cd infrastructure
.\deploy.ps1

# Then update appsettings.json manually with the values from:
cat deployment-outputs.json
```

---

### Step 2: Set Up Azure AD B2C (20-30 minutes)

**This must be done manually through Azure Portal**

```powershell
# Run the interactive guide
cd infrastructure
.\setup-b2c.ps1
```

The script will walk you through:
1. Creating B2C tenant at: https://portal.azure.com
2. Setting up user flow (`B2C_1_signupsignin`)
3. Creating API app registration
4. Defining app roles (Student, Recruiter, Admin)
5. Configuring token claims

**Important**: Save the following values:
- B2C Tenant Name (e.g., `studentlink`)
- Domain (e.g., `studentlink.onmicrosoft.com`)
- Tenant ID (GUID)
- API Client ID (GUID)

---

### Step 3: Update Configuration (5 minutes)

Open `StudentLinkApi/appsettings.json` and update the Azure AD B2C section:

```json
{
  "AzureAdB2C": {
    "Instance": "https://YOUR_TENANT_NAME.b2clogin.com",
    "Domain": "YOUR_TENANT_NAME.onmicrosoft.com",
    "TenantId": "PASTE_TENANT_ID_HERE",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ClientId": "PASTE_API_CLIENT_ID_HERE"
  }
}
```

The `quick-start.ps1` script should have already updated:
- ? `ConnectionStrings.DefaultConnection`
- ? `KeyVault.Uri`
- ? `ApplicationInsights.ConnectionString`

---

### Step 4: Test Locally (5 minutes)

```powershell
# Navigate to API project
cd "C:\MAUI Applications\StudentLinkApi_Sln\StudentLinkApi"

# Run the application
dotnet run
```

**Test the health endpoint**:
```powershell
# Should return: {"status":"ok","time":"..."}
curl https://localhost:7XXX/auth/ping
```

Replace `7XXX` with the actual port shown when you run `dotnet run`.

---

### Step 5: Create Test Users in B2C (10 minutes)

1. Go to your B2C tenant in Azure Portal
2. Navigate to **Users** ? **All users**
3. Click **+ New user** ? **Create user**
4. Create test users:
   - **Student User**: test-student@yourdomain.com
   - **Recruiter User**: test-recruiter@yourdomain.com
   - **Admin User**: test-admin@yourdomain.com

5. **Assign Roles**:
   - Go to **Enterprise applications**
   - Find your API app registration
   - Go to **Users and groups**
   - Assign users to their respective roles (Student/Recruiter/Admin)

---

### Step 6: Test Authentication (10 minutes)

You have two options to get a test token:

**Option A: Use Postman/Thunder Client**
1. Install Postman: https://www.postman.com/downloads/
2. Create a new request
3. Configure OAuth 2.0:
   - Auth URL: `https://YOUR_TENANT.b2clogin.com/YOUR_TENANT.onmicrosoft.com/B2C_1_signupsignin/oauth2/v2.0/authorize`
   - Token URL: `https://YOUR_TENANT.b2clogin.com/YOUR_TENANT.onmicrosoft.com/B2C_1_signupsignin/oauth2/v2.0/token`
   - Client ID: `YOUR_API_CLIENT_ID`
   - Scope: `openid profile YOUR_API_CLIENT_ID`

**Option B: Use B2C Test Flow**
1. In Azure Portal, go to B2C tenant
2. Go to **User flows** ? **B2C_1_signupsignin**
3. Click **Run user flow**
4. Select your application
5. Login with test user
6. Copy the token from the URL

**Test the protected endpoints**:
```powershell
# Get current user info (requires token)
curl -X GET https://localhost:7XXX/auth/me `
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Register user in database
curl -X POST https://localhost:7XXX/auth/register `
  -H "Authorization: Bearer YOUR_TOKEN_HERE" `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "role": "Student",
    "firstName": "Test",
    "lastName": "User"
  }'
```

---

### Step 7: Deploy to Azure App Service (Optional - 15 minutes)

```powershell
# 1. Build and publish
cd "C:\MAUI Applications\StudentLinkApi_Sln\StudentLinkApi"
dotnet publish -c Release -o ./publish

# 2. Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# 3. Get your App Service name
# (It's in infrastructure/deployment-info.json or deployment-outputs.json)
$APP_NAME = "studentlink-dev-XXXXX-identity-api"

# 4. Deploy to Azure
az webapp deployment source config-zip `
    --resource-group rg-studentlink-dev `
    --name $APP_NAME `
    --src ./publish.zip

# 5. Test the deployed API
curl https://$APP_NAME.azurewebsites.net/auth/ping
```

---

## ?? Success Checklist

Mark these off as you complete them:

- [ ] Ran `az login` successfully
- [ ] Ran `.\quick-start.ps1` without errors
- [ ] Azure resources deployed (SQL, Storage, Key Vault, etc.)
- [ ] Created Azure AD B2C tenant
- [ ] Created user flow (`B2C_1_signupsignin`)
- [ ] Created API app registration
- [ ] Defined app roles (Student, Recruiter, Admin)
- [ ] Updated `appsettings.json` with B2C configuration
- [ ] Ran `dotnet run` successfully
- [ ] Tested `/auth/ping` endpoint
- [ ] Created test users in B2C
- [ ] Assigned roles to test users
- [ ] Obtained B2C token
- [ ] Tested `/auth/me` with token
- [ ] Registered a user via `/auth/register`
- [ ] Verified data in SQL database

**Once all checked**: ? Phase 1 is complete!

---

## ?? If Something Goes Wrong

### Deployment Failed?
Check: [infrastructure/TROUBLESHOOTING.md](infrastructure/TROUBLESHOOTING.md)

Common fixes:
```powershell
# Issue: "Could not find path"
# Fix: Make sure you're in the infrastructure directory
cd infrastructure
.\quick-start.ps1

# Issue: "Not logged in"
# Fix: Login to Azure
az login

# Issue: "SQL password too weak"
# Fix: Use a strong password like: MyP@ssw0rd2025!
```

### Can't Get B2C Token?
1. Verify the Authority URL matches your B2C tenant
2. Check Client ID is correct
3. Ensure user flow is named `B2C_1_signupsignin`
4. Make sure test user exists and has assigned role

### Database Connection Failed?
```powershell
# Add your IP to SQL firewall
az sql server firewall-rule create `
    --resource-group rg-studentlink-dev `
    --server YOUR_SQL_SERVER_NAME `
    --name AllowMyIP `
    --start-ip-address YOUR_IP `
    --end-ip-address YOUR_IP
```

---

## ?? Documentation Reference

- **[README.md](../README.md)** - Project overview
- **[PHASE1-DEPLOYMENT.md](../PHASE1-DEPLOYMENT.md)** - Detailed deployment guide
- **[QUICK-REFERENCE.md](../QUICK-REFERENCE.md)** - Command reference
- **[infrastructure/TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Troubleshooting guide

---

## ?? Tips

1. **Use PowerShell** (not Command Prompt) for all commands
2. **Keep the Terminal open** when running deployment (takes 5-10 mins)
3. **Save your passwords** in a secure location (e.g., password manager)
4. **Take screenshots** of B2C configuration for reference
5. **Test locally first** before deploying to Azure

---

## ?? After Phase 1 is Complete

You'll have:
- ? Fully functional authentication system
- ? User management API
- ? Azure infrastructure deployed
- ? Database with User and Profile tables
- ? Monitoring with Application Insights
- ? Secure secrets management with Key Vault

**Total Cost**: ~$5-10/month

**Ready for Phase 2**: CV upload and parsing! ??

---

## ?? Need Help?

1. Check the troubleshooting guide
2. Review Application Insights logs
3. Verify Azure Portal resources are running
4. Check the deployment output files:
   - `infrastructure/deployment-outputs.json`
   - `infrastructure/deployment-info.json`

---

**Good luck with your deployment!** ??

If everything works, you'll see:
- ? Green checkmarks in the terminal
- ? Health endpoint responding at `/auth/ping`
- ? User data being saved to SQL database
- ? JWT tokens validating correctly

**Time to complete**: ~1-2 hours (first time)
