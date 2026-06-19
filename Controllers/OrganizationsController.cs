using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

[ApiController, Route("api/organizations"), Authorize]
public class OrganizationsController(LmsDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var q = db.Organizations.AsQueryable();
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync();
        var dtos = items.Select(o => MapOrg(o,
            db.Users.Count(u => u.OrganizationId == o.Id),
            db.Courses.Count(c => c.OrganizationId == o.Id))).ToList();
        return Ok(new PagedResult<OrganizationDto>(dtos, total, page, size, (int)Math.Ceiling(total / (double)size)));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id)
    {
        var o = await db.Organizations.FindAsync(id);
        if (o is null) return NotFound();
        var uc = await db.Users.CountAsync(u => u.OrganizationId == id);
        var cc = await db.Courses.CountAsync(c => c.OrganizationId == id);
        return Ok(MapOrg(o, uc, cc));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest req)
    {
        var slug = req.Name.ToLower().Replace(" ", "-");
        if (await db.Organizations.AnyAsync(o => o.Slug == slug)) slug = $"{slug}-{Random.Shared.Next(1000, 9999)}";
        var org = new Organization
        {
            Name = req.Name,
            Slug = slug,
            Website = req.Website,
            PrimaryColor = req.PrimaryColor ?? "#6366f1",
            PortalUrl = req.PortalUrl
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = org.Id }, MapOrg(org, 0, 0));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrganizationRequest req)
    {
        var org = await db.Organizations.FindAsync(id);
        if (org is null) return NotFound();

        if (req.Name is not null) org.Name = req.Name;
        if (req.Website is not null) org.Website = req.Website;
        if (req.PrimaryColor is not null) org.PrimaryColor = req.PrimaryColor;
        if (req.SecondaryColor is not null) org.SecondaryColor = req.SecondaryColor;
        if (req.AccentColor is not null) org.AccentColor = req.AccentColor;
        if (req.LogoUrl is not null) org.LogoUrl = req.LogoUrl;
        if (req.BannerUrl is not null) org.BannerUrl = req.BannerUrl;
        if (req.Tagline is not null) org.Tagline = req.Tagline;
        if (req.ThemeFont is not null) org.ThemeFont = req.ThemeFont;
        if (req.PortalUrl is not null) org.PortalUrl = req.PortalUrl;
        if (req.IsActive is not null) org.IsActive = req.IsActive.Value;

        // ── Homepage feature flags ──────────────────────────────────────
        if (req.ShowScrollingBanner is not null) org.ShowScrollingBanner = req.ShowScrollingBanner.Value;
        if (req.ScrollingBannerText is not null) org.ScrollingBannerText = req.ScrollingBannerText;
        if (req.ShowReferralOffer is not null) org.ShowReferralOffer = req.ShowReferralOffer.Value;
        if (req.ReferralOfferText is not null) org.ReferralOfferText = req.ReferralOfferText;
        if (req.ShowCourseBatches is not null) org.ShowCourseBatches = req.ShowCourseBatches.Value;
        if (req.ShowAllCourses is not null) org.ShowAllCourses = req.ShowAllCourses.Value;
        if (req.ShowContactUs is not null) org.ShowContactUs = req.ShowContactUs.Value;
        if (req.ShowAboutUs is not null) org.ShowAboutUs = req.ShowAboutUs.Value;
        if (req.ShowOpenings is not null) org.ShowOpenings = req.ShowOpenings.Value;

        // ── Content fields ───────────────────────────────────────────────
        if (req.AboutUsContent is not null) org.AboutUsContent = req.AboutUsContent;
        if (req.ContactEmail is not null) org.ContactEmail = req.ContactEmail;
        if (req.ContactPhone is not null) org.ContactPhone = req.ContactPhone;
        if (req.ContactAddress is not null) org.ContactAddress = req.ContactAddress;
        if (req.ContactMapEmbed is not null) org.ContactMapEmbed = req.ContactMapEmbed;
        if (req.OpeningsContent is not null) org.OpeningsContent = req.OpeningsContent;
        if (req.CustomMenuJson is not null) org.CustomMenuJson = req.CustomMenuJson;
        if (req.AboutUsTemplate is not null) org.AboutUsTemplate = req.AboutUsTemplate;
        if (req.ContactUsTemplate is not null) org.ContactUsTemplate = req.ContactUsTemplate;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var org = await db.Organizations.FindAsync(id);
        if (org is null) return NotFound();
        org.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    static OrganizationDto MapOrg(Organization o, int uc, int cc) => new(
        o.Id, o.Name, o.Slug, o.LogoUrl, o.BannerUrl, o.Tagline,
        o.PrimaryColor, o.SecondaryColor, o.AccentColor,
        o.ThemeFont, o.Website, o.PortalUrl,
        o.IsActive, o.CreatedAt, uc, cc,
        // Homepage feature flags
        o.ShowScrollingBanner, o.ScrollingBannerText,
        o.ShowReferralOffer, o.ReferralOfferText,
        o.ShowCourseBatches, o.ShowAllCourses,
        o.ShowContactUs, o.ShowAboutUs, o.ShowOpenings,
        // Content
        o.AboutUsContent, o.ContactEmail, o.ContactPhone,
        o.ContactAddress, o.ContactMapEmbed, o.OpeningsContent,
        o.CustomMenuJson,
        o.AboutUsTemplate, o.ContactUsTemplate
    );
}