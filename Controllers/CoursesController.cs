using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

// ═══════════════════════════════════════════════════════════════
//  COURSES
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/courses"), Authorize]
public class CoursesController(LmsDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? orgId, [FromQuery] int? categoryId, [FromQuery] int? instructorId,
        [FromQuery] string? level, [FromQuery] string? status,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var q = db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Organization)
            .Include(c => c.Enrollments)
            .Include(c => c.Ratings)
            .AsQueryable();

        if (orgId.HasValue) q = q.Where(c => c.OrganizationId == orgId.Value);
        if (categoryId.HasValue) q = q.Where(c => c.CategoryId == categoryId.Value);
        if (instructorId.HasValue) q = q.Where(c => c.InstructorId == instructorId.Value);
        if (!string.IsNullOrEmpty(level) && Enum.TryParse<CourseLevel>(level, out var lv))
            q = q.Where(c => c.Level == lv);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CourseStatus>(status, out var st))
            q = q.Where(c => c.Status == st);
        // No default status filter — admin sees all, public portal filters separately
        if (!string.IsNullOrEmpty(search))
            q = q.Where(c => c.Title.Contains(search) || (c.Description != null && c.Description.Contains(search)));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync();
        return Ok(new PagedResult<CourseDto>(items.Select(c => MapCourse(c)).ToList(), total, page, size, (int)Math.Ceiling(total / (double)size)));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id)
    {
        var c = await db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Organization)
            .Include(c => c.Enrollments)
            .Include(c => c.Ratings)
            .Include(c => c.Modules.OrderBy(m => m.DisplayOrder))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Id == id);
        return c is null ? NotFound() : Ok(MapCourse(c, includeModules: true));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Title is required" });
        if (!Enum.TryParse<CourseLevel>(req.Level, true, out var level))
            return BadRequest(new { message = $"Invalid level: {req.Level}" });

        try
        {
            var course = new Course
            {
                Title = req.Title,
                Description = req.Description,
                ThumbnailUrl = req.ThumbnailUrl,
                Level = level,
                Price = req.Price,
                IsFree = req.IsFree,
                // Nullable on both the DTO and the model — only assign when
                // the frontend actually sent a real category/instructor.
                // Previously this defaulted to 0 when omitted, which is not
                // a valid foreign key and crashed SaveChangesAsync with an
                // unhandled (and unreturned) exception.
                CategoryId = req.CategoryId is > 0 ? req.CategoryId : null,
                InstructorId = req.InstructorId is > 0 ? req.InstructorId : null,
                OrganizationId = req.OrganizationId,
                Tags = req.Tags,
                Language = req.Language,
                EnforceSequentialLessons = req.EnforceSequentialLessons
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = course.Id }, new { course.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to create course: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCourseRequest req)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        try
        {
            if (req.Title is not null) course.Title = req.Title;
            if (req.Description is not null) course.Description = req.Description;
            if (req.ThumbnailUrl is not null) course.ThumbnailUrl = req.ThumbnailUrl;
            if (req.Level is not null) course.Level = Enum.Parse<CourseLevel>(req.Level);
            if (req.Status is not null) course.Status = Enum.Parse<CourseStatus>(req.Status);
            if (req.Price is not null) course.Price = req.Price.Value;
            if (req.IsFree is not null) course.IsFree = req.IsFree.Value;
            if (req.CategoryId is not null) course.CategoryId = req.CategoryId.Value;
            if (req.Tags is not null) course.Tags = req.Tags;
            if (req.EnforceSequentialLessons is not null) course.EnforceSequentialLessons = req.EnforceSequentialLessons.Value;
            course.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Failed to update course: {ex.Message}" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();
        course.Status = CourseStatus.Archived;
        await db.SaveChangesAsync();
        return NoContent();
    }

    static CourseDto MapCourse(Course c, bool includeModules = false) => new(
        c.Id, c.Title, c.Description, c.ThumbnailUrl,
        c.Level.ToString(), c.Status.ToString(), c.Price, c.IsFree,
        c.DurationMinutes, c.Tags, c.Language,
        c.OrganizationId, c.Organization.Name,
        (int?)c.InstructorId, c.Instructor != null ? $"{c.Instructor.FirstName} {c.Instructor.LastName}" : null,
        (int?)c.CategoryId, c.Category?.Name,
        c.Enrollments.Count,
        c.Ratings.Count > 0 ? c.Ratings.Average(r => r.Rating) : 0,
        c.Ratings.Count,
        c.CreatedAt, c.UpdatedAt,
        includeModules ? c.Modules.OrderBy(m => m.DisplayOrder).Select(m => new ModuleDto(
            m.Id, m.Title, m.Description, m.DisplayOrder, m.IsPreview, m.CourseId,
            m.Lessons.OrderBy(l => l.DisplayOrder).Select(l => (object)new
            {
                l.Id,
                l.Title,
                l.Description,
                Type = l.Type.ToString(),
                l.IsPreview,
                l.IsPublished,
                l.DisplayOrder,
                l.DurationSecs,
                l.ModuleId,
                ModuleTitle = m.Title,
                l.VideoUrl,
                l.FileUrl,
                l.Content
            }).ToList()
        )).ToList() : null,
        c.EnforceSequentialLessons
    );
}

// ═══════════════════════════════════════════════════════════════
//  CATEGORIES
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/categories"), Authorize]
public class CategoriesController(LmsDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int? orgId, [FromQuery] int? departmentId)
    {
        var q = db.Categories.Include(c => c.Children).Include(c => c.Courses).Include(c => c.Department).AsQueryable();
        if (orgId.HasValue) q = q.Where(c => c.OrganizationId == orgId.Value);
        if (departmentId.HasValue) q = q.Where(c => c.DepartmentId == departmentId.Value);

        var all = await q.Where(c => c.ParentId == null).OrderBy(c => c.DisplayOrder).ToListAsync();
        return Ok(all.Select(c => MapCategory(c)));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        var cat = new Category
        {
            Name = req.Name,
            Description = req.Description,
            ParentId = req.ParentId,
            OrganizationId = req.OrganizationId,
            DisplayOrder = req.DisplayOrder,
            DepartmentId = req.DepartmentId
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return CreatedAtAction(null, new { id = cat.Id }, new { cat.Id, cat.Name });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest req)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        if (req.Name is not null) cat.Name = req.Name;
        if (req.Description is not null) cat.Description = req.Description;
        if (req.DisplayOrder is not null) cat.DisplayOrder = req.DisplayOrder.Value;
        if (req.IsActive is not null) cat.IsActive = req.IsActive.Value;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return NoContent();
    }

    static CategoryDto MapCategory(Category c) => new(
        c.Id, c.Name, c.Description, c.ParentId, c.Parent?.Name,
        c.DisplayOrder, c.IsActive,
        c.Children.Select(ch => MapCategory(ch)).ToList(),
        c.Courses.Count
    );
}

// ═══════════════════════════════════════════════════════════════
//  MODULES
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/modules"), Authorize]
public class ModulesController(LmsDbContext db) : ControllerBase
{
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var modules = await db.Modules
            .Include(m => m.Lessons.OrderBy(l => l.DisplayOrder))
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
        return Ok(modules.Select(m => new ModuleDto(m.Id, m.Title, m.Description, m.DisplayOrder, m.IsPreview, m.CourseId,
            m.Lessons.OrderBy(l => l.DisplayOrder).Select(l => (object)new
            {
                l.Id,
                l.Title,
                l.Description,
                Type = l.Type.ToString(),
                l.IsPreview,
                l.IsPublished,
                l.DisplayOrder,
                l.DurationSecs,
                l.ModuleId,
                ModuleTitle = m.Title,
                l.VideoUrl,
                l.FileUrl,
                l.Content,
                ContentBlocksCount = string.IsNullOrEmpty(l.ContentBlocksJson) ? 0 :
                    System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(l.ContentBlocksJson).GetArrayLength()
            }).ToList())));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateModuleRequest req)
    {
        var m = new Module { Title = req.Title, Description = req.Description, CourseId = req.CourseId, DisplayOrder = req.DisplayOrder, IsPreview = req.IsPreview };
        db.Modules.Add(m);
        await db.SaveChangesAsync();
        return Ok(new { m.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateModuleRequest req)
    {
        var m = await db.Modules.FindAsync(id);
        if (m is null) return NotFound();
        if (req.Title is not null) m.Title = req.Title;
        if (req.Description is not null) m.Description = req.Description;
        if (req.DisplayOrder is not null) m.DisplayOrder = req.DisplayOrder.Value;
        if (req.IsPreview is not null) m.IsPreview = req.IsPreview.Value;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await db.Modules.FindAsync(id);
        if (m is null) return NotFound();
        db.Modules.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }
}