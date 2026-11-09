using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: create, edit and delete events (including the banner image).</summary>
[SessionAuthorize(AppRoles.Organiser)]
public class ManageEventsController : BaseController
{
    public ManageEventsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEventsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        var model = new ManageEventsViewModel();
        if (result.Success && result.Data is not null) model.Events = result.Data;
        else ViewBag.LoadError = result.Error;

        return View(model);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormViewModel { Date = DateTime.Today.AddDays(30).AddHours(7) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        var imageError = ValidateImage(model.Banner);
        if (imageError is not null) ModelState.AddModelError(nameof(model.Banner), imageError);
        if (!ModelState.IsValid) return View("Form", model);

        var created = await Api.CreateEventAsync(ToRequest(model));
        if (RedirectIfSessionExpired(created) is { } redirect) return redirect;
        if (!created.Success || created.Data is null)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await Api.UploadEventBannerAsync(created.Data.EventId, model.Banner);
            if (!banner.Success)
            {
                FlashWarning($"“{created.Data.Name}” was created, but the banner could not be uploaded: {banner.Error} You can add it by editing the event.");
                return RedirectToAction(nameof(Index));
            }
        }

        FlashSuccess($"“{created.Data.Name}” has been created. Next, add categories for it.");
        return RedirectToAction("Index", "Categories", new { eventId = created.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;
        if (!result.Success || result.Data is null)
        {
            FlashError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        var e = result.Data;
        return View("Form", new EventFormViewModel
        {
            EventId = e.EventId,
            Name = e.Name,
            Description = e.Description,
            Date = e.Date,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            ExistingBanner = e.BannerImage
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;

        var imageError = ValidateImage(model.Banner);
        if (imageError is not null) ModelState.AddModelError(nameof(model.Banner), imageError);
        if (!ModelState.IsValid) return View("Form", model);

        var updated = await Api.UpdateEventAsync(id, ToRequest(model));
        if (RedirectIfSessionExpired(updated) is { } redirect) return redirect;
        if (!updated.Success || updated.Data is null)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var banner = await Api.UploadEventBannerAsync(id, model.Banner);
            if (!banner.Success)
            {
                FlashWarning($"The event was updated, but the banner could not be uploaded: {banner.Error}");
                return RedirectToAction(nameof(Index));
            }
        }

        FlashSuccess($"“{updated.Data.Name}” has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success) FlashSuccess("The event was deleted.");
        else FlashError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    private static object ToRequest(EventFormViewModel m) => new
    {
        name = m.Name.Trim(),
        description = m.Description,
        date = m.Date!.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
        location = m.Location.Trim(),
        distance = m.Distance.Trim(),
        eventType = m.EventType
    };
}
