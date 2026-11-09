using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects MVC pages using the session. Unauthenticated users are redirected to the login page;
/// users with the wrong role are sent to "Access denied".
/// [SessionAuthorize] = any logged-in user, [SessionAuthorize(AppRoles.Organiser)] = Organisers only.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles) => _roles = roles;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var request = context.HttpContext.Request;

        if (!session.IsAuthenticated())
        {
            string? returnUrl = HttpMethods.IsGet(request.Method) ? request.Path.Value + request.QueryString.Value : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(session.GetRole()))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
