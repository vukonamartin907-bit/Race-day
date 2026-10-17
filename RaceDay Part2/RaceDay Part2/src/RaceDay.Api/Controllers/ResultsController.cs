using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class ResultsController(RaceDayContext db) : ControllerBase
{
    /// <summary>Record or update a result for an enrolment in an event owned by the Organiser.</summary>
    [HttpPost("events/{eventId:int}/results"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Record(int eventId, [FromQuery] int enrollmentId, ResultRequest r)
    {
        var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var oid = await db.OrganiserProfiles.Where(x => x.UserId == uid).Select(x => (int?)x.OrganiserId).SingleOrDefaultAsync();
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        if (oid != e.OrganiserId) return Forbid();
        if (!new[] { "Pending", "Finished", "DNF", "DNS" }.Contains(r.ResultStatus)) return BadRequest(new { message = "Invalid result status." });
        if (r.Position is <= 0 || (r.FinishTime.HasValue && r.FinishTime.Value < TimeSpan.Zero)) return BadRequest(new { message = "Position must be positive and finish time cannot be negative." });
        if (r.ResultStatus == "Finished" && (r.Position is null || r.FinishTime is null)) return BadRequest(new { message = "Finished results require FinishTime and Position." });
        var en = await db.Enrollments.SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId && x.EventId == eventId);
        if (en is null) return NotFound(new { message = "Enrolment not found for this event." });
        var result = await db.Results.SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId);
        if (result is null) { result = new Result { EnrollmentId = enrollmentId }; db.Results.Add(result); }
        result.FinishTime = r.FinishTime; result.Position = r.Position; result.ResultStatus = r.ResultStatus; result.RecordedAt = DateTime.UtcNow;
        en.Status = r.ResultStatus == "Finished" ? "Completed" : en.Status;
        await db.SaveChangesAsync(); return Ok(new { result.ResultId, result.EnrollmentId, result.FinishTime, result.Position, result.ResultStatus, result.RecordedAt });
    }

    /// <summary>View results linked only to the current Participant's profile.</summary>
    [HttpGet("results/mine"), Authorize(Roles = Roles.Participant)]
    public async Task<IActionResult> Mine()
    {
        var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var pid = await db.ParticipantProfiles.Where(x => x.UserId == uid).Select(x => (int?)x.ParticipantId).SingleOrDefaultAsync();
        if (pid is null) return Forbid();
        return Ok(await db.Results.Where(r => r.Enrollment!.ParticipantId == pid).Select(r => new { r.ResultId, r.EnrollmentId,
            EventName = r.Enrollment!.Event!.EventName, r.Enrollment.RaceNumber, r.FinishTime, r.Position, r.ResultStatus, r.RecordedAt }).ToListAsync());
    }
}
