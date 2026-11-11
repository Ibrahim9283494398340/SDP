using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OxuMen.Data;
using OxuMen.Repositories;
using OxuMen.Security;

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
builder.Services.AddScoped<IUserRepository, EfUserRepository>();

// Which classes the signed-in teacher may see.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClassAccess, ClassAccess>();

// Sign-in with a cookie. API calls get 401/403 instead of a redirect
// to the login page.
static bool IsApiRequest(HttpRequest request) =>
    request.Path.StartsWithSegments("/api");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "OxuMen.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal = AppClaims.ValidatePrincipalAsync;

        options.Events.OnRedirectToLogin = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Every page needs a signed-in user unless it is marked
    // [AllowAnonymous]. Role checks are added per controller.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Slows down password guessing: 10 login attempts per minute per address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Create the database on first run and bring in any old JSON data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OxuMenDbContext>();

    await db.Database.EnsureCreatedAsync();

    // Adds the Users table to a database created before logins existed.
    await UserSetup.EnsureUsersTableAsync(db);
    await UserSetup.EnsureTeacherClassesTableAsync(db);

    await JsonDataImporter.ImportAsync(
        db,
        dataDirectory,
        app.Logger);

    await UserSetup.SeedAccountsAsync(
        scope.ServiceProvider.GetRequiredService<IUserRepository>(),
        app.Configuration,
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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// "/" sends admins to the teacher list, teachers to their classes and
// students to their own page.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Start}/{id?}");

app.Run();
