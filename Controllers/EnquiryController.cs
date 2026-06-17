using LMS.API.Data;
using LMS.API.Models;
using LMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

public record BatchEnquiryRequest(
    string Name, string Phone, string? Email,
    string? CourseInterest, int? BatchId, int OrganizationId
);

[ApiController, Route("api/enquiries")]
public class EnquiryController(LmsDbContext db, IEmailService email) : ControllerBase
{
    [HttpPost("batch")]
    public async Task<IActionResult> BatchEnquiry([FromBody] BatchEnquiryRequest req)
    {
        var enquiry = new BatchEnquiry
        {
            Name = req.Name,
            Phone = req.Phone,
            Email = req.Email,
            CourseInterest = req.CourseInterest,
            BatchId = req.BatchId,
            OrganizationId = req.OrganizationId,
            CreatedAt = DateTime.UtcNow,
        };
        db.BatchEnquiries.Add(enquiry);
        await db.SaveChangesAsync();

        // Email admin of the org
        var org = await db.Organizations.FindAsync(req.OrganizationId);
        if (org?.ContactEmail != null)
        {
            var batchName = req.BatchId.HasValue
                ? (await db.TrainingBatches.FindAsync(req.BatchId.Value))?.BatchName ?? ""
                : req.CourseInterest ?? "General Enquiry";

            _ = email.SendEmailAsync(
                org.ContactEmail,
                $"New Batch Enquiry — {req.Name}",
                $@"<h2>New Enquiry Received</h2>
                   <p><strong>Name:</strong> {req.Name}</p>
                   <p><strong>Phone:</strong> {req.Phone}</p>
                   <p><strong>Email:</strong> {req.Email ?? "—"}</p>
                   <p><strong>Interested in:</strong> {batchName}</p>
                   <p><em>Received at {DateTime.Now:dd MMM yyyy, hh:mm tt}</em></p>"
            );
        }

        return Ok(new { message = "Enquiry received! We will contact you shortly." });
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int? orgId)
    {
        var q = db.BatchEnquiries
            .Include(e => e.Batch)
            .AsQueryable();
        if (orgId.HasValue) q = q.Where(e => e.OrganizationId == orgId.Value);
        var list = await q.OrderByDescending(e => e.CreatedAt).Take(200).ToListAsync();
        return Ok(list.Select(e => new {
            e.Id,
            e.Name,
            e.Phone,
            e.Email,
            e.CourseInterest,
            batchName = e.Batch?.BatchName,
            e.CreatedAt,
        }));
    }
}