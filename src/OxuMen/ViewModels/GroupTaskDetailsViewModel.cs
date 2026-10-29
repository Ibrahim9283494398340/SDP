using OxuMen.Models;

namespace OxuMen.ViewModels;

public class GroupTaskDetailsViewModel
{
    public GroupTask Task { get; set; } = new();

    public List<GroupTaskStudentViewModel> Students { get; set; } = new();
}

public class GroupTaskStudentViewModel
{
    public Guid StudentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public ReadingAssessment? Assessment { get; set; }
}