using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: capture and publish finish times and finishing positions after an event.</summary>
[SessionAuthorize(AppRoles.Organiser)]
public class ResultsController : BaseController
{
    public ResultsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId is null) return await PickEventAsync();

        var model = await BuildModelAsync(eventId.Value);
        if (model.Redirect is not null) return model.Redirect;
        return View(model.Page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ResultsEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            FlashError("Some results are not valid - please check the highlighted fields.");
            return View("Index", model);
        }

        var saved = 0;
        var problems = new List<string>();

        foreach (var row in model.Rows)
        {
            var hasData = !string.IsNullOrWhiteSpace(row.FinishTime) || row.Position is not null;
            if (!hasData && row.ResultId is null) continue; // nothing entered for this participant yet

            var time = string.IsNullOrWhiteSpace(row.FinishTime) ? null : row.FinishTime.Trim();

            var result = row.ResultId is null
                ? await Api.CreateResultAsync(row.EnrolmentId, time, row.Position, row.Published)
                : await Api.UpdateResultAsync(row.ResultId.Value, time, row.Position, row.Published);

            if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

            if (result.Success) saved++;
            else problems.Add($"{row.ParticipantName}: {result.Error}");
        }

        if (problems.Count > 0)
            FlashError(string.Join(" ", problems));
        else if (saved > 0)
            FlashSuccess($"{saved} result{(saved == 1 ? "" : "s")} saved.");
        else
            FlashWarning("There was nothing to save - enter a finish time or position first.");

        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    private async Task<(ResultsEntryViewModel? Page, IActionResult? Redirect)> BuildModelAsync(int eventId)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (RedirectIfSessionExpired(ev) is { } redirect) return (null, redirect);
        if (!ev.Success || ev.Data is null)
        {
            FlashError(ev.Error);
            return (null, RedirectToAction(nameof(Index), new { eventId = (int?)null }));
        }

        var rows = await Api.GetEventResultsAsync(eventId);
        if (RedirectIfSessionExpired(rows) is { } redirect2) return (null, redirect2);
        if (!rows.Success) ViewBag.LoadError = rows.Error;

        var page = new ResultsEntryViewModel
        {
            EventId = ev.Data.EventId,
            EventName = ev.Data.Name,
            EventDate = ev.Data.Date,
            Rows = (rows.Data ?? new()).Select(r => new ResultRowInput
            {
                EnrolmentId = r.EnrolmentId,
                ResultId = r.ResultId,
                ParticipantName = r.ParticipantName,
                CategoryName = r.CategoryName,
                EnrolmentStatus = r.EnrolmentStatus,
                FinishTime = r.FinishTime,
                Position = r.Position,
                Published = r.Published
            }).ToList()
        };

        return (page, null);
    }

    private async Task<IActionResult> PickEventAsync()
    {
        var result = await Api.GetMyEventsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        // Results are captured after an event, so show the events that have already started first.
        var events = (result.Data ?? new()).OrderByDescending(e => e.Date).ToList();

        return View("~/Views/Shared/EventPicker.cshtml", new EventPickerViewModel
        {
            Title = "Results",
            Subtitle = "Choose an event to capture and publish finish times and positions.",
            Controller = "Results",
            Events = events
        });
    }
}
