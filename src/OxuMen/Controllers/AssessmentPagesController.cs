using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

public class AssessmentPagesController : Controller
{
    private readonly IStudentRepository _students;
    private readonly IReadingAssessmentRepository _assessments;
    private readonly JsonGroupTaskRepository _tasks;

    public AssessmentPagesController(
        IStudentRepository students,
        IReadingAssessmentRepository assessments,
        JsonGroupTaskRepository tasks)
    {
        _students = students;
        _assessments = assessments;
        _tasks = tasks;
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

        if (student is null)
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

        if (task is null || !task.StudentIds.Contains(studentId))
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

        if (task is null || !task.StudentIds.Contains(studentId))
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

        return View(new AssessmentPageViewModel
        {
            Student = student,
            GroupTaskId = assessment.GroupTaskId,
            Assessment = assessment
        });
    }
}