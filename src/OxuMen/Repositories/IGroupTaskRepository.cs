using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IGroupTaskRepository
{
    Task<List<GroupTask>> GetAllAsync();

    Task<GroupTask?> GetByIdAsync(Guid id);

    Task<GroupTask> CreateAsync(GroupTask task);
}
