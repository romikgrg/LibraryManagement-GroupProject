using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class MusicRecording : Item
{
    [StringLength(150)]
    public string? Artist { get; set; }

    [Display(Name = "Year released")]
    [Range(1877, 2100, ErrorMessage = "Enter a year between 1877 and 2100.")]
    public int? YearReleased { get; set; }

    [StringLength(50)]
    public string? Format { get; set; }

    [StringLength(100)]
    public string? Label { get; set; }

    public override string ItemType => "Music";

    public override string Summary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Artist)) parts.Add(Artist!);
            if (YearReleased.HasValue) parts.Add(YearReleased.Value.ToString());
            if (!string.IsNullOrWhiteSpace(Format)) parts.Add(Format!);
            return string.Join(" \u00b7 ", parts);
        }
    }
}
