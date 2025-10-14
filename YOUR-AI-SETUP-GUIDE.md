# ?? StudentLink AI - Quick Start (Your Setup)

## ? **What's Already Done**

You've manually created all Azure AI resources:
- ? Azure OpenAI (`studentlink-openai`) with gpt-5-mini
- ? Document Intelligence (`studentlink-doc-int`)
- ? Service Bus Queue (`cv-parser-queue`)
- ? Blob Storage container (`cvs`)
- ? Configuration file updated

---

## ?? **Final Setup (3 Steps)**

### **Step 1: Get Service Bus Connection String**

```powershell
cd "C:\MAUI Applications\StudentLinkApi_Sln\infrastructure"
.\get-servicebus-connection.ps1
```

**Then:**
1. Copy the connection string from the output
2. Open `StudentLinkApi\appsettings.Development.json`
3. Update this line:
```json
"ServiceBus": {
  "ConnectionString": "PASTE-CONNECTION-STRING-HERE"
}
```

---

### **Step 2: Apply Database Migration**

```powershell
.\add-ai-features.ps1
```

This creates these tables:
- `CVFeedbacks` - AI-generated feedback
- `CVAnalysisResults` - Extracted CV data
- `Jobs` - Job postings
- `JobMatches` - AI-powered matches

---

### **Step 3: Verify & Start**

```powershell
# Verify everything is configured
.\verify-ai-setup.ps1

# Start API
cd ..\StudentLinkApi
dotnet run

# Start Frontend (new terminal)
cd ..\studentlink-frontend
npm start
```

---

## ?? **Test Your AI System**

### **Quick Test**

1. Open `http://localhost:3000`
2. Login as Student (or register new account)
3. Click "**My CV**" in navbar
4. Upload a PDF resume
5. Wait ~10 seconds
6. Check `/cv/{id}/feedback` endpoint

### **Expected AI Response**

```json
{
  "qualityScore": 0.75,
  "isApproved": false,
  "overallFeedback": "Your CV shows good structure...",
  "structureIssues": "Consider adding...",
  "grammarIssues": "Check spelling in...",
  "recommendations": "Add skills section..."
}
```

---

## ?? **Your Azure Resources**

| Resource | Name | Region | Status |
|----------|------|--------|--------|
| **OpenAI** | studentlink-openai | South Africa North | ? Ready |
| **Model** | gpt-5-mini | - | ? Deployed |
| **Document AI** | studentlink-doc-int | South Africa North | ? Ready |
| **Service Bus** | studentlink-sb-dev | South Africa North | ? Ready |
| **Queue** | cv-parser-queue | - | ? Created |
| **Storage** | studentlinkstore | South Africa North | ? Ready |
| **Container** | cvs | - | ? Created |

---

## ?? **Configuration Summary**

**appsettings.Development.json:**
```json
{
  "Azure": {
    "AI": {
      "Enabled": true  ?
    },
    "OpenAI": {
      "Endpoint": "https://studentlink-openai.openai.azure.com/",
      "DeploymentName": "gpt-5-mini"
    },
    "FormRecognizer": {
      "Endpoint": "https://studentlink-doc-int.cognitiveservices.azure.com/"
    },
    "BlobStorage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=studentlinkstore..."
    }
  },
  "FileStorage": {
    "UseAzure": true  ?
  }
}
```

---

## ?? **AI Processing Flow**

```
1. User uploads CV (PDF) via frontend
        ?
2. File saved to Azure Blob Storage (cvs container)
        ?
3. CV queued in Service Bus (cv-parser-queue)
        ?
4. Background worker processes:
   a. Document Intelligence extracts text
   b. GPT-5-mini analyzes quality
   c. GPT-5-mini extracts skills
        ?
5. Results saved to database
        ?
6. User gets feedback via API
        ?
7. User improves CV and re-uploads
        ?
8. Loop until approved!
```

---

## ?? **Your Monthly Costs**

| Service | Tier | Estimated Cost |
|---------|------|----------------|
| **Document Intelligence** | Free F0 | $0.00 |
| **Azure OpenAI (gpt-5-mini)** | Pay-per-use | $5-20/month |
| **Service Bus** | Standard | $0.05/month |
| **Blob Storage** | Standard | $0.02/month |
| **Total** | | **~$5-20/month** |

**Note:** GPT-5-mini is ~90% cheaper than GPT-4o!

---

## ?? **Troubleshooting**

### **Issue: "Deployment 'gpt-5-mini' not found"**

Your deployment name is `gpt-5-mini`, which is correct in the config.

### **Issue: "Service Bus connection failed"**

Make sure to run `.\get-servicebus-connection.ps1` and update the connection string.

### **Issue: "Blob container not found"**

Verify the `cvs` container exists in `studentlinkstore`.

### **Issue: "AI processing not starting"**

Check Application Insights logs in Azure Portal for errors.

---

## ? **Verification Checklist**

- [x] Azure OpenAI created
- [x] GPT-5-mini model deployed
- [x] Document Intelligence created
- [x] Service Bus queue created
- [x] Blob container created
- [x] Configuration file updated
- [ ] Service Bus connection string added
- [ ] Database migration applied
- [ ] API builds successfully
- [ ] Frontend connects to API
- [ ] CV upload works
- [ ] AI feedback received

---

## ?? **You're Ready!**

Run these commands to start:

```powershell
# 1. Get Service Bus connection (if not done)
cd infrastructure
.\get-servicebus-connection.ps1

# 2. Verify setup
.\verify-ai-setup.ps1

# 3. Apply migration
.\add-ai-features.ps1

# 4. Start API
cd ..\StudentLinkApi
dotnet run

# 5. Start Frontend (new terminal)
cd ..\studentlink-frontend
npm start
```

**Upload a CV and see the AI magic!** ?

---

**Questions?** Check:
- `FINAL-IMPLEMENTATION-SUMMARY.md` - Complete guide
- `AI-CV-PROCESSING-GUIDE.md` - AI implementation details
- API Swagger: `https://localhost:7068/swagger`