using System.ComponentModel.DataAnnotations;

namespace OxuMen.Models;

public class GroupTask
{
    public Guid Id { get; set; }

    public string ClassGroup { get; set; } = string.Empty;

    public AssessmentKind Kind { get; set; }

    public string TextReference { get; set; } = string.Empty;

    public string TextVersion { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public List<Guid> StudentIds { get; set; } = new();
}

public class GroupTaskRequest
{
    [Required]
    [RegularExpression(@"^[1-5][A-D]$", ErrorMessage = "Select a valid group.")]
    public string ClassGroup { get; set; } = string.Empty;

    [EnumDataType(typeof(AssessmentKind))]
    public AssessmentKind Kind { get; set; } = AssessmentKind.Initial;

    [Required]
    [StringLength(100)]
    public string TextReference { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string TextVersion { get; set; } = "1";
}