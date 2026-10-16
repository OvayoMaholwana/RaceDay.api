using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // POST /api/results
    [HttpPost]
    public async Task<IActionResult> CreateResult([FromBody] CreateResultRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid(); // Only Organisers can capture results

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        // Only the organiser of the event can add results
        if (enrolment.Event.OrganiserId != userId)
            return Forbid();

        // Prevent duplicate result
        if (enrolment.Result != null)
            return BadRequest(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            message = "Result recorded successfully.",
            resultId = result.ResultId
        });
    }

    // GET /api/results/mine
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        var results = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
            .Where(r => r.Enrolment.ParticipantId == userId)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                FinishTime = r.FinishTime,
                Position = r.Position,
                EnrolmentId = r.EnrolmentId,
                ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                EventName = r.Enrolment.Event.Name
            })
            .ToListAsync();

        return Ok(results);
    }

    // GET /api/results/event/{eventId}
    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> GetResultsForEvent(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });

        // Organiser of the event or any logged-in user can view results (adjust if needed)
        // For stricter control you can force only the organiser:
        // if (role != "Organiser" || eventItem.OrganiserId != userId) return Forbid();

        var results = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
            .Where(r => r.Enrolment.EventId == eventId)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                FinishTime = r.FinishTime,
                Position = r.Position,
                EnrolmentId = r.EnrolmentId,
                ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                EventName = r.Enrolment.Event.Name
            })
            .ToListAsync();

        return Ok(results);
    }
}