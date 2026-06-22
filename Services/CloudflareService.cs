using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.Runtime;

namespace LMS.API.Services;

public interface ICloudflareService
{
    Task<UploadResult> UploadImageAsync(IFormFile file, string folder);
    Task<UploadResult> UploadVideoAsync(IFormFile file, string folder);
    Task<UploadResult> UploadFileAsync(IFormFile file, string folder);
    Task<bool> DeleteFileAsync(string fileKey);
    string GetPublicUrl(string fileKey);
}

public record UploadResult(bool Success, string Url, string FileKey, string? Error = null);

public class CloudflareService(IConfiguration config, ILogger<CloudflareService> logger) : ICloudflareService
{
    readonly string _accessKey = config["CloudflareR2:AccessKeyId"]!;
    readonly string _secretKey = config["CloudflareR2:SecretAccessKey"]!;
    readonly string _bucket = config["CloudflareR2:BucketName"]!;
    readonly string _publicUrl = config["CloudflareR2:PublicUrl"]!;
    readonly string _endpoint = config["CloudflareR2:Endpoint"]!;

    static readonly string[] ImageExts = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"];
    static readonly string[] VideoExts = [".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v"];

    // Reused across requests — AmazonS3Client is thread-safe and expensive
    // to construct repeatedly (each call re-resolves credentials/endpoint).
    static readonly Lazy<AmazonS3Client> _sharedClient = new(() => new AmazonS3Client(
        new BasicAWSCredentials(
            Environment.GetEnvironmentVariable("R2_ACCESS_KEY") ?? "",
            Environment.GetEnvironmentVariable("R2_SECRET_KEY") ?? ""
        ),
        new AmazonS3Config { ForcePathStyle = true, SignatureVersion = "4" }
    ));

    AmazonS3Client CreateClient() => new(
        new BasicAWSCredentials(_accessKey, _secretKey),
        new AmazonS3Config
        {
            ServiceURL = _endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4",
            // Generous timeouts for large video uploads on slower connections
            Timeout = TimeSpan.FromMinutes(30),
            MaxErrorRetry = 3,
        }
    );

    public Task<UploadResult> UploadImageAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!ImageExts.Contains(ext))
            return Task.FromResult(new UploadResult(false, "", "", $"Invalid image type: {ext}"));
        return UploadAsync(file, folder);
    }

    public Task<UploadResult> UploadVideoAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!VideoExts.Contains(ext))
            return Task.FromResult(new UploadResult(false, "", "", $"Invalid video type: {ext}"));
        return UploadAsync(file, folder, isLargeFile: true);
    }

    public Task<UploadResult> UploadFileAsync(IFormFile file, string folder)
        => UploadAsync(file, folder);

    // ── Threshold for switching to multipart upload ──────────────────────
    // Anything at or above 25MB uses multipart streaming — this is much
    // lower than the old 100MB cutoff. Multipart streams directly from the
    // incoming request to R2 in chunks, so the server never has to hold the
    // entire file in RAM, and upload to R2 starts almost immediately instead
    // of waiting for the whole file to land on disk/memory first.
    const long MultipartThresholdBytes = 25L * 1024 * 1024;

    async Task<UploadResult> UploadAsync(IFormFile file, string folder, bool isLargeFile = false)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        var key = $"{folder.TrimEnd('/')}/{Guid.NewGuid():N}{ext}";

        try
        {
            using var client = CreateClient();

            if (file.Length >= MultipartThresholdBytes)
            {
                logger.LogInformation("Starting multipart upload for {Name} ({SizeMB:F1}MB)",
                    file.FileName, file.Length / 1024.0 / 1024.0);

                var transfer = new TransferUtility(client);

                // Stream directly from the request body — no intermediate
                // MemoryStream, no buffering the whole file before any
                // bytes reach R2. This is what makes large uploads fast.
                await using var stream = file.OpenReadStream();

                var uploadReq = new TransferUtilityUploadRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    CannedACL = S3CannedACL.PublicRead,
                    InputStream = stream,
                    // 10MB parts uploaded in parallel — good balance between
                    // throughput and memory use. R2 supports up to 10,000 parts.
                    PartSize = 10 * 1024 * 1024,
                    AutoCloseStream = false, // we own the stream's lifetime via `using`
                    // See note above on PutObjectRequest — R2 doesn't support
                    // the chunked streaming-signature mode the SDK defaults
                    // to for non-seekable streams; disable it here too.
                    DisablePayloadSigning = true,
                };

                var lastLoggedPct = -10;
                uploadReq.UploadProgressEvent += (_, e) =>
                {
                    // Throttle logging to every ~10% to avoid log spam on large files
                    if (e.PercentDone - lastLoggedPct >= 10)
                    {
                        lastLoggedPct = e.PercentDone;
                        logger.LogInformation("Upload progress for {Name}: {Pct}% ({MB:F0}MB of {TotalMB:F0}MB)",
                            file.FileName, e.PercentDone,
                            e.TransferredBytes / 1024.0 / 1024.0,
                            e.TotalBytes / 1024.0 / 1024.0);
                    }
                };

                await transfer.UploadAsync(uploadReq);
            }
            else
            {
                // Small files only (< 25MB) — direct PutObject is simpler
                // and faster than multipart for small payloads.
                await using var stream = file.OpenReadStream();

                var req = new PutObjectRequest
                {
                    BucketName = _bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    CannedACL = S3CannedACL.PublicRead,
                    AutoCloseStream = false,
                    Headers = { ContentLength = file.Length },
                    // Cloudflare R2 doesn't implement the AWS SDK's chunked
                    // streaming-signature mode ("STREAMING-AWS4-HMAC-SHA256-
                    // PAYLOAD not implemented"), which .NET's PutObjectRequest
                    // uses by default for non-seekable streams. Disabling
                    // payload signing makes the SDK compute a single upfront
                    // signature instead — the mode R2 actually supports.
                    DisablePayloadSigning = true,
                    UseChunkEncoding = false,
                };
                await client.PutObjectAsync(req);
            }

            var url = $"{_publicUrl.TrimEnd('/')}/{key}";
            logger.LogInformation("R2 upload complete: {Key} ({SizeMB:F1}MB)", key, file.Length / 1024.0 / 1024.0);
            return new UploadResult(true, url, key);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "R2 S3 error [{Code}] for {Name}", ex.ErrorCode, file.FileName);
            return new UploadResult(false, "", "", $"{ex.ErrorCode}: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "R2 upload failed for {Name}", file.FileName);
            return new UploadResult(false, "", "", ex.Message);
        }
    }

    public async Task<bool> DeleteFileAsync(string fileKey)
    {
        try
        {
            using var client = CreateClient();
            await client.DeleteObjectAsync(_bucket, fileKey);
            logger.LogInformation("Deleted: {Key}", fileKey);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete failed: {Key}", fileKey);
            return false;
        }
    }

    public string GetPublicUrl(string fileKey)
        => $"{_publicUrl.TrimEnd('/')}/{fileKey}";
}