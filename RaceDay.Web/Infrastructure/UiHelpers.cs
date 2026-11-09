using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Infrastructure;

public static class UiHelpers
{
    /// <summary>CSS class for the colour-coded enrolment status badge (green = Confirmed, amber = Pending, ...).</summary>
    public static string StatusClass(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
    {
        "confirmed" => "status-confirmed",
        "pending" => "status-pending",
        "cancelled" => "status-cancelled",
        "disqualified" => "status-disqualified",
        _ => "status-pending"
    };

    /// <summary>URL of an image held in Azure Blob Storage, served through the MVC app (which asks the API for it).</summary>
    public static string? ImageUrl(this IUrlHelper url, string? blobName) =>
        string.IsNullOrWhiteSpace(blobName) ? null : url.Action("Get", "Files", new { path = blobName });

    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public static string Ordinal(int number)
    {
        var tens = number % 100;
        if (tens is >= 11 and <= 13) return number + "th";
        return (number % 10) switch { 1 => number + "st", 2 => number + "nd", 3 => number + "rd", _ => number + "th" };
    }
}
