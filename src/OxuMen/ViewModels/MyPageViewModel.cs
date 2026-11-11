using OxuMen.Models;

namespace OxuMen.ViewModels;

public class MyPageViewModel
{
    public Student Student { get; set; } = new();

    public List<MyTaskViewModel> Tasks { get; set; } = new();

    public List<ReadingAssessment> Assessments { get; set; } = new();
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
