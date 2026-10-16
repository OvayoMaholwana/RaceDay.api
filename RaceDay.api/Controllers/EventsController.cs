using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET /api/events
    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await _context.Events
            .Include(e => e.Organiser)
            .Select(e => new EventDto
            {
                EventId = e.EventId,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET /api/events/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var eventItem = await _context.Events
            .Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (eventItem == null)
            return NotFound(new { message = "Event not found." });

        var dto = new EventDto
        {
            EventId = eventItem.EventId,
            Name = eventItem.Name,
            Description = eventItem.Description,
            EventDate = eventItem.EventDate,
            Location = eventItem.Location,
            DistanceKm = eventItem.DistanceKm,
            EventType = eventItem.EventType,
            OrganiserId = eventItem.OrganiserId,
            OrganiserName = eventItem.Organiser.FirstName + " " + eventItem.Organiser.LastName
        };

        return Ok(dto);
    }

    // GET /api/events/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid(); // 403

        var events = await _context.Events
            .Where(e => e.OrganiserId == userId)
            .Include(e => e.Organiser)
            .Select(e => new EventDto
            {
                EventId = e.EventId,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName
            })
            .ToListAsync();

        return Ok(events);
    }

    // POST /api/events
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid(); // Only Organisers can create events

        var newEvent = new Event
        {
            Name = request.Name,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            OrganiserId = userId.Value
        };

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { message = "Event created successfully.", eventId = newEvent.EventId });
    }

    // PUT /api/events/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid();

        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });

        // Only the organiser who owns the event can update it
        if (eventItem.OrganiserId != userId)
            return Forbid();

        eventItem.Name = request.Name;
        eventItem.Description = request.Description;
        eventItem.EventDate = request.EventDate;
        eventItem.Location = request.Location;
        eventItem.DistanceKm = request.DistanceKm;
        eventItem.EventType = request.EventType;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Event updated successfully." });
    }

    // DELETE /api/events/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid();

        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });

        if (eventItem.OrganiserId != userId)
            return Forbid();

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Event deleted successfully." });
    }
}