using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;

namespace OxuMen.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;

    public StudentsController(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Student>>> GetAll()
    {
        var students = await _studentRepository.GetAllAsync();

        return Ok(students);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Student>> GetById(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        if (student is null)
        {
            return NotFound(new { message = "Student not found." });
        }

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create(StudentRequest request)
    {
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