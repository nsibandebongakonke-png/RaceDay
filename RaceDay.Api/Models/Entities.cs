using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

/// <summary>The two roles a RaceDay user can have (strict 1:1 with a user, as in the Part 1 ERD).</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Disqualified = "Disqualified";

    public static readonly string[] All = { Pending, Confirmed, Cancelled, Disqualified };
}

public class User
{
    public int UserId { get; set; }

    [Required, MaxLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string LastName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash. The original password is never stored.</summary>
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)] public string? Phone { get; set; }

    /// <summary>Blob name of the profile picture in Azure Blob Storage (e.g. profiles/abc.jpg).</summary>
    [MaxLength(500)] public string? ProfilePic { get; set; }

    public Role? Role { get; set; }
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Role
{
    public int UserId { get; set; }
    [Required, MaxLength(50)] public string RoleName { get; set; } = string.Empty;
    public User? User { get; set; }
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }

    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    public DateTime Date { get; set; }
    [Required, MaxLength(255)] public string Location { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Distance { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string EventType { get; set; } = string.Empty;

    /// <summary>Blob name of the banner image in Azure Blob Storage (e.g. banners/abc.jpg).</summary>
    [MaxLength(500)] public string? BannerImage { get; set; }

    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }

    /// <summary>"Age" or "Distance".</summary>
    [Required, MaxLength(20)] public string CategoryType { get; set; } = "Age";

    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = EnrolmentStatuses.Pending;

    public User? Participant { get; set; }
    public Event? Event { get; set; }
    public Category? Category { get; set; }
    public RaceResult? Result { get; set; }
}

public class RaceResult
{
    public int RaceResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }

    /// <summary>false = draft (only the organiser sees it), true = published to the participant.</summary>
    public bool Published { get; set; }

    public Enrolment? Enrolment { get; set; }
}
