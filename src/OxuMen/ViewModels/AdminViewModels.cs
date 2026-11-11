using OxuMen.Models;

namespace OxuMen.ViewModels;

public class TeacherListItemViewModel
{
    public AppUser User { get; set; } = new();

    public List<string> Classes { get; set; } = new();

    public int StudentCount { get; set; }
}

public class EditTeacherViewModel
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public EditTeacherRequest Input { get; set; } = new();
}

// The grid of class check boxes on the teacher forms.
public class ClassPickerViewModel
{
    // Form field name the boxes post as, e.g. "Classes" or "Input.Classes".
    public string FieldName { get; set; } = "Classes";

    public List<string> Selected { get; set; } = new();
}
