# ?? Azure AD B2C Quick Setup Guide

## ?? Time Required: 20-30 minutes

Follow these steps **in order** in the Azure Portal.

---

## ?? Step 1: Create B2C Tenant (5 mins)

1. Go to: https://portal.azure.com
2. Search for **"Azure AD B2C"**
3. Click **"Create a B2C Tenant"**
4. Fill in:
   - **Organization name**: `StudentLink`
   - **Initial domain name**: `studentlink` (or any available name like `studentlink2024`)
   - **Country/Region**: `South Africa`
   - **Subscription**: Your subscription
   - **Resource Group**: `rg-studentlink-proj` (use existing)
5. Click **"Review + create"** ? **"Create"**
6. Wait 2-3 minutes for creation
7. Click **"Switch to new tenant"** when ready

---

## ?? Step 2: Create User Flow (5 mins)

1. In the B2C tenant, click **"User flows"** in the left menu
2. Click **"+ New user flow"**
3. Select **"Sign up and sign in"**
4. Version: **"Recommended"**
5. Name: `signupsignin` (it will become `B2C_1_signupsignin`)
6. **Identity providers**: Check ? **"Email signup"**
7. **User attributes and token claims**:
   - Check ? **Email Address**
   - Check ? **Display Name**
   - Check ? **Given Name**
   - Check ? **Surname**
   - Check ? **User's Object ID**
8. Click **"Create"**

---

## ?? Step 3: Register API Application (5 mins)

1. In B2C tenant, go to **"App registrations"**
2. Click **"+ New registration"**
3. Fill in:
   - **Name**: `StudentLink API`
   - **Supported account types**: Select **"Accounts in this organizational directory only"**
   - **Redirect URI**: Leave blank
4. Click **"Register"**
5. **SAVE THIS**: Copy the **Application (client) ID** (it's a GUID)
   - Example: `12345678-1234-1234-1234-123456789abc`

---

## ?? Step 4: Expose API Scope (3 mins)

1. Still in the **StudentLink API** app registration
2. Click **"Expose an API"** in left menu
3. Click **"Add a scope"**
4. Click **"Save and continue"** (accepts default Application ID URI)
5. Fill in:
   - **Scope name**: `user_impersonation`
   - **Admin consent display name**: `Access StudentLink API`
   - **Admin consent description**: `Allows access to StudentLink API`
   - **User consent display name**: `Access StudentLink API`
   - **User consent description**: `Allows access to StudentLink API`
   - **State**: ? **Enabled**
6. Click **"Add scope"**

---

## ?? Step 5: Add App Roles (5 mins)

1. Still in **StudentLink API** app registration
2. Click **"App roles"** in left menu
3. Click **"+ Create app role"** (repeat 3 times)

### Role 1: Student
- **Display name**: `Student`
- **Allowed member types**: ? **Users/Groups**
- **Value**: `Student`
- **Description**: `Student user with profile access`
- **Enable this app role**: ? Checked
- Click **"Apply"**

### Role 2: Recruiter
- **Display name**: `Recruiter`
- **Allowed member types**: ? **Users/Groups**
- **Value**: `Recruiter`
- **Description**: `Recruiter with candidate search access`
- **Enable this app role**: ? Checked
- Click **"Apply"**

### Role 3: Admin
- **Display name**: `Admin`
- **Allowed member types**: ? **Users/Groups**
- **Value**: `Admin`
- **Description**: `System administrator`
- **Enable this app role**: ? Checked
- Click **"Apply"**

---

## ?? Step 6: Configure Token Claims (3 mins)

1. Still in **StudentLink API** app registration
2. Click **"Token configuration"** in left menu
3. Click **"+ Add optional claim"**
4. Token type: Select ? **ID** and ? **Access**
5. Check:
   - ? `email`
   - ? `family_name`
   - ? `given_name`
6. Click **"Add"**
7. If prompted about Microsoft Graph permissions, click **"Yes"**

---

## ?? Step 7: Get Tenant ID (1 min)

1. In B2C tenant, click **"Overview"** in left menu
2. **SAVE THIS**: Copy the **Tenant ID** (it's a GUID)
   - Example: `abcd1234-5678-90ab-cdef-1234567890ab`

---

## ? Configuration Values You Need

After completing the steps above, you should have:

1. **Tenant Name**: `studentlink` (or whatever you chose)
2. **Domain**: `studentlink.onmicrosoft.com`
3. **Tenant ID**: `[GUID from Step 7]`
4. **API Client ID**: `[GUID from Step 3]`
5. **Instance**: `https://studentlink.b2clogin.com`
6. **Policy**: `B2C_1_signupsignin`

---

## ?? Update Your appsettings.json

Open `StudentLinkApi/appsettings.json` and update the B2C section:

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

Replace:
- `YOUR_TENANT_NAME` with your tenant name
- `PASTE_TENANT_ID_HERE` with the Tenant ID from Step 7
- `PASTE_API_CLIENT_ID_HERE` with the Client ID from Step 3

---

## ?? Test Setup (Optional - 5 mins)

### Create a Test User

1. In B2C tenant, click **"Users"** ? **"All users"**
2. Click **"+ New user"** ? **"Create user"**
3. Fill in:
   - **User name**: `teststudent@studentlink.onmicrosoft.com`
   - **Name**: `Test Student`
   - **Password**: Create a strong password
4. Click **"Create"**

### Assign Role to Test User

1. In B2C tenant, go to **"Enterprise applications"**
2. Find and click **"StudentLink API"**
3. Click **"Users and groups"** ? **"+ Add user/group"**
4. Select the test user
5. Click **"Select role"** ? Choose **"Student"**
6. Click **"Assign"**

---

## ? Verification

After updating `appsettings.json`, restart your API and test:

```powershell
# Restart the API
cd ..\StudentLinkApi
dotnet run
```

You can now use the B2C login flow to get tokens and test your protected endpoints!

---

## ?? Troubleshooting

**Can't find B2C tenant after creation?**
- Click your profile in top-right ? "Switch directory"
- Select your B2C tenant

**Role assignments not working?**
- Wait 5-10 minutes for role propagation
- Sign out and sign in again

**Token validation fails?**
- Verify Tenant ID and Client ID are correct
- Check that Instance URL matches your tenant name
- Ensure SignUpSignInPolicyId is exactly `B2C_1_signupsignin`

---

**Once complete, your authentication system is fully functional!** ??