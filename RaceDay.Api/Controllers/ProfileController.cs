using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[SessionAuthorize]
public class ProfileController : ApiControllerBase
{
    private readonly IBlobStorageService _storage;

    public ProfileController(AppDbContext db, IBlobStorageService storage) : base(db) => _storage = storage;

    /// <summary>View own profile.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await Db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        return user is null ? Fail(401, "Your session is no longer valid.") : Ok(user.ToDto());
    }

    /// <summary>Update own profile.</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        var user = await Db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return Fail(401, "Your session is no longer valid.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        await Db.SaveChangesAsync();

        return Ok(user.ToDto());
    }

    /// <summary>Upload / replace the profile picture (stored in Azure Blob Storage).</summary>
    [HttpPost("picture")]
    public async Task<IActionResult> UploadPicture(IFormFile file, CancellationToken ct)
    {
        var user = await Db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId, ct);
        if (user is null) return Fail(401, "Your session is no longer valid.");

        var newBlob = await _storage.UploadImageAsync(file, "profiles", ct);
        var oldBlob = user.ProfilePic;

        user.ProfilePic = newBlob;
        await Db.SaveChangesAsync(ct);

        await _storage.DeleteAsync(oldBlob, ct); // tidy up the old picture
        return Ok(user.ToDto());
    }
}
