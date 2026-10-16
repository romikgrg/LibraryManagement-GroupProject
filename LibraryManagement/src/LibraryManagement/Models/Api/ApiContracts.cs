using LibraryManagement.Models;

namespace LibraryManagement.Controllers.Api;

/// <summary>
/// The shapes the public API returns.
/// </summary>
/// <remarks>
/// These exist so that the API contract and the database schema can change
/// independently. Nothing here exposes a borrower, a loan or an internal
/// identifier: an outside caller sees the catalogue the way a member of the
/// public sees the shelves.
/// </remarks>
public class CatalogueItemDto
{
    public string LibraryCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Creator { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Available { get; set; }
    public string? BranchCode { get; set; }
    public string? BranchName { get; set; }

    /// <summary>
    /// Builds the public view of one item. The branch is included only when it
    /// was loaded; the API never triggers a lazy read to fill it in.
    /// </summary>
    public static CatalogueItemDto From(Item item) => new()
    {
        LibraryCode = item.LibraryCode,
        Title = item.Name,
        Type = item.ItemType,
        Description = item.Description,
        Creator = item switch
        {
            Book book => book.Author,
            MusicRecording music => music.Artist,
            Toy toy => toy.ToyType,
            _ => null
        },
        Genre = item switch
        {
            Book book => book.Genre,
            MusicRecording music => music.Format,
            Toy toy => toy.MinimumAge.HasValue ? $"Ages {toy.MinimumAge}+" : null,
            _ => null
        },
        Year = item switch
        {
            Book book => book.YearPublished,
            MusicRecording music => music.YearReleased,
            _ => null
        },
        Status = item.Status.ToString(),
        Available = item.Status == ItemStatus.Available,
        BranchCode = item.Branch?.Code,
        BranchName = item.Branch?.Name
    };
}

/// <summary>A page of results, with the counts a caller needs to page through.</summary>
public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
}

public record CategoryDto(string Name, int Count);

public class CategoriesDto
{
    public IReadOnlyList<CategoryDto> Types { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<CategoryDto> Genres { get; set; } = Array.Empty<CategoryDto>();
}

public class BranchDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? OpeningHours { get; set; }
    public bool IsOpen { get; set; }
}

public class ServiceStatusDto
{
    public string Service { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string? LendingTerms { get; set; }
    public IReadOnlyList<BranchDto> Branches { get; set; } = Array.Empty<BranchDto>();
}

/// <summary>A machine-readable error, so callers do not have to parse prose.</summary>
public record ApiError(string Message);
