using LibraryManagement.Models;
using LibraryManagement.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// The notification log. Shows every message the system has written to a member,
/// whether it reached them, and lets staff run the due-date sweep by hand.
/// </summary>
/// <remarks>
/// Open to reception and to the manager: the desk needs it to answer "did you
/// tell me?", and the manager needs it to see whether the reminders are working.
/// </remarks>
[Authorize(Roles = $"{Roles.Reception},{Roles.Manager}")]
public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Index(NotificationKind? kind)
    {
        ViewData["Kind"] = kind;
        ViewData["Summary"] = await _notifications.GetSummaryAsync();
        return View(await _notifications.GetRecentAsync(100, kind));
    }

    /// <summary>
    /// Runs the due-soon and overdue pass. In a deployed system this would be a
    /// scheduled job; here it is a button so the behaviour can be demonstrated.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunSweep()
    {
        var result = await _notifications.RunDueDateSweepAsync();
        TempData["Success"] = result.Describe();
        return RedirectToAction(nameof(Index));
    }
}
