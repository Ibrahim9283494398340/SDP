namespace OxuMen.Models;

public static class SchoolGroups
{
    public static readonly string[] All = Enumerable.Range(1, 5)
        .SelectMany(grade => "ABCD".Select(letter => $"{grade}{letter}"))
        .ToArray();

    public static string GetName(Student student)
    {
        var group = student.ClassGroup.Trim().ToUpperInvariant();

        return group.Length == 1
            ? $"{student.GradeLevel}{group}"
            : group;
    }
}