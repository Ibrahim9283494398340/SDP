namespace OxuMen.Models;

// Links a parent account to one of their children. A parent can have
// several children and a child can have two parents.
public class ParentChild
{
    public Guid ParentUserId { get; set; }

    public Guid StudentId { get; set; }
}

// The code a student (or their teacher) gives to a parent so the parent
// can link their account to that student. One code per student.
public class ParentCode
{
    public Guid StudentId { get; set; }

    // Stored without the dash, upper case, e.g. "K7Q42MXA".
    public string Code { get; set; } = string.Empty;

    public static string Normalize(string? code)
    {
        return new string((code ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    // "K7Q42MXA" -> "K7Q4-2MXA", easier to read out and type.
    public static string Format(string code)
    {
        return code.Length == 8 ? $"{code[..4]}-{code[4..]}" : code;
    }
}

// One message between a parent and a teacher about one child. A
// conversation is all messages with the same student, parent and teacher.
public class Message
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid ParentUserId { get; set; }

    public Guid TeacherUserId { get; set; }

    // Either ParentUserId or TeacherUserId.
    public Guid SenderUserId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }

    // Set when the other person opens the conversation.
    public DateTime? ReadAtUtc { get; set; }
}
