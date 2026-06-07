using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/attendance"), Authorize]
public class AttendanceController(LmsDbContext db) : ControllerBase
{
    // ─── Mark attendance for a date (batch) ───────────────────
    [HttpPost("mark")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest req)
    {
        var date = req.Date.Date;

        foreach (var entry in req.Entries)
        {
            var existing = await db.Attendances.FirstOrDefaultAsync(a =>
                a.CourseId == req.CourseId && a.StudentId == entry.StudentId && a.Date == date);

            if (existing is not null)
            {
                if (Enum.TryParse<AttendanceStatus>(entry.Status, out var st)) existing.Status = st;
                existing.Remarks   = entry.Remarks;
                existing.MarkedById = req.MarkedById;
                existing.MarkedAt  = DateTime.UtcNow;
            }
            else
            {
                if (!Enum.TryParse<AttendanceStatus>(entry.Status, out var st)) st = AttendanceStatus.Absent;
                db.Attendances.Add(new Attendance
                {
                    CourseId   = req.CourseId,
                    StudentId  = entry.StudentId,
                    Date       = date,
                    Status     = st,
                    Remarks    = entry.Remarks,
                    MarkedById = req.MarkedById
                });
            }
        }
        await db.SaveChangesAsync();
        return Ok(new { marked = req.Entries.Count });
    }

    // ─── Get attendance for a course on a date ─────────────────
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(int courseId, [FromQuery] DateTime? date)
    {
        var q = db.Attendances
            .Include(a => a.Student)
            .Include(a => a.Course)
            .Where(a => a.CourseId == courseId);

        if (date.HasValue) q = q.Where(a => a.Date == date.Value.Date);

        var list = await q.OrderBy(a => a.Date).ThenBy(a => a.Student.FirstName).ToListAsync();
        return Ok(list.Select(MapAttendance));
    }

    // ─── Get student attendance ────────────────────────────────
    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetByStudent(int studentId, [FromQuery] int? courseId)
    {
        var q = db.Attendances
            .Include(a => a.Course)
            .Include(a => a.Student)
            .Where(a => a.StudentId == studentId);

        if (courseId.HasValue) q = q.Where(a => a.CourseId == courseId.Value);

        var list = await q.OrderByDescending(a => a.Date).ToListAsync();
        return Ok(list.Select(MapAttendance));
    }

    // ─── Summary: attendance % per student ────────────────────
    [HttpGet("course/{courseId}/summary")]
    public async Task<IActionResult> GetSummary(int courseId)
    {
        var enrolledStudents = await db.Enrollments
            .Include(e => e.User)
            .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.User).ToListAsync();

        var allAttendance = await db.Attendances
            .Where(a => a.CourseId == courseId)
            .ToListAsync();

        var totalDates = allAttendance.Select(a => a.Date.Date).Distinct().Count();

        var summaries = enrolledStudents.Select(s => {
            var records = allAttendance.Where(a => a.StudentId == s.Id).ToList();
            var present = records.Count(a => a.Status == AttendanceStatus.Present);
            var absent  = records.Count(a => a.Status == AttendanceStatus.Absent);
            var late    = records.Count(a => a.Status == AttendanceStatus.Late);
            var pct     = totalDates > 0 ? Math.Round((present + late * 0.5) / totalDates * 100, 1) : 0;
            return new AttendanceSummaryDto(s.Id, $"{s.FirstName} {s.LastName}", totalDates, present, absent, late, pct);
        }).OrderByDescending(s => s.Percentage).ToList();

        return Ok(summaries);
    }

    static AttendanceDto MapAttendance(Attendance a) => new(
        a.Id, a.CourseId, a.Course.Title, a.StudentId,
        $"{a.Student.FirstName} {a.Student.LastName}",
        a.Date, a.Status.ToString(), a.Remarks
    );
}
