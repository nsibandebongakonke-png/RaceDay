using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant: personal view of every event they enrolled for and the enrolment status.</summary>
[SessionAuthorize(AppRoles.Participant)]
public class MyEnrolmentsController : BaseController
{
    public MyEnrolmentsController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyEnrolmentsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (!result.Success) ViewBag.LoadError = result.Error;
        return View(result.Data ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await Api.CancelEnrolmentAsync(id);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success) FlashSuccess("Your enrolment has been cancelled.");
        else FlashError(result.Error);

        return RedirectToAction(nameof(Index));
    }
}
