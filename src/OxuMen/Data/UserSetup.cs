using Microsoft.EntityFrameworkCore;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;

namespace OxuMen.Data;

public static class UserSetup
{
    // EnsureCreated does nothing on a database that already exists, so a
    // database made before logins were added gets the Users table here.
    // The columns match what EF Core creates for AppUser on a new database.
    public static async Task EnsureUsersTableAsync(OxuMenDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                "UserName" TEXT NOT NULL,
                "NormalizedUserName" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "Role" TEXT NOT NULL,
                "StudentId" TEXT NULL,
                "SecurityStamp" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                CONSTRAINT "FK_Users_Students_StudentId" FOREIGN KEY ("StudentId")
                    REFERENCES "Students" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedUserName"
                ON "Users" ("NormalizedUserName");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_StudentId"
                ON "Users" ("StudentId");
            """);
    }

    // Same idea for the TeacherClasses table.
    public static async Task EnsureTeacherClassesTableAsync(OxuMenDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "TeacherClasses" (
                "UserId" TEXT NOT NULL,
                "ClassGroup" TEXT NOT NULL,
                CONSTRAINT "PK_TeacherClasses" PRIMARY KEY ("UserId", "ClassGroup"),
                CONSTRAINT "FK_TeacherClasses_Users_UserId" FOREIGN KEY ("UserId")
                    REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_TeacherClasses_ClassGroup"
                ON "TeacherClasses" ("ClassGroup");
            """);
    }

    // 1. Creates an admin account when there is none, so someone can sign
    //    in and set up teachers. Password from Seed:AdminPassword, or a
    //    random one printed once in the log.
    // 2. Only when Seed:TeacherPassword is set (development): creates the
    //    example teacher, and gives it Seed:TeacherClasses if it has no
    //    classes yet.
    public static async Task SeedAccountsAsync(
        IUserRepository users,
        IConfiguration configuration,
        ILogger logger)
    {
        await SeedAdminAsync(users, configuration, logger);
        await SeedExampleTeacherAsync(users, configuration, logger);
    }

    private static async Task SeedAdminAsync(
        IUserRepository users,
        IConfiguration configuration,
        ILogger logger)
    {
        if (await users.AnyWithRoleAsync(AppRoles.Admin))
        {
            return;
        }

        var userName = configuration["Seed:AdminUserName"];

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "admin";
        }

        if (await users.UserNameExistsAsync(userName))
        {
            logger.LogWarning(
                "No admin account exists, and the user name {UserName} is " +
                "already used by another account. Set Seed:AdminUserName " +
                "to a free user name to create the admin.",
                userName);

            return;
        }

        var password = configuration["Seed:AdminPassword"];
        var generated = string.IsNullOrWhiteSpace(password);

        if (generated)
        {
            password = PasswordHashing.GeneratePassword();
        }

        await users.CreateAsync(
            new AppUser
            {
                UserName = userName,
                Role = AppRoles.Admin
            },
            password!);

        if (generated)
        {
            logger.LogWarning(
                "Created the admin account. User name: {UserName}. " +
                "Temporary password: {Password}. Sign in and change it " +
                "from the account menu.",
                userName,
                password);
        }
        else
        {
            logger.LogInformation(
                "Created the admin account {UserName} from configuration.",
                userName);
        }
    }

    private static async Task SeedExampleTeacherAsync(
        IUserRepository users,
        IConfiguration configuration,
        ILogger logger)
    {
        var password = configuration["Seed:TeacherPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userName = configuration["Seed:TeacherUserName"];

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "teacher";
        }

        var teacher = await users.GetByUserNameAsync(userName);

        if (teacher is null)
        {
            teacher = await users.CreateAsync(
                new AppUser
                {
                    UserName = userName,
                    Role = AppRoles.Teacher
                },
                password);

            logger.LogInformation(
                "Created the example teacher account {UserName}.",
                userName);
        }

        if (teacher.Role != AppRoles.Teacher)
        {
            return;
        }

        if ((await users.GetTeacherClassesAsync(teacher.Id)).Count > 0)
        {
            return;
        }

        var classes = (configuration["Seed:TeacherClasses"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
            .ToList();

        if (classes.Count > 0)
        {
            await users.SetTeacherClassesAsync(teacher.Id, classes);

            logger.LogInformation(
                "Gave the example teacher {UserName} the classes {Classes}.",
                userName,
                string.Join(", ", classes));
        }
    }
}
