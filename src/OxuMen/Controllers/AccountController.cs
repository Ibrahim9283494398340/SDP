using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OxuMen.Models;
using OxuMen.Repositories;
using OxuMen.Security;

namespace OxuMen.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;
    private readonly IParentRepository _parents;

    public AccountController(
        IUserRepository users,
        IStudentRepository students,
        IParentRepository parents)
    {
        _users = users;
        _students = students;
        _parents = parents;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Start", "Home");
        }

        return View(new LoginRequest { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            request.Password = string.Empty;
            return View(request);
        }

        var user = await _users.GetByUserNameAsync(request.UserName);

        bool passwordIsCorrect;

        if (user is null)
        {
            PasswordHashing.VerifyAgainstDummy(request.Password);
            passwordIsCorrect = false;
        }
        else
        {
            passwordIsCorrect =
                PasswordHashing.Verify(request.Password, user.PasswordHash);
        }

        if (user is null || !passwordIsCorrect)
        {
            // Same message either way, so user names cannot be probed.
            ModelState.AddModelError(
                string.Empty,
                "Incorrect user name or password.");

            request.Password = string.Empty;
            return View(request);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AppClaims.CreatePrincipal(user));

        if (!string.IsNullOrEmpty(request.ReturnUrl) &&
            Url.IsLocalUrl(request.ReturnUrl))
        {
            return LocalRedirect(request.ReturnUrl);
        }

        return RedirectToAction("Start", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Start", "Home");
        }

        return View(new StudentRegisterRequest());
    }

    // Students create their own account. Only the Student role can be
    // created here; teacher accounts are still made by a teacher.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(StudentRegisterRequest request)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Start", "Home");
        }

        var group = (request.ClassGroup ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (!SchoolGroups.All.Contains(group))
        {
            ModelState.AddModelError(
                nameof(request.ClassGroup),
                "Choose your class.");
        }

        if (ModelState.IsValid &&
            await _users.UserNameExistsAsync(request.UserName))
        {
            ModelState.AddModelError(
                nameof(request.UserName),
                "That user name is already taken. Try another one.");
        }

        if (!ModelState.IsValid)
        {
            return View(KeepNonSecretFields(request));
        }

        // Always a new profile: a student can never attach themselves to an
        // existing profile (and see someone else's results) by typing a name.
        var student = await _students.CreateAsync(new Student
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            GradeLevel = group[0] - '0',
            ClassGroup = group
        });

        AppUser user;

        try
        {
            user = await _users.CreateAsync(
                new AppUser
                {
                    UserName = request.UserName,
                    Role = AppRoles.Student,
                    StudentId = student.Id
                },
                request.Password);
        }
        catch (InvalidOperationException)
        {
            // Someone took the user name a moment ago; undo the profile.
            await _students.DeleteAsync(student.Id);

            ModelState.AddModelError(
                nameof(request.UserName),
                "That user name is already taken. Try another one.");

            return View(KeepNonSecretFields(request));
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AppClaims.CreatePrincipal(user));

        TempData["SuccessMessage"] = "Your account was created. Welcome!";

        return RedirectToAction("Index", "My");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterParent()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Start", "Home");
        }

        return View(new ParentRegisterRequest());
    }

    // A parent signs up with their child's parent code, which links the
    // new account to that child. Without a valid code there is no account.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> RegisterParent(ParentRegisterRequest request)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Start", "Home");
        }

        Guid? studentId = null;

        if (ModelState.IsValid)
        {
            studentId = await _parents.FindStudentByCodeAsync(request.ChildCode);

            if (studentId is null)
            {
                ModelState.AddModelError(
                    nameof(request.ChildCode),
                    "That code does not match any student. Check it with " +
                    "your child or their teacher.");
            }
            else if (await _users.UserNameExistsAsync(request.UserName))
            {
                ModelState.AddModelError(
                    nameof(request.UserName),
                    "That user name is already taken. Try another one.");
            }
        }

        if (!ModelState.IsValid || studentId is null)
        {
            return View(new ParentRegisterRequest
            {
                ChildCode = request.ChildCode,
                UserName = request.UserName
            });
        }

        AppUser parent;

        try
        {
            parent = await _users.CreateAsync(
                new AppUser
                {
                    UserName = request.UserName,
                    Role = AppRoles.Parent
                },
                request.Password);
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(
                nameof(request.UserName),
                "That user name is already taken. Try another one.");

            return View(new ParentRegisterRequest
            {
                ChildCode = request.ChildCode,
                UserName = request.UserName
            });
        }

        await _parents.LinkAsync(parent.Id, studentId.Value);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AppClaims.CreatePrincipal(parent));

        TempData["SuccessMessage"] = "Your parent account was created.";

        return RedirectToAction("Index", "Parent");
    }

    private static StudentRegisterRequest KeepNonSecretFields(
        StudentRegisterRequest request)
    {
        return new StudentRegisterRequest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            ClassGroup = request.ClassGroup ?? string.Empty,
            UserName = request.UserName
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = User.GetUserId();
        var user = userId.HasValue
            ? await _users.GetByIdAsync(userId.Value)
            : null;

        if (user is null)
        {
            return Challenge();
        }

        if (ModelState.IsValid &&
            !PasswordHashing.Verify(request.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(
                nameof(request.CurrentPassword),
                "The current password is incorrect.");
        }

        if (!ModelState.IsValid)
        {
            return View(new ChangePasswordRequest());
        }

        var updated = await _users.SetPasswordAsync(user.Id, request.NewPassword);

        if (updated is null)
        {
            return Challenge();
        }

        // The security stamp changed, so sign in again with the new one.
        // Other devices using the old password are signed out.
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AppClaims.CreatePrincipal(updated));

        TempData["SuccessMessage"] = "Your password was changed.";

        return RedirectToAction("Start", "Home");
    }
}
