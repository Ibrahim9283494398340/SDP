using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;

namespace OxuMen.Controllers;

// Teachers and admins, limited to the teacher's own classes.
[Authorize(Roles = AppRoles.TeacherOrAdmin)]
[ApiController]
[Route("api/reading-assessments")]
public class ReadingAssessmentsController : ControllerBase
{
    private readonly IReadingAssessmentRepository _assessmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassAccess _classAccess;

    public ReadingAssessmentsController(
        IReadingAssessmentRepository assessmentRepository,
        IStudentRepository studentRepository,
        IClassAccess classAccess)
    {
        _assessmentRepository = assessmentRepository;
        _studentRepository = studentRepository;
        _classAccess = classAccess;
    }

    // The student, or null when missing or outside the teacher's classes.
    private async Task<Student?> GetAccessibleStudentAsync(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        return student is not null &&
               await _classAccess.CanAccessStudentAsync(student)
            ? student
            : null;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReadingAssessment>> GetById(Guid id)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(id);

        if (assessment is null ||
            await GetAccessibleStudentAsync(assessment.StudentId) is null)
        {
            return NotFound(new { message = "Assessment not found." });
        }

        return Ok(assessment);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<ReadingAssessment>>> GetForStudent(
        Guid studentId)
    {
        var student = await GetAccessibleStudentAsync(studentId);

        if (student is null)
        {
            return NotFound(new { message = "Student not found." });
        }

        var assessments =
            await _assessmentRepository.GetForStudentAsync(studentId);

        return Ok(assessments);
    }

    [HttpPost]
    public async Task<ActionResult<ReadingAssessment>> Create(
        ReadingAssessmentRequest request)
    {
        var student = await GetAccessibleStudentAsync(request.StudentId);

        if (student is null)
        {
            return NotFound(new { message = "Student not found." });
        }

        var assessment = new ReadingAssessment
        {
            StudentId = request.StudentId,
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

        var createdAssessment =
            await _assessmentRepository.CreateAsync(assessment);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdAssessment.Id },
            createdAssessment);
    }
}