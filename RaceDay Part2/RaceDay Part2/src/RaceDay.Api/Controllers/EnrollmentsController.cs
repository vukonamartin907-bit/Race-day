using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize(Roles = Roles.Participant), Route("api/enrollments")]
public class EnrollmentsController(RaceDayContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task<int?> ParticipantId() => db.ParticipantProfiles.Where(x => x.UserId == UserId).Select(x => (int?)x.ParticipantId).SingleOrDefaultAsync();

    /// <summary>Enrol in an event by selecting one of its categories.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(EnrollmentRequest r)
    {
        var pid = await ParticipantId(); if (pid is null) return Forbid();
        var e = await db.Events.SingleOrDefaultAsync(x => x.EventId == r.EventId);
        if (e is null) return NotFound(new { message = "Event not found." });
        if (e.Status != "Open") return BadRequest(new { message = "Enrolment is available only for Open events." });
        var cat = await db.Categories.SingleOrDefaultAsync(x => x.EventId == r.EventId && x.CategoryId == r.CategoryId);
        if (cat is null) return BadRequest(new { message = "Category does not belong to the selected event." });
        if (await db.Enrollments.AnyAsync(x => x.EventId == r.EventId && x.ParticipantId == pid)) return Conflict(new { message = "Already enrolled in this event." });
        var count = await db.Enrollments.CountAsync(x => x.CategoryId == r.CategoryId && x.Status != "Cancelled");
        if (count >= cat.MaxParticipants) return Conflict(new { message = "This category is full." });
        var maxRaceNo = await db.Enrollments.Select(x => (int?)x.RaceNumber).MaxAsync() ?? 1000;
        var enrollment = new Enrollment { EventId = r.EventId, CategoryId = r.CategoryId, ParticipantId = pid.Value, RaceNumber = maxRaceNo + 1, Status = "Confirmed", EnrollmentDate = DateTime.UtcNow };
        db.Enrollments.Add(enrollment); await db.SaveChangesAsync();
        return StatusCode(201, new { enrollment.EnrollmentId, enrollment.EventId, enrollment.CategoryId, enrollment.ParticipantId, enrollment.RaceNumber, enrollment.Status });
    }

    /// <summary>View only the current Participant's enrolments.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var pid = await ParticipantId(); if (pid is null) return Forbid();
        return Ok(await db.Enrollments.Where(x => x.ParticipantId == pid).Select(x => new { x.EnrollmentId, x.EventId, EventName = x.Event!.EventName,
            x.CategoryId, CategoryName = x.Category!.CategoryName, x.EnrollmentDate, x.RaceNumber, x.Status }).ToListAsync());
    }

    /// <summary>Organisers can view enrolments for events they own.</summary>
    [HttpGet("~/api/events/{eventId:int}/enrollments"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var oid = await db.OrganiserProfiles.Where(x => x.UserId == uid).Select(x => (int?)x.OrganiserId).SingleOrDefaultAsync();
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        if (oid != e.OrganiserId) return Forbid();
        return Ok(await db.Enrollments.Where(x => x.EventId == eventId).Select(x => new { x.EnrollmentId, x.RaceNumber, x.Status,
            ParticipantName = x.ParticipantProfile!.UserAccount!.FirstName + " " + x.ParticipantProfile.UserAccount.LastName,
            x.CategoryId, CategoryName = x.Category!.CategoryName, x.EnrollmentDate }).ToListAsync());
    }
}
