using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/departments"), Authorize]
public class DepartmentsController(LmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? orgId)
    {
        var q = db.Departments
            .Include(d => d.Categories)
            .Include(d => d.UserDepartments)
            .AsQueryable();

        if (orgId.HasValue) q = q.Where(d => d.OrganizationId == orgId.Value);

        var items = await q.OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var d = await db.Departments
            .Include(d => d.Categories)
            .Include(d => d.UserDepartments)
            .FirstOrDefaultAsync(d => d.Id == id);
        return d is null ? NotFound() : Ok(Map(d));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest req)
    {
        var dept = new Department
        {
            Name = req.Name,
            Description = req.Description,
            IconEmoji = req.IconEmoji ?? "🏢",
            Color = req.Color ?? "#6366f1",
            OrganizationId = req.OrganizationId,
            DisplayOrder = req.DisplayOrder
        };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = dept.Id }, Map(dept));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest req)
    {
        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();
        if (req.Name is not null) dept.Name = req.Name;
        if (req.Description is not null) dept.Description = req.Description;
        if (req.IconEmoji is not null) dept.IconEmoji = req.IconEmoji;
        if (req.Color is not null) dept.Color = req.Color;
        if (req.IsActive is not null) dept.IsActive = req.IsActive.Value;
        if (req.DisplayOrder is not null) dept.DisplayOrder = req.DisplayOrder.Value;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();
        dept.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Assign users to department
    [HttpPost("{id}/users")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> AssignUsers(int id, [FromBody] List<int> userIds)
    {
        var dept = await db.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        foreach (var uid in userIds)
        {
            if (!await db.UserDepartments.AnyAsync(ud => ud.UserId == uid && ud.DepartmentId == id))
                db.UserDepartments.Add(new UserDepartment { UserId = uid, DepartmentId = id });
        }
        await db.SaveChangesAsync();
        return Ok(new { assigned = userIds.Count });
    }

    [HttpDelete("{id}/users/{userId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> RemoveUser(int id, int userId)
    {
        var ud = await db.UserDepartments.FirstOrDefaultAsync(x => x.DepartmentId == id && x.UserId == userId);
        if (ud is not null) { db.UserDepartments.Remove(ud); await db.SaveChangesAsync(); }
        return NoContent();
    }

    static DepartmentDto Map(Department d) => new(
        d.Id, d.Name, d.Description, d.IconEmoji, d.Color, d.IsActive,
        d.DisplayOrder, d.Categories.Count, d.UserDepartments.Count, d.CreatedAt
    );
}
