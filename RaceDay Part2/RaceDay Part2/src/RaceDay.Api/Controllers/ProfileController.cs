using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize, Route("api/profile")]
public class ProfileController(RaceDayContext db) : ControllerBase
{
    private int Id => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>View the current user's account and role-specific profile.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var u = await db.UserAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == Id);
        if (u is null) return NotFound();
        if (u.Role == Roles.Organiser)
        {
            var p = await db.OrganiserProfiles.AsNoTracking().SingleAsync(x => x.UserId == Id);
            return Ok(new { u.UserId, u.FirstName, u.LastName, u.Email, u.Role, p.OrganiserId, p.OrganisationName, p.ContactNumber });
        }
        var pp = await db.ParticipantProfiles.AsNoTracking().SingleAsync(x => x.UserId == Id);
        return Ok(new { u.UserId, u.FirstName, u.LastName, u.Email, u.Role, pp.ParticipantId, pp.DateOfBirth, pp.EmergencyContactName, pp.EmergencyContactNumber });
    }

    /// <summary>Update own name and role-specific profile fields.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> Update(UpdateProfileRequest r)
    {
        var u = await db.UserAccounts.SingleOrDefaultAsync(x => x.UserId == Id);
        if (u is null) return NotFound();
        u.FirstName = r.FirstName.Trim(); u.LastName = r.LastName.Trim();
        if (u.Role == Roles.Organiser)
        {
            var p = await db.OrganiserProfiles.SingleAsync(x => x.UserId == Id);
            if (string.IsNullOrWhiteSpace(r.OrganisationName) || string.IsNullOrWhiteSpace(r.ContactNumber))
                return BadRequest(new { message = "OrganisationName and ContactNumber are required." });
            p.OrganisationName = r.OrganisationName.Trim(); p.ContactNumber = r.ContactNumber.Trim();
        }
        else
        {
            var p = await db.ParticipantProfiles.SingleAsync(x => x.UserId == Id);
            p.DateOfBirth = r.DateOfBirth; p.EmergencyContactName = r.EmergencyContactName; p.EmergencyContactNumber = r.EmergencyContactNumber;
        }
        await db.SaveChangesAsync(); return NoContent();
    }
}
