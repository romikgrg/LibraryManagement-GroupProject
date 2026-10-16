using LibraryManagement.Models.ViewModels;

namespace LibraryManagement.Services.Import;

/// <summary>
/// A stand-in for a public metadata service such as Open Library or Trove.
/// </summary>
/// <remarks>
/// It answers from a fixed in-memory catalogue rather than the network, which
/// keeps the assignment runnable offline and on a marker's virtual machine with
/// no internet. A short artificial delay is included so the import screen's
/// loading behaviour is the same as it would be against a real endpoint.
/// </remarks>
public class MockMetadataProvider : IMetadataProvider
{
    public string Name => "Mock Public Metadata Service";

    private static readonly IReadOnlyList<MetadataResult> Catalogue = new List<MetadataResult>
    {
        new() { Identifier = "OL-0001", Kind = ItemKind.Book, Title = "The Secret River", Creator = "Kate Grenville", Genre = "Historical fiction", Year = 2005, Isbn = "9781841959146", Publisher = "Canongate", Description = "A convict family settles on the Hawkesbury." },
        new() { Identifier = "OL-0002", Kind = ItemKind.Book, Title = "Dark Emu", Creator = "Bruce Pascoe", Genre = "History", Year = 2014, Isbn = "9781921248016", Publisher = "Magabala Books", Description = "Argues for Aboriginal agriculture and aquaculture." },
        new() { Identifier = "OL-0003", Kind = ItemKind.Book, Title = "Cloudstreet", Creator = "Tim Winton", Genre = "Literary fiction", Year = 1991, Isbn = "9780143180043", Publisher = "Penguin", Description = "Two families share a rambling house in Perth." },
        new() { Identifier = "OL-0004", Kind = ItemKind.Book, Title = "Growing Up Aboriginal in Australia", Creator = "Anita Heiss", Genre = "Anthology", Year = 2018, Isbn = "9781863959810", Publisher = "Black Inc.", Description = "Fifty writers on childhood and identity." },
        new() { Identifier = "OL-0005", Kind = ItemKind.Book, Title = "Jasper Jones", Creator = "Craig Silvey", Genre = "Young adult", Year = 2009, Isbn = "9781741758450", Publisher = "Allen & Unwin", Description = "A small West Australian town in the summer of 1965." },

        new() { Identifier = "MB-0001", Kind = ItemKind.Music, Title = "Gurrumul", Creator = "Geoffrey Gurrumul Yunupingu", Year = 2008, Format = "CD", Publisher = "Skinnyfish Music", Description = "Sung in Gumatj, Gälpu and Djambarrpuynu." },
        new() { Identifier = "MB-0002", Kind = ItemKind.Music, Title = "Back in Black", Creator = "AC/DC", Year = 1980, Format = "Vinyl", Publisher = "Albert Productions", Description = "Recorded in the Bahamas after Bon Scott's death." },
        new() { Identifier = "MB-0003", Kind = ItemKind.Music, Title = "The Boatman's Call", Creator = "Nick Cave and the Bad Seeds", Year = 1997, Format = "CD", Publisher = "Mute", Description = "Spare piano ballads." },

        new() { Identifier = "TY-0001", Kind = ItemKind.Toy, Title = "Magnetic tile set", Creator = "Connetix", Year = 2021, MinimumAge = 3, PieceCount = 62, Description = "Translucent magnetic building tiles." },
        new() { Identifier = "TY-0002", Kind = ItemKind.Toy, Title = "Ticket to Ride", Creator = "Days of Wonder", Year = 2004, MinimumAge = 8, PieceCount = 240, Description = "Railway route building board game." },
        new() { Identifier = "TY-0003", Kind = ItemKind.Toy, Title = "Robotics starter kit", Creator = "Makeblock", Year = 2019, MinimumAge = 10, PieceCount = 38, Description = "Programmable rover with sensors." }
    };

    public async Task<IReadOnlyList<MetadataResult>> SearchAsync(string query, string? type = null, int maxResults = 10)
    {
        await Task.Delay(120); // Stands in for the network round trip.

        IEnumerable<MetadataResult> results = Catalogue;

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ItemKind>(type, true, out var kind))
        {
            results = results.Where(r => r.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            results = results.Where(r =>
                r.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (r.Creator?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Genre?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Isbn?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                r.Identifier.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return results.Take(maxResults).ToList();
    }

    public async Task<MetadataResult?> GetByIdentifierAsync(string identifier)
    {
        await Task.Delay(60);

        return Catalogue.FirstOrDefault(r =>
            string.Equals(r.Identifier, identifier?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
