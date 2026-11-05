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
    }
}
