using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Parents only. A parent sees their own linked children and nothing
// else: each child's tasks, results and teacher notes, and the teachers
// they can message. Any other child looks the same as a missing one.
[Authorize(Roles = AppRoles.Parent)]
public class ParentController : Controller
{
    private readonly IParentRepository _parents;
    private readonly IStudentRepository _students;
    private readonly IReadingAssessmentRepository _assessments;
    private readonly IGroupTaskRepository _tasks;
    private readonly IUserRepository _users;

    public ParentController(
        IParentRepository parents,
        IStudentRepository students,
        IReadingAssessmentRepository assessments,
        IGroupTaskRepository tasks,
        IUserRepository users)
    {
        _parents = parents;
        _students = students;
        _assessments = assessments;
        _tasks = tasks;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new ParentHomeViewModel
        {
            Children = await GetChildrenAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> AddChild(
        [Bind(Prefix = "AddChild")] AddChildRequest request)
    {
        var parentId = User.GetUserId();

        if (parentId is null)
        {
            return Challenge();
        }

        Guid? studentId = null;

        if (ModelState.IsValid)
        {
            studentId = await _parents.FindStudentByCodeAsync(request.ChildCode);

            if (studentId is null)
            {
                ModelState.AddModelError(
                    "AddChild.ChildCode",
                    "That code does not match any student. Check it with " +
                    "your child or their teacher.");
            }
        }

        if (!ModelState.IsValid || studentId is null)
        {
            return View(nameof(Index), new ParentHomeViewModel
            {
                Children = await GetChildrenAsync(),
                AddChild = new AddChildRequest { ChildCode = request.ChildCode }
            });
        }

        await _parents.LinkAsync(parentId.Value, studentId.Value);

        TempData["SuccessMessage"] = "Your child was added.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Child(Guid id)
    {
        var student = await GetLinkedStudentAsync(id);

        if (student is null)
        {
            return NotFound("Child not found.");
        }

        var assessments = await _assessments.GetForStudentAsync(student.Id);
        var tasks = await _tasks.GetForStudentAsync(student.Id);

        return View(new ParentChildViewModel
        {
            Student = student,
            Assessments = assessments,
            Tasks = tasks
                .Select(task => new MyTaskViewModel
                {
                    Task = task,
                    Assessment = assessments.FirstOrDefault(
                        assessment => assessment.GroupTaskId == task.Id)
                })
                .ToList(),
            Teachers = await _users.GetTeachersForClassAsync(
                SchoolGroups.GetName(student))
        });
    }

    [HttpGet]
    public async Task<IActionResult> Result(Guid id)
    {
        var assessment = await _assessments.GetByIdAsync(id);

        var student = assessment is null
            ? null
            : await GetLinkedStudentAsync(assessment.StudentId);

        if (assessment is null || student is null)
        {
            return NotFound("Result not found.");
        }

        return View(new AssessmentPageViewModel
        {
            Student = student,
            Assessment = assessment
        });
    }

    private async Task<List<Student>> GetChildrenAsync()
    {
        var parentId = User.GetUserId();

        if (parentId is null)
        {
            return new List<Student>();
        }

        var children = new List<Student>();

        foreach (var studentId in await _parents.GetChildIdsAsync(parentId.Value))
        {
            var student = await _students.GetByIdAsync(studentId);

            if (student is not null)
            {
                children.Add(student);
            }
        }

        return children
            .OrderBy(student => student.FirstName)
            .ThenBy(student => student.LastName)
            .ToList();
    }

    // The student, or null when missing or not linked to this parent.
    private async Task<Student?> GetLinkedStudentAsync(Guid studentId)
    {
        var parentId = User.GetUserId();

        if (parentId is null ||
            !await _parents.IsLinkedAsync(parentId.Value, studentId))
        {
            return null;
        }

        return await _students.GetByIdAsync(studentId);
    }
}
