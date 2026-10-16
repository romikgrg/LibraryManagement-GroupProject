using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// Branch records. Managed by the administrator, because adding or closing a
/// location is a network decision rather than a front desk one.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class BranchesController : Controller
{
    private readonly IBranchService _branches;

    public BranchesController(IBranchService branches)
    {
        _branches = branches;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var branches = await _branches.GetAllAsync();

        // Each row shows its stock position, so the list doubles as a quick
        // network overview rather than a bare directory.
        var inventories = new List<BranchInventory>();
        foreach (var branch in branches)
        {
            inventories.Add(await _branches.GetInventoryAsync(branch.Id));
        }

        return View(inventories);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var inventory = await _branches.GetInventoryAsync(id);
        return inventory.Branch is null ? NotFound() : View(inventory);
    }

    [HttpGet]
    public IActionResult Create() => View(new Branch());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Branch branch)
    {
        if (!ModelState.IsValid)
        {
            return View(branch);
        }

        var result = await _branches.CreateAsync(branch);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(branch.Code), result.Message);
            return View(branch);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var branch = await _branches.GetAsync(id);
        return branch is null ? NotFound() : View(branch);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Branch branch)
    {
        if (id != branch.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(branch);
        }

        var result = await _branches.UpdateAsync(branch);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(branch);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var inventory = await _branches.GetInventoryAsync(id);
        return inventory.Branch is null ? NotFound() : View(inventory);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _branches.DeleteAsync(id);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
