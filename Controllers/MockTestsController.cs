using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/mocktests"), Authorize]
public class MockTestsController(LmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? orgId, [FromQuery] int? courseId, [FromQuery] string? status)
    {
        var q = db.MockTests.Include(m => m.CreatedBy).Include(m => m.Course).AsQueryable();
        if (orgId.HasValue)    q = q.Where(m => m.OrganizationId == orgId.Value);
        if (courseId.HasValue) q = q.Where(m => m.CourseId == courseId.Value);
        q = !string.IsNullOrEmpty(status) && Enum.TryParse<MockTestStatus>(status, out var st)
            ? q.Where(m => m.Status == st)
            : q.Where(m => m.Status == MockTestStatus.Published);
        var list = await q.Include(m => m.Attempts).OrderByDescending(m => m.CreatedAt).ToListAsync();
        return Ok(list.Select(m => MapTest(m, false)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var m = await db.MockTests
            .Include(m => m.CreatedBy).Include(m => m.Course)
            .Include(m => m.Questions.OrderBy(q => q.DisplayOrder))
                .ThenInclude(q => q.Options.OrderBy(o => o.DisplayOrder))
            .Include(m => m.Attempts)
            .FirstOrDefaultAsync(m => m.Id == id);
        return m is null ? NotFound() : Ok(MapTest(m, true));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateMockTestRequest req)
    {
        if (!Enum.TryParse<MockTestDifficulty>(req.Difficulty, out var diff)) diff = MockTestDifficulty.Mixed;
        var m = new MockTest
        {
            Title = req.Title, Description = req.Description, Topic = req.Topic,
            Difficulty = diff, TimeLimitMins = req.TimeLimitMins,
            TotalQuestions = req.TotalQuestions, PassMarkPercent = req.PassMarkPercent,
            RandomizeQuestions = req.RandomizeQuestions, ShowResultImmediately = req.ShowResultImmediately,
            MaxAttempts = req.MaxAttempts, Tags = req.Tags,
            OrganizationId = req.OrganizationId, CourseId = req.CourseId, CreatedById = req.CreatedById
        };
        db.MockTests.Add(m);
        await db.SaveChangesAsync();
        return Ok(new { m.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateMockTestRequest req)
    {
        var m = await db.MockTests.FindAsync(id);
        if (m is null) return NotFound();
        if (!Enum.TryParse<MockTestDifficulty>(req.Difficulty, out var diff)) diff = MockTestDifficulty.Mixed;
        m.Title = req.Title; m.Description = req.Description; m.Topic = req.Topic;
        m.Difficulty = diff; m.TimeLimitMins = req.TimeLimitMins;
        m.TotalQuestions = req.TotalQuestions; m.PassMarkPercent = req.PassMarkPercent;
        m.RandomizeQuestions = req.RandomizeQuestions; m.Tags = req.Tags;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/publish")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Publish(int id)
    {
        var m = await db.MockTests.FindAsync(id);
        if (m is null) return NotFound();
        m.Status = MockTestStatus.Published;
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await db.MockTests.FindAsync(id);
        if (m is null) return NotFound();
        m.Status = MockTestStatus.Archived;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Questions ─────────────────────────────────────────────
    [HttpPost("{testId}/questions")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> AddQuestion(int testId, [FromBody] AddMockQuestionRequest req)
    {
        if (!Enum.TryParse<MockTestDifficulty>(req.Difficulty, out var diff)) diff = MockTestDifficulty.Medium;
        if (!Enum.TryParse<MockQuestionType>(req.QuestionType, out var qType)) qType = MockQuestionType.SingleChoice;

        var q = new MockTestQuestion
        {
            Text = req.Text, Topic = req.Topic, Difficulty = diff,
            QuestionType = qType,
            Marks = req.Marks, NegativeMarks = req.NegativeMarks,
            Explanation = req.Explanation, ExplanationImageUrl = req.ExplanationImageUrl,
            ImageUrl = req.ImageUrl, FormulaLatex = req.FormulaLatex,
            MockTestId = testId,
            DisplayOrder = await db.MockTestQuestions.CountAsync(x => x.MockTestId == testId)
        };
        db.MockTestQuestions.Add(q);
        await db.SaveChangesAsync();

        foreach (var (opt, idx) in req.Options.Select((o, i) => (o, i)))
            db.MockTestOptions.Add(new MockTestOption
            {
                Text = opt.Text, IsCorrect = opt.IsCorrect,
                ImageUrl = opt.ImageUrl,
                QuestionId = q.Id, DisplayOrder = idx
            });

        // TrueFalse: auto-create True/False options if none provided
        if (qType == MockQuestionType.TrueFalse && !req.Options.Any())
        {
            db.MockTestOptions.Add(new MockTestOption { Text = "True",  IsCorrect = false, QuestionId = q.Id, DisplayOrder = 0 });
            db.MockTestOptions.Add(new MockTestOption { Text = "False", IsCorrect = false, QuestionId = q.Id, DisplayOrder = 1 });
        }

        await db.SaveChangesAsync();
        return Ok(new { q.Id });
    }

    [HttpPut("questions/{questionId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] AddMockQuestionRequest req)
    {
        var q = await db.MockTestQuestions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == questionId);
        if (q is null) return NotFound();

        if (!Enum.TryParse<MockTestDifficulty>(req.Difficulty, out var diff)) diff = MockTestDifficulty.Medium;
        if (!Enum.TryParse<MockQuestionType>(req.QuestionType, out var qType)) qType = MockQuestionType.SingleChoice;

        q.Text = req.Text; q.Topic = req.Topic; q.Difficulty = diff;
        q.QuestionType = qType; q.Marks = req.Marks; q.NegativeMarks = req.NegativeMarks;
        q.Explanation = req.Explanation; q.ExplanationImageUrl = req.ExplanationImageUrl;
        q.ImageUrl = req.ImageUrl; q.FormulaLatex = req.FormulaLatex;

        // Replace options
        db.MockTestOptions.RemoveRange(q.Options);
        foreach (var (opt, idx) in req.Options.Select((o, i) => (o, i)))
            db.MockTestOptions.Add(new MockTestOption
            {
                Text = opt.Text, IsCorrect = opt.IsCorrect,
                ImageUrl = opt.ImageUrl, QuestionId = q.Id, DisplayOrder = idx
            });

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("questions/{questionId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        var q = await db.MockTestQuestions.FindAsync(questionId);
        if (q is null) return NotFound();
        db.MockTestQuestions.Remove(q);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Start attempt ─────────────────────────────────────────
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartMockAttemptRequest req)
    {
        var test = await db.MockTests
            .Include(m => m.Questions.OrderBy(q => q.DisplayOrder))
                .ThenInclude(q => q.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(m => m.Id == req.MockTestId && m.Status == MockTestStatus.Published);
        if (test is null) return NotFound(new { message = "Test not found or not published" });

        var prevCount = await db.MockTestAttempts.CountAsync(a => a.MockTestId == req.MockTestId && a.StudentId == req.StudentId);
        if (prevCount >= test.MaxAttempts)
            return BadRequest(new { message = $"Maximum {test.MaxAttempts} attempts reached" });

        var attempt = new MockTestAttempt
        {
            MockTestId = req.MockTestId,
            StudentId  = req.StudentId,
            AttemptNumber = prevCount + 1
        };
        db.MockTestAttempts.Add(attempt);
        await db.SaveChangesAsync();

        var questions = test.RandomizeQuestions
            ? test.Questions.OrderBy(_ => Guid.NewGuid()).Take(test.TotalQuestions).ToList()
            : test.Questions.Take(test.TotalQuestions).ToList();

        return Ok(new
        {
            attemptId      = attempt.Id,
            timeLimitMins  = test.TimeLimitMins,
            totalQuestions = questions.Count,
            attemptNumber  = attempt.AttemptNumber,
            questions = questions.Select(q => new
            {
                q.Id, q.Text, q.ImageUrl, q.Topic, q.Marks,
                q.FormulaLatex,
                Difficulty   = q.Difficulty.ToString(),
                QuestionType = q.QuestionType.ToString(),
                // Hide IsCorrect from student
                options = q.Options.Select(o => new { o.Id, o.Text, o.ImageUrl })
            })
        });
    }

    // ─── Submit attempt ────────────────────────────────────────
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitMockAttemptRequest req)
    {
        var attempt = await db.MockTestAttempts
            .Include(a => a.MockTest)
            .ThenInclude(t => t.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == req.AttemptId);
        if (attempt is null) return NotFound();
        if (attempt.Status != MockAttemptStatus.InProgress)
            return BadRequest(new { message = "Already submitted" });

        var questions = attempt.MockTest.Questions.ToList();
        int totalMarks = questions.Sum(q => q.Marks);
        int earned = 0, negative = 0;
        var topicData = new Dictionary<string, (int total, int correct)>();

        foreach (var q in questions)
        {
            var ans = req.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            bool isSkipped  = ans == null;
            bool isCorrect  = false;
            int  marks      = 0;

            if (!isSkipped)
            {
                switch (q.QuestionType)
                {
                    case MockQuestionType.SingleChoice:
                    case MockQuestionType.Dropdown:
                    case MockQuestionType.TrueFalse:
                    {
                        var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrect);
                        isCorrect = correctOpt?.Id == ans!.SelectedOptionId;
                        if (isCorrect) { marks = q.Marks; earned += q.Marks; }
                        else { marks = -q.NegativeMarks; negative += q.NegativeMarks; }
                        break;
                    }
                    case MockQuestionType.MultiChoice:
                    {
                        // All correct options must be selected, no extras
                        var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(x => x).ToList();
                        var selectedIds = (ans!.SelectedOptionIds ?? []).OrderBy(x => x).ToList();
                        isCorrect = correctIds.SequenceEqual(selectedIds);
                        if (isCorrect) { marks = q.Marks; earned += q.Marks; }
                        else { marks = -q.NegativeMarks; negative += q.NegativeMarks; }
                        break;
                    }
                    case MockQuestionType.ShortAnswer:
                    case MockQuestionType.Formula:
                        marks = 0; // Manual grading
                        isSkipped = true;
                        break;
                }
            }

            // Build selected option ids string for multi-choice
            string? selectedIdsStr = ans?.SelectedOptionIds?.Count > 0
                ? string.Join(",", ans.SelectedOptionIds)
                : null;

            db.MockTestAnswers.Add(new MockTestAnswer
            {
                AttemptId        = attempt.Id,
                QuestionId       = q.Id,
                SelectedOptionId = ans?.SelectedOptionId,
                SelectedOptionIds = selectedIdsStr,
                TextAnswer       = ans?.TextAnswer,
                IsCorrect        = isCorrect,
                IsSkipped        = isSkipped,
                MarksAwarded     = marks,
            });

            if (!topicData.ContainsKey(q.Topic)) topicData[q.Topic] = (0, 0);
            var (t, cor) = topicData[q.Topic];
            topicData[q.Topic] = (t + 1, cor + (isCorrect ? 1 : 0));
        }

        var net          = Math.Max(0, earned - negative);
        var scorePct     = totalMarks > 0 ? (int)Math.Round(net * 100.0 / totalMarks) : 0;
        var passed       = scorePct >= attempt.MockTest.PassMarkPercent;
        var readiness    = scorePct >= 80 ? "Ready" : scorePct >= 60 ? "NeedsPractice" : "Weak";

        attempt.CompletedAt        = DateTime.UtcNow;
        attempt.TimeTakenSecs      = req.TimeTakenSecs;
        attempt.TotalMarks         = totalMarks;
        attempt.MarksObtained      = net;
        attempt.NegativeMarks      = negative;
        attempt.ScorePercent       = scorePct;
        attempt.Passed             = passed;
        attempt.Status             = MockAttemptStatus.Completed;
        attempt.InterviewReadiness = readiness;

        foreach (var (topic, (tot, cor)) in topicData)
            db.TopicScores.Add(new TopicScore
            {
                AttemptId      = attempt.Id, Topic = topic,
                TotalQuestions = tot, Correct = cor,
                ScorePercent   = tot > 0 ? (int)Math.Round(cor * 100.0 / tot) : 0
            });

        await db.SaveChangesAsync();

        var allScores = await db.MockTestAttempts
            .Where(a => a.MockTestId == attempt.MockTestId && a.Status == MockAttemptStatus.Completed)
            .OrderByDescending(a => a.ScorePercent).Select(a => a.Id).ToListAsync();
        attempt.Rank = allScores.IndexOf(attempt.Id) + 1;
        await db.SaveChangesAsync();

        return Ok(new { scorePct, passed, readiness, net, totalMarks, negativeMarks = negative, rank = attempt.Rank, attemptId = attempt.Id });
    }

    // ─── Attempt result detail ─────────────────────────────────
    [HttpGet("attempt/{attemptId}")]
    public async Task<IActionResult> GetAttemptResult(int attemptId)
    {
        var a = await db.MockTestAttempts
            .Include(a => a.MockTest)
            .Include(a => a.Student)
            .Include(a => a.TopicScores)
            .Include(a => a.Answers).ThenInclude(ans => ans.Question).ThenInclude(q => q.Options)
            .Include(a => a.Answers).ThenInclude(ans => ans.SelectedOption)
            .FirstOrDefaultAsync(a => a.Id == attemptId);
        if (a is null) return NotFound();

        var ansDetails = a.Answers.Select(ans => {
            var q = ans.Question;
            var correctOpts = q.Options.Where(o => o.IsCorrect).Select(o => o.Text).ToList();
            var selectedOpts = new List<string>();
            if (ans.SelectedOption is not null) selectedOpts.Add(ans.SelectedOption.Text);
            if (!string.IsNullOrEmpty(ans.SelectedOptionIds))
            {
                var selIds = ans.SelectedOptionIds.Split(',').Select(int.Parse).ToHashSet();
                selectedOpts = q.Options.Where(o => selIds.Contains(o.Id)).Select(o => o.Text).ToList();
            }
            return new MockAnswerDetailDto(
                q.Id, q.Text, q.ImageUrl,
                q.QuestionType.ToString(),
                ans.SelectedOption?.Text,
                selectedOpts,
                correctOpts.FirstOrDefault(),
                correctOpts,
                ans.IsCorrect, ans.IsSkipped, ans.MarksAwarded,
                q.Explanation, q.ExplanationImageUrl, q.FormulaLatex
            );
        }).ToList();

        return Ok(new MockAttemptResultDto(
            a.Id, a.MockTestId, a.MockTest.Title,
            a.StudentId, $"{a.Student.FirstName} {a.Student.LastName}",
            a.StartedAt, a.CompletedAt, a.TimeTakenSecs,
            a.TotalMarks, a.MarksObtained, a.NegativeMarks,
            a.ScorePercent, a.Rank, a.Passed, a.InterviewReadiness,
            a.Status.ToString(), a.AttemptNumber,
            a.TopicScores.Select(t => new TopicScoreDto(t.Topic, t.TotalQuestions, t.Correct, t.ScorePercent)).ToList(),
            ansDetails
        ));
    }

    // ─── Analysis ──────────────────────────────────────────────
    [HttpGet("analysis/student/{studentId}")]
    public async Task<IActionResult> GetStudentAnalysis(int studentId)
    {
        var student = await db.Users.FindAsync(studentId);
        if (student is null) return NotFound();

        var attempts = await db.MockTestAttempts
            .Include(a => a.MockTest).Include(a => a.TopicScores)
            .Where(a => a.StudentId == studentId && a.Status == MockAttemptStatus.Completed)
            .OrderByDescending(a => a.CompletedAt).ToListAsync();

        if (!attempts.Any()) return Ok(new { message = "No completed attempts", studentId });

        var avgScore  = attempts.Average(a => a.ScorePercent);
        var bestScore = attempts.Max(a => a.ScorePercent);
        var readiness = bestScore >= 80 ? "Ready" : bestScore >= 60 ? "NeedsPractice" : "Weak";

        var topicAgg = attempts.SelectMany(a => a.TopicScores)
            .GroupBy(t => t.Topic)
            .Select(g => new TopicScoreDto(g.Key, g.Sum(x => x.TotalQuestions), g.Sum(x => x.Correct),
                g.Sum(x => x.TotalQuestions) > 0 ? (int)Math.Round(g.Sum(x => x.Correct) * 100.0 / g.Sum(x => x.TotalQuestions)) : 0))
            .ToList();

        return Ok(new MockTestAnalysisDto(
            studentId, $"{student.FirstName} {student.LastName}",
            attempts.Count, Math.Round(avgScore, 1), bestScore, readiness,
            topicAgg.Where(t => t.ScorePercent < 60).OrderBy(t => t.ScorePercent).ToList(),
            topicAgg.Where(t => t.ScorePercent >= 75).OrderByDescending(t => t.ScorePercent).ToList(),
            attempts.Take(5).Select(a => new MockAttemptResultDto(
                a.Id, a.MockTestId, a.MockTest.Title, a.StudentId,
                $"{student.FirstName} {student.LastName}",
                a.StartedAt, a.CompletedAt, a.TimeTakenSecs,
                a.TotalMarks, a.MarksObtained, a.NegativeMarks,
                a.ScorePercent, a.Rank, a.Passed, a.InterviewReadiness,
                a.Status.ToString(), a.AttemptNumber,
                a.TopicScores.Select(t => new TopicScoreDto(t.Topic, t.TotalQuestions, t.Correct, t.ScorePercent)).ToList(), null
            )).ToList()
        ));
    }

    [HttpGet("{testId}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(int testId)
    {
        var attempts = await db.MockTestAttempts
            .Include(a => a.Student)
            .Where(a => a.MockTestId == testId && a.Status == MockAttemptStatus.Completed)
            .OrderByDescending(a => a.ScorePercent).ThenBy(a => a.TimeTakenSecs)
            .Take(50).ToListAsync();
        return Ok(attempts.Select((a, i) => new
        {
            rank = i + 1, a.ScorePercent, a.MarksObtained, a.TotalMarks,
            a.TimeTakenSecs, a.Passed, a.InterviewReadiness, a.CompletedAt,
            student = new { a.Student.Id, a.Student.FirstName, a.Student.LastName }
        }));
    }

    static MockTestDto MapTest(MockTest m, bool includeQuestions) => new(
        m.Id, m.Title, m.Description, m.Topic,
        m.Difficulty.ToString(), m.Status.ToString(),
        m.TimeLimitMins, m.TotalQuestions, m.PassMarkPercent,
        m.RandomizeQuestions, m.ShowResultImmediately, m.MaxAttempts, m.Tags,
        m.OrganizationId, m.CourseId, m.CreatedAt, m.Attempts.Count,
        includeQuestions
            ? m.Questions.OrderBy(q => q.DisplayOrder).Select(q => new MockTestQuestionDto(
                q.Id, q.Text, q.ImageUrl, q.Explanation, q.ExplanationImageUrl,
                q.FormulaLatex, q.Topic, q.Difficulty.ToString(),
                q.QuestionType.ToString(), q.Marks, q.NegativeMarks, q.DisplayOrder,
                q.Options.OrderBy(o => o.DisplayOrder)
                    .Select(o => new MockTestOptionDto(o.Id, o.Text, o.ImageUrl, o.IsCorrect, o.DisplayOrder)).ToList()
            )).ToList() : null
    );
}
