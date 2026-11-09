using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// The MVC app keeps the user's authenticated state and role in server-side session
/// (Part 3, requirement 2) so every page can check who is logged in.
/// </summary>
public static class SessionUserExtensions
{
    public static bool IsAuthenticated(this ISession session) => session.GetInt32(SessionKeys.UserId) is not null;

    public static string? GetRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static bool IsOrganiser(this ISession session) => session.GetRole() == AppRoles.Organiser;

    public static bool IsParticipant(this ISession session) => session.GetRole() == AppRoles.Participant;

    public static string GetFullName(this ISession session) => session.GetString(SessionKeys.FullName) ?? string.Empty;

    public static string? GetProfilePicture(this ISession session) => session.GetString(SessionKeys.ProfilePicture);

    public static void SignIn(this ISession session, UserDto user, string apiCookie)
    {
        session.Clear();
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.ApiCookie, apiCookie);
        session.SetUserDisplay(user);
    }

    public static void SetUserDisplay(this ISession session, UserDto user)
    {
        session.SetString(SessionKeys.FullName, $"{user.FirstName} {user.LastName}");
        if (string.IsNullOrEmpty(user.ProfilePicture)) session.Remove(SessionKeys.ProfilePicture);
        else session.SetString(SessionKeys.ProfilePicture, user.ProfilePicture);
    }

    public static string HomeControllerName(this ISession session) =>
        session.IsOrganiser() ? "Organiser" : "Events";

    public static string HomeActionName(this ISession session) =>
        session.IsOrganiser() ? "Dashboard" : "Index";
}
