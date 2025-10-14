# ?? AI-Powered CV Processing - Complete Implementation Guide

## ?? Overview

Your StudentLink platform now includes an **intelligent CV processing pipeline** powered by Azure AI:

```
Student uploads CV 
    ?
Azure Form Recognizer extracts text
    ?
Azure OpenAI analyzes quality
    ?
Feedback provided to student
    ?
Student improves & re-uploads
    ?
LOOP until satisfactory
    ?
Profile auto-populated
    ?
AI matches with jobs
```

---

## ?? Quick Start (2 Options)

### **Option A: Full AI Pipeline (Requires Azure)**

```powershell
# 1. Setup Azure AI services
cd infrastructure
.\setup-azure-ai.ps1

# 2. Apply database migration
.\add-ai-features.ps1

# 3. Restart API
cd ..\StudentLinkApi
dotnet run
```

**Cost**: ~$11-55/month depending on usage

### **Option B: Development Mode (No Azure, No Cost)**

```powershell
# 1. Apply database migration only
cd infrastructure
.\add-ai-features.ps1

# 2. Keep Azure:AI:Enabled = false in appsettings.json

# 3. Start API
cd ..\StudentLinkApi
dotnet run
```

**Cost**: $0 (CVs upload but no AI processing)

---

## ??? Architecture

### **Components**

| Service | Purpose | Required |
|---------|---------|----------|
| **Azure OpenAI** | CV analysis, feedback, job matching | Optional |
| **Azure Form Recognizer** | Text extraction from PDFs | Optional |
| **Azure Service Bus** | Async CV processing queue | Optional |
| **Azure Blob Storage** | File storage | Optional |
| **Local Storage** | Fallback file storage | Default |

### **Database Schema**

```sql
-- Stores AI-generated feedback
CVFeedbacks (
    Id, CVId, UserId,
    FeedbackText, QualityScore,
    StructureIssues, GrammarIssues,
    MissingFields, Recommendations,
    IsApproved, CreatedAt
)

-- Stores extracted CV data
CVAnalysisResults (
    Id, CVId,
    ExtractedText, ExtractedSkills,
    ExtractedExperience, ExtractedEducation,
    AIConfidenceScore, ProcessingStatus,
    ProcessedAt
)

-- Job postings
Jobs (
    Id, RecruiterId, Title, Description,
    RequiredSkills, Location, JobType,
    SalaryMin, SalaryMax, ExperienceYears,
    IsActive, CreatedAt
)

-- AI-powered matches
JobMatches (
    Id, UserId, JobId,
    MatchScore, MatchReason,
    IsViewed, IsApplied, CreatedAt
)
```

---

## ?? API Endpoints

### **CV Upload with AI**

```http
POST /cv/upload
Content-Type: multipart/form-data
Authorization: Bearer {token}

Body: file (PDF/DOC/DOCX, max 5MB)

Response:
{
  "message": "CV uploaded and queued for AI analysis",
  "cv": {
    "id": "guid",
    "fileName": "resume.pdf",
    "processingStatus": "Queued"
  }
}
```

### **Get AI Feedback**

```http
GET /cv/{cvId}/feedback
Authorization: Bearer {token}

Response:
{
  "qualityScore": 0.75,
  "isApproved": false,
  "overallFeedback": "Your CV shows good structure...",
  "structureIssues": "Missing contact section",
  "grammarIssues": "Several spelling errors found",
  "missingFields": "Education, Skills sections missing",
  "recommendations": "Add a professional summary, include..."
}
```

### **Get Extracted Data**

```http
GET /cv/{cvId}/analysis
Authorization: Bearer {token}

Response:
{
  "extractedSkills": ["Python", "SQL", "Machine Learning"],
  "extractedExperience": "5 years in software development",
  "extractedEducation": "BSc Computer Science",
  "confidenceScore": 0.92,
  "processingStatus": "Completed"
}
```

### **Reprocess CV**

```http
POST /cv/{cvId}/reprocess
Authorization: Bearer {token}

Response:
{
  "message": "CV queued for reprocessing"
}
```

---

## ?? AI Processing Flow

### **Step-by-Step**

1. **Upload**: Student uploads CV via frontend
2. **Storage**: File saved to Azure Blob or local storage
3. **Queue**: CV ID sent to Service Bus queue (or processed immediately)
4. **Extract**: Form Recognizer extracts text from PDF
5. **Analyze**: OpenAI analyzes quality and provides feedback
6. **Skills**: OpenAI extracts skills list
7. **Save**: Results saved to database
8. **Notify**: Frontend polls for feedback
9. **Review**: Student sees feedback and recommendations
10. **Improve**: Student updates CV and re-uploads
11. **Repeat**: Steps 2-10 until `IsApproved = true`
12. **Match**: Once approved, AI matches with jobs

---

## ?? AI Prompts Used

### **Quality Analysis Prompt**

```
Analyze this CV and provide detailed feedback in JSON format:

CV Text: {cvText}

Evaluate on:
- Structure and organization
- Grammar and spelling
- Missing sections
- Overall professionalism

Return JSON with:
- qualityScore (0.0-1.0)
- structureIssues
- grammarIssues
- missingFields
- recommendations
- isApproved (true/false)
```

### **Skill Extraction Prompt**

```
Extract all technical and professional skills from this CV.
Return ONLY a JSON array of skills.

Example: ["Python", "Leadership", "SQL"]
```

### **Job Matching Prompt**

```
Calculate match score between CV and job (0.0-1.0).

Consider:
- Skills alignment
- Experience level
- Education requirements
- Overall fit

Return only numeric score (e.g., 0.85)
```

---

## ?? Testing

### **Test Scenario 1: Upload CV**

```powershell
# Login first
$login = Invoke-RestMethod -Method POST -Uri "https://localhost:7068/auth/login" `
    -ContentType "application/json" `
    -Body '{"email":"student@test.com","password":"Password123!"}'

$token = $login.token

# Upload CV
$cv = @{file = Get-Item "C:\path\to\resume.pdf"}
$response = Invoke-RestMethod -Method POST -Uri "https://localhost:7068/cv/upload" `
    -Headers @{Authorization = "Bearer $token"} `
    -Form $cv

Write-Host "CV uploaded: $($response.cv.id)"
```

### **Test Scenario 2: Check Feedback**

```powershell
# Wait a few seconds for AI processing
Start-Sleep -Seconds 10

# Get feedback
$feedback = Invoke-RestMethod -Method GET -Uri "https://localhost:7068/cv/$($response.cv.id)/feedback" `
    -Headers @{Authorization = "Bearer $token"}

Write-Host "Quality Score: $($feedback.qualityScore)"
Write-Host "Approved: $($feedback.isApproved)"
Write-Host "Feedback: $($feedback.overallFeedback)"
```

---

## ?? Cost Optimization

### **Reduce Costs**

1. **Use GPT-4o-mini** instead of GPT-4o (~90% cheaper)
   ```json
   "DeploymentName": "gpt-4o-mini"
   ```

2. **Cache responses** for similar CVs

3. **Batch processing** via Service Bus

4. **Set quotas** in Azure OpenAI

5. **Use Basic tier** for Service Bus

### **Free Alternatives**

- **Development**: Use mock service (included)
- **Small scale**: Process synchronously without Service Bus
- **Local only**: Disable AI, manual review

---

## ?? Security Best Practices

1. **Never commit API keys** to Git
   ```bash
   # Add to .gitignore
   appsettings.Development.json
   ```

2. **Use Azure Key Vault** in production
   ```csharp
   builder.Configuration.AddAzureKeyVault(...)
   ```

3. **Implement rate limiting** to prevent abuse

4. **Validate file types** before processing

5. **Sanitize extracted text** before AI processing

---

## ?? Monitoring

### **Key Metrics to Track**

- CV upload volume
- AI processing success rate
- Average quality scores
- Feedback loop iterations
- Job match accuracy
- Processing time
- API costs

### **Application Insights Queries**

```kusto
// CV processing success rate
customEvents
| where name == "CVProcessingCompleted"
| summarize Success=count() by bin(timestamp, 1h)

// Average quality scores
customMetrics
| where name == "CVQualityScore"
| summarize avg(value) by bin(timestamp, 1d)
```

---

## ?? Troubleshooting

### **Issue: "OpenAI endpoint not configured"**

**Solution**:
```json
// In appsettings.Development.json
{
  "Azure": {
    "AI": { "Enabled": true },
    "OpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-key"
    }
  }
}
```

### **Issue: "Processing takes too long"**

**Solution**:
1. Enable Service Bus for async processing
2. Use smaller AI model (gpt-4o-mini)
3. Reduce retry attempts

### **Issue: "Form Recognizer fails"**

**Solution**:
1. Check file is valid PDF
2. Verify Form Recognizer quota
3. Check endpoint URL format

---

## ?? Next Enhancements

### **Phase 3 Features**

- [ ] **CV Templates** - Pre-built professional templates
- [ ] **Real-time Preview** - Live CV rendering
- [ ] **Version Comparison** - Show improvements over time
- [ ] **Skill Recommendations** - Suggest skills based on career goals
- [ ] **Interview Prep** - AI-generated interview questions
- [ ] **Salary Insights** - Market rate for skills
- [ ] **Career Path** - AI-suggested career progression

---

## ?? Resources

- **Azure OpenAI**: https://learn.microsoft.com/azure/ai-services/openai/
- **Form Recognizer**: https://learn.microsoft.com/azure/ai-services/document-intelligence/
- **Service Bus**: https://learn.microsoft.com/azure/service-bus-messaging/
- **Application Insights**: https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview

---

## ? Success Checklist

- [ ] Azure AI services created
- [ ] Database migration applied
- [ ] Configuration updated
- [ ] API running with AI enabled
- [ ] Uploaded test CV
- [ ] Received AI feedback
- [ ] Reviewed extracted skills
- [ ] Tested feedback loop
- [ ] Verified job matching (when jobs added)

---

## ?? You Now Have

? **Intelligent CV Analysis** - AI evaluates quality  
? **Automated Feedback Loop** - Students improve iteratively  
? **Skill Extraction** - Auto-populate profiles  
? **Job Matching** - AI-powered recommendations  
? **Scalable Architecture** - Async processing ready  
? **Cost-Effective** - Optional tiers available  

**Your StudentLink platform is now powered by Azure AI!** ??