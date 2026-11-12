using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OxuMen.Models;

public class GroupTask
{
    public Guid Id { get; set; }

    public string ClassGroup { get; set; } = string.Empty;

    public AssessmentKind Kind { get; set; }

    public string TextReference { get; set; } = string.Empty;

    public string TextVersion { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    // Not a column: filled from the GroupTaskStudents table by the repository,
    // so controllers and views can keep using a plain list of IDs.
    [NotMapped]
    public List<Guid> StudentIds { get; set; } = new();

    // Rows of the GroupTaskStudents join table.
    [JsonIgnore]
    public List<GroupTaskStudent> Assignments { get; set; } = new();
}

// One row per student assigned to a group task.
public class GroupTaskStudent
{
    public Guid GroupTaskId { get; set; }

    public Guid StudentId { get; set; }
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
