using System.Text.Json;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class JsonReadingAssessmentRepository : IReadingAssessmentRepository
{
    private readonly string _filePath;

    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        IgnoreReadOnlyProperties = true
    };

    public JsonReadingAssessmentRepository(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(
            environment.ContentRootPath,
            "App_Data");

        Directory.CreateDirectory(dataDirectory);

        _filePath = Path.Combine(dataDirectory, "assessments.json");
    }

    public async Task<ReadingAssessment?> GetByIdAsync(Guid id)
    {
        await _fileLock.WaitAsync();

        try
        {
            var assessments = await ReadAssessmentsAsync();

            return assessments.FirstOrDefault(
                assessment => assessment.Id == id);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<ReadingAssessment>> GetForStudentAsync(
        Guid studentId)
    {
        await _fileLock.WaitAsync();

        try
        {
            var assessments = await ReadAssessmentsAsync();

            return assessments
                .Where(assessment => assessment.StudentId == studentId)
                .OrderByDescending(assessment => assessment.AssessedAtUtc)
                .ThenByDescending(assessment => assessment.CreatedAtUtc)
                .ToList();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<ReadingAssessment> CreateAsync(
        ReadingAssessment assessment)
    {
        await _fileLock.WaitAsync();

        try
        {
            var assessments = await ReadAssessmentsAsync();

            if (assessment.GroupTaskId.HasValue &&
                assessments.Any(existing =>
                    existing.GroupTaskId == assessment.GroupTaskId &&
                    existing.StudentId == assessment.StudentId))
            {
                throw new InvalidOperationException(
                    "Results have already been recorded for this student and task.");
            }

            assessment.Id = Guid.NewGuid();
            assessment.CreatedAtUtc = DateTimeOffset.UtcNow;

            assessments.Add(assessment);

            await SaveAssessmentsAsync(assessments);

            return assessment;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<ReadingAssessment>> ReadAssessmentsAsync()
    {
        if (!File.Exists(_filePath))
        {
            var assessments = new List<ReadingAssessment>();

            await SaveAssessmentsAsync(assessments);

            return assessments;
        }

        await using var stream = File.OpenRead(_filePath);

        var savedAssessments =
            await JsonSerializer.DeserializeAsync<List<ReadingAssessment>>(
                stream,
                _jsonOptions);

        return savedAssessments
            ?? throw new InvalidDataException(
                "assessments.json must contain a JSON array.");
    }

    private async Task SaveAssessmentsAsync(
        List<ReadingAssessment> assessments)
    {
        var temporaryPath = _filePath + "." + Guid.NewGuid() + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    assessments,
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