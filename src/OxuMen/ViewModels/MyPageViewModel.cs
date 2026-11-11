using OxuMen.Models;

namespace OxuMen.ViewModels;

public class MyPageViewModel
{
    public Student Student { get; set; } = new();

    public List<MyTaskViewModel> Tasks { get; set; } = new();

    public List<ReadingAssessment> Assessments { get; set; } = new();

    // Shown so the student can give it to a parent (formatted, with dash).
    public string ParentCode { get; set; } = string.Empty;
}

public class MyTaskViewModel
{
    public GroupTask Task { get; set; } = new();

    public ReadingAssessment? Assessment { get; set; }
}

public class StudentLoginViewModel
{
    public Student Student { get; set; } = new();

    public string? ExistingUserName { get; set; }

    public StudentLoginRequest Input { get; set; } = new();
}
