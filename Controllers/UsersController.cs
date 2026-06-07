using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/users"), Authorize]
public class UsersController(LmsDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? orgId, [FromQuery] string? role,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var q = db.Users
            .Include(u => u.Organization)
            .Include(u => u.RoleAssignments.Where(r => r.IsActive))
            .Include(u => u.UserDepartments).ThenInclude(ud => ud.Department)
            .AsQueryable();

        if (User.IsInRole("OrgAdmin"))
        {
            var myOrgId = int.Parse(User.FindFirst("orgId")!.Value);
            q = q.Where(u => u.OrganizationId == myOrgId);
        }
        else if (orgId.HasValue)
            q = q.Where(u => u.OrganizationId == orgId.Value);

        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, out var r))
            q = q.Where(u => u.Role == r || u.RoleAssignments.Any(ra => ra.Role == r && ra.IsActive));

        if (!string.IsNullOrEmpty(search))
            q = q.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync();

        return Ok(new PagedResult<object>(items.Select(u => MapUser(u)).Cast<object>().ToList(),
            total, page, size, (int)Math.Ceiling(total / (double)size)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var u = await db.Users
            .Include(u => u.Organization)
            .Include(u => u.RoleAssignments.Where(r => r.IsActive))
            .Include(u => u.UserDepartments).ThenInclude(ud => ud.Department)
            .FirstOrDefaultAsync(u => u.Id == id);
        return u is null ? NotFound() : Ok(MapUser(u));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserMultiRoleRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { message = "Email already exists" });

        if (!req.Roles.Any()) return BadRequest(new { message = "At least one role required" });

        // Parse primary role (highest privilege)
        var primaryRole = req.Roles
            .Select(r => Enum.TryParse<UserRole>(r, out var rv) ? rv : (UserRole?)null)
            .Where(r => r.HasValue).Select(r => r!.Value)
            .OrderBy(r => r).First();

        // OrgAdmin cannot create SuperAdmin
        if (User.IsInRole("OrgAdmin") && primaryRole == UserRole.SuperAdmin)
            return Forbid();

        var user = new User
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = primaryRole,
            OrganizationId = req.OrganizationId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Add all role assignments
        foreach (var roleName in req.Roles)
        {
            if (Enum.TryParse<UserRole>(roleName, out var rv))
                db.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, Role = rv });
        }

        // Add department assignments
        if (req.DepartmentIds != null)
        {
            foreach (var deptId in req.DepartmentIds)
                db.UserDepartments.Add(new UserDepartment { UserId = user.Id, DepartmentId = deptId });
        }

        await db.SaveChangesAsync();

        var created = await db.Users.Include(u => u.Organization)
            .Include(u => u.RoleAssignments.Where(r => r.IsActive))
            .Include(u => u.UserDepartments).ThenInclude(ud => ud.Department)
            .FirstAsync(u => u.Id == user.Id);

        return CreatedAtAction(nameof(Get), new { id = user.Id }, MapUser(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (req.FirstName is not null) user.FirstName = req.FirstName;
        if (req.LastName is not null) user.LastName = req.LastName;
        if (req.PhoneNumber is not null) user.PhoneNumber = req.PhoneNumber;
        if (req.AvatarUrl is not null) user.AvatarUrl = req.AvatarUrl;
        if (req.IsActive is not null) user.IsActive = req.IsActive.Value;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Update roles for a user
    [HttpPut("{id}/roles")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> UpdateRoles(int id, [FromBody] UpdateUserRolesRequest req)
    {
        var user = await db.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        // Remove all existing role assignments
        db.UserRoleAssignments.RemoveRange(user.RoleAssignments);

        // Add new ones
        var roles = new List<UserRole>();
        foreach (var roleName in req.Roles)
        {
            if (Enum.TryParse<UserRole>(roleName, out var rv))
            {
                db.UserRoleAssignments.Add(new UserRoleAssignment { UserId = id, Role = rv });
                roles.Add(rv);
            }
        }

        // Update primary role
        if (roles.Any()) user.Role = roles.OrderBy(r => r).First();

        // Update department assignments
        if (req.DepartmentIds != null)
        {
            var existing = await db.UserDepartments.Where(ud => ud.UserId == id).ToListAsync();
            db.UserDepartments.RemoveRange(existing);
            foreach (var deptId in req.DepartmentIds)
                db.UserDepartments.Add(new UserDepartment { UserId = id, DepartmentId = deptId });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    static object MapUser(User u) => new
    {
        u.Id, u.FirstName, u.LastName, u.Email, u.AvatarUrl, u.PhoneNumber, u.Bio,
        Role = u.Role.ToString(),
        Roles = u.RoleAssignments.Any()
            ? u.RoleAssignments.Where(r => r.IsActive).Select(r => r.Role.ToString()).ToList()
            : new List<string> { u.Role.ToString() },
        u.IsActive, u.CreatedAt, u.LastLogin,
        u.OrganizationId,
        OrganizationName = u.Organization.Name,
        Departments = u.UserDepartments.Select(ud => new { ud.DepartmentId, ud.Department?.Name }).ToList()
    };
}
