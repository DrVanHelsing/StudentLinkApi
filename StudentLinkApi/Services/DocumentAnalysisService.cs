using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace StudentLinkApi.Services;

public interface IDocumentAnalysisService
{
    Task<string> ExtractTextFromPdfAsync(Stream pdfStream);
    Task<CVExtractedData> ExtractStructuredDataAsync(Stream documentStream);
}

public class AzureDocumentAnalysisService : IDocumentAnalysisService
{
    private readonly DocumentAnalysisClient _client;
    private readonly ILogger<AzureDocumentAnalysisService> _logger;

    public AzureDocumentAnalysisService(IConfiguration configuration, ILogger<AzureDocumentAnalysisService> logger)
    {
        var endpoint = configuration["Azure:FormRecognizer:Endpoint"] ?? throw new InvalidOperationException("Form Recognizer endpoint not configured");
        var apiKey = configuration["Azure:FormRecognizer:ApiKey"] ?? throw new InvalidOperationException("Form Recognizer API key not configured");
        
        _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _logger = logger;
    }

    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        try
        {
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", pdfStream);
            var result = operation.Value;

            var text = string.Join("\n", result.Pages.SelectMany(page => 
                page.Lines.Select(line => line.Content)));

            _logger.LogInformation("Extracted {Length} characters from document", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            throw;
        }
    }

    public async Task<CVExtractedData> ExtractStructuredDataAsync(Stream documentStream)
    {
        try
        {
            // Using prebuilt-document model for general document analysis
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", documentStream);
            var result = operation.Value;

            var extractedData = new CVExtractedData
            {
                FullText = string.Join("\n", result.Pages.SelectMany(p => p.Lines.Select(l => l.Content))),
                ExtractedAt = DateTime.UtcNow
            };

            // Extract key-value pairs
            foreach (var kvp in result.KeyValuePairs)
            {
                var key = kvp.Key.Content.ToLower();
                var value = kvp.Value?.Content ?? string.Empty;

                if (key.Contains("email"))
                    extractedData.Email = value;
                else if (key.Contains("phone") || key.Contains("mobile"))
                    extractedData.Phone = value;
                else if (key.Contains("name") && string.IsNullOrEmpty(extractedData.Name))
                    extractedData.Name = value;
            }

            // Extract skills (look for common skill sections)
            var skillsSection = result.Pages
                .SelectMany(p => p.Lines)
                .Where(l => l.Content.ToLower().Contains("skills") || l.Content.ToLower().Contains("technologies"))
                .ToList();

            if (skillsSection.Any())
            {
                extractedData.Skills = string.Join(", ", skillsSection.Select(s => s.Content));
            }

            _logger.LogInformation("Structured data extraction completed");
            return extractedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting structured data");
            throw;
        }
    }
}

public class CVExtractedData
{
    public string FullText { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Skills { get; set; }
    public string? Education { get; set; }
    public string? Experience { get; set; }
    public DateTime ExtractedAt { get; set; }
}