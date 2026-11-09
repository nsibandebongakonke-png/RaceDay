using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant: personal race history (published results only).</summary>
[SessionAuthorize(AppRoles.Participant)]
public class MyResultsController : BaseController
{
    public MyResultsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyResultsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new());
    }
}
