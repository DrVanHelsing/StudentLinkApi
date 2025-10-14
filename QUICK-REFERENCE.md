# ?? StudentLink Platform - Quick Reference Card

## ?? Essential Commands

### Local Development
```powershell
# Run the API
cd StudentLinkApi
dotnet run

# Run with hot reload
dotnet watch run

# Build
dotnet build

# Test endpoints
curl https://localhost:7XXX/auth/ping
```

### Database Operations
```powershell
# Install EF Core tools (one-time)
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add MigrationName --context ApplicationDbContext

# Apply migrations
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# View SQL for migration
dotnet ef migrations script
```

### Azure Deployment
```powershell
# Deploy infrastructure (one-time)
cd infrastructure
.\quick-start.ps1

# Or manually:
az login
az group create --name rg-studentlink-dev --location southafricanorth
az deployment group create --resource-group rg-studentlink-dev --template-file main.bicep --parameters main.parameters.json

# Deploy application
cd StudentLinkApi
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az webapp deployment source config-zip --resource-group rg-studentlink-dev --name YOUR_APP_NAME --src ./publish.zip
```

---

## ?? Key URLs & Resources

### Local Development
- API: `https://localhost:7XXX`
- Health Check: `https://localhost:7XXX/auth/ping`
- OpenAPI: `https://localhost:7XXX/openapi/v1.json`

### Azure Resources
- Azure Portal: https://portal.azure.com
- Resource Group: `rg-studentlink-dev`
- Region: South Africa North

### Documentation
- Project README: `README.md`
- Deployment Guide: `PHASE1-DEPLOYMENT.md`
- Task Checklist: `PHASE1-CHECKLIST.md`
- Summary: `PHASE1-SUMMARY.md`

---

## ?? Configuration Keys

### appsettings.json Structure
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "SQL_CONNECTION_STRING"
  },
  "KeyVault": {
    "Uri": "https://YOUR_KEYVAULT.vault.azure.net/"
  },
  "ApplicationInsights": {
    "ConnectionString": "YOUR_APPINSIGHTS_CONNECTION_STRING"
  },
  "AzureAdB2C": {
    "Instance": "https://YOUR_TENANT.b2clogin.com",
    "Domain": "YOUR_TENANT.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ClientId": "YOUR_API_CLIENT_ID"
  }
}
```

---

## ?? API Endpoints Reference

### Public
| Endpoint       | Method | Description  |
|----------------|--------|--------------|
| `/auth/ping`   | GET    | Health check |

### Authenticated (Any Role)
| Endpoint              | Method | Body                      | Description         |
|-----------------------|--------|---------------------------|---------------------|
| `/auth/me`            | GET    | -                         | Current user info   |
| `/auth/register`      | POST   | CreateUserDto             | Register user       |
| `/auth/profile/{id}`  | GET    | -                         | Get user profile    |
| `/auth/profile`       | PUT    | UpdateUserDto             | Update profile      |

### Role-Based
| Endpoint              | Method | Required Role | Description      |
|-----------------------|--------|---------------|------------------|
| `/auth/me/student`    | GET    | Student       | Test access      |
| `/auth/me/recruiter`  | GET    | Recruiter     | Test access      |
| `/auth/me/admin`      | GET    | Admin         | Test access      |

---

## ?? Request/Response Examples

### Register User
```bash
POST /auth/register
Authorization: Bearer YOUR_B2C_TOKEN
Content-Type: application/json

{
  "email": "john@example.com",
  "role": "Student",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+27821234567"
}

# Response: 201 Created
{
  "id": "guid...",
  "email": "john@example.com",
  "role": "Student",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+27821234567",
  "createdAt": "2025-01-14T10:30:00Z"
}
```

### Update Profile
```bash
PUT /auth/profile
Authorization: Bearer YOUR_B2C_TOKEN
Content-Type: application/json

{
  "firstName": "Johnny",
  "phoneNumber": "+27829999999"
}

# Response: 200 OK
{
  "id": "guid...",
  "email": "john@example.com",
  "role": "Student",
  "firstName": "Johnny",
  "lastName": "Doe",
  "phoneNumber": "+27829999999",
  "createdAt": "2025-01-14T10:30:00Z"
}
```

---

## ?? Testing with curl

```bash
# Health check (no auth)
curl -X GET https://localhost:7XXX/auth/ping

# Get current user (with auth)
curl -X GET https://localhost:7XXX/auth/me \
  -H "Authorization: Bearer YOUR_TOKEN"

# Register user
curl -X POST https://localhost:7XXX/auth/register \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "role": "Student",
    "firstName": "Test",
    "lastName": "User"
  }'

# Update profile
curl -X PUT https://localhost:7XXX/auth/profile \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Updated",
    "phoneNumber": "+27821234567"
  }'
```

---

## ?? Troubleshooting Quick Fixes

### Build Errors
```powershell
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Database Issues
```powershell
# Reset local database
dotnet ef database drop --force
dotnet ef database update

# Check connection string
dotnet user-secrets list
```

### Azure Deployment Issues
```powershell
# Check App Service logs
az webapp log tail --name YOUR_APP_NAME --resource-group rg-studentlink-dev

# Restart App Service
az webapp restart --name YOUR_APP_NAME --resource-group rg-studentlink-dev

# Check deployment status
az webapp deployment list-publishing-credentials --name YOUR_APP_NAME --resource-group rg-studentlink-dev
```

### Authentication Issues
- Verify B2C tenant name matches `appsettings.json`
- Check ClientId is correct API app registration ID
- Ensure token includes `aud` claim matching ClientId
- Verify user flow name is `B2C_1_signupsignin`

---

## ?? Database Schema Quick Reference

### Users Table
```
Id (PK)          - UNIQUEIDENTIFIER
B2CObjectId      - NVARCHAR(100) UNIQUE
Email            - NVARCHAR(256)
Role             - NVARCHAR(50) [Student, Recruiter, Admin]
FirstName        - NVARCHAR(100)
LastName         - NVARCHAR(100)
PhoneNumber      - NVARCHAR(20)
CreatedAt        - DATETIME2
UpdatedAt        - DATETIME2
IsActive         - BIT
```

### Profiles Table
```
Id (PK)          - UNIQUEIDENTIFIER
UserId (FK)      - UNIQUEIDENTIFIER UNIQUE
Summary          - NVARCHAR(500)
Skills           - NVARCHAR(MAX) [JSON]
Education        - NVARCHAR(MAX) [JSON]
Experience       - NVARCHAR(MAX) [JSON]
CvUrl            - NVARCHAR(500)
LinkedInUrl      - NVARCHAR(100)
GitHubUrl        - NVARCHAR(100)
PortfolioUrl     - NVARCHAR(100)
CreatedAt        - DATETIME2
UpdatedAt        - DATETIME2
```

---

## ?? Azure Resource Names (Pattern)

```
Resource Group:   rg-studentlink-{env}
SQL Server:       studentlink-{env}-{uniqueid}-sql
SQL Database:     studentlinkdb
Storage Account:  studentlink{env}{uniqueid}storage
Service Bus:      studentlink-{env}-{uniqueid}-sb
Key Vault:        studentlink-{env}-{uniqueid}-kv
App Insights:     studentlink-{env}-{uniqueid}-ai
App Service:      studentlink-{env}-{uniqueid}-identity-api
```

---

## ?? Cost Breakdown

| Service           | Tier     | Monthly Cost |
|-------------------|----------|--------------|
| SQL Database      | Basic    | ~$5          |
| App Service       | Free F1  | $0           |
| Storage           | LRS      | <$1          |
| Service Bus       | Basic    | $0.05        |
| Key Vault         | Standard | $0.03        |
| App Insights      | Free     | $0           |
| **Total**         |          | **~$5-10**   |

---

## ?? Support Contacts

- Documentation: `README.md`, `PHASE1-DEPLOYMENT.md`
- Issues: Check Application Insights logs
- Azure Support: Azure Portal ? Support + troubleshooting

---

## ? Hot Tips

1. **Use User Secrets in Development**
   ```powershell
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
   ```

2. **Enable Detailed Errors in Development**
   ```csharp
   app.UseDeveloperExceptionPage();
   ```

3. **Monitor Costs**
   ```powershell
   az consumption usage list --start-date 2025-01-01
   ```

4. **Quick SQL Firewall Update**
   ```powershell
   az sql server firewall-rule create --resource-group rg-studentlink-dev --server YOUR_SQL_SERVER --name AllowMyIP --start-ip-address YOUR_IP --end-ip-address YOUR_IP
   ```

---

**Version:** Phase 1 | **Last Updated:** 2025-01-14
