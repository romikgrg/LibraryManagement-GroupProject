using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers.Api;

/// <summary>
/// The public developer API: a read-only view of what the library holds.
/// </summary>
/// <remarks>
/// Deliberately separate from the MVC controllers that serve HTML. It returns
/// JSON, it is versioned in the route so a later version can change shape
/// without breaking callers, and every response is a purpose-built contract
/// object rather than an entity. Serialising entities directly would leak
/// borrower names through navigation properties and tie the public contract to
/// the database schema, so that the next migration becomes a breaking API
/// change.
///
/// Read-only is enforced structurally: there are no POST, PUT or DELETE actions
/// to call, so no amount of guessing a URL will change anything.
/// </remarks>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class CatalogueApiController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IItemService _items;
    private readonly IBranchService _branches;
    private readonly IFinePolicy _finePolicy;

    public CatalogueApiController(IItemService items, IBranchService branches, IFinePolicy finePolicy)
    {
        _items = items;
        _branches = branches;
        _finePolicy = finePolicy;
    }

    /// <summary>Lists catalogue items, newest search first. Only public fields are returned.</summary>
    /// <param name="query">Free text across titles, creators and codes.</param>
    /// <param name="type">Book, Music or Toy.</param>
    /// <param name="branch">Branch code, for example DRW.</param>
    /// <param name="availableOnly">Restrict to items on the shelf right now.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page, capped at 100.</param>
    [HttpGet("items")]
    [ProducesResponseType(typeof(PagedResponse<CatalogueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? query = null,
        [FromQuery] string? type = null,
        [FromQuery] string? branch = null,
        [FromQuery] bool availableOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (page < 1)
        {
            return BadRequest(new ApiError("page must be 1 or greater."));
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            return BadRequest(new ApiError($"pageSize must be between 1 and {MaxPageSize}."));
        }

        if (type is not null && !IsKnownType(type))
        {
            return BadRequest(new ApiError("type must be Book, Music or Toy."));
        }

        int? branchId = null;
        if (!string.IsNullOrWhiteSpace(branch))
        {
            var found = await _branches.GetByCodeAsync(branch);
            if (found is null)
            {
                return BadRequest(new ApiError($"No branch exists with the code '{branch}'."));
            }
            branchId = found.Id;
        }

        var results = await _items.SearchAsync(query, type, status: null, availableOnly: availableOnly, branchId: branchId);

        var pageItems = results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(CatalogueItemDto.From)
            .ToList();

        return Ok(new PagedResponse<CatalogueItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = results.Count,
            TotalPages = (int)Math.Ceiling(results.Count / (double)pageSize),
            Items = pageItems
        });
    }

    /// <summary>Fetches a single item by its library code.</summary>
    [HttpGet("items/{libraryCode}")]
    [ProducesResponseType(typeof(CatalogueItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(string libraryCode)
    {
        var item = await _items.GetByCodeAsync(libraryCode);

        return item is null
            ? NotFound(new ApiError($"No item exists with the library code '{libraryCode}'."))
            : Ok(CatalogueItemDto.From(item));
    }

    /// <summary>The item types and the genres in use, for building a filter interface.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(CategoriesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var all = await _items.SearchAsync(null, null, null);

        return Ok(new CategoriesDto
        {
            Types = all.GroupBy(i => i.ItemType)
                .Select(g => new CategoryDto(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList(),

            Genres = all.OfType<Book>()
                .Where(b => !string.IsNullOrWhiteSpace(b.Genre))
                .GroupBy(b => b.Genre!)
                .Select(g => new CategoryDto(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList()
        });
    }

    /// <summary>Branch list with opening hours and whether each is open.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ServiceStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var branches = await _branches.GetAllAsync();

        return Ok(new ServiceStatusDto
        {
            Service = "Darwin Community Library",
            ApiVersion = "1.0",
            GeneratedAt = DateTime.UtcNow,
            LendingTerms = _finePolicy.Description,
            Branches = branches.Select(b => new BranchDto
            {
                Code = b.Code,
                Name = b.Name,
                Address = b.Address,
                Phone = b.Phone,
                OpeningHours = b.OpeningHours,
                IsOpen = b.IsOpen
            }).ToList()
        });
    }

    private static bool IsKnownType(string type) =>
        type.Equals("Book", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("Music", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("Toy", StringComparison.OrdinalIgnoreCase);
}
