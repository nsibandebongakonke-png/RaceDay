using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Role).WithOne(r => r.User).HasForeignKey<Role>(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Role>().HasKey(r => r.UserId);

        b.Entity<Event>(e =>
        {
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents).HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OrganiserId);
            e.HasIndex(x => x.Date);
        });

        b.Entity<Category>(e =>
        {
            e.HasOne(c => c.Event).WithMany(ev => ev.Categories).HasForeignKey(c => c.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments).HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ParticipantId);
            e.HasIndex(x => x.EventId);
        });

        b.Entity<RaceResult>(e =>
        {
            e.HasOne(r => r.Enrolment).WithOne(x => x.Result).HasForeignKey<RaceResult>(r => r.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => r.EnrolmentId).IsUnique();
        });
    }
}
