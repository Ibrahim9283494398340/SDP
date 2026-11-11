using Microsoft.EntityFrameworkCore;
using OxuMen.Models;

namespace OxuMen.Data;

public class OxuMenDbContext : DbContext
{
    public OxuMenDbContext(DbContextOptions<OxuMenDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<ReadingAssessment> ReadingAssessments => Set<ReadingAssessment>();

    public DbSet<GroupTask> GroupTasks => Set<GroupTask>();

    public DbSet<GroupTaskStudent> GroupTaskStudents => Set<GroupTaskStudent>();

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<TeacherClass> TeacherClasses => Set<TeacherClass>();

    public DbSet<ParentChild> ParentChildren => Set<ParentChild>();

    public DbSet<ParentCode> ParentCodes => Set<ParentCode>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(student => student.Id);

            entity.Property(student => student.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(student => student.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(student => student.ClassGroup)
                .HasMaxLength(2)
                .IsRequired();

            entity.HasIndex(student => student.ClassGroup);
        });

        modelBuilder.Entity<GroupTask>(entity =>
        {
            entity.ToTable("GroupTasks");
            entity.HasKey(task => task.Id);

            entity.Property(task => task.ClassGroup)
                .HasMaxLength(2)
                .IsRequired();

            entity.Property(task => task.Kind)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(task => task.TextReference)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(task => task.TextVersion)
                .HasMaxLength(30)
                .IsRequired();

            entity.Ignore(task => task.StudentIds);

            entity.HasMany(task => task.Assignments)
                .WithOne()
                .HasForeignKey(assignment => assignment.GroupTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(task => task.ClassGroup);
        });

        modelBuilder.Entity<GroupTaskStudent>(entity =>
        {
            entity.ToTable("GroupTaskStudents");
            entity.HasKey(assignment => new
            {
                assignment.GroupTaskId,
                assignment.StudentId
            });

            // No foreign key to Students on purpose: the app lets a student
            // be deleted while a task still lists them, and the task page
            // then shows "Student no longer available".
            entity.HasIndex(assignment => assignment.StudentId);
        });

        modelBuilder.Entity<ReadingAssessment>(entity =>
        {
            entity.ToTable("ReadingAssessments");
            entity.HasKey(assessment => assessment.Id);

            entity.Property(assessment => assessment.Kind)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(assessment => assessment.TextReference)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(assessment => assessment.TextVersion)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(assessment => assessment.Notes)
                .HasMaxLength(2000);

            entity.Property(assessment => assessment.DurationSeconds)
                .HasPrecision(10, 2);

            // Calculated in C#, not stored.
            entity.Ignore(assessment => assessment.WordsPerMinute);
            entity.Ignore(assessment => assessment.CorrectWordsPerMinute);
            entity.Ignore(assessment => assessment.AccuracyPercentage);

            // A student with assessments cannot be deleted.
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(assessment => assessment.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<GroupTask>()
                .WithMany()
                .HasForeignKey(assessment => assessment.GroupTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // One result per student per task.
            entity.HasIndex(assessment => new
                {
                    assessment.GroupTaskId,
                    assessment.StudentId
                })
                .IsUnique();

            entity.HasIndex(assessment => assessment.StudentId);
        });

        // Keep in step with UserSetup.EnsureUsersTableAsync, which creates
        // this table on databases made before logins were added.
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.UserName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(user => user.NormalizedUserName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(user => user.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(user => user.SecurityStamp)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(user => user.NormalizedUserName)
                .IsUnique();

            // At most one login per student.
            entity.HasIndex(user => user.StudentId)
                .IsUnique();

            // Deleting a student also deletes their login.
            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(user => user.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Keep in step with UserSetup.EnsureTeacherClassesTableAsync.
        modelBuilder.Entity<TeacherClass>(entity =>
        {
            entity.ToTable("TeacherClasses");
            entity.HasKey(item => new { item.UserId, item.ClassGroup });

            entity.Property(item => item.ClassGroup)
                .HasMaxLength(2)
                .IsRequired();

            // Deleting a teacher removes their class list.
            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(item => item.ClassGroup);
        });

        // The three tables below: keep in step with
        // UserSetup.EnsureParentTablesAsync.
        modelBuilder.Entity<ParentChild>(entity =>
        {
            entity.ToTable("ParentChildren");
            entity.HasKey(link => new { link.ParentUserId, link.StudentId });

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(link => link.ParentUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(link => link.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(link => link.StudentId);
        });

        modelBuilder.Entity<ParentCode>(entity =>
        {
            entity.ToTable("ParentCodes");
            entity.HasKey(code => code.StudentId);

            entity.Property(code => code.Code)
                .HasMaxLength(16)
                .IsRequired();

            entity.HasIndex(code => code.Code)
                .IsUnique();

            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(code => code.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(message => message.Id);

            entity.Property(message => message.Body)
                .HasMaxLength(2000)
                .IsRequired();

            entity.HasOne<Student>()
                .WithMany()
                .HasForeignKey(message => message.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(message => message.ParentUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(message => message.TeacherUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(message => new
            {
                message.StudentId,
                message.ParentUserId,
                message.TeacherUserId
            });

            entity.HasIndex(message => message.TeacherUserId);
            entity.HasIndex(message => message.ParentUserId);
        });
    }
}
