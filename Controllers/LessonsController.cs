using System.Security.Claims;
using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

// ─── DTOs ─────────────────────────────────────────────────────
public record LessonDto(
    int Id, string Title, string? Description,
    string Type, bool IsPreview, bool IsPublished,
    int DisplayOrder, int DurationSecs,
    int ModuleId, string ModuleTitle,
    string? VideoUrl, string? FileUrl, string? Content,
    List<ContentBlock> ContentBlocks,
    List<LessonResourceDto> Resources
);

public record LessonResourceDto(int Id, string Title, string FileUrl, long FileSizeBytes, string Type, int DisplayOrder);

public record CreateLessonRequest(
    string Title, string? Description,
    string Type, bool IsPreview, bool IsPublished,
    int DisplayOrder, int DurationSecs, int ModuleId,
    string? VideoUrl, string? FileUrl, string? Content
);

public record UpdateLessonRequest(
    string? Title, string? Description,
    string? Type, bool? IsPreview, bool? IsPublished,
    int? DisplayOrder, int? DurationSecs,
    string? VideoUrl, string? FileUrl, string? Content
);

public record SaveContentBlocksRequest(List<ContentBlock> Blocks);
public record AddResourceRequest(string Title, string FileUrl, string? FileKey, long FileSizeBytes, string Type);
public record ReorderItem(int Id, int Order);

// ─── LESSONS CONTROLLER ────────────────────────────────────────
[ApiController, Route("api/lessons"), Authorize]
public class LessonsController(LmsDbContext db) : ControllerBase
{
    [HttpGet("module/{moduleId}")]
    public async Task<IActionResult> GetByModule(int moduleId)
    {
        var lessons = await db.Lessons
            .Include(l => l.Module)
            .Include(l => l.Resources)
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();
        return Ok(lessons.Select(Map));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var l = await db.Lessons
            .Include(l => l.Module)
            .Include(l => l.Resources.OrderBy(r => r.DisplayOrder))
            .FirstOrDefaultAsync(l => l.Id == id);
        return l is null ? NotFound() : Ok(Map(l));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateLessonRequest req)
    {
        if (!Enum.TryParse<LessonType>(req.Type, out var lt)) lt = LessonType.Video;

        var lesson = new Lesson
        {
            Title = req.Title,
            Description = req.Description,
            Type = lt,
            IsPreview = req.IsPreview,
            IsPublished = req.IsPublished,
            DisplayOrder = req.DisplayOrder,
            DurationSecs = req.DurationSecs,
            ModuleId = req.ModuleId,
            VideoUrl = req.VideoUrl,
            FileUrl = req.FileUrl,
            Content = req.Content,
        };

        var blocks = new List<ContentBlock>();
        if (lt == LessonType.Video && req.VideoUrl is not null)
            blocks.Add(new ContentBlock { Order = 0, Type = BlockType.Video, VideoUrl = req.VideoUrl, VideoTitle = req.Title });
        else if (lt == LessonType.Article && req.Content is not null)
            blocks.Add(new ContentBlock { Order = 0, Type = BlockType.Text, TextContent = req.Content });
        else if (lt == LessonType.Audio && req.FileUrl is not null)
            blocks.Add(new ContentBlock { Order = 0, Type = BlockType.Audio, AudioUrl = req.FileUrl, AudioTitle = req.Title });

        if (blocks.Any())
            lesson.ContentBlocksJson = ContentBlocks.Serialize(blocks);

        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = lesson.Id }, Map(lesson));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLessonRequest req)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();
        if (req.Title is not null) l.Title = req.Title;
        if (req.Description is not null) l.Description = req.Description;
        if (req.IsPreview is not null) l.IsPreview = req.IsPreview.Value;
        if (req.IsPublished is not null) l.IsPublished = req.IsPublished.Value;
        if (req.DisplayOrder is not null) l.DisplayOrder = req.DisplayOrder.Value;
        if (req.DurationSecs is not null) l.DurationSecs = req.DurationSecs.Value;
        if (req.VideoUrl is not null) l.VideoUrl = req.VideoUrl;
        if (req.FileUrl is not null) l.FileUrl = req.FileUrl;
        if (req.Content is not null) l.Content = req.Content;
        if (req.Type is not null && Enum.TryParse<LessonType>(req.Type, out var lt)) l.Type = lt;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();
        db.Lessons.Remove(l);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── CONTENT BLOCKS ──────────────────────────────────────
    [HttpGet("{id}/blocks")]
    public async Task<IActionResult> GetBlocks(int id)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();
        return Ok(ContentBlocks.Parse(l.ContentBlocksJson).OrderBy(b => b.Order).ToList());
    }

    [HttpPut("{id}/blocks")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> SaveBlocks(int id, [FromBody] SaveContentBlocksRequest req)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();

        var blocks = req.Blocks.Select((b, i) => { b.Order = i; return b; }).ToList();
        l.ContentBlocksJson = ContentBlocks.Serialize(blocks);

        var hasVideo = blocks.Any(b => b.Type == BlockType.Video);
        var hasAudio = blocks.Any(b => b.Type == BlockType.Audio);
        var hasPdf = blocks.Any(b => b.Type == BlockType.PDF || b.Type == BlockType.File);
        var hasText = blocks.Any(b => b.Type == BlockType.Text || b.Type == BlockType.Heading);

        l.Type = (hasVideo, hasAudio, hasPdf, hasText) switch
        {
            (true, _, _, _) => blocks.Count == 1 ? LessonType.Video : LessonType.Mixed,
            (false, true, _, _) => LessonType.Audio,
            (false, false, true, _) => LessonType.File,
            _ => LessonType.Article,
        };

        var firstVideo = blocks.FirstOrDefault(b => b.Type == BlockType.Video);
        if (firstVideo?.VideoUrl is not null) l.VideoUrl = firstVideo.VideoUrl;
        l.DurationSecs = blocks.Where(b => b.Type == BlockType.Video).Sum(b => b.VideoDurationSecs);

        await db.SaveChangesAsync();
        return Ok(new { saved = blocks.Count, lessonType = l.Type.ToString() });
    }

    [HttpPost("{id}/blocks")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> AddBlock(int id, [FromBody] ContentBlock block)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();
        var blocks = ContentBlocks.Parse(l.ContentBlocksJson);
        block.Order = blocks.Count;
        blocks.Add(block);
        l.ContentBlocksJson = ContentBlocks.Serialize(blocks);
        await db.SaveChangesAsync();
        return Ok(new { order = block.Order, total = blocks.Count });
    }

    [HttpDelete("{id}/blocks/{order}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> DeleteBlock(int id, int order)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();
        var blocks = ContentBlocks.Parse(l.ContentBlocksJson);
        blocks = blocks.Where(b => b.Order != order).Select((b, i) => { b.Order = i; return b; }).ToList();
        l.ContentBlocksJson = ContentBlocks.Serialize(blocks);
        await db.SaveChangesAsync();
        return Ok(new { remaining = blocks.Count });
    }

    // ─── RESOURCES ───────────────────────────────────────────
    [HttpGet("{id}/resources")]
    public async Task<IActionResult> GetResources(int id)
    {
        var resources = await db.LessonResources
            .Where(r => r.LessonId == id).OrderBy(r => r.DisplayOrder).ToListAsync();
        return Ok(resources.Select(MapResource));
    }

    [HttpPost("{id}/resources")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> AddResource(int id, [FromBody] AddResourceRequest req)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();
        if (!Enum.TryParse<ResourceType>(req.Type, out var rt)) rt = ResourceType.Other;
        var count = await db.LessonResources.CountAsync(r => r.LessonId == id);
        var resource = new LessonResource
        {
            LessonId = id,
            Title = req.Title,
            FileUrl = req.FileUrl,
            FileKey = req.FileKey,
            FileSizeBytes = req.FileSizeBytes,
            Type = rt,
            DisplayOrder = count
        };
        db.LessonResources.Add(resource);
        await db.SaveChangesAsync();
        return Ok(MapResource(resource));
    }

    [HttpDelete("{lessonId}/resources/{resourceId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> DeleteResource(int lessonId, int resourceId)
    {
        var r = await db.LessonResources.FirstOrDefaultAsync(r => r.Id == resourceId && r.LessonId == lessonId);
        if (r is null) return NotFound();
        db.LessonResources.Remove(r);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ─── Reorder ─────────────────────────────────────────────
    [HttpPost("reorder")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Reorder([FromBody] List<ReorderItem> items)
    {
        foreach (var item in items)
        {
            var l = await db.Lessons.FindAsync(item.Id);
            if (l is not null) l.DisplayOrder = item.Order;
        }
        await db.SaveChangesAsync();
        return Ok();
    }

    // ─── Progress ─────────────────────────────────────────────
    [HttpPost("progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest req)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var lp = await db.LessonProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == req.LessonId);

        if (lp is null)
        {
            lp = new LessonProgress { UserId = userId, LessonId = req.LessonId };
            db.LessonProgresses.Add(lp);
        }
        lp.WatchedSeconds = req.WatchedSeconds;
        lp.LastPositionSec = req.LastPositionSec;
        lp.IsCompleted = req.IsCompleted;
        lp.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await RecalcCourseProgress(userId, req.LessonId);
        return Ok(new LessonProgressDto(lp.LessonId, lp.IsCompleted, lp.WatchedSeconds, lp.LastPositionSec, lp.UpdatedAt));
    }

    [HttpGet("progress/course/{courseId}")]
    public async Task<IActionResult> GetCourseProgress(int courseId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var lessonIds = await db.Lessons.Where(l => l.Module.CourseId == courseId).Select(l => l.Id).ToListAsync();
        var progresses = await db.LessonProgresses.Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId)).ToListAsync();
        return Ok(progresses.Select(p => new LessonProgressDto(p.LessonId, p.IsCompleted, p.WatchedSeconds, p.LastPositionSec, p.UpdatedAt)));
    }

    // ─── Watch time report ────────────────────────────────────
    [HttpGet("watch-report/course/{courseId}")]
    public async Task<IActionResult> GetWatchReport(int courseId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var lessons = await db.Lessons
            .Where(l => l.Module.CourseId == courseId && l.IsPublished)
            .OrderBy(l => l.Module.DisplayOrder).ThenBy(l => l.DisplayOrder)
            .Select(l => new { l.Id, l.Title, l.DurationSecs, ModuleTitle = l.Module.Title })
            .ToListAsync();

        var lessonIds = lessons.Select(l => l.Id).ToList();
        var progresses = await db.LessonProgresses
            .Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId))
            .ToListAsync();

        var progressMap = progresses.ToDictionary(p => p.LessonId);
        var totalDuration = lessons.Sum(l => l.DurationSecs);
        var totalWatched = progresses.Sum(p => p.WatchedSeconds);
        var completed = progresses.Count(p => p.IsCompleted);

        var report = lessons.Select(l => {
            var p = progressMap.GetValueOrDefault(l.Id);
            return new
            {
                l.Id,
                l.Title,
                l.ModuleTitle,
                l.DurationSecs,
                WatchedSeconds = p?.WatchedSeconds ?? 0,
                IsCompleted = p?.IsCompleted ?? false,
                LastPositionSec = p?.LastPositionSec ?? 0,
                LastWatchedAt = p?.UpdatedAt,
                ProgressPercent = l.DurationSecs > 0 && p != null
                    ? (int)Math.Min(100, Math.Round(p.WatchedSeconds * 100.0 / l.DurationSecs)) : 0
            };
        }).ToList();

        return Ok(new
        {
            courseId,
            userId,
            totalLessons = lessons.Count,
            completedLessons = completed,
            totalDurationSecs = totalDuration,
            totalWatchedSecs = totalWatched,
            overallPercent = totalDuration > 0 ? (int)Math.Round(totalWatched * 100.0 / totalDuration) : 0,
            lessons = report
        });
    }

    // ─── Helpers ─────────────────────────────────────────────
    static LessonDto Map(Lesson l) => new(
        l.Id, l.Title, l.Description,
        l.Type.ToString(), l.IsPreview, l.IsPublished,
        l.DisplayOrder, l.DurationSecs,
        l.ModuleId, l.Module?.Title ?? "",
        l.VideoUrl, l.FileUrl, l.Content,
        ContentBlocks.Parse(l.ContentBlocksJson).OrderBy(b => b.Order).ToList(),
        l.Resources.Select(MapResource).ToList()
    );

    static LessonResourceDto MapResource(LessonResource r) => new(
        r.Id, r.Title, r.FileUrl, r.FileSizeBytes, r.Type.ToString(), r.DisplayOrder
    );

    async Task RecalcCourseProgress(int userId, int lessonId)
    {
        var courseId = await db.Lessons
            .Where(l => l.Id == lessonId)
            .Select(l => l.Module.CourseId)
            .FirstOrDefaultAsync();
        if (courseId == 0) return;

        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        if (enrollment is null) return;

        var totalLessons = await db.Lessons.CountAsync(l => l.Module.CourseId == courseId && l.IsPublished);
        if (totalLessons == 0) return;

        var completedLessons = await db.LessonProgresses.CountAsync(p =>
            p.UserId == userId && p.IsCompleted &&
            db.Lessons.Any(l => l.Id == p.LessonId && l.Module.CourseId == courseId));

        enrollment.ProgressPercent = (int)(completedLessons * 100.0 / totalLessons);
        enrollment.TotalWatchSeconds = await db.LessonProgresses
            .Where(p => p.UserId == userId && db.Lessons.Any(l => l.Id == p.LessonId && l.Module.CourseId == courseId))
            .SumAsync(p => p.WatchedSeconds);

        if (enrollment.ProgressPercent >= 100)
        {
            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }
}
