using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Serves banner and profile images. The MVC app never talks to Azure Blob Storage directly:
/// it asks the API for the file and passes the bytes on to the browser.
/// </summary>
[SessionAuthorize]
public class FilesController : BaseController
{
    public FilesController(IRaceDayApiClient api) : base(api) { }

    [HttpGet("files/{**path}")]
    public async Task<IActionResult> Get(string path)
    {
        var result = await Api.GetFileAsync(path);
        if (!result.Success || result.Data is null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=3600";
        return File(result.Data.Content, result.Data.ContentType);
    }
}
