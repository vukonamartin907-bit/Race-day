using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(RaceDayContext db) : ControllerBase
{
    private readonly PasswordHasher<UserAccount> hasher = new();

    /// <summary>Register an account and create the role-specific profile.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest r)
    {
        var role = r.Role.Trim();
        if (role != Roles.Organiser && role != Roles.Participant) return BadRequest(new { message = "Role must be Organiser or Participant." });
        var email = r.Email.Trim().ToLowerInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.Email == email)) return Conflict(new { message = "Email already registered." });
        if (role == Roles.Organiser && (string.IsNullOrWhiteSpace(r.OrganisationName) || string.IsNullOrWhiteSpace(r.ContactNumber)))
            return BadRequest(new { message = "OrganisationName and ContactNumber are required for an Organiser." });
        var user = new UserAccount { FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Email = email, Role = role, CreatedAt = DateTime.UtcNow, IsActive = true };
        user.PasswordHash = hasher.HashPassword(user, r.Password);
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
        if (role == Roles.Organiser) db.OrganiserProfiles.Add(new OrganiserProfile { UserId = user.UserId, OrganisationName = r.OrganisationName!.Trim(), ContactNumber = r.ContactNumber!.Trim() });
        else db.ParticipantProfiles.Add(new ParticipantProfile { UserId = user.UserId, DateOfBirth = r.DateOfBirth, EmergencyContactName = r.EmergencyContactName, EmergencyContactNumber = r.EmergencyContactNumber });
        await db.SaveChangesAsync();
        return StatusCode(201, new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role });
    }

    /// <summary>Log in and establish an HTTP-only cookie session.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Email == r.Email.Trim().ToLowerInvariant() && x.IsActive);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });
        HttpContext.Session.SetString("UserId", user.UserId.ToString()); HttpContext.Session.SetString("Role", user.Role);
        return Ok(new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role });
    }

    /// <summary>Log out and clear authentication cookie and session.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); HttpContext.Session.Clear(); return NoContent();
    }
}
