using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models.ViewModels;

/// <summary>
/// One form for all three item types. The admin picks a kind and only the fields
/// for that kind are validated and saved, which keeps a single Create view and a
/// single Edit view instead of three of each.
/// </summary>
public class ItemFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Item type")]
    public ItemKind Kind { get; set; }

    [Required(ErrorMessage = "A library code is required.")]
    [StringLength(20, MinimumLength = 3)]
    [Display(Name = "Library code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "A name is required.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Available;

    /// <summary>
    /// The branch that holds the item. On an existing item this is read only in
    /// the interface: stock moves between branches through a transfer, never by
    /// editing a dropdown, so that the loan history and the shelves agree.
    /// </summary>
    [Display(Name = "Held at")]
    public int BranchId { get; set; }

    [Display(Name = "Owned by")]
    public int OwningBranchId { get; set; }

    /// <summary>Filled by the controller so the create form can offer a picker.</summary>
    public IReadOnlyList<Branch> Branches { get; set; } = Array.Empty<Branch>();

    /// <summary>Shown beside the locked branch field on the edit form.</summary>
    public string? BranchName { get; set; }

    // Book
    [StringLength(150)] public string? Author { get; set; }
    [StringLength(100)] public string? Genre { get; set; }
    [Display(Name = "Year published")] public int? YearPublished { get; set; }
    [StringLength(20)] [Display(Name = "ISBN")] public string? Isbn { get; set; }

    // Music
    [StringLength(150)] public string? Artist { get; set; }
    [Display(Name = "Year released")] public int? YearReleased { get; set; }
    [StringLength(50)] public string? Format { get; set; }
    [StringLength(100)] public string? Label { get; set; }

    // Toy
    [StringLength(100)] [Display(Name = "Toy type")] public string? ToyType { get; set; }
    [Display(Name = "Minimum age")] public int? MinimumAge { get; set; }
    [Display(Name = "Number of pieces")] public int? PieceCount { get; set; }

    /// <summary>True once the item exists, because the type cannot be changed afterwards.</summary>
    public bool IsExisting => Id != 0;

    /// <summary>Builds the right concrete subclass for a brand new item.</summary>
    public Item ToNewItem()
    {
        Item item = Kind switch
        {
            ItemKind.Book => new Book(),
            ItemKind.Music => new MusicRecording(),
            ItemKind.Toy => new Toy(),
            _ => throw new InvalidOperationException($"Unknown item kind '{Kind}'.")
        };
        ApplyTo(item);
        item.AddedOn = DateTime.Today;
        return item;
    }

    /// <summary>Copies the form values onto an item that already exists.</summary>
    public void ApplyTo(Item item)
    {
        item.LibraryCode = LibraryCode.Trim();
        item.Name = Name.Trim();
        item.Description = Description?.Trim();
        item.Status = Status;

        // Only a brand new item takes its branch from this form. An existing
        // item's location belongs to the transfer workflow.
        if (item.Id == 0)
        {
            item.BranchId = BranchId;
            item.OwningBranchId = OwningBranchId == 0 ? BranchId : OwningBranchId;
        }

        switch (item)
        {
            case Book book:
                book.Author = Author?.Trim();
                book.Genre = Genre?.Trim();
                book.YearPublished = YearPublished;
                book.Isbn = Isbn?.Trim();
                break;
            case MusicRecording music:
                music.Artist = Artist?.Trim();
                music.YearReleased = YearReleased;
                music.Format = Format?.Trim();
                music.Label = Label?.Trim();
                break;
            case Toy toy:
                toy.ToyType = ToyType?.Trim();
                toy.MinimumAge = MinimumAge;
                toy.PieceCount = PieceCount;
                break;
        }
    }

    /// <summary>Fills a form from an existing item, for the edit screen.</summary>
    public static ItemFormViewModel FromItem(Item item)
    {
        var model = new ItemFormViewModel
        {
            Id = item.Id,
            LibraryCode = item.LibraryCode,
            Name = item.Name,
            Description = item.Description,
            Status = item.Status,
            BranchId = item.BranchId,
            OwningBranchId = item.OwningBranchId,
            BranchName = item.Branch?.DisplayName
        };

        switch (item)
        {
            case Book book:
                model.Kind = ItemKind.Book;
                model.Author = book.Author;
                model.Genre = book.Genre;
                model.YearPublished = book.YearPublished;
                model.Isbn = book.Isbn;
                break;
            case MusicRecording music:
                model.Kind = ItemKind.Music;
                model.Artist = music.Artist;
                model.YearReleased = music.YearReleased;
                model.Format = music.Format;
                model.Label = music.Label;
                break;
            case Toy toy:
                model.Kind = ItemKind.Toy;
                model.ToyType = toy.ToyType;
                model.MinimumAge = toy.MinimumAge;
                model.PieceCount = toy.PieceCount;
                break;
        }

        return model;
    }

    /// <summary>
    /// Only validate the fields that belong to the chosen kind. A toy with a year
    /// of publication typed into a hidden book field should not block the save.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsExisting && BranchId == 0)
        {
            yield return new ValidationResult("Choose the branch that will hold this item.", new[] { nameof(BranchId) });
        }

        switch (Kind)
        {
            case ItemKind.Book:
                if (string.IsNullOrWhiteSpace(Author))
                    yield return new ValidationResult("An author is required for a book.", new[] { nameof(Author) });
                if (YearPublished is < 1400 or > 2100)
                    yield return new ValidationResult("Enter a year between 1400 and 2100.", new[] { nameof(YearPublished) });
                break;

            case ItemKind.Music:
                if (string.IsNullOrWhiteSpace(Artist))
                    yield return new ValidationResult("An artist is required for a music item.", new[] { nameof(Artist) });
                if (YearReleased is < 1877 or > 2100)
                    yield return new ValidationResult("Enter a year between 1877 and 2100.", new[] { nameof(YearReleased) });
                break;

            case ItemKind.Toy:
                if (string.IsNullOrWhiteSpace(ToyType))
                    yield return new ValidationResult("A toy type is required for a toy.", new[] { nameof(ToyType) });
                if (MinimumAge is < 0 or > 18)
                    yield return new ValidationResult("Enter a minimum age between 0 and 18.", new[] { nameof(MinimumAge) });
                break;
        }
    }
}
