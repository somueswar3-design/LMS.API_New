using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using LMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/assignments"), Authorize]
public class AssignmentsController(LmsDbContext db, IEmailService email) : ControllerBase
{
    // ─── Trainer: list assignments for a course ────────────────
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var list = await db.Assignments
            .Include(a => a.CreatedBy)
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Where(a => a.CourseId == courseId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(a => MapAssignment(a, null)));
    }

    // ─── Student: my assignments ───────────────────────────────
    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetForStudent(int studentId)
    {
        var enrolledCourseIds = await db.Enrollments
            .Where(e => e.UserId == studentId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.CourseId).ToListAsync();

        var list = await db.Assignments
            .Include(a => a.CreatedBy)
            .Include(a => a.Course)
            .Include(a => a.Submissions.Where(s => s.StudentId == studentId))
            .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.Status == AssignmentStatus.Published)
            .OrderBy(a => a.DueDate)
            .ToListAsync();

        return Ok(list.Select(a => {
            var sub = a.Submissions.FirstOrDefault();
            return MapAssignment(a, sub);
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var a = await db.Assignments.Include(a => a.CreatedBy).Include(a => a.Course)
            .Include(a => a.Submissions).ThenInclude(s => s.Student)
            .FirstOrDefaultAsync(a => a.Id == id);
        return a is null ? NotFound() : Ok(MapAssignment(a, null));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest req)
    {
        var a = new Assignment
        {
            Title       = req.Title, Description = req.Description,
            AttachmentUrl = req.AttachmentUrl, MaxMarks = req.MaxMarks,
            DueDate     = req.DueDate, CourseId = req.CourseId,
            CreatedById = req.CreatedById, Status = AssignmentStatus.Published
        };
        db.Assignments.Add(a);
        await db.SaveChangesAsync();

        // Notify enrolled students
        var students = await db.Enrollments
            .Include(e => e.User)
            .Where(e => e.CourseId == req.CourseId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.User).ToListAsync();
        var course = await db.Courses.FindAsync(req.CourseId);
        foreach (var s in students)
            _ = email.SendAssignmentNotificationAsync(s.Email, s.FirstName, req.Title, req.DueDate, course?.Title ?? "");

        return Ok(new { a.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAssignmentRequest req)
    {
        var a = await db.Assignments.FindAsync(id);
        if (a is null) return NotFound();
        a.Title = req.Title; a.Description = req.Description;
        a.MaxMarks = req.MaxMarks; a.DueDate = req.DueDate;
        a.AttachmentUrl = req.AttachmentUrl;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        var a = await db.Assignments.FindAsync(id);
        if (a is null) return NotFound();
        db.Assignments.Remove(a);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Student: submit assignment ────────────────────────────
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitAssignmentRequest req)
    {
        var existing = await db.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.AssignmentId == req.AssignmentId && s.StudentId == req.StudentId);
        if (existing is not null) return BadRequest(new { message = "Already submitted" });

        var assignment = await db.Assignments.FindAsync(req.AssignmentId);
        var isLate = assignment is not null && DateTime.UtcNow > assignment.DueDate;

        var sub = new AssignmentSubmission
        {
            AssignmentId   = req.AssignmentId,
            StudentId      = req.StudentId,
            SubmissionText = req.SubmissionText,
            FileUrl        = req.FileUrl,
            SubmittedAt    = DateTime.UtcNow,
            Status         = isLate ? SubmissionStatus.Late : SubmissionStatus.Submitted
        };
        db.AssignmentSubmissions.Add(sub);
        await db.SaveChangesAsync();
        return Ok(new { sub.Id, sub.Status });
    }

    // ─── Trainer: grade submission ─────────────────────────────
    [HttpPost("grade")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Grade([FromBody] GradeSubmissionRequest req)
    {
        var sub = await db.AssignmentSubmissions
            .Include(s => s.Student)
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(s => s.Id == req.SubmissionId);
        if (sub is null) return NotFound();

        sub.MarksObtained = req.MarksObtained;
        sub.Feedback      = req.Feedback;
        sub.GradedById    = req.GradedById;
        sub.GradedAt      = DateTime.UtcNow;
        sub.Status        = SubmissionStatus.Graded;
        await db.SaveChangesAsync();

        // Notify student
        _ = email.SendGradeNotificationAsync(
            sub.Student.Email, sub.Student.FirstName,
            sub.Assignment.Title, req.MarksObtained,
            sub.Assignment.MaxMarks, req.Feedback ?? "",
            sub.Assignment.Course.Title
        );
        return Ok(new { sub.MarksObtained, sub.Feedback });
    }

    // ─── Trainer: all submissions for an assignment ────────────
    [HttpGet("{id}/submissions")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> GetSubmissions(int id)
    {
        var subs = await db.AssignmentSubmissions
            .Include(s => s.Student)
            .Include(s => s.Assignment)
            .Where(s => s.AssignmentId == id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
        return Ok(subs.Select(MapSub));
    }

    static AssignmentDto MapAssignment(Assignment a, AssignmentSubmission? sub) => new(
        a.Id, a.Title, a.Description, a.AttachmentUrl, a.MaxMarks, a.DueDate,
        a.Status.ToString(), a.CourseId, a.Course.Title,
        a.CreatedById, $"{a.CreatedBy.FirstName} {a.CreatedBy.LastName}",
        a.CreatedAt, a.Submissions.Count,
        sub?.MarksObtained, sub?.Status.ToString()
    );

    static SubmissionDto MapSub(AssignmentSubmission s) => new(
        s.Id, s.AssignmentId, s.Assignment.Title,
        s.StudentId, $"{s.Student.FirstName} {s.Student.LastName}",
        s.SubmissionText, s.FileUrl, s.MarksObtained, s.Feedback,
        s.Status.ToString(), s.SubmittedAt, s.GradedAt
    );
}
