using System.ComponentModel.DataAnnotations;

namespace OxuMen.Models;

public class ReadingAssessmentRequest : IValidatableObject
{
    public Guid StudentId { get; set; }

    [EnumDataType(typeof(AssessmentKind))]
    public AssessmentKind Kind { get; set; } = AssessmentKind.Initial;

    [Required]
    [StringLength(100)]
    public string TextReference { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string TextVersion { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset? AssessedAtUtc { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalWordsRead { get; set; }

    [Range(0, int.MaxValue)]
    public int CorrectWordsRead { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "86400",
        ErrorMessage = "Duration must be greater than zero and no more than 24 hours.")]
    public decimal DurationSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int? ComprehensionCorrectAnswers { get; set; }

    [Range(1, int.MaxValue)]
    public int? ComprehensionTotalQuestions { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (StudentId == Guid.Empty)
        {
            yield return new ValidationResult(
                "A student ID is required.",
                new[] { nameof(StudentId) });
        }

        if (CorrectWordsRead > TotalWordsRead)
        {
            yield return new ValidationResult(
                "Correct words cannot exceed total words read.",
                new[] { nameof(CorrectWordsRead) });
        }

        if (ComprehensionCorrectAnswers.HasValue !=
            ComprehensionTotalQuestions.HasValue)
        {
            yield return new ValidationResult(
                "Provide both comprehension fields, or leave both out.",
                new[]
                {
                    nameof(ComprehensionCorrectAnswers),
                    nameof(ComprehensionTotalQuestions)
                });
        }

        if (ComprehensionCorrectAnswers.HasValue &&
            ComprehensionTotalQuestions.HasValue &&
            ComprehensionCorrectAnswers.Value >
            ComprehensionTotalQuestions.Value)
        {
            yield return new ValidationResult(
                "Correct answers cannot exceed the number of questions.",
                new[] { nameof(ComprehensionCorrectAnswers) });
        }
    }
}