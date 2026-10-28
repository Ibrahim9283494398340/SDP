using System.ComponentModel.DataAnnotations;

namespace OxuMen.Models;

public class StudentRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Range(1, 4, ErrorMessage = "Grade level must be between 1 and 4.")]
    public int GradeLevel { get; set; }

    [Required]
    [StringLength(20)]
    public string ClassGroup { get; set; } = string.Empty;
}