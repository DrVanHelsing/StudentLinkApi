using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace StudentLinkApi.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<Stream> DownloadFileAsync(string fileUrl);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _uploadPath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "cvs");
        _logger = logger;
        
        // Ensure directory exists
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            using (var outputStream = File.Create(filePath))
            {
                await fileStream.CopyToAsync(outputStream);
            }

            _logger.LogInformation("File uploaded successfully: {FileName}", uniqueFileName);
            return $"/uploads/cvs/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted successfully: {FileName}", fileName);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public Task<Stream> DownloadFileAsync(string fileUrl)
    {
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_uploadPath, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", fileName);
        }

        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }
}

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        var connectionString = configuration["Azure:BlobStorage:ConnectionString"];
        _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "cvs";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("File uploaded to Azure Blob Storage: {FileName}", uniqueFileName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobName = Path.GetFileName(uri.LocalPath);
            
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var result = await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("File deleted from Azure Blob Storage: {BlobName}", blobName);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Azure: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<Stream> DownloadFileAsync(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        var blobName = Path.GetFileName(uri.LocalPath);
        
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }
}