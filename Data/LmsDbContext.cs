using Microsoft.EntityFrameworkCore;

namespace LMS.API.Data;

public class LmsDbContext(DbContextOptions<LmsDbContext> options) : DbContext(options)
{
    // Core
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Department> Departments => Set<Department>();

    // Courses & Lessons
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonResource> LessonResources => Set<LessonResource>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // Assessments
    public DbSet<MockTest> MockTests => Set<MockTest>();
    public DbSet<MockTestQuestion> MockTestQuestions => Set<MockTestQuestion>();
    public DbSet<MockTestAttempt> MockTestAttempts => Set<MockTestAttempt>();
    public DbSet<TopicScore> TopicScores => Set<TopicScore>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    // Live Classes & Interviews
    public DbSet<LiveClass> LiveClasses => Set<LiveClass>();
    public DbSet<LiveClassAttendee> LiveClassAttendees => Set<LiveClassAttendee>();
    public DbSet<InterviewSchedule> InterviewSchedules => Set<InterviewSchedule>();
    public DbSet<BatchResource> BatchResources => Set<BatchResource>();

    // Payments
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

        // Unique constraints
        b.Entity<Organization>().HasIndex(o => o.Slug).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Certificate>().HasIndex(c => c.CertificateNumber).IsUnique();
        b.Entity<LessonProgress>().HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
        b.Entity<LiveClassAttendee>().HasIndex(a => new { a.LiveClassId, a.UserId }).IsUnique();

        // Ignore computed property
        b.Entity<TrainingBatch>().Ignore(t => t.EndDate);
        b.Entity<BatchStudent>().Ignore(s => s.PendingAmount);

        // Decimal precision
        b.Entity<Order>().Property(o => o.Amount).HasPrecision(18, 2);
        b.Entity<OrderItem>().Property(i => i.Price).HasPrecision(18, 2);
        b.Entity<Course>().Property(c => c.Price).HasPrecision(18, 2);
        b.Entity<PaymentTransaction>().Property(p => p.Amount).HasPrecision(18, 2);
        b.Entity<TrainingBatch>().Property(t => t.TotalFee).HasPrecision(18, 2);
        b.Entity<BatchStudent>().Property(s => s.TotalFee).HasPrecision(18, 2);
        b.Entity<BatchStudent>().Property(s => s.PaidAmount).HasPrecision(18, 2);

        // Prevent cascade delete issues
        b.Entity<Course>().HasOne(c => c.Instructor).WithMany().HasForeignKey(c => c.InstructorId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Enrollment>().HasOne(e => e.User).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Certificate>().HasOne(c => c.User).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<MockTestAttempt>().HasOne(a => a.User).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<AssignmentSubmission>().HasOne(s => s.Student).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<LiveClass>().HasOne(l => l.Host).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<InterviewSchedule>().HasOne(i => i.Student).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<TrainingBatch>().HasOne(t => t.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<BatchStudent>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.SetNull);

        // Many-to-many: User <-> Department
        b.Entity<User>().HasMany(u => u.Departments).WithMany(d => d.Users);
    }
}