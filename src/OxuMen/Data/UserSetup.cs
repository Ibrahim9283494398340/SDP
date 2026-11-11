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

    // Tables for parents: parent-child links, parent codes and messages.
    public static async Task EnsureParentTablesAsync(OxuMenDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ParentChildren" (
                "ParentUserId" TEXT NOT NULL,
                "StudentId" TEXT NOT NULL,
                CONSTRAINT "PK_ParentChildren" PRIMARY KEY ("ParentUserId", "StudentId"),
                CONSTRAINT "FK_ParentChildren_Users_ParentUserId" FOREIGN KEY ("ParentUserId")
                    REFERENCES "Users" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ParentChildren_Students_StudentId" FOREIGN KEY ("StudentId")
                    REFERENCES "Students" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ParentChildren_StudentId"
                ON "ParentChildren" ("StudentId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ParentCodes" (
                "StudentId" TEXT NOT NULL CONSTRAINT "PK_ParentCodes" PRIMARY KEY,
                "Code" TEXT NOT NULL,
                CONSTRAINT "FK_ParentCodes_Students_StudentId" FOREIGN KEY ("StudentId")
                    REFERENCES "Students" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ParentCodes_Code"
                ON "ParentCodes" ("Code");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Messages" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Messages" PRIMARY KEY,
                "StudentId" TEXT NOT NULL,
                "ParentUserId" TEXT NOT NULL,
                "TeacherUserId" TEXT NOT NULL,
                "SenderUserId" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "SentAtUtc" TEXT NOT NULL,
                "ReadAtUtc" TEXT NULL,
                CONSTRAINT "FK_Messages_Students_StudentId" FOREIGN KEY ("StudentId")
                    REFERENCES "Students" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_Messages_Users_ParentUserId" FOREIGN KEY ("ParentUserId")
                    REFERENCES "Users" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_Messages_Users_TeacherUserId" FOREIGN KEY ("TeacherUserId")
                    REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Messages_StudentId_ParentUserId_TeacherUserId"
                ON "Messages" ("StudentId", "ParentUserId", "TeacherUserId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Messages_TeacherUserId"
                ON "Messages" ("TeacherUserId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Messages_ParentUserId"
                ON "Messages" ("ParentUserId");
            """);
    }

    // 1. Creates an admin account when there is none, so someone can sign
    //    in and set up teachers. Password from Seed:AdminPassword, or a
    //    random one printed once in the log.
    // 2. Only when Seed:TeacherPassword is set (development): creates the
    //    example teacher, and gives it Seed:TeacherClasses if it has no
    //    classes yet.
    // 3. Only when Seed:StudentPassword is set (development): creates an
    //    example student with one sample result and teacher note, and if
    //    Seed:ParentPassword is set, an example parent linked to them.
    public static async Task SeedAccountsAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var users = services.GetRequiredService<IUserRepository>();

        await SeedAdminAsync(users, configuration, logger);
        await SeedExampleTeacherAsync(users, configuration, logger);
        await SeedExampleStudentAndParentAsync(services, configuration, logger);
    }

    private static async Task SeedExampleStudentAndParentAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var studentPassword = configuration["Seed:StudentPassword"];

        if (string.IsNullOrWhiteSpace(studentPassword))
        {
            return;
        }

        var users = services.GetRequiredService<IUserRepository>();

        var studentUserName = configuration["Seed:StudentUserName"];

        if (string.IsNullOrWhiteSpace(studentUserName))
        {
            studentUserName = "student";
        }

        var classGroup = (configuration["Seed:StudentClass"] ?? "1A")
            .Trim()
            .ToUpperInvariant();

        if (!SchoolGroups.All.Contains(classGroup))
        {
            classGroup = "1A";
        }

        Guid? studentId;
        var studentLogin = await users.GetByUserNameAsync(studentUserName);

        if (studentLogin is null)
        {
            studentId = await CreateExampleStudentAsync(
                services, users, studentUserName, studentPassword, classGroup);

            logger.LogInformation(
                "Created the example student {UserName} in {Class}.",
                studentUserName,
                classGroup);
        }
        else
        {
            studentId = studentLogin.StudentId;
        }

        var parentPassword = configuration["Seed:ParentPassword"];

        if (string.IsNullOrWhiteSpace(parentPassword) || studentId is null)
        {
            return;
        }

        var parentUserName = configuration["Seed:ParentUserName"];

        if (string.IsNullOrWhiteSpace(parentUserName))
        {
            parentUserName = "parent";
        }

        var parent = await users.GetByUserNameAsync(parentUserName);

        if (parent is null)
        {
            parent = await users.CreateAsync(
                new AppUser
                {
                    UserName = parentUserName,
                    Role = AppRoles.Parent
                },
                parentPassword);

            await services.GetRequiredService<IParentRepository>()
                .LinkAsync(parent.Id, studentId.Value);

            logger.LogInformation(
                "Created the example parent {UserName}, linked to {Student}.",
                parentUserName,
                studentUserName);
        }
    }

    // The example student's profile, login, one task and one result with a
    // teacher note, so the parent pages have something to show.
    private static async Task<Guid> CreateExampleStudentAsync(
        IServiceProvider services,
        IUserRepository users,
        string userName,
        string password,
        string classGroup)
    {
        var student = await services.GetRequiredService<IStudentRepository>()
            .CreateAsync(new Student
            {
                FirstName = "Example",
                LastName = "Student",
                GradeLevel = classGroup[0] - '0',
                ClassGroup = classGroup
            });

        await users.CreateAsync(
            new AppUser
            {
                UserName = userName,
                Role = AppRoles.Student,
                StudentId = student.Id
            },
            password);

        var task = await services.GetRequiredService<IGroupTaskRepository>()
            .CreateAsync(new GroupTask
            {
                ClassGroup = classGroup,
                Kind = AssessmentKind.Initial,
                TextReference = "example-text-001",
                TextVersion = "1",
                StudentIds = new List<Guid> { student.Id }
            });

        await services.GetRequiredService<IReadingAssessmentRepository>()
            .CreateAsync(new ReadingAssessment
            {
                StudentId = student.Id,
                GroupTaskId = task.Id,
                Kind = task.Kind,
                TextReference = task.TextReference,
                TextVersion = task.TextVersion,
                AssessedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                TotalWordsRead = 62,
                CorrectWordsRead = 57,
                DurationSeconds = 60,
                ComprehensionCorrectAnswers = 4,
                ComprehensionTotalQuestions = 5,
                Notes = "Reads with good expression. Some longer words are " +
                        "still hard; reading short texts aloud at home will help."
            });

        return student.Id;
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
