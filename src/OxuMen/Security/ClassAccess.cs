using OxuMen.Models;
using OxuMen.Repositories;

namespace OxuMen.Security;

// Answers "which classes may the signed-in user see?".
// Admins see every class; teachers see only the classes the admin gave
// them; anyone else sees none. Read from the database on each request,
// so a change by the admin applies immediately.
public interface IClassAccess
{
    Task<IReadOnlyList<string>> GetAllowedGroupsAsync();

    Task<bool> CanAccessGroupAsync(string? group);

    Task<bool> CanAccessStudentAsync(Student student);
}

public class ClassAccess : IClassAccess
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _users;

    private IReadOnlyList<string>? _allowed;

    public ClassAccess(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository users)
    {
        _httpContextAccessor = httpContextAccessor;
        _users = users;
    }

    public async Task<IReadOnlyList<string>> GetAllowedGroupsAsync()
    {
        if (_allowed is not null)
        {
            return _allowed;
        }

        var user = _httpContextAccessor.HttpContext?.User;

        if (user is null)
        {
            _allowed = Array.Empty<string>();
        }
        else if (user.IsInRole(AppRoles.Admin))
        {
            _allowed = SchoolGroups.All;
        }
        else if (user.IsInRole(AppRoles.Teacher) &&
                 user.GetUserId() is Guid userId)
        {
            _allowed = await _users.GetTeacherClassesAsync(userId);
        }
        else
        {
            _allowed = Array.Empty<string>();
        }

        return _allowed;
    }

    public async Task<bool> CanAccessGroupAsync(string? group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return false;
        }

        var allowed = await GetAllowedGroupsAsync();

        return allowed.Contains(group.Trim().ToUpperInvariant());
    }

    public async Task<bool> CanAccessStudentAsync(Student student)
    {
        return await CanAccessGroupAsync(SchoolGroups.GetName(student));
    }
}
