using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(IRaceDayApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.LoginAsync(model.Email.Trim(), model.Password);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            model.Password = string.Empty;
            return View(model);
        }

        // Authenticated state + role are held in session for every later page.
        HttpContext.Session.SignIn(result.Data.User, result.Data.ApiCookie);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToHome();
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role != AppRoles.Organiser && model.Role != AppRoles.Participant)
            ModelState.AddModelError(nameof(model.Role), "Please choose Organiser or Participant.");

        if (!ModelState.IsValid) return View(Clear(model));

        var result = await Api.RegisterAsync(new
        {
            firstName = model.FirstName.Trim(),
            lastName = model.LastName.Trim(),
            email = model.Email.Trim(),
            phone = model.Phone,
            password = model.Password,
            role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(Clear(model));
        }

        FlashSuccess("Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Ends the session (in the API and in this app) and redirects to the login page.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Session.IsAuthenticated())
            await Api.LogoutAsync(); // best effort - we clear our own session regardless

        HttpContext.Session.Clear();
        FlashSuccess("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToHome() =>
        RedirectToAction(HttpContext.Session.HomeActionName(), HttpContext.Session.HomeControllerName());

    private static RegisterViewModel Clear(RegisterViewModel model)
    {
        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
        return model;
    }
}
