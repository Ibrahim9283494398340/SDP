using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IMessageRepository
{
    // Oldest first.
    Task<List<Message>> GetConversationAsync(
        Guid studentId,
        Guid parentUserId,
        Guid teacherUserId);

    Task<Message> SendAsync(
        Guid studentId,
        Guid parentUserId,
        Guid teacherUserId,
        Guid senderUserId,
        string body);

    // Marks the other person's messages in this conversation as read.
    Task MarkReadAsync(
        Guid studentId,
        Guid parentUserId,
        Guid teacherUserId,
        Guid readerUserId);

    // Every message the user sent or received, newest first.
    Task<List<Message>> GetForUserAsync(Guid userId);

    Task<int> CountUnreadAsync(Guid userId);

    Task DeleteForStudentAsync(Guid studentId);

    Task DeleteForUserAsync(Guid userId);
}
