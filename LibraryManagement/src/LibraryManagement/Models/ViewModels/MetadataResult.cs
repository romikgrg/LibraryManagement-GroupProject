namespace LibraryManagement.Models.ViewModels;

/// <summary>
/// One candidate record returned by an external metadata provider, before staff
/// have decided to accept it into the catalogue.
/// </summary>
/// <remarks>
/// Deliberately a flat shape rather than an <see cref="Item"/>. A provider
/// result is untrusted suggestion data: it has no library code, no branch and no
/// status, and it only becomes an item once a staff member reviews it and the
/// importer builds the right subclass from it.
/// </remarks>
public class MetadataResult
{
    /// <summary>The provider's own identifier, used to fetch the full record.</summary>
    public string Identifier { get; set; } = string.Empty;

    public ItemKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Author, artist or manufacturer, depending on the kind.</summary>
    public string? Creator { get; set; }

    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public string? Publisher { get; set; }
    public int? MinimumAge { get; set; }
    public int? PieceCount { get; set; }

    /// <summary>A one line description of the match, shown in the results table.</summary>
    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Creator)) parts.Add(Creator!);
            if (Year.HasValue) parts.Add(Year.Value.ToString());
            if (!string.IsNullOrWhiteSpace(Genre)) parts.Add(Genre!);
            if (!string.IsNullOrWhiteSpace(Format)) parts.Add(Format!);
            return string.Join(" · ", parts);
        }
    }
}
