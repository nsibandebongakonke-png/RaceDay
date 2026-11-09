using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant home (browse + filter events), event details and enrolment.</summary>
[SessionAuthorize]
public class EventsController : BaseController
{
    public EventsController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Home page for participants: all upcoming events with filtering options.</summary>
    [SessionAuthorize(AppRoles.Participant)]
    public async Task<IActionResult> Index([FromQuery] EventFilterModel filter)
    {
        var result = await Api.GetEventsAsync(filter);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        var model = new ParticipantEventsViewModel { Filter = filter };
        if (result.Success && result.Data is not null) model.Events = result.Data;
        else ViewBag.LoadError = result.Error;

        return View(model);
    }

    /// <summary>Full event information and available categories (organisers can preview it too).</summary>
    public async Task<IActionResult> Details(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;
        if (!result.Success || result.Data is null)
        {
            FlashError(result.Error);
            return RedirectToAction(HttpContext.Session.HomeActionName(), HttpContext.Session.HomeControllerName());
        }

        var model = new EventDetailsViewModel { Event = result.Data };

        if (HttpContext.Session.IsParticipant())
        {
            // Is this participant already enrolled? (so we can show their status instead of the enrol form)
            var mine = await Api.GetMyEnrolmentsAsync();
            if (RedirectIfSessionExpired(mine) is { } expired) return expired;
            model.MyEnrolment = mine.Data?.FirstOrDefault(e => e.EventId == id && e.Status != "Cancelled");
        }

        return View(model);
    }

    /// <summary>Enrol the logged-in participant in an event with the chosen category.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [SessionAuthorize(AppRoles.Participant)]
    public async Task<IActionResult> Enrol(EnrolFormModel form)
    {
        if (!ModelState.IsValid)
        {
            FlashError("Please choose a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id = form.EventId });
        }

        var result = await Api.EnrolAsync(form.EventId, form.CategoryId);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success)
        {
            FlashSuccess($"You are enrolled for {result.Data?.EventName}. Your enrolment is pending until the organiser confirms it.");
            return RedirectToAction("Index", "MyEnrolments");
        }

        FlashError(result.Error);
        return RedirectToAction(nameof(Details), new { id = form.EventId });
    }
}
