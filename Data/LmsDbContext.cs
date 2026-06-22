using Microsoft.EntityFrameworkCore;

namespace LMS.API.Data;

public class LmsDbContext(DbContextOptions<LmsDbContext> options) : DbContext(options)
{
    // Core
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    // Departments
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();

    // Categories
    public DbSet<Category> Categories => Set<Category>();

    // Courses & Lessons
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseRating> CourseRatings => Set<CourseRating>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonResource> LessonResources => Set<LessonResource>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // Exams
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamQuestion> Questions => Set<ExamQuestion>(); // alias for ExamQuestions
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<Answer> Answers => Set<Answer>();

    // Mock Tests
    public DbSet<MockTest> MockTests => Set<MockTest>();
    public DbSet<MockTestQuestion> MockTestQuestions => Set<MockTestQuestion>();
    public DbSet<MockTestOption> MockTestOptions => Set<MockTestOption>();
    public DbSet<MockTestAttempt> MockTestAttempts => Set<MockTestAttempt>();
    public DbSet<MockTestAnswer> MockTestAnswers => Set<MockTestAnswer>();
    public DbSet<TopicScore> TopicScores => Set<TopicScore>();

    // Assignments
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    // Live Classes & Interviews
    public DbSet<LiveClass> LiveClasses => Set<LiveClass>();
    public DbSet<LiveClassAttendee> LiveClassAttendees => Set<LiveClassAttendee>();
    public DbSet<BatchResource> BatchResources => Set<BatchResource>();
    public DbSet<InterviewSchedule> InterviewSchedules => Set<InterviewSchedule>();

    // Attendance
    public DbSet<Attendance> Attendances => Set<Attendance>();

    // Homepage
    public DbSet<HomePageConfig> HomePageConfigs => Set<HomePageConfig>();

    // Cart & Payments
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // Training Batches
    public DbSet<TrainingBatch> TrainingBatches => Set<TrainingBatch>();
    public DbSet<BatchStudent> BatchStudents => Set<BatchStudent>();
    public DbSet<BatchEnquiry> BatchEnquiries => Set<BatchEnquiry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Question is a subclass of ExamQuestion - map to same table, no discriminator
        b.Entity<ExamQuestion>().HasDiscriminator<string>("Discriminator")
            .HasValue<ExamQuestion>("ExamQuestion")
            .HasValue<Question>("Question");


        // ── Course nullable FKs ───────────────────────────────────
        b.Entity<Course>()
            .HasOne(c => c.Category).WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Lesson self-referencing tree (infinite sub-lesson nesting) ──
        // Restrict (not Cascade) — EF/SQL Server/MySQL reject a cascade
        // path here since a lesson is both the "one" and "many" side of
        // the same relationship, which would create a delete cycle.
        // Application code (DeleteLesson) is responsible for recursively
        // deleting/reassigning children before removing a parent lesson.
        b.Entity<Lesson>()
            .HasOne(l => l.ParentLesson).WithMany(l => l.ChildLessons)
            .HasForeignKey(l => l.ParentLessonId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Course>()
            .HasOne(c => c.Instructor).WithMany()
            .HasForeignKey(c => c.InstructorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Unique indexes ─────────────────────────────────────
        b.Entity<Organization>().HasIndex(o => o.Slug).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Certificate>().HasIndex(c => c.CertificateNumber).IsUnique();
        b.Entity<LessonProgress>().HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
        b.Entity<LiveClassAttendee>().HasIndex(a => new { a.LiveClassId, a.UserId }).IsUnique();
        b.Entity<CourseRating>().HasIndex(r => new { r.UserId, r.CourseId }).IsUnique();
        b.Entity<Cart>().HasIndex(c => new { c.UserId, c.CourseId }).IsUnique();
        b.Entity<UserDepartment>().HasIndex(ud => new { ud.UserId, ud.DepartmentId }).IsUnique();
        b.Entity<Attendance>().HasIndex(a => new { a.CourseId, a.StudentId, a.Date }).IsUnique();

        // ── Computed / ignored properties ──────────────────────
        b.Entity<TrainingBatch>().Ignore(t => t.EndDate);
        b.Entity<BatchStudent>().Ignore(s => s.PendingAmount);
        b.Entity<MockTestAttempt>().Ignore(a => a.Student); // NotMapped alias for User

        // ── Decimal precision ──────────────────────────────────
        b.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(18, 2);
        b.Entity<OrderItem>().Property(i => i.Price).HasPrecision(18, 2);
        b.Entity<Course>().Property(c => c.Price).HasPrecision(18, 2);
        b.Entity<PaymentTransaction>().Property(p => p.Amount).HasPrecision(18, 2);
        b.Entity<PaymentTransaction>().Property(p => p.RefundAmount).HasPrecision(18, 2);
        b.Entity<TrainingBatch>().Property(t => t.TotalFee).HasPrecision(18, 2);
        b.Entity<BatchStudent>().Property(s => s.TotalFee).HasPrecision(18, 2);
        b.Entity<BatchStudent>().Property(s => s.PaidAmount).HasPrecision(18, 2);

        // ── Delete behavior ────────────────────────────────────
        // Enrollment
        b.Entity<Enrollment>()
            .HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

        // Certificate
        b.Entity<Certificate>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Certificate>()
            .HasOne(c => c.ExamAttempt).WithMany()
            .HasForeignKey(c => c.ExamAttemptId).OnDelete(DeleteBehavior.SetNull);

        // ExamAttempt — single relationship for User
        b.Entity<ExamAttempt>()
            .HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        // Exam Answers
        b.Entity<Answer>()
            .HasOne(a => a.Attempt).WithMany(a => a.Answers)
            .HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Answer>()
            .HasOne(a => a.Question).WithMany()
            .HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);

        // QuestionOption → ExamQuestion (Question is alias for ExamQuestion)
        b.Entity<QuestionOption>()
            .HasOne(o => o.Question).WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId).IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // MockTest
        b.Entity<MockTestAttempt>()
            .HasOne(a => a.User).WithMany()
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MockTestOption>()
            .HasOne(o => o.Question).WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MockTestAnswer>()
            .HasOne(a => a.Attempt).WithMany(a => a.Answers)
            .HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MockTestAnswer>()
            .HasOne(a => a.Question).WithMany()
            .HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MockTestAnswer>()
            .HasOne(a => a.SelectedOption).WithMany()
            .HasForeignKey(a => a.SelectedOptionId).OnDelete(DeleteBehavior.SetNull);

        // Assignments
        b.Entity<AssignmentSubmission>()
            .HasOne(s => s.Student).WithMany()
            .HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AssignmentSubmission>()
            .HasOne(s => s.GradedBy).WithMany()
            .HasForeignKey(s => s.GradedById).OnDelete(DeleteBehavior.SetNull);

        // Live Classes
        b.Entity<LiveClass>()
            .HasOne(l => l.Host).WithMany()
            .HasForeignKey(l => l.HostId).OnDelete(DeleteBehavior.Restrict);

        // Interviews
        b.Entity<InterviewSchedule>()
            .HasOne(i => i.Student).WithMany()
            .HasForeignKey(i => i.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Attendance
        b.Entity<Attendance>()
            .HasOne(a => a.Student).WithMany()
            .HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Attendance>()
            .HasOne(a => a.Course).WithMany()
            .HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Attendance>()
            .HasOne(a => a.MarkedBy).WithMany()
            .HasForeignKey(a => a.MarkedById).OnDelete(DeleteBehavior.SetNull);

        // Cart
        b.Entity<Cart>()
            .HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Cart>()
            .HasOne(c => c.Course).WithMany()
            .HasForeignKey(c => c.CourseId).OnDelete(DeleteBehavior.Cascade);

        // Payments
        b.Entity<PaymentTransaction>()
            .HasOne(t => t.User).WithMany()
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PaymentTransaction>()
            .HasOne(t => t.Order).WithMany()
            .HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Order>()
            .HasOne(o => o.User).WithMany()
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OrderItem>()
            .HasOne(i => i.Course).WithMany()
            .HasForeignKey(i => i.CourseId).OnDelete(DeleteBehavior.Restrict);

        // CourseRating
        b.Entity<CourseRating>()
            .HasOne(r => r.User).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        // Category self-referencing
        b.Entity<Category>()
            .HasOne(c => c.Parent).WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);

        // UserDepartment explicit join — single relationship
        b.Entity<UserDepartment>()
            .HasOne(ud => ud.User).WithMany(u => u.UserDepartments)
            .HasForeignKey(ud => ud.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UserDepartment>()
            .HasOne(ud => ud.Department).WithMany(d => d.UserDepartments)
            .HasForeignKey(ud => ud.DepartmentId).OnDelete(DeleteBehavior.Cascade);

        // Training Batch
        b.Entity<TrainingBatch>()
            .HasOne(t => t.CreatedBy).WithMany()
            .HasForeignKey(t => t.CreatedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BatchStudent>()
            .HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}