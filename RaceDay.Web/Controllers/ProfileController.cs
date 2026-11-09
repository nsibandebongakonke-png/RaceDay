using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Both roles: view and update personal information and upload a profile picture.</summary>
[SessionAuthorize]
public class ProfileController : BaseController
{
    public ProfileController(IRaceDayApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetProfileAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;
        if (!result.Success || result.Data is null)
        {
            FlashError(result.Error);
            return RedirectToAction(HttpContext.Session.HomeActionName(), HttpContext.Session.HomeControllerName());
        }

        HttpContext.Session.SetUserDisplay(result.Data); // keep the navbar name/avatar fresh
        return View(ToModel(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var result = await Api.UpdateProfileAsync(model.FirstName.Trim(), model.LastName.Trim(), model.Phone);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("Index", model);
        }

        HttpContext.Session.SetUserDisplay(result.Data);
        FlashSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            FlashError("Please choose a picture to upload.");
            return RedirectToAction(nameof(Index));
        }

        var imageError = ValidateImage(picture);
        if (imageError is not null)
        {
            FlashError(imageError);
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.UploadProfilePictureAsync(picture);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success && result.Data is not null)
        {
            HttpContext.Session.SetUserDisplay(result.Data);
            FlashSuccess("Your profile picture has been updated.");
        }
        else
        {
            FlashError(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToModel(UserDto user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Email = user.Email,
        Role = user.Role,
        ProfilePicture = user.ProfilePicture
    };
}
