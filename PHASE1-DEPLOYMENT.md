# Phase 1 Deployment Guide - StudentLink Platform

## ?? Overview
This guide covers the complete setup of Phase 1: Identity & Core Infrastructure.

## ?? Prerequisites

1. **Azure CLI** - [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
   ```powershell
   # Verify installation
   az --version
   ```

2. **Azure Account** with active subscription
   ```powershell
   az login
   az account list --output table
   az account set --subscription "YOUR_SUBSCRIPTION_ID"
   ```

3. **.NET 9 SDK** - [Download .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0)
   ```powershell
   dotnet --version  # Should show 9.x.x
   ```

4. **SQL Server Management Studio (SSMS)** or **Azure Data Studio** (optional, for database management)

---

## ?? Deployment Steps

### Step 1: Deploy Azure Infrastructure

1. **Navigate to infrastructure directory**:
   ```powershell
   cd infrastructure
   ```

2. **Edit `main.parameters.json`**:
   - Update `sqlAdminPassword` with a strong password (min 8 chars, uppercase, lowercase, number, special char)
   - Keep `baseName` as `studentlink` or customize it

3. **Run deployment script**:
   ```powershell
   # Windows PowerShell
   .\deploy.ps1
   
   # Or manually:
   $RESOURCE_GROUP = "rg-studentlink-dev"
   $LOCATION = "southafricanorth"
   
   az group create --name $RESOURCE_GROUP --location $LOCATION
   
   az deployment group create `
       --resource-group $RESOURCE_GROUP `
       --template-file main.bicep `
       --parameters main.parameters.json `
       --parameters sqlAdminPassword="YourStrongP@ssw0rd123!"
   ```

4. **Wait for deployment** (5-10 minutes)

5. **Save the outputs** - the script will save connection strings to `deployment-outputs.json`

---

### Step 2: Set Up Azure AD B2C

1. **Run the B2C setup guide**:
   ```powershell
   .\setup-b2c.ps1
   ```

2. **Follow the interactive prompts** to create:
   - B2C Tenant
   - User Flow (B2C_1_signupsignin)
   - API App Registration
   - App Roles (Student, Recruiter, Admin)

3. **Save the configuration** - outputs will be in `b2c-config.json`

---

### Step 3: Configure Application Settings

1. **Update `appsettings.json`** with Azure resources:

   From `deployment-outputs.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "YOUR_SQL_CONNECTION_STRING"
     },
     "KeyVault": {
       "Uri": "https://YOUR_KEY_VAULT_NAME.vault.azure.net/"
     },
     "ApplicationInsights": {
       "ConnectionString": "YOUR_APP_INSIGHTS_CONNECTION_STRING"
     }
   }
   ```

   From `b2c-config.json`:
   ```json
   {
     "AzureAdB2C": {
       "Instance": "https://YOUR_TENANT.b2clogin.com",
       "Domain": "YOUR_TENANT.onmicrosoft.com",
       "TenantId": "YOUR_TENANT_ID",
       "SignUpSignInPolicyId": "B2C_1_signupsignin",
       "ClientId": "YOUR_API_CLIENT_ID"
     }
   }
   ```

2. **For production**, use `appsettings.Production.json` and reference Key Vault.

---

### Step 4: Database Setup

1. **Install EF Core CLI tools** (if not already installed):
   ```powershell
   dotnet tool install --global dotnet-ef
   ```

2. **Navigate to project directory**:
   ```powershell
   cd StudentLinkApi
   ```

3. **Create initial migration**:
   ```powershell
   dotnet ef migrations add InitialCreate --context ApplicationDbContext
   ```

4. **Apply migration to database**:
   ```powershell
   # Local development
   dotnet ef database update --context ApplicationDbContext
   
   # For Azure SQL (update connection string first)
   dotnet ef database update --context ApplicationDbContext --connection "YOUR_AZURE_SQL_CONNECTION_STRING"
   ```

5. **Verify database**:
   - Connect to SQL Server with SSMS/Azure Data Studio
   - Check that `Users` and `Profiles` tables exist

---

### Step 5: Test Locally

1. **Run the application**:
   ```powershell
   dotnet run --project StudentLinkApi
   ```

2. **Test endpoints**:
   ```powershell
   # Health check (anonymous)
   curl https://localhost:7XXX/auth/ping
   
   # Get current user (requires auth token)
   curl https://localhost:7XXX/auth/me -H "Authorization: Bearer YOUR_B2C_TOKEN"
   ```

3. **Access OpenAPI documentation**:
   - Navigate to: `https://localhost:7XXX/openapi/v1.json`
   - Use Swagger UI or Postman to test

---

### Step 6: Deploy to Azure App Service

1. **Build and publish**:
   ```powershell
   cd StudentLinkApi
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy to App Service**:
   ```powershell
   # Get App Service name from deployment outputs
   $APP_NAME = "studentlink-dev-XXXXX-identity-api"
   $RESOURCE_GROUP = "rg-studentlink-dev"
   
   # Zip the publish folder
   Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
   
   # Deploy
   az webapp deployment source config-zip `
       --resource-group $RESOURCE_GROUP `
       --name $APP_NAME `
       --src ./publish.zip
   ```

3. **Configure App Service settings**:
   ```powershell
   # Add connection string
   az webapp config connection-string set `
       --resource-group $RESOURCE_GROUP `
       --name $APP_NAME `
       --connection-string-type SQLAzure `
       --settings DefaultConnection="YOUR_SQL_CONNECTION_STRING"
   
   # Add Key Vault reference (already configured via Bicep)
   # Secrets are automatically loaded from Key Vault using Managed Identity
   ```

4. **Run database migrations on Azure SQL**:
   ```powershell
   # Option 1: From local machine (ensure firewall allows your IP)
   dotnet ef database update --connection "YOUR_AZURE_SQL_CONNECTION_STRING"
   
   # Option 2: Use Azure Cloud Shell or Azure Data Studio
   ```

---

## ?? Testing Authentication

### Get a B2C Token (for testing)

1. **Install Postman** or use **Thunder Client** (VS Code extension)

2. **Configure OAuth 2.0 Authorization**:
   - Auth URL: `https://YOUR_TENANT.b2clogin.com/YOUR_TENANT.onmicrosoft.com/B2C_1_signupsignin/oauth2/v2.0/authorize`
   - Token URL: `https://YOUR_TENANT.b2clogin.com/YOUR_TENANT.onmicrosoft.com/B2C_1_signupsignin/oauth2/v2.0/token`
   - Client ID: YOUR_API_CLIENT_ID
   - Scope: `openid profile YOUR_API_CLIENT_ID`

3. **Test endpoints**:
   ```bash
   GET /auth/ping          # Should return 200 OK (no auth required)
   GET /auth/me            # Should return user claims (requires token)
   POST /auth/register     # Create user in database
   PUT /auth/profile       # Update user profile
   GET /auth/me/student    # Test role-based access
   ```

---

## ?? Verify Deployment

### Check Azure Resources

```powershell
$RESOURCE_GROUP = "rg-studentlink-dev"

# List all resources
az resource list --resource-group $RESOURCE_GROUP --output table

# Check App Service status
az webapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query "state"

# Check SQL Database
az sql db show --name studentlinkdb --server YOUR_SQL_SERVER --resource-group $RESOURCE_GROUP
```

### Check Application Insights

1. Go to Azure Portal ? Your App Insights resource
2. Check **Live Metrics** for real-time requests
3. Check **Failures** for any errors
4. Check **Performance** for response times

---

## ?? Security Checklist

- [ ] SQL firewall configured (only Azure services + your IP)
- [ ] Key Vault access policies set (App Service Managed Identity has access)
- [ ] HTTPS enforced on App Service
- [ ] B2C token validation working
- [ ] Role-based authorization configured
- [ ] Connection strings stored in Key Vault (not appsettings)
- [ ] Secrets rotation policy defined

---

## ?? Cost Management

### Current Setup (~$5-10/month):
- **SQL Database (Basic)**: ~$5/month
- **App Service (Free F1)**: $0
- **Storage Account (LRS)**: <$1/month
- **Service Bus (Basic)**: ~$0.05/month
- **Key Vault**: $0.03/secret/month
- **Application Insights**: Free up to 5GB/month

### Monitor costs:
```powershell
az consumption usage list --start-date 2025-01-01 --end-date 2025-01-31
```

---

## ?? Troubleshooting

### Issue: Database connection fails
**Solution**: Check SQL firewall rules, ensure your IP is whitelisted:
```powershell
az sql server firewall-rule create `
    --resource-group $RESOURCE_GROUP `
    --server YOUR_SQL_SERVER `
    --name AllowMyIP `
    --start-ip-address YOUR_IP `
    --end-ip-address YOUR_IP
```

### Issue: B2C token validation fails
**Solution**: 
1. Verify `Authority` URL in `Program.cs` matches B2C tenant
2. Check `ClientId` matches API app registration
3. Ensure token includes `aud` (audience) claim

### Issue: Key Vault access denied
**Solution**: Grant App Service Managed Identity access:
```powershell
$PRINCIPAL_ID = az webapp identity show --name $APP_NAME --resource-group $RESOURCE_GROUP --query principalId -o tsv

az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee $PRINCIPAL_ID `
    --scope "/subscriptions/YOUR_SUB_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/YOUR_KEY_VAULT"
```

---

## ?? Next Steps (Phase 2)

After completing Phase 1:
1. Test authentication end-to-end with a frontend
2. Create test users with different roles (Student, Recruiter, Admin)
3. Proceed to **Phase 2: CV Management & Parsing**
   - Add CV upload endpoints
   - Configure Blob Storage
   - Set up Service Bus queue

---

## ?? Additional Resources

- [Azure AD B2C Documentation](https://docs.microsoft.com/en-us/azure/active-directory-b2c/)
- [Entity Framework Core 9](https://docs.microsoft.com/en-us/ef/core/)
- [Azure App Service](https://docs.microsoft.com/en-us/azure/app-service/)
- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

---

## ?? Support

For issues or questions:
1. Check Application Insights logs
2. Review Azure Portal resource health
3. Consult the deployment outputs: `infrastructure/deployment-outputs.json`
