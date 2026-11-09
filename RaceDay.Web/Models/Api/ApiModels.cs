namespace RaceDay.Web.Models.Api;

// These classes mirror the JSON the RaceDay API returns. They contain NO business logic -
// the MVC app only displays what the API gives it.

public class UserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
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

/// <summary>What a successful login gives the MVC app: the user, plus the API's session cookie to forward later.</summary>
public record LoginResult(UserDto User, string ApiCookie);

/// <summary>Filters for the participant's event list.</summary>
public class EventFilterModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool IncludePast { get; set; }
}

public class DownloadedFile
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}
