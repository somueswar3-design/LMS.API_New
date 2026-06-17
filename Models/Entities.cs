using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// ─── ENUMS ────────────────────────────────────────────────────────────────────
public enum UserRole { SuperAdmin, OrgAdmin, Instructor, Student }
public enum LessonType { Video, Article, Quiz, File, Audio, Mixed }
public enum EnrollmentStatus { Active, Completed, Dropped }
public enum LiveClassStatus { Scheduled, Live, Completed, Cancelled }
public enum InterviewStatus { Scheduled, Completed, Cancelled, Rescheduled }
public enum BatchStatus { Active, Completed, Cancelled, Upcoming }
public enum BatchStudentStatus { Active, Dropped, Completed }
public enum PaymentStatus { Free, FullyPaid, PartiallyPaid, Pending }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlockType { Heading, Text, Image, Video, Audio, Pdf, File, Callout, Code, Divider }

// ─── ORGANIZATION ─────────────────────────────────────────────────────────────
public class Organization
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(100)] public string Slug { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Tagline { get; set; }
    public string? PrimaryColor { get; set; } = "#6366f1";
    public string? SecondaryColor { get; set; } = "#8b5cf6";
    public string? AccentColor { get; set; } = "#f59e0b";
    public string? ThemeFont { get; set; } = "Inter";
    public string? Website { get; set; }
    public string? PortalUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Homepage feature flags ─────────────────────────────────────────────────
    public bool ShowScrollingBanner { get; set; } = false;
    public string? ScrollingBannerText { get; set; }   // pipe-separated e.g. "Batch Open|Enroll Now"
    public bool ShowReferralOffer { get; set; } = false;
    public string? ReferralOfferText { get; set; }   // e.g. "Earn ₹2500 per referral"
    public bool ShowCourseBatches { get; set; } = false;
    public bool ShowAllCourses { get; set; } = true;
    public bool ShowContactUs { get; set; } = true;
    public bool ShowAboutUs { get; set; } = true;
    public bool ShowOpenings { get; set; } = false;

    // ── Content ────────────────────────────────────────────────────────────────
    public string? AboutUsContent { get; set; }   // HTML
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? ContactMapEmbed { get; set; }   // Google Maps iframe src
    public string? OpeningsContent { get; set; }   // HTML
    public string? CustomMenuJson { get; set; }   // JSON array [{label,url,isPage,pageContent}]

    // Navigation
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
}

// ─── USER ─────────────────────────────────────────────────────────────────────
public class User
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName { get; set; } = "";
    [Required, MaxLength(200)] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
}

// ─── USER ROLE ASSIGNMENT ─────────────────────────────────────────────────────
public class UserRoleAssignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public UserRole Role { get; set; }
}

// ─── CATEGORY ─────────────────────────────────────────────────────────────────
public class Category
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = [];
}

// ─── DEPARTMENT ───────────────────────────────────────────────────────────────
public class Department
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<User> Users { get; set; } = [];
}

// ─── COURSE ───────────────────────────────────────────────────────────────────
public class Course
{
    public int Id { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Level { get; set; }   // Beginner, Intermediate, Advanced
    public bool IsFree { get; set; } = true;
    public decimal Price { get; set; } = 0;
    public bool IsPublished { get; set; } = false;
    public float AverageRating { get; set; } = 0;
    public int EnrollmentCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? InstructorId { get; set; }
    public User? Instructor { get; set; }
    public ICollection<Module> Modules { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}

// ─── MODULE ───────────────────────────────────────────────────────────────────
public class Module
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int Order { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = [];
}

// ─── LESSON ───────────────────────────────────────────────────────────────────
public class Lesson
{
    public int Id { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public LessonType Type { get; set; } = LessonType.Video;
    public string? VideoUrl { get; set; }
    public string? Content { get; set; }
    public int DurationSecs { get; set; }
    public int Order { get; set; }
    public bool IsFree { get; set; } = false;
    public string? ContentBlocksJson { get; set; }
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public ICollection<LessonProgress> Progresses { get; set; } = [];
    public ICollection<LessonResource> Resources { get; set; } = [];
}

// ─── LESSON RESOURCE ──────────────────────────────────────────────────────────
public class LessonResource
{
    public int Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public long FileSizeBytes { get; set; }
    [MaxLength(50)] public string Type { get; set; } = "file";
    public int DisplayOrder { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}

// ─── LESSON PROGRESS ──────────────────────────────────────────────────────────
public class LessonProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public bool IsCompleted { get; set; } = false;
    public int WatchedSeconds { get; set; } = 0;
    public int LastPositionSec { get; set; } = 0;
    public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
}

// ─── ENROLLMENT ───────────────────────────────────────────────────────────────
public class Enrollment
{
    public int Id { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public float ProgressPercent { get; set; } = 0;
    public int TotalWatchSeconds { get; set; } = 0;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

// ─── CERTIFICATE ──────────────────────────────────────────────────────────────
public class Certificate
{
    public int Id { get; set; }
    [MaxLength(100)] public string CertificateNumber { get; set; } = "";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}

// ─── MOCK TEST ────────────────────────────────────────────────────────────────
public class MockTest
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int TimeLimitMins { get; set; } = 30;
    public int PassMarkPercent { get; set; } = 60;
    public bool RandomizeQuestions { get; set; } = false;
    public string? Difficulty { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public ICollection<MockTestQuestion> Questions { get; set; } = [];
    public ICollection<MockTestAttempt> Attempts { get; set; } = [];
}

public class MockTestQuestion
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = "";
    public string OptionsJson { get; set; } = "[]";
    public int CorrectOptionIndex { get; set; }
    public string? Explanation { get; set; }
    public int Marks { get; set; } = 1;
    public string? Topic { get; set; }
    public int Order { get; set; }
    public int MockTestId { get; set; }
    public MockTest MockTest { get; set; } = null!;
}

public class MockTestAttempt
{
    public int Id { get; set; }
    public int Score { get; set; }
    public int TotalMarks { get; set; }
    public float Percentage { get; set; }
    public bool Passed { get; set; }
    public string? AnswersJson { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int MockTestId { get; set; }
    public MockTest MockTest { get; set; } = null!;
    public ICollection<TopicScore> TopicScores { get; set; } = [];
}

public class TopicScore
{
    public int Id { get; set; }
    public string Topic { get; set; } = "";
    public int TotalQuestions { get; set; }
    public int Correct { get; set; }
    public int ScorePercent { get; set; }
    public int AttemptId { get; set; }
    public MockTestAttempt Attempt { get; set; } = null!;
}

// ─── ASSIGNMENT ───────────────────────────────────────────────────────────────
public class Assignment
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public int TotalMarks { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public ICollection<AssignmentSubmission> Submissions { get; set; } = [];
}

public class AssignmentSubmission
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public bool IsLate { get; set; } = false;
    public int? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
}

// ─── LIVE CLASS ───────────────────────────────────────────────────────────────
public enum LiveClassPlatform { Zoom, GoogleMeet, MicrosoftTeams, Webex, Other }

public class LiveClass
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public LiveClassPlatform Platform { get; set; } = LiveClassPlatform.Zoom;
    public string? MeetingLink { get; set; }
    public string? MeetingId { get; set; }
    public string? MeetingPassword { get; set; }
    public LiveClassStatus Status { get; set; } = LiveClassStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int HostId { get; set; }
    public User Host { get; set; } = null!;
    public ICollection<LiveClassAttendee> Attendees { get; set; } = [];
}

public class LiveClassAttendee
{
    public int Id { get; set; }
    public int LiveClassId { get; set; }
    public LiveClass LiveClass { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

// ─── INTERVIEW ────────────────────────────────────────────────────────────────
public class InterviewSchedule
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? Platform { get; set; }
    public string? MeetingLink { get; set; }
    public string? InterviewerName { get; set; }
    public string? InterviewerEmail { get; set; }
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public string? Notes { get; set; }
    public string? Feedback { get; set; }
    public bool EmailSent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}

// ─── PAYMENT / ORDERS ────────────────────────────────────────────────────────
public class Order
{
    public int Id { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public decimal Price { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

public class PaymentTransaction
{
    public int Id { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
}

// ─── CONTENT BLOCKS ───────────────────────────────────────────────────────────
public class ContentBlock
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BlockType Type { get; set; }
    public int Order { get; set; }
    // Heading
    public string? HeadingText { get; set; }
    public int? HeadingLevel { get; set; }
    // Text
    public string? TextContent { get; set; }
    // Image
    public string? ImageUrl { get; set; }
    public string? ImageCaption { get; set; }
    public string? ImageAlt { get; set; }
    public string? ImageAlign { get; set; }
    // Video
    public string? VideoUrl { get; set; }
    public string? VideoTitle { get; set; }
    public int? VideoDurationSecs { get; set; }
    // Audio
    public string? AudioUrl { get; set; }
    public string? AudioTitle { get; set; }
    public int? AudioDurationSecs { get; set; }
    // File / PDF
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    public bool? EmbedPdf { get; set; }
    // Callout
    public string? CalloutText { get; set; }
    public string? CalloutStyle { get; set; }
    // Code
    public string? CodeContent { get; set; }
    public string? CodeLanguage { get; set; }
}

public static class ContentBlocks
{
    private static readonly System.Text.Json.JsonSerializerOptions _opts = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static List<ContentBlock> Parse(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return System.Text.Json.JsonSerializer.Deserialize<List<ContentBlock>>(json, _opts) ?? []; }
        catch { return []; }
    }

    public static string Serialize(List<ContentBlock> blocks)
        => System.Text.Json.JsonSerializer.Serialize(blocks, _opts);
}

// ─── TRAINING BATCH ───────────────────────────────────────────────────────────
public class TrainingBatch
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string BatchName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; } = 30;
    public DateTime EndDate => StartDate.AddDays(DurationDays);
    public BatchStatus Status { get; set; } = BatchStatus.Upcoming;
    public decimal TotalFee { get; set; } = 0;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public ICollection<BatchStudent> Students { get; set; } = [];
}

public class BatchStudent
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public TrainingBatch Batch { get; set; } = null!;
    public int? UserId { get; set; }
    public User? User { get; set; }
    // Guest student info
    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestMobile { get; set; }
    // Payment
    public decimal TotalFee { get; set; } = 0;
    public decimal PaidAmount { get; set; } = 0;
    public decimal PendingAmount => TotalFee - PaidAmount;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public BatchStudentStatus Status { get; set; } = BatchStudentStatus.Active;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

// ─── BATCH RESOURCE ───────────────────────────────────────────────────────────
public class BatchResource
{
    public int Id { get; set; }
    public int LiveClassId { get; set; }
    public LiveClass LiveClass { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    public string FileUrl { get; set; } = "";
    [MaxLength(50)] public string FileType { get; set; } = "file";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ─── BATCH ENQUIRY ────────────────────────────────────────────────────────────
public class BatchEnquiry
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public string? CourseInterest { get; set; }
    public int? BatchId { get; set; }
    public TrainingBatch? Batch { get; set; }
    public int OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}