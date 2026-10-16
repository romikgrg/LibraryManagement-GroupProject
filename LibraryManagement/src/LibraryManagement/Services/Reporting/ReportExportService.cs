using System.Globalization;
using System.Text;
using CsvHelper;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Services.Reporting;

/// <summary>
/// Builds the manager's three exportable reports in CSV and PDF.
/// </summary>
/// <remarks>
/// Each report is gathered once into a simple table of rows, and the two
/// formatters render that same table. The figures in the PDF and the CSV
/// therefore cannot disagree, which they could if each format ran its own
/// queries.
/// </remarks>
public class ReportExportService : IReportExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly ILogger<ReportExportService> _logger;

    public ReportExportService(ApplicationDbContext context, IClock clock, ILogger<ReportExportService> logger)
    {
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ExportedFile> ExportCsvAsync(ReportKind kind, int? branchId = null)
    {
        var table = await BuildAsync(kind, branchId);

        using var buffer = new MemoryStream();
        using (var writer = new StreamWriter(buffer, new UTF8Encoding(true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var column in table.Columns)
            {
                csv.WriteField(column);
            }
            csv.NextRecord();

            foreach (var row in table.Rows)
            {
                foreach (var cell in row)
                {
                    csv.WriteField(cell);
                }
                csv.NextRecord();
            }
        }

        _logger.LogInformation("Exported {Report} as CSV ({Rows} rows).", kind, table.Rows.Count);

        return new ExportedFile(
            $"{table.Slug}-{_clock.Today:yyyy-MM-dd}.csv",
            "text/csv",
            buffer.ToArray());
    }

    public async Task<ExportedFile> ExportPdfAsync(ReportKind kind, int? branchId = null)
    {
        var table = await BuildAsync(kind, branchId);

        var bytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(9).FontFamily(Fonts.Calibri));

                page.Header().Column(header =>
                {
                    header.Item().Text(table.Title).FontSize(16).SemiBold();
                    header.Item().Text(table.Subtitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                    header.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Table(grid =>
                {
                    grid.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in table.Columns)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    grid.Header(headerRow =>
                    {
                        foreach (var column in table.Columns)
                        {
                            headerRow.Cell()
                                .Background(Colors.Grey.Lighten3)
                                .Padding(4)
                                .Text(column).SemiBold();
                        }
                    });

                    foreach (var row in table.Rows)
                    {
                        foreach (var cell in row)
                        {
                            grid.Cell()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(4)
                                .Text(cell);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Darwin Community Library  ·  page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        _logger.LogInformation("Exported {Report} as PDF ({Rows} rows).", kind, table.Rows.Count);

        return new ExportedFile(
            $"{table.Slug}-{_clock.Today:yyyy-MM-dd}.pdf",
            "application/pdf",
            bytes);
    }

    // ---------------------------------------------------------------------
    // Report building. Each returns the same shape, so both formatters work
    // against one structure.
    // ---------------------------------------------------------------------

    private Task<ReportTable> BuildAsync(ReportKind kind, int? branchId) => kind switch
    {
        ReportKind.Borrowing => BuildBorrowingAsync(branchId),
        ReportKind.FineRevenue => BuildFineRevenueAsync(branchId),
        ReportKind.InventoryHealth => BuildInventoryHealthAsync(branchId),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown report.")
    };

    private async Task<ReportTable> BuildBorrowingAsync(int? branchId)
    {
        var loans = await LoanQuery(branchId)
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Include(l => l.Branch)
            .AsNoTracking()
            .OrderByDescending(l => l.BorrowedOn)
            .ToListAsync();

        var table = new ReportTable
        {
            Title = "Borrowing statistics",
            Subtitle = await SubtitleAsync(branchId, $"{loans.Count} loan record(s)"),
            Slug = "borrowing-statistics",
            Columns = new List<string>
            {
                "Loan", "Library code", "Item", "Type", "Member", "Membership",
                "Branch", "Borrowed", "Due", "Returned", "Days kept", "Days late", "Fine"
            }
        };

        var today = _clock.Today;

        foreach (var loan in loans)
        {
            var daysKept = ((loan.ReturnedOn ?? today).Date - loan.BorrowedOn.Date).Days;

            table.Rows.Add(new List<string>
            {
                loan.Id.ToString(),
                loan.Item?.LibraryCode ?? "",
                loan.Item?.Name ?? "",
                loan.Item?.ItemType ?? "",
                loan.Borrower?.FullName ?? "",
                loan.Borrower?.MembershipNumber ?? "",
                loan.Branch?.Name ?? "",
                loan.BorrowedOn.ToString("yyyy-MM-dd"),
                loan.DueOn.ToString("yyyy-MM-dd"),
                loan.ReturnedOn?.ToString("yyyy-MM-dd") ?? "Still out",
                daysKept.ToString(),
                loan.DaysOverdue(today).ToString(),
                loan.FineAmount.ToString("0.00")
            });
        }

        return table;
    }

    private async Task<ReportTable> BuildFineRevenueAsync(int? branchId)
    {
        var fined = await LoanQuery(branchId)
            .Where(l => l.FineAmount > 0)
            .Include(l => l.Item)
            .Include(l => l.Borrower)
            .Include(l => l.Branch)
            .AsNoTracking()
            .OrderByDescending(l => l.ReturnedOn)
            .ToListAsync();

        var charged = fined.Sum(l => l.FineAmount);
        var paid = fined.Where(l => l.FinePaid).Sum(l => l.FineAmount);

        var table = new ReportTable
        {
            Title = "Fine revenue audit",
            Subtitle = await SubtitleAsync(branchId,
                $"{charged:C} charged, {paid:C} collected, {charged - paid:C} outstanding"),
            Slug = "fine-revenue-audit",
            Columns = new List<string>
            {
                "Loan", "Member", "Membership", "Item", "Library code", "Branch",
                "Due", "Returned", "Days late", "Fine", "Paid"
            }
        };

        var today = _clock.Today;

        foreach (var loan in fined)
        {
            table.Rows.Add(new List<string>
            {
                loan.Id.ToString(),
                loan.Borrower?.FullName ?? "",
                loan.Borrower?.MembershipNumber ?? "",
                loan.Item?.Name ?? "",
                loan.Item?.LibraryCode ?? "",
                loan.Branch?.Name ?? "",
                loan.DueOn.ToString("yyyy-MM-dd"),
                loan.ReturnedOn?.ToString("yyyy-MM-dd") ?? "Still out",
                loan.DaysOverdue(today).ToString(),
                loan.FineAmount.ToString("0.00"),
                loan.FinePaid ? "Yes" : "No"
            });
        }

        // A money report that does not add up is no use to an auditor, so the
        // totals are part of the export rather than something the reader works out.
        table.Rows.Add(new List<string>
        {
            "", "", "", "", "", "", "", "", "TOTAL", charged.ToString("0.00"), paid.ToString("0.00")
        });

        return table;
    }

    private async Task<ReportTable> BuildInventoryHealthAsync(int? branchId)
    {
        var branches = await _context.Branches
            .Where(b => branchId == null || b.Id == branchId)
            .OrderBy(b => b.Name)
            .AsNoTracking()
            .ToListAsync();

        // AsNoTracking is constrained to reference types, so it has to be applied to
        // the entity query before any projection to a value type.
        var items = await _context.Items
            .AsNoTracking()
            .Select(i => new { i.BranchId, i.OwningBranchId, i.Status })
            .ToListAsync();

        var openHolds = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready)
            .Select(r => r.CollectionBranchId)
            .ToListAsync();

        var table = new ReportTable
        {
            Title = "Inventory health",
            Subtitle = await SubtitleAsync(branchId, $"{items.Count} item(s) across the network"),
            Slug = "inventory-health",
            Columns = new List<string>
            {
                "Branch", "Held", "Available", "On loan", "In transit", "Awaiting collection",
                "Damaged", "Destroyed", "Visiting stock", "Owned but away", "Open holds", "Availability %"
            }
        };

        foreach (var branch in branches)
        {
            var held = items.Where(i => i.BranchId == branch.Id).ToList();
            var available = held.Count(i => i.Status == ItemStatus.Available);

            table.Rows.Add(new List<string>
            {
                branch.DisplayName,
                held.Count.ToString(),
                available.ToString(),
                held.Count(i => i.Status == ItemStatus.Borrowed).ToString(),
                held.Count(i => i.Status == ItemStatus.InTransit).ToString(),
                held.Count(i => i.Status == ItemStatus.AwaitingCollection).ToString(),
                held.Count(i => i.Status == ItemStatus.Damaged).ToString(),
                held.Count(i => i.Status == ItemStatus.Destroyed).ToString(),
                held.Count(i => i.OwningBranchId != branch.Id).ToString(),
                items.Count(i => i.OwningBranchId == branch.Id && i.BranchId != branch.Id).ToString(),
                openHolds.Count(id => id == branch.Id).ToString(),
                held.Count == 0 ? "0.0" : (available * 100.0 / held.Count).ToString("0.0")
            });
        }

        return table;
    }

    private IQueryable<Loan> LoanQuery(int? branchId)
    {
        IQueryable<Loan> query = _context.Loans;
        return branchId.HasValue
            ? query.Where(l => l.BranchId == branchId.Value)
            : query;
    }

    private async Task<string> SubtitleAsync(int? branchId, string detail)
    {
        var scope = "All branches";

        if (branchId.HasValue)
        {
            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId.Value);
            scope = branch?.DisplayName ?? "Unknown branch";
        }

        return $"{scope}  ·  generated {_clock.Now:d MMMM yyyy, h:mm tt}  ·  {detail}";
    }

    /// <summary>
    /// The intermediate shape both formatters render. Keeping it deliberately
    /// plain means neither CsvHelper nor QuestPDF leaks into the report logic.
    /// </summary>
    private class ReportTable
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Slug { get; set; } = "report";
        public List<string> Columns { get; set; } = new();
        public List<List<string>> Rows { get; } = new();
    }
}
