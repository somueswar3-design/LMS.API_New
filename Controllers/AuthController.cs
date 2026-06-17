using LMS.API.Data;
using LMS.API.Services;
using LMS.API.DTOs;
using LMS.API.Models;
using LMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(LmsDbContext db, IAuthService auth, IEmailService emailService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        user.LastLogin = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token        = auth.GenerateJwt(user);
        var refreshToken = auth.GenerateRefreshToken();

        return Ok(new LoginResponse(
            token,
            refreshToken,
            MapUser(user)
        ));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email already registered" });

        var org = await db.Organizations.FindAsync(req.OrganizationId);
        if (org is null) return BadRequest(new { message = "Organization not found" });

        var user = new User
        {
            FirstName      = req.FirstName,
            LastName       = req.LastName,
            Email          = req.Email,
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role           = UserRole.Student,
            OrganizationId = req.OrganizationId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        user = await db.Users.Include(u => u.Organization).FirstAsync(u => u.Id == user.Id);
        await emailService.SendWelcomeEmailAsync(user.Email, user.FirstName, org.Name);
        return Ok(new LoginResponse(auth.GenerateJwt(user), auth.GenerateRefreshToken(), MapUser(user)));
    }

    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        return Ok(MapUser(user));
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations()
    {
        var orgs = await db.Organizations
            .Where(o => o.IsActive)
            .Select(o => new { o.Id, o.Name, o.Slug, o.LogoUrl })
            .ToListAsync();
        return Ok(orgs);
    }

    static UserDto MapUser(User u) => new(
        u.Id, u.FirstName, u.LastName, u.Email, u.AvatarUrl,
        u.Role.ToString(), u.IsActive, u.CreatedAt, u.LastLogin,
        u.OrganizationId, u.Organization.Name
    );
}
