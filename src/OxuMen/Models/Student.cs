namespace OxuMen.Models;

public class Student
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int GradeLevel { get; set; }

    public string ClassGroup { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}