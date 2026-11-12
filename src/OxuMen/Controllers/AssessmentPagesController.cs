using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Teachers and admins, limited to the teacher's own classes.
// Students use MyController.
[Authorize(Roles = AppRoles.TeacherOrAdmin)]
public class AssessmentPagesController : Controller
{
    private readonly IStudentRepository _students;
    private readonly IReadingAssessmentRepository _assessments;
    private readonly IGroupTaskRepository _tasks;
    private readonly IClassAccess _classAccess;

    public AssessmentPagesController(
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
    public async Task<IActionResult> Index(Guid? studentId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid student ID.");
        }

        if (!studentId.HasValue)
        {
            return RedirectToAction("Index", "GroupTasks");
        }

        var student = await _students.GetByIdAsync(studentId.Value);

        if (student is null || !await _classAccess.CanAccessStudentAsync(student))
        {
            return NotFound("Student not found.");
        }

        var model = new AssessmentPageViewModel
        {
            Student = student,
            Assessments = await _assessments.GetForStudentAsync(student.Id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid studentId, Guid? taskId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid student or task ID.");
        }

        if (!taskId.HasValue)
        {
            return RedirectToAction("Index", "GroupTasks");
        }

        var task = await _tasks.GetByIdAsync(taskId.Value);

        if (task is null ||
            !task.StudentIds.Contains(studentId) ||
            !await _classAccess.CanAccessGroupAsync(task.ClassGroup))
        {
            return NotFound("Student is not assigned to this task.");
        }

        var student = await _students.GetByIdAsync(studentId);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var assessments = await _assessments.GetForStudentAsync(studentId);

        var existing = assessments.FirstOrDefault(
            assessment => assessment.GroupTaskId == task.Id);

        if (existing is not null)
        {
            return RedirectToAction(
                nameof(Details),
                new { id = existing.Id });
        }

        var model = new AssessmentPageViewModel
        {
            Student = student,
            GroupTaskId = task.Id,
            Input = new ReadingAssessmentRequest
            {
                StudentId = studentId,
                Kind = task.Kind,
                TextReference = task.TextReference,
                TextVersion = task.TextVersion,
                AssessedAtUtc = DateTimeOffset.UtcNow
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid studentId,
        Guid taskId,
        [Bind(Prefix = "Input")] ReadingAssessmentRequest request)
    {
        var task = await _tasks.GetByIdAsync(taskId);

        if (task is null ||
            !task.StudentIds.Contains(studentId) ||
            !await _classAccess.CanAccessGroupAsync(task.ClassGroup))
        {
            return NotFound("Student is not assigned to this task.");
        }

        var student = await _students.GetByIdAsync(studentId);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        if (request.StudentId != studentId)
        {
            return BadRequest("Student IDs do not match.");
        }

        request.Kind = task.Kind;
        request.TextReference = task.TextReference;
        request.TextVersion = task.TextVersion;

        ModelState.Remove("Input.Kind");
        ModelState.Remove("Input.TextReference");
        ModelState.Remove("Input.TextVersion");

        if (!ModelState.IsValid)
        {
            return View(new AssessmentPageViewModel
            {
                Student = student,
                GroupTaskId = task.Id,
                Input = request
            });
        }

        var assessment = new ReadingAssessment
        {
            GroupTaskId = task.Id,
            StudentId = studentId,
            Kind = task.Kind,
            TextReference = task.TextReference,
            TextVersion = task.TextVersion,
            AssessedAtUtc = request.AssessedAtUtc!.Value.ToUniversalTime(),
            TotalWordsRead = request.TotalWordsRead,
            CorrectWordsRead = request.CorrectWordsRead,
            DurationSeconds = request.DurationSeconds,
            ComprehensionCorrectAnswers =
                request.ComprehensionCorrectAnswers,
            ComprehensionTotalQuestions =
                request.ComprehensionTotalQuestions,
            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim()
        };

        try
        {
            await _assessments.CreateAsync(assessment);

            TempData["SuccessMessage"] =
                "Student results saved successfully.";
        }
        catch (InvalidOperationException)
        {
            var assessments =
                await _assessments.GetForStudentAsync(studentId);

            if (!assessments.Any(item => item.GroupTaskId == task.Id))
            {
                throw;
            }

            TempData["SuccessMessage"] =
                "Results were already recorded for this student.";
        }

        return RedirectToAction(
            "Details",
            "GroupTasks",
            new { id = task.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid assessment ID.");
        }

        var assessment = await _assessments.GetByIdAsync(id);

        if (assessment is null)
        {
            return NotFound("Assessment not found.");
        }

        var student = await _students.GetByIdAsync(assessment.StudentId);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        // Visible if the student is in one of the teacher's classes now,
        // or the result was recorded for a task in one of them.
        var canSee = await _classAccess.CanAccessStudentAsync(student);

        if (!canSee && assessment.GroupTaskId.HasValue)
        {
            var task = await _tasks.GetByIdAsync(assessment.GroupTaskId.Value);

            canSee = task is not null &&
                     await _classAccess.CanAccessGroupAsync(task.ClassGroup);
        }

        if (!canSee)
        {
            return NotFound("Assessment not found.");
        }

        return View(new AssessmentPageViewModel
        {
            Student = student,
            GroupTaskId = assessment.GroupTaskId,
            Assessment = assessment
        });
    }
}