using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(AppRoles.Organiser)]
public class OrganiserController : BaseController
{
    public OrganiserController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Summary of all events the organiser has created, enrolments per event and upcoming dates.</summary>
    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetMyEventsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        var model = new OrganiserDashboardViewModel();
        if (result.Success && result.Data is not null)
        {
            model.Events = result.Data;
            model.Upcoming = result.Data.Where(e => e.Date >= DateTime.Today).OrderBy(e => e.Date).Take(5).ToList();
        }
        else
        {
            ViewBag.LoadError = result.Error;
        }

        return View(model);
    }
}
