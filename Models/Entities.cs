using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// ─── ENUMS ────────────────────────────────────────────────────────────────────
public enum UserRole { SuperAdmin, OrgAdmin, Instructor, Student }
public enum LessonType { Video, Article, Quiz, File, Audio, Mixed }
public enum EnrollmentStatus { Active, Completed, Dropped, Cancelled }
public enum LiveClassStatus { Scheduled, Live, Completed, Cancelled }
public enum InterviewStatus { Scheduled, Completed, Cancelled, Rescheduled }
public enum BatchStatus { Active, Completed, Cancelled, Upcoming }
public enum BatchStudentStatus { Active, Dropped, Completed }
public enum PaymentStatus { Free, FullyPaid, PartiallyPaid, Pending }

[JsonConverter(typeof(JsonStringEnumConverter))]

// ─── ADDITIONAL ENUMS ─────────────────────────────────────────────────────────
public enum MockTestDifficulty { Easy, Medium, Hard, Mixed }
public enum ResourceType { Video, PDF, Document, Image, Link, Other }

public enum BlockType { Heading, Text, Image, Video, Audio, Pdf, PDF, File, Callout, Code, Divider }

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
    public string? Currency { get; set; } = "INR";
    public string? RazorpayKeyId { get; set; }
    public string? SignatureUrl { get; set; }
    public string? AuthorizedBy { get; set; }
    public string? AuthorizedTitle { get; set; }
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
    public string? AboutUsTemplate { get; set; } = "classic";   // classic | split | timeline | card
    public string? ContactUsTemplate { get; set; } = "classic";   // classic | split | minimal | map-focus

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
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
    public ICollection<UserDepartment> UserDepartments { get; set; } = [];
}

// ─── USER ROLE ASSIGNMENT ─────────────────────────────────────────────────────
public class UserRoleAssignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

// ─── CATEGORY ─────────────────────────────────────────────────────────────────
public class Category
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? IconEmoji { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
}

// ─── DEPARTMENT ───────────────────────────────────────────────────────────────
public class Department
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconEmoji { get; set; } = "🏢";
    public string? Color { get; set; } = "#6366f1";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<UserDepartment> UserDepartments { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
}

// ─── USER DEPARTMENT (explicit join table) ────────────────────────────────────
public class UserDepartment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

// ─── COURSE ───────────────────────────────────────────────────────────────────
public enum CourseLevel { Beginner, Intermediate, Advanced, Expert, AllLevels }

public class Course
{
    public int Id { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public bool IsFree { get; set; } = true;
    public decimal Price { get; set; } = 0;
    public bool IsPublished { get; set; } = false;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public int DurationMinutes { get; set; } = 0;
    public string? Tags { get; set; }
    public string? Language { get; set; } = "English";
    public float AverageRating { get; set; } = 0;
    public int EnrollmentCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? InstructorId { get; set; }
    public User? Instructor { get; set; }
    // When true, students must complete each lesson (reach the watch-time
    // threshold) before the next lesson in sequence becomes playable. Set
    // once per course at creation/edit time — not a per-module or
    // org-wide setting.
    public bool EnforceSequentialLessons { get; set; } = false;
    public ICollection<Module> Modules { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<CourseRating> Ratings { get; set; } = [];
}

public class CourseRating
{
    public int Id { get; set; }
    public int Rating { get; set; } = 5;
    public string? Review { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

// ─── MODULE ───────────────────────────────────────────────────────────────────
public class Module
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int Order { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPreview { get; set; } = false;
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
    public string? FileUrl { get; set; }
    public string? Content { get; set; }
    public int DurationSecs { get; set; }  // non-nullable, controller should use ?? 0
    public int Order { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFree { get; set; } = false;
    public bool IsPreview { get; set; } = false;
    public bool IsPublished { get; set; } = true;
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
    public string? FileKey { get; set; }
    public long FileSizeBytes { get; set; }
    public ResourceType Type { get; set; } = ResourceType.Other;
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
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ─── ENROLLMENT ───────────────────────────────────────────────────────────────
public class Enrollment
{
    public int Id { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public int ProgressPercent { get; set; } = 0;
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
    public string? PdfUrl { get; set; }
    public int TotalWatchMinutes { get; set; } = 0;
    public int? ExamAttemptId { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }
    public DateTime? DownloadedAt { get; set; }
    public int DownloadCount { get; set; } = 0;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}

// ─── MOCK TEST ────────────────────────────────────────────────────────────────
public enum MockTestStatus { Draft, Published, Archived }
public enum MockAttemptStatus { InProgress, Completed, Abandoned }
public enum MockQuestionType { SingleChoice, MultipleChoice, MultiChoice, TrueFalse, ShortAnswer, Dropdown, Formula }

public class MockTest
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Topic { get; set; }
    public MockTestDifficulty Difficulty { get; set; } = MockTestDifficulty.Medium;
    public MockTestStatus Status { get; set; } = MockTestStatus.Draft;
    public int TimeLimitMins { get; set; } = 30;
    public int TotalQuestions { get; set; } = 0;
    public int PassMarkPercent { get; set; } = 60;
    public bool RandomizeQuestions { get; set; } = false;
    public bool ShowResultImmediately { get; set; } = true;
    public int MaxAttempts { get; set; } = 3;
    public string? Tags { get; set; }
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
    public string Text { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Explanation { get; set; }
    public string? ExplanationImageUrl { get; set; }
    public string? FormulaLatex { get; set; }
    public string Topic { get; set; } = "General";
    public MockTestDifficulty Difficulty { get; set; } = MockTestDifficulty.Medium;
    public MockQuestionType QuestionType { get; set; } = MockQuestionType.SingleChoice;
    public int Marks { get; set; } = 1;
    public int NegativeMarks { get; set; } = 0;
    public int DisplayOrder { get; set; }
    public int MockTestId { get; set; }
    public MockTest MockTest { get; set; } = null!;
    public ICollection<MockTestOption> Options { get; set; } = [];
}

public class MockTestOption
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string? ImageUrl { get; set; }
    public bool IsCorrect { get; set; } = false;
    public int DisplayOrder { get; set; }
    public int QuestionId { get; set; }
    public MockTestQuestion Question { get; set; } = null!;
}

public class MockTestAttempt
{
    public int Id { get; set; }
    public MockAttemptStatus Status { get; set; } = MockAttemptStatus.InProgress;
    public int MarksObtained { get; set; }
    public int NegativeMarks { get; set; }
    public int TotalMarks { get; set; }
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }
    public string InterviewReadiness { get; set; } = "Average";
    public int TimeTakenSecs { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public int Rank { get; set; }
    public string? AnswersJson { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int UserId { get; set; }
    public int StudentId { get; set; }   // alias for UserId
    public User User { get; set; } = null!;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public User? Student => User;         // alias - not mapped
    public int MockTestId { get; set; }
    public MockTest MockTest { get; set; } = null!;
    public ICollection<TopicScore> TopicScores { get; set; } = [];
    public ICollection<MockTestAnswer> Answers { get; set; } = [];
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
public enum AssignmentStatus { Draft, Published, Closed }
public enum SubmissionStatus { NotSubmitted, Submitted, Late, Graded, Rejected, ResubmitRequested }

public class Assignment
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public int MaxMarks { get; set; } = 100;
    public DateTime DueDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Published;
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
    public string? SubmissionText { get; set; }
    public string? FileUrl { get; set; }
    public int? MarksObtained { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GradedAt { get; set; }
    public int? GradedById { get; set; }
    public User? GradedBy { get; set; }
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
    public string? RecordingUrl { get; set; }
    public bool EmailSent { get; set; } = false;
    public bool ReminderSent { get; set; } = false;
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

// ─── CART ────────────────────────────────────────────────────────────────────
public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

// ─── PAYMENT / ORDERS ────────────────────────────────────────────────────────
public enum OrderStatus { Pending, Paid, Failed, Refunded, Cancelled }
public enum PaymentTransactionStatus { Initiated, Success, Failed, Refunded, PartialRefund }
public enum CourseStatus { Draft, Published, Archived }

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
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
    public string TransactionRef { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
    public PaymentMethod Method { get; set; } = PaymentMethod.Unknown;
    public string? MethodDetail { get; set; }
    public string? FailureReason { get; set; }
    public string? RefundId { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
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
    public int VideoDurationSecs { get; set; }
    // Audio
    public string? AudioUrl { get; set; }
    public string? AudioTitle { get; set; }
    public int AudioDurationSecs { get; set; }
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

// ─── ATTENDANCE ───────────────────────────────────────────────────────────────
public enum AttendanceStatus { Present, Absent, Late, Excused }

public class Attendance
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public string? Remarks { get; set; }
    public int? MarkedById { get; set; }
    public User? MarkedBy { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
}

// ─── PAYMENT METHOD ───────────────────────────────────────────────────────────
public enum PaymentMethod { Unknown, UPI, Card, NetBanking, Wallet, EMI, Cash, BankTransfer, Other }

// ─── EXAM (legacy quiz system) ────────────────────────────────────────────────
public enum AttemptStatus { InProgress, Completed, Abandoned, TimedOut, Graded }
public enum QuestionType { SingleChoice, MultipleChoice, TrueFalse, ShortAnswer }

public class Exam
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int TimeLimitMins { get; set; } = 60;
    public int DurationMinutes { get; set; } = 60;
    public int PassMarkPercent { get; set; } = 50;
    public int MaxAttempts { get; set; } = 3;
    public bool Randomize { get; set; } = false;
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public ICollection<ExamQuestion> Questions { get; set; } = [];
    public ICollection<ExamAttempt> Attempts { get; set; } = [];
}

public class ExamQuestion
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string QuestionText { get; set; } = "";  // legacy alias
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public string OptionsJson { get; set; } = "[]";
    public int CorrectOptionIndex { get; set; }
    public string? Explanation { get; set; }
    public int Marks { get; set; } = 1;
    public int Order { get; set; }
    public int DisplayOrder { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = [];
}

// Question is a subclass of ExamQuestion for controller compatibility
// db.Questions maps to the ExamQuestions table (TPH inheritance)
public class Question : ExamQuestion { }

public class QuestionOption
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; } = false;
    public int DisplayOrder { get; set; }
    public int QuestionId { get; set; }
    public ExamQuestion Question { get; set; } = null!;
}

public class Answer
{
    public int Id { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? TextAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int MarksAwarded { get; set; }
    public int AttemptId { get; set; }
    public int ExamAttemptId { get; set; }   // alias for AttemptId
    public ExamAttempt Attempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public ExamQuestion Question { get; set; } = null!;  // maps to ExamQuestions table
}

public class ExamAttempt
{
    public int Id { get; set; }
    public int? Score { get; set; }
    public int? Marks { get; set; }
    public int TotalMarks { get; set; }
    public float Percentage { get; set; }
    public bool Passed { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public string? AnswersJson { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = [];
}

// ─── HOMEPAGE CONFIG ──────────────────────────────────────────────────────────
public class HomePageConfig
{
    public int Id { get; set; }
    public string TemplateId { get; set; } = "default";
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroButtonText { get; set; }
    public string? HeroButtonUrl { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? HeroVideoUrl { get; set; }
    public string? HeroStyle { get; set; }
    public string? SectionsConfig { get; set; }
    public bool ShowStats { get; set; } = true;
    public string? StatsCustom { get; set; }
    public string? AnnouncementText { get; set; }
    public bool ShowAnnouncement { get; set; } = false;
    public string? NavLinksJson { get; set; }
    public string? FooterTagline { get; set; }
    public string? FooterLinksJson { get; set; }
    public string? FooterSocialJson { get; set; }
    public string? FooterCopyright { get; set; }
    public bool ShowFooterNewsletter { get; set; } = false;
    public string? CustomSectionsJson { get; set; }
    public string? CustomHtml { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}

// ─── MOCK TEST ANSWER ─────────────────────────────────────────────────────────
public class MockTestAnswer
{
    public int Id { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? SelectedOptionIds { get; set; }   // comma-separated e.g. "1,3,5"
    public MockTestOption? SelectedOption { get; set; } // FK navigation
    public string? TextAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsSkipped { get; set; }
    public int MarksAwarded { get; set; }
    public int AttemptId { get; set; }
    public MockTestAttempt Attempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public MockTestQuestion Question { get; set; } = null!;
}