using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IParentRepository
{
    // The student's parent code (stored form, no dash); made on first use.
    Task<string> GetOrCreateCodeAsync(Guid studentId);

    // The student a parent code belongs to, or null if no student has it.
    Task<Guid?> FindStudentByCodeAsync(string code);

    // Links a parent to a child. Doing it twice is harmless.
    Task LinkAsync(Guid parentUserId, Guid studentId);

    Task<bool> IsLinkedAsync(Guid parentUserId, Guid studentId);

    Task<List<Guid>> GetChildIdsAsync(Guid parentUserId);

    Task<List<Guid>> GetParentIdsAsync(Guid studentId);

    // Removes the student's parent links and code.
    Task DeleteForStudentAsync(Guid studentId);
}
