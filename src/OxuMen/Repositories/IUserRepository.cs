using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id);

    Task<AppUser?> GetByUserNameAsync(string userName);

    Task<AppUser?> GetForStudentAsync(Guid studentId);

    Task<bool> UserNameExistsAsync(string userName);

    Task<bool> AnyTeacherAsync();

    // Hashes the password and fills Id, NormalizedUserName,
    // SecurityStamp and CreatedAtUtc.
    Task<AppUser> CreateAsync(AppUser user, string password);

    // Also rotates the security stamp, which signs the user out elsewhere.
    Task<AppUser?> SetPasswordAsync(Guid id, string password);

    Task DeleteForStudentAsync(Guid studentId);

    Task<List<AppUser>> GetByRoleAsync(string role);

    Task<bool> AnyWithRoleAsync(string role);

    Task<bool> DeleteAsync(Guid id);

    // Classes (like "2A") a teacher is responsible for, sorted.
    Task<List<string>> GetTeacherClassesAsync(Guid userId);

    // Every teacher's classes, keyed by user ID.
    Task<Dictionary<Guid, List<string>>> GetAllTeacherClassesAsync();

    // Replaces the teacher's class list.
    Task SetTeacherClassesAsync(Guid userId, IEnumerable<string> classGroups);
}
