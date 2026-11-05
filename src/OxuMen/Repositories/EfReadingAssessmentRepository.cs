using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class EfReadingAssessmentRepository : IReadingAssessmentRepository
{
    private readonly OxuMenDbContext _db;

    public EfReadingAssessmentRepository(OxuMenDbContext db)
    {
        _db = db;
    }

    public async Task<ReadingAssessment?> GetByIdAsync(Guid id)
    {
        return await _db.ReadingAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(assessment => assessment.Id == id);
    }

    public async Task<List<ReadingAssessment>> GetForStudentAsync(
        Guid studentId)
    {
        var assessments = await _db.ReadingAssessments
            .AsNoTracking()
            .Where(assessment => assessment.StudentId == studentId)
            .ToListAsync();

        // SQLite cannot sort DateTimeOffset columns in SQL, so sort here.
        return assessments
            .OrderByDescending(assessment => assessment.AssessedAtUtc)
            .ThenByDescending(assessment => assessment.CreatedAtUtc)
            .ToList();
    }

    public async Task<ReadingAssessment> CreateAsync(
        ReadingAssessment assessment)
    {
        if (assessment.GroupTaskId.HasValue &&
            await _db.ReadingAssessments.AnyAsync(existing =>
                existing.GroupTaskId == assessment.GroupTaskId &&
                existing.StudentId == assessment.StudentId))
        {
            throw new InvalidOperationException(
                "Results have already been recorded for this student and task.");
        }

        assessment.Id = Guid.NewGuid();
        assessment.CreatedAtUtc = DateTimeOffset.UtcNow;

        _db.ReadingAssessments.Add(assessment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _db.Entry(assessment).State = EntityState.Detached;

            // The unique index caught two saves for the same student and task.
            throw new InvalidOperationException(
                "Results have already been recorded for this student and task.",
                exception);
        }

        return assessment;
    }
}
