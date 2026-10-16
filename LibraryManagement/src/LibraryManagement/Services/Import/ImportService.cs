using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services.Import;

/// <summary>
/// Bulk loading of stock from a CSV file or the external metadata provider.
/// </summary>
/// <remarks>
/// Importing runs in two passes on purpose. The first reads the file and judges
/// every row without writing anything, so staff see what will happen and can fix
/// a bad file before it reaches the catalogue. The second writes only the rows
/// that passed. A bulk tool that writes first and reports afterwards leaves
/// somebody to clean up by hand.
/// </remarks>
public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IMetadataProvider _provider;
    private readonly IClock _clock;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        ApplicationDbContext context,
        IMetadataProvider provider,
        IClock clock,
        ILogger<ImportService> logger)
    {
        _context = context;
        _provider = provider;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ImportPreview> PreviewCsvAsync(Stream csv, int branchId)
    {
        var preview = new ImportPreview { BranchId = branchId };

        List<ImportRow> rows;
        try
        {
            rows = ReadRows(csv);
        }
        catch (Exception ex)
        {
            // A malformed file is the user's problem to fix, not a server error.
            preview.FileError = $"The file could not be read as CSV: {ex.Message}";
            return preview;
        }

        if (rows.Count == 0)
        {
            preview.FileError = "The file has a header but no rows.";
            return preview;
        }

        // One query for every code in the file, rather than one per row.
        var codes = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.LibraryCode))
            .Select(r => r.LibraryCode!.Trim().ToUpperInvariant())
            .ToList();

        var existing = await _context.Items
            .Where(i => codes.Contains(i.LibraryCode.ToUpper()))
            .Select(i => i.LibraryCode.ToUpper())
            .ToListAsync();

        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            Validate(row);

            if (row.Verdict != ImportVerdict.Ready)
            {
                continue;
            }

            var code = row.LibraryCode!.Trim().ToUpperInvariant();

            if (existing.Contains(code))
            {
                row.Verdict = ImportVerdict.Duplicate;
                row.Problems.Add($"An item with the code {code} is already in the catalogue.");
            }
            else if (!seenInFile.Add(code))
            {
                row.Verdict = ImportVerdict.Duplicate;
                row.Problems.Add($"The code {code} appears more than once in this file.");
            }
        }

        preview.Rows = rows;
        return preview;
    }

    public async Task<ImportResult> CommitAsync(ImportPreview preview, int branchId)
    {
        var result = new ImportResult();

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
        if (branch is null)
        {
            result.Problems.Add("The destination branch could not be found.");
            return result;
        }

        foreach (var row in preview.Rows)
        {
            if (!row.WillImport)
            {
                result.Skipped++;
                continue;
            }

            var item = BuildItem(row, branchId);
            _context.Items.Add(item);
            result.Created++;
            result.CreatedCodes.Add(item.LibraryCode);
        }

        if (result.Created > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("CSV import created {Count} items at branch {Branch}.", result.Created, branch.Code);
        }

        return result;
    }

    public async Task<ImportResult> ImportFromProviderAsync(IEnumerable<string> identifiers, int branchId)
    {
        var result = new ImportResult();

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);
        if (branch is null)
        {
            result.Problems.Add("The destination branch could not be found.");
            return result;
        }

        foreach (var identifier in identifiers.Distinct())
        {
            var record = await _provider.GetByIdentifierAsync(identifier);
            if (record is null)
            {
                result.Skipped++;
                result.Problems.Add($"{identifier} was not found in {_provider.Name}.");
                continue;
            }

            // Provider records carry no library code, so one is generated from the
            // type and the next free number at this branch.
            var code = await NextCodeAsync(record.Kind);

            var item = BuildItemFromMetadata(record, code, branchId);
            _context.Items.Add(item);
            result.Created++;
            result.CreatedCodes.Add(code);
        }

        if (result.Created > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Provider import created {Count} items at branch {Branch}.",
                result.Created, branch.Code);
        }

        return result;
    }

    public string BuildCsvTemplate()
    {
        var builder = new StringBuilder();
        builder.AppendLine("LibraryCode,Kind,Name,Description,Creator,Genre,Year,Isbn,Format,MinimumAge,PieceCount");
        builder.AppendLine("BK-0100,Book,Example Book,A short description,Author Name,Fiction,2020,9780000000000,,,");
        builder.AppendLine("MU-0100,Music,Example Album,A short description,Artist Name,,1998,,CD,,");
        builder.AppendLine("TY-0100,Toy,Example Game,A short description,,,2015,,,6,48");
        return builder.ToString();
    }

    // ---------------------------------------------------------------------

    private static List<ImportRow> ReadRows(Stream csv)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,   // A short row is a row problem, not an exception.
            HeaderValidated = null,     // Absent optional columns are fine.
            BadDataFound = null
        };

        using var reader = new StreamReader(csv, Encoding.UTF8, leaveOpen: true);
        using var parser = new CsvReader(reader, config);

        var rows = new List<ImportRow>();
        parser.Read();
        parser.ReadHeader();

        var line = 1;
        while (parser.Read())
        {
            line++;
            rows.Add(new ImportRow
            {
                LineNumber = line,
                LibraryCode = parser.GetField("LibraryCode"),
                Kind = parser.GetField("Kind"),
                Name = parser.GetField("Name"),
                Description = parser.GetField("Description"),
                Creator = parser.GetField("Creator"),
                Genre = parser.GetField("Genre"),
                Year = parser.GetField("Year"),
                Isbn = parser.GetField("Isbn"),
                Format = parser.GetField("Format"),
                MinimumAge = parser.GetField("MinimumAge"),
                PieceCount = parser.GetField("PieceCount")
            });
        }

        return rows;
    }

    /// <summary>
    /// Checks one row on its own. Every problem is collected rather than
    /// returning at the first, so staff can fix a row in one pass instead of
    /// re-uploading to discover the next fault.
    /// </summary>
    private static void Validate(ImportRow row)
    {
        if (string.IsNullOrWhiteSpace(row.LibraryCode))
        {
            row.Problems.Add("A library code is required.");
        }
        else if (row.LibraryCode.Trim().Length is < 3 or > 20)
        {
            row.Problems.Add("The library code must be between 3 and 20 characters.");
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            row.Problems.Add("A name is required.");
        }
        else if (row.Name.Trim().Length > 200)
        {
            row.Problems.Add("The name is longer than 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(row.Kind))
        {
            row.Problems.Add("A kind is required: Book, Music or Toy.");
        }
        else if (Enum.TryParse<ItemKind>(row.Kind.Trim(), true, out var kind))
        {
            row.ParsedKind = kind;

            switch (kind)
            {
                case ItemKind.Book when string.IsNullOrWhiteSpace(row.Creator):
                    row.Problems.Add("Books need an author in the Creator column.");
                    break;
                case ItemKind.Music when string.IsNullOrWhiteSpace(row.Creator):
                    row.Problems.Add("Music needs an artist in the Creator column.");
                    break;
                case ItemKind.Toy when string.IsNullOrWhiteSpace(row.Genre) && string.IsNullOrWhiteSpace(row.Format):
                    // Toys use Genre or Format as the toy type; either will do.
                    row.Problems.Add("Toys need a toy type in the Genre or Format column.");
                    break;
            }
        }
        else
        {
            row.Problems.Add($"'{row.Kind}' is not a known kind. Use Book, Music or Toy.");
        }

        if (!string.IsNullOrWhiteSpace(row.Year) && !int.TryParse(row.Year, out _))
        {
            row.Problems.Add($"'{row.Year}' is not a whole number year.");
        }

        if (!string.IsNullOrWhiteSpace(row.MinimumAge) && !int.TryParse(row.MinimumAge, out _))
        {
            row.Problems.Add($"'{row.MinimumAge}' is not a whole number age.");
        }

        if (!string.IsNullOrWhiteSpace(row.PieceCount) && !int.TryParse(row.PieceCount, out _))
        {
            row.Problems.Add($"'{row.PieceCount}' is not a whole number of pieces.");
        }

        if (row.Problems.Count > 0)
        {
            row.Verdict = ImportVerdict.Invalid;
        }
    }

    private Item BuildItem(ImportRow row, int branchId)
    {
        static int? ParseInt(string? value) =>
            int.TryParse(value, out var parsed) ? parsed : null;

        Item item = row.ParsedKind switch
        {
            ItemKind.Book => new Book
            {
                Author = row.Creator?.Trim(),
                Genre = row.Genre?.Trim(),
                YearPublished = ParseInt(row.Year),
                Isbn = row.Isbn?.Trim()
            },
            ItemKind.Music => new MusicRecording
            {
                Artist = row.Creator?.Trim(),
                YearReleased = ParseInt(row.Year),
                Format = row.Format?.Trim(),
                Label = row.Genre?.Trim()
            },
            ItemKind.Toy => new Toy
            {
                ToyType = (row.Genre ?? row.Format)?.Trim(),
                MinimumAge = ParseInt(row.MinimumAge),
                PieceCount = ParseInt(row.PieceCount)
            },
            _ => throw new InvalidOperationException($"Unknown kind '{row.ParsedKind}'.")
        };

        item.LibraryCode = row.LibraryCode!.Trim().ToUpperInvariant();
        item.Name = row.Name!.Trim();
        item.Description = row.Description?.Trim();
        item.Status = ItemStatus.Available;
        item.AddedOn = _clock.Today;
        item.BranchId = branchId;
        item.OwningBranchId = branchId;

        return item;
    }

    private Item BuildItemFromMetadata(MetadataResult record, string code, int branchId)
    {
        Item item = record.Kind switch
        {
            ItemKind.Book => new Book
            {
                Author = record.Creator,
                Genre = record.Genre,
                YearPublished = record.Year,
                Isbn = record.Isbn
            },
            ItemKind.Music => new MusicRecording
            {
                Artist = record.Creator,
                YearReleased = record.Year,
                Format = record.Format,
                Label = record.Publisher
            },
            ItemKind.Toy => new Toy
            {
                ToyType = record.Genre ?? record.Creator,
                MinimumAge = record.MinimumAge,
                PieceCount = record.PieceCount
            },
            _ => throw new InvalidOperationException($"Unknown kind '{record.Kind}'.")
        };

        item.LibraryCode = code;
        item.Name = record.Title.Trim();
        item.Description = record.Description?.Trim();
        item.Status = ItemStatus.Available;
        item.AddedOn = _clock.Today;
        item.BranchId = branchId;
        item.OwningBranchId = branchId;

        return item;
    }

    /// <summary>
    /// Builds the next free code for a type, for example BK-0012. Reads the
    /// existing codes with the right prefix and takes the highest number.
    /// </summary>
    private async Task<string> NextCodeAsync(ItemKind kind)
    {
        var prefix = kind switch
        {
            ItemKind.Book => "BK",
            ItemKind.Music => "MU",
            _ => "TY"
        };

        var codes = await _context.Items
            .Where(i => i.LibraryCode.StartsWith(prefix))
            .Select(i => i.LibraryCode)
            .ToListAsync();

        var highest = codes
            .Select(c =>
            {
                var tail = c.Split('-').LastOrDefault();
                return int.TryParse(tail, out var number) ? number : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}-{highest + 1:D4}";
    }
}
