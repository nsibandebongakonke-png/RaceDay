using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    protected readonly IRaceDayApiClient Api;

    protected BaseController(IRaceDayApiClient api) => Api = api;

    /// <summary>
    /// If the API says the session has ended (401), clear the MVC session and send the user to the login page.
    /// Returns null when the result is not an expired-session failure.
    /// </summary>
    protected IActionResult? RedirectIfSessionExpired(ApiResult result)
    {
        if (!result.IsUnauthorized) return null;

        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }

    protected void FlashSuccess(string message) => TempData["Success"] = message;
    protected void FlashError(string message) => TempData["Error"] = message;
    protected void FlashWarning(string message) => TempData["Warning"] = message;

    /// <summary>Friendly pre-check of an uploaded image (the API validates again - never trust the client).</summary>
    protected static string? ValidateImage(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > 5 * 1024 * 1024) return "Images must be 5 MB or smaller.";

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant())
            ? null
            : "Only JPG, PNG, GIF or WEBP images are allowed.";
    }
}
