using OxuMen.Models;

namespace OxuMen.ViewModels;

public class ParentHomeViewModel
{
    public List<Student> Children { get; set; } = new();

    public AddChildRequest AddChild { get; set; } = new();
}

public class ParentChildViewModel
{
    public Student Student { get; set; } = new();

    public List<MyTaskViewModel> Tasks { get; set; } = new();

    public List<ReadingAssessment> Assessments { get; set; } = new();

    // Teachers of the child's class, whom the parent can message.
    public List<AppUser> Teachers { get; set; } = new();
}

public class ConversationListItemViewModel
{
    public Student? Student { get; set; }

    public Guid StudentId { get; set; }

    // The other person: the teacher for a parent, the parent for a teacher.
    public Guid OtherUserId { get; set; }

    public string OtherUserName { get; set; } = string.Empty;

    public Message LastMessage { get; set; } = new();

    public int UnreadCount { get; set; }
}

public class ConversationViewModel
{
    public Student Student { get; set; } = new();

    public Guid OtherUserId { get; set; }

    public string OtherUserName { get; set; } = string.Empty;

    // "teacher" or "parent", for the page heading.
    public string OtherRole { get; set; } = string.Empty;

    public Guid MyUserId { get; set; }

    public List<Message> Messages { get; set; } = new();

    public SendMessageRequest Input { get; set; } = new();
}
