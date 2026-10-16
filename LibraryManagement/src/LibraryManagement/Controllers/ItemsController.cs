using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

/// <summary>
/// Admin CRUD over the collection. Role based authorisation is applied once at
/// the class level, so a new action cannot be added without inheriting it.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class ItemsController : Controller
{
    private readonly IItemService _items;
    private readonly IBranchService _branches;
    private readonly IBranchContext _branchContext;

    public ItemsController(IItemService items, IBranchService branches, IBranchContext branchContext)
    {
        _items = items;
        _branches = branches;
        _branchContext = branchContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query, string? type, ItemStatus? status, int? branchId)
    {
        var model = new ItemSearchViewModel
        {
            Query = query,
            Type = type,
            Status = status,
            BranchId = branchId,
            Branches = await _branches.GetAllAsync(),
            Results = await _items.SearchAsync(query, type, status, availableOnly: false, branchId: branchId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _items.GetWithHistoryAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create(ItemKind kind = ItemKind.Book)
    {
        return View(new ItemFormViewModel
        {
            Kind = kind,
            Branches = await _branches.GetOpenAsync(),
            // An administrator at a branch almost always adds stock for their own
            // shelves, so that branch is preselected.
            BranchId = await _branchContext.GetOperatingBranchIdAsync() ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ItemFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            form.Branches = await _branches.GetOpenAsync();
            return View(form);
        }

        var result = await _items.CreateAsync(form.ToNewItem());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(form.LibraryCode), result.Message);
            form.Branches = await _branches.GetOpenAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _items.GetAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var form = ItemFormViewModel.FromItem(item);
        form.Branches = await _branches.GetAllAsync();
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ItemFormViewModel form)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            form.Branches = await _branches.GetAllAsync();
            return View(form);
        }

        // The service loads the tracked entity and hands it back here to be filled
        // in, which keeps the concrete type intact.
        var result = await _items.UpdateAsync(id, item => form.ApplyTo(item), form.LibraryCode);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            form.Branches = await _branches.GetAllAsync();
            return View(form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _items.GetWithHistoryAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _items.DeleteAsync(id);

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
