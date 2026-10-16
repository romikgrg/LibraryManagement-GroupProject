using System.Diagnostics;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>Staff landing page and the error screens.</summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>Sends each signed in user to the desk they actually work at.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        if (User.IsInRole(Roles.Manager)) return RedirectToAction("Index", "Manager");
        if (User.IsInRole(Roles.Reception)) return RedirectToAction("Index", "Circulation");
        if (User.IsInRole(Roles.Admin)) return RedirectToAction("Index", "Items");
        return RedirectToAction("Index", "Catalogue");
    }

    [HttpGet]
    public IActionResult Denied()
    {
        _logger.LogWarning("Access denied for {User} at {Path}.", User.Identity?.Name ?? "anonymous", Request.Path);
        return View(new ErrorViewModel
        {
            StatusCode = 403,
            Message = "Your account does not have access to that part of the system."
        });
    }

    /// <summary>Handles the status codes re-executed by UseStatusCodePagesWithReExecute.</summary>
    [HttpGet]
    public IActionResult HttpStatus(int? code)
    {
        var message = code switch
        {
            404 => "That page could not be found. It may have been moved or the link may be out of date.",
            403 => "Your account does not have access to that part of the system.",
            400 => "The request could not be understood. Please check the address and try again.",
            _ => "Something went wrong handling that request."
        };

        return View("Error", new ErrorViewModel { StatusCode = code, Message = message });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError("Unhandled error shown to the user. Request id {RequestId}.", requestId);

        return View(new ErrorViewModel
        {
            RequestId = requestId,
            Message = "Something went wrong and the page could not be shown. The problem has been logged."
        });
    }
}
