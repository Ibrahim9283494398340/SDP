using System.Text.Json;
using OxuMen.Models;

namespace OxuMen.Repositories;

public class JsonGroupTaskRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public JsonGroupTaskRepository(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");

        Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "group-tasks.json");
    }

    public async Task<List<GroupTask>> GetAllAsync()
    {
        await _fileLock.WaitAsync();

        try
        {
            return (await ReadAsync())
                .OrderByDescending(task => task.CreatedAtUtc)
                .ToList();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<GroupTask?> GetByIdAsync(Guid id)
    {
        var tasks = await GetAllAsync();

        return tasks.FirstOrDefault(task => task.Id == id);
    }

    public async Task<GroupTask> CreateAsync(GroupTask task)
    {
        await _fileLock.WaitAsync();

        try
        {
            var tasks = await ReadAsync();

            task.Id = Guid.NewGuid();
            task.CreatedAtUtc = DateTimeOffset.UtcNow;

            tasks.Add(task);

            await SaveAsync(tasks);

            return task;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<GroupTask>> ReadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<GroupTask>();
        }

        await using var stream = File.OpenRead(_filePath);

        return await JsonSerializer.DeserializeAsync<List<GroupTask>>(
            stream,
            _options)
            ?? throw new InvalidDataException(
                "group-tasks.json must contain a JSON array.");
    }

    private async Task SaveAsync(List<GroupTask> tasks)
    {
        var temporaryPath = _filePath + "." + Guid.NewGuid() + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, tasks, _options);
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