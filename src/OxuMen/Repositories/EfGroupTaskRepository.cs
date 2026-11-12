using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class EfGroupTaskRepository : IGroupTaskRepository
{
    private readonly OxuMenDbContext _db;

    public EfGroupTaskRepository(OxuMenDbContext db)
    {
        _db = db;
    }

    public async Task<List<GroupTask>> GetAllAsync()
    {
        var tasks = await _db.GroupTasks
            .AsNoTracking()
            .Include(task => task.Assignments)
            .ToListAsync();

        tasks.ForEach(FillStudentIds);

        // SQLite cannot sort DateTimeOffset columns in SQL, so sort here.
        return tasks
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToList();
    }

    public async Task<GroupTask?> GetByIdAsync(Guid id)
    {
        var task = await _db.GroupTasks
            .AsNoTracking()
            .Include(task => task.Assignments)
            .FirstOrDefaultAsync(task => task.Id == id);

        if (task is not null)
        {
            FillStudentIds(task);
        }

        return task;
    }

    public async Task<List<GroupTask>> GetForStudentAsync(Guid studentId)
    {
        var tasks = await _db.GroupTasks
            .AsNoTracking()
            .Include(task => task.Assignments)
            .Where(task => task.Assignments.Any(
                assignment => assignment.StudentId == studentId))
            .ToListAsync();

        tasks.ForEach(FillStudentIds);

        return tasks
            .OrderByDescending(task => task.CreatedAtUtc)
            .ToList();
    }

    public async Task<GroupTask> CreateAsync(GroupTask task)
    {
        task.Id = Guid.NewGuid();
        task.CreatedAtUtc = DateTimeOffset.UtcNow;

        task.Assignments = task.StudentIds
            .Distinct()
            .Select(studentId => new GroupTaskStudent
            {
                GroupTaskId = task.Id,
                StudentId = studentId
            })
            .ToList();

        _db.GroupTasks.Add(task);
        await _db.SaveChangesAsync();

        FillStudentIds(task);

        return task;
    }

    private static void FillStudentIds(GroupTask task)
    {
        task.StudentIds = task.Assignments
            .Select(assignment => assignment.StudentId)
            .ToList();
    }
}
