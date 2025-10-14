# StudentLink API (ASP.NET Core / .NET 9)

Comprehensive backend for a career readiness platform:
- CV upload with AI analysis (overall + section-by-section)
- Interactive improvement actions and progress tracking
- Job postings & application workflow
- Role-based access: Student, Recruiter, Admin

This README targets the "frontend engineer" who will build a production UI. It explains every API capability and how to test them using the included sandbox React frontend (`studentlink-frontend`).

---
## 1. Tech Stack
| Layer | Technology |
|-------|------------|
| Runtime | .NET 9 / ASP.NET Core Web API |
| Auth | JWT (local dev) – extensible to Azure AD B2C |
| AI | Azure OpenAI (GPT) + optional Azure Document Intelligence OCR |
| Data | EF Core (SQL Server / LocalDB) |
| Storage | Local filesystem (pluggable for Blob) |
| Sandbox Frontend | React (Create React App) |

---
## 2. Repository Structure
```
StudentLinkApi_Sln/
 ├─ StudentLinkApi/                # API project
 │   ├─ Controllers/               # Auth, CV, Interactive, Jobs, Admin
 │   ├─ Services/                  # AI, CV processing, Jobs, Progress
 │   ├─ Models/                    # Entities & DTOs
 │   ├─ Data/                      # DbContext & Migrations
 │   └─ Program.cs                 # Composition root
 └─ studentlink-frontend/          # Sandbox React UI
     └─ src/
```

---
## 2.1 Running Everything Locally (Quick)
### Prerequisites
- .NET 9 SDK installed
- Node.js 18+ & npm
- SQL Server LocalDB (Windows) or SQL Server / container alternative
- (Optional) Azure OpenAI & Document Intelligence keys (set `Enabled:false` to skip OCR)

### 1) Configure API Settings
Create `StudentLinkApi/appsettings.Development.json` (see Section 4). For first run you can stub AI keys:
```json
"AzureOpenAI": { "Endpoint": "https://placeholder/", "Key": "dummy", "Deployment": "gpt" }
```
Set `DocumentIntelligence.Enabled` to false if no key.

### 2) Restore & Migrate Database
```bash
cd StudentLinkApi
 dotnet restore
 dotnet ef database update
```
> If `dotnet-ef` not installed: `dotnet tool install --global dotnet-ef`

### 3) Run API (default HTTPS port auto-assigned; usually 7068/5001)
```bash
# Development run with hot reload
dotnet watch run
```
OR plain:
```bash
dotnet run
```
Console output shows listening URLs e.g.:
```
Now listening on: https://localhost:7068
Now listening on: http://localhost:5068
```
Swagger (if enabled) at: `https://localhost:7068/swagger`

### 4) Run Frontend
In a separate terminal:
```bash
cd ../studentlink-frontend
npm install
npm start
```
Open `http://localhost:3000`.

### 5) Login & Test
Use a sample account (buttons on login page) → Upload CV → View interactive feedback & jobs.

### Environment Variables (Optional)
Instead of `appsettings.Development.json` you may export environment variables:
| Setting | Env Variable | Example |
|---------|--------------|---------|
| JWT Secret | `JwtSettings__SecretKey` | `export JwtSettings__SecretKey=DevSecretValue1234567890` |
| OpenAI Key | `AzureOpenAI__Key` | `export AzureOpenAI__Key=xxxx` |
| OCR Enabled | `DocumentIntelligence__Enabled` | `export DocumentIntelligence__Enabled=false` |

On PowerShell prefix with `$env:` (e.g. `$env:JwtSettings__SecretKey="DevSecret"`).

### Using Docker (Optional Sketch)
Not included yet, but you could add a simple `Dockerfile` then run:
```bash
docker build -t studentlink-api ./StudentLinkApi
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development studentlink-api
```
Update frontend `.env` with `REACT_APP_API_BASE=https://localhost:8080` if you externalize base URL.

---
## 3. Domains & Entities (Summary)
| Domain | Entity | Highlights |
|--------|--------|-----------|
| Users | User | Email, Role (Student/Recruiter/Admin), IsActive |
| CV | CV | Versioned uploads, QualityScore, IsApproved, UploadedAt |
| Interactive Feedback | CVInteractiveFeedback | Per-section feedback & scores, improvementPriorities, nextSteps, comparison |
| Progress | CVImprovementProgress | Aggregated improvement metrics per user |
| Jobs | Job | Title, Description, Location, JobType, Skills, Salary, IsActive |
| Applications | JobApplication | Status lifecycle (Applied→...) |
| Improvement Action | (JSON item) | priority, section, action, reason, example, isCompleted |

Sections analyzed: `contact`, `summary`, `experience`, `education`, `skills`.

---
## 4. Configuration
Create `StudentLinkApi/appsettings.Development.json`:
```json
{
  "ConnectionStrings": { "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=StudentLink;Trusted_Connection=True;" },
  "JwtSettings": { "SecretKey": "CHANGE_ME_DEV_32+CHARS", "Issuer": "StudentLink", "Audience": "StudentLink", "ExpirationMinutes": 120 },
  "AzureOpenAI": { "Endpoint": "https://YOUR.openai.azure.com/", "Key": "<key>", "Deployment": "gpt-4o-mini" },
  "DocumentIntelligence": { "Endpoint": "https://YOUR-docint.cognitiveservices.azure.com/", "Key": "<key>", "Enabled": true },
  "Storage": { "Mode": "Local", "LocalPath": "wwwroot/cv" }
}
```
Disable OCR: set `DocumentIntelligence.Enabled` false.

---
## 5. Database
```bash
cd StudentLinkApi
.dotnet ef database update
```
(If `.dotnet` alias not present use `dotnet`.)

---
## 6. Auth Flow
1. Register (or use seeded sample user) / Login → obtain JWT.  
2. Use `Authorization: Bearer <token>` on protected endpoints.  
3. Role guards restrict recruiter & admin features.  

Extend to Azure AD B2C by swapping local JWT issuance for token validation middleware.

---
## 7. CV + Interactive Feedback Flow
1. Student uploads CV (`POST /cv/upload`).
2. Service stores file, extracts text (OCR optional), calls AI prompt.
3. AI returns: overallScore (0–1), section scores, textual feedback, prioritized actions (with examples), nextSteps, improvementFromPrevious.
4. If long running: feedback endpoint returns `{ "status": "Processing" }`; sandbox FE polls until complete.
5. Student marks action items complete → progress recalculates.
6. Upload improved CV → deltas & progress updated.

---
## 8. API Endpoints
### 8.1 Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /auth/login | Email/password → JWT |
| POST | /auth/register | Create user (dev) |
| GET | /auth/me | Current user + role |
| PUT | /auth/profile | Update profile |
| GET | /auth/ping | Health check |
| GET | /auth/me/student | Role probe |
| GET | /auth/me/recruiter | Role probe |
| GET | /auth/me/admin | Role probe |

### 8.2 CV
| Method | Endpoint | Notes |
|--------|----------|-------|
| POST | /cv/upload | multipart/form-data `file` (PDF/DOC/DOCX <=5MB) |
| GET | /cv/current | Current active CV |
| GET | /cv/history | All versions |
| GET | /cv/download/{cvId} | Binary download |
| DELETE | /cv/{cvId} | Delete (soft/hard impl-dependent) |

### 8.3 Interactive Feedback
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /cv/interactive/{cvId}/feedback | Full interactive JSON (may be Processing) |
| POST | /cv/interactive/{cvId}/action/{i}/complete | Mark action i complete |
| GET | /cv/interactive/progress | Aggregated progress metrics |
| GET | /cv/interactive/dashboard | Composite: currentCV + progress + history + nextSteps |

Feedback example:
```json
{
  "overallScore": 0.76,
  "isApproved": false,
  "sections": {
    "summary": { "feedback": "Add quantified achievements.", "score": 0.55 }
  },
  "improvementPriorities": [
    { "priority": "High", "section": "Summary", "action": "Add a 2-3 sentence summary", "reason": "Top-of-page recruiter scan", "example": "Before: ... After: ...", "isCompleted": false }
  ],
  "nextSteps": "1. Improve summary 2. Reformat skills",
  "improvementFromPrevious": "Skills section improved by 15%"
}
```

### 8.4 Jobs & Applications
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | /jobs/search | Any/Auth | Query params: q, location, jobType |
| POST | /jobs | Recruiter | Create job |
| GET | /jobs/mine | Recruiter | Own jobs |
| PUT | /jobs/{id} | Recruiter | Update |
| DELETE | /jobs/{id} | Recruiter | Delete/Close |
| POST | /jobs/{id}/apply | Student | Apply |
| GET | /jobs/applications/mine | Student | Own applications |
| GET | /jobs/{id}/applications | Recruiter | Applicants |
| PATCH | /jobs/applications/{appId}/status | Recruiter | Update status |

### 8.5 Admin
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /admin/users | List users |
| GET | /admin/stats | Platform metrics |
| POST | /admin/users/{id}/role | Change role |
| POST | /admin/users/{id}/status | Activate / deactivate |

### 8.6 Progress Metrics Sample
```json
{
 "totalUploads": 3,
 "initialScore": 0.45,
 "currentScore": 0.78,
 "improvementPercentage": 73.3,
 "completedActions": 4,
 "totalActions": 6,
 "firstUploadDate": "2025-01-08T10:00:00Z",
 "lastUpdateDate": "2025-01-14T12:30:00Z",
 "daysInProgress": 6
}
```

---
## 9. Error Conventions
| Code | Meaning | Example |
|------|---------|---------|
| 202 | Processing | `{ "status": "Processing" }` |
| 400 | Validation | `{ "error": "Invalid file type" }` |
| 401 | Unauthorized | `{ "error": "Unauthorized" }` |
| 403 | Forbidden | `{ "error": "Forbidden" }` |
| 404 | Not found | `{ "error": "CV not found" }` |
| 409 | Conflict | `{ "error": "Already applied" }` |
| 500 | Server | `{ "error": "Server error" }` |

---
## 10. AI Integration Details
| Aspect | Behavior |
|--------|----------|
| Extraction | Uses Document Intelligence if enabled; fallback plain text extraction. |
| Prompt | Requests section scores, improvements (priority + reason + example), next steps, comparison with previous CV. |
| Persistence | Stored per CV; re-fetch doesn’t re-run AI. |
| Re-run Trigger | Only new CV upload. |

---
## 11. Sample cURL
Upload:
```bash
curl -X POST https://localhost:7068/cv/upload \
 -H "Authorization: Bearer $TOKEN" \
 -F file=@Resume.pdf
```
Feedback:
```bash
curl -H "Authorization: Bearer $TOKEN" https://localhost:7068/cv/interactive/$CV_ID/feedback
```
Complete action 0:
```bash
curl -X POST -H "Authorization: Bearer $TOKEN" https://localhost:7068/cv/interactive/$CV_ID/action/0/complete
```
Search jobs:
```bash
curl -H "Authorization: Bearer $TOKEN" "https://localhost:7068/jobs/search?q=engineer&location=Remote"
```

---
## 12. Sandbox React Frontend
Location: `studentlink-frontend/`

### Run
```bash
cd studentlink-frontend
npm install
npm start
```
Default: http://localhost:3000

### Key Routes
| Path | Purpose |
|------|---------|
| /login | Acquire JWT (sample autofill buttons) |
| /cv-upload | Upload & manage CV versions |
| /cv-feedback/:cvId | Interactive feedback & action plan |
| /cv-progress | Progress dashboard (scores, history, actions) |
| /jobs | Browse & apply |
| /applications | Student's applications |
| /recruiter/jobs | Recruiter job management |
| /admin/users | Admin management & stats |

### Manual Test Flow
1. Login (Student sample).  
2. Upload CV.  
3. Wait for analysis (spinner) then open interactive feedback.  
4. Mark action items complete.  
5. View /cv-progress for updated metrics.  
6. Apply to a job on /jobs.  
7. Switch to recruiter sample → view applicants.  
8. Switch to admin sample → user stats / role toggles.  

*Icons are emoji placeholders; replace with a consistent production icon set.*

---
## 13. Production Frontend Recommendations
| Area | Recommendation |
|------|---------------|
| Component System | Adopt design tokens + UI library (e.g., Radix + Tailwind) |
| API Client | Generate typed client (OpenAPI + NSwag) |
| Data Fetch | React Query for caching/polling & mutation states |
| Upload UX | Show parsing pipeline states, diff vs previous CV |
| AI Display | Highlight improved sections; provide before/after diff blocks |
| Accessibility | Semantics, focus management, skipping links |
| Security | Rate limiting, file scanning, secret management |
| Observability | Logging (Serilog), metrics, error tracking (Sentry) |
| Internationalization | Externalize strings early |

---
## 14. Build / Run API
```bash
cd StudentLinkApi
 dotnet restore
 dotnet build
 ASPNETCORE_ENVIRONMENT=Development dotnet run
# Swagger (if enabled): https://localhost:7068/swagger
```
Hot reload during development:
```bash
dotnet watch run
```

---
## 15. Security Checklist
- Rotate JWT secret (store in KeyVault / env var)
- Enforce HTTPS + HSTS
- Validate file mime + size; antivirus scan in production
- Limit upload rate + auth brute force attempts
- Add structured logging + audit events

---
## 16. Glossary
| Term | Definition |
|------|------------|
| Overall Score | AI aggregate quality (0–1) |
| Section Score | Quality per section (0–1) |
| Improvement Action | Prioritized suggestion with rational & example |
| Progress Metrics | Cross-version improvement stats |
| Current CV | Latest active CV version |

---
## 17. Quick Start (TL;DR)
1. Configure `appsettings.Development.json`.
2. `dotnet ef database update`.
3. `dotnet watch run` (or `dotnet run`) API.
4. Run frontend (`npm start`).
5. Login → upload CV → open interactive feedback.

---
## 18. Extension Points
| Extension | Hook |
|-----------|------|
| Additional Sections | Extend model + prompt + serialization |
| Alternate AI | Implement / swap AI service interface |
| Blob Storage | Replace local file handler with Azure Blob impl |
| Eventing | Publish domain events to Service Bus / queue |

---
## 19. Auth Service Deep Dive
| Aspect | Detail |
|--------|--------|
| Why | Provide stateless auth for SPA + mobile without session affinity. |
| How | Email/password validated → bcrypt hash compare → issue JWT with role + user id claims. |
| Claims | `sub` (user id), `role`, `email`, plus custom if needed. |
| Middleware | `AddAuthentication().AddJwtBearer(...)` with symmetric key from config. |
| Role Enforcement | `[Authorize(Roles="Admin")]` etc; multiple roles comma separated. |
| Token Lifetime | `ExpirationMinutes` config; short-lived encourages re-login / refresh pattern. |
| Refresh Strategy (future) | Introduce refresh token table + rotation / revocation list or shift to B2C. |
| Testing | Use `/auth/me` to confirm claims decode. Intentionally send expired / tampered token to verify 401. |

### Testing Auth Quickly
```bash
# Successful login (example)
curl -X POST https://localhost:7068/auth/login -H "Content-Type: application/json" -d '{"email":"john.doe@student.com","password":"Student123!"}'
# Use token
curl -H "Authorization: Bearer $TOKEN" https://localhost:7068/auth/me
```

---
## 20. JWT Lifecycle
1. User credentials validated.  
2. Server builds claim set.  
3. Token signed (HS256) with secret; no DB persistence.  
4. Client stores (memory or secure storage).  
5. Each request attaches `Authorization` header.  
6. Middleware validates signature + expiry; builds `ClaimsPrincipal`.  
7. Controllers access `User` object; role checks performed.  
8. On expiry client re-authenticates (or future refresh endpoint).  

| Pitfall | Mitigation |
|---------|-----------|
| Token leakage | Use HTTPS only, avoid localStorage if possible (HTTP-only cookie or memory). |
| Long-lived tokens | Keep short + rotate secret on compromise. |
| Role change mid-life | Force re-login after admin role change (track `LastSecurityStamp`). |

---
## 21. CV Processing Pipeline
| Step | Description | Failure Handling |
|------|-------------|------------------|
| Upload Validation | Size/type check; rejects non-whitelisted extensions | 400 error returned |
| Storage | Saved to local path (or blob later) | On IO failure → 500 + logged |
| Text Extraction | OCR if enabled; fallback naive text parse | If OCR fails fallback path logged warning |
| AI Prompt Build | Consolidates text + previous CV deltas | Missing previous CV handled gracefully |
| Azure OpenAI Call | Receives structured JSON (guardrails) | Retry/backoff or mark processing status |
| Parsing & Mapping | JSON -> domain (scores, improvements) | If parse error → mark analysis failed |
| Persist Entities | CV metadata + feedback + progress update | Transaction to keep consistency |
| Progress Recalc | Update aggregated improvement metrics | Calculated even if some sections null |

### Why This Structure
- Separation of extraction vs AI reduces cost if reusing raw text.
- Idempotent per upload; re-runs only when a new CV is posted.
- Allows asynchronous expansion (queue based) later; current polling simulates that.

### Testing Pipeline
1. Upload deliberately minimal CV → expect low scores + many actions.
2. Upload enriched version → verify `improvementFromPrevious` populated.
3. Delete latest CV → ensure `current` reverts (if implemented) or returns 404.

---
## 22. Document Analysis (Azure Document Intelligence)
| Aspect | Detail |
|--------|-------|
| Purpose | Higher fidelity extraction (handles PDF layout, tables, contact info). |
| Toggle | `DocumentIntelligence.Enabled` flag. |
| Flow | File → OCR API → structured text (optionally sections) → flatten to prompt. |
| Fallback | If disabled or error → simple text read (e.g., stream to string). |
| Performance | Cache not yet implemented; same file re-upload triggers fresh extraction. |
| Security | Do not log raw document content; only derived metrics. |
| Testing | Enable flag, upload PDF with varied fonts; compare extracted text vs fallback by disabling flag. |

---
## 23. Azure OpenAI Integration
| Aspect | Detail |
|--------|-------|
| Deployment | Config key `AzureOpenAI.Deployment` (model name). |
| Prompt Strategy | Single composite prompt asking for JSON: overall score, per-section, prioritized actions (with example fields), next steps, comparison summary. |
| Safety | Keep temperature moderate (0.4–0.6) for consistency (set inside service). |
| JSON Robustness | Post-process: trim, attempt `JsonDocument.Parse`; if fails, fallback regex extraction or mark processing. |
| Cost Control | Only triggered per new CV; no re-analysis on action completion. |
| Latency | Frontend polls every few seconds until `status != Processing`. |
| Testing | Inject mock service returning deterministic sample payload for UI dev. |

---
## 24. Database Schema (Conceptual)
| Table | Key Columns | Notes |
|-------|-------------|------|
| Users | Id, Email, Role, IsActive, CreatedAt | Basic identity + role; password hash if local auth (not shown here) |
| Profiles (optional) | UserId (FK), Summary, Skills | Extend user attributes |
| CVs | Id, UserId, FileName, FileSize, QualityScore, IsApproved, UploadedAt, IsActive | One active CV per user |
| CVInteractiveFeedbacks | CVId, OverallScore, Section*Score, Section*Feedback, ImprovementPriorities(JSON), NextSteps, ImprovementFromPrevious, CreatedAt | Denormalized for fast read |
| CVImprovementProgresses | UserId, TotalUploads, InitialScore, CurrentScore, ImprovementPercentage, CompletedActions, TotalActions, FirstUploadDate, LastUpdateDate | Cached aggregates |
| Jobs | Id, RecruiterId, Title, Description, Location, JobType, RequiredSkills, SalaryMin/Max, IsActive, CreatedAt | Listings |
| JobApplications | Id, JobId, UserId, Status, AppliedAt | Status enum or string |

### Rationale
- Feedback denormalized: avoids multiple joins per page load.
- Progress table acts as materialized view to prevent recalculating on every dashboard hit.
- JSON column for improvement list keeps flexibility (add new fields without migration).

### Testing Data Integrity
- Upload CV twice → expect CV count =2, progress.TotalUploads=2.
- Mark action complete → expect CompletedActions increments; CompletedActions ≤ TotalActions.
- Delete user (if implemented) → cascade or restrict (check FK constraints).

---
## 25. How to Test Critical Services Quickly
| Service | Quick Test | Expected |
|---------|------------|----------|
| Auth | Login invalid password | 401 error |
| JWT | Decode token (jwt.io) | role + sub claims present |
| CV Upload | Upload >5MB file | 400 size error |
| OCR | Toggle enabled/disabled | Different raw text length counts |
| AI | Force mock (temporarily return static JSON) | Deterministic UI rendering |
| Progress | Complete all actions | progress completedActions==totalActions, milestone message |
| Jobs | Apply same job twice | 409 conflict |

---
## 26. Azure Resources Provisioning & Integration
The table below lists recommended Azure resources, suggested names (adjust region prefix), purpose, creation (Azure CLI), and how to wire them into the application configuration. Replace `<rg>`, `<loc>` with your resource group and Azure region (e.g. `westeurope`, `eastus`). Use consistent naming to simplify automation.

| Resource | Suggested Name | Purpose | CLI Creation (excerpt) | Config Mapping |
|----------|----------------|---------|------------------------|----------------|
| Resource Group | `rg-studentlink-dev` | Logical container | `az group create -n rg-studentlink-dev -l <loc>` | N/A |
| Azure SQL Server | `stulink-sqlsrv` | Host SQL DB | `az sql server create -g <rg> -n stulink-sqlsrv -u sqladmin -p <Pwd>` | In ConnectionStrings (Server=tcp:stulink-sqlsrv.database.windows.net,1433; ...) |
| Azure SQL DB | `studentlinkdb` | Persistent relational storage | `az sql db create -g <rg> -s stulink-sqlsrv -n studentlinkdb --service-objective S0` | `ConnectionStrings:DefaultConnection` |
| Azure OpenAI | `stulink-openai` | AI analysis (CV scoring) | `az cognitiveservices account create -n stulink-openai -g <rg> -l <loc> --kind OpenAI --sku S0 --custom-domain stulink-openai` | `AzureOpenAI:Endpoint`, `AzureOpenAI:Key`, `AzureOpenAI:Deployment` |
| OpenAI Deployment | `gpt-4o-mini` | Model variant | Portal / REST deploy model | `AzureOpenAI:Deployment` |
| Document Intelligence | `stulink-docai` | OCR / structure extraction | `az cognitiveservices account create -n stulink-docai -g <rg> -l <loc> --kind FormRecognizer --sku S0` | `DocumentIntelligence:Endpoint`, `DocumentIntelligence:Key` |
| Storage Account | `stulinkstorage` | CV file storage (prod) | `az storage account create -n stulinkstorage -g <rg> -l <loc> --sku Standard_LRS` | Replace local path with Blob implementation |
| Blob Container | `cv-files` | Store original CV uploads | `az storage container create --account-name stulinkstorage -n cv-files --auth-mode login` | Blob container name in storage options |
| Key Vault | `kv-stulink-dev` | Secret management (keys, conn strings) | `az keyvault create -n kv-stulink-dev -g <rg> -l <loc>` | Store secrets referenced via Managed Identity |
| App Service Plan | `asp-stulink-dev` | Host Web API | `az appservice plan create -g <rg> -n asp-stulink-dev --sku B1 --is-linux` | N/A |
| App Service (API) | `stulink-api-dev` | Deploy backend | `az webapp create -g <rg> -p asp-stulink-dev -n stulink-api-dev --runtime "DOTNET:9"` | App Settings mapped to config keys |
| App Insights | `stulink-ai` | Telemetry (logs/metrics) | `az monitor.app-insights.component.create -g <rg> -l <loc> -a stulink-ai` | `APPLICATIONINSIGHTS_CONNECTION_STRING` env var |
| Azure AD B2C (optional) | `<tenant>.onmicrosoft.com` | Production identity | Portal setup | Replace local JWT settings |
| Service Bus (future) | `stulink-sb` | Async events (upload, action completed) | `az servicebus namespace create -g <rg> -n stulink-sb -l <loc>` | Event dispatcher config |

### 26.1 Retrieving Keys / Connection Strings
```bash
# SQL DB connection string (ADO .NET)
az sql db show-connection-string -s stulink-sqlsrv -n studentlinkdb -c ado.net
# OpenAI key
az cognitiveservices account keys list -n stulink-openai -g <rg>
# Document Intelligence key
az cognitiveservices account keys list -n stulink-docai -g <rg>
# Storage account key
az storage account keys list -n stulinkstorage -g <rg>
```

### 26.2 Storing Secrets in Key Vault
```bash
az keyvault secret set --vault-name kv-stulink-dev --name SqlConnection --value "<connection-string>"
az keyvault secret set --vault-name kv-stulink-dev --name OpenAIKey --value "<openai-key>"
az keyvault secret set --vault-name kv-stulink-dev --name DocAIKey --value "<docai-key>"
```
Enable Managed Identity on the App Service, grant `get`/`list` permissions to secrets (`az keyvault set-policy`). Retrieve in code via DefaultAzureCredential or map them into environment variables at startup.

### 26.3 Switching to Blob Storage
Add to config:
```json
"Storage": { "Mode": "Blob", "AccountName": "stulinkstorage", "Container": "cv-files" }
```
Implement / enable a Blob storage service class using `BlobContainerClient`. Migrate existing local files by uploading them with original file names (or store metadata in DB referencing blob name).

### 26.4 Deploying the API
```bash
# Build & publish
cd StudentLinkApi
 dotnet publish -c Release -o publish
# Deploy (zip deploy)
az webapp deploy -g <rg> -n stulink-api-dev --src-path publish --type zip
# Set settings
az webapp config appsettings set -g <rg> -n stulink-api-dev --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  AzureOpenAI__Endpoint=https://stulink-openai.openai.azure.com/ \
  AzureOpenAI__Deployment=gpt-4o-mini \
  DocumentIntelligence__Endpoint=https://stulink-docai.cognitiveservices.azure.com/ \
  Storage__Mode=Blob Storage__AccountName=stulinkstorage Storage__Container=cv-files
```
If using Key Vault references (recommended):
```
AzureOpenAI__Key=@Microsoft.KeyVault(SecretUri=https://kv-stulink-dev.vault.azure.net/secrets/OpenAIKey/)
```

### 26.5 Cost & Scaling Notes
| Resource | Scale Considerations |
|----------|----------------------|
| OpenAI | Use smaller deployment in dev; monitor token usage. |
| Document Intelligence | Disable for plain-text only quick tests. |
| SQL | Start S0 / serverless; scale up if DTU or CPU saturated. |
| Storage | Standard_LRS sufficient; enable lifecycle policies for old CVs. |
| App Service | Start B1; autoscale based on CPU / requests when prod-ready. |
| App Insights | Configure sampling (Adaptive) to control ingestion cost. |

### 26.6 Minimal Cloud Footprint (Cheapest Dev)
If cost-sensitive skip: Document Intelligence, Service Bus, Key Vault (use env vars). Use: SQL (or Azure SQL serverless), OpenAI (single small deployment), Storage, App Service + App Insights (basic logs).

---
## 27. Happy Building
Replace the sandbox UI with the polished experience and connect the MAUI app / new frontend & mobile clients confidently using the configuration and resource guide above.
