using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayContext(DbContextOptions<RaceDayContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<OrganiserProfile> OrganiserProfiles => Set<OrganiserProfile>();
    public DbSet<ParticipantProfile> ParticipantProfiles => Set<ParticipantProfile>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserAccount>().ToTable("UserAccount").HasKey(x => x.UserId);
        b.Entity<UserAccount>().HasIndex(x => x.Email).IsUnique();
        b.Entity<UserAccount>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Entity<UserAccount>().Property(x => x.IsActive).HasDefaultValue(true);
        b.Entity<UserAccount>().ToTable(t => t.HasCheckConstraint("CK_UserAccount_Role", "[Role] IN ('Organiser','Participant')"));

        b.Entity<OrganiserProfile>().ToTable("OrganiserProfile").HasKey(x => x.OrganiserId);
        b.Entity<OrganiserProfile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<OrganiserProfile>().HasOne(x => x.UserAccount).WithOne(x => x.OrganiserProfile).HasForeignKey<OrganiserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ParticipantProfile>().ToTable("ParticipantProfile").HasKey(x => x.ParticipantId);
        b.Entity<ParticipantProfile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<ParticipantProfile>().HasOne(x => x.UserAccount).WithOne(x => x.ParticipantProfile).HasForeignKey<ParticipantProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<RaceEvent>().ToTable("Event").HasKey(x => x.EventId);
        b.Entity<RaceEvent>().Property(x => x.Status).HasDefaultValue("Planned");
        b.Entity<RaceEvent>().Property(x => x.EventType).HasMaxLength(10);
        b.Entity<RaceEvent>().ToTable(t => {
            t.HasCheckConstraint("CK_Event_Status", "[Status] IN ('Planned','Open','Closed','Completed','Cancelled')");
            t.HasCheckConstraint("CK_Event_EventType", "[EventType] IN ('Run','Walk','Cycle')");
        });
        b.Entity<RaceEvent>().HasOne(x => x.OrganiserProfile).WithMany(x => x.Events).HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Category>().ToTable("Category").HasKey(x => x.CategoryId);
        b.Entity<Category>().Property(x => x.DistanceKm).HasPrecision(6, 2);
        b.Entity<Category>().Property(x => x.EntryFee).HasPrecision(10, 2);
        b.Entity<Category>().HasIndex(x => new { x.EventId, x.CategoryName }).IsUnique();
        b.Entity<Category>().HasAlternateKey(x => new { x.EventId, x.CategoryId });
        b.Entity<Category>().ToTable(t => {
            t.HasCheckConstraint("CK_Category_Distance", "[DistanceKm] > 0");
            t.HasCheckConstraint("CK_Category_MaxParticipants", "[MaxParticipants] > 0");
            t.HasCheckConstraint("CK_Category_EntryFee", "[EntryFee] >= 0");
        });
        b.Entity<Category>().HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Enrollment>().ToTable("Enrollment").HasKey(x => x.EnrollmentId);
        b.Entity<Enrollment>().Property(x => x.EnrollmentDate).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Entity<Enrollment>().Property(x => x.Status).HasDefaultValue("Confirmed");
        b.Entity<Enrollment>().HasIndex(x => x.RaceNumber).IsUnique();
        b.Entity<Enrollment>().HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();
        b.Entity<Enrollment>().ToTable(t => t.HasCheckConstraint("CK_Enrollment_Status", "[Status] IN ('Pending','Confirmed','Cancelled','Completed')"));
        b.Entity<Enrollment>().HasOne(x => x.ParticipantProfile).WithMany(x => x.Enrollments).HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasOne(x => x.Event).WithMany(x => x.Enrollments).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasOne(x => x.Category).WithMany(x => x.Enrollments).HasForeignKey(x => new { x.EventId, x.CategoryId }).HasPrincipalKey(x => new { x.EventId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Result>().ToTable("Result").HasKey(x => x.ResultId);
        b.Entity<Result>().HasIndex(x => x.EnrollmentId).IsUnique();
        b.Entity<Result>().Property(x => x.ResultStatus).HasDefaultValue("Pending");
        b.Entity<Result>().ToTable(t => {
            t.HasCheckConstraint("CK_Result_Position", "[Position] IS NULL OR [Position] > 0");
            t.HasCheckConstraint("CK_Result_Status", "[ResultStatus] IN ('Pending','Finished','DNF','DNS')");
        });
        b.Entity<Result>().HasOne(x => x.Enrollment).WithOne(x => x.Result).HasForeignKey<Result>(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        base.OnModelCreating(b);
    }
}
