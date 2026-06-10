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
        var items = await q.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * size).Take(size)
            .Select(o => MapOrg(o, db.Users.Count(u => u.OrganizationId == o.Id), db.Courses.Count(c => c.OrganizationId == o.Id)))
            .ToListAsync();
        return Ok(new PagedResult<OrganizationDto>(items, total, page, size, (int)Math.Ceiling(total / (double)size)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var o = await db.Organizations.FindAsync(id);
        if (o is null) return NotFound();
        var uc = await db.Users.CountAsync(u => u.OrganizationId == id);
        var cc = await db.Courses.CountAsync(c => c.OrganizationId == id);
        return Ok(MapOrg(o, uc, cc));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
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
        o.IsActive, o.CreatedAt, uc, cc
    );
}
