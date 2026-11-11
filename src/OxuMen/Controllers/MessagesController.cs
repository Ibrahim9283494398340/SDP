using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;
using OxuMen.ViewModels;

namespace OxuMen.Controllers;

// Messages between a parent and a teacher about one child.
// - A parent can write to a teacher of their linked child's class.
// - A teacher can write to a parent linked to a student in their classes.
// - Old conversations stay readable after a class changes hands, but new
//   messages need the current parent-child and teacher-class links.
// Anyone else's conversation looks the same as a missing one.
[Authorize(Roles = AppRoles.TeacherOrParent)]
public class MessagesController : Controller
{
    private readonly IMessageRepository _messages;
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;
    private readonly IParentRepository _parents;

    public MessagesController(
        IMessageRepository messages,
        IUserRepository users,
        IStudentRepository students,
        IParentRepository parents)
    {
        _messages = messages;
        _users = users;
        _students = students;
        _parents = parents;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = User.GetUserId();

        if (me is null)
        {
            return Challenge();
        }

        var isParent = User.IsInRole(AppRoles.Parent);
        var all = await _messages.GetForUserAsync(me.Value);

        // Only conversations where I am the parent (as a parent) or the
        // teacher (as a teacher).
        var mine = all.Where(message => isParent
            ? message.ParentUserId == me.Value
            : message.TeacherUserId == me.Value);

        var userNames = new Dictionary<Guid, string>();
        var students = new Dictionary<Guid, Student?>();
        var list = new List<ConversationListItemViewModel>();

        foreach (var conversation in mine.GroupBy(message => new
                 {
                     message.StudentId,
                     message.ParentUserId,
                     message.TeacherUserId
                 }))
        {
            var otherId = isParent
                ? conversation.Key.TeacherUserId
                : conversation.Key.ParentUserId;

            if (!userNames.TryGetValue(otherId, out var otherName))
            {
                otherName = (await _users.GetByIdAsync(otherId))?.UserName
                    ?? "(removed account)";
                userNames[otherId] = otherName;
            }

            if (!students.TryGetValue(conversation.Key.StudentId, out var student))
            {
                student = await _students.GetByIdAsync(conversation.Key.StudentId);
                students[conversation.Key.StudentId] = student;
            }

            list.Add(new ConversationListItemViewModel
            {
                StudentId = conversation.Key.StudentId,
                Student = student,
                OtherUserId = otherId,
                OtherUserName = otherName,
                LastMessage = conversation
                    .OrderByDescending(message => message.SentAtUtc)
                    .First(),
                UnreadCount = conversation.Count(message =>
                    message.SenderUserId != me.Value &&
                    message.ReadAtUtc is null)
            });
        }

        return View(list
            .OrderByDescending(item => item.LastMessage.SentAtUtc)
            .ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Conversation(Guid studentId, Guid otherUserId)
    {
        var conversation = await ResolveAsync(studentId, otherUserId);

        if (conversation is null)
        {
            return NotFound("Conversation not found.");
        }

        await _messages.MarkReadAsync(
            studentId,
            conversation.ParentId,
            conversation.TeacherId,
            conversation.MyId);

        return View(await BuildViewModelAsync(conversation, new SendMessageRequest()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        Guid studentId,
        Guid otherUserId,
        [Bind(Prefix = "Input")] SendMessageRequest request)
    {
        var conversation = await ResolveAsync(studentId, otherUserId);

        if (conversation is null)
        {
            return NotFound("Conversation not found.");
        }

        if (!conversation.CanSend)
        {
            ModelState.AddModelError(
                string.Empty,
                "You can no longer send messages here: the child or the " +
                "class is not linked to you any more.");
        }

        if (!ModelState.IsValid)
        {
            return View(
                nameof(Conversation),
                await BuildViewModelAsync(conversation, request));
        }

        await _messages.SendAsync(
            studentId,
            conversation.ParentId,
            conversation.TeacherId,
            conversation.MyId,
            request.Body);

        return RedirectToAction(
            nameof(Conversation),
            new { studentId, otherUserId });
    }

    private sealed record ResolvedConversation(
        Student Student,
        Guid MyId,
        Guid ParentId,
        Guid TeacherId,
        AppUser Other,
        bool CanSend);

    // Works out who the parent and teacher are and whether the signed-in
    // user may read (null if not) and send in this conversation.
    private async Task<ResolvedConversation?> ResolveAsync(
        Guid studentId,
        Guid otherUserId)
    {
        var me = User.GetUserId();
        var student = await _students.GetByIdAsync(studentId);
        var other = await _users.GetByIdAsync(otherUserId);

        if (me is null || student is null || other is null)
        {
            return null;
        }

        var isParent = User.IsInRole(AppRoles.Parent);
        var parentId = isParent ? me.Value : other.Id;
        var teacherId = isParent ? other.Id : me.Value;

        // The other side must have the opposite role.
        if (other.Role != (isParent ? AppRoles.Teacher : AppRoles.Parent))
        {
            return null;
        }

        var parentLinked = await _parents.IsLinkedAsync(parentId, studentId);

        var teacherHasClass = (await _users.GetTeacherClassesAsync(teacherId))
            .Contains(SchoolGroups.GetName(student));

        var canSend = parentLinked && teacherHasClass;

        if (!canSend)
        {
            // Still readable if these two have talked about this child before
            // and the signed-in user is still allowed to see the child.
            var meStillLinked = isParent ? parentLinked : teacherHasClass;

            var history = await _messages.GetConversationAsync(
                studentId, parentId, teacherId);

            if (!meStillLinked || history.Count == 0)
            {
                return null;
            }
        }

        return new ResolvedConversation(
            student, me.Value, parentId, teacherId, other, canSend);
    }

    private async Task<ConversationViewModel> BuildViewModelAsync(
        ResolvedConversation conversation,
        SendMessageRequest input)
    {
        ViewData["CanSend"] = conversation.CanSend;

        return new ConversationViewModel
        {
            Student = conversation.Student,
            OtherUserId = conversation.Other.Id,
            OtherUserName = conversation.Other.UserName,
            OtherRole = conversation.Other.Role == AppRoles.Teacher
                ? "teacher"
                : "parent",
            MyUserId = conversation.MyId,
            Messages = await _messages.GetConversationAsync(
                conversation.Student.Id,
                conversation.ParentId,
                conversation.TeacherId),
            Input = input
        };
    }
}
