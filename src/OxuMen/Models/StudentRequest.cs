using System.ComponentModel.DataAnnotations;

namespace OxuMen.Models;

public class StudentRequest : IValidatableObject
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Grade level must be between 1 and 5.")]
    public int GradeLevel { get; set; }

    [Required]
    [RegularExpression(
        @"^[1-5][A-D]$",
        ErrorMessage = "Choose a class group from 1A to 5D.")]
    public string ClassGroup { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(ClassGroup) &&
            ClassGroup.Length == 2 &&
            ClassGroup[0] >= '1' &&
            ClassGroup[0] <= '5')
        {
            var groupGrade = ClassGroup[0] - '0';

            if (GradeLevel != groupGrade)
            {
                yield return new ValidationResult(
                    "The class group must match the selected grade.",
                    new[] { nameof(ClassGroup) });
            }
        }
    }
}