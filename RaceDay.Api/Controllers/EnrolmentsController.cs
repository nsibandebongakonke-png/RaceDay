using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[SessionAuthorize]
public class EnrolmentsController : ApiControllerBase
{
    public EnrolmentsController(AppDbContext db) : base(db) { }

    /// <summary>Enrol in an event with a selected category (records Participant + Event + Category).</summary>
    [HttpPost("api/enrolments")]
    [SessionAuthorize(Roles.Participant)]
    public async Task<IActionResult> Enrol([FromBody] EnrolRequest request)
    {
        var ev = await Db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == request.EventId);
        if (ev is null) return Fail(404, "Event not found.");

        if (ev.Date < DateTime.Now) return Fail(400, "This event has already taken place, so enrolment is closed.");

        var category = await Db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);
        if (category is null) return Fail(400, "Please choose a valid category for this event.");

        var userId = CurrentUserId;
        var alreadyEnrolled = await Db.Enrolments.AnyAsync(e =>
            e.EventId == request.EventId && e.ParticipantId == userId && e.Status != EnrolmentStatuses.Cancelled);
        if (alreadyEnrolled) return Fail(409, "You are already enrolled for this event.");

        var enrolment = new Enrolment
        {
            ParticipantId = userId,
            EventId = ev.EventId,
            CategoryId = category.CategoryId,
            EnrolDate = DateTime.UtcNow,
            Status = EnrolmentStatuses.Pending
        };

        Db.Enrolments.Add(enrolment);
        await Db.SaveChangesAsync();

        return StatusCode(201, new MyEnrolmentDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            EventId = ev.EventId,
            EventName = ev.Name,
            EventDate = ev.Date,
            Location = ev.Location,
            BannerImage = ev.BannerImage,
            CategoryId = category.CategoryId,
            CategoryName = category.Name,
            EnrolDate = enrolment.EnrolDate,
            Status = enrolment.Status
        });
    }

    /// <summary>The logged-in participant's own enrolments (newest event first).</summary>
    [HttpGet("api/enrolments/my")]
    [SessionAuthorize(Roles.Participant)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var list = await Db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.Event!.Date)
            .Select(e => new MyEnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                EventId = e.EventId,
                EventName = e.Event!.Name,
                EventDate = e.Event.Date,
                Location = e.Event.Location,
                BannerImage = e.Event.BannerImage,
                CategoryId = e.CategoryId,
                CategoryName = e.Category!.Name,
                EnrolDate = e.EnrolDate,
                Status = e.Status
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Cancel the logged-in participant's own enrolment.</summary>
    [HttpPut("api/enrolments/{id:int}/cancel")]
    [SessionAuthorize(Roles.Participant)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await Db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null || enrolment.ParticipantId != CurrentUserId) return Fail(404, "Enrolment not found.");

        if (enrolment.Status == EnrolmentStatuses.Cancelled) return Fail(409, "This enrolment is already cancelled.");
        if (enrolment.Status == EnrolmentStatuses.Disqualified) return Fail(400, "A disqualified enrolment cannot be cancelled.");
        if (enrolment.Event!.Date < DateTime.Now) return Fail(400, "You cannot cancel an enrolment for an event that has already taken place.");

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await Db.SaveChangesAsync();
        return Ok(new { message = "Enrolment cancelled." });
    }

    /// <summary>View all enrolments for an event (organiser who owns the event only).</summary>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var (_, error) = await GetOwnedEventAsync(eventId);
        if (error is not null) return error;

        var list = await Db.Enrolments.AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolDate)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                Email = e.Participant.Email,
                Phone = e.Participant.Phone,
                CategoryId = e.CategoryId,
                CategoryName = e.Category!.Name,
                EnrolDate = e.EnrolDate,
                Status = e.Status
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>Change an enrolment's status, e.g. Pending -> Confirmed (organiser who owns the event only).</summary>
    [HttpPut("api/enrolments/{id:int}/status")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var status = EnrolmentStatuses.All.FirstOrDefault(s => s.Equals(request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null) return Fail(400, "Status must be Pending, Confirmed, Cancelled or Disqualified.");

        var enrolment = await Db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return Fail(404, "Enrolment not found.");
        if (enrolment.Event!.OrganiserId != CurrentUserId) return Fail(403, "You can only manage enrolments for events that you created.");

        enrolment.Status = status;
        await Db.SaveChangesAsync();
        return Ok(new { enrolmentId = enrolment.EnrolmentId, status = enrolment.Status });
    }
}
