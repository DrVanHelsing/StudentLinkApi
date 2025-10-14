# ?? Custom JWT Authentication - Testing Guide

## ? What Changed

We've switched from Azure AD B2C to a **custom JWT authentication system** that you fully control.

**Benefits:**
- ? No external dependencies
- ? Works with any subscription
- ? Full control over users and roles
- ? Secure password hashing with BCrypt
- ? JWT tokens for stateless authentication

---

## ?? Setup (Run Once)

### 1. Install Packages & Update Database

```powershell
cd infrastructure
.\setup-custom-auth.ps1
```

This will:
- Install BCrypt and JWT packages
- Create new database migration
- Update database schema

---

## ?? Testing Your API

### 1. Start the API

```powershell
cd ..\StudentLinkApi
dotnet run
```

Expected output:
```
Now listening on: https://localhost:7068
```

---

### 2. Test Endpoints with cURL or Postman

## ?? **Endpoint 1: Health Check (No Auth Required)**

```powershell
curl https://localhost:7068/auth/ping
```

**Expected Response:**
```json
{"status":"ok","time":"2025-01-14T..."}
```

---

## ?? **Endpoint 2: Register a New User**

```powershell
curl -X POST https://localhost:7068/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "student@test.com",
    "password": "Password123!",
    "role": "Student",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+27821234567"
  }'
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "guid-here",
    "email": "student@test.com",
    "role": "Student",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+27821234567",
    "createdAt": "2025-01-14T..."
  },
  "expiresAt": "2025-01-14T..."
}
```

**? Save the `token` value - you'll need it for protected endpoints!**

---

## ?? **Endpoint 3: Login**

```powershell
curl -X POST https://localhost:7068/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "student@test.com",
    "password": "Password123!"
  }'
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {...},
  "expiresAt": "..."
}
```

---

## ??? **Endpoint 4: Get Current User (Requires Token)**

```powershell
# Replace YOUR_TOKEN with the token from login/register
curl -X GET https://localhost:7068/auth/me `
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected Response:**
```json
{
  "id": "guid",
  "email": "student@test.com",
  "role": "Student",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+27821234567",
  "createdAt": "...",
  "profile": null
}
```

---

## ?? **Endpoint 5: Update Profile**

```powershell
curl -X PUT https://localhost:7068/auth/profile `
  -H "Authorization: Bearer YOUR_TOKEN" `
  -H "Content-Type: application/json" `
  -d '{
    "firstName": "Jane",
    "lastName": "Smith",
    "phoneNumber": "+27829876543"
  }'
```

---

## ?? **Endpoint 6: Role-Based Access**

### Student Role (? Will Work):
```powershell
curl -X GET https://localhost:7068/auth/me/student `
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Recruiter Role (? Will Fail - Forbidden):
```powershell
curl -X GET https://localhost:7068/auth/me/recruiter `
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected Response:**
```json
{
  "message": "This endpoint is accessible to Students only",
  "user": "student@test.com"
}
```

---

## ?? Create Users with Different Roles

### Register a Recruiter:
```powershell
curl -X POST https://localhost:7068/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "recruiter@test.com",
    "password": "Password123!",
    "role": "Recruiter",
    "firstName": "Sarah",
    "lastName": "Recruiter"
  }'
```

### Register an Admin:
```powershell
curl -X POST https://localhost:7068/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "admin@test.com",
    "password": "Password123!",
    "role": "Admin",
    "firstName": "Admin",
    "lastName": "User"
  }'
```

---

## ?? Using Postman (Recommended)

1. **Download Postman**: https://www.postman.com/downloads/
2. **Import these requests**:

### Create a Collection:
- **Collection Name**: StudentLink API
- **Base URL**: `https://localhost:7068`

### Add Requests:

**1. Register**
- Method: `POST`
- URL: `{{baseUrl}}/auth/register`
- Body (JSON):
```json
{
  "email": "test@example.com",
  "password": "Password123!",
  "role": "Student",
  "firstName": "Test",
  "lastName": "User"
}
```

**2. Login**
- Method: `POST`
- URL: `{{baseUrl}}/auth/login`
- Body (JSON):
```json
{
  "email": "test@example.com",
  "password": "Password123!"
}
```

**3. Get Current User**
- Method: `GET`
- URL: `{{baseUrl}}/auth/me`
- Headers:
  - `Authorization`: `Bearer {{token}}`

**4. Update Profile**
- Method: `PUT`
- URL: `{{baseUrl}}/auth/profile`
- Headers:
  - `Authorization`: `Bearer {{token}}`
- Body (JSON):
```json
{
  "firstName": "Updated",
  "lastName": "Name"
}
```

---

## ?? Debugging Tips

### Check Database:
```sql
-- View all users
SELECT Id, Email, Role, FirstName, LastName, CreatedAt 
FROM Users;

-- View user with profiles
SELECT u.Email, u.Role, p.Summary
FROM Users u
LEFT JOIN Profiles p ON u.Id = p.UserId;
```

### Check JWT Token:
Paste your token at https://jwt.io to decode and inspect claims.

---

## ?? Configuration

### Change Token Expiration:
Edit `appsettings.json`:
```json
{
  "JwtSettings": {
    "ExpirationMinutes": 120
  }
}
```

### Change JWT Secret (Production):
```json
{
  "JwtSettings": {
    "SecretKey": "YourProductionSecretKeyHere_MustBe32+Characters"
  }
}
```

---

## ? Success Checklist

- [ ] API runs without errors
- [ ] Can register new users
- [ ] Can login with email/password
- [ ] Receive JWT token on login
- [ ] Can access `/auth/me` with token
- [ ] Role-based endpoints work correctly
- [ ] Can update profile
- [ ] Passwords are hashed (check database)

---

## ?? You're Ready!

Your authentication system is now fully functional with:
- ? User registration
- ? Secure password storage (BCrypt)
- ? JWT token-based authentication
- ? Role-based authorization
- ? Profile management

**Next Steps:**
- Build your frontend and connect to these endpoints
- Add password reset functionality
- Implement email verification
- Deploy to Azure App Service

---

## ?? Common Issues

**"Invalid token" error:**
- Token may have expired (default: 60 minutes)
- Get a new token by logging in again

**"Unauthorized" when accessing protected endpoints:**
- Make sure you're including the `Authorization: Bearer TOKEN` header
- Check that the token hasn't expired

**Database errors:**
- Run: `dotnet ef database update`
- Check connection string in appsettings.json

---

**Happy coding!** ??