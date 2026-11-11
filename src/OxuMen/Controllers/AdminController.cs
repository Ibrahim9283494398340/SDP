using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Admins only: create teachers, decide which classes each teacher sees,
// and remove teachers. The admin cannot change a teacher's password.
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;

    public AdminController(
        IUserRepository users,
        IStudentRepository students)
    {
        _users = users;
        _students = students;
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

        // Students, tasks and results stay; only the account goes.
        await _users.DeleteAsync(teacher.Id);

        TempData["SuccessMessage"] =
            $"Teacher \"{teacher.UserName}\" was removed.";

        return RedirectToAction(nameof(Teachers));
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
