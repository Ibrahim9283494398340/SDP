using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;

namespace OxuMen.Controllers;

[ApiController]
[Route("api/reading-assessments")]
public class ReadingAssessmentsController : ControllerBase
{
    private readonly IReadingAssessmentRepository _assessmentRepository;
    private readonly IStudentRepository _studentRepository;

    public ReadingAssessmentsController(
        IReadingAssessmentRepository assessmentRepository,
        IStudentRepository studentRepository)
    {
        _assessmentRepository = assessmentRepository;
        _studentRepository = studentRepository;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReadingAssessment>> GetById(Guid id)
    {
        var assessment = await _assessmentRepository.GetByIdAsync(id);

        if (assessment is null)
        {
            return NotFound(new { message = "Assessment not found." });
        }

        return Ok(assessment);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<ReadingAssessment>>> GetForStudent(
        Guid studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);

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
        var student =
            await _studentRepository.GetByIdAsync(request.StudentId);

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