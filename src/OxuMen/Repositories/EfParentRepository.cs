using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class EfParentRepository : IParentRepository
{
    // No look-alike characters (0/O, 1/I), so codes are easy to read out.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    private readonly OxuMenDbContext _db;

    public EfParentRepository(OxuMenDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetOrCreateCodeAsync(Guid studentId)
    {
        var existing = await _db.ParentCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(code => code.StudentId == studentId);

        if (existing is not null)
        {
            return existing.Code;
        }

        // A clash between two random codes is very unlikely; retry if it happens.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = new ParentCode
            {
                StudentId = studentId,
                Code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength)
            };

            _db.ParentCodes.Add(code);

            try
            {
                await _db.SaveChangesAsync();
                return code.Code;
            }
            catch (DbUpdateException)
            {
                _db.Entry(code).State = EntityState.Detached;

                // Another request may have made this student's code meanwhile.
                var made = await _db.ParentCodes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.StudentId == studentId);

                if (made is not null)
                {
                    return made.Code;
                }
            }
        }

        throw new InvalidOperationException("Could not create a parent code.");
    }

    public async Task<Guid?> FindStudentByCodeAsync(string code)
    {
        var normalized = ParentCode.Normalize(code);

        if (normalized.Length == 0)
        {
            return null;
        }

        var match = await _db.ParentCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Code == normalized);

        return match?.StudentId;
    }

    public async Task LinkAsync(Guid parentUserId, Guid studentId)
    {
        if (await IsLinkedAsync(parentUserId, studentId))
        {
            return;
        }

        _db.ParentChildren.Add(new ParentChild
        {
            ParentUserId = parentUserId,
            StudentId = studentId
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Linked by a second request at the same moment: already done.
            _db.ChangeTracker.Clear();
        }
    }

    public async Task<bool> IsLinkedAsync(Guid parentUserId, Guid studentId)
    {
        return await _db.ParentChildren.AnyAsync(link =>
            link.ParentUserId == parentUserId &&
            link.StudentId == studentId);
    }

    public async Task<List<Guid>> GetChildIdsAsync(Guid parentUserId)
    {
        return await _db.ParentChildren
            .AsNoTracking()
            .Where(link => link.ParentUserId == parentUserId)
            .Select(link => link.StudentId)
            .ToListAsync();
    }

    public async Task<List<Guid>> GetParentIdsAsync(Guid studentId)
    {
        return await _db.ParentChildren
            .AsNoTracking()
            .Where(link => link.StudentId == studentId)
            .Select(link => link.ParentUserId)
            .ToListAsync();
    }

    public async Task DeleteForStudentAsync(Guid studentId)
    {
        var links = await _db.ParentChildren
            .Where(link => link.StudentId == studentId)
            .ToListAsync();

        var codes = await _db.ParentCodes
            .Where(code => code.StudentId == studentId)
            .ToListAsync();

        _db.ParentChildren.RemoveRange(links);
        _db.ParentCodes.RemoveRange(codes);

        await _db.SaveChangesAsync();
    }
}
