using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

public class AssessmentPagesController : Controller
{
    private readonly IStudentRepository _students;
    private readonly IReadingAssessmentRepository _assessments;

    public AssessmentPagesController(
        IStudentRepository students,
        IReadingAssessmentRepository assessments)
    {
        _students = students;
        _assessments = assessments;
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
            var students = await _students.GetAllAsync();

            var sortedStudents = students
                .OrderBy(student => student.ClassGroup)
                .ThenBy(student => student.LastName)
                .ThenBy(student => student.FirstName)
                .ToList();

            return View("Students", sortedStudents);
        }

        var student = await _students.GetByIdAsync(studentId.Value);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var model = new AssessmentPageViewModel
        {
            Student = student,
            Assessments =
                await _assessments.GetForStudentAsync(student.Id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid studentId)
    {
        var student = await _students.GetByIdAsync(studentId);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var model = new AssessmentPageViewModel
        {
            Student = student,
            Input = new ReadingAssessmentRequest
            {
                StudentId = studentId,
                Kind = AssessmentKind.Initial,
                AssessedAtUtc = DateTimeOffset.UtcNow
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid studentId,
        [Bind(Prefix = "Input")] ReadingAssessmentRequest request)
    {
        var student = await _students.GetByIdAsync(studentId);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        if (request.StudentId != studentId)
        {
            return BadRequest("Student IDs do not match.");
        }

        if (!ModelState.IsValid)
        {
            return View(new AssessmentPageViewModel
            {
                Student = student,
                Input = request
            });
        }

        var assessment = new ReadingAssessment
        {
            StudentId = studentId,
            Kind = request.Kind,
            TextReference = request.TextReference.Trim(),
            TextVersion = request.TextVersion.Trim(),
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

        var created = await _assessments.CreateAsync(assessment);

        TempData["SuccessMessage"] = "Assessment saved successfully.";

        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
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
            Assessment = assessment
        });
    }
}