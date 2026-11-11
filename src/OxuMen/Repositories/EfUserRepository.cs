using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Models;
using OxuMen.Security;

namespace OxuMen.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly OxuMenDbContext _db;

    public EfUserRepository(OxuMenDbContext db)
    {
        _db = db;
    }

    public static string Normalize(string userName)
    {
        return userName.Trim().ToUpperInvariant();
    }

    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<AppUser?> GetByUserNameAsync(string userName)
    {
        var normalized = Normalize(userName);

        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.NormalizedUserName == normalized);
    }

    public async Task<AppUser?> GetForStudentAsync(Guid studentId)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.StudentId == studentId);
    }

    public async Task<bool> UserNameExistsAsync(string userName)
    {
        var normalized = Normalize(userName);

        return await _db.Users
            .AnyAsync(user => user.NormalizedUserName == normalized);
    }

    public async Task<bool> AnyTeacherAsync()
    {
        return await AnyWithRoleAsync(AppRoles.Teacher);
    }

    public async Task<bool> AnyWithRoleAsync(string role)
    {
        return await _db.Users.AnyAsync(user => user.Role == role);
    }

    public async Task<List<AppUser>> GetByRoleAsync(string role)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(user => user.Role == role)
            .OrderBy(user => user.NormalizedUserName)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user is null)
        {
            return false;
        }

        var classes = await _db.TeacherClasses
            .Where(item => item.UserId == id)
            .ToListAsync();

        _db.TeacherClasses.RemoveRange(classes);
        _db.Users.Remove(user);

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<string>> GetTeacherClassesAsync(Guid userId)
    {
        var classes = await _db.TeacherClasses
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.ClassGroup)
            .ToListAsync();

        return SortGroups(classes);
    }

    public async Task<Dictionary<Guid, List<string>>> GetAllTeacherClassesAsync()
    {
        var rows = await _db.TeacherClasses
            .AsNoTracking()
            .ToListAsync();

        return rows
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => SortGroups(group.Select(item => item.ClassGroup)));
    }

    public async Task SetTeacherClassesAsync(
        Guid userId,
        IEnumerable<string> classGroups)
    {
        var wanted = classGroups
            .Select(group => group.Trim().ToUpperInvariant())
            .Where(group => SchoolGroups.All.Contains(group))
            .Distinct()
            .ToList();

        var existing = await _db.TeacherClasses
            .Where(item => item.UserId == userId)
            .ToListAsync();

        _db.TeacherClasses.RemoveRange(
            existing.Where(item => !wanted.Contains(item.ClassGroup)));

        _db.TeacherClasses.AddRange(wanted
            .Where(group => existing.All(item => item.ClassGroup != group))
            .Select(group => new TeacherClass
            {
                UserId = userId,
                ClassGroup = group
            }));

        await _db.SaveChangesAsync();
    }

    // Keeps the 1A, 1B, ... 5D order used everywhere else.
    private static List<string> SortGroups(IEnumerable<string> groups)
    {
        return groups
            .OrderBy(group => Array.IndexOf(SchoolGroups.All, group))
            .ToList();
    }

    public async Task<AppUser> CreateAsync(AppUser user, string password)
    {
        user.Id = Guid.NewGuid();
        user.UserName = user.UserName.Trim();
        user.NormalizedUserName = Normalize(user.UserName);
        user.PasswordHash = PasswordHashing.Hash(password);
        user.SecurityStamp = PasswordHashing.NewSecurityStamp();
        user.CreatedAtUtc = DateTime.UtcNow;

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _db.Entry(user).State = EntityState.Detached;

            // A unique index caught a duplicate user name or a second
            // login for the same student.
            throw new InvalidOperationException(
                "That user name is taken, or the student already has a login.",
                exception);
        }

        return user;
    }

    public async Task<AppUser?> SetPasswordAsync(Guid id, string password)
    {
        var user = await _db.Users.FindAsync(id);

        if (user is null)
        {
            return null;
        }

        user.PasswordHash = PasswordHashing.Hash(password);
        user.SecurityStamp = PasswordHashing.NewSecurityStamp();

        await _db.SaveChangesAsync();

        return user;
    }

    public async Task DeleteForStudentAsync(Guid studentId)
    {
        var users = await _db.Users
            .Where(user => user.StudentId == studentId)
            .ToListAsync();

        if (users.Count == 0)
        {
            return;
        }

        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();
    }
}
