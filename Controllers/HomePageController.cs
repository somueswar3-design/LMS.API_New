using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/homepage")]
public class HomePageController(LmsDbContext db) : ControllerBase
{
    // Public: used by portal on page load
    [HttpGet("{orgId}")]
    public async Task<IActionResult> Get(int orgId)
    {
        var cfg = await db.HomePageConfigs.FirstOrDefaultAsync(h => h.OrganizationId == orgId);
        if (cfg is null)
        {
            // Return sensible defaults
            var org = await db.Organizations.FindAsync(orgId);
            cfg = DefaultConfig(orgId, org?.Name);
        }
        return Ok(MapDto(cfg));
    }

    // Admin: save homepage config
    [HttpPut("{orgId}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Save(int orgId, [FromBody] SaveHomePageConfigRequest req)
    {
        var cfg = await db.HomePageConfigs.FirstOrDefaultAsync(h => h.OrganizationId == orgId);
        bool isNew = cfg is null;
        cfg ??= new HomePageConfig { OrganizationId = orgId };

        cfg.TemplateId        = req.TemplateId;
        cfg.HeroTitle         = req.HeroTitle;
        cfg.HeroSubtitle      = req.HeroSubtitle;
        cfg.HeroButtonText    = req.HeroButtonText;
        cfg.HeroButtonUrl     = req.HeroButtonUrl;
        cfg.HeroImageUrl      = req.HeroImageUrl;
        cfg.HeroVideoUrl      = req.HeroVideoUrl;
        cfg.HeroStyle         = req.HeroStyle;
        cfg.SectionsConfig    = req.SectionsConfig;
        cfg.ShowStats         = req.ShowStats;
        cfg.StatsCustom       = req.StatsCustom;
        cfg.AnnouncementText  = req.AnnouncementText;
        cfg.ShowAnnouncement  = req.ShowAnnouncement;
        cfg.NavLinksJson      = req.NavLinksJson;
        cfg.FooterTagline     = req.FooterTagline;
        cfg.FooterLinksJson   = req.FooterLinksJson;
        cfg.FooterSocialJson  = req.FooterSocialJson;
        cfg.FooterCopyright   = req.FooterCopyright;
        cfg.ShowFooterNewsletter = req.ShowFooterNewsletter;
        cfg.CustomSectionsJson = req.CustomSectionsJson;
        cfg.CustomHtml         = req.CustomHtml;
        cfg.UpdatedAt         = DateTime.UtcNow;

        if (isNew) db.HomePageConfigs.Add(cfg);
        await db.SaveChangesAsync();
        return Ok(MapDto(cfg));
    }

    static HomePageConfig DefaultConfig(int orgId, string? orgName) => new()
    {
        OrganizationId   = orgId,
        TemplateId       = "modern",
        HeroTitle        = $"Welcome to {orgName ?? "Learning Portal"}",
        HeroSubtitle     = "Discover expert-led courses and grow your skills today.",
        HeroButtonText   = "Start Learning",
        HeroButtonUrl    = "/register",
        HeroStyle        = "gradient",
        ShowStats        = true,
        ShowAnnouncement = false,
        ShowFooterNewsletter = false,
        SectionsConfig   = """[{"id":"stats","enabled":true,"order":1},{"id":"categories","enabled":true,"order":2},{"id":"courses","enabled":true,"order":3},{"id":"instructors","enabled":true,"order":4},{"id":"cta","enabled":true,"order":5}]""",
        NavLinksJson     = """[{"label":"Courses","url":"#courses"},{"label":"Instructors","url":"#instructors"}]""",
        FooterLinksJson  = """[{"label":"Home","url":"/"},{"label":"Login","url":"/login"},{"label":"Register","url":"/register"}]""",
        FooterSocialJson = """[]""",
        FooterCopyright  = $"© {DateTime.UtcNow.Year} {orgName}. All rights reserved.",
    };

    static HomePageConfigDto MapDto(HomePageConfig c) => new(
        c.Id, c.OrganizationId, c.TemplateId,
        c.HeroTitle, c.HeroSubtitle, c.HeroButtonText,
        c.HeroButtonUrl, c.HeroImageUrl, c.HeroVideoUrl, c.HeroStyle,
        c.SectionsConfig, c.ShowStats, c.StatsCustom,
        c.AnnouncementText, c.ShowAnnouncement,
        c.NavLinksJson, c.FooterTagline, c.FooterLinksJson,
        c.FooterSocialJson, c.FooterCopyright, c.ShowFooterNewsletter,
        c.CustomSectionsJson,
        c.CustomHtml
    );
}
