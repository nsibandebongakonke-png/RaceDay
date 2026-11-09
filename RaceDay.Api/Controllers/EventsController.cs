using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[SessionAuthorize]
public class EventsController : ApiControllerBase
{
    private readonly IBlobStorageService _storage;

    public EventsController(AppDbContext db, IBlobStorageService storage) : base(db) => _storage = storage;

    private static readonly Expression<Func<Event, EventSummaryDto>> ToSummary = e => new EventSummaryDto
    {
        EventId = e.EventId,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        BannerImage = e.BannerImage,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName,
        CategoryCount = e.Categories.Count,
        EnrolmentCount = e.Enrolments.Count(x => x.Status != EnrolmentStatuses.Cancelled)
    };

    private static readonly Expression<Func<Event, EventDetailDto>> ToDetail = e => new EventDetailDto
    {
        EventId = e.EventId,
        Name = e.Name,
        Description = e.Description,
        Date = e.Date,
        Location = e.Location,
        Distance = e.Distance,
        EventType = e.EventType,
        BannerImage = e.BannerImage,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName,
        CategoryCount = e.Categories.Count,
        EnrolmentCount = e.Enrolments.Count(x => x.Status != EnrolmentStatuses.Cancelled),
        Categories = e.Categories.OrderBy(c => c.Name).Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            Description = c.Description,
            CategoryType = c.CategoryType,
            EnrolmentCount = c.Enrolments.Count(x => x.Status != EnrolmentStatuses.Cancelled)
        }).ToList()
    };

    private Task<EventDetailDto?> LoadDetailAsync(int id) =>
        Db.Events.AsNoTracking().Where(e => e.EventId == id).Select(ToDetail).FirstOrDefaultAsync();

    /// <summary>View all events (supports search / type / date filters).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EventFilter filter)
    {
        var query = Db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(e => e.Name.Contains(term) || e.Location.Contains(term) || (e.Description != null && e.Description.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            var type = filter.Type.Trim();
            query = query.Where(e => e.EventType == type);
        }

        if (filter.UpcomingOnly)
        {
            var today = DateTime.Today;
            query = query.Where(e => e.Date >= today);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(e => e.Date >= from);
        }

        if (filter.To.HasValue)
        {
            var toExclusive = filter.To.Value.Date.AddDays(1);
            query = query.Where(e => e.Date < toExclusive);
        }

        var events = await query.OrderBy(e => e.Date).Select(ToSummary).ToListAsync();
        return Ok(events);
    }

    /// <summary>The logged-in organiser's own events (with enrolment totals) - used by the dashboard.</summary>
    [HttpGet("mine")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var events = await Db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.Date)
            .Select(ToSummary)
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>View a single event with its categories.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var detail = await LoadDetailAsync(id);
        return detail is null ? Fail(404, "Event not found.") : Ok(detail);
    }

    /// <summary>Create an event.</summary>
    [HttpPost]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Create([FromBody] EventRequest request)
    {
        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Date = request.Date!.Value,
            Location = request.Location.Trim(),
            Distance = request.Distance.Trim(),
            EventType = request.EventType
        };

        Db.Events.Add(ev);
        await Db.SaveChangesAsync();

        return StatusCode(201, await LoadDetailAsync(ev.EventId));
    }

    /// <summary>Update an event (only the organiser who created it).</summary>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Update(int id, [FromBody] EventRequest request)
    {
        var (ev, error) = await GetOwnedEventAsync(id);
        if (error is not null) return error;

        ev!.Name = request.Name.Trim();
        ev.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        ev.Date = request.Date!.Value;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance.Trim();
        ev.EventType = request.EventType;
        await Db.SaveChangesAsync();

        return Ok(await LoadDetailAsync(id));
    }

    /// <summary>Delete an event (and its categories, enrolments and results).</summary>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (ev, error) = await GetOwnedEventAsync(id);
        if (error is not null) return error;

        var banner = ev!.BannerImage;

        // Enrolments first (their results cascade), because Category -> Enrolment is restricted.
        await Db.Enrolments.Where(e => e.EventId == id).ExecuteDeleteAsync(ct);
        Db.Events.Remove(ev);
        await Db.SaveChangesAsync(ct);

        await _storage.DeleteAsync(banner, ct);
        return NoContent();
    }

    /// <summary>Upload / replace the event banner image (stored in Azure Blob Storage).</summary>
    [HttpPost("{id:int}/banner")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var (ev, error) = await GetOwnedEventAsync(id);
        if (error is not null) return error;

        var newBlob = await _storage.UploadImageAsync(file, "banners", ct);
        var oldBlob = ev!.BannerImage;

        ev.BannerImage = newBlob;
        await Db.SaveChangesAsync(ct);

        await _storage.DeleteAsync(oldBlob, ct);
        return Ok(await LoadDetailAsync(id));
    }
}
