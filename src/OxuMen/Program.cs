using OxuMen.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<
    IStudentRepository,
    JsonStudentRepository>();

builder.Services.AddSingleton<
    IReadingAssessmentRepository,
    JsonReadingAssessmentRepository>();

var app = builder.Build();

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