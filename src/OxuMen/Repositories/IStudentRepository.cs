using OxuMen.Models;

namespace OxuMen.Repositories;

public interface IStudentRepository
{
    Task<List<Student>> GetAllAsync();

    Task<Student?> GetByIdAsync(Guid id);

    Task<Student> CreateAsync(Student student);

    Task<Student?> UpdateAsync(Student student);

    Task<bool> DeleteAsync(Guid id);
}