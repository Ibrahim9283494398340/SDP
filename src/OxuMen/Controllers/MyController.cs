using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// The student's own area. Everything here is limited to the student
// linked to the signed-in account; the ID never comes from the URL.
[Authorize(Roles = AppRoles.Student)]
public class MyController : Controller
{
    private readonly IStudentRepository _students;
    private readonly IReadingAssessmentRepository _assessments;
    private readonly IGroupTaskRepository _tasks;
    private readonly IParentRepository _parents;

    public MyController(
        IStudentRepository students,
        IReadingAssessmentRepository assessments,
        IGroupTaskRepository tasks,
        IParentRepository parents)
    {
        _students = students;
        _assessments = assessments;
        _tasks = tasks;
        _parents = parents;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var student = await GetCurrentStudentAsync();

        if (student is null)
        {
            return await SignOutAndGoToLoginAsync();
        }

        var assessments = await _assessments.GetForStudentAsync(student.Id);
        var tasks = await _tasks.GetForStudentAsync(student.Id);

        var model = new MyPageViewModel
        {
            Student = student,
            Assessments = assessments,
            ParentCode = OxuMen.Models.ParentCode.Format(
                await _parents.GetOrCreateCodeAsync(student.Id)),
            Tasks = tasks
                .Select(task => new MyTaskViewModel
                {
                    Task = task,
                    Assessment = assessments.FirstOrDefault(
                        assessment => assessment.GroupTaskId == task.Id)
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Result(Guid id)
    {
        var student = await GetCurrentStudentAsync();

        if (student is null)
        {
            return await SignOutAndGoToLoginAsync();
        }

        var assessment = await _assessments.GetByIdAsync(id);

        // Someone else's result looks exactly like a missing one.
        if (assessment is null || assessment.StudentId != student.Id)
        {
            return NotFound("Result not found.");
        }

        return View(new AssessmentPageViewModel
        {
            Student = student,
            Assessment = assessment
        });
    }

    private async Task<Student?> GetCurrentStudentAsync()
    {
        var studentId = User.GetStudentId();

        return studentId.HasValue
            ? await _students.GetByIdAsync(studentId.Value)
            : null;
    }

    private async Task<IActionResult> SignOutAndGoToLoginAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login", "Account");
    }
}
