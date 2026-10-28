using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;

namespace OxuMen.Controllers;

public class StudentPagesController : Controller
{
    private readonly IStudentRepository _studentRepository;
    private readonly IReadingAssessmentRepository _assessmentRepository;

    public StudentPagesController(
        IStudentRepository studentRepository,
        IReadingAssessmentRepository assessmentRepository)
    {
        _studentRepository = studentRepository;
        _assessmentRepository = assessmentRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var students = await _studentRepository.GetAllAsync();

        var sortedStudents = students
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToList();

        return View(sortedStudents);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        return View(student);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new StudentRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var student = new Student
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            GradeLevel = request.GradeLevel,
            ClassGroup = request.ClassGroup.Trim()
        };

        var createdStudent =
            await _studentRepository.CreateAsync(student);

        TempData["SuccessMessage"] = "Student created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = createdStudent.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var request = new StudentRequest
        {
            FirstName = student.FirstName,
            LastName = student.LastName,
            GradeLevel = student.GradeLevel,
            ClassGroup = student.ClassGroup
        };

        ViewData["StudentId"] = student.Id;

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        StudentRequest request)
    {
        var existingStudent =
            await _studentRepository.GetByIdAsync(id);

        if (existingStudent is null)
        {
            return NotFound("Student not found.");
        }

        ViewData["StudentId"] = id;

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var student = new Student
        {
            Id = id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            GradeLevel = request.GradeLevel,
            ClassGroup = request.ClassGroup.Trim()
        };

        var updatedStudent =
            await _studentRepository.UpdateAsync(student);

        if (updatedStudent is null)
        {
            return NotFound("Student not found.");
        }

        TempData["SuccessMessage"] = "Student updated successfully.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var assessments =
            await _assessmentRepository.GetForStudentAsync(id);

        ViewData["HasAssessments"] = assessments.Count > 0;

        return View(student);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var assessments =
            await _assessmentRepository.GetForStudentAsync(id);

        if (assessments.Count > 0)
        {
            ViewData["HasAssessments"] = true;

            ModelState.AddModelError(
                string.Empty,
                "This student has assessments and cannot be deleted.");

            return View("Delete", student);
        }

        var deleted = await _studentRepository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound("Student not found.");
        }

        TempData["SuccessMessage"] = "Student deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}