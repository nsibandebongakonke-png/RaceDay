using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Infrastructure;

public static class Mapping
{
    public static UserDto ToDto(this User user) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        Role = user.Role?.RoleName ?? string.Empty,
        ProfilePicture = user.ProfilePic
    };

    public static string? FormatTime(TimeSpan? time) => time?.ToString(@"hh\:mm\:ss");
}
