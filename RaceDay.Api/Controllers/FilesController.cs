using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Streams images out of the (private) Azure Blob container. The MVC app calls this endpoint
/// so that it never has to talk to Azure itself.
/// </summary>
[ApiController]
[SessionAuthorize]
public class FilesController : ApiControllerBase
{
    private readonly IBlobStorageService _storage;

    public FilesController(AppDbContext db, IBlobStorageService storage) : base(db) => _storage = storage;

    [HttpGet("api/files/{**blobName}")]
    public async Task<IActionResult> Get(string blobName, CancellationToken ct)
    {
        var allowed = (blobName.StartsWith("banners/") || blobName.StartsWith("profiles/"))
                      && !blobName.Contains("..") && !blobName.Contains('\\') && blobName.Length <= 200;
        if (!allowed) return Fail(404, "File not found.");

        var file = await _storage.DownloadAsync(blobName, ct);
        if (file is null) return Fail(404, "File not found.");

        Response.Headers.CacheControl = "private, max-age=3600";
        return File(file.Content, file.ContentType);
    }
}
