using LMS.API.Data;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

public record CreateBatchRequest(
    string BatchName, string? Description, DateTime StartDate,
    int DurationDays, decimal TotalFee, string? Notes,
    int? CourseId
);
public record AddBatchStudentRequest(
    int? UserId,
    string? GuestName, string? GuestEmail, string? GuestMobile,
    decimal TotalFee, decimal PaidAmount, string? Notes
);
public record UpdatePaymentRequest(decimal PaidAmount, string? Notes);

[ApiController, Route("api/batches"), Authorize]
public class TrainingBatchController(LmsDbContext db) : ControllerBase
{
    // ── List batches for org ──────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> GetAll([FromQuery] int? orgId)
    {
        var q = db.TrainingBatches
            .Include(b => b.Course)
            .Include(b => b.Students).ThenInclude(s => s.User)
            .Include(b => b.CreatedBy)
            .AsQueryable();

        if (orgId.HasValue) q = q.Where(b => b.OrganizationId == orgId.Value);

        var batches = await q.OrderByDescending(b => b.StartDate).ToListAsync();

        return Ok(batches.Select(b => new {
            b.Id,
            b.BatchName,
            b.Description,
            b.StartDate,
            EndDate = b.EndDate,
            b.DurationDays,
            b.Status,
            b.TotalFee,
            b.Notes,
            b.CreatedAt,
            b.CourseId,
            courseTitle = b.Course?.Title,
            createdBy = $"{b.CreatedBy.FirstName} {b.CreatedBy.LastName}",
            studentCount = b.Students.Count,
            students = b.Students.Select(s => new {
                s.Id,
                s.UserId,
                s.BatchId,
                name = s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : s.GuestName,
                email = s.User?.Email ?? s.GuestEmail,
                mobile = s.GuestMobile ?? s.User?.PhoneNumber,
                s.TotalFee,
                s.PaidAmount,
                pendingAmount = s.TotalFee - s.PaidAmount,
                s.PaymentStatus,
                s.Status,
                s.JoinedAt,
                s.Notes,
                isGuest = s.UserId == null,
            }),
        }));
    }

    // ── Get single batch ──────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var b = await db.TrainingBatches
            .Include(b => b.Course)
            .Include(b => b.Students).ThenInclude(s => s.User)
            .Include(b => b.CreatedBy)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (b is null) return NotFound();
        return Ok(new
        {
            b.Id,
            b.BatchName,
            b.Description,
            b.StartDate,
            EndDate = b.EndDate,
            b.DurationDays,
            b.Status,
            b.TotalFee,
            b.Notes,
            b.CreatedAt,
            b.CourseId,
            courseTitle = b.Course?.Title,
            createdBy = $"{b.CreatedBy.FirstName} {b.CreatedBy.LastName}",
            students = b.Students.Select(s => new {
                s.Id,
                s.UserId,
                s.BatchId,
                name = s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : s.GuestName,
                email = s.User?.Email ?? s.GuestEmail,
                mobile = s.GuestMobile,
                s.TotalFee,
                s.PaidAmount,
                pendingAmount = s.TotalFee - s.PaidAmount,
                s.PaymentStatus,
                s.Status,
                s.JoinedAt,
                s.Notes,
                isGuest = s.UserId == null,
            }),
        });
    }

    // ── Create batch ──────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateBatchRequest req)
    {
        var creatorId = int.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value ?? "0");
        var creator = await db.Users.FindAsync(creatorId);
        if (creator is null) return Unauthorized();

        var batch = new TrainingBatch
        {
            BatchName = req.BatchName,
            Description = req.Description,
            StartDate = req.StartDate,
            DurationDays = req.DurationDays,
            TotalFee = req.TotalFee,
            Notes = req.Notes,
            CourseId = req.CourseId,
            OrganizationId = creator.OrganizationId,
            CreatedById = creatorId,
            Status = req.StartDate <= DateTime.UtcNow ? BatchStatus.Active : BatchStatus.Upcoming,
        };
        db.TrainingBatches.Add(batch);
        await db.SaveChangesAsync();
        return Ok(new { batch.Id });
    }

    // ── Update batch ──────────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateBatchRequest req)
    {
        var b = await db.TrainingBatches.FindAsync(id);
        if (b is null) return NotFound();
        b.BatchName = req.BatchName;
        b.Description = req.Description;
        b.StartDate = req.StartDate;
        b.DurationDays = req.DurationDays;
        b.TotalFee = req.TotalFee;
        b.Notes = req.Notes;
        b.CourseId = req.CourseId;
        b.Status = req.StartDate <= DateTime.UtcNow
                         ? (req.StartDate.AddDays(req.DurationDays) < DateTime.UtcNow ? BatchStatus.Completed : BatchStatus.Active)
                         : BatchStatus.Upcoming;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Delete batch ──────────────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await db.TrainingBatches.FindAsync(id);
        if (b is null) return NotFound();
        b.Status = BatchStatus.Cancelled;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Add student to batch ──────────────────────────────────
    [HttpPost("{id}/students")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> AddStudent(int id, [FromBody] AddBatchStudentRequest req)
    {
        var batch = await db.TrainingBatches.FindAsync(id);
        if (batch is null) return NotFound();

        // Validate: need either userId or guest info
        if (req.UserId == null && string.IsNullOrWhiteSpace(req.GuestName))
            return BadRequest(new { message = "Provide either a user or guest name" });

        // Check duplicate
        var exists = await db.BatchStudents.AnyAsync(s =>
            s.BatchId == id && (
                (req.UserId != null && s.UserId == req.UserId) ||
                (req.GuestEmail != null && s.GuestEmail == req.GuestEmail)
            ));
        if (exists) return BadRequest(new { message = "Student already in this batch" });

        var paid = req.PaidAmount;
        var total = req.TotalFee;
        var ps = total == 0 ? PaymentStatus.Free
               : paid >= total ? PaymentStatus.FullyPaid
               : paid > 0 ? PaymentStatus.PartiallyPaid
               : PaymentStatus.Pending;

        var student = new BatchStudent
        {
            BatchId = id,
            UserId = req.UserId,
            GuestName = req.GuestName,
            GuestEmail = req.GuestEmail,
            GuestMobile = req.GuestMobile,
            TotalFee = total,
            PaidAmount = paid,
            PaymentStatus = ps,
            Notes = req.Notes,
        };
        db.BatchStudents.Add(student);
        await db.SaveChangesAsync();
        return Ok(new { student.Id });
    }

    // ── Update student payment ────────────────────────────────
    [HttpPut("{id}/students/{studentId}/payment")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> UpdatePayment(int id, int studentId, [FromBody] UpdatePaymentRequest req)
    {
        var s = await db.BatchStudents.FindAsync(studentId);
        if (s is null || s.BatchId != id) return NotFound();
        s.PaidAmount = req.PaidAmount;
        s.Notes = req.Notes ?? s.Notes;
        s.PaymentStatus = s.TotalFee == 0 ? PaymentStatus.Free
                        : req.PaidAmount >= s.TotalFee ? PaymentStatus.FullyPaid
                        : req.PaidAmount > 0 ? PaymentStatus.PartiallyPaid
                        : PaymentStatus.Pending;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Remove student ────────────────────────────────────────
    [HttpDelete("{id}/students/{studentId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> RemoveStudent(int id, int studentId)
    {
        var s = await db.BatchStudents.FindAsync(studentId);
        if (s is null || s.BatchId != id) return NotFound();
        db.BatchStudents.Remove(s);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Active batches for dashboard ──────────────────────────
    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] int orgId)
    {
        var now = DateTime.UtcNow;
        var batches = await db.TrainingBatches
            .Include(b => b.Course)
            .Include(b => b.Students)
            .Where(b => b.OrganizationId == orgId && b.Status == BatchStatus.Active)
            .OrderBy(b => b.StartDate)
            .Take(5)
            .ToListAsync();

        return Ok(batches.Select(b => new {
            b.Id,
            b.BatchName,
            b.StartDate,
            EndDate = b.EndDate,
            b.DurationDays,
            daysLeft = Math.Max(0, (int)(b.EndDate - now).TotalDays),
            courseTitle = b.Course?.Title,
            studentCount = b.Students.Count,
            b.Status,
        }));
    }
}