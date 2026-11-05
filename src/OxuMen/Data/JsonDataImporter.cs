using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OxuMen.Models;

namespace OxuMen.Data;

// Moves the old App_Data/*.json files into the SQLite database once.
// After a file is imported it is renamed to *.json.imported so it is
// never imported twice.
public static class JsonDataImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task ImportAsync(
        OxuMenDbContext db,
        string dataDirectory,
        ILogger logger)
    {
        var studentsFile = Path.Combine(dataDirectory, "students.json");
        var tasksFile = Path.Combine(dataDirectory, "group-tasks.json");
        var assessmentsFile = Path.Combine(dataDirectory, "assessments.json");

        if (!File.Exists(studentsFile) &&
            !File.Exists(tasksFile) &&
            !File.Exists(assessmentsFile))
        {
            return;
        }

        var students = await ReadAsync<Student>(studentsFile);
        var tasks = await ReadAsync<GroupTask>(tasksFile);
        var assessments = await ReadAsync<ReadingAssessment>(assessmentsFile);

        await using var transaction = await db.Database.BeginTransactionAsync();

        // Students
        var existingStudentIds = (await db.Students
            .Select(student => student.Id)
            .ToListAsync())
            .ToHashSet();

        var newStudents = students
            .Where(student => existingStudentIds.Add(student.Id))
            .ToList();

        db.Students.AddRange(newStudents);

        // Group tasks and their student lists
        var existingTaskIds = (await db.GroupTasks
            .Select(task => task.Id)
            .ToListAsync())
            .ToHashSet();

        var newTasks = tasks
            .Where(task => existingTaskIds.Add(task.Id))
            .ToList();

        foreach (var task in newTasks)
        {
            task.Assignments = task.StudentIds
                .Distinct()
                .Select(studentId => new GroupTaskStudent
                {
                    GroupTaskId = task.Id,
                    StudentId = studentId
                })
                .ToList();
        }

        db.GroupTasks.AddRange(newTasks);

        // Assessments
        var existingAssessmentIds = (await db.ReadingAssessments
            .Select(assessment => assessment.Id)
            .ToListAsync())
            .ToHashSet();

        var taskStudentPairs = new HashSet<(Guid, Guid)>();
        var skipped = 0;
        var newAssessments = new List<ReadingAssessment>();

        foreach (var assessment in assessments)
        {
            if (!existingAssessmentIds.Add(assessment.Id) ||
                !existingStudentIds.Contains(assessment.StudentId))
            {
                skipped++;
                continue;
            }

            if (assessment.GroupTaskId.HasValue)
            {
                if (!existingTaskIds.Contains(assessment.GroupTaskId.Value))
                {
                    assessment.GroupTaskId = null;
                }
                else if (!taskStudentPairs.Add(
                    (assessment.GroupTaskId.Value, assessment.StudentId)))
                {
                    skipped++;
                    continue;
                }
            }

            newAssessments.Add(assessment);
        }

        db.ReadingAssessments.AddRange(newAssessments);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        MarkImported(studentsFile);
        MarkImported(tasksFile);
        MarkImported(assessmentsFile);

        logger.LogInformation(
            "Imported {Students} students, {Tasks} group tasks and " +
            "{Assessments} assessments from JSON ({Skipped} assessments skipped).",
            newStudents.Count,
            newTasks.Count,
            newAssessments.Count,
            skipped);
    }

    private static async Task<List<T>> ReadAsync<T>(string path)
    {
        if (!File.Exists(path))
        {
            return new List<T>();
        }

        await using var stream = File.OpenRead(path);

        return await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions)
            ?? new List<T>();
    }

    private static void MarkImported(string path)
    {
        if (File.Exists(path))
        {
            File.Move(path, path + ".imported", overwrite: true);
        }
    }
}
