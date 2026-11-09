using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Demo data so the application can be demonstrated straight away (switch off with Seed:DemoData = false).
///   Organiser:    organiser@raceday.co.za  /  Organiser@123
///   Participants: runner@raceday.co.za, sipho@raceday.co.za, ayesha@raceday.co.za  /  Participant@123
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        static User NewUser(string first, string last, string email, string password, string role, string phone) => new()
        {
            FirstName = first,
            LastName = last,
            Email = email,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = new Role { RoleName = role }
        };

        var organiser = NewUser("Thabo", "Mokoena", "organiser@raceday.co.za", "Organiser@123", Roles.Organiser, "0821234567");
        var naledi = NewUser("Naledi", "Dlamini", "runner@raceday.co.za", "Participant@123", Roles.Participant, "0839876543");
        var sipho = NewUser("Sipho", "Khumalo", "sipho@raceday.co.za", "Participant@123", Roles.Participant, "0845551234");
        var ayesha = NewUser("Ayesha", "Naidoo", "ayesha@raceday.co.za", "Participant@123", Roles.Participant, "0817778899");

        db.Users.AddRange(organiser, naledi, sipho, ayesha);
        await db.SaveChangesAsync();

        var today = DateTime.Today;

        var pastEvent = new Event
        {
            OrganiserId = organiser.UserId,
            Name = "Durban Beachfront 10K",
            Description = "A fast, flat out-and-back along the Golden Mile. Sea breeze included.",
            Date = today.AddDays(-30).AddHours(6),
            Location = "Durban Beachfront, KwaZulu-Natal",
            Distance = "10 km",
            EventType = "Run",
            Categories =
            {
                new Category { Name = "10km Open", CategoryType = "Distance", Description = "The full 10 km race." },
                new Category { Name = "5km Fun Run", CategoryType = "Distance", Description = "Half the course, all the fun." },
                new Category { Name = "Under 20", CategoryType = "Age", Description = "Runners aged 19 and younger." }
            }
        };

        var sunriseRun = new Event
        {
            OrganiserId = organiser.UserId,
            Name = "Umhlanga Sunrise Half Marathon",
            Description = "Chase the sunrise along the Umhlanga promenade and lighthouse. Chip timing and finisher medals.",
            Date = today.AddDays(21).AddHours(5).AddMinutes(30),
            Location = "Umhlanga Promenade, Durban",
            Distance = "21.1 km",
            EventType = "Run",
            Categories =
            {
                new Category { Name = "21km Half Marathon", CategoryType = "Distance" },
                new Category { Name = "10km", CategoryType = "Distance" },
                new Category { Name = "Under 20", CategoryType = "Age" },
                new Category { Name = "Masters 40+", CategoryType = "Age" }
            }
        };

        var trailWalk = new Event
        {
            OrganiserId = organiser.UserId,
            Name = "Drakensberg Trail Walk",
            Description = "A scenic guided walk through the foothills. Suitable for all fitness levels.",
            Date = today.AddDays(45).AddHours(8),
            Location = "Champagne Valley, Drakensberg",
            Distance = "15 km",
            EventType = "Walk",
            Categories =
            {
                new Category { Name = "15km Walk", CategoryType = "Distance" },
                new Category { Name = "Family 5km", CategoryType = "Distance" }
            }
        };

        var cycleClassic = new Event
        {
            OrganiserId = organiser.UserId,
            Name = "Midlands Meander Cycle Classic",
            Description = "Rolling hills, farm stalls and fast descents through the KZN Midlands.",
            Date = today.AddDays(70).AddHours(7),
            Location = "Howick, KwaZulu-Natal",
            Distance = "65 km",
            EventType = "Cycle",
            Categories =
            {
                new Category { Name = "65km Race", CategoryType = "Distance" },
                new Category { Name = "35km Ride", CategoryType = "Distance" },
                new Category { Name = "Under 20", CategoryType = "Age" }
            }
        };

        db.Events.AddRange(pastEvent, sunriseRun, trailWalk, cycleClassic);
        await db.SaveChangesAsync();

        Category Cat(Event ev, string name) => ev.Categories.First(c => c.Name == name);

        // Past event: three confirmed enrolments with published results
        var e1 = new Enrolment { ParticipantId = naledi.UserId, EventId = pastEvent.EventId, CategoryId = Cat(pastEvent, "10km Open").CategoryId, EnrolDate = today.AddDays(-60), Status = EnrolmentStatuses.Confirmed };
        var e2 = new Enrolment { ParticipantId = sipho.UserId, EventId = pastEvent.EventId, CategoryId = Cat(pastEvent, "10km Open").CategoryId, EnrolDate = today.AddDays(-58), Status = EnrolmentStatuses.Confirmed };
        var e3 = new Enrolment { ParticipantId = ayesha.UserId, EventId = pastEvent.EventId, CategoryId = Cat(pastEvent, "5km Fun Run").CategoryId, EnrolDate = today.AddDays(-55), Status = EnrolmentStatuses.Confirmed };

        // Upcoming event: one confirmed, one pending (shows the colour-coded statuses)
        var e4 = new Enrolment { ParticipantId = naledi.UserId, EventId = sunriseRun.EventId, CategoryId = Cat(sunriseRun, "21km Half Marathon").CategoryId, EnrolDate = today.AddDays(-3), Status = EnrolmentStatuses.Confirmed };
        var e5 = new Enrolment { ParticipantId = sipho.UserId, EventId = sunriseRun.EventId, CategoryId = Cat(sunriseRun, "10km").CategoryId, EnrolDate = today.AddDays(-1), Status = EnrolmentStatuses.Pending };

        db.Enrolments.AddRange(e1, e2, e3, e4, e5);
        await db.SaveChangesAsync();

        db.RaceResults.AddRange(
            new RaceResult { EnrolmentId = e1.EnrolmentId, FinishTime = new TimeSpan(0, 44, 18), Position = 1, Published = true },
            new RaceResult { EnrolmentId = e2.EnrolmentId, FinishTime = new TimeSpan(0, 51, 40), Position = 2, Published = true },
            new RaceResult { EnrolmentId = e3.EnrolmentId, FinishTime = new TimeSpan(0, 29, 5), Position = 3, Published = true });
        await db.SaveChangesAsync();
    }
}
