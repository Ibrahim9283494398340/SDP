using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IStudentRepository _studentRepository;
    private readonly IReadingAssessmentRepository _assessmentRepository;
    private readonly IGroupTaskRepository _groupTaskRepository;
    private readonly IClassAccess _classAccess;

    public HomeController(
        ILogger<HomeController> logger,
        IStudentRepository studentRepository,
        IReadingAssessmentRepository assessmentRepository,
        IGroupTaskRepository groupTaskRepository,
        IClassAccess classAccess)
    {
        _logger = logger;
        _studentRepository = studentRepository;
        _assessmentRepository = assessmentRepository;
        _groupTaskRepository = groupTaskRepository;
        _classAccess = classAccess;
    }

    // "/" lands here: send each role to its own start page.
    [HttpGet]
    public async Task<IActionResult> Start()
    {
        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Teachers", "Admin");
        }

        if (User.IsInRole(AppRoles.Teacher))
        {
            return RedirectToAction("Index", "Home");
        }

        if (User.IsInRole(AppRoles.Student))
        {
            return RedirectToAction("Index", "My");
        }

        if (User.IsInRole(AppRoles.Parent))
        {
            return RedirectToAction("Index", "Parent");
        }

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login", "Account");
    }

    // Teacher dashboard. Only the signed-in teacher's classes are counted;
    // an admin sees every class.
    [HttpGet]
    [Authorize(Roles = AppRoles.TeacherOrAdmin)]
    public async Task<IActionResult> Index()
    {
        var allowed = await _classAccess.GetAllowedGroupsAsync();

        var students = (await _studentRepository.GetAllAsync())
            .Where(student => allowed.Contains(SchoolGroups.GetName(student)))
            .ToList();

        var taskCount = (await _groupTaskRepository.GetAllAsync())
            .Count(task => allowed.Contains(task.ClassGroup.Trim().ToUpperInvariant()));

        var assessments = await _assessmentRepository.GetForStudentsAsync(
            students.Select(student => student.Id).ToList());

        // Newest result per student (the list is already newest first).
        var latest = assessments
            .GroupBy(assessment => assessment.StudentId)
            .Select(group => group.First())
            .ToList();

        var studentsById = students.ToDictionary(student => student.Id);

        var rows = latest
            .Select(assessment =>
            {
                var student = studentsById[assessment.StudentId];

                return new DashboardStudentRow
                {
                    StudentId = student.Id,
                    AssessmentId = assessment.Id,
                    Name = $"{student.FirstName} {student.LastName}".Trim(),
                    ClassGroup = SchoolGroups.GetName(student),
                    AssessedAtUtc = assessment.AssessedAtUtc,
                    WordsPerMinute = assessment.WordsPerMinute,
                    Accuracy = assessment.AccuracyPercentage,
                    Comprehension = GetComprehension(assessment)
                };
            })
            .ToList();

        var isAdmin = User.IsInRole(AppRoles.Admin);

        var model = new DashboardViewModel
        {
            IsAdmin = isAdmin,
            DisplayName = User.Identity?.Name ?? string.Empty,
            // An admin "has" every class; show only the ones in use.
            Classes = isAdmin
                ? students
                    .Select(SchoolGroups.GetName)
                    .Distinct()
                    .OrderBy(group => group)
                    .ToList()
                : allowed,
            StudentCount = students.Count,
            TaskCount = taskCount,
            AssessedStudentCount = rows.Count,
            AverageWordsPerMinute = Average(rows.Select(row => row.WordsPerMinute)),
            AverageAccuracy = Average(rows.Select(row => row.Accuracy)),
            AverageComprehension = Average(rows.Select(row => row.Comprehension)),
            RecentResults = rows.Take(8).ToList()
        };

        return View(model);
    }

    private static decimal? GetComprehension(ReadingAssessment assessment)
    {
        return assessment.ComprehensionCorrectAnswers is int correct &&
               assessment.ComprehensionTotalQuestions is int total &&
               total > 0
            ? Math.Round(correct * 100m / total, 0)
            : null;
    }

    private static decimal? Average(IEnumerable<decimal?> values)
    {
        var present = values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        return present.Count > 0
            ? Math.Round(present.Average(), 0)
            : null;
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
