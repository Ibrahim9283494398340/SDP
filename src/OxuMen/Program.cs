using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// SQLite database in App_Data, next to the old JSON files.
var dataDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data");

Directory.CreateDirectory(dataDirectory);

var connection = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("OxuMen")
    ?? "Data Source=App_Data/oxumen.db");

// Resolve a relative path against the project folder, not the
// folder the app happens to be started from.
if (!Path.IsPathRooted(connection.DataSource))
{
    connection.DataSource = Path.Combine(
        builder.Environment.ContentRootPath,
        connection.DataSource);
}

builder.Services.AddDbContext<OxuMenDbContext>(options =>
    options.UseSqlite(connection.ToString()));

builder.Services.AddScoped<IStudentRepository, EfStudentRepository>();
builder.Services.AddScoped<IReadingAssessmentRepository, EfReadingAssessmentRepository>();
builder.Services.AddScoped<IGroupTaskRepository, EfGroupTaskRepository>();

var app = builder.Build();

// Create the database on first run and bring in any old JSON data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OxuMenDbContext>();

    await db.Database.EnsureCreatedAsync();

    await JsonDataImporter.ImportAsync(
        db,
        dataDirectory,
        app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=StudentPages}/{action=Index}/{id?}");

app.Run();
