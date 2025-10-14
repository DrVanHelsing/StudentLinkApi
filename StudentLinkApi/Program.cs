using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StudentLinkApi.Data;
using StudentLinkApi.Services;
using Azure.Identity;
using StudentLinkApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container (force camelCase JSON for frontend expectations)
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.CommandTimeout(60);
        sql.EnableRetryOnFailure(5);
    }));

// Configure JWT Authentication with diagnostic logging & small clock skew tolerance
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var issuer = jwtSettings["Issuer"] ?? "StudentLinkAPI";
var audience = jwtSettings["Audience"] ?? "StudentLinkClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        // Allow slight client/server clock differences
        ClockSkew = TimeSpan.FromMinutes(2)
    };

    // Add event logging for troubleshooting auto sign-out
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogWarning(ctx.Exception, "JWT authentication failed. Path: {Path}", ctx.HttpContext.Request.Path);
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
            logger.LogInformation("JWT challenge at path {Path}. Error: {Error} Description: {Desc}", ctx.HttpContext.Request.Path, ctx.Error, ctx.ErrorDescription);
            return Task.CompletedTask;
        },
        OnMessageReceived = ctx =>
        {
            // Helpful for verifying token presence
            if (string.IsNullOrEmpty(ctx.Token))
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                logger.LogDebug("No JWT token found in request headers for path {Path}", ctx.HttpContext.Request.Path);
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Register Core Services
builder.Services.AddScoped<IJwtService, JwtService>();

// Register File Storage Service
var useAzureStorage = builder.Configuration.GetValue<bool>("FileStorage:UseAzure");
if (useAzureStorage)
{
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}

// Enforce Azure AI usage (fail fast if misconfigured)
var openAiEndpoint = builder.Configuration["Azure:OpenAI:Endpoint"]; // e.g. https://YOUR-RESOURCE.openai.azure.com/
var openAiKey = builder.Configuration["Azure:OpenAI:ApiKey"];      // key from Azure portal
var openAiDeployment = builder.Configuration["Azure:OpenAI:DeploymentName"]; // model deployment name
var formRecEndpoint = builder.Configuration["Azure:FormRecognizer:Endpoint"]; // e.g. https://YOUR-RESOURCE.cognitiveservices.azure.com/
var formRecKey = builder.Configuration["Azure:FormRecognizer:ApiKey"];       // key from Azure portal

if (string.IsNullOrWhiteSpace(openAiEndpoint) ||
    string.IsNullOrWhiteSpace(openAiKey) ||
    string.IsNullOrWhiteSpace(openAiDeployment) ||
    string.IsNullOrWhiteSpace(formRecEndpoint) ||
    string.IsNullOrWhiteSpace(formRecKey))
{
    throw new InvalidOperationException("Azure AI is mandatory: configure Azure:OpenAI:(Endpoint,ApiKey,DeploymentName) and Azure:FormRecognizer:(Endpoint,ApiKey) in appsettings or secrets.");
}

builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IDocumentAnalysisService, AzureDocumentAnalysisService>();
builder.Services.AddScoped<ICVProcessingService, CVProcessingService>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure Key Vault (optional)
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Configure Application Insights (optional)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply EF Core migrations automatically at startup (creates DB if missing in LocalDB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
        
        // Seed database
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply EF Core migrations at startup");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files for local file storage
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
