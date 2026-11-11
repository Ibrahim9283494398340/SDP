using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;

namespace OxuMen.Controllers;

// Teachers and admins. A teacher only sees and changes students in their
// own classes; a student outside them looks the same as a missing one.
// Students use MyController.
[Authorize(Roles = AppRoles.TeacherOrAdmin)]
public class StudentPagesController : Controller
{
    private readonly IStudentRepository _studentRepository;
    private readonly IReadingAssessmentRepository _assessmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClassAccess _classAccess;

    private static readonly string[] GroupNames = Enumerable.Range(1, 5)
        .SelectMany(grade => "ABCD".Select(letter => $"{grade}{letter}"))
        .ToArray();

    public StudentPagesController(
        IStudentRepository studentRepository,
        IReadingAssessmentRepository assessmentRepository,
        IUserRepository userRepository,
        IClassAccess classAccess)
    {
        _studentRepository = studentRepository;
        _assessmentRepository = assessmentRepository;
        _userRepository = userRepository;
        _classAccess = classAccess;
    }

    private static string GetGroupName(Student student)
    {
        var group = student.ClassGroup.Trim().ToUpperInvariant();

        return group.Length == 1
            ? $"{student.GradeLevel}{group}"
            : group;
    }

    // The student, or null when it does not exist or is not in one of
    // the signed-in teacher's classes.
    private async Task<Student?> GetAccessibleStudentAsync(Guid id)
    {
        var student = await _studentRepository.GetByIdAsync(id);

        return student is not null &&
               await _classAccess.CanAccessStudentAsync(student)
            ? student
            : null;
    }

    // Used by the class drop-downs on the Create and Edit forms.
    private async Task SetAllowedGroupsAsync()
    {
        ViewData["AllowedGroups"] = await _classAccess.GetAllowedGroupsAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var students = await _studentRepository.GetAllAsync();
        var allowed = await _classAccess.GetAllowedGroupsAsync();

        // Only the signed-in teacher's classes, in 1A..5D order.
        var groups = GroupNames
            .Where(group => allowed.Contains(group))
            .ToDictionary(
                group => group,
                group => students.Count(student => GetGroupName(student) == group));

        return View(groups);
    }

    [HttpGet]
    public async Task<IActionResult> Group(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return NotFound("Group not found.");
        }

        group = group.Trim().ToUpperInvariant();

        if (!GroupNames.Contains(group))
        {
            return NotFound("Group not found.");
        }

        if (!await _classAccess.CanAccessGroupAsync(group))
        {
            return Forbid();
        }

        var students = await _studentRepository.GetAllAsync();

        var groupStudents = students
            .Where(student => GetGroupName(student) == group)
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .ToList();

        ViewData["GroupName"] = group;

        return View(groupStudents);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var student = await GetAccessibleStudentAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var login = await _userRepository.GetForStudentAsync(id);

        ViewData["LoginUserName"] = login?.UserName;

        return View(student);
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? group)
    {
        var request = new StudentRequest();

        if (!string.IsNullOrWhiteSpace(group))
        {
            group = group.Trim().ToUpperInvariant();

            if (!GroupNames.Contains(group))
            {
                return NotFound("Group not found.");
            }

            if (!await _classAccess.CanAccessGroupAsync(group))
            {
                return Forbid();
            }

            request.ClassGroup = group;
            request.GradeLevel = group[0] - '0';
        }

        await SetAllowedGroupsAsync();

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentRequest request)
    {
        var group = (request.ClassGroup ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (!GroupNames.Contains(group))
        {
            ModelState.AddModelError(
                nameof(StudentRequest.ClassGroup),
                "Choose a group from 1A to 5D.");
        }
        else if (!await _classAccess.CanAccessGroupAsync(group))
        {
            ModelState.AddModelError(
                nameof(StudentRequest.ClassGroup),
                "You can only add students to your own classes.");
        }
        else
        {
            request.ClassGroup = group;
            request.GradeLevel = group[0] - '0';

            ModelState.Remove(nameof(StudentRequest.GradeLevel));
        }

        if (!ModelState.IsValid)
        {
            await SetAllowedGroupsAsync();
            return View(request);
        }

        var student = new Student
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            GradeLevel = request.GradeLevel,
            ClassGroup = request.ClassGroup
        };

        await _studentRepository.CreateAsync(student);

        TempData["SuccessMessage"] = "Student created successfully.";

        return RedirectToAction(nameof(Group), new { group });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var student = await GetAccessibleStudentAsync(id);

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
        await SetAllowedGroupsAsync();

        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StudentRequest request)
    {
        var existingStudent = await GetAccessibleStudentAsync(id);

        if (existingStudent is null)
        {
            return NotFound("Student not found.");
        }

        ViewData["StudentId"] = id;

        // A teacher can only move a student into another of their classes.
        if (!string.IsNullOrWhiteSpace(request.ClassGroup) &&
            !await _classAccess.CanAccessGroupAsync(request.ClassGroup))
        {
            ModelState.AddModelError(
                nameof(StudentRequest.ClassGroup),
                "You can only move students into your own classes.");
        }

        if (!ModelState.IsValid)
        {
            await SetAllowedGroupsAsync();
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

        var updatedStudent = await _studentRepository.UpdateAsync(student);

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
        var student = await GetAccessibleStudentAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var assessments = await _assessmentRepository.GetForStudentAsync(id);

        ViewData["HasAssessments"] = assessments.Count > 0;

        return View(student);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var student = await GetAccessibleStudentAsync(id);

        if (student is null)
        {
            return NotFound("Student not found.");
        }

        var assessments = await _assessmentRepository.GetForStudentAsync(id);

        if (assessments.Count > 0)
        {
            ViewData["HasAssessments"] = true;

            ModelState.AddModelError(
                string.Empty,
                "This student has assessments and cannot be deleted.");

            return View("Delete", student);
        }

        var group = GetGroupName(student);

        // The student's login goes with the profile.
        await _userRepository.DeleteForStudentAsync(id);

        var deleted = await _studentRepository.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound("Student not found.");
        }

        TempData["SuccessMessage"] = "Student deleted successfully.";

        return GroupNames.Contains(group)
            ? RedirectToAction(nameof(Group), new { group })
            : RedirectToAction(nameof(Index));
    }
}
