using Microsoft.EntityFrameworkCore;
using LMS.API.Models;

namespace LMS.API.Data;

public class LmsDbContext(DbContextOptions<LmsDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CourseRating> CourseRatings => Set<CourseRating>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<HomePageConfig> HomePageConfigs => Set<HomePageConfig>();
    public DbSet<LessonResource>       LessonResources       => Set<LessonResource>();
    public DbSet<Assignment>           Assignments           => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<Attendance>           Attendances           => Set<Attendance>();
    public DbSet<LiveClass>            LiveClasses           => Set<LiveClass>();
    public DbSet<LiveClassAttendee>    LiveClassAttendees    => Set<LiveClassAttendee>();
    public DbSet<BatchResource>        BatchResources        => Set<BatchResource>();
    public DbSet<TrainingBatch>        TrainingBatches       => Set<TrainingBatch>();
    public DbSet<BatchStudent>         BatchStudents         => Set<BatchStudent>();
    public DbSet<MockTest>             MockTests             => Set<MockTest>();
    public DbSet<MockTestQuestion>     MockTestQuestions     => Set<MockTestQuestion>();
    public DbSet<MockTestOption>       MockTestOptions       => Set<MockTestOption>();
    public DbSet<MockTestAttempt>      MockTestAttempts      => Set<MockTestAttempt>();
    public DbSet<MockTestAnswer>       MockTestAnswers       => Set<MockTestAnswer>();
    public DbSet<TopicScore>           TopicScores           => Set<TopicScore>();
    public DbSet<InterviewSchedule>    InterviewSchedules    => Set<InterviewSchedule>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Organization>(e => {
            e.HasIndex(o => o.Slug).IsUnique();
            e.Property(o => o.Currency).HasDefaultValue("INR");
        });

        mb.Entity<User>(e => {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Organization).WithMany(o => o.Users)
             .HasForeignKey(u => u.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            e.Property(u => u.Role).HasConversion<string>();
        });

        mb.Entity<Category>(e => {
            e.HasOne(c => c.Parent).WithMany(c => c.Children)
             .HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Organization).WithMany(o => o.Categories)
             .HasForeignKey(c => c.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Course>(e => {
            e.HasOne(c => c.Instructor).WithMany(u => u.CoursesOwned)
             .HasForeignKey(c => c.InstructorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Organization).WithMany(o => o.Courses)
             .HasForeignKey(c => c.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.Property(c => c.Price).HasPrecision(10, 2);
            e.Property(c => c.Level).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
        });

        mb.Entity<Enrollment>(e => {
            e.HasIndex(en => new { en.UserId, en.CourseId }).IsUnique();
            e.HasOne(en => en.User).WithMany(u => u.Enrollments)
             .HasForeignKey(en => en.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(en => en.Course).WithMany(c => c.Enrollments)
             .HasForeignKey(en => en.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.Property(en => en.Status).HasConversion<string>();
        });

        mb.Entity<LessonProgress>(e => {
            e.HasIndex(lp => new { lp.UserId, lp.LessonId }).IsUnique();
        });

        mb.Entity<Lesson>(e => e.Property(l => l.Type).HasConversion<string>());

        mb.Entity<Answer>()
          .HasMany(a => a.SelectedOptions).WithMany()
          .UsingEntity("AnswerSelectedOptions");

        mb.Entity<ExamAttempt>(e => {
            e.HasOne(ea => ea.User).WithMany(u => u.ExamAttempts)
             .HasForeignKey(ea => ea.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(ea => ea.Status).HasConversion<string>();
        });

        mb.Entity<Question>(e => e.Property(q => q.Type).HasConversion<string>());

        mb.Entity<Certificate>(e => {
            e.HasIndex(c => c.CertificateNumber).IsUnique();
            e.HasOne(c => c.User).WithMany(u => u.Certificates)
             .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Course).WithMany()
             .HasForeignKey(c => c.CourseId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<CourseRating>(e => {
            e.HasIndex(r => new { r.UserId, r.CourseId }).IsUnique();
        });

        mb.Entity<Cart>(e => {
            e.HasIndex(c => new { c.UserId, c.CourseId }).IsUnique();
            e.HasOne(c => c.User).WithMany(u => u.CartItems)
             .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Course).WithMany(co => co.CartItems)
             .HasForeignKey(c => c.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Order>(e => {
            e.HasOne(o => o.User).WithMany(u => u.Orders)
             .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(o => o.TotalAmount).HasPrecision(10, 2);
            e.Property(o => o.Status).HasConversion<string>();
        });

        mb.Entity<OrderItem>(e => {
            e.HasOne(oi => oi.Course).WithMany(c => c.OrderItems)
             .HasForeignKey(oi => oi.CourseId).OnDelete(DeleteBehavior.Restrict);
            e.Property(oi => oi.Price).HasPrecision(10, 2);
        });

        mb.Entity<Department>(e => {
            e.HasOne(d => d.Organization).WithMany(o => o.Departments)
             .HasForeignKey(d => d.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<UserDepartment>(e => {
            e.HasKey(ud => new { ud.UserId, ud.DepartmentId });
            e.HasOne(ud => ud.User).WithMany(u => u.UserDepartments)
             .HasForeignKey(ud => ud.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ud => ud.Department).WithMany(d => d.UserDepartments)
             .HasForeignKey(ud => ud.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<UserRoleAssignment>(e => {
            e.HasIndex(r => new { r.UserId, r.Role }).IsUnique();
            e.HasOne(r => r.User).WithMany(u => u.RoleAssignments)
             .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(r => r.Role).HasConversion<string>();
        });

        mb.Entity<Category>(e => {
            e.HasOne(c => c.Department).WithMany(d => d.Categories)
             .HasForeignKey(c => c.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });


        mb.Entity<LessonResource>(e => {
            e.HasOne(r => r.Lesson).WithMany(l => l.Resources)
             .HasForeignKey(r => r.LessonId).OnDelete(DeleteBehavior.Cascade);
            e.Property(r => r.Type).HasConversion<string>();
        });

        mb.Entity<Assignment>(e => {
            e.HasOne(a => a.Course).WithMany().HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.CreatedBy).WithMany().HasForeignKey(a => a.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.Property(a => a.Status).HasConversion<string>();
        });
        mb.Entity<AssignmentSubmission>(e => {
            e.HasOne(s => s.Assignment).WithMany(a => a.Submissions).HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Student).WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.GradedBy).WithMany().HasForeignKey(s => s.GradedById).OnDelete(DeleteBehavior.SetNull);
            e.Property(s => s.Status).HasConversion<string>();
        });
        mb.Entity<Attendance>(e => {
            e.HasIndex(a => new { a.CourseId, a.StudentId, a.Date });
            e.Property(a => a.Status).HasConversion<string>();
        });
        mb.Entity<LiveClass>(e => {
            e.HasOne(l => l.Course).WithMany().HasForeignKey(l => l.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Host).WithMany().HasForeignKey(l => l.HostId).OnDelete(DeleteBehavior.Restrict);
            e.Property(l => l.Platform).HasConversion<string>();
            e.Property(l => l.Status).HasConversion<string>();
        });
        mb.Entity<LiveClassAttendee>(e => {
            e.HasIndex(a => new { a.LiveClassId, a.UserId }).IsUnique();
        });
        mb.Entity<MockTestQuestion>(e => {
            e.Property(q => q.QuestionType).HasConversion<string>();
        });

        mb.Entity<MockTest>(e => {
            e.HasOne(m => m.Organization).WithMany().HasForeignKey(m => m.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.CreatedBy).WithMany().HasForeignKey(m => m.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.Property(m => m.Status).HasConversion<string>();
            e.Property(m => m.Difficulty).HasConversion<string>();
        });
        mb.Entity<MockTestQuestion>(e => {
            e.Property(q => q.Difficulty).HasConversion<string>();
        });
        mb.Entity<MockTestAttempt>(e => {
            e.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.Property(a => a.Status).HasConversion<string>();
        });
        mb.Entity<InterviewSchedule>(e => {
            e.HasOne(i => i.Student).WithMany().HasForeignKey(i => i.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Organization).WithMany().HasForeignKey(i => i.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.Property(i => i.Status).HasConversion<string>();
        });

        mb.Entity<PaymentTransaction>(e => {
            e.HasOne(pt => pt.User).WithMany()
             .HasForeignKey(pt => pt.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(pt => pt.Order).WithMany()
             .HasForeignKey(pt => pt.OrderId).OnDelete(DeleteBehavior.SetNull);
            e.Property(pt => pt.Amount).HasPrecision(10,2);
            e.Property(pt => pt.RefundAmount).HasPrecision(10,2);
            e.Property(pt => pt.Status).HasConversion<string>();
            e.Property(pt => pt.Method).HasConversion<string>();
        });

        mb.Entity<HomePageConfig>(e => {
            e.HasOne(h => h.Organization).WithOne(o => o.HomePageConfig)
             .HasForeignKey<HomePageConfig>(h => h.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
