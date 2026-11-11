using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OxuMen.Models;

namespace OxuMen.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // "/" lands here: send each role to its own start page.
    [HttpGet]
    public async Task<IActionResult> Start()
    {
        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Teachers", "Admin");
        }

        if (User.IsInRole(AppRoles.Teacher))
        {
            return RedirectToAction("Index", "StudentPages");
        }

        if (User.IsInRole(AppRoles.Student))
        {
            return RedirectToAction("Index", "My");
        }

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Login", "Account");
    }

    // Sample teacher dashboard.
    [Authorize(Roles = AppRoles.TeacherOrAdmin)]
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
