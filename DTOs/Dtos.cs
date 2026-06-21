using LMS.API.Models;

namespace LMS.API.DTOs;

// ─── AUTH ──────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string RefreshToken, UserDto User);
public record RegisterRequest(string FirstName, string LastName, string Email, string Password, int OrganizationId);

// ─── ORGANIZATION ──────────────────────────────────────────────────────────────
public record OrganizationDto(
    int Id, string Name, string Slug, string? LogoUrl, string? BannerUrl, string? Tagline,
    string? PrimaryColor, string? SecondaryColor, string? AccentColor,
    string? ThemeFont, string? Website, string? PortalUrl,
    bool IsActive, DateTime CreatedAt, int UserCount, int CourseCount,
    // Homepage feature flags
    bool ShowScrollingBanner = false, string? ScrollingBannerText = null,
    bool ShowReferralOffer = false, string? ReferralOfferText = null,
    bool ShowCourseBatches = false, bool ShowAllCourses = true,
    bool ShowContactUs = true, bool ShowAboutUs = true,
    bool ShowOpenings = false,
    // Content
    string? AboutUsContent = null, string? ContactEmail = null,
    string? ContactPhone = null, string? ContactAddress = null,
    string? ContactMapEmbed = null, string? OpeningsContent = null,
    string? CustomMenuJson = null,
    // Template selection
    string? AboutUsTemplate = "classic", string? ContactUsTemplate = "classic"
);
public record CreateOrganizationRequest(string Name, string? Website, string? PrimaryColor, string? PortalUrl);
public record UpdateOrganizationRequest(
    string? Name, string? Website, string? PrimaryColor, string? SecondaryColor,
    string? AccentColor, string? LogoUrl, string? BannerUrl, string? Tagline,
    string? ThemeFont, string? PortalUrl, bool? IsActive,
    // Homepage feature flags
    bool? ShowScrollingBanner = null, string? ScrollingBannerText = null,
    bool? ShowReferralOffer = null, string? ReferralOfferText = null,
    bool? ShowCourseBatches = null, bool? ShowAllCourses = null,
    bool? ShowContactUs = null, bool? ShowAboutUs = null,
    bool? ShowOpenings = null,
    // Content
    string? AboutUsContent = null, string? ContactEmail = null,
    string? ContactPhone = null, string? ContactAddress = null,
    string? ContactMapEmbed = null, string? OpeningsContent = null,
    string? CustomMenuJson = null,
    // Template selection
    string? AboutUsTemplate = null, string? ContactUsTemplate = null
);

// ─── USER ──────────────────────────────────────────────────────────────────────
public record UserDto(int Id, string FirstName, string LastName, string Email, string? AvatarUrl, string Role, bool IsActive, DateTime CreatedAt, DateTime? LastLogin, int OrganizationId, string OrganizationName);
public record CreateUserRequest(string FirstName, string LastName, string Email, string Password, string Role, int OrganizationId);
public record UpdateUserRequest(string? FirstName, string? LastName, string? PhoneNumber, string? AvatarUrl, bool? IsActive);
public record CreateUserMultiRoleRequest(string FirstName, string LastName, string Email, string Password, List<string> Roles, int OrganizationId, List<int>? DepartmentIds = null);
public record UpdateUserRolesRequest(List<string> Roles, List<int>? DepartmentIds = null);

// ─── CATEGORY ──────────────────────────────────────────────────────────────────
public record CategoryDto(int Id, string Name, string? Description, int? ParentId, string? ParentName, int DisplayOrder, bool IsActive, List<CategoryDto> Children, int CourseCount);
public record CreateCategoryRequest(string Name, string? Description, int? ParentId, int OrganizationId, int? DepartmentId = null, int DisplayOrder = 0);
public record UpdateCategoryRequest(string? Name, string? Description, int? DisplayOrder, bool? IsActive);

// ─── COURSE ────────────────────────────────────────────────────────────────────
public record CourseDto(
    int Id, string Title, string? Description, string? ThumbnailUrl,
    string Level, string Status, decimal Price, bool IsFree,
    int DurationMinutes, string? Tags, string? Language,
    int OrganizationId, string OrganizationName,
    int? InstructorId, string? InstructorName,
    int? CategoryId, string? CategoryName,
    int EnrollmentCount, double AverageRating, int RatingCount,
    DateTime CreatedAt, DateTime UpdatedAt,
    List<ModuleDto>? Modules,
    bool EnforceSequentialLessons = false
);
public record CreateCourseRequest(string Title, string? Description, string? ThumbnailUrl, string Level, decimal Price, bool IsFree, int CategoryId, int InstructorId, int OrganizationId, string? Tags, string? Language = "English", bool EnforceSequentialLessons = false);
public record UpdateCourseRequest(string? Title, string? Description, string? ThumbnailUrl, string? Level, string? Status, decimal? Price, bool? IsFree, int? CategoryId, string? Tags, bool? EnforceSequentialLessons = null);

// ─── MODULE ────────────────────────────────────────────────────────────────────
public record ModuleDto(int Id, string Title, string? Description, int DisplayOrder, bool IsPreview, int CourseId, List<object>? Lessons);
public record CreateModuleRequest(string Title, string? Description, int CourseId, int DisplayOrder = 0, bool IsPreview = false);
public record UpdateModuleRequest(string? Title, string? Description, int? DisplayOrder, bool? IsPreview);

// ─── LESSON ────────────────────────────────────────────────────────────────────
public record CreateLessonRequest(string Title, string? Content, string? VideoUrl, string? FileUrl, string Type, int? DurationSecs, int ModuleId, int DisplayOrder = 0, bool IsPreview = false);
public record UpdateLessonRequest(string? Title, string? Content, string? VideoUrl, string? FileUrl, int? DurationSecs, int? DisplayOrder, bool? IsPublished);
public record LessonProgressDto(int LessonId, bool IsCompleted, int WatchedSeconds, int LastPositionSec, DateTime UpdatedAt);
public record UpdateProgressRequest(int LessonId, int WatchedSeconds, int LastPositionSec, bool IsCompleted);

// ─── ENROLLMENT ────────────────────────────────────────────────────────────────
public record EnrollmentDto(int Id, int UserId, string UserName, int CourseId, string CourseTitle, DateTime EnrolledAt, DateTime? CompletedAt, string Status, int ProgressPercent, int TotalWatchSeconds = 0);
public record EnrollRequest(int UserId, int CourseId);

// ─── EXAM ──────────────────────────────────────────────────────────────────────
public record ExamDto(int Id, string Title, string? Instructions, int TimeLimitMins, int PassMarkPercent, int MaxAttempts, bool IsPublished, bool Randomize, int CourseId, string CourseTitle, List<QuestionDto>? Questions);
public record CreateExamRequest(string Title, string? Instructions, int CourseId, int TimeLimitMins = 60, int PassMarkPercent = 60, int MaxAttempts = 3, bool Randomize = false);
public record UpdateExamRequest(string? Title, string? Instructions, int? TimeLimitMins, int? PassMarkPercent, int? MaxAttempts, bool? IsPublished, bool? Randomize);
public record QuestionDto(int Id, string Text, string Type, int Marks, string? Explanation, int DisplayOrder, List<OptionDto>? Options);
public record CreateQuestionRequest(string Text, string Type, int Marks, int ExamId, string? Explanation, int DisplayOrder = 0, List<CreateOptionRequest>? Options = null);
public record UpdateQuestionRequest(string? Text, int? Marks, string? Explanation, int? DisplayOrder);
public record OptionDto(int Id, string Text, bool IsCorrect, int DisplayOrder);
public record CreateOptionRequest(string Text, bool IsCorrect, int DisplayOrder = 0);
public record ExamAttemptDto(int Id, int UserId, string UserName, int ExamId, string ExamTitle, DateTime StartedAt, DateTime? SubmittedAt, int? Score, int? Marks, int? TotalMarks, bool Passed, string Status);
public record StartAttemptRequest(int ExamId, int UserId);
public record SubmitAttemptRequest(int AttemptId, List<SubmitAnswerRequest> Answers);
public record SubmitAnswerRequest(int QuestionId, string? TextAnswer, List<int>? SelectedOptionIds);

// ─── CERTIFICATE ───────────────────────────────────────────────────────────────
public record CertificateDto(
    int Id, string CertificateNumber, DateTime IssuedAt, string? PdfUrl,
    int UserId, string UserName, string? UserEmail,
    int CourseId, string CourseTitle, string? CourseLevel, string? CourseLanguage,
    int TotalWatchMinutes,
    string OrgName, string? OrgLogoUrl, string? OrgSignatureUrl,
    string? AuthorizedBy, string? AuthorizedTitle
);
public record CertificateDownloadDto(int Id, string CertificateNumber, DateTime IssuedAt, DateTime? DownloadedAt, int DownloadCount, int TotalWatchMinutes, string UserName, string CourseTitle);

// ─── DASHBOARD ─────────────────────────────────────────────────────────────────
public record AdminDashboardDto(int TotalOrgs, int TotalUsers, int TotalCourses, int TotalEnrollments, List<RecentActivityDto> RecentActivity);
public record OrgDashboardDto(int TotalUsers, int TotalCourses, int TotalEnrollments, int ActiveStudents, double CompletionRate, List<CourseStatsDto> TopCourses);
public record StudentDashboardDto(int EnrolledCourses, int CompletedCourses, int CertificatesEarned, int TotalWatchMinutes, List<EnrollmentDto> ActiveEnrollments);
public record RecentActivityDto(string Type, string Message, DateTime At);
public record CourseStatsDto(int CourseId, string Title, int Enrollments, double CompletionRate, double AverageRating);
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);

// ─── CART / ORDER ──────────────────────────────────────────────────────────────
public record CartItemDto(int Id, int CourseId, string CourseTitle, string? ThumbnailUrl, decimal Price, bool IsFree, DateTime AddedAt);
public record OrderDto(int Id, string OrderNumber, decimal TotalAmount, string Currency, string Status, string? RazorpayOrderId, DateTime CreatedAt, List<OrderItemDto> Items);
public record OrderItemDto(int CourseId, string CourseTitle, decimal Price);
public record CreateOrderRequest(int UserId, List<int> CourseIds);
public record VerifyPaymentRequest(int OrderId, string RazorpayOrderId, string RazorpayPaymentId, string RazorpaySignature);

// ─── DEPARTMENT ────────────────────────────────────────────────────────────────
public record DepartmentDto(int Id, string Name, string? Description, string? IconEmoji, string? Color, bool IsActive, int DisplayOrder, int CategoryCount, int UserCount, DateTime CreatedAt);
public record CreateDepartmentRequest(string Name, string? Description, string? IconEmoji, string? Color, int OrganizationId, int DisplayOrder = 0);
public record UpdateDepartmentRequest(string? Name, string? Description, string? IconEmoji, string? Color, bool? IsActive, int? DisplayOrder);

// ─── HOMEPAGE CONFIG ───────────────────────────────────────────────────────────
public record HomePageConfigDto(
    int Id, int OrganizationId, string TemplateId,
    string? HeroTitle, string? HeroSubtitle, string? HeroButtonText,
    string? HeroButtonUrl, string? HeroImageUrl, string? HeroVideoUrl, string? HeroStyle,
    string? SectionsConfig, bool ShowStats, string? StatsCustom,
    string? AnnouncementText, bool ShowAnnouncement,
    string? NavLinksJson, string? FooterTagline, string? FooterLinksJson,
    string? FooterSocialJson, string? FooterCopyright, bool ShowFooterNewsletter,
    string? CustomSectionsJson, string? CustomHtml
);
public record SaveHomePageConfigRequest(
    string TemplateId,
    string? HeroTitle, string? HeroSubtitle, string? HeroButtonText,
    string? HeroButtonUrl, string? HeroImageUrl, string? HeroVideoUrl, string? HeroStyle,
    string? SectionsConfig, bool ShowStats, string? StatsCustom,
    string? AnnouncementText, bool ShowAnnouncement,
    string? NavLinksJson, string? FooterTagline, string? FooterLinksJson,
    string? FooterSocialJson, string? FooterCopyright, bool ShowFooterNewsletter,
    string? CustomSectionsJson, string? CustomHtml
);

// ─── ASSIGNMENTS ───────────────────────────────────────────────────────────────
public record AssignmentDto(int Id, string Title, string? Description, string? AttachmentUrl, int MaxMarks, DateTime DueDate, string Status, int CourseId, string CourseTitle, int CreatedById, string CreatedByName, DateTime CreatedAt, int SubmissionCount, int GradedCount, int? MyMarks, string? MyStatus, string? MyFeedback);
public record CreateAssignmentRequest(string Title, string? Description, string? AttachmentUrl, int MaxMarks, DateTime DueDate, int CourseId, int CreatedById);
public record SubmitAssignmentRequest(int AssignmentId, int StudentId, string? SubmissionText, string? FileUrl);
public record GradeSubmissionRequest(int SubmissionId, int? MarksObtained, string? Feedback, int GradedById, string? Status = null);
public record SubmissionDto(int Id, int AssignmentId, string AssignmentTitle, int StudentId, string StudentName, string? SubmissionText, string? FileUrl, int? MarksObtained, string? Feedback, string Status, DateTime? SubmittedAt, DateTime? GradedAt);

// ─── ATTENDANCE ────────────────────────────────────────────────────────────────
public record AttendanceDto(int Id, int CourseId, string CourseTitle, int StudentId, string StudentName, DateTime Date, string Status, string? Remarks);
public record MarkAttendanceRequest(int CourseId, DateTime Date, int MarkedById, List<StudentAttendanceEntry> Entries);
public record StudentAttendanceEntry(int StudentId, string Status, string? Remarks);
public record AttendanceSummaryDto(int StudentId, string StudentName, int TotalClasses, int Present, int Absent, int Late, double Percentage);
public record AttendanceEntryDto(int StudentId, string Status, string? Remarks);

// ─── LIVE CLASSES ──────────────────────────────────────────────────────────────
public record LiveClassDto(int Id, string Title, string? Description, DateTime ScheduledAt, int DurationMinutes, string Platform, string? MeetingLink, string? MeetingId, string? MeetingPassword, string? RecordingUrl, string Status, int CourseId, string CourseTitle, int HostId, string HostName, bool EmailSent, DateTime CreatedAt);
public record CreateLiveClassRequest(string Title, string? Description, DateTime ScheduledAt, int DurationMinutes, string Platform, string? MeetingLink, string? MeetingId, string? MeetingPassword, int CourseId, int HostId);
public record UpdateLiveClassRequest(string? Title, string? Description, DateTime? ScheduledAt, int? DurationMinutes, string? MeetingLink, string? RecordingUrl, string? Status);

// ─── MOCK TEST ─────────────────────────────────────────────────────────────────
public record MockTestDto(int Id, string Title, string? Description, string? Topic, string Difficulty, string Status, int TimeLimitMins, int TotalQuestions, int PassMarkPercent, bool RandomizeQuestions, bool ShowResultImmediately, int MaxAttempts, string? Tags, int OrganizationId, int? CourseId, DateTime CreatedAt, int AttemptCount, List<MockTestQuestionDto>? Questions);
public record CreateMockTestRequest(string Title, string? Description, string? Topic, string Difficulty, int TimeLimitMins, int TotalQuestions, int PassMarkPercent, bool RandomizeQuestions, bool ShowResultImmediately, int MaxAttempts, string? Tags, int OrganizationId, int? CourseId, int CreatedById);
public record MockTestQuestionDto(int Id, string Text, string? ImageUrl, string? Explanation, string? ExplanationImageUrl, string? FormulaLatex, string Topic, string Difficulty, string QuestionType, int Marks, int NegativeMarks, int DisplayOrder, List<MockTestOptionDto> Options);
public record MockTestOptionDto(int Id, string Text, string? ImageUrl, bool IsCorrect, int DisplayOrder);
public record AddMockQuestionRequest(string Text, string Topic, string Difficulty, string QuestionType, int Marks, int NegativeMarks, string? Explanation, string? ExplanationImageUrl, string? ImageUrl, string? FormulaLatex, int MockTestId, List<CreateMockOptionRequest> Options);
public record CreateMockOptionRequest(string Text, bool IsCorrect, string? ImageUrl = null);
public record StartMockAttemptRequest(int MockTestId, int StudentId);
public record SubmitMockAttemptRequest(int AttemptId, List<MockAnswerEntry> Answers, int TimeTakenSecs);
public record MockAnswerEntry(int QuestionId, int? SelectedOptionId, List<int>? SelectedOptionIds = null, string? TextAnswer = null);
public record MockAttemptResultDto(int Id, int MockTestId, string MockTestTitle, int StudentId, string StudentName, DateTime StartedAt, DateTime? CompletedAt, int TimeTakenSecs, int TotalMarks, int MarksObtained, int NegativeMarks, int ScorePercent, int Rank, bool Passed, string InterviewReadiness, string Status, int AttemptNumber, List<TopicScoreDto> TopicScores, List<MockAnswerDetailDto>? AnswerDetails);
public record TopicScoreDto(string Topic, int TotalQuestions, int Correct, int ScorePercent);
public record MockAnswerDetailDto(int QuestionId, string QuestionText, string? QuestionImageUrl, string QuestionType, string? SelectedOption, List<string>? SelectedOptions, string? CorrectOption, List<string>? CorrectOptions, bool IsCorrect, bool IsSkipped, int MarksAwarded, string? Explanation, string? ExplanationImageUrl, string? FormulaLatex);
public record MockTestAnalysisDto(int StudentId, string StudentName, int TotalAttempts, double AverageScore, int BestScore, string InterviewReadiness, List<TopicScoreDto> WeakTopics, List<TopicScoreDto> StrongTopics, List<MockAttemptResultDto> RecentAttempts);

// ─── INTERVIEW SCHEDULE ────────────────────────────────────────────────────────
public record InterviewScheduleDto(int Id, string Title, string? Description, DateTime ScheduledAt, int DurationMinutes, string? Platform, string? MeetingLink, string? InterviewerName, string? InterviewerEmail, string Status, string? Notes, string? Feedback, int StudentId, string StudentName, int? CourseId, string? CourseTitle, bool EmailSent, DateTime CreatedAt);
public record CreateInterviewRequest(string Title, string? Description, DateTime ScheduledAt, int DurationMinutes, string? Platform, string? MeetingLink, string? InterviewerName, string? InterviewerEmail, string? Notes, int StudentId, int? CourseId, int OrganizationId);
public record UpdateInterviewRequest(string? Status, string? Notes, string? Feedback, DateTime? ScheduledAt, string? MeetingLink);