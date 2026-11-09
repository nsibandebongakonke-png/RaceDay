using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: add age or distance categories to each event.</summary>
[SessionAuthorize(AppRoles.Organiser)]
public class CategoriesController : BaseController
{
    public CategoriesController(IRaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index(int? eventId)
    {
        if (eventId is null) return await PickEventAsync();

        var result = await Api.GetEventAsync(eventId.Value);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;
        if (!result.Success || result.Data is null)
        {
            FlashError(result.Error);
            return RedirectToAction(nameof(Index), new { eventId = (int?)null });
        }

        return View(new CategoriesPageViewModel
        {
            Event = result.Data,
            NewCategory = new CategoryFormViewModel { EventId = eventId.Value }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, [Bind(Prefix = "NewCategory")] CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            // Re-show the page with the validation messages next to the form.
            var page = await Api.GetEventAsync(eventId);
            if (RedirectIfSessionExpired(page) is { } expired) return expired;
            if (!page.Success || page.Data is null)
            {
                FlashError(page.Error);
                return RedirectToAction(nameof(Index), new { eventId = (int?)null });
            }

            form.EventId = eventId;
            return View("Index", new CategoriesPageViewModel { Event = page.Data, NewCategory = form });
        }

        var result = await Api.CreateCategoryAsync(eventId, ToRequest(form));
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success) FlashSuccess($"Category “{form.Name.Trim()}” was added.");
        else FlashError(result.Error);

        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var result = await Api.GetCategoriesAsync(eventId);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        var category = result.Data?.FirstOrDefault(c => c.CategoryId == id);
        if (!result.Success || category is null)
        {
            FlashError(result.Success ? "That category could not be found." : result.Error);
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ViewBag.EventId = eventId;
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            Name = category.Name,
            Description = category.Description,
            CategoryType = category.CategoryType
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;
        model.EventId = eventId;

        if (!ModelState.IsValid)
        {
            ViewBag.EventId = eventId;
            return View(model);
        }

        var result = await Api.UpdateCategoryAsync(id, ToRequest(model));
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            ViewBag.EventId = eventId;
            return View(model);
        }

        FlashSuccess("Category updated.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await Api.DeleteCategoryAsync(id);
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        if (result.Success) FlashSuccess("Category deleted.");
        else FlashError(result.Error);

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<IActionResult> PickEventAsync()
    {
        var result = await Api.GetMyEventsAsync();
        if (RedirectIfSessionExpired(result) is { } redirect) return redirect;

        return View("~/Views/Shared/EventPicker.cshtml", new EventPickerViewModel
        {
            Title = "Categories",
            Subtitle = "Choose an event to manage its age and distance categories.",
            Controller = "Categories",
            Events = result.Data ?? new()
        });
    }

    private static object ToRequest(CategoryFormViewModel m) => new
    {
        name = m.Name.Trim(),
        description = m.Description,
        categoryType = m.CategoryType
    };
}
