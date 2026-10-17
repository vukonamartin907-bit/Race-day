using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/events")]
public class EventsController(RaceDayContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Task<int?> OrganiserId() => db.OrganiserProfiles.Where(x => x.UserId == UserId).Select(x => (int?)x.OrganiserId).SingleOrDefaultAsync();

    /// <summary>View all events and their current status.</summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await db.Events.AsNoTracking().Select(e => new { e.EventId, e.EventName, e.Description, e.EventType, e.EventDate, e.Venue, e.City, e.Status }).ToListAsync());

    /// <summary>View an event and its categories.</summary>
    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> Get(int id)
    {
        var e = await db.Events.AsNoTracking().Where(x => x.EventId == id)
            .Select(x => new { x.EventId, x.OrganiserId, x.EventName, x.Description, x.EventType, x.EventDate, x.Venue, x.City, x.Status,
                Categories = x.Categories.Select(c => new { c.CategoryId, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.EntryFee }) }).SingleOrDefaultAsync();
        return e is null ? NotFound() : Ok(e);
    }

    /// <summary>Create an event for the authenticated Organiser.</summary>
    [HttpPost, Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Create(EventRequest r)
    {
        var oid = await OrganiserId(); if (oid is null) return Forbid();
        if (!ValidStatus(r.Status)) return BadRequest(new { message = "Invalid event status." });
        if (!ValidEventType(r.EventType)) return BadRequest(new { message = "EventType must be Run, Walk, or Cycle." });
        var e = new RaceEvent { OrganiserId = oid.Value, EventName = r.EventName.Trim(), Description = r.Description, EventType = CanonicalEventType(r.EventType), EventDate = r.EventDate,
            Venue = r.Venue.Trim(), City = r.City.Trim(), Status = r.Status };
        db.Events.Add(e); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Get), new { id = e.EventId }, new { e.EventId, e.EventName, e.Status });
    }

    /// <summary>Update an event owned by the current Organiser.</summary>
    [HttpPut("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Update(int id, EventRequest r)
    {
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound();
        var oid = await OrganiserId(); if (oid != e.OrganiserId) return Forbid();
        if (!ValidStatus(r.Status)) return BadRequest(new { message = "Invalid event status." });
        if (!ValidEventType(r.EventType)) return BadRequest(new { message = "EventType must be Run, Walk, or Cycle." });
        e.EventName = r.EventName.Trim(); e.Description = r.Description; e.EventType = CanonicalEventType(r.EventType); e.EventDate = r.EventDate; e.Venue = r.Venue.Trim(); e.City = r.City.Trim(); e.Status = r.Status;
        await db.SaveChangesAsync(); return NoContent();
    }

    /// <summary>Delete an event owned by the current Organiser.</summary>
    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Events.FindAsync(id); if (e is null) return NotFound();
        var oid = await OrganiserId(); if (oid != e.OrganiserId) return Forbid();
        if (await db.Enrollments.AnyAsync(x => x.EventId == id)) return Conflict(new { message = "Cannot delete an event with enrolments." });
        db.Events.Remove(e); await db.SaveChangesAsync(); return NoContent();
    }

    /// <summary>List categories for an event.</summary>
    [HttpGet("{eventId:int}/categories"), AllowAnonymous]
    public async Task<IActionResult> Categories(int eventId)
    {
        if (!await db.Events.AnyAsync(x => x.EventId == eventId)) return NotFound();
        return Ok(await db.Categories.Where(x => x.EventId == eventId).Select(c => new { c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.EntryFee }).ToListAsync());
    }

    /// <summary>Add a category to an event owned by the current Organiser.</summary>
    [HttpPost("{eventId:int}/categories"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> AddCategory(int eventId, CategoryRequest r)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        var oid = await OrganiserId(); if (oid != e.OrganiserId) return Forbid();
        if (await db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == r.CategoryName)) return Conflict(new { message = "Category name already exists for this event." });
        var c = new Category { EventId = eventId, CategoryName = r.CategoryName.Trim(), DistanceKm = r.DistanceKm, MaxParticipants = r.MaxParticipants, EntryFee = r.EntryFee };
        db.Categories.Add(c); await db.SaveChangesAsync(); return StatusCode(201, new { c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.EntryFee });
    }

    /// <summary>Update a category belonging to an event owned by the current Organiser.</summary>
    [HttpPut("{eventId:int}/categories/{categoryId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> UpdateCategory(int eventId, int categoryId, CategoryRequest r)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        var oid = await OrganiserId(); if (oid != e.OrganiserId) return Forbid();
        var category = await db.Categories.SingleOrDefaultAsync(c => c.EventId == eventId && c.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (await db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryId != categoryId && c.CategoryName == r.CategoryName.Trim()))
            return Conflict(new { message = "Category name already exists for this event." });
        category.CategoryName = r.CategoryName.Trim(); category.DistanceKm = r.DistanceKm;
        category.MaxParticipants = r.MaxParticipants; category.EntryFee = r.EntryFee;
        await db.SaveChangesAsync();
        return Ok(new { category.CategoryId, category.EventId, category.CategoryName, category.DistanceKm, category.MaxParticipants, category.EntryFee });
    }

    /// <summary>Delete a category if no enrolments use it.</summary>
    [HttpDelete("{eventId:int}/categories/{categoryId:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var e = await db.Events.FindAsync(eventId); if (e is null) return NotFound();
        var oid = await OrganiserId(); if (oid != e.OrganiserId) return Forbid();
        var category = await db.Categories.SingleOrDefaultAsync(c => c.EventId == eventId && c.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (await db.Enrollments.AnyAsync(x => x.CategoryId == categoryId))
            return Conflict(new { message = "Cannot delete a category with enrolments." });
        db.Categories.Remove(category); await db.SaveChangesAsync(); return NoContent();
    }

    private static bool ValidStatus(string s) => new[] { "Planned", "Open", "Closed", "Completed", "Cancelled" }.Contains(s);
    private static bool ValidEventType(string s) => new[] { "Run", "Walk", "Cycle" }.Contains(s, StringComparer.OrdinalIgnoreCase);
    private static string CanonicalEventType(string s) => s.Trim().ToLowerInvariant() switch { "run" => "Run", "walk" => "Walk", "cycle" => "Cycle", _ => s.Trim() };
}
