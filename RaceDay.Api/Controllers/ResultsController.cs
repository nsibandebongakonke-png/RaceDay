using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[SessionAuthorize]
public class ResultsController : ApiControllerBase
{
    public ResultsController(AppDbContext db) : base(db) { }

    /// <summary>Organiser's results-entry grid for an event: every active enrolment plus its result (if captured).</summary>
    [HttpGet("api/events/{eventId:int}/results")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var (_, error) = await GetOwnedEventAsync(eventId);
        if (error is not null) return error;

        var rows = await Db.Enrolments.AsNoTracking()
            .Where(e => e.EventId == eventId && e.Status != EnrolmentStatuses.Cancelled)
            .OrderBy(e => e.Participant!.LastName).ThenBy(e => e.Participant!.FirstName)
            .Select(e => new
            {
                e.EnrolmentId,
                ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                CategoryName = e.Category!.Name,
                e.Status,
                ResultId = e.Result != null ? (int?)e.Result.RaceResultId : null,
                FinishTime = e.Result != null ? e.Result.FinishTime : null,
                Position = e.Result != null ? e.Result.Position : null,
                Published = e.Result != null && e.Result.Published
            })
            .ToListAsync();

        var dto = rows.Select(r => new ResultRowDto
        {
            EnrolmentId = r.EnrolmentId,
            ParticipantName = r.ParticipantName,
            CategoryName = r.CategoryName,
            EnrolmentStatus = r.Status,
            ResultId = r.ResultId,
            FinishTime = Mapping.FormatTime(r.FinishTime),
            Position = r.Position,
            Published = r.Published
        }).ToList();

        return Ok(dto);
    }

    /// <summary>Capture a finish time and position for an enrolment.</summary>
    [HttpPost("api/results")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Create([FromBody] ResultRequest request)
    {
        var enrolment = await Db.Enrolments.Include(e => e.Event).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment is null) return Fail(404, "Enrolment not found.");
        if (enrolment.Event!.OrganiserId != CurrentUserId) return Fail(403, "You can only capture results for events that you created.");
        if (enrolment.Result is not null) return Fail(409, "A result already exists for this enrolment. Update it instead.");

        var problem = Validate(enrolment, request, out var finishTime);
        if (problem is not null) return problem;

        var result = new RaceResult
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = finishTime,
            Position = request.Position,
            Published = request.Published
        };

        Db.RaceResults.Add(result);
        await Db.SaveChangesAsync();
        return StatusCode(201, ToRow(enrolment, result));
    }

    /// <summary>Update (or publish / unpublish) an existing result.</summary>
    [HttpPut("api/results/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Update(int id, [FromBody] ResultRequest request)
    {
        var result = await Db.RaceResults.Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.RaceResultId == id);
        if (result is null) return Fail(404, "Result not found.");

        var enrolment = result.Enrolment!;
        if (enrolment.Event!.OrganiserId != CurrentUserId) return Fail(403, "You can only capture results for events that you created.");

        var problem = Validate(enrolment, request, out var finishTime);
        if (problem is not null) return problem;

        result.FinishTime = finishTime;
        result.Position = request.Position;
        result.Published = request.Published;
        await Db.SaveChangesAsync();
        return Ok(ToRow(enrolment, result));
    }

    /// <summary>The logged-in participant's published results, with "position out of N finishers".</summary>
    [HttpGet("api/results/my")]
    [SessionAuthorize(Roles.Participant)]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;

        var rows = await Db.RaceResults.AsNoTracking()
            .Where(r => r.Published && r.Enrolment!.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment!.Event!.Date)
            .Select(r => new
            {
                r.Enrolment!.EventId,
                EventName = r.Enrolment.Event!.Name,
                EventDate = r.Enrolment.Event.Date,
                CategoryName = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.Position
            })
            .ToListAsync();

        var eventIds = rows.Select(r => r.EventId).Distinct().ToList();
        var totals = await Db.RaceResults.AsNoTracking()
            .Where(r => r.Published && r.FinishTime != null && eventIds.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var dto = rows.Select(r => new MyResultDto
        {
            EventId = r.EventId,
            EventName = r.EventName,
            EventDate = r.EventDate,
            CategoryName = r.CategoryName,
            FinishTime = Mapping.FormatTime(r.FinishTime),
            Position = r.Position,
            TotalFinishers = totals.TryGetValue(r.EventId, out var n) ? n : 0
        }).ToList();

        return Ok(dto);
    }

    private IActionResult? Validate(Enrolment enrolment, ResultRequest request, out TimeSpan? finishTime)
    {
        finishTime = null;

        if (enrolment.Event!.Date > DateTime.Now)
            return Fail(400, "Results can only be captured after the event has taken place.");
        if (enrolment.Status == EnrolmentStatuses.Cancelled)
            return Fail(400, "Results cannot be captured for a cancelled enrolment.");
        if (!FinishTimeParser.TryParse(request.FinishTime, out finishTime))
            return Fail(400, "Finish time must be in the format hh:mm:ss, for example 01:23:45.");
        if (request.Published && finishTime is null && request.Position is null)
            return Fail(400, "Enter a finish time or a position before publishing a result.");

        return null;
    }

    private static ResultRowDto ToRow(Enrolment enrolment, RaceResult result) => new()
    {
        EnrolmentId = enrolment.EnrolmentId,
        EnrolmentStatus = enrolment.Status,
        ResultId = result.RaceResultId,
        FinishTime = Mapping.FormatTime(result.FinishTime),
        Position = result.Position,
        Published = result.Published
    };
}
