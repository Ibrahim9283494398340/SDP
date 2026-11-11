using System.ComponentModel.DataAnnotations;

namespace OxuMen.Models;

public static class AccountRules
{
    public const string UserNamePattern = @"^[A-Za-z0-9._-]{3,50}$";

    public const string UserNameMessage =
        "Use 3–50 letters, digits, dots, dashes or underscores.";
}

public class LoginRequest
{
    [Required]
    [Display(Name = "User name")]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "The password must be at least 6 characters.")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// The admin creates a teacher account.
public class NewTeacherRequest
{
    [Required]
    [RegularExpression(AccountRules.UserNamePattern,
        ErrorMessage = AccountRules.UserNameMessage)]
    [Display(Name = "User name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Teacher passwords must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Classes like "2A" the teacher will see.
    public List<string> Classes { get; set; } = new();
}

// The admin changes which classes a teacher sees. Passwords are changed
// only by the teacher themselves (Change Password).
public class EditTeacherRequest
{
    public List<string> Classes { get; set; } = new();
}

// Used both to create a student's login and to reset its password.
// UserName is only needed when the login does not exist yet.
public class StudentLoginRequest
{
    [RegularExpression(AccountRules.UserNamePattern,
        ErrorMessage = AccountRules.UserNameMessage)]
    [Display(Name = "User name")]
    public string? UserName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "The password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// A student signs up on their own. This creates their Student profile
// and their login together.
public class StudentRegisterRequest
{
    [Required]
    [StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose your class.")]
    [RegularExpression(@"^[1-5][A-D]$", ErrorMessage = "Choose your class.")]
    [Display(Name = "Class")]
    public string ClassGroup { get; set; } = string.Empty;

    [Required]
    [RegularExpression(AccountRules.UserNamePattern,
        ErrorMessage = AccountRules.UserNameMessage)]
    [Display(Name = "User name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "The password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
