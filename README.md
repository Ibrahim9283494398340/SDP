# SDP

ASP.NET Core MVC app (OxuMen) for student reading assessments.

## Database

Data is stored in SQLite through Entity Framework Core.

- Database file: `src/OxuMen/App_Data/oxumen.db` (created automatically on first run)
- Connection string: `ConnectionStrings:OxuMen` in `appsettings.json`
- Tables: `Students`, `GroupTasks`, `GroupTaskStudents`, `ReadingAssessments`

### Moving old JSON data

If `App_Data` still contains `students.json`, `group-tasks.json` or
`assessments.json` from the earlier version, they are imported into the
database on startup and renamed to `*.json.imported`.

## Run

```
cd src/OxuMen
dotnet restore
dotnet run
```
