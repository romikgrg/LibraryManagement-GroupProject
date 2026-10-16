using LibraryManagement.Models.ViewModels;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;


public class CatalogueController : Controller
{
    private const string RecentSearchesKey = "catalogue.recent";
    private const int RecentSearchesKept = 5;

    private readonly IItemService _items;
    private readonly IFinePolicy _finePolicy;

    public CatalogueController(IItemService items, IFinePolicy finePolicy)
    {
        _items = items;
        _finePolicy = finePolicy;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query, string? type, bool availableOnly = false)
    {
        RememberSearch(query);

        var model = new CatalogueViewModel
        {
            Query = query,
            Type = type,
            AvailableOnly = availableOnly,
            Results = await _items.SearchAsync(query, type, status: null, availableOnly: availableOnly),
            RecentSearches = HttpContext.Session.GetObject<List<string>>(RecentSearchesKey) ?? new List<string>()
        };

        ViewData["LendingTerms"] = _finePolicy.Description;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _items.GetAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        ViewData["LendingTerms"] = _finePolicy.Description;
        return View(item);
    }

    /// <summary>
    /// Keeps the last few searches in session so a visitor on a phone can step
    /// back to what they tried a minute ago.
    /// </summary>
    private void RememberSearch(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var term = query.Trim();
        var recent = HttpContext.Session.GetObject<List<string>>(RecentSearchesKey) ?? new List<string>();

        recent.RemoveAll(existing => string.Equals(existing, term, StringComparison.OrdinalIgnoreCase));
        recent.Insert(0, term);

        if (recent.Count > RecentSearchesKept)
        {
            recent = recent.Take(RecentSearchesKept).ToList();
        }

        HttpContext.Session.SetObject(RecentSearchesKey, recent);
    }
}
