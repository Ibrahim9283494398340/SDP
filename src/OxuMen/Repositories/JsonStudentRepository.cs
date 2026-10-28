using System.Text.Json;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class JsonStudentRepository : IStudentRepository
{
    private readonly string _filePath;

    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public JsonStudentRepository(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(
            environment.ContentRootPath,
            "App_Data");

        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "students.json");
    }

    public async Task<List<Student>> GetAllAsync()
    {
        await _fileLock.WaitAsync();

        try
        {
            return await ReadStudentsAsync();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        await _fileLock.WaitAsync();

        try
        {
            var students = await ReadStudentsAsync();

            return students.FirstOrDefault(student => student.Id == id);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Student> CreateAsync(Student student)
    {
        await _fileLock.WaitAsync();

        try
        {
            var students = await ReadStudentsAsync();

            student.Id = Guid.NewGuid();
            student.CreatedAtUtc = DateTime.UtcNow;

            students.Add(student);

            await SaveStudentsAsync(students);

            return student;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Student?> UpdateAsync(Student student)
    {
        await _fileLock.WaitAsync();

        try
        {
            var students = await ReadStudentsAsync();

            var existingStudent = students.FirstOrDefault(
                existing => existing.Id == student.Id);

            if (existingStudent is null)
            {
                return null;
            }

            existingStudent.FirstName = student.FirstName;
            existingStudent.LastName = student.LastName;
            existingStudent.GradeLevel = student.GradeLevel;
            existingStudent.ClassGroup = student.ClassGroup;

            // Keep the original ID and creation date.
            await SaveStudentsAsync(students);

            return existingStudent;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _fileLock.WaitAsync();

        try
        {
            var students = await ReadStudentsAsync();

            var student = students.FirstOrDefault(
                existing => existing.Id == id);

            if (student is null)
            {
                return false;
            }

            students.Remove(student);

            await SaveStudentsAsync(students);

            return true;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<Student>> ReadStudentsAsync()
    {
        if (!File.Exists(_filePath))
        {
            var students = new List<Student>();

            await SaveStudentsAsync(students);

            return students;
        }

        await using var stream = File.OpenRead(_filePath);

        var savedStudents =
            await JsonSerializer.DeserializeAsync<List<Student>>(
                stream,
                _jsonOptions);

        return savedStudents
            ?? throw new InvalidDataException(
                "students.json must contain a JSON array.");
    }

    private async Task SaveStudentsAsync(List<Student> students)
    {
        var temporaryPath = _filePath + "." + Guid.NewGuid() + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    students,
                    _jsonOptions);

                await stream.FlushAsync();
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}