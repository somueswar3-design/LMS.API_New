using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Services;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

// ═══════════════════════════════════════════════════════════════
//  ENROLLMENTS
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/enrollments"), Authorize]
public class EnrollmentsController(LmsDbContext db) : ControllerBase
{
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var list = await db.Enrollments
            .Include(e => e.Course).ThenInclude(c => c.Instructor)
            .Include(e => e.User)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
        return Ok(list.Select(MapEnrollment));
    }

    [HttpGet("course/{courseId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> GetByCourse(int courseId, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var q = db.Enrollments.Include(e => e.User).Include(e => e.Course).Where(e => e.CourseId == courseId);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(e => e.EnrolledAt).Skip((page - 1) * size).Take(size).ToListAsync();
        return Ok(new PagedResult<EnrollmentDto>(items.Select(MapEnrollment).ToList(), total, page, size, (int)Math.Ceiling(total / (double)size)));
    }

    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest req)
    {
        if (await db.Enrollments.AnyAsync(e => e.UserId == req.UserId && e.CourseId == req.CourseId))
            return BadRequest(new { message = "Already enrolled" });

        var course = await db.Courses.FindAsync(req.CourseId);
        if (course is null) return NotFound();

        var enrollment = new Enrollment { UserId = req.UserId, CourseId = req.CourseId };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        return Ok(new { enrollment.Id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Unenroll(int id)
    {
        var e = await db.Enrollments.FindAsync(id);
        if (e is null) return NotFound();
        e.Status = EnrollmentStatus.Cancelled;
        await db.SaveChangesAsync();
        return NoContent();
    }

    static EnrollmentDto MapEnrollment(Enrollment e) => new(
        e.Id, e.UserId, $"{e.User.FirstName} {e.User.LastName}",
        e.CourseId, e.Course.Title, e.EnrolledAt, e.CompletedAt,
        e.Status.ToString(), e.ProgressPercent, e.TotalWatchSeconds
    );
}

// ═══════════════════════════════════════════════════════════════
//  EXAMS
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/exams"), Authorize]
public class ExamsController(LmsDbContext db, IEmailService emailSvc) : ControllerBase
{
    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var exams = await db.Exams
            .Include(e => e.Course)
            .Include(e => e.Questions.OrderBy(q => q.DisplayOrder))
                .ThenInclude(q => q.Options.OrderBy(o => o.DisplayOrder))
            .Where(e => e.CourseId == courseId)
            .ToListAsync();
        return Ok(exams.Select(MapExam));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var exam = await db.Exams
            .Include(e => e.Course)
            .Include(e => e.Questions.OrderBy(q => q.DisplayOrder))
                .ThenInclude(q => q.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(e => e.Id == id);
        return exam is null ? NotFound() : Ok(MapExam(exam));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest req)
    {
        var exam = new Exam
        {
            Title = req.Title,
            Instructions = req.Instructions,
            CourseId = req.CourseId,
            TimeLimitMins = req.TimeLimitMins,
            PassMarkPercent = req.PassMarkPercent,
            MaxAttempts = req.MaxAttempts,
            Randomize = req.Randomize
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();
        return Ok(new { exam.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExamRequest req)
    {
        var exam = await db.Exams.FindAsync(id);
        if (exam is null) return NotFound();
        if (req.Title is not null) exam.Title = req.Title;
        if (req.Instructions is not null) exam.Instructions = req.Instructions;
        if (req.TimeLimitMins is not null) exam.TimeLimitMins = req.TimeLimitMins.Value;
        if (req.PassMarkPercent is not null) exam.PassMarkPercent = req.PassMarkPercent.Value;
        if (req.MaxAttempts is not null) exam.MaxAttempts = req.MaxAttempts.Value;
        if (req.IsPublished is not null) exam.IsPublished = req.IsPublished.Value;
        if (req.Randomize is not null) exam.Randomize = req.Randomize.Value;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Questions
    [HttpPost("{examId}/questions")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> AddQuestion(int examId, [FromBody] CreateQuestionRequest req)
    {
        var q = new Question
        {
            ExamId = examId,
            Text = req.Text,
            Type = Enum.Parse<QuestionType>(req.Type),
            Marks = req.Marks,
            Explanation = req.Explanation,
            DisplayOrder = req.DisplayOrder
        };
        db.Questions.Add(q);
        await db.SaveChangesAsync();

        if (req.Options != null)
        {
            foreach (var opt in req.Options)
                db.QuestionOptions.Add(new QuestionOption { QuestionId = q.Id, Text = opt.Text, IsCorrect = opt.IsCorrect, DisplayOrder = opt.DisplayOrder });
            await db.SaveChangesAsync();
        }
        return Ok(new { q.Id });
    }

    // Attempts
    [HttpPost("start")]
    public async Task<IActionResult> StartAttempt([FromBody] StartAttemptRequest req)
    {
        var prevAttempts = await db.ExamAttempts.CountAsync(a => a.ExamId == req.ExamId && a.UserId == req.UserId);
        var exam = await db.Exams.FindAsync(req.ExamId);
        if (exam is null) return NotFound();
        if (prevAttempts >= exam.MaxAttempts)
            return BadRequest(new { message = "Maximum attempts reached" });

        var attempt = new ExamAttempt { ExamId = req.ExamId, UserId = req.UserId };
        db.ExamAttempts.Add(attempt);
        await db.SaveChangesAsync();
        return Ok(new { attempt.Id, attempt.StartedAt, exam.TimeLimitMins });
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitAttemptRequest req)
    {
        var attempt = await db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == req.AttemptId);
        if (attempt is null) return NotFound();
        if (attempt.Status != AttemptStatus.InProgress)
            return BadRequest(new { message = "Attempt already submitted" });

        int totalMarks = attempt.Exam.Questions.Sum(q => q.Marks);
        int earned = 0;

        foreach (var sa in req.Answers)
        {
            var q = attempt.Exam.Questions.FirstOrDefault(q => q.Id == sa.QuestionId);
            if (q is null) continue;
            bool correct = false;
            if (q.Type is QuestionType.ShortAnswer)
                correct = true; // manual grading
            else if (sa.SelectedOptionIds != null)
            {
                var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                correct = correctIds.SetEquals(sa.SelectedOptionIds);
            }
            var answer = new Answer
            {
                ExamAttemptId = attempt.Id,
                QuestionId = sa.QuestionId,
                TextAnswer = sa.TextAnswer,
                IsCorrect = correct,
                MarksAwarded = correct ? q.Marks : 0
            };
            db.Answers.Add(answer);
            if (correct) earned += q.Marks;
        }

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.TotalMarks = totalMarks;
        attempt.Marks = earned;
        attempt.Score = totalMarks > 0 ? (int)(earned * 100.0 / totalMarks) : 0;
        attempt.Passed = attempt.Score >= attempt.Exam.PassMarkPercent;
        attempt.Status = AttemptStatus.Graded;

        // Auto-issue certificate if passed - record total watch time
        if (attempt.Passed && !await db.Certificates.AnyAsync(c => c.UserId == attempt.UserId && c.CourseId == attempt.Exam.CourseId))
        {
            var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.UserId == attempt.UserId && e.CourseId == attempt.Exam.CourseId);
            db.Certificates.Add(new Certificate
            {
                UserId = attempt.UserId,
                CourseId = attempt.Exam.CourseId,
                ExamAttemptId = attempt.Id,
                CertificateNumber = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                TotalWatchMinutes = enrollment != null ? enrollment.TotalWatchSeconds / 60 : 0
            });
        }

        await db.SaveChangesAsync();

        // Send exam result email
        var examUser = await db.Users.FindAsync(attempt.UserId);
        var examOrg = examUser is not null ? (await db.Organizations.FindAsync(examUser.OrganizationId))?.Name ?? "" : "";
        if (examUser is not null)
            _ = emailSvc.SendExamResultAsync(examUser.Email, examUser.FirstName, attempt.Exam.Title, attempt.Score ?? 0, attempt.Passed, examOrg);

        // Send certificate email if passed
        if (attempt.Passed && examUser is not null)
        {
            var cert = await db.Certificates.FirstOrDefaultAsync(c => c.UserId == attempt.UserId && c.CourseId == attempt.Exam.CourseId);
            if (cert is not null)
                _ = emailSvc.SendCertificateIssuedAsync(examUser.Email, examUser.FirstName, attempt.Exam.Course.Title, cert.CertificateNumber, examOrg);
        }

        return Ok(new { attempt.Score, attempt.Marks, attempt.TotalMarks, attempt.Passed, attempt.Status });
    }

    [HttpGet("attempts/user/{userId}")]
    public async Task<IActionResult> GetAttempts(int userId)
    {
        var list = await db.ExamAttempts
            .Include(a => a.Exam).ThenInclude(e => e.Course)
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
        return Ok(list.Select(a => new ExamAttemptDto(
            a.Id, a.UserId, $"{a.User.FirstName} {a.User.LastName}",
            a.ExamId, a.Exam.Title, a.StartedAt, a.SubmittedAt,
            a.Score, a.Marks, a.TotalMarks, a.Passed, a.Status.ToString()
        )));
    }

    static ExamDto MapExam(Exam e) => new(
        e.Id, e.Title, e.Instructions, e.TimeLimitMins, e.PassMarkPercent,
        e.MaxAttempts, e.IsPublished, e.Randomize, e.CourseId, e.Course.Title,
        e.Questions.Select(q => new QuestionDto(
            q.Id, q.Text, q.Type.ToString(), q.Marks, q.Explanation, q.DisplayOrder,
            q.Options.Select(o => new OptionDto(o.Id, o.Text, o.IsCorrect, o.DisplayOrder)).ToList()
        )).ToList()
    );
}

// ═══════════════════════════════════════════════════════════════
//  CERTIFICATES
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/certificates"), Authorize]
public class CertificatesController(LmsDbContext db) : ControllerBase
{
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var list = await db.Certificates
            .Include(c => c.User).ThenInclude(u => u.Organization)
            .Include(c => c.Course).ThenInclude(c => c.Organization)
            .Where(c => c.UserId == userId)
            .ToListAsync();
        return Ok(list.Select(MapCert));
    }

    [HttpGet("{number}/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string number)
    {
        var cert = await db.Certificates.Include(c => c.User).Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.CertificateNumber == number);
        return cert is null ? NotFound(new { valid = false }) : Ok(new { valid = true, certificate = MapCert(cert) });
    }

    // Track download time
    [HttpPost("{id}/download")]
    public async Task<IActionResult> RecordDownload(int id)
    {
        var cert = await db.Certificates.FindAsync(id);
        if (cert is null) return NotFound();
        if (cert.DownloadedAt is null) cert.DownloadedAt = DateTime.UtcNow;
        cert.DownloadCount++;
        await db.SaveChangesAsync();
        return Ok(new { cert.DownloadedAt, cert.DownloadCount });
    }

    static CertificateDto MapCert(Certificate c)
    {
        var org = c.Course.Organization ?? c.User.Organization;
        return new CertificateDto(
            c.Id, c.CertificateNumber, c.IssuedAt, c.PdfUrl,
            c.UserId, $"{c.User.FirstName} {c.User.LastName}", c.User.Email,
            c.CourseId, c.Course.Title,
            c.Course.Level.ToString(), c.Course.Language,
            c.TotalWatchMinutes,
            org?.Name ?? "EKSHA TECHNOLOGIES",
            org?.LogoUrl,
            org?.SignatureUrl,
            org?.AuthorizedBy,
            org?.AuthorizedTitle
        );
    }
}