using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OxuMen.Models;
using OxuMen.Repositories;

namespace OxuMen.Security;

public static class AppClaims
{
    public const string StudentId = "oxumen:student_id";

    public const string SecurityStamp = "oxumen:security_stamp";

    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role),
            new(SecurityStamp, user.SecurityStamp)
        };

        if (user.StudentId.HasValue)
        {
            claims.Add(new Claim(StudentId, user.StudentId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        return Guid.TryParse(
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out var id)
            ? id
            : null;
    }

    public static Guid? GetStudentId(this ClaimsPrincipal principal)
    {
        return Guid.TryParse(principal.FindFirst(StudentId)?.Value, out var id)
            ? id
            : null;
    }

    // Runs on every request: signs the user out if the account was deleted,
    // its role changed, or its password was changed or reset elsewhere.
    public static async Task ValidatePrincipalAsync(
        CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var userId = principal?.GetUserId();
        var stamp = principal?.FindFirst(SecurityStamp)?.Value;
        var role = principal?.FindFirst(ClaimTypes.Role)?.Value;

        AppUser? user = null;

        if (userId.HasValue && stamp is not null)
        {
            var users = context.HttpContext.RequestServices
                .GetRequiredService<IUserRepository>();

            user = await users.GetByIdAsync(userId.Value);
        }

        if (user is null ||
            user.SecurityStamp != stamp ||
            user.Role != role ||
            user.StudentId != principal!.GetStudentId())
        {
            context.RejectPrincipal();

            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
