using System.ComponentModel.DataAnnotations;

namespace LMS.API.Models;

public class Organization
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(60)] public string Slug { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? Website { get; set; }
    public string? PortalUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? Tagline { get; set; }
    public string? ThemeFont { get; set; }
    public string? ThemeMode { get; set; } = "light"; // light | dark
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RazorpayKeyId { get; set; }
    public string? RazorpayKeySecret { get; set; }
    public string? Currency { get; set; } = "INR";

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
    public HomePageConfig? HomePageConfig { get; set; }
}

public enum UserRole { SuperAdmin, OrgAdmin, Instructor, Student }

public class User
{
    public int Id { get; set; }
    [Required, MaxLength(60)] public string FirstName { get; set; } = "";
    [Required, MaxLength(60)] public string LastName { get; set; } = "";
    [Required, MaxLength(120)] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Course> CoursesOwned { get; set; } = [];
    public ICollection<Certificate> Certificates { get; set; } = [];
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = [];
    public ICollection<Cart> CartItems { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
    public ICollection<UserDepartment> UserDepartments { get; set; } = [];
}

public class Category
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? IconEmoji { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
}

public enum CourseLevel { Beginner, Intermediate, Advanced }
public enum CourseStatus { Draft, Published, Archived }

public class Course
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public decimal Price { get; set; } = 0;
    public bool IsFree { get; set; } = true;
    public int DurationMinutes { get; set; }
    public string? Tags { get; set; }
    public string? Language { get; set; } = "English";
    public string? Requirements { get; set; }
    public string? WhatYouLearn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int InstructorId { get; set; }
    public User Instructor { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<Module> Modules { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Exam> Exams { get; set; } = [];
    public ICollection<CourseRating> Ratings { get; set; } = [];
    public ICollection<Cart> CartItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

public class Module
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPreview { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = [];
}

public enum LessonType { Video, Article, Quiz, File, Audio, Mixed }

public class Lesson
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    // Legacy single-content fields (kept for backward compat)
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public string? FileUrl { get; set; }
    public LessonType Type { get; set; } = LessonType.Video;
    // Rich content: JSON array of ContentBlock objects
    // [{type,order,data:{...}}]
    public string? ContentBlocksJson { get; set; }
    public int DurationSecs { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPreview { get; set; }
    public bool IsPublished { get; set; } = true;
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;
    public ICollection<LessonProgress> Progresses { get; set; } = [];
    public ICollection<LessonResource> Resources { get; set; } = [];
}

// Downloadable resources attached to a lesson (PDFs, docs, etc.)
public enum ResourceType { PDF, Document, Spreadsheet, Presentation, Audio, Other }

public class LessonResource
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public string? FileKey { get; set; }
    public long FileSizeBytes { get; set; }
    public ResourceType Type { get; set; } = ResourceType.PDF;
    public int DisplayOrder { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}

public enum EnrollmentStatus { Active, Completed, Cancelled }

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

public class LessonProgress
{
    public int Id { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchedSeconds { get; set; }
    public int LastPositionSec { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}

public class Exam
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Instructions { get; set; }
    public int TimeLimitMins { get; set; } = 60;
    public int PassMarkPercent { get; set; } = 60;
    public int MaxAttempts { get; set; } = 3;
    public bool IsPublished { get; set; } = false;
    public bool Randomize { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<ExamAttempt> Attempts { get; set; } = [];
}

public enum QuestionType { SingleChoice, MultiChoice, TrueFalse, ShortAnswer }

public class Question
{
    public int Id { get; set; }
    [Required] public string Text { get; set; } = "";
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public int Marks { get; set; } = 1;
    public string? Explanation { get; set; }
    public int DisplayOrder { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = [];
    public ICollection<Answer> Answers { get; set; } = [];
}

public class QuestionOption
{
    public int Id { get; set; }
    [Required] public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
}

public enum AttemptStatus { InProgress, Submitted, Graded }

public class ExamAttempt
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int? Score { get; set; }
    public int? Marks { get; set; }
    public int? TotalMarks { get; set; }
    public bool Passed { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = [];
    public Certificate? Certificate { get; set; }
}

public class Answer
{
    public int Id { get; set; }
    public string? TextAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int MarksAwarded { get; set; }
    public int ExamAttemptId { get; set; }
    public ExamAttempt ExamAttempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public ICollection<QuestionOption> SelectedOptions { get; set; } = [];
}

public class Certificate
{
    public int Id { get; set; }
    public string CertificateNumber { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? PdfUrl { get; set; }
    public int TotalWatchMinutes { get; set; } // tracked time at certificate issue
    public DateTime? DownloadedAt { get; set; }
    public int DownloadCount { get; set; } = 0;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int? ExamAttemptId { get; set; }
    public ExamAttempt? ExamAttempt { get; set; }
}

public class CourseRating
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Review { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

// ─── CART ──────────────────────────────────────────────────────
public class Cart
{
    public int Id { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

// ─── ORDER / PAYMENT ──────────────────────────────────────────
public enum OrderStatus { Pending, Paid, Failed, Refunded }

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100,999)}";
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

// ─── DEPARTMENT ────────────────────────────────────────────────
public class Department
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? IconEmoji { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<UserDepartment> UserDepartments { get; set; } = [];
}

// User ↔ Department (many-to-many)
public class UserDepartment
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}

// User ↔ Roles (multi-role support)
public class UserRoleAssignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

// ─── HOMEPAGE CONFIG ───────────────────────────────────────────
public class HomePageConfig
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Template selection
    public string TemplateId { get; set; } = "modern"; // modern|bold|minimal|indian|dark

    // Hero section
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroButtonText { get; set; } = "Get Started";
    public string? HeroButtonUrl { get; set; } = "/register";
    public string? HeroImageUrl { get; set; }
    public string? HeroVideoUrl { get; set; }
    public string? HeroStyle { get; set; } = "gradient"; // gradient|image|video|split

    // Section visibility & order (JSON: [{id,enabled,order}])
    public string? SectionsConfig { get; set; }

    // Stats bar
    public bool ShowStats { get; set; } = true;
    public string? StatsCustom { get; set; } // JSON override for stat labels

    // Announcements ticker
    public string? AnnouncementText { get; set; }
    public bool ShowAnnouncement { get; set; } = false;

    // Nav links (JSON: [{label,url,isExternal}])
    public string? NavLinksJson { get; set; }

    // Footer
    public string? FooterTagline { get; set; }
    public string? FooterLinksJson { get; set; } // JSON: [{label,url}]
    public string? FooterSocialJson { get; set; } // JSON: [{platform,url}]
    public string? FooterCopyright { get; set; }
    public bool ShowFooterNewsletter { get; set; } = false;

    // Custom sections (JSON array)
    public string? CustomSectionsJson { get; set; }
    // Raw custom HTML that gets injected before </body> — admin can paste any HTML
    public string? CustomHtml { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ─── PAYMENT TRANSACTION LOG ──────────────────────────────────
public enum PaymentTransactionStatus { Initiated, Pending, Success, Failed, Refunded, PartialRefund }
public enum PaymentMethod { UPI, Card, NetBanking, Wallet, EMI, Unknown }

public class PaymentTransaction
{
    public int Id { get; set; }
    public string TransactionRef    { get; set; } = Guid.NewGuid().ToString("N")[..16].ToUpper();
    public string? RazorpayOrderId  { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public decimal Amount           { get; set; }
    public string Currency          { get; set; } = "INR";
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
    public PaymentMethod Method     { get; set; } = PaymentMethod.Unknown;
    public string? MethodDetail     { get; set; }  // e.g. "HDFC Debit Card", "GPay"
    public string? FailureReason    { get; set; }
    public string? RefundId         { get; set; }
    public decimal? RefundAmount    { get; set; }
    public DateTime? RefundedAt     { get; set; }
    public string? IpAddress        { get; set; }
    public string? UserAgent        { get; set; }
    public string? WebhookPayload   { get; set; }  // raw webhook JSON for audit
    public DateTime InitiatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt    { get; set; }
    public int UserId               { get; set; }
    public User User                { get; set; } = null!;
    public int? OrderId             { get; set; }
    public Order? Order             { get; set; }
}

// ══════════════════════════════════════════════════════════════
//  TRAINER / FACULTY FEATURES
// ══════════════════════════════════════════════════════════════

// ─── ASSIGNMENT ───────────────────────────────────────────────
public enum AssignmentStatus { Draft, Published, Closed }

public class Assignment
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public int MaxMarks { get; set; } = 100;
    public DateTime DueDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public ICollection<AssignmentSubmission> Submissions { get; set; } = [];
}

public enum SubmissionStatus { Pending, Submitted, Graded, Late }

public class AssignmentSubmission
{
    public int Id { get; set; }
    public string? SubmissionText { get; set; }
    public string? FileUrl { get; set; }
    public int? MarksObtained { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? GradedById { get; set; }
    public User? GradedBy { get; set; }
}

// ─── ATTENDANCE ───────────────────────────────────────────────
public enum AttendanceStatus { Present, Absent, Late, Excused }

public class Attendance
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public string? Remarks { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int? MarkedById { get; set; }
    public User? MarkedBy { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
}

// ─── LIVE CLASS / SCHEDULE ────────────────────────────────────
public enum LiveClassStatus { Scheduled, Live, Completed, Cancelled }
public enum LiveClassPlatform { Zoom, GoogleMeet, Teams, YouTube, Custom }

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
    public string? RecordingUrl { get; set; }
    public LiveClassStatus Status { get; set; } = LiveClassStatus.Scheduled;
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
    public bool Attended { get; set; } = false;
    public DateTime? JoinedAt { get; set; }
    public int LiveClassId { get; set; }
    public LiveClass LiveClass { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

// ══════════════════════════════════════════════════════════════
//  MOCK TEST SYSTEM
// ══════════════════════════════════════════════════════════════

public enum MockTestStatus { Draft, Published, Archived }
public enum MockTestDifficulty { Easy, Medium, Hard, Mixed }

public class MockTest
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Topic { get; set; }
    public MockTestDifficulty Difficulty { get; set; } = MockTestDifficulty.Mixed;
    public MockTestStatus Status { get; set; } = MockTestStatus.Draft;
    public int TimeLimitMins { get; set; } = 30;
    public int TotalQuestions { get; set; } = 20;
    public int PassMarkPercent { get; set; } = 60;
    public bool RandomizeQuestions { get; set; } = true;
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

// Question input types
public enum MockQuestionType
{
    SingleChoice,       // Radio buttons — one correct answer
    MultiChoice,        // Checkboxes — multiple correct answers
    Dropdown,           // Dropdown select — one answer
    TrueFalse,          // True/False radio
    ShortAnswer,        // Text input (manual grading)
    Formula,            // Mathematical formula with LaTeX
}

public class MockTestQuestion
{
    public int Id { get; set; }
    [Required] public string Text { get; set; } = "";   // Supports LaTeX: \( \frac{1}{2} \)
    public string? ImageUrl { get; set; }               // Question image
    public string? Explanation { get; set; }            // Shown after submit
    public string? ExplanationImageUrl { get; set; }    // Explanation image
    public string? FormulaLatex { get; set; }           // Raw LaTeX for formula questions
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
    [Required] public string Text { get; set; } = "";   // Supports LaTeX inline
    public string? ImageUrl { get; set; }               // Option image (optional)
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }
    public int QuestionId { get; set; }
    public MockTestQuestion Question { get; set; } = null!;
}

public enum MockAttemptStatus { InProgress, Completed, Abandoned }

public class MockTestAttempt
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TimeTakenSecs { get; set; }
    public int TotalMarks { get; set; }
    public int MarksObtained { get; set; }
    public int NegativeMarks { get; set; }
    public int ScorePercent { get; set; }
    public int Rank { get; set; }
    public bool Passed { get; set; }
    public MockAttemptStatus Status { get; set; } = MockAttemptStatus.InProgress;
    public int AttemptNumber { get; set; } = 1;
    // Interview readiness: 80+=Ready, 60-80=NeedsPractice, <60=Weak
    public string InterviewReadiness { get; set; } = "Weak";
    public int MockTestId { get; set; }
    public MockTest MockTest { get; set; } = null!;
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public ICollection<MockTestAnswer> Answers { get; set; } = [];
    public ICollection<TopicScore> TopicScores { get; set; } = [];
}

public class MockTestAnswer
{
    public int Id { get; set; }
    public bool IsCorrect { get; set; }
    public bool IsSkipped { get; set; }
    public int MarksAwarded { get; set; }
    public int TimeTakenSecs { get; set; }
    // Single choice: SelectedOptionId used
    public int? SelectedOptionId { get; set; }
    public MockTestOption? SelectedOption { get; set; }
    // Multi-choice: comma-separated option IDs e.g. "3,7,12"
    public string? SelectedOptionIds { get; set; }
    // Short answer text
    public string? TextAnswer { get; set; }
    public int AttemptId { get; set; }
    public MockTestAttempt Attempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public MockTestQuestion Question { get; set; } = null!;
}

// Per-topic breakdown score
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

// ─── INTERVIEW NOTIFICATION ───────────────────────────────────
public enum InterviewStatus { Scheduled, Completed, Cancelled, Rescheduled }

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
