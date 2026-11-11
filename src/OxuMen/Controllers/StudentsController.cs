using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;

namespace OxuMen.Controllers;

// Teachers and admins, limited to the teacher's own classes.
[Authorize(Roles = AppRoles.TeacherOrAdmin)]
[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IClassAccess _classAccess;

    public StudentsController(
        IStudentRepository studentRepository,
        IClassAccess classAccess)
    {
        _studentRepository = studentRepository;
        _classAccess = classAccess;
    }

    [HttpGet]
    public async Task<ActionResult<List<Student>>> GetAll()
    {
        var students = await _studentRepository.GetAllAsync();
        var allowed = await _classAccess.GetAllowedGroupsAsync();

        return Ok(students
            .Where(student => allowed.Contains(SchoolGroups.GetName(student)))
            .ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Student>> GetById(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null || !await _classAccess.CanAccessStudentAsync(student))
        {
            return NotFound(new { message = "Student not found." });
        }

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create(StudentRequest request)
    {
        if (!await _classAccess.CanAccessGroupAsync(request.ClassGroup))
        {
            return Forbid();
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

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdStudent.Id },
            createdStudent);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Student>> Update(
        Guid id,
        StudentRequest request)
    {
        var existing = await _studentRepository.GetByIdAsync(id);

        if (existing is null || !await _classAccess.CanAccessStudentAsync(existing))
        {
            return NotFound(new { message = "Student not found." });
        }

        if (!await _classAccess.CanAccessGroupAsync(request.ClassGroup))
        {
            return Forbid();
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
            return NotFound(new { message = "Student not found." });
        }

        return Ok(updatedStudent);
    }
}