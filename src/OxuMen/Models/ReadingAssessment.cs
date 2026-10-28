using System.Text.Json.Serialization;

namespace OxuMen.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssessmentKind
{
    Initial,
    Reassessment
}

public class ReadingAssessment
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public AssessmentKind Kind { get; set; }

    public string TextReference { get; set; } = string.Empty;

    public string TextVersion { get; set; } = string.Empty;

    public DateTimeOffset AssessedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public int TotalWordsRead { get; set; }

    public int CorrectWordsRead { get; set; }

    public decimal DurationSeconds { get; set; }

    public int? ComprehensionCorrectAnswers { get; set; }

    public int? ComprehensionTotalQuestions { get; set; }

    public string? Notes { get; set; }

    public decimal? WordsPerMinute =>
        DurationSeconds > 0
            ? Math.Round(TotalWordsRead * 60m / DurationSeconds, 2)
            : null;

    public decimal? CorrectWordsPerMinute =>
        DurationSeconds > 0
            ? Math.Round(CorrectWordsRead * 60m / DurationSeconds, 2)
            : null;

    public decimal? AccuracyPercentage =>
        TotalWordsRead > 0
            ? Math.Round(CorrectWordsRead * 100m / TotalWordsRead, 2)
            : null;
}