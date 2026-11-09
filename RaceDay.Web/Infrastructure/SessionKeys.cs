namespace RaceDay.Web.Infrastructure;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string ProfilePicture = "ProfilePicture";

    /// <summary>The API's own session cookie ("name=value"), forwarded on every API call for this user.</summary>
    public const string ApiCookie = "ApiCookie";
}
