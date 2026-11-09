using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(IRaceDayApiClient api) : base(api) { }

    /// <summary>Public landing page. Logged-in users go straight to their own home.</summary>
    public IActionResult Index()
    {
        if (HttpContext.Session.IsAuthenticated())
            return RedirectToAction(HttpContext.Session.HomeActionName(), HttpContext.Session.HomeControllerName());

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>Friendly pages for 404 etc. (wired up with UseStatusCodePagesWithReExecute).</summary>
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code;
        return View(code == 404 ? "NotFound" : "Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
