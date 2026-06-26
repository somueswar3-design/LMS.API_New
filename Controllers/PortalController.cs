using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System.Composition;
using System.Drawing;
using System.Reflection;

using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

/// <summary>
/// All endpoints are public (no auth). Serves the org-branded homepage.
/// </summary>
[ApiController, Route("api/portal")]
public class PortalController(LmsDbContext db) : ControllerBase
{
    // ─── Resolve org from the caller's Origin / explicit URL param ──────
    // GET /api/portal/org?url=http://localhost:5174
    [HttpGet("org")]
    public async Task<IActionResult> GetOrgByUrl([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { message = "url parameter is required" });

        // ── Local dev bypass ─────────────────────────────────────────────────
        // If running on localhost (any port), skip URL matching and return the
        // first active organization so developers don't need to update PortalUrl.
        var host = url.TrimEnd('/').ToLowerInvariant();
        var isLocalhost = host.StartsWith("http://localhost") ||
                          host.StartsWith("http://127.0.0.1") ||
                          host.StartsWith("https://localhost") ||
                          host.StartsWith("https://127.0.0.1");

        if (isLocalhost)
        {
            // Return the FULL organization (not a stripped theme DTO) so the
            // homepage feature flags and content fields are available too.
            var devOrg = await db.Organizations
                .Where(o => o.IsActive)
                .FirstOrDefaultAsync();

            if (devOrg is null)
                return NotFound(new { authorized = false, message = "No active organization found." });

            return Ok(new { authorized = true, organization = devOrg });
        }

        // ── Production: match URL against PortalUrl in DB ────────────────────
        var normalised = host;

        var org = await db.Organizations
            .Where(o => o.IsActive &&
                        o.PortalUrl != null &&
                        o.PortalUrl.ToLower().TrimEnd('/') == normalised)
            .FirstOrDefaultAsync();

        if (org is null)
            return NotFound(new { authorized = false, message = "No organization is registered for this URL." });

        return Ok(new { authorized = true, organization = org });
    }

    // ─── Categories with course counts ─────────────────────────────────
    // GET /api/portal/{orgId}/categories
    [HttpGet("{orgId:int}/categories")]
    public async Task<IActionResult> GetCategories(int orgId)
    {
        var cats = await db.Categories
            .Where(c => c.OrganizationId == orgId && c.IsActive && c.ParentId == null)
            .Include(c => c.Children.Where(ch => ch.IsActive))
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new PublicCategoryDto(
                c.Id, c.Name, c.Description, c.IconEmoji ?? c.IconUrl,
                c.Courses.Count(co => co.Status == CourseStatus.Published),
                c.Children.Select(ch => new PublicCategoryDto(
                    ch.Id, ch.Name, ch.Description, ch.IconEmoji ?? ch.IconUrl,
                    ch.Courses.Count(co => co.Status == CourseStatus.Published),
                    new List<PublicCategoryDto>()
                )).ToList()
            ))
            .ToListAsync();

        return Ok(cats);
    }

    // ─── Published courses (with optional category filter) ─────────────
    // GET /api/portal/{orgId}/courses?categoryId=&search=&page=
    [HttpGet("{orgId:int}/courses")]
    public async Task<IActionResult> GetCourses(
        int orgId,
        [FromQuery] int? categoryId,
        [FromQuery] string? level,
        [FromQuery] string? search,
        [FromQuery] bool? free,
        [FromQuery] int page = 1,
        [FromQuery] int size = 12)
    {
        var q = db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Enrollments)
            .Include(c => c.Ratings)
            .Where(c => c.OrganizationId == orgId && c.Status == CourseStatus.Published)
            .AsQueryable();

        if (categoryId.HasValue) q = q.Where(c => c.CategoryId == categoryId || c.Category.ParentId == categoryId);
        if (!string.IsNullOrEmpty(level) && Enum.TryParse<CourseLevel>(level, out var lv)) q = q.Where(c => c.Level == lv);
        if (!string.IsNullOrEmpty(search)) q = q.Where(c => c.Title.Contains(search) || (c.Description != null && c.Description.Contains(search)));
        if (free.HasValue) q = q.Where(c => c.IsFree == free.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(c => c.Enrollments.Count)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync();

        return Ok(new PagedResult<PublicCourseDto>(
            items.Select(c => MapCourse(c)).ToList(),
            total, page, size,
            (int)Math.Ceiling(total / (double)size)
        ));
    }

    // ─── Single course detail (public) ─────────────────────────────────
    // GET /api/portal/{orgId}/courses/{courseId}
    [HttpGet("{orgId:int}/courses/{courseId:int}")]
    public async Task<IActionResult> GetCourse(int orgId, int courseId)
    {
        var c = await db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Enrollments)
            .Include(c => c.Ratings)
            .Include(c => c.Modules.OrderBy(m => m.DisplayOrder))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Id == courseId && c.OrganizationId == orgId && c.Status == CourseStatus.Published);

        if (c is null) return NotFound();

        return Ok(MapCourseDetail(c));
    }

    // ─── Featured / top courses (homepage hero) ─────────────────────────
    // GET /api/portal/{orgId}/featured
    [HttpGet("{orgId:int}/featured")]
    public async Task<IActionResult> GetFeatured(int orgId)
    {
        var courses = await db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Enrollments)
            .Include(c => c.Ratings)
            .Where(c => c.OrganizationId == orgId && c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(6)
            .ToListAsync();

        return Ok(courses.Select(c => MapCourse(c)));
    }

    // ─── Stats for homepage ─────────────────────────────────────────────
    [HttpGet("{orgId:int}/stats")]
    public async Task<IActionResult> GetStats(int orgId)
    {
        return Ok(new
        {
            totalCourses = await db.Courses.CountAsync(c => c.OrganizationId == orgId && c.Status == CourseStatus.Published),
            totalStudents = await db.Users.CountAsync(u => u.OrganizationId == orgId && u.Role == UserRole.Student),
            totalInstructors = await db.Users.CountAsync(u => u.OrganizationId == orgId && u.Role == UserRole.Instructor),
            totalEnrollments = await db.Enrollments.CountAsync(e => e.Course.OrganizationId == orgId)
        });
    }

    // ─── Instructors (public) ──────────────────────────────────────────
    [HttpGet("{orgId:int}/instructors")]
    public async Task<IActionResult> GetInstructors(int orgId)
    {
        var instructors = await db.Users
            .Where(u => u.OrganizationId == orgId && u.Role == UserRole.Instructor && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.AvatarUrl,
                u.Bio,
                CourseCount = db.Courses.Count(c => c.InstructorId == u.Id && c.Status == CourseStatus.Published),
                StudentCount = db.Enrollments.Count(e => e.Course.InstructorId == u.Id),
            })
            .ToListAsync();

        return Ok(instructors);
    }

    // ── Public reviews — top-rated, with a written review, across the org's
    // published courses. Used for the homepage testimonials section so the
    // copy shown there is always real student feedback, never fabricated.
    [HttpGet("{orgId:int}/reviews")]
    public async Task<IActionResult> GetReviews(int orgId, [FromQuery] int limit = 9)
    {
        var reviews = await db.CourseRatings
            .Include(r => r.User)
            .Include(r => r.Course)
            .Where(r => r.Course.OrganizationId == orgId
                && r.Course.Status == CourseStatus.Published
                && r.Rating >= 4
                && r.Review != null && r.Review != "")
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Review,
                r.CreatedAt,
                StudentName = r.User.FirstName + " " + r.User.LastName,
                StudentAvatar = r.User.AvatarUrl,
                CourseTitle = r.Course.Title,
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // ─── Mappers ───────────────────────────────────────────────────────
    static PublicCourseDto MapCourse(Course c) => new(
        c.Id, c.Title, c.Description, c.ThumbnailUrl,
        c.Level.ToString(), c.Price, c.IsFree,
        c.DurationMinutes, c.Language,
        c.InstructorId,
        $"{c.Instructor.FirstName} {c.Instructor.LastName}",
        c.Instructor.AvatarUrl,
        c.CategoryId, c.Category.Name,
        c.Enrollments.Count,
        c.Ratings.Count > 0 ? c.Ratings.Average(r => r.Rating) : 0,
        c.Ratings.Count,
        c.Tags, c.CreatedAt, null
    );

    static PublicCourseDto MapCourseDetail(Course c) => new(
        c.Id, c.Title, c.Description, c.ThumbnailUrl,
        c.Level.ToString(), c.Price, c.IsFree,
        c.DurationMinutes, c.Language,
        c.InstructorId,
        $"{c.Instructor.FirstName} {c.Instructor.LastName}",
        c.Instructor.AvatarUrl,
        c.CategoryId, c.Category.Name,
        c.Enrollments.Count,
        c.Ratings.Count > 0 ? c.Ratings.Average(r => r.Rating) : 0,
        c.Ratings.Count,
        c.Tags, c.CreatedAt,
        c.Modules.OrderBy(m => m.DisplayOrder).Select(m => new PublicModuleDto(
            m.Id, m.Title, m.Description,
            m.Lessons.OrderBy(l => l.DisplayOrder).Select(l => new PublicLessonDto(
                l.Id, l.Title, l.Type.ToString(), l.DurationSecs, l.IsPreview
            )).ToList()
        )).ToList()
    );
}

// ─── Portal DTOs ──────────────────────────────────────────────
public record OrgThemeDto(
    int Id, string Name, string Slug,
    string? LogoUrl, string? BannerUrl, string? Tagline,
    string? PrimaryColor, string? SecondaryColor, string? AccentColor,
    string? ThemeFont, string? Website, string? PortalUrl
);

public record PublicCategoryDto(
    int Id, string Name, string? Description,
    string? Icon, int CourseCount, List<PublicCategoryDto> Children
);

public record PublicCourseDto(
    int Id, string Title, string? Description, string? ThumbnailUrl,
    string Level, decimal Price, bool IsFree, int DurationMinutes, string? Language,
    int? InstructorId, string? InstructorName, string? InstructorAvatar,
    int? CategoryId, string? CategoryName,
    int EnrollmentCount, double AverageRating, int RatingCount,
    string? Tags, DateTime CreatedAt,
    List<PublicModuleDto>? Modules
);

public record PublicModuleDto(int Id, string Title, string? Description, List<PublicLessonDto> Lessons);
public record PublicLessonDto(int Id, string Title, string Type, int DurationSecs, bool IsPreview);