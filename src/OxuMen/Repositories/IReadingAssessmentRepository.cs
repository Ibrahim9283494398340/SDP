using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IReadingAssessmentRepository
{
    Task<ReadingAssessment?> GetByIdAsync(Guid id);

    Task<List<ReadingAssessment>> GetForStudentAsync(Guid studentId);

    // All results of the given students, newest first. Used by the dashboard.
    Task<List<ReadingAssessment>> GetForStudentsAsync(IReadOnlyCollection<Guid> studentIds);

    Task<ReadingAssessment> CreateAsync(ReadingAssessment assessment);
}