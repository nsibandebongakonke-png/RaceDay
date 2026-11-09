using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Infrastructure;

/// <summary>
/// Session-based authorisation. The login endpoint stores the user's id and role in the server-side
/// session; this filter rejects requests with no session (401) or the wrong role (403).
/// Usage: [SessionAuthorize] = any logged-in user, [SessionAuthorize("Organiser")] = Organisers only.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) is null)
        {
            context.Result = new ObjectResult(new ApiError("You must be logged in to do that.")) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        var role = session.GetString(SessionKeys.Role);
        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(new ApiError("Your account does not have permission to do that.")) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
