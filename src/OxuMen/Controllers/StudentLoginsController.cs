using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Teachers reset a student's password here. Students create their
// own accounts on the Create account page.
[Authorize(Roles = AppRoles.TeacherOrAdmin)]
public class StudentLoginsController : Controller
{
    private readonly IStudentRepository _students;
    private readonly IUserRepository _users;
    private readonly IClassAccess _classAccess;

    public StudentLoginsController(
        IStudentRepository students,
        IUserRepository users,
        IClassAccess classAccess)
    {
        _students = students;
        _users = users;
        _classAccess = classAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Manage(Guid id)
    {
        var student = await _students.GetByIdAsync(id);

        // Only students in the teacher's own classes.
        if (student is null || !await _classAccess.CanAccessStudentAsync(student))
        {
            return NotFound("Student not found.");
        }

        var login = await _users.GetForStudentAsync(id);

        return View(new StudentLoginViewModel
        {
            Student = student,
            ExistingUserName = login?.UserName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(
        Guid id,
        [Bind(Prefix = "Input")] StudentLoginRequest request)
    {
        var student = await _students.GetByIdAsync(id);

        // Only students in the teacher's own classes.
        if (student is null || !await _classAccess.CanAccessStudentAsync(student))
        {
            return NotFound("Student not found.");
        }

        var login = await _users.GetForStudentAsync(id);

        // Students create their own accounts; teachers can only reset
        // the password of an account that already exists.
        if (login is null)
        {
            return RedirectToAction(nameof(Manage), new { id });
        }

        // The user name is not part of a reset.
        ModelState.Remove("Input.UserName");

        if (!ModelState.IsValid)
        {
            return View(new StudentLoginViewModel
            {
                Student = student,
                ExistingUserName = login.UserName
            });
        }

        // Also signs the student out on any device still signed in.
        await _users.SetPasswordAsync(login.Id, request.Password);

        TempData["SuccessMessage"] =
            $"The password for \"{login.UserName}\" was reset.";

        return RedirectToAction("Details", "StudentPages", new { id });
    }
}
