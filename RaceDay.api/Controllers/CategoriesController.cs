using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET /api/events/{eventId}/categories
    [HttpGet("events/{eventId}/categories")]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        var exists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!exists)
            return NotFound(new { message = "Event not found." });

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                EventId = c.EventId
            })
            .ToListAsync();

        return Ok(categories);
    }

    // POST /api/events/{eventId}/categories
    [HttpPost("events/{eventId}/categories")]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid();

        var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (eventItem == null)
            return NotFound(new { message = "Event not found." });

        // Only the organiser who owns the event can add categories
        if (eventItem.OrganiserId != userId)
            return Forbid();

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            EventId = eventId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            message = "Category created successfully.",
            categoryId = category.CategoryId
        });
    }

    // PUT /api/categories/{id}
    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid();

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != userId)
            return Forbid();

        category.Name = request.Name;
        category.Description = request.Description;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Category updated successfully." });
    }

    // DELETE /api/categories/{id}
    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");

        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        if (role != "Organiser")
            return Forbid();

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != userId)
            return Forbid();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Category deleted successfully." });
    }
}