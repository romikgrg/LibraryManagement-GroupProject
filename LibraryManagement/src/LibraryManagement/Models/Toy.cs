using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Toy : Item
{
    [StringLength(100)]
    [Display(Name = "Toy type")]
    public string? ToyType { get; set; }

    [Display(Name = "Minimum age")]
    [Range(0, 18, ErrorMessage = "Enter a minimum age between 0 and 18.")]
    public int? MinimumAge { get; set; }

    [Display(Name = "Number of pieces")]
    [Range(1, 10000)]
    public int? PieceCount { get; set; }

    public override string ItemType => "Toy";

    public override string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(ToyType)) parts.Add(ToyType!);
            if (MinimumAge.HasValue) parts.Add($"ages {MinimumAge}+");
            if (PieceCount.HasValue) parts.Add($"{PieceCount} pieces");
            return string.Join(" \u00b7 ", parts);
        }
    }
}
