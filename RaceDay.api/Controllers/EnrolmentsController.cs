using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // POST /api/enrolments
    [HttpPost]
    public async Task<IActionResult> CreateEnrolment([FromBody] CreateEnrolmentRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Participant")
            return Forbid(); // Only Participants can enrol

        // Check event exists
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == request.EventId);
        if (!eventExists)
            return NotFound(new { message = "Event not found." });

        // Check category exists and belongs to the event
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);

        if (category == null)
            return BadRequest(new { message = "Category not found or does not belong to this event." });

        // Prevent duplicate enrolment
        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(e => e.ParticipantId == userId && e.EventId == request.EventId);

        if (alreadyEnrolled)
            return BadRequest(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = userId.Value,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            Status = "Pending"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            message = "Enrolment created successfully.",
            enrolmentId = enrolment.EnrolmentId
        });
    }

    // GET /api/enrolments/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        var enrolments = await _context.Enrolments
            .Where(e => e.ParticipantId == userId)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                Status = e.Status,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                EventId = e.EventId,
                EventName = e.Event.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // GET /api/enrolments/event/{eventId}
    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });

        // Only the organiser of the event can view all enrolments
        if (role != "Organiser" || eventItem.OrganiserId != userId)
            return Forbid();

        var enrolments = await _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Include(e => e.Event)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                Status = e.Status,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                EventId = e.EventId,
                EventName = e.Event.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // PUT /api/enrolments/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateEnrolmentStatus(int id, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid();

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        // Only the organiser of the event can change status
        if (enrolment.Event.OrganiserId != userId)
            return Forbid();

        // Basic validation
        var allowedStatuses = new[] { "Pending", "Confirmed", "Cancelled" };
        if (!allowedStatuses.Contains(request.Status))
            return BadRequest(new { message = "Invalid status. Use Pending, Confirmed or Cancelled." });

        enrolment.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Enrolment status updated successfully." });
    }
}