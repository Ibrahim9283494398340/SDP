using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class EfMessageRepository : IMessageRepository
{
    private readonly OxuMenDbContext _db;

    public EfMessageRepository(OxuMenDbContext db)
    {
        _db = db;
    }

    public async Task<List<Message>> GetConversationAsync(
        Guid studentId,
        Guid parentUserId,
        Guid teacherUserId)
    {
        return await _db.Messages
            .AsNoTracking()
            .Where(message =>
                message.StudentId == studentId &&
                message.ParentUserId == parentUserId &&
                message.TeacherUserId == teacherUserId)
            .OrderBy(message => message.SentAtUtc)
            .ToListAsync();
    }

    public async Task<Message> SendAsync(
        Guid studentId,
        Guid parentUserId,
        Guid teacherUserId,
        Guid senderUserId,
        string body)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ParentUserId = parentUserId,
            TeacherUserId = teacherUserId,
            SenderUserId = senderUserId,
            Body = body.Trim(),
            SentAtUtc = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        return message;
    }

    public async Task MarkReadAsync(
        Guid studentId,
        Guid parentUserId,
        Guid teacherUserId,
        Guid readerUserId)
    {
        var unread = await _db.Messages
            .Where(message =>
                message.StudentId == studentId &&
                message.ParentUserId == parentUserId &&
                message.TeacherUserId == teacherUserId &&
                message.SenderUserId != readerUserId &&
                message.ReadAtUtc == null)
            .ToListAsync();

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        unread.ForEach(message => message.ReadAtUtc = now);

        await _db.SaveChangesAsync();
    }

    public async Task<List<Message>> GetForUserAsync(Guid userId)
    {
        return await _db.Messages
            .AsNoTracking()
            .Where(message =>
                message.ParentUserId == userId ||
                message.TeacherUserId == userId)
            .OrderByDescending(message => message.SentAtUtc)
            .ToListAsync();
    }

    public async Task<int> CountUnreadAsync(Guid userId)
    {
        return await _db.Messages.CountAsync(message =>
            (message.ParentUserId == userId || message.TeacherUserId == userId) &&
            message.SenderUserId != userId &&
            message.ReadAtUtc == null);
    }

    public async Task DeleteForStudentAsync(Guid studentId)
    {
        var messages = await _db.Messages
            .Where(message => message.StudentId == studentId)
            .ToListAsync();

        _db.Messages.RemoveRange(messages);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteForUserAsync(Guid userId)
    {
        var messages = await _db.Messages
            .Where(message =>
                message.ParentUserId == userId ||
                message.TeacherUserId == userId)
            .ToListAsync();

        _db.Messages.RemoveRange(messages);
        await _db.SaveChangesAsync();
    }
}
