using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class EfStudentRepository : IStudentRepository
{
    private readonly OxuMenDbContext _db;

    public EfStudentRepository(OxuMenDbContext db)
    {
        _db = db;
    }

    public async Task<List<Student>> GetAllAsync()
    {
        return await _db.Students
            .AsNoTracking()
            .OrderBy(student => student.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        return await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(student => student.Id == id);
    }

    public async Task<Student> CreateAsync(Student student)
    {
        student.Id = Guid.NewGuid();
        student.CreatedAtUtc = DateTime.UtcNow;

        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return student;
    }

    public async Task<Student?> UpdateAsync(Student student)
    {
        var existingStudent = await _db.Students.FindAsync(student.Id);

        if (existingStudent is null)
        {
            return null;
        }

        existingStudent.FirstName = student.FirstName;
        existingStudent.LastName = student.LastName;
        existingStudent.GradeLevel = student.GradeLevel;
        existingStudent.ClassGroup = student.ClassGroup;

        // Keep the original ID and creation date.
        await _db.SaveChangesAsync();

        return existingStudent;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var student = await _db.Students.FindAsync(id);

        if (student is null)
        {
            return false;
        }

        _db.Students.Remove(student);
        await _db.SaveChangesAsync();

        return true;
    }
}
