using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: see everyone enrolled for an event, with their category and enrolment status.</summary>
[SessionAuthorize(AppRoles.Organiser)]
public class EnrolmentsController : BaseController
{
    public EnrolmentsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId is null) return await PickEventAsync();

        var ev = await Api.GetEventAsync(eventId.Value);
        if (RedirectIfSessionExpired(ev) is { } redirect) return redirect;
        if (!ev.Success || ev.Data is null)
        {
            FlashError(ev.Error);
            return RedirectToAction(nameof(Index), new { eventId = (int?)null });
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId.Value);
        if (RedirectIfSessionExpired(enrolments) is { } redirect2) return redirect2;
        if (!enrolments.Success) ViewBag.LoadError = enrolments.Error;

        return View(new EnrolmentsPageViewModel { Event = ev.Data, Enrolments = enrolments.Data ?? new() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int eventId, int enrolmentId, string status)
    {
        if (!EventOptions.Statuses.Contains(status))
        {
            FlashError("Please choose a valid status.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var result = await Api.UpdateEnrolmentStatusAsync(enrolmentId, status);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success) FlashSuccess($"Enrolment marked as {status}.");
        else FlashError(result.Error);

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<IActionResult> PickEventAsync()
    {
        var result = await Api.GetMyEventsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        return View("~/Views/Shared/EventPicker.cshtml", new EventPickerViewModel
        {
            Title = "Enrolments",
            Subtitle = "Choose an event to see who has enrolled.",
            Controller = "Enrolments",
            Events = result.Data ?? new()
        });
    }
}
