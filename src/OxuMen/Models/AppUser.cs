namespace OxuMen.Models;

public static class AppRoles
{
    public const string Admin = "Admin";

    public const string Teacher = "Teacher";

    public const string Student = "Student";

    public const string Parent = "Parent";

    // For the messages pages, which both sides of a conversation use.
    public const string TeacherOrParent = Teacher + "," + Parent;

    // For [Authorize(Roles = ...)]: teacher pages are also open to admins.
    public const string TeacherOrAdmin = Teacher + "," + Admin;
}

// A login account. Teachers have no StudentId; a student account is
// linked to exactly one Student profile.
public class AppUser
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    // Upper-case copy of UserName, used for case-insensitive lookups.
    public string NormalizedUserName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = AppRoles.Student;

    public Guid? StudentId { get; set; }

    // Changes whenever the password changes, so old sign-in cookies stop working.
    public string SecurityStamp { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

// One row per class a teacher is responsible for. A teacher only sees
// students, tasks and results in these classes. Two teachers may share
// a class.
public class TeacherClass
{
    public Guid UserId { get; set; }

    public string ClassGroup { get; set; } = string.Empty;
}
