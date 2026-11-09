using System.Globalization;
using System.Text.RegularExpressions;

namespace RaceDay.Api.Infrastructure;

public static class FinishTimeParser
{
    // h:mm:ss or hh:mm:ss, hours 0-23
    private static readonly Regex Pattern = new(@"^([01]?\d|2[0-3]):[0-5]\d:[0-5]\d$", RegexOptions.Compiled);

    public static bool TryParse(string? text, out TimeSpan? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true; // blank = "no time captured"

        var trimmed = text.Trim();
        if (!Pattern.IsMatch(trimmed)) return false;

        value = TimeSpan.Parse(trimmed, CultureInfo.InvariantCulture);
        return true;
    }
}
