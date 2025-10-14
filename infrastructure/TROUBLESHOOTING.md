# Deployment Troubleshooting Guide

## Common Issues and Solutions

### 1. "Could not find a part of the path" Error

**Problem**: The deployment script can't find the Bicep files.

**Solution**:
```powershell
# Make sure you're in the infrastructure directory
cd infrastructure

# Run the script from there
.\deploy.ps1
# OR
.\quick-start.ps1
```

**Alternative**: Run from the root solution directory:
```powershell
# From C:\MAUI Applications\StudentLinkApi_Sln\
.\infrastructure\deploy.ps1
```

---

### 2. "Not logged in to Azure" Error

**Problem**: Azure CLI is not authenticated.

**Solution**:
```powershell
# Login to Azure
az login

# Verify login
az account show

# Set the correct subscription (if you have multiple)
az account list --output table
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

---

### 3. SQL Password Validation Error

**Problem**: SQL password doesn't meet complexity requirements.

**Requirements**:
- Minimum 8 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)
- At least one number (0-9)
- At least one special character (!@#$%^&*)

**Good Example**: `MyP@ssw0rd2025!`

**Solution**:
```powershell
# Edit the deploy.ps1 script and change this line:
$SQL_ADMIN_PASSWORD = "YourStrongP@ssw0rd123!"

# Or when prompted in quick-start.ps1, enter a strong password
```

---

### 4. Resource Already Exists Error

**Problem**: Resources with the same name already exist.

**Solution Option 1 - Use existing resources**:
```powershell
# Skip deployment, just use the existing resources
# Get their connection strings from Azure Portal
```

**Solution Option 2 - Delete and redeploy**:
```powershell
# Delete the resource group
az group delete --name rg-studentlink-dev --yes --no-wait

# Wait a few minutes, then run deployment again
.\deploy.ps1
```

**Solution Option 3 - Change resource names**:
```powershell
# Edit main.parameters.json
{
  "baseName": {
    "value": "studentlink2"  # Change this
  }
}
```

---

### 5. Bicep Compilation Error

**Problem**: Bicep file has syntax errors or Azure CLI can't find Bicep.

**Solution**:
```powershell
# Install/Update Azure CLI Bicep extension
az bicep install
az bicep upgrade

# Validate the Bicep file
az bicep build --file infrastructure/main.bicep

# Check for errors in the output
```

---

### 6. "Subscription not found" Error

**Problem**: Your Azure account doesn't have an active subscription.

**Solution**:
```powershell
# Check your subscriptions
az account list --output table

# If empty, you need to:
# 1. Create a free Azure account at https://azure.microsoft.com/free/
# 2. Or ask your organization admin to grant you access
```

---

### 7. Database Migration Fails

**Problem**: `dotnet ef database update` fails.

**Solution**:
```powershell
# Check if EF Core tools are installed
dotnet ef --version

# Install if missing
dotnet tool install --global dotnet-ef --version 9.*

# Check SQL firewall - add your IP
az sql server firewall-rule create `
    --resource-group rg-studentlink-dev `
    --server YOUR_SQL_SERVER_NAME `
    --name AllowMyIP `
    --start-ip-address YOUR_IP `
    --end-ip-address YOUR_IP

# Run migration with explicit connection string
cd StudentLinkApi
dotnet ef database update --connection "YOUR_CONNECTION_STRING"
```

---

### 8. Key Vault Access Denied

**Problem**: Script can't store secrets in Key Vault.

**Solution**:
```powershell
# Wait longer for RBAC propagation (15-30 seconds)
Start-Sleep -Seconds 30

# Manually assign yourself the role
$userId = az ad signed-in-user show --query id -o tsv
az role assignment create `
    --role "Key Vault Secrets Officer" `
    --assignee $userId `
    --scope "/subscriptions/YOUR_SUB_ID/resourceGroups/rg-studentlink-dev/providers/Microsoft.KeyVault/vaults/YOUR_KEYVAULT_NAME"

# Then manually add secrets
az keyvault secret set --vault-name YOUR_KEYVAULT --name "SqlConnectionString" --value "YOUR_VALUE"
```

---

### 9. Location Not Available Error

**Problem**: "South Africa North" region doesn't support a resource.

**Solution**:
```powershell
# Use West Europe as fallback
# Edit main.parameters.json:
{
  "location": {
    "value": "westeurope"
  }
}

# Or check available locations
az account list-locations --query "[].{Name:name, DisplayName:displayName}" --output table
```

---

### 10. Free Tier Not Available

**Problem**: "Free tier limit exceeded" for App Service.

**Solutions**:

**Option 1 - Delete existing Free tier resources**:
```powershell
# Find existing free tier app services
az resource list --resource-type "Microsoft.Web/sites" --query "[?sku.tier=='Free'].{Name:name, ResourceGroup:resourceGroup}" --output table

# Delete one to free up the quota
az webapp delete --name EXISTING_APP_NAME --resource-group EXISTING_RG
```

**Option 2 - Use Basic tier** (costs ~$13/month):
```bicep
// Edit main.bicep, change:
sku: {
  name: 'B1'  // Changed from F1
  tier: 'Basic'
  capacity: 1
}
```

---

## How to Get Help

### View Deployment Logs
```powershell
# Check Azure deployment status
az deployment group list --resource-group rg-studentlink-dev --output table

# View specific deployment
az deployment group show --resource-group rg-studentlink-dev --name DEPLOYMENT_NAME
```

### Check Application Insights
```powershell
# View recent logs
az monitor app-insights query `
    --app YOUR_APP_INSIGHTS_NAME `
    --analytics-query "traces | order by timestamp desc | take 50"
```

### Manual Deployment Step-by-Step

If automated scripts fail, deploy manually:

1. **Create Resource Group**:
```powershell
az group create --name rg-studentlink-dev --location southafricanorth
```

2. **Deploy Bicep Template**:
```powershell
cd infrastructure
az deployment group create `
    --resource-group rg-studentlink-dev `
    --template-file main.bicep `
    --parameters main.parameters.json `
    --parameters sqlAdminPassword="YourP@ssw0rd123!"
```

3. **Get Outputs**:
```powershell
az deployment group show `
    --resource-group rg-studentlink-dev `
    --name main `
    --query properties.outputs
```

4. **Manually Update appsettings.json** with the values from step 3.

5. **Run Database Migration**:
```powershell
cd StudentLinkApi
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Still Having Issues?

1. **Check Azure Service Health**: https://status.azure.com/
2. **Review Azure quotas**: Azure Portal ? Subscriptions ? Usage + quotas
3. **Verify prerequisites**:
   - Azure CLI installed: `az --version`
   - .NET 9 SDK installed: `dotnet --version`
   - Logged into Azure: `az account show`
   - Active subscription: `az account list`

4. **Enable verbose logging**:
```powershell
# Add --debug flag to any az command
az deployment group create --debug ...
```

5. **Check the deployment outputs file** (if deployment succeeded):
```powershell
cat infrastructure/deployment-outputs.json
# or
cat infrastructure/deployment-info.json
```

---

## Clean Up (Start Fresh)

If you want to delete everything and start over:

```powershell
# Delete the entire resource group (WARNING: Cannot be undone!)
az group delete --name rg-studentlink-dev --yes --no-wait

# Wait 5 minutes, then run quick-start.ps1 again
```

---

## Contact Support

If none of these solutions work:
1. Check the `PHASE1-DEPLOYMENT.md` guide
2. Review Azure documentation: https://docs.microsoft.com/azure/
3. Post on Stack Overflow with tag `azure-bicep`
4. Contact Azure support: https://azure.microsoft.com/support/
