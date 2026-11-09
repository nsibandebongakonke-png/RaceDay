using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    public AuthController(AppDbContext db) : base(db) { }

    /// <summary>Register a new user and select a role (Organiser or Participant).</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Role != Roles.Organiser && request.Role != Roles.Participant)
            return Fail(400, "Role must be either Organiser or Participant.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email))
            return Fail(409, "An account with this email address already exists.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // hashed - never stored in plain text
            Role = new Role { RoleName = request.Role }
        };

        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        return StatusCode(201, user.ToDto());
    }

    /// <summary>Log in and start a session (the session stores the user's id and role).</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await Db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || user.Role is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Fail(401, "Invalid email or password.");

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role.RoleName);

        return Ok(user.ToDto());
    }

    /// <summary>End the current session.</summary>
    [HttpPost("logout")]
    [SessionAuthorize]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }

    /// <summary>Who is logged in on this session?</summary>
    [HttpGet("me")]
    [SessionAuthorize]
    public async Task<IActionResult> Me()
    {
        var user = await Db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        return user is null ? Fail(401, "Your session is no longer valid.") : Ok(user.ToDto());
    }
}
