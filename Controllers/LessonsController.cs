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
    List<LessonResourceDto> Resources,
    int? ParentLessonId = null,
    List<LessonDto>? ChildLessons = null
);

public record LessonResourceDto(int Id, string Title, string FileUrl, long FileSizeBytes, string Type, int DisplayOrder);

public record CreateLessonRequest(
    string Title, string? Description,
    string Type, bool IsPreview, bool IsPublished,
    int DisplayOrder, int DurationSecs, int ModuleId,
    string? VideoUrl, string? FileUrl, string? Content,
    int? ParentLessonId = null
);

public record UpdateLessonRequest(
    string? Title, string? Description,
    string? Type, bool? IsPreview, bool? IsPublished,
    int? DisplayOrder, int? DurationSecs,
    string? VideoUrl, string? FileUrl, string? Content,
    int? ParentLessonId = null
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
        // Fetch the whole module's lesson tree in one query, then build
        // the parent→children structure in memory. Doing the recursion
        // client-side (in C#, not via N+1 queries) keeps this to a single
        // round-trip to the database regardless of how deep the tree goes.
        var allLessons = await db.Lessons
            .Include(l => l.Module)
            .Include(l => l.Resources)
            .Where(l => l.ModuleId == moduleId)
            .OrderBy(l => l.DisplayOrder)
            .ToListAsync();

        var byParent = allLessons
            .GroupBy(l => l.ParentLessonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        LessonDto MapWithChildren(Lesson l)
        {
            var children = byParent.TryGetValue(l.Id, out var kids)
                ? kids.OrderBy(k => k.DisplayOrder).Select(MapWithChildren).ToList()
                : new List<LessonDto>();
            return Map(l) with { ChildLessons = children };
        }

        var roots = byParent.TryGetValue(null, out var rootLessons) ? rootLessons : [];
        return Ok(roots.OrderBy(l => l.DisplayOrder).Select(MapWithChildren));
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

        // A parent lesson, if specified, must exist and belong to the same
        // module — prevents accidentally building a tree that spans
        // multiple modules, which would make the module's own lesson list
        // inconsistent.
        if (req.ParentLessonId is not null)
        {
            var parentExists = await db.Lessons.AnyAsync(p => p.Id == req.ParentLessonId.Value && p.ModuleId == req.ModuleId);
            if (!parentExists)
                return BadRequest(new { message = "Parent lesson not found in this module" });
        }

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
            ParentLessonId = req.ParentLessonId,
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

        if (req.ParentLessonId != l.ParentLessonId)
        {
            if (req.ParentLessonId == id)
                return BadRequest(new { message = "A lesson cannot be its own parent" });
            if (req.ParentLessonId is not null)
            {
                // Walk up from the proposed new parent to the root,
                // checking we never re-encounter this lesson — that would
                // mean moving a lesson underneath one of its own
                // descendants, creating a cycle in the tree.
                var cursor = await db.Lessons.FindAsync(req.ParentLessonId.Value);
                while (cursor is not null)
                {
                    if (cursor.Id == id)
                        return BadRequest(new { message = "Cannot move a lesson under its own descendant" });
                    cursor = cursor.ParentLessonId is not null
                        ? await db.Lessons.FindAsync(cursor.ParentLessonId.Value)
                        : null;
                }
            }
            l.ParentLessonId = req.ParentLessonId;
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin,Instructor")]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await db.Lessons.FindAsync(id);
        if (l is null) return NotFound();

        // The self-referencing FK uses Restrict (not Cascade) delete
        // behavior, so removing a lesson with children would otherwise
        // fail at the database level. Recursively collect and remove the
        // whole subtree first, deepest descendants first, then the
        // lesson itself.
        var allInModule = await db.Lessons.Where(x => x.ModuleId == l.ModuleId).ToListAsync();
        var toDelete = new List<Lesson>();
        void CollectDescendants(int parentId)
        {
            foreach (var child in allInModule.Where(x => x.ParentLessonId == parentId))
            {
                CollectDescendants(child.Id);
                toDelete.Add(child);
            }
        }
        CollectDescendants(id);
        toDelete.Add(l);

        db.Lessons.RemoveRange(toDelete);
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
    // req.WatchedSeconds is the SECONDS WATCHED SINCE THE LAST SAVE (a
    // delta), not a running total. The frontend resets its own counter to 0
    // each time a lesson opens (so Next/Prev navigation can't mix one
    // lesson's time into another's), then sends small periodic deltas as
    // it plays. The backend's job is simply to add each delta onto
    // WatchedSeconds, so multiple separate viewing sessions accumulate
    // correctly instead of the latest session overwriting earlier ones.
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

        lp.WatchedSeconds += Math.Max(0, req.WatchedSeconds);
        lp.LastPositionSec = req.LastPositionSec;
        lp.IsCompleted = lp.IsCompleted || req.IsCompleted; // never un-complete a lesson
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
        l.Resources.Select(MapResource).ToList(),
        l.ParentLessonId
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

            // Auto-issue certificate on course completion
            var alreadyHasCert = await db.Certificates
                .AnyAsync(cert => cert.UserId == userId && cert.CourseId == courseId);
            if (!alreadyHasCert)
            {
                db.Certificates.Add(new Certificate
                {
                    UserId = userId,
                    CourseId = courseId,
                    IssuedAt = DateTime.UtcNow,
                    TotalWatchMinutes = enrollment.TotalWatchSeconds / 60,
                    CertificateNumber = Guid.NewGuid().ToString("N")[..12].ToUpper()
                });
            }
        }
        await db.SaveChangesAsync();
    }
}