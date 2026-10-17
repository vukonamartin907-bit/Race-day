using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;
public record RegisterRequest([Required, MaxLength(50)] string FirstName, [Required, MaxLength(50)] string LastName,
    [Required, EmailAddress, MaxLength(150)] string Email, [Required, MinLength(8)] string Password,
    [Required] string Role, string? OrganisationName, string? ContactNumber, DateOnly? DateOfBirth,
    string? EmergencyContactName, string? EmergencyContactNumber);
public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record UpdateProfileRequest([Required, MaxLength(50)] string FirstName, [Required, MaxLength(50)] string LastName,
    string? OrganisationName, string? ContactNumber, DateOnly? DateOfBirth, string? EmergencyContactName, string? EmergencyContactNumber);
public record EventRequest([Required, MaxLength(150)] string EventName, [MaxLength(500)] string? Description,
    [Required] string EventType, DateOnly EventDate, [Required, MaxLength(150)] string Venue, [Required, MaxLength(80)] string City,
    [Required] string Status);
public record CategoryRequest([Required, MaxLength(100)] string CategoryName,
    [Range(0.01, 9999)] decimal DistanceKm, [Range(1, int.MaxValue)] int MaxParticipants,
    [Range(0, 999999)] decimal EntryFee);
public record EnrollmentRequest([Range(1, int.MaxValue)] int EventId, [Range(1, int.MaxValue)] int CategoryId);
public record ResultRequest(TimeSpan? FinishTime, int? Position, [Required] string ResultStatus);
