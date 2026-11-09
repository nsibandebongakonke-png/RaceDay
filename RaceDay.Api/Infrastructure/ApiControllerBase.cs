using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Infrastructure;

public abstract class ApiControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;

    protected ApiControllerBase(AppDbContext db) => Db = db;

    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected IActionResult Fail(int statusCode, string message) => StatusCode(statusCode, new ApiError(message));

    /// <summary>Loads an event (tracked) and checks the logged-in organiser created it.</summary>
    protected async Task<(Event? Event, IActionResult? Error)> GetOwnedEventAsync(int eventId)
    {
        var ev = await Db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return (null, Fail(404, "Event not found."));
        if (ev.OrganiserId != CurrentUserId) return (null, Fail(403, "You can only manage events that you created."));
        return (ev, null);
    }
}
