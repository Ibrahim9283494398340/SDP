using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IReadingAssessmentRepository
{
    Task<ReadingAssessment?> GetByIdAsync(Guid id);

    Task<List<ReadingAssessment>> GetForStudentAsync(Guid studentId);

    Task<ReadingAssessment> CreateAsync(ReadingAssessment assessment);
}