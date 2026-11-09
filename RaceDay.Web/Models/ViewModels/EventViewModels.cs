using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

public static class EventOptions
{
    public static readonly string[] Types = { "Run", "Walk", "Cycle" };
    public static readonly string[] CategoryTypes = { "Age", "Distance" };
    public static readonly string[] Statuses = { "Pending", "Confirmed", "Cancelled", "Disqualified" };
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please give the event a name."), StringLength(100)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the date and start time.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date and start time")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance, e.g. 10 km."), StringLength(50)]
    public string Distance { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose an event type."), RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = string.Empty;

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBanner { get; set; }
}

public class ManageEventsViewModel
{
    public List<EventSummaryDto> Events { get; set; } = new();
}

public class OrganiserDashboardViewModel
{
    public List<EventSummaryDto> Events { get; set; } = new();
    public List<EventSummaryDto> Upcoming { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingCount => Upcoming.Count;
}

/// <summary>Used by Categories / Enrolments / Results when no event has been chosen yet.</summary>
public class EventPickerViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public List<EventSummaryDto> Events { get; set; } = new();
}

public class ParticipantEventsViewModel
{
    public EventFilterModel Filter { get; set; } = new();
    public List<EventSummaryDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = new();

    /// <summary>The participant's active enrolment for this event, if any.</summary>
    public MyEnrolmentDto? MyEnrolment { get; set; }
}

public class EnrolFormModel
{
    public int EventId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    public int CategoryId { get; set; }
}
