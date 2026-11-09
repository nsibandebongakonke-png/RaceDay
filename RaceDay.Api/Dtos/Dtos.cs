using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// ---------- Shared ----------
/// <summary>Every error from the API has this shape so the MVC app can show a friendly message.</summary>
public record ApiError(string Message);

// ---------- Auth / profile ----------
public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8-100 characters with an upper-case letter, a lower-case letter and a number.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(20), Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Password is required.")] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required."), StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required."), StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20), Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? Phone { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    /// <summary>Blob name, e.g. profiles/abc.jpg. Fetch the bytes through GET /api/files/{blobName}.</summary>
    public string? ProfilePicture { get; set; }
}

// ---------- Events ----------
public class EventRequest
{
    [Required(ErrorMessage = "Event name is required."), StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Event date is required.")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Location is required."), StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Distance is required."), StringLength(50)]
    public string Distance { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event type is required."), RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;
}

public class EventFilter
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool UpcomingOnly { get; set; }
}

public class EventSummaryDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? BannerImage { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

public class EventDetailDto : EventSummaryDto
{
    public List<CategoryDto> Categories { get; set; } = new();
}

// ---------- Categories ----------
public class CategoryRequest
{
    [Required(ErrorMessage = "Category name is required."), StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Category type is required."), RegularExpression("^(Age|Distance)$", ErrorMessage = "Category type must be Age or Distance.")]
    public string CategoryType { get; set; } = "Age";
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public int EnrolmentCount { get; set; }
}

// ---------- Enrolments ----------
public class EnrolRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Please choose an event.")]
    public int EventId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>An enrolment as the participant sees it.</summary>
public class MyEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? BannerImage { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>An enrolment as the organiser sees it.</summary>
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ---------- Results ----------
public class ResultRequest
{
    /// <summary>Only used when creating (POST). Ignored on update.</summary>
    public int EnrolmentId { get; set; }

    /// <summary>hh:mm:ss, e.g. 01:23:45</summary>
    public string? FinishTime { get; set; }

    [Range(1, 1000000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }

    public bool Published { get; set; }
}

/// <summary>One row of the organiser's results-entry grid (an enrolment plus its result, if any).</summary>
public class ResultRowDto
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string EnrolmentStatus { get; set; } = string.Empty;
    public int? ResultId { get; set; }
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public bool Published { get; set; }
}

public class MyResultDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
}
