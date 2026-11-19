namespace OxuMen.ViewModels;

// The teacher dashboard (Home/Index). Everything here is limited to the
// classes the signed-in teacher may see; an admin sees every class.
public class DashboardViewModel
{
    public bool IsAdmin { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyList<string> Classes { get; set; } = Array.Empty<string>();

    public int StudentCount { get; set; }

    public int TaskCount { get; set; }

    // Students with at least one result.
    public int AssessedStudentCount { get; set; }

    // Averages over each student's latest result; null when nobody has one.
    public decimal? AverageWordsPerMinute { get; set; }

    public decimal? AverageAccuracy { get; set; }

    public decimal? AverageComprehension { get; set; }

    // Latest result per student, newest first.
    public List<DashboardStudentRow> RecentResults { get; set; } = new();
}

public class DashboardStudentRow
{
    public Guid StudentId { get; set; }

    public Guid AssessmentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ClassGroup { get; set; } = string.Empty;

    public DateTimeOffset AssessedAtUtc { get; set; }

    public decimal? WordsPerMinute { get; set; }

    public decimal? Accuracy { get; set; }

    // Comprehension as a percentage of questions answered correctly.
    public decimal? Comprehension { get; set; }
}
