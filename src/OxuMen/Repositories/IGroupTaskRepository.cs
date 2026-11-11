using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IGroupTaskRepository
{
    Task<List<GroupTask>> GetAllAsync();

    Task<GroupTask?> GetByIdAsync(Guid id);

    Task<List<GroupTask>> GetForStudentAsync(Guid studentId);

    Task<GroupTask> CreateAsync(GroupTask task);
}
