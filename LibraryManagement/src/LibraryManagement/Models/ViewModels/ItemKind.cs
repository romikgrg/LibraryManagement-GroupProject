using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models.ViewModels;

/// <summary>Which concrete subclass of <see cref="Item"/> a form is editing.</summary>
public enum ItemKind
{
    [Display(Name = "Book")] Book = 0,
    [Display(Name = "Music")] Music = 1,
    [Display(Name = "Toy")] Toy = 2
}
