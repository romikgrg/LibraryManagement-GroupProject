namespace LibraryManagement.Models.ViewModels;

/// <summary>What the importer decided about one row of a CSV.</summary>
public enum ImportVerdict
{
    /// <summary>Valid and new. It will be created.</summary>
    Ready = 0,

    /// <summary>Valid, but an item with this library code already exists. It will be skipped.</summary>
    Duplicate = 1,

    /// <summary>Something is wrong with the row. It will be skipped and the reason shown.</summary>
    Invalid = 2
}

/// <summary>
/// One line of an uploaded CSV, with the verdict the importer reached about it.
/// </summary>
/// <remarks>
/// The raw values are kept alongside the parsed ones so the preview table can
/// show staff exactly what was in the file, including the parts that failed to
/// parse. A row the user cannot see is a row they cannot fix.
/// </remarks>
public class ImportRow
{
    /// <summary>Line number in the uploaded file, counting the header as line 1.</summary>
    public int LineNumber { get; set; }

    public ImportVerdict Verdict { get; set; } = ImportVerdict.Ready;
    public List<string> Problems { get; set; } = new();

    public string? LibraryCode { get; set; }
    public string? Kind { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Creator { get; set; }
    public string? Genre { get; set; }
    public string? Year { get; set; }
    public string? Isbn { get; set; }
    public string? Format { get; set; }
    public string? MinimumAge { get; set; }
    public string? PieceCount { get; set; }

    /// <summary>The parsed type, set only once the row has validated.</summary>
    public ItemKind ParsedKind { get; set; }

    public bool WillImport => Verdict == ImportVerdict.Ready;

    public string ProblemText => string.Join(" ", Problems);
}

/// <summary>
/// The result of reading a file without saving: every row, with verdicts, plus
/// the counts the confirmation screen shows.
/// </summary>
public class ImportPreview
{
    public string FileName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public List<ImportRow> Rows { get; set; } = new();

    /// <summary>A problem with the file itself rather than with any one row.</summary>
    public string? FileError { get; set; }

    public int ReadyCount => Rows.Count(r => r.Verdict == ImportVerdict.Ready);
    public int DuplicateCount => Rows.Count(r => r.Verdict == ImportVerdict.Duplicate);
    public int InvalidCount => Rows.Count(r => r.Verdict == ImportVerdict.Invalid);

    public bool HasAnythingToImport => ReadyCount > 0;
    public bool IsUsable => FileError is null && Rows.Count > 0;
}

/// <summary>What a committed import actually did.</summary>
public class ImportResult
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public List<string> Problems { get; set; } = new();
    public List<string> CreatedCodes { get; set; } = new();

    public bool Succeeded => Created > 0;

    public string Describe()
    {
        if (Created == 0)
        {
            return Skipped == 0
                ? "Nothing was imported."
                : $"Nothing was imported. {Skipped} row{(Skipped == 1 ? " was" : "s were")} skipped.";
        }

        var message = $"Imported {Created} item{(Created == 1 ? "" : "s")}.";
        if (Skipped > 0)
        {
            message += $" {Skipped} row{(Skipped == 1 ? " was" : "s were")} skipped.";
        }

        return message;
    }
}
