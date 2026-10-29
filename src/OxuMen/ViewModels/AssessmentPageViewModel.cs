using OxuMen.Models;

namespace OxuMen.ViewModels;

public class AssessmentPageViewModel
{
    public Student Student { get; set; } = new();

    public ReadingAssessmentRequest Input { get; set; } = new();

    public List<ReadingAssessment> Assessments { get; set; } = new();

    public ReadingAssessment? Assessment { get; set; }

    public Guid? GroupTaskId { get; set; }
}