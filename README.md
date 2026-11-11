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

## Sign-in and roles

Every page needs a signed-in user. There are three roles:

| Role    | Can see and do |
|---------|----------------|
| Admin   | Manages teachers on the **Teachers** page: creates them, chooses which classes each one has, removes them. The admin cannot change a teacher's password; teachers change their own under **Change Password**. Can also see and work in every class. |
| Teacher | Only **their own classes** (chosen by the admin): those groups, their students, tasks and results, and the same through `/api`. Other classes do not appear in lists or drop-downs, and opening one directly shows *Access Denied* or *Not Found*. A teacher with no classes sees nothing. Can reset the password of a student in their classes. |
| Student | Only **My Page** (`/My`): their own profile, the tasks assigned to them and their own results. Teacher notes are not shown. |

Accounts live in the `Users` table (passwords are PBKDF2-hashed) and
teachers' classes in `TeacherClasses`. On a database created before these
existed, the tables are added on startup; existing data is kept.

### Example accounts

On startup:

- If there is no admin, one is created from `Seed:AdminUserName` /
  `Seed:AdminPassword`. If the password is empty, a random one is printed
  **once** in the console log.
- If `Seed:TeacherPassword` is set, an example teacher is created from
  `Seed:TeacherUserName`, and if it has no classes it gets
  `Seed:TeacherClasses`.

`appsettings.Development.json` sets these for local development:

| User name | Password     | Role                    |
|-----------|--------------|-------------------------|
| `admin`   | `admin123`   | Admin                   |
| `teacher` | `teacher123` | Teacher of classes 1A and 1B |

These passwords are in the repository, so change them before the site is
used for real. The development settings are not used in production, where
the admin password should be set without a file, for example:

```
dotnet user-secrets init
dotnet user-secrets set "Seed:AdminPassword" "a-strong-password"
```

or with the environment variable `Seed__AdminPassword`.

### Student accounts

Students create their own account: on the sign-in page they choose
**Create your account** and enter their name, class, a user name and a
password. This makes a new student profile in that class and signs them in.
Only student accounts can be created this way; teacher accounts are added
by a teacher under **Add Teacher**.

If a student forgets their password, a teacher opens the student and
chooses **Reset Password**. Resetting a password, or a student changing
their own, signs that account out everywhere else. Deleting a student also
deletes their account.

## Run

```
cd src/OxuMen
dotnet restore
dotnet run
```
