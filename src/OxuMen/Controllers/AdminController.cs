using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Admins only: create teachers, decide which classes each teacher sees,
// and remove teachers; see every student and move students between
// classes. The admin cannot change a teacher's password.
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;
    private readonly IMessageRepository _messages;

    public AdminController(
        IUserRepository users,
        IStudentRepository students,
        IMessageRepository messages)
    {
        _users = users;
        _students = students;
        _messages = messages;
    }

    [HttpGet]
    public async Task<IActionResult> Teachers()
    {
        var teachers = await _users.GetByRoleAsync(AppRoles.Teacher);
        var classes = await _users.GetAllTeacherClassesAsync();
        var students = await _students.GetAllAsync();

        var model = teachers
            .Select(teacher =>
            {
                var teacherClasses = classes.TryGetValue(teacher.Id, out var list)
                    ? list
                    : new List<string>();

                return new TeacherListItemViewModel
                {
                    User = teacher,
                    Classes = teacherClasses,
                    StudentCount = students.Count(student =>
                        teacherClasses.Contains(SchoolGroups.GetName(student)))
                };
            })
            .ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult CreateTeacher()
    {
        return View(new NewTeacherRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeacher(NewTeacherRequest request)
    {
        request.Classes = CleanClasses(request.Classes);

        if (ModelState.IsValid &&
            await _users.UserNameExistsAsync(request.UserName))
        {
            ModelState.AddModelError(
                nameof(request.UserName),
                "That user name is already taken.");
        }

        if (!ModelState.IsValid)
        {
            return View(new NewTeacherRequest
            {
                UserName = request.UserName,
                Classes = request.Classes
            });
        }

        AppUser teacher;

        try
        {
            teacher = await _users.CreateAsync(
                new AppUser
                {
                    UserName = request.UserName,
                    Role = AppRoles.Teacher
                },
                request.Password);
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(
                nameof(request.UserName),
                "That user name is already taken.");

            return View(new NewTeacherRequest
            {
                UserName = request.UserName,
                Classes = request.Classes
            });
        }

        await _users.SetTeacherClassesAsync(teacher.Id, request.Classes);

        TempData["SuccessMessage"] =
            $"Teacher \"{teacher.UserName}\" was created.";

        return RedirectToAction(nameof(Teachers));
    }

    [HttpGet]
    public async Task<IActionResult> EditTeacher(Guid id)
    {
        var teacher = await GetTeacherAsync(id);

        if (teacher is null)
        {
            return NotFound("Teacher not found.");
        }

        return View(new EditTeacherViewModel
        {
            Id = teacher.Id,
            UserName = teacher.UserName,
            Input = new EditTeacherRequest
            {
                Classes = await _users.GetTeacherClassesAsync(teacher.Id)
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeacher(
        Guid id,
        [Bind(Prefix = "Input")] EditTeacherRequest request)
    {
        var teacher = await GetTeacherAsync(id);

        if (teacher is null)
        {
            return NotFound("Teacher not found.");
        }

        request.Classes = CleanClasses(request.Classes);

        if (!ModelState.IsValid)
        {
            return View(new EditTeacherViewModel
            {
                Id = teacher.Id,
                UserName = teacher.UserName,
                Input = new EditTeacherRequest { Classes = request.Classes }
            });
        }

        await _users.SetTeacherClassesAsync(teacher.Id, request.Classes);

        TempData["SuccessMessage"] =
            $"Classes for \"{teacher.UserName}\" were saved.";

        return RedirectToAction(nameof(Teachers));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteTeacher(Guid id)
    {
        var teacher = await GetTeacherAsync(id);

        if (teacher is null)
        {
            return NotFound("Teacher not found.");
        }

        return View(teacher);
    }

    [HttpPost]
    [ActionName("DeleteTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeacherConfirmed(Guid id)
    {
        var teacher = await GetTeacherAsync(id);

        if (teacher is null)
        {
            return NotFound("Teacher not found.");
        }

        // Students, tasks and results stay; the account and its messages go.
        await _messages.DeleteForUserAsync(teacher.Id);
        await _users.DeleteAsync(teacher.Id);

        TempData["SuccessMessage"] =
            $"Teacher \"{teacher.UserName}\" was removed.";

        return RedirectToAction(nameof(Teachers));
    }

    [HttpGet]
    public async Task<IActionResult> Students(string? group)
    {
        group = string.IsNullOrWhiteSpace(group)
            ? null
            : group.Trim().ToUpperInvariant();

        if (group is not null && !SchoolGroups.All.Contains(group))
        {
            group = null;
        }

        var students = await _students.GetAllAsync();
        var rows = new List<AdminStudentRowViewModel>();

        foreach (var student in students)
        {
            var studentGroup = SchoolGroups.GetName(student);

            if (group is not null && studentGroup != group)
            {
                continue;
            }

            rows.Add(new AdminStudentRowViewModel
            {
                Student = student,
                ClassGroup = studentGroup,
                UserName = (await _users.GetForStudentAsync(student.Id))?.UserName
            });
        }

        return View(new AdminStudentsViewModel
        {
            Group = group,
            Students = rows
                .OrderBy(row => Array.IndexOf(SchoolGroups.All, row.ClassGroup))
                .ThenBy(row => row.Student.LastName)
                .ThenBy(row => row.Student.FirstName)
                .ToList()
        });
    }

    // Moves a student to another class. Their results stay with them; the
    // teachers of the new class see them from now on.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeClass(
        Guid id,
        string? classGroup,
        string? returnGroup)
    {
        var student = await _students.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var group = (classGroup ?? string.Empty).Trim().ToUpperInvariant();

        if (!SchoolGroups.All.Contains(group))
        {
            TempData["ErrorMessage"] = "Choose a class from 1A to 5D.";

            return RedirectToAction(nameof(Students), new { group = returnGroup });
        }

        var oldGroup = SchoolGroups.GetName(student);

        if (oldGroup != group)
        {
            student.ClassGroup = group;
            student.GradeLevel = group[0] - '0';

            await _students.UpdateAsync(student);

            TempData["SuccessMessage"] =
                $"{student.FirstName} {student.LastName} was moved from " +
                $"{oldGroup} to {group}.";
        }

        return RedirectToAction(nameof(Students), new { group = returnGroup });
    }

    // Only teacher accounts can be managed here, never admins or students.
    private async Task<AppUser?> GetTeacherAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id);

        return user?.Role == AppRoles.Teacher ? user : null;
    }

    private static List<string> CleanClasses(IEnumerable<string>? classes)
    {
        return (classes ?? Enumerable.Empty<string>())
            .Select(group => group.Trim().ToUpperInvariant())
            .Where(group => SchoolGroups.All.Contains(group))
            .Distinct()
            .OrderBy(group => Array.IndexOf(SchoolGroups.All, group))
            .ToList();
    }
}
