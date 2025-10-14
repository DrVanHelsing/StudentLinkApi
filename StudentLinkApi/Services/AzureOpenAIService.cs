using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace StudentLinkApi.Services;

public interface IAzureOpenAIService
{
    Task<CVQualityAnalysis> AnalyzeCVQualityAsync(string cvText);
    Task<string[]> ExtractSkillsAsync(string cvText);
    Task<double> CalculateJobMatchScoreAsync(string cvText, string jobDescription);
    Task<InteractiveCVAnalysis> AnalyzeCVInteractiveAsync(string cvText, string? previousCVText = null);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
    {
        var endpoint = configuration["Azure:OpenAI:Endpoint"] ?? throw new InvalidOperationException("OpenAI endpoint not configured");
        var apiKey = configuration["Azure:OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        _deploymentName = configuration["Azure:OpenAI:DeploymentName"] ?? "gpt-5-mini";
        
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _logger = logger;
    }

    public async Task<CVQualityAnalysis> AnalyzeCVQualityAsync(string cvText)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var prompt = "Analyze this CV and provide detailed feedback in JSON format:\n\n" +
                $"CV Text:\n{cvText}\n\n" +
                "Evaluate the CV on the following criteria and return a JSON object with these exact fields:\n" +
                "{\n" +
                "    \"qualityScore\": <0.0 to 1.0>,\n" +
                "    \"structureIssues\": \"<list of structural problems>\",\n" +
                "    \"grammarIssues\": \"<list of grammar and spelling errors>\",\n" +
                "    \"missingFields\": \"<list of important missing sections>\",\n" +
                "    \"recommendations\": \"<specific actionable recommendations>\",\n" +
                "    \"isApproved\": <true/false>,\n" +
                "    \"overallFeedback\": \"<2-3 sentence summary>\"\n" +
                "}\n\n" +
                "Be constructive and specific in your feedback.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an expert CV reviewer and career counselor. Analyze CVs and provide constructive feedback."),
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);

            var content = response.Value.Content[0].Text;
            
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}') + 1;
            var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart);
            
            var analysis = JsonSerializer.Deserialize<CVQualityAnalysis>(jsonContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _logger.LogInformation("CV quality analysis completed with score: {Score}", analysis?.QualityScore);
            return analysis ?? new CVQualityAnalysis();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing CV quality");
            throw;
        }
    }

    public async Task<InteractiveCVAnalysis> AnalyzeCVInteractiveAsync(string cvText, string? previousCVText = null)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var comparisonSection = string.IsNullOrEmpty(previousCVText) ? "" : 
                "\n\nPREVIOUS CV VERSION (for comparison):\n" + previousCVText +
                "\n\nPlease also provide 'improvementFromPrevious' explaining what improved.";

            var prompt = @"Analyze this CV section-by-section and provide detailed, actionable feedback with specific examples.

CURRENT CV:
" + cvText + comparisonSection + @"
Return a JSON object with this structure:
{
  ""overallScore"": 0.0-1.0,
  ""isApproved"": true/false,
  ""contactSectionFeedback"": ""Detailed feedback with specific examples"",
  ""contactSectionScore"": 0.0-1.0,
  ""summarySectionFeedback"": ""Detailed feedback with specific examples of good summaries"",
  ""summarySectionScore"": 0.0-1.0,
  ""experienceSectionFeedback"": ""Detailed feedback with before/after examples"",
  ""experienceSectionScore"": 0.0-1.0,
  ""educationSectionFeedback"": ""Detailed feedback with formatting examples"",
  ""educationSectionScore"": 0.0-1.0,
  ""skillsSectionFeedback"": ""Detailed feedback with categorization examples"",
  ""skillsSectionScore"": 0.0-1.0,
  ""improvementPriorities"": [
    {
      ""section"": ""Section name"",
      ""priority"": ""High/Medium/Low"",
      ""action"": ""Specific action to take"",
      ""reason"": ""Why this matters"",
      ""example"": ""Before: 'Worked on projects' ? After: 'Led 3-person team to deliver e-commerce platform, increasing sales by 40%'"",
      ""isCompleted"": false
    }
  ],
  ""nextSteps"": ""Clear next actions with timeline"",
  ""improvementFromPrevious"": ""Changes since last version (if applicable)""
}

IMPORTANT: For each improvement action, provide a concrete before/after example showing exactly how to improve. Be specific and actionable. Focus on measurable achievements, action verbs, and quantifiable results.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are an expert CV coach. Provide detailed, actionable feedback with specific examples. 
For every suggestion, include a before/after example showing exactly what to change. 
Use real-world scenarios and be encouraging while being honest about areas for improvement."),
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            var content = response.Value.Content[0].Text;
            
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}') + 1;
            var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart);
            
            var analysis = JsonSerializer.Deserialize<InteractiveCVAnalysis>(jsonContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _logger.LogInformation("Interactive CV analysis completed with score: {Score}", analysis?.OverallScore);
            return analysis ?? new InteractiveCVAnalysis();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in interactive CV analysis");
            throw;
        }
    }

    public async Task<string[]> ExtractSkillsAsync(string cvText)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var prompt = $"Extract all technical and professional skills from this CV.\nReturn ONLY a JSON array of skills.\n\nCV Text:\n{cvText}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a skill extraction expert. Extract skills from CVs."),
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);

            var content = response.Value.Content[0].Text;
            
            var jsonStart = content.IndexOf('[');
            var jsonEnd = content.LastIndexOf(']') + 1;
            var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart);
            
            var skills = JsonSerializer.Deserialize<string[]>(jsonContent) ?? Array.Empty<string>();
            
            _logger.LogInformation("Extracted {Count} skills from CV", skills.Length);
            return skills;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting skills");
            return Array.Empty<string>();
        }
    }

    public async Task<double> CalculateJobMatchScoreAsync(string cvText, string jobDescription)
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);

            var prompt = $"Calculate how well this CV matches the job description.\nReturn ONLY a number between 0.0 and 1.0.\n\nCV:\n{cvText}\n\nJob Description:\n{jobDescription}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a job matching AI. Calculate match scores between CVs and jobs."),
                new UserChatMessage(prompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);

            var content = response.Value.Content[0].Text.Trim();
            
            if (double.TryParse(content, out var score))
            {
                _logger.LogInformation("Job match score calculated: {Score}", score);
                return Math.Clamp(score, 0.0, 1.0);
            }

            _logger.LogWarning("Failed to parse match score: {Content}", content);
            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating job match score");
            return 0.0;
        }
    }
}

public class CVQualityAnalysis
{
    public double QualityScore { get; set; }
    public string StructureIssues { get; set; } = string.Empty;
    public string GrammarIssues { get; set; } = string.Empty;
    public string MissingFields { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string OverallFeedback { get; set; } = string.Empty;
}

public class InteractiveCVAnalysis
{
    public double OverallScore { get; set; }
    public bool IsApproved { get; set; }
    
    public string ContactSectionFeedback { get; set; } = string.Empty;
    public double ContactSectionScore { get; set; }
    
    public string SummarySectionFeedback { get; set; } = string.Empty;
    public double SummarySectionScore { get; set; }
    
    public string ExperienceSectionFeedback { get; set; } = string.Empty;
    public double ExperienceSectionScore { get; set; }
    
    public string EducationSectionFeedback { get; set; } = string.Empty;
    public double EducationSectionScore { get; set; }
    
    public string SkillsSectionFeedback { get; set; } = string.Empty;
    public double SkillsSectionScore { get; set; }
    
    public List<ImprovementActionItem> ImprovementPriorities { get; set; } = new();
    public string NextSteps { get; set; } = string.Empty;
    public string? ImprovementFromPrevious { get; set; }
}

public class ImprovementActionItem
{
    public string Section { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty; // NEW: Before/after example
    public bool IsCompleted { get; set; }
}