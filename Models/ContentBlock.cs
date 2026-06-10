using System.Text.Json;
using System.Text.Json.Serialization;

namespace LMS.API.Models;

// ─── Content block types ──────────────────────────────────────
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlockType
{
    Heading,    // h1/h2/h3 heading text
    Text,       // rich paragraph text (markdown supported)
    Image,      // image with optional caption
    Video,      // video file URL (R2)
    Audio,      // audio file URL (R2)
    PDF,        // PDF embed/viewer
    File,       // downloadable file link
    Divider,    // horizontal rule
    Callout,    // highlighted info/warning box
    Code,       // code snippet with syntax highlight
}

// Base class — all blocks have type + order
public class ContentBlock
{
    public int      Order { get; set; }
    public BlockType Type  { get; set; }

    // ─── Heading ──────────────────────────────────────────────
    public string? HeadingText  { get; set; }
    public int     HeadingLevel { get; set; } = 2;  // 1, 2, 3

    // ─── Text ─────────────────────────────────────────────────
    public string? TextContent  { get; set; }       // Markdown or plain text

    // ─── Image ────────────────────────────────────────────────
    public string? ImageUrl     { get; set; }
    public string? ImageCaption { get; set; }
    public string? ImageAlt     { get; set; }
    public string  ImageAlign   { get; set; } = "center";  // left|center|full

    // ─── Video ────────────────────────────────────────────────
    public string? VideoUrl     { get; set; }
    public string? VideoTitle   { get; set; }
    public int     VideoDurationSecs { get; set; }
    public string? VideoThumbnail    { get; set; }
    public bool    AutoPlay     { get; set; } = false;

    // ─── Audio ────────────────────────────────────────────────
    public string? AudioUrl     { get; set; }
    public string? AudioTitle   { get; set; }
    public int     AudioDurationSecs { get; set; }

    // ─── PDF / File ───────────────────────────────────────────
    public string? FileUrl      { get; set; }
    public string? FileName     { get; set; }
    public long    FileSizeBytes { get; set; }
    public bool    EmbedPdf     { get; set; } = true;

    // ─── Callout ──────────────────────────────────────────────
    public string? CalloutText  { get; set; }
    public string  CalloutStyle { get; set; } = "info";  // info|warning|success|danger

    // ─── Code ─────────────────────────────────────────────────
    public string? CodeContent  { get; set; }
    public string  CodeLanguage { get; set; } = "plaintext";
}

// Helper: parse/serialize content blocks JSON
public static class ContentBlocks
{
    // Write options: camelCase for API responses
    static readonly JsonSerializerOptions WriteOpts = new()
    {
        PropertyNamingPolicy  = JsonNamingPolicy.CamelCase,
        WriteIndented         = false,
        Converters            = { new JsonStringEnumConverter() }  // PascalCase: 'Video' not 'video'
    };

    // Read options: case-insensitive to handle both old PascalCase and new camelCase stored JSON
    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters                  = { new JsonStringEnumConverter() }
    };

    public static List<ContentBlock> Parse(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<ContentBlock>>(json, ReadOpts) ?? []; }
        catch { return []; }
    }

    public static string Serialize(List<ContentBlock> blocks)
        => JsonSerializer.Serialize(blocks, WriteOpts);
}
