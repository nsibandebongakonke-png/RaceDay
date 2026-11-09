using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

public class AzureBlobOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "raceday-images";
}

public class InvalidImageException : Exception
{
    public InvalidImageException(string message) : base(message) { }
}

public class StorageUnavailableException : Exception
{
    public StorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

public record BlobFile(Stream Content, string ContentType);

/// <summary>
/// All file handling in RaceDay goes through this service (Part 3, requirement 5).
/// The MVC application never talks to Azure directly - it calls the API, which calls this.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>Validates and uploads an image; returns the blob name (e.g. banners/3f2a....jpg).</summary>
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);

    Task DeleteAsync(string? blobName, CancellationToken ct = default);

    /// <summary>Returns null when the blob does not exist.</summary>
    Task<BlobFile?> DownloadAsync(string blobName, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    private readonly AzureBlobOptions _options;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BlobContainerClient? _container;

    public BlobStorageService(IOptions<AzureBlobOptions> options, ILogger<BlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new InvalidImageException("Please choose an image file to upload.");
        if (file.Length > MaxBytes)
            throw new InvalidImageException("Images must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
            throw new InvalidImageException("Only JPG, PNG, GIF or WEBP images are allowed.");

        await using var stream = file.OpenReadStream();

        // Don't trust the file name or client content-type: check the real file signature too.
        if (!await HasImageSignatureAsync(stream, extension.ToLowerInvariant(), ct))
            throw new InvalidImageException("The selected file is not a valid image.");
        stream.Seek(0, SeekOrigin.Begin);

        var blobName = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        try
        {
            var container = await GetContainerAsync(ct);
            var blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, ct);
            return blobName;
        }
        catch (Exception ex) when (ex is not StorageUnavailableException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Blob upload failed for {BlobName}", blobName);
            throw new StorageUnavailableException("File storage is temporarily unavailable. Please try again shortly.", ex);
        }
    }

    public async Task DeleteAsync(string? blobName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;

        try
        {
            var container = await GetContainerAsync(ct);
            await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best effort: an orphaned blob must never break the user's request.
            _logger.LogWarning(ex, "Could not delete blob {BlobName}", blobName);
        }
    }

    public async Task<BlobFile?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            var container = await GetContainerAsync(ct);
            var response = await container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: ct);
            return new BlobFile(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex) when (ex is not StorageUnavailableException and not OperationCanceledException)
        {
            _logger.LogError(ex, "Blob download failed for {BlobName}", blobName);
            throw new StorageUnavailableException("File storage is temporarily unavailable. Please try again shortly.", ex);
        }
    }

    /// <summary>Creates the (private) container on first use.</summary>
    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken ct)
    {
        if (_container is not null) return _container;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new StorageUnavailableException("Azure Blob Storage is not configured on the server.");

        await _lock.WaitAsync(ct);
        try
        {
            if (_container is null)
            {
                var clientOptions = new BlobClientOptions();
                clientOptions.Retry.MaxRetries = 2;
                clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);

                var service = new BlobServiceClient(_options.ConnectionString, clientOptions);
                var container = service.GetBlobContainerClient(_options.ContainerName);
                await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
                _container = container;
            }
            return _container;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<bool> HasImageSignatureAsync(Stream stream, string extension, CancellationToken ct)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < 4) return false;

        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".gif" => header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8',
            ".webp" => read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                       && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P',
            _ => false
        };
    }
}
