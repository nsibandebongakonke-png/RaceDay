using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[SessionAuthorize]
public class CategoriesController : ApiControllerBase
{
    public CategoriesController(AppDbContext db) : base(db) { }

    private static CategoryDto ToDto(Category c, int enrolmentCount = 0) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        Description = c.Description,
        CategoryType = c.CategoryType,
        EnrolmentCount = enrolmentCount
    };

    /// <summary>View the categories for an event.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await Db.Events.AnyAsync(e => e.EventId == eventId)) return Fail(404, "Event not found.");

        var categories = await Db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                Description = c.Description,
                CategoryType = c.CategoryType,
                EnrolmentCount = c.Enrolments.Count(x => x.Status != EnrolmentStatuses.Cancelled)
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Create a category for an event.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var (ev, error) = await GetOwnedEventAsync(eventId);
        if (error is not null) return error;

        var name = request.Name.Trim();
        if (await Db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return Fail(409, "This event already has a category with that name.");

        var category = new Category
        {
            EventId = ev!.EventId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CategoryType = request.CategoryType
        };

        Db.Categories.Add(category);
        await Db.SaveChangesAsync();
        return StatusCode(201, ToDto(category));
    }

    /// <summary>Update a category.</summary>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await Db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return Fail(404, "Category not found.");

        var (_, error) = await GetOwnedEventAsync(category.EventId);
        if (error is not null) return error;

        var name = request.Name.Trim();
        if (await Db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == name && c.CategoryId != id))
            return Fail(409, "This event already has a category with that name.");

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.CategoryType = request.CategoryType;
        await Db.SaveChangesAsync();

        return Ok(ToDto(category));
    }

    /// <summary>Delete a category (not allowed while participants are enrolled in it).</summary>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await Db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return Fail(404, "Category not found.");

        var (_, error) = await GetOwnedEventAsync(category.EventId);
        if (error is not null) return error;

        if (await Db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Fail(409, "This category has enrolments, so it cannot be deleted.");

        Db.Categories.Remove(category);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
