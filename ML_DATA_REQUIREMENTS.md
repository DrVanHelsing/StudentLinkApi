# Machine Learning Job Recommendation System - Data Requirements & Feature Engineering Strategy

## Document Overview

**Project:** StudentLink Job Recommendation System  
**Purpose:** ML-powered matching between students and job opportunities  
**Technology Stack:** .NET 9, Azure OpenAI, ML.NET  
**Last Updated:** January 2025

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Data Sources](#data-sources)
3. [Detailed Database Schemas (Existing & New)](#detailed-database-schemas-existing--new)
4. [Feature Engineering Pipeline](#feature-engineering-pipeline)
5. [Feature Store Schemas](#feature-store-schemas)
6. [Training Data Generation](#training-data-generation)
7. [Model Input Features](#model-input-features)
8. [Data Quality Requirements](#data-quality-requirements)
9. [Data Gaps & Recommendations](#data-gaps--recommendations)
10. [Implementation Workstreams](#implementation-workstreams)
11. [Team Workload Split (3 Developers)](#team-workload-split-3-developers)
12. [Appendix B: Embedding Strategy](#appendix-b-embedding-strategy)
13. [Appendix C: Sample Feature Extraction Code (Pseudocode)](#appendix-c-sample-feature-extraction-code-pseudocode)
14. [Appendix D: Training Data Quality Checklist](#appendix-d-training-data-quality-checklist)
15. [Appendix E: Evaluation Metrics Definitions](#appendix-e-evaluation-metrics-definitions)

---

## Executive Summary

### Current Data Availability: **80% Complete**

Your StudentLink database already contains most of the critical data needed for an effective ML recommendation system:

- ✅ **CV Analysis Data** - Extracted skills, experience, education via Azure Document Intelligence + OpenAI
- ✅ **Job Postings** - Title, description, required skills, experience levels
- ✅ **Application History** - Student-job interactions with status outcomes
- ✅ **User Profiles** - Bio, skills, education, work history
- ⛔ **Company Profiles** - Missing (needs to be created)
- ⚠️ **User Preferences** - Partially available (needs enhancement)

### Recommended ML Approach

**Hybrid Content-Based + Collaborative Filtering**

- **Content-Based:** Match student skills/experience embeddings with job requirement embeddings (primary)
- **Collaborative Filtering:** Learn from application patterns (secondary)
- **Deep Learning:** Optional transformer-based model for advanced semantic matching

---

## Data Sources

### 1. Student CV Data

#### Source Tables
- `CVs` - File metadata
- `CVAnalysisResult` - Extracted structured data from Form Recognizer + GPT
- `CVFeedback` - Quality assessment
- `CVInteractiveFeedback` - Section-by-section analysis

#### Available Data Points

| Field | Table.Column | Data Type | Quality | Purpose |
|-------|--------------|-----------|---------|---------|
| **Full CV Text** | `CVAnalysisResult.ExtractedText` | Text (unlimited) | High | Generate semantic embeddings |
| **Skills List** | `CVAnalysisResult.ExtractedSkills` | NVARCHAR(MAX) (comma-separated) | High | Hard skill matching |
| **Experience Description** | `CVAnalysisResult.ExtractedExperience` | NVARCHAR(MAX) | High | Semantic experience matching |
| **Education Details** | `CVAnalysisResult.ExtractedEducation` | NVARCHAR(MAX) | Medium | Education level filtering |
| **Contact Info** | `CVAnalysisResult.ExtractedContact` | NVARCHAR(500) | Low | Not used for ML |
| **AI Quality Score** | `CVAnalysisResult.AIConfidenceScore` | DECIMAL(4,3) | High | Candidate quality signal |
| **CV Approval Status** | `CVFeedback.IsApproved` | BIT | High | Filter unqualified candidates |
| **Overall CV Score** | `CVFeedback.QualityScore` | DECIMAL(4,3) | High | Ranking signal |
| **Section Scores** | `CVInteractiveFeedback.*SectionScore` | DECIMAL(4,3) | Medium | Granular quality metrics |

#### Data Quality Metrics
- **Coverage:** ~95% of active students have CVs
- **Completeness:** ~85% of CVs have all required fields extracted
- **Freshness:** Updated when student uploads new CV
- **Processing Status:** Check `CVAnalysisResult.ProcessingStatus = 'Completed'`

#### Required CV Extraction Targets (to improve model inputs)
- `skills` (canonicalized): list of strings
- `experience_items`: array of { title, company, start_date, end_date, location, responsibilities (bulleted), achievements (quantified), technologies }
- `education_items`: array of { degree, field_of_study, institution, graduation_year, gpa? }
- `certifications`: array of { name, issuer, issued_year, expires_year? }
- `projects`: array of { name, description, technologies, link? }
- `languages`: array of { name, proficiency }
- `summary`: short professional summary paragraph
- `total_years_experience`: numeric
- `preferred_locations`: list of strings (if stated)

Recommended JSON shapes to store in `CVAnalysisResult` (new columns below):
```json
{
  "skills": ["c#", ".net", "azure", "machine learning"],
  "experience_items": [
    {
      "title": "Software Engineer",
      "company": "TechCorp",
      "start_date": "2022-06-01",
      "end_date": "2024-01-01",
      "location": "New York, NY",
      "responsibilities": ["Built APIs", "Maintained CI/CD"],
      "achievements": ["Reduced latency 30%"],
      "technologies": ["c#", "azure functions", "cosmos db"]
    }
  ],
  "education_items": [
    {
      "degree": "Bachelor's",
      "field_of_study": "Computer Science",
      "institution": "XYZ University",
      "graduation_year": 2022,
      "gpa": 3.7
    }
  ],
  "certifications": [{ "name": "AZ-900", "issuer": "Microsoft", "issued_year": 2023 }],
  "projects": [{ "name": "Portfolio", "technologies": ["react", "azure"] }],
  "languages": [{ "name": "English", "proficiency": "Native" }],
  "summary": "CS graduate interested in ML and cloud.",
  "total_years_experience": 2.5,
  "preferred_locations": ["Remote", "NYC"]
}
```

#### Recommended Preprocessing

1. **Text Normalization**
   - Lowercase; strip special characters (keep alphanumerics, spaces)
   - Normalize dates to `YYYY-MM-DD`
   - Collapse duplicate spaces/newlines
2. **Skills Extraction Enhancement**
   - Parse into JSON array (canonical forms)
   - Maintain taxonomy mapping to canonical skills
3. **Experience Parsing**
   - Extract roles, companies, dates, achievements with metrics
   - Compute `total_years_experience`
4. **Education Level Mapping**
   - High School=1, Associate’s=2, Bachelor’s=3, Master’s=4, PhD=5

---

### 2. User Profile Data

#### Source Table
- `Profiles` - Student-created profile information

#### Available Data Points

| Field | Column | Data Type | Quality | Purpose |
|-------|--------|-----------|---------|---------|
| **Bio/Summary** | `Summary` | NVARCHAR(500) | Medium | Career goals, interests |
| **Skills JSON** | `Skills` | NVARCHAR(MAX) (JSON) | Low | Supplemental skills |
| **Education JSON** | `Education` | NVARCHAR(MAX) (JSON) | Low | Supplemental education |
| **Experience JSON** | `Experience` | NVARCHAR(MAX) (JSON) | Low | Supplemental experience |
| **LinkedIn** | `LinkedInUrl` | NVARCHAR(100) | Medium | Professional signal |
| **GitHub** | `GitHubUrl` | NVARCHAR(100) | High (for tech) | Portfolio signal |
| **Portfolio** | `PortfolioUrl` | NVARCHAR(100) | Medium | Work samples |

#### Data Quality Issues
- Completeness ~40% fully filled
- Overlap with CV data
- Profiles updated less frequently than CVs

#### Feature Engineering from Profiles
- Profile completeness (weighted)
- Career goals embedding from `Summary`
- Portfolio signals: `has_linkedin`, `has_github`, `has_portfolio`
- Skills taxonomy merge (profile + CV)

---

### 3. Job Posting Data

#### Source Table
- `Jobs` - Recruiter-posted job opportunities

#### Available Data Points

| Field | Column | Data Type | Quality | Purpose |
|-------|--------|-----------|---------|---------|
| **Job Title** | `Title` | NVARCHAR(200) | High | Role type matching |
| **Job Description** | `Description` | NVARCHAR(MAX) | High | Requirements & responsibilities |
| **Required Skills** | `RequiredSkills` | NVARCHAR(500) | Medium | Hard skill matching |
| **Location** | `Location` | NVARCHAR(200) | High | Geographic filtering |
| **Job Type** | `JobType` | NVARCHAR(50) or ENUM | High | Full-time, Internship, Contract |
| **Salary Min/Max** | `SalaryMin/SalaryMax` | DECIMAL(18,2) | Low | Compensation features |
| **Experience Years** | `ExperienceYears` | INT | Medium | Minimum required years |
| **Education Level** | `EducationLevel` | NVARCHAR(100) | Medium | Required education |
| **Is Active** | `IsActive` | BIT | High | Availability filter |
| **Created Date** | `CreatedAt` | DATETIME2 | High | Freshness |
| **Recruiter ID** | `RecruiterId` | UNIQUEIDENTIFIER | High | Link to company/profile |

#### Feature Engineering from Jobs
- Popularity score (applications, views, freshness)
- Salary normalization
- Freshness decay
- Parsed requirements, seniority, benefits
- Skills standardization to canonical ontology

---

### 4. Job Application History (Training Labels)

#### Source Table
- `JobApplications` - Student application events and outcomes

#### Available Data Points

| Field | Column | Data Type | Quality | Purpose |
|-------|--------|-----------|---------|---------|
| **Student ID** | `UserId` | UNIQUEIDENTIFIER | High | Link to student |
| **Job ID** | `JobId` | UNIQUEIDENTIFIER | High | Link to job |
| **Application Status** | `Status` | NVARCHAR(50) or ENUM | High | Outcome label |
| **Notes** | `Notes` | NVARCHAR(MAX) | Low | Recruiter feedback |
| **Applied Date** | `AppliedAt` | DATETIME2 | High | Timestamp |
| **Last Updated** | `UpdatedAt` | DATETIME2 | Medium | Status change tracking |

#### Application Status Values & ML Labels

| Status | Frequency | ML Label (0-1) | Training Weight |
|--------|-----------|----------------|-----------------|
| Applied | ~60% | 0.3 | 1.0 |
| Reviewed | ~20% | 0.5 | 1.5 |
| Interview | ~10% | 0.7 | 2.0 |
| Offer | ~3% | 0.9 | 3.0 |
| Hired | ~2% | 1.0 | 5.0 |
| Rejected | ~5% | 0.0 | 1.0 |

#### Temporal Features
- Application velocity
- Student success rates (interview, offer, hire)
- Time to status change

#### Negative Sampling Strategy
- For each positive, sample 2-3 time-aligned negatives a student did not apply to
- Constrain by job type, time window, activity, location

---

### 5. Job Match Table (Recommendation Output & Feedback)

#### Source Table
- `JobMatch` - ML-generated recommendations and user interactions

#### Available Data Points

| Field | Column | Data Type | Quality | Purpose |
|-------|--------|-----------|---------|---------|
| **Match Score** | `MatchScore` | DECIMAL(4,3) | High | Model output |
| **Match Reason** | `MatchReason` | NVARCHAR(MAX) | Medium | Explainability |
| **Is Viewed** | `IsViewed` | BIT | High | Engagement signal |
| **Is Applied** | `IsApplied` | BIT | High | Conversion metric |
| **Created At** | `CreatedAt` | DATETIME2 | High | Recommendation timestamp |

#### Usage for ML Feedback Loop
- Precision@K, CTR, Conversion, MRR
- A/B testing across traffic splits

---

### 6. Company/Recruiter Data (Missing - High Priority)

#### Current State
- No dedicated company profile table
- Limited to recruiter identity fields on `Users`

#### Recommended New Table: `CompanyProfiles`

```sql
CREATE TABLE CompanyProfiles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    RecruiterId UNIQUEIDENTIFIER NOT NULL,

    -- Basic Info
    CompanyName NVARCHAR(200) NOT NULL,
    Industry NVARCHAR(100),                -- Tech, Finance, Healthcare, etc.
    CompanySize NVARCHAR(50),              -- Startup, Small, Medium, Large
    FoundedYear INT,
    HeadquartersLocation NVARCHAR(200),
    TechStack NVARCHAR(MAX),               -- JSON array of technologies

    -- Culture & Values
    CompanyDescription NVARCHAR(MAX),
    CultureKeywords NVARCHAR(500),         -- "innovative, collaborative, inclusive"
    WorkEnvironment NVARCHAR(50),          -- Startup, Corporate, Non-profit, Agency

    -- Benefits & Policies
    Benefits NVARCHAR(500),
    RemotePolicy NVARCHAR(50),             -- Fully Remote, Hybrid, On-site

    -- Social Proof
    Website NVARCHAR(200),
    LinkedInUrl NVARCHAR(200),
    GlassdoorRating DECIMAL(3,1),

    -- Embeddings & Metadata
    CultureEmbedding VARBINARY(MAX),       -- normalized float[]
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,

    CONSTRAINT FK_CompanyProfiles_Users_RecruiterId FOREIGN KEY (RecruiterId)
        REFERENCES Users(Id) ON DELETE CASCADE
);
CREATE INDEX IX_CompanyProfiles_RecruiterId ON CompanyProfiles(RecruiterId);
CREATE INDEX IX_CompanyProfiles_Industry_Size ON CompanyProfiles(Industry, CompanySize);
```

---

### 7. User Preferences (Partially Missing - Medium Priority)

#### Recommended New Table: `UserPreferences`

```sql
CREATE TABLE UserPreferences (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,

    PreferredLocations NVARCHAR(300),      -- JSON: ["New York", "Remote"]
    PreferredJobTypes NVARCHAR(100),       -- JSON: ["Full-time", "Internship"]
    PreferredIndustries NVARCHAR(300),     -- JSON
    PreferredCompanySizes NVARCHAR(100),   -- JSON

    MinSalaryExpectation DECIMAL(18,2) NULL,
    MaxSalaryExpectation DECIMAL(18,2) NULL,

    RemotePreference NVARCHAR(50),         -- Remote Only, Hybrid OK, On-site OK, No Preference
    WillingToRelocate BIT DEFAULT 0,

    CareerGoals NVARCHAR(500) NULL,
    SkillsToLearn NVARCHAR(300) NULL,      -- JSON array

    ImplicitJobTypePreference NVARCHAR(50) NULL,
    ImplicitIndustryPreference NVARCHAR(100) NULL,

    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,

    CONSTRAINT FK_UserPreferences_Users_UserId FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX UX_UserPreferences_UserId ON UserPreferences(UserId);
```

---

## Detailed Database Schemas (Existing & New)

Below are proposed column-level details, constraints, and indexes. Adjust to match current state; add migrations for new columns where indicated.

### Existing Entities (augment with suggested columns)

1) `CVs`
- `Id` UNIQUEIDENTIFIER (PK)
- `UserId` UNIQUEIDENTIFIER (FK -> `Users.Id`, IX)
- `BlobUrl` NVARCHAR(400)
- `FileName` NVARCHAR(255)
- `FileType` NVARCHAR(50)
- `UploadedAt` DATETIME2 (IX DESC)
- `IsActive` BIT (IX)

2) `CVAnalysisResult` (additions marked NEW)
- `Id` UNIQUEIDENTIFIER (PK)
- `CVId` UNIQUEIDENTIFIER (FK -> `CVs.Id`, IX)
- `ExtractedText` NVARCHAR(MAX)
- `ExtractedSkills` NVARCHAR(MAX)  -- raw comma string
- NEW: `SkillsJson` NVARCHAR(MAX)  -- canonicalized JSON array
- `ExtractedExperience` NVARCHAR(MAX)
- NEW: `ExperienceJson` NVARCHAR(MAX)  -- experience_items JSON
- `ExtractedEducation` NVARCHAR(MAX)
- NEW: `EducationJson` NVARCHAR(MAX)   -- education_items JSON
- NEW: `CertificationsJson` NVARCHAR(MAX)
- NEW: `ProjectsJson` NVARCHAR(MAX)
- NEW: `LanguagesJson` NVARCHAR(MAX)
- NEW: `Summary` NVARCHAR(1000)
- NEW: `TotalYearsExperience` DECIMAL(5,2)
- `AIConfidenceScore` DECIMAL(4,3)
- `ProcessingStatus` NVARCHAR(30)
- `CreatedAt` DATETIME2 DEFAULT SYSUTCDATETIME()

3) `CVFeedback`
- `Id` UNIQUEIDENTIFIER (PK)
- `CVId` UNIQUEIDENTIFIER (FK -> `CVs.Id`, IX)
- `QualityScore` DECIMAL(4,3)
- `IsApproved` BIT
- `Comments` NVARCHAR(MAX)
- `CreatedAt` DATETIME2 DEFAULT SYSUTCDATETIME()

4) `CVInteractiveFeedback`
- `Id` UNIQUEIDENTIFIER (PK)
- `CVId` UNIQUEIDENTIFIER (FK -> `CVs.Id`, IX)
- `SummarySectionScore` DECIMAL(4,3) NULL
- `ExperienceSectionScore` DECIMAL(4,3) NULL
- `SkillsSectionScore` DECIMAL(4,3) NULL
- `EducationSectionScore` DECIMAL(4,3) NULL
- `FormattingScore` DECIMAL(4,3) NULL
- `CreatedAt` DATETIME2 DEFAULT SYSUTCDATETIME()

5) `Profiles`
- `Id` UNIQUEIDENTIFIER (PK)
- `UserId` UNIQUEIDENTIFIER (FK -> `Users.Id`, UX)
- `Summary` NVARCHAR(500)
- `Skills` NVARCHAR(MAX)  -- JSON
- `Education` NVARCHAR(MAX)  -- JSON
- `Experience` NVARCHAR(MAX)  -- JSON
- `LinkedInUrl` NVARCHAR(100)
- `GitHubUrl` NVARCHAR(100)
- `PortfolioUrl` NVARCHAR(100)
- `UpdatedAt` DATETIME2

6) `Jobs` (augmented)
- `Id` UNIQUEIDENTIFIER (PK)
- `Title` NVARCHAR(200)
- `Description` NVARCHAR(MAX)
- `RequiredSkills` NVARCHAR(500)
- NEW: `RequiredSkillsJson` NVARCHAR(MAX)  -- canonical array
- NEW: `SeniorityLevel` NVARCHAR(50)  -- Junior/Mid/Senior/Lead
- `Location` NVARCHAR(200)
- NEW: `AllowsRemote` BIT DEFAULT 0
- `JobType` NVARCHAR(50)
- `SalaryMin` DECIMAL(18,2) NULL
- `SalaryMax` DECIMAL(18,2) NULL
- `ExperienceYears` INT NULL
- `EducationLevel` NVARCHAR(100) NULL
- NEW: `Benefits` NVARCHAR(500) NULL
- NEW: `RemotePolicy` NVARCHAR(50) NULL
- `IsActive` BIT
- `CreatedAt` DATETIME2
- `RecruiterId` UNIQUEIDENTIFIER (FK -> `Users.Id`)
- Indexes: `IX_Jobs_IsActive_CreatedAt`, `IX_Jobs_JobType`, `IX_Jobs_Recruiter`

7) `JobApplications` (augmented)
- `Id` UNIQUEIDENTIFIER (PK)
- `UserId` UNIQUEIDENTIFIER (FK -> `Users.Id`, IX)
- `JobId` UNIQUEIDENTIFIER (FK -> `Jobs.Id`, IX)
- `Status` NVARCHAR(50)
- NEW: `Source` NVARCHAR(50) NULL  -- Web, Mobile, Referral, Recommendation
- NEW: `ResumeVersionCVId` UNIQUEIDENTIFIER NULL (FK -> `CVs.Id`)
- `Notes` NVARCHAR(MAX) NULL
- `AppliedAt` DATETIME2
- `UpdatedAt` DATETIME2 NULL
- NEW: `ReviewedAt` DATETIME2 NULL
- NEW: `InterviewedAt` DATETIME2 NULL
- NEW: `OfferedAt` DATETIME2 NULL
- NEW: `HiredAt` DATETIME2 NULL
- NEW: `RejectedAt` DATETIME2 NULL
- NEW: `OutcomeReason` NVARCHAR(500) NULL

8) `JobMatch` (augmented)
- `Id` UNIQUEIDENTIFIER (PK)
- `UserId` UNIQUEIDENTIFIER (FK -> `Users.Id`, IX)
- `JobId` UNIQUEIDENTIFIER (FK -> `Jobs.Id`, IX)
- `MatchScore` DECIMAL(4,3)
- `MatchReason` NVARCHAR(MAX)
- NEW: `Rank` INT NULL
- NEW: `ExperimentGroup` NVARCHAR(50) NULL  -- A/B testing bucket
- `IsViewed` BIT DEFAULT 0
- NEW: `ViewedAt` DATETIME2 NULL
- `IsApplied` BIT DEFAULT 0
- NEW: `AppliedAt` DATETIME2 NULL
- NEW: `Dismissed` BIT DEFAULT 0
- NEW: `DismissReason` NVARCHAR(200) NULL
- `CreatedAt` DATETIME2 DEFAULT SYSUTCDATETIME()
- Unique Index suggestion: one row per user-job per generation window if needed

9) `Users` (relevant fields)
- `Id` UNIQUEIDENTIFIER (PK)
- `Email` NVARCHAR(256) (UX)
- `FirstName` NVARCHAR(100)
- `LastName` NVARCHAR(100)
- `Role` NVARCHAR(50)  -- Student, Recruiter, Admin

### New Supporting Entities

10) `CachedEmbeddings`
```sql
CREATE TABLE CachedEmbeddings (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SourceType NVARCHAR(50) NOT NULL,    -- CV, Job, Profile, Company
    SourceId UNIQUEIDENTIFIER NOT NULL,
    EmbeddingType NVARCHAR(50) NOT NULL, -- Skills, Experience, Description, Culture, Title
    Embedding VARBINARY(MAX) NOT NULL,   -- float[] normalized
    TextHash CHAR(64) NOT NULL,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UX_CachedEmbeddings UNIQUE (SourceType, SourceId, EmbeddingType, TextHash)
);
```

11) `SkillOntology` and `SkillAliases`
```sql
CREATE TABLE SkillOntology (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CanonicalName NVARCHAR(100) UNIQUE NOT NULL,
    Category NVARCHAR(50) NULL          -- Language, Framework, Tool, Soft, Domain
);
CREATE TABLE SkillAliases (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    OntologyId UNIQUEIDENTIFIER NOT NULL,
    Alias NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_SkillAliases_SkillOntology FOREIGN KEY (OntologyId) REFERENCES SkillOntology(Id) ON DELETE CASCADE,
    CONSTRAINT UX_SkillAliases UNIQUE (OntologyId, Alias)
);
```

12) `JobAnalytics` (optional for richer engagement events)
```sql
CREATE TABLE JobAnalytics (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    JobId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(50) NOT NULL,        -- Impression, View, ClickApply, Save, Share
    OccurredAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    Metadata NVARCHAR(MAX) NULL             -- JSON payload (durations, sections viewed)
);
CREATE INDEX IX_JobAnalytics_User_Job ON JobAnalytics(UserId, JobId);
CREATE INDEX IX_JobAnalytics_EventType ON JobAnalytics(EventType);
```

---

## Feature Engineering Pipeline

### Phase 1: Raw Data Extraction

#### 1.1 Student Feature Extraction

**Input Sources:**
- `CVAnalysisResult` (latest per student)
- `CVFeedback` (latest per student)
- `Profiles` (current profile)
- `User` (account metadata)

**Output: StudentFeatureVector**

```json
{
  "student_id": "guid",
  "timestamp": "2025-01-15T10:30:00Z",
  
  "text_embeddings": {
    "skills_embedding": [0.123, -0.456, ...], // 384-dim float array
    "experience_embedding": [0.789, ...],     // 384-dim
    "bio_embedding": [0.234, ...]            // 384-dim
  },
  
  "structured_features": {
    "years_experience": 3,
    "education_level": "Bachelor's",
    "cv_quality_score": 0.82,
    "is_cv_approved": true,
    "skills_list": ["Python", "SQL", "Machine Learning", "Docker"],
    "skills_count": 4,
    "has_linkedin": true,
    "has_github": true,
    "has_portfolio": false,
    "profile_completeness": 0.75
  },
  
  "behavioral_features": {
    "total_applications": 12,
    "applications_last_30_days": 3,
    "interview_rate": 0.25,
    "avg_days_to_apply": 2.5
  }
}
```

**Processing Steps:**

1. **Fetch Latest CV**
   ```sql
   SELECT TOP 1 * FROM CVs 
   WHERE UserId = @studentId AND IsActive = 1
   ORDER BY UploadedAt DESC
   ```

2. **Generate Text Embeddings**
   - Use Azure OpenAI `text-embedding-3-small` (384 dimensions)
   - Input: `ExtractedSkills`, `ExtractedExperience`, `Profile.Summary`
   - Cache embeddings to avoid repeated API calls

3. **Parse Structured Data**
   - Extract years from `ExtractedExperience` date ranges
   - Map education level to numeric scale
   - Parse skills into array (split by comma, trim whitespace)

4. **Calculate Derived Features**
   - Profile completeness = (filled fields / total fields)
   - Application frequency = applications / days since first application

#### 1.2 Job Feature Extraction

**Input Sources:**
- `Jobs` (job posting)
- `JobApplications` (for popularity metrics)
- `CompanyProfiles` (when available)

**Output: JobFeatureVector**

```json
{
  "job_id": "guid",
  "timestamp": "2025-01-15T10:30:00Z",
  
  "text_embeddings": {
    "title_embedding": [0.345, ...],          // 384-dim
    "description_embedding": [0.567, ...],    // 384-dim
    "requirements_embedding": [0.123, ...]    // 384-dim
  },
  
  "structured_features": {
    "required_skills": ["Java", "Spring Boot", "Kubernetes"],
    "required_skills_count": 3,
    "location": "New York, NY",
    "job_type": "Full-time",
    "min_experience_years": 2,
    "education_level": "Bachelor's",
    "salary_midpoint": 85000,
    "salary_normalized": 0.65,
    "is_active": true
  },
  
  "derived_features": {
    "days_since_posted": 5,
    "freshness_score": 1.0,
    "application_count": 23,
    "view_count": 145,
    "popularity_score": 0.78,
    "application_to_view_ratio": 0.159
  },
  
  "company_features": {
    "company_name": "TechCorp Inc.",
    "industry": "Technology",
    "company_size": "Medium",
    "remote_policy": "Hybrid",
    "culture_embedding": [0.789, ...]         // 384-dim (if company profile exists)
  }
}
```

**Processing Steps:**

1. **Generate Embeddings**
   - Title: Directly embed `Title` field
   - Description: Embed full `Description` (may need chunking if >8K tokens)
   - Requirements: Extract "Requirements" section from description or use `RequiredSkills`

2. **Calculate Popularity**
   ```sql
   SELECT 
       COUNT(DISTINCT UserId) as application_count,
       COUNT(DISTINCT CASE WHEN IsViewed = 1 THEN UserId END) as view_count
   FROM JobMatch
   WHERE JobId = @jobId
   ```

3. **Normalize Salary**
   ```sql
   Global salary stats:
   - Min: $30,000
   - Max: $200,000
   -- TODO: Query and calculate on insert/update
   normalized_salary = (salary_midpoint - 30000) / (200000 - 30000)
   ```

4. **Freshness Decay**
   ```sql
   days = (NOW - CreatedAt).TotalDays
   freshness = 1.0 - (days / 90)  // Linear decay over 90 days
   freshness = max(0, freshness)   // Floor at 0
   ```

---

### Phase 2: Similarity Feature Engineering

#### 2.1 Skills Matching

**Method 1: Exact Matching (Baseline)**

```
student_skills = {"Python", "SQL", "Docker", "AWS"}
job_required_skills = {"Python", "Java", "Docker", "Kubernetes"}

exact_matches = student_skills ? job_required_skills = {"Python", "Docker"}
exact_match_count = 2

skill_coverage = exact_match_count / len(job_required_skills) = 2/4 = 0.5
skill_relevance = exact_match_count / len(student_skills) = 2/4 = 0.5

jaccard_similarity = |intersection| / |union| = 2 / 6 = 0.333
```

**Method 2: Semantic Matching (Advanced)**

```
Use embeddings to find similar skills even if not exact:

student_skill_embedding = avg(embed("Python"), embed("SQL"), embed("Docker"), embed("AWS"))
job_skill_embedding = avg(embed("Python"), embed("Java"), embed("Docker"), embed("Kubernetes"))


semantic_similarity = cosine_similarity(student_skill_embedding, job_skill_embedding)

Result: ~0.75 (higher than Jaccard because "Java" and "Python" are semantically similar)
```

**Method 3: Weighted Skills (Best)**

```
Assign importance weights to each job skill:
- "Python" ? 1.0 (required)
- "Java" ? 0.8 (preferred)
- "Docker" ? 0.6 (nice to have)
- "Kubernetes" ? 0.4 (optional)

For each student skill:
  If exact match: score += job_skill_weight
  Else if semantic_similarity > 0.7: score += job_skill_weight * 0.5

weighted_skill_score = score / sum(job_skill_weights)
```

#### 2.2 Experience Matching

**Text-Based Approach:**

```
student_experience_text = "Worked as Software Engineer at TechCorp for 2 years..."
job_description = "We are looking for a mid-level developer with experience in..."

experience_embedding_similarity = cosine_similarity(
    embed(student_experience_text),
    embed(job_description)
)

Typical range: 0.4 - 0.8
Interpretation:
- > 0.7: High relevance
- 0.5-0.7: Moderate relevance
- < 0.5: Low relevance
```

**Numeric Experience Gap:**

```
student_years = 3
required_years = 2

experience_gap = student_years - required_years = 1

experience_match_score:
  If gap >= 0: 1.0 (meets or exceeds requirement)
  If gap == -1: 0.7 (1 year short)
  If gap == -2: 0.4 (2 years short)
  If gap <= -3: 0.0 (significantly under-qualified)
```

**Combined Experience Score:**

```
experience_score = (
    0.6 * experience_embedding_similarity +
    0.4 * experience_match_score
)
```

#### 2.3 Education Matching

**Level Mapping:**

```
High School = 1
Associate's = 2
Bachelor's = 3
Master's = 4
PhD = 5

student_education_level = 3 (Bachelor's)
required_education_level = 3 (Bachelor's)

education_match:
  If student >= required: 1.0
  If student == required - 1: 0.5 (e.g., Associate's when Bachelor's required)
  If student < required - 1: 0.0
```

**Field of Study Matching (if available):**

```
student_major = "Computer Science"
job_preferred_major = "Computer Science, Software Engineering, related field"

field_match = embed_similarity(student_major, job_preferred_major)
```


#### 2.4 Location Matching

**Geographic Distance:**

```
student_location = "Boston, MA"
job_location = "New York, NY"

distance_km = 350

location_score:
  If job allows "Remote": 1.0
  If distance < 50 km: 1.0
  If 50 <= distance < 200: 0.7
  If 200 <= distance < 500: 0.4
  If distance >= 500: 0.1
```

**Relocation Willingness:**

```
If student indicates "willing to relocate":
    location_score = max(location_score, 0.8)
```


#### 2.5 Salary Compatibility

**Range Overlap:**

```
student_min = 70,000
student_max = 90,000

job_min = 75,000
job_max = 95,000

overlap_start = max(student_min, job_min) = 75,000
overlap_end = min(student_max, job_max) = 90,000

Has overlap: Yes
overlap_amount = 15,000

salary_match = overlap_amount / (student_max - student_min) = 15,000 / 20,000 = 0.75
```

**No Salary Data:**

```
If job doesn't specify salary:
    salary_match = 0.5 (neutral, don't penalize)
```

---

### Phase 3: Composite Feature Construction

#### 3.1 Overall Match Score Calculation

**Weighted Linear Combination (Simple Baseline):**

```
match_score = (
    0.35 * skills_similarity +
    0.25 * experience_similarity +
    0.15 * education_match +
    0.10 * location_match +
    0.10 * salary_compatibility +
    0.05 * cv_quality_score
)

Weights based on domain expertise and can be tuned via hyperparameter search
```

**Feature Vector for ML Model:**

```python
features = [
    # Similarity features (most important)
    skills_exact_match_count,      # Integer
    skills_jaccard_similarity,     # Float 0-1
    skills_semantic_similarity,    # Float 0-1
    experience_embedding_similarity, # Float 0-1
    experience_years_gap,          # Integer (can be negative)
    education_level_match,         # Float 0-1
    location_match_score,          # Float 0-1
    salary_compatibility,          # Float 0-1
    
    # Student quality features
    cv_quality_score,              # Float 0-1
    profile_completeness,          # Float 0-1
    years_experience,              # Integer
    total_skills_count,            # Integer
    has_github,                    # Boolean (0/1)
    has_portfolio,                 # Boolean (0/1)
    
    # Job features
    job_freshness_score,           # Float 0-1
    job_popularity_normalized,     # Float 0-1
    required_experience_years,     # Integer
    salary_midpoint_normalized,    # Float 0-1
    
    # Interaction features
    student_application_count,     # Integer
    student_interview_rate,        # Float 0-1
    
    # Categorical features (one-hot encoded)
    job_type_fulltime,             # Boolean
    job_type_internship,           # Boolean
    job_type_contract,             # Boolean
    remote_policy_remote,          # Boolean
    remote_policy_hybrid,          # Boolean
    remote_policy_onsite           # Boolean
]

Total features: ~30-40 dimensions
```

---

### Phase 4: Training Data Preparation

#### 4.1 Positive Sample Collection

**SQL Query:**

```sql
SELECT 
    ja.UserId AS StudentId,
    ja.JobId,
    ja.Status,
    CASE 
        WHEN ja.Status = 'Hired' THEN 1.0
        WHEN ja.Status = 'Offer' THEN 0.9
        WHEN ja.Status = 'Interview' THEN 0.7
        WHEN ja.Status = 'Reviewed' THEN 0.5
        WHEN ja.Status = 'Applied' THEN 0.3
        WHEN ja.Status = 'Rejected' THEN 0.0
    END AS Label,
    ja.AppliedAt,
    DATEDIFF(day, ja.AppliedAt, ISNULL(ja.UpdatedAt, GETUTCDATETIME())) AS DaysInStatus
FROM JobApplications ja
WHERE ja.Status IN ('Applied', 'Reviewed', 'Interview', 'Offer', 'Hired', 'Rejected')
ORDER BY ja.AppliedAt DESC;
```

**Expected Output:** ~220 positive samples (based on current data)

#### 4.2 Negative Sample Generation

**Strategy:** For each positive sample, generate 2-3 negative samples

**Constraints for Valid Negative Samples:**

1. **Temporal Constraint:** Job must have been posted before or during student's application period
2. **Visibility Constraint:** Job must have been active when student was active
3. **Job Type Constraint:** Same job type category (don't compare internships to full-time)
4. **Diversity Constraint:** Sample from different recruiters to avoid bias

**SQL Query:**

```sql
-- For each student application
DECLARE @StudentId UNIQUEIDENTIFIER = 'student-guid';
DECLARE @ApplicationDate DATETIME2 = '2025-01-10';
DECLARE @PositiveJobIds TABLE (JobId UNIQUEIDENTIFIER);

-- Get jobs the student actually applied to
INSERT INTO @PositiveJobIds
SELECT JobId FROM JobApplications WHERE UserId = @StudentId;

-- Sample negative jobs
SELECT TOP 3
    @StudentId AS StudentId,
    j.Id AS JobId,
    0.0 AS Label,
    'NotApplied' AS Status
FROM Jobs j
WHERE j.Id NOT IN (SELECT JobId FROM @PositiveJobIds)
  AND j.CreatedAt <= @ApplicationDate
  AND j.IsActive = 1
  AND DATEDIFF(day, j.CreatedAt, @ApplicationDate) <= 30  -- Posted within 30 days before
ORDER BY NEWID();  -- Random sampling
```

**Expected Output:** ~440-660 negative samples (2-3x positives)

**Final Training Dataset Size:** ~660-880 samples

#### 4.3 Train/Test Split

**Recommended Split:**

- **Training Set:** 70% (~462-616 samples)
- **Validation Set:** 15% (~99-132 samples)
- **Test Set:** 15% (~99-132 samples)

**Splitting Strategy:**

**Option 1: Random Split** (Simple but may leak temporal info)
```
Randomly assign each sample to train/val/test
```

**Option 2: Temporal Split** (Better for production deployment)
```
- Training: Applications before 2025-01-01
- Validation: Applications 2025-01-01 to 2025-01-15
- Test: Applications after 2025-01-15

Prevents training on future data
```

**Option 3: Student-Based Split** (Ensures no student appears in multiple sets)
```
- Randomly assign students to train/val/test
- All applications from a student go to the same set
- Tests model's ability to generalize to new students
```

**Recommendation:** Use **Option 3** for initial development, **Option 2** for production validation

---

## Feature Store Schemas

Persist features for offline training, audits, and faster online scoring.

1) `StudentFeatureSnapshots`
```sql
CREATE TABLE StudentFeatureSnapshots (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SnapshotAt DATETIME2 DEFAULT SYSUTCDATETIME(),

    SkillsEmbedding VARBINARY(MAX) NULL,
    ExperienceEmbedding VARBINARY(MAX) NULL,
    BioEmbedding VARBINARY(MAX) NULL,

    StructuredFeatures NVARCHAR(MAX) NULL,      -- JSON: { years_experience, education_level_num, cv_quality_score, has_github, ... }
    BehavioralFeatures NVARCHAR(MAX) NULL,      -- JSON: { total_applications, applications_last_30_days, interview_rate, ... }

    Version NVARCHAR(20) NULL,                  -- feature set versioning
    CONSTRAINT IX_StudentFeatureSnapshots_UserId_SnapshotAt UNIQUE (UserId, SnapshotAt)
);
```

2) `JobFeatureSnapshots`
```sql
CREATE TABLE JobFeatureSnapshots (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    JobId UNIQUEIDENTIFIER NOT NULL,
    SnapshotAt DATETIME2 DEFAULT SYSUTCDATETIME(),

    TitleEmbedding VARBINARY(MAX) NULL,
    DescriptionEmbedding VARBINARY(MAX) NULL,
    RequirementsEmbedding VARBINARY(MAX) NULL,

    StructuredFeatures NVARCHAR(MAX) NULL,      -- JSON: { required_skills, required_skills_count, location, job_type, ... }
    DerivedFeatures NVARCHAR(MAX) NULL,         -- JSON: { days_since_posted, freshness_score, popularity_score, ... }

    Version NVARCHAR(20) NULL,
    CONSTRAINT IX_JobFeatureSnapshots_JobId_SnapshotAt UNIQUE (JobId, SnapshotAt)
);
```

3) `PairwiseFeatures` (student-job pair used for training and online re-ranking)
```sql
CREATE TABLE PairwiseFeatures (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    JobId UNIQUEIDENTIFIER NOT NULL,
    SnapshotAt DATETIME2 DEFAULT SYSUTCDATETIME(),

    FeatureVector NVARCHAR(MAX) NOT NULL,        -- JSON: flattened list or name->value map
    Label DECIMAL(3,2) NULL,                    -- when known (0..1)
    Source NVARCHAR(50) NULL,                    -- Training, OnlineScoring

    CONSTRAINT UX_PairwiseFeatures UNIQUE (UserId, JobId, SnapshotAt)
);
```

---

## Data Quality Requirements

### Minimum Data Thresholds

| Data Type | Minimum Records | Current Status | Action Required |
|-----------|-----------------|----------------|-----------------|
| **Student CVs** | 50 active | ~128 | None |
| **Job Postings** | 30 active | ~35 | None |
| **Applications** | 200 | ~220 | None |
| **Completed Hires** | 5 | ~4 (estimated) | Need more time to collect |
| **Company Profiles** | 10 | 0 | Create table & populate |
| **User Preferences** | 20 | 0 (inferred only) | Create table & add UI |

### Data Completeness Requirements

...existing content unchanged...

### Data Freshness Requirements

...existing content unchanged...

### Data Consistency Checks

...existing content unchanged...

---

## Data Gaps & Recommendations

### Critical Gaps (Must Fix)

1) Company Culture Data
- Impact: High — Cannot match students to company values
- Solution: Create `CompanyProfiles` + recruiter UI + backfill
- Priority: Critical

2) User Preference Data
- Impact: High — Cannot filter by student preferences
- Solution: Create `UserPreferences` + onboarding flow
- Priority: Critical

3) Insufficient Hire Outcome Data
- Impact: Medium — Labels skewed to "Applied"
- Solution: Weighted training; collect more outcomes; consider transfer learning
- Priority: Medium

### Moderate Gaps (Should Fix)

4) Skill Taxonomy Standardization
- Solution: `SkillOntology` + aliases; normalization service

5) Job Description Parsing
- Solution: GPT-based extractor for responsibilities, must-have / nice-to-have, seniority

6) Application View/Click Tracking
- Solution: `JobAnalytics` for fine-grained events

### Minor Gaps (Nice to Have)

7) Student Career Progression Tracking

8) Recruiter Feedback on Candidates (rating + reasons)

---

## Implementation Workstreams (No Calendar)

Workstreams can run in parallel with clear interfaces and contracts.

- Workstream A: Data & Storage
  - Design and migrate DB schemas (`CompanyProfiles`, `UserPreferences`, `CachedEmbeddings`, `SkillOntology`, `JobAnalytics`, feature snapshot tables)
  - Add indexes and constraints; ensure seeders updated
  - Data validation jobs (consistency checks, completeness reports)

- Workstream B: Extraction & Feature Engineering
  - CV parsing enhancements; JSON outputs into `CVAnalysisResult` new columns
  - Embedding generation service with caching (`CachedEmbeddings`)
  - Feature calculators (student, job, pairwise) -> write to Feature Store tables
  - Skill normalization pipeline using `SkillOntology`

- Workstream C: APIs & Analytics
  - `/api/recommendations` endpoint with explainability
  - A/B experimentation plumbing (`ExperimentGroup`, rank logging)
  - Analytics ingestion (`JobAnalytics`) and metrics (Precision@K, CTR, Conversion)
  - Admin/reporting endpoints for data quality dashboards

---

## Team Workload Split (3 Developers)

Define three parallel swimlanes with deliverables and acceptance criteria.

Developer 1 — Data & DB Engineer
- Migrations
  - Create: `CompanyProfiles`, `UserPreferences`, `CachedEmbeddings`, `SkillOntology`, `SkillAliases`, `JobAnalytics`, `StudentFeatureSnapshots`, `JobFeatureSnapshots`, `PairwiseFeatures`
  - Augment: `CVAnalysisResult` (JSON columns), `Jobs` (new columns), `JobApplications` (stage timestamps), `JobMatch` (rank, experiment, events)
- Indexing & Constraints
  - Add recommended indexes; enforce unique constraints and FKs
- Data Seeding & Backfill
  - Backfill `RequiredSkillsJson` from `RequiredSkills`
  - Canonicalize `ExtractedSkills` -> `SkillsJson` via taxonomy
- Acceptance Criteria
  - All migrations apply cleanly
  - EF Core models updated; database round-trip tests pass
  - Query plans show index usage on key paths

Developer 2 — ML & Feature Engineering
- Embedding Service
  - Azure OpenAI client; batch embeddings; normalization; `CachedEmbeddings` integration
- CV Extraction Enhancements
  - Parsers to produce `SkillsJson`, `ExperienceJson`, `EducationJson`, `ProjectsJson`, etc.
- Feature Pipelines
  - Student, Job, Pairwise feature computation; write to Feature Store
  - Skills similarity (exact, semantic, weighted); experience and education scores
- Acceptance Criteria
  - Deterministic features with versioning
  - Unit tests for similarity metrics and parsers
  - Pairwise samples exported for training with expected schema

Developer 3 — API & Analytics
- Recommendation API
  - Endpoint returning ranked jobs + reasons; supports pagination and experiment group
- Explainability & Logging
  - Populate `MatchReason`; log `Rank`, `ViewedAt`, `AppliedAt`, `Dismissed`
- Analytics Pipeline
  - Ingest `JobAnalytics` events; compute metrics (Precision@K, MRR, CTR, Conversion)
  - Admin endpoints/dashboards for monitoring
- Acceptance Criteria
  - Load-tested API with p95 latency target met
  - A/B split applied and logged; metrics computed daily
  - Security and input validation checks pass

Cross-cutting
- Contracts
  - Clear DTOs for embeddings and features
  - Feature names/types catalog maintained in repo
- CI/CD
  - Lint, tests, migrations, and seeded data in pipelines

Recommended Ticket Breakdown (sample)
- DB-01 Create `CompanyProfiles` (+ EF models)
- DB-02 Create `UserPreferences` (+ EF models)
- DB-03 Create `CachedEmbeddings` and service interface
- DB-04 Augment `CVAnalysisResult` with JSON columns
- DB-05 Augment `Jobs` and backfill `RequiredSkillsJson`
- FEAT-01 Implement skill taxonomy and normalization service
- FEAT-02 Embedding client (batch + caching + normalization)
- FEAT-03 Student feature snapshot writer
- FEAT-04 Job feature snapshot writer
- FEAT-05 Pairwise feature generator + exporter
- API-01 Recommendations endpoint with explainability
- API-02 A/B experiment assignment + logging
- API-03 Analytics ingestion + metrics endpoints
- QA-01 Data quality validation suite

---

## Appendix B: Embedding Strategy

**Recommended Model:** `text-embedding-3-small`
- Dimensions: 384 (vs 1536 for large)
- Cost: $0.02 per 1M tokens
- Performance: 98% of `text-embedding-3-large` quality
- Speed: 2x faster

**Alternative:** `text-embedding-ada-002` (legacy, still good)

### Embedding Generation Best Practices

**1. Text Preprocessing**
```
Original: "Python, Machine Learning, AWS, Docker"
Processed: "python machine learning aws docker"

Rules:
- Lowercase all text
- Remove special characters
- Remove duplicate spaces
- Preserve word boundaries
```

**2. Chunking Long Text**
```
If text > 8,000 tokens (rare for CVs):
- Split into chunks at sentence boundaries
- Generate embedding per chunk
- Average embeddings or use max pooling
```

**3. Caching Strategy**
```
Cache embeddings in database to avoid re-generation:

Table: CachedEmbeddings
- Id: UNIQUEIDENTIFIER
- SourceType: VARCHAR(50) -- 'CV', 'Job', 'Profile'
- SourceId: UNIQUEIDENTIFIER
- EmbeddingType: VARCHAR(50) -- 'Skills', 'Experience', 'Description'
- Embedding: VARBINARY(MAX) -- Serialized float array
- CreatedAt: DATETIME2
- TextHash: VARCHAR(64) -- SHA256 of input text (for cache invalidation)
```

**4. Batch Processing**
```
Process multiple texts in single API call:
- Azure OpenAI supports batch embedding
- Up to 16 texts per request
- 90% cost reduction vs individual calls
```

### Similarity Calculation

**Cosine Similarity:**
```
Given two embeddings A and B:

cosine_similarity(A, B) = (A · B) / (||A|| * ||B||)

Where:
- A · B = sum(A[i] * B[i]) for all i
- ||A|| = sqrt(sum(A[i]^2))

Result: -1.0 to 1.0 (higher = more similar)

Interpretation:
- > 0.8: Very similar
- 0.6-0.8: Moderately similar
- 0.4-0.6: Somewhat similar
- < 0.4: Not similar
```

**Optimization:**
```
Pre-compute and normalize embeddings:
1. Generate embedding
2. Normalize to unit length: A_norm = A / ||A||
3. Store normalized version

Then cosine similarity becomes simple dot product:
similarity = A_norm · B_norm
```

---

## Appendix C: Sample Feature Extraction Code (Pseudocode)

```csharp
public class FeatureEngineeringService
{
    public async Task<StudentJobFeatures> ExtractFeaturesAsync(
        Guid studentId, 
        Guid jobId)
    {
        // 1. Fetch raw data
        var student = await GetStudentDataAsync(studentId);
        var job = await GetJobDataAsync(jobId);
        
        // 2. Generate embeddings (cached)
        var studentSkillsEmbed = await GetOrGenerateEmbeddingAsync(
            "Student-Skills", studentId, student.ExtractedSkills);
        var jobSkillsEmbed = await GetOrGenerateEmbeddingAsync(
            "Job-Skills", jobId, job.RequiredSkills);
        
        // 3. Calculate similarity features
        var skillsSimilarity = CosineSimilarity(studentSkillsEmbed, jobSkillsEmbed);
        var experienceSimilarity = CosineSimilarity(
            student.ExperienceEmbedding, job.DescriptionEmbedding);
        
        // 4. Calculate structured features
        var exactSkillMatches = CountExactMatches(
            student.SkillsList, job.RequiredSkillsList);
        var experienceGap = student.YearsExperience - (job.MinExperience ?? 0);
        
        // 5. Construct feature vector
        return new StudentJobFeatures
        {
            // Similarity features
            SkillsSimilarity = skillsSimilarity,
            ExperienceSimilarity = experienceSimilarity,
            ExactSkillMatchCount = exactSkillMatches,
            
            // Gap features
            ExperienceGap = experienceGap,
            EducationMatch = student.EducationLevel >= job.RequiredEducation,
            
            // Quality features
            CVQualityScore = student.CVQualityScore,
            JobPopularityScore = job.ApplicationCount / 100.0f,
            
            // ... additional features
        };
    }
    
    private async Task<float[]> GetOrGenerateEmbeddingAsync(
        string type, Guid id, string text)
    {
        var cached = await _cache.GetEmbeddingAsync(type, id, text);
        if (cached != null) return cached;
        
        var embedding = await _openAI.GenerateEmbeddingAsync(text);
        await _cache.StoreEmbeddingAsync(type, id, text, embedding);
        return embedding;
    }
}
```

---

## Appendix D: Training Data Quality Checklist

Before training ML model, verify:

- [ ] **Minimum 200 training samples** collected
- [ ] **Positive:Negative ratio** is 1:2 or 1:3
- [ ] **Class distribution** logged (Applied:Reviewed:Interview:Hired)
- [ ] **No data leakage** (test set is truly held out)
- [ ] **Temporal consistency** (training data is older than test data)
- [ ] **No duplicate samples** (same student-job pair)
- [ ] **All embeddings generated** and cached
- [ ] **All features normalized** to 0-1 range (except categorical)
- [ ] **Missing values handled** (imputed or flagged)
- [ ] **Outliers identified** and logged (e.g., 50 years experience)
- [ ] **Feature correlations checked** (remove highly correlated pairs)
- [ ] **Data balanced by job type** (not all internships or all full-time)
- [ ] **Data balanced by industry** (diverse job sectors)
- [ ] **Data balanced by student experience level** (junior to senior)

---

## Appendix E: Evaluation Metrics Definitions

### Precision @ K
```
For each student, recommend top K jobs (e.g., K=10)
Precision@K = (Number of jobs applied to from top K) / K

Example:
Top 10 recommendations for Student A: [Job1, Job2, ..., Job10]
Student applied to: Job2, Job7
Precision@10 = 2/10 = 0.20 (20%)

Industry benchmark: 15-30% for job recommendations
```

### Mean Reciprocal Rank (MRR)
```
For each student, find rank of first job they applied to

Example:
Recommendation list: [Job1, Job2, Job3, Job4, ...]
First application: Job3 (rank 3)
Reciprocal rank = 1/3 = 0.333

MRR = Average reciprocal rank across all students

Industry benchmark: 0.3-0.5 (first application in top 2-3)
```

### Normalized Discounted Cumulative Gain (NDCG)
```
Accounts for position and relevance:
- Higher relevance at top positions = better score
- Relevance = application outcome (Hired > Interview > Applied)

NDCG@K = DCG@K / Ideal_DCG@K

Where DCG@K = sum((2^relevance[i] - 1) / log2(i + 1)) for i=1 to K

Industry benchmark: >0.5 for job recommendations
```

### Click-Through Rate (CTR)
```
CTR = (Jobs clicked/viewed) / (Jobs recommended)

Example:
Recommended 10 jobs to student
Student clicked on 3 jobs
CTR = 3/10 = 30%

Industry benchmark: 15-25%
```

### Conversion Rate
```
Conversion = (Jobs applied to) / (Jobs clicked)

Example:
Student viewed 5 jobs
Student applied to 2 jobs
Conversion = 2/5 = 40%

Industry benchmark: 40-60%
```

---
