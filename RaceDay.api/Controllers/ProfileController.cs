using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET /api/profile/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var profile = new ProfileDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.Name
        };

        return Ok(profile);
    }

    // PUT /api/profile/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized(new { message = "You must be logged in." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Check if new email is already taken by another user
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserId != userId))
            return BadRequest(new { message = "Email is already in use." });

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully." });
    }
}