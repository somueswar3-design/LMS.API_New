using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using LMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/interviews"), Authorize]
public class InterviewsController(LmsDbContext db, IEmailService email) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> GetAll([FromQuery] int? orgId, [FromQuery] int? studentId, [FromQuery] string? status)
    {
        var q = db.InterviewSchedules
            .Include(i => i.Student)
            .Include(i => i.Course)
            .AsQueryable();

        if (orgId.HasValue)    q = q.Where(i => i.OrganizationId == orgId.Value);
        if (studentId.HasValue) q = q.Where(i => i.StudentId == studentId.Value);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InterviewStatus>(status, out var st))
            q = q.Where(i => i.Status == st);

        var list = await q.OrderBy(i => i.ScheduledAt).ToListAsync();
        return Ok(list.Select(MapInterview));
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudent(int studentId)
    {
        var list = await db.InterviewSchedules
            .Include(i => i.Course)
            .Include(i => i.Student)
            .Where(i => i.StudentId == studentId)
            .OrderByDescending(i => i.ScheduledAt)
            .ToListAsync();
        return Ok(list.Select(MapInterview));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var i = await db.InterviewSchedules
            .Include(i => i.Student).Include(i => i.Course)
            .FirstOrDefaultAsync(i => i.Id == id);
        return i is null ? NotFound() : Ok(MapInterview(i));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateInterviewRequest req)
    {
        var interview = new InterviewSchedule
        {
            Title            = req.Title,
            Description      = req.Description,
            ScheduledAt      = req.ScheduledAt,
            DurationMinutes  = req.DurationMinutes,
            Platform         = req.Platform,
            MeetingLink      = req.MeetingLink,
            InterviewerName  = req.InterviewerName,
            InterviewerEmail = req.InterviewerEmail,
            Notes            = req.Notes,
            StudentId        = req.StudentId,
            CourseId         = req.CourseId,
            OrganizationId   = req.OrganizationId
        };
        db.InterviewSchedules.Add(interview);
        await db.SaveChangesAsync();

        // Send notification email to student
        var student = await db.Users.FindAsync(req.StudentId);
        if (student is not null)
        {
            _ = email.SendInterviewNotificationAsync(
                student.Email, student.FirstName,
                req.Title, req.ScheduledAt, req.DurationMinutes,
                req.Platform ?? "Online", req.MeetingLink ?? "",
                req.InterviewerName ?? "Interviewer", req.Notes ?? ""
            );
            interview.EmailSent = true;
            await db.SaveChangesAsync();
        }

        return Ok(new { interview.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInterviewRequest req)
    {
        var interview = await db.InterviewSchedules.Include(i => i.Student).FirstOrDefaultAsync(i => i.Id == id);
        if (interview is null) return NotFound();

        if (req.Status is not null && Enum.TryParse<InterviewStatus>(req.Status, out var st)) interview.Status = st;
        if (req.Notes is not null)       interview.Notes       = req.Notes;
        if (req.Feedback is not null)    interview.Feedback    = req.Feedback;
        if (req.ScheduledAt.HasValue)    interview.ScheduledAt = req.ScheduledAt.Value;
        if (req.MeetingLink is not null) interview.MeetingLink = req.MeetingLink;

        await db.SaveChangesAsync();

        // Send reschedule email if date changed
        if (req.ScheduledAt.HasValue && interview.Student is not null)
        {
            _ = email.SendInterviewNotificationAsync(
                interview.Student.Email, interview.Student.FirstName,
                $"[Rescheduled] {interview.Title}", interview.ScheduledAt,
                interview.DurationMinutes, interview.Platform ?? "Online",
                interview.MeetingLink ?? "", interview.InterviewerName ?? "",
                "Your interview has been rescheduled."
            );
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        var i = await db.InterviewSchedules.FindAsync(id);
        if (i is null) return NotFound();
        i.Status = InterviewStatus.Cancelled;
        await db.SaveChangesAsync();
        return NoContent();
    }

    static InterviewScheduleDto MapInterview(InterviewSchedule i) => new(
        i.Id, i.Title, i.Description, i.ScheduledAt, i.DurationMinutes,
        i.Platform, i.MeetingLink, i.InterviewerName, i.InterviewerEmail,
        i.Status.ToString(), i.Notes, i.Feedback,
        i.StudentId, $"{i.Student.FirstName} {i.Student.LastName}",
        i.CourseId, i.Course?.Title, i.EmailSent, i.CreatedAt
    );
}
