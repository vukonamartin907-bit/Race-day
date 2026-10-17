using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public static class Roles { public const string Organiser = "Organiser"; public const string Participant = "Participant"; }
public class UserAccount
{
    public int UserId { get; set; }
    [MaxLength(50)] public string FirstName { get; set; } = "";
    [MaxLength(50)] public string LastName { get; set; } = "";
    [MaxLength(150)] public string Email { get; set; } = "";
    [MaxLength(255)] public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string Role { get; set; } = Roles.Participant;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public OrganiserProfile? OrganiserProfile { get; set; }
    public ParticipantProfile? ParticipantProfile { get; set; }
}
public class OrganiserProfile
{
    public int OrganiserId { get; set; }
    public int UserId { get; set; }
    [MaxLength(120)] public string OrganisationName { get; set; } = "";
    [MaxLength(30)] public string ContactNumber { get; set; } = "";
    public UserAccount? UserAccount { get; set; }
    public ICollection<RaceEvent> Events { get; set; } = new List<RaceEvent>();
}
public class ParticipantProfile
{
    public int ParticipantId { get; set; }
    public int UserId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(100)] public string? EmergencyContactName { get; set; }
    [MaxLength(30)] public string? EmergencyContactNumber { get; set; }
    public UserAccount? UserAccount { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
public class RaceEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    [MaxLength(150)] public string EventName { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(10)] public string EventType { get; set; } = "Run";
    public DateOnly EventDate { get; set; }
    [MaxLength(150)] public string Venue { get; set; } = "";
    [MaxLength(80)] public string City { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Planned";
    public OrganiserProfile? OrganiserProfile { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    [MaxLength(100)] public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
    public RaceEvent? Event { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public int RaceNumber { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Confirmed";
    public RaceEvent? Event { get; set; }
    public Category? Category { get; set; }
    public ParticipantProfile? ParticipantProfile { get; set; }
    public Result? Result { get; set; }
}
public class Result
{
    public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    [MaxLength(20)] public string ResultStatus { get; set; } = "Pending";
    public DateTime? RecordedAt { get; set; }
    public Enrollment? Enrollment { get; set; }
}
