# ?? CV Upload Feature - Complete Guide

## ?? What's New

Your StudentLink platform now includes complete CV/Resume management!

### ? Backend Features
- **File Upload** - Upload CV (PDF, DOC, DOCX)
- **File Storage** - Local storage (can switch to Azure Blob)
- **File Download** - Download uploaded CVs
- **File Delete** - Remove old CVs
- **CV History** - Track all uploaded versions
- **Validation** - File type and size validation (max 5MB)

### ? Frontend Features
- **Drag & Drop Upload** - Modern upload interface
- **Current CV Display** - View active CV
- **CV History** - See all previous uploads
- **Download CVs** - Download any version
- **Delete CVs** - Remove unwanted files
- **Student-Only Access** - Role-based protection

---

## ?? Setup Instructions

### Step 1: Apply Database Migration

```powershell
cd "C:\MAUI Applications\StudentLinkApi_Sln\infrastructure"
.\add-cv-feature.ps1
```

This will:
- ? Add CV model to database
- ? Create file storage service
- ? Add CV controller
- ? Apply database migration

### Step 2: Restart Your API

```powershell
cd ..\StudentLinkApi
dotnet run
```

### Step 3: Restart Frontend (if running)

The frontend will auto-reload with the new CV upload page!

---

## ?? Testing CV Upload

### Test in Browser

1. **Login as Student**:
   - Email: `student355154426@test.com`
   - Password: `TestPassword123!`

2. **Access CV Upload**:
   - Click "My CV" in navbar
   - Or click "Upload CV" on dashboard

3. **Upload a CV**:
   - Drag & drop a PDF/DOC/DOCX file
   - Or click "Choose File"
   - Max size: 5MB

4. **View/Download/Delete**:
   - See your current CV
   - Download it
   - View upload history
   - Delete if needed

### Test with API (Postman/Swagger)

**Swagger UI**: `https://localhost:7068/swagger`

**Upload CV**:
```
POST /cv/upload
Headers:
  Authorization: Bearer {your-token}
  Content-Type: multipart/form-data
Body:
  file: [select file]
```

**Get Current CV**:
```
GET /cv/current
Headers:
  Authorization: Bearer {your-token}
```

**Download CV**:
```
GET /cv/download/{cvId}
Headers:
  Authorization: Bearer {your-token}
```

---

## ?? New API Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/cv/upload` | POST | ? Student | Upload new CV |
| `/cv/current` | GET | ? Yes | Get active CV |
| `/cv/history` | GET | ? Yes | Get all CVs |
| `/cv/download/{id}` | GET | ? Yes | Download CV |
| `/cv/{id}` | DELETE | ? Yes | Delete CV |

---

## ?? File Storage

### Local Storage (Default)
Files stored in: `wwwroot/uploads/cvs/`

### Switch to Azure Blob Storage

1. **Update appsettings.json**:
```json
{
  "FileStorage": {
    "UseAzure": true
  },
  "Azure": {
    "BlobStorage": {
      "ConnectionString": "your-connection-string",
      "ContainerName": "cvs"
    }
  }
}
```

2. **Restart API**

---

## ?? Frontend Components

### CVUploadPage Features:
- ? Drag & drop upload zone
- ? File type validation
- ? File size validation
- ? Upload progress
- ? Current CV display
- ? CV history with download
- ? Delete functionality
- ? Beautiful UI with Tailwind

### Navigation:
- **Navbar**: "My CV" link (Students only)
- **Dashboard**: "Upload CV" quick action

---

## ?? Security Features

- ? **Authentication Required** - Must be logged in
- ? **Role-Based Access** - Students only
- ? **File Type Validation** - Only PDF, DOC, DOCX
- ? **File Size Limit** - Max 5MB
- ? **User Isolation** - Can only access own CVs
- ? **Secure Storage** - Unique filenames (GUID)

---

## ?? Database Schema

```sql
CREATE TABLE CVs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FileUrl NVARCHAR(500) NOT NULL,
    FileType NVARCHAR(50) NOT NULL,
    FileSize BIGINT NOT NULL,
    UploadedAt DATETIME NOT NULL,
    UpdatedAt DATETIME,
    IsActive BIT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

---

## ?? Usage Examples

### Student Workflow:
1. Login to StudentLink
2. Go to Dashboard
3. Click "Upload CV"
4. Drag & drop or select CV file
5. File uploads instantly
6. View/download/delete as needed

### Recruiter View (Phase 3):
- Browse student profiles
- Download student CVs
- Filter by qualifications

---

## ?? Troubleshooting

### Upload Fails
**Problem**: "Failed to upload CV"

**Solutions**:
- Check file size (must be < 5MB)
- Check file type (PDF, DOC, DOCX only)
- Ensure you're logged in
- Check API is running

### Can't See CV
**Problem**: CV doesn't appear after upload

**Solutions**:
- Refresh the page
- Check API logs
- Verify database migration applied

### Download Fails
**Problem**: Can't download CV

**Solutions**:
- Check file exists in `wwwroot/uploads/cvs/`
- Verify CV ID is correct
- Check API logs for errors

---

## ?? Next Enhancements

### Phase 3 Features:
- [ ] CV parsing (extract text, skills, etc.)
- [ ] PDF preview
- [ ] CV templates
- [ ] Auto-fill profile from CV
- [ ] CV analytics (views, downloads)
- [ ] Share CV with recruiters
- [ ] CV versions/drafts

---

## ?? Success!

You now have a fully functional CV management system!

**What You Can Do:**
- ? Upload CVs (multiple formats)
- ? Store CVs securely
- ? View CV history
- ? Download any version
- ? Delete unwanted CVs
- ? Student-only access
- ? Beautiful drag & drop UI

**Your Platform Now Has:**
- ? Authentication ?
- ? User Management ?
- ? Role-Based Access ?
- ? Profile Management ?
- ? **CV Upload** ?

**Ready for Phase 3!** ??