using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Book : Item
{
    [StringLength(150)]
    public string? Author { get; set; }

    [StringLength(100)]
    public string? Genre { get; set; }

    [Display(Name = "Year published")]
    [Range(1400, 2100, ErrorMessage = "Enter a year between 1400 and 2100.")]
    public int? YearPublished { get; set; }

    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    public override string ItemType => "Book";

    public override string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Author)) parts.Add($"by {Author}");
            if (!string.IsNullOrWhiteSpace(Genre)) parts.Add(Genre!);
            if (YearPublished.HasValue) parts.Add(YearPublished.Value.ToString());
            return string.Join(" \u00b7 ", parts);
        }
    }
}
