using LMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

public class ImageUploadRequest { public IFormFile File { get; set; } = null!; }
public class VideoUploadRequest { public IFormFile File { get; set; } = null!; }
public class FileUploadRequest  { public IFormFile File { get; set; } = null!; }

[ApiController, Route("api/upload"), Authorize]
public class UploadController(ICloudflareService cloudflare, ILogger<UploadController> logger) : ControllerBase
{
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]           // 10 MB for images
    public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest request, [FromQuery] string folder = "images")
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var result = await cloudflare.UploadImageAsync(file, folder);
        if (!result.Success)
            return StatusCode(500, new { message = result.Error ?? "Upload failed" });

        logger.LogInformation("Image uploaded: {Url}", result.Url);
        return Ok(new { url = result.Url, key = result.FileKey });
    }

    [HttpPost("video")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)]    // 2 GB for videos
    [RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024)]
    public async Task<IActionResult> UploadVideo([FromForm] VideoUploadRequest request, [FromQuery] string folder = "videos")
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var result = await cloudflare.UploadVideoAsync(file, folder);
        if (!result.Success)
            return StatusCode(500, new { message = result.Error ?? "Upload failed" });

        logger.LogInformation("Video uploaded: {Url}", result.Url);
        return Ok(new { url = result.Url, key = result.FileKey });
    }

    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]          // 100 MB for documents
    public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request, [FromQuery] string folder = "documents")
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var result = await cloudflare.UploadFileAsync(file, folder);
        if (!result.Success)
            return StatusCode(500, new { message = result.Error ?? "Upload failed" });

        logger.LogInformation("File uploaded: {Url}", result.Url);
        return Ok(new { url = result.Url, key = result.FileKey });
    }

    [HttpDelete]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> DeleteFile([FromQuery] string key)
    {
        var ok = await cloudflare.DeleteFileAsync(key);
        return ok ? NoContent() : StatusCode(500, new { message = "Delete failed" });
    }
}
