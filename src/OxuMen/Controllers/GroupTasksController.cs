using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Teachers and admins. A teacher only sees and assigns tasks for their own
// classes. Students use MyController.
[Authorize(Roles = AppRoles.TeacherOrAdmin)]
public class GroupTasksController : Controller
{
    private readonly IStudentRepository _students;
    private readonly IReadingAssessmentRepository _assessments;
    private readonly IGroupTaskRepository _tasks;
    private readonly IClassAccess _classAccess;

    public GroupTasksController(
        IStudentRepository students,
        IReadingAssessmentRepository assessments,
        IGroupTaskRepository tasks,
        IClassAccess classAccess)
    {
        _students = students;
        _assessments = assessments;
        _tasks = tasks;
        _classAccess = classAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // The signed-in teacher's classes, in 1A..5D order.
        var groups = (await _classAccess.GetAllowedGroupsAsync()).ToList();

        return View(groups);
    }

    [HttpGet]
    public async Task<IActionResult> Group(string group)
    {
        if (!SchoolGroups.All.Contains(group))
        {
            return NotFound("Group not found.");
        }

        if (!await _classAccess.CanAccessGroupAsync(group))
        {
            return Forbid();
        }

        var tasks = await _tasks.GetAllAsync();

        ViewData["GroupName"] = group;

        return View(tasks
            .Where(task => task.ClassGroup == group)
            .ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? group)
    {
        if (group is not null && !SchoolGroups.All.Contains(group))
        {
            return NotFound("Group not found.");
        }

        if (group is not null && !await _classAccess.CanAccessGroupAsync(group))
        {
            return Forbid();
        }

        ViewData["AllowedGroups"] = await _classAccess.GetAllowedGroupsAsync();

        return View(new GroupTaskRequest
        {
            ClassGroup = group ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupTaskRequest request)
    {
        ViewData["AllowedGroups"] = await _classAccess.GetAllowedGroupsAsync();

        if (ModelState.IsValid &&
            !await _classAccess.CanAccessGroupAsync(request.ClassGroup))
        {
            ModelState.AddModelError(
                nameof(request.ClassGroup),
                "You can only assign tasks to your own classes.");
        }

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var students = await _students.GetAllAsync();

        var studentIds = students
            .Where(student =>
                SchoolGroups.GetName(student) == request.ClassGroup)
            .Select(student => student.Id)
            .ToList();

        if (studentIds.Count == 0)
        {
            ModelState.AddModelError(
                nameof(request.ClassGroup),
                "Add students to this group before assigning a task.");

            return View(request);
        }

        var task = await _tasks.CreateAsync(new GroupTask
        {
            ClassGroup = request.ClassGroup,
            Kind = request.Kind,
            TextReference = request.TextReference.Trim(),
            TextVersion = request.TextVersion.Trim(),
            StudentIds = studentIds
        });

        TempData["SuccessMessage"] = "Task assigned successfully.";

        return RedirectToAction(nameof(Details), new { id = task.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var task = await _tasks.GetByIdAsync(id);

        // A task for another teacher's class looks the same as a missing one.
        if (task is null || !await _classAccess.CanAccessGroupAsync(task.ClassGroup))
        {
            return NotFound("Task not found.");
        }

        var students = (await _students.GetAllAsync())
            .ToDictionary(student => student.Id);

        var model = new GroupTaskDetailsViewModel
        {
            Task = task
        };

        foreach (var studentId in task.StudentIds)
        {
            students.TryGetValue(studentId, out var student);

            var assessments = await _assessments.GetForStudentAsync(studentId);

            model.Students.Add(new GroupTaskStudentViewModel
            {
                StudentId = studentId,
                Name = student is null
                    ? "Student no longer available"
                    : $"{student.FirstName} {student.LastName}",
                IsAvailable = student is not null,
                Assessment = assessments.FirstOrDefault(
                    assessment => assessment.GroupTaskId == task.Id)
            });
        }

        model.Students = model.Students
            .OrderBy(student => student.Name)
            .ToList();

        return View(model);
    }
}