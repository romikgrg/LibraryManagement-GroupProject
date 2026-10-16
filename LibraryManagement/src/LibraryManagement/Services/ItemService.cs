using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

/// <summary>
/// Item reads and writes. The controller hands over intent; every rule about what
/// is allowed - unique codes, no deleting something with history - lives here.
/// </summary>
public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ItemService> _logger;

    public ItemService(ApplicationDbContext context, ILogger<ItemService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(string? query, string? type, ItemStatus? status,
        bool availableOnly = false, int? branchId = null)
    {
        // The branch is included because every list view and the public API show
        // where an item is held.
        IQueryable<Item> items = _context.Items.Include(i => i.Branch).AsNoTracking();

        if (branchId.HasValue)
        {
            items = items.Where(i => i.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            // "ItemKind" is the discriminator column configured in the context, so
            // filtering by type stays a WHERE clause in SQL.
            var kind = type.Trim();
            items = items.Where(i => EF.Property<string>(i, "ItemKind") == kind);
        }

        if (status.HasValue)
        {
            items = items.Where(i => i.Status == status.Value);
        }

        if (availableOnly)
        {
            items = items.Where(i => i.Status == ItemStatus.Available);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            items = items.Where(i =>
                i.Name.Contains(term) ||
                i.LibraryCode.Contains(term) ||
                (i.Description != null && i.Description.Contains(term)) ||
                (i is Book && ((Book)i).Author != null && ((Book)i).Author!.Contains(term)) ||
                (i is Book && ((Book)i).Genre != null && ((Book)i).Genre!.Contains(term)) ||
                (i is MusicRecording && ((MusicRecording)i).Artist != null && ((MusicRecording)i).Artist!.Contains(term)) ||
                (i is Toy && ((Toy)i).ToyType != null && ((Toy)i).ToyType!.Contains(term)));
        }

        return await items.OrderBy(i => i.Name).ToListAsync();
    }

    public Task<Item?> GetAsync(int id) =>
        _context.Items.Include(i => i.Branch).FirstOrDefaultAsync(i => i.Id == id);

    public Task<Item?> GetByCodeAsync(string libraryCode)
    {
        var code = Normalise(libraryCode);
        return _context.Items
            .Include(i => i.Branch)
            .FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == code);
    }

    public Task<Item?> GetWithHistoryAsync(int id) =>
        _context.Items
            .Include(i => i.Branch)
            .Include(i => i.OwningBranch)
            .Include(i => i.Loans.OrderByDescending(l => l.BorrowedOn))
                .ThenInclude(l => l.Borrower)
            .Include(i => i.Reservations.Where(r =>
                    r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready)
                .OrderBy(r => r.PlacedOn))
                .ThenInclude(r => r.Borrower)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<ServiceResult<Item>> CreateAsync(Item item)
    {
        item.LibraryCode = Normalise(item.LibraryCode);

        if (await LibraryCodeExistsAsync(item.LibraryCode))
        {
            return ServiceResult<Item>.Fail($"Library code {item.LibraryCode} is already in use.");
        }

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Type} {Code} ({Name}).", item.ItemType, item.LibraryCode, item.Name);
        return ServiceResult<Item>.Ok(item, $"{item.Name} has been added to the catalogue.");
    }

    /// <summary>
    /// Loads the tracked item, lets the caller copy the edited values onto it and
    /// saves. Doing it this way means the concrete type is never rebuilt, which
    /// matters because Entity Framework cannot change a row from a Book into a Toy.
    /// </summary>
    public async Task<ServiceResult> UpdateAsync(int id, Action<Item> applyChanges, string newLibraryCode)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return ServiceResult.Fail("That item no longer exists.");
        }

        var code = Normalise(newLibraryCode);
        if (await LibraryCodeExistsAsync(code, id))
        {
            return ServiceResult.Fail($"Library code {code} is already in use.");
        }

        var wasOnLoan = item.Status == ItemStatus.Borrowed;
        applyChanges(item);
        item.LibraryCode = code;

        // An item that is out with a member cannot be quietly put back on the shelf
        // from the edit screen; it has to be returned at the front desk.
        if (wasOnLoan && item.Status != ItemStatus.Borrowed)
        {
            var stillOut = await _context.Loans.AnyAsync(l => l.ItemId == id && l.ReturnedOn == null);
            if (stillOut)
            {
                return ServiceResult.Fail(
                    "This item is currently on loan. Process the return at the front desk before changing its status.");
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated item {Code}.", item.LibraryCode);
        return ServiceResult.Ok($"{item.Name} has been updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return ServiceResult.Fail("That item no longer exists.");
        }

        if (await _context.Loans.AnyAsync(l => l.ItemId == id && l.ReturnedOn == null))
        {
            return ServiceResult.Fail("This item is on loan and cannot be deleted until it is returned.");
        }

        if (await _context.Loans.AnyAsync(l => l.ItemId == id))
        {
            return ServiceResult.Fail(
                "This item has borrowing history, which the manager's reports rely on. " +
                "Mark it as destroyed instead of deleting it.");
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted item {Code}.", item.LibraryCode);
        return ServiceResult.Ok($"{item.Name} has been removed from the catalogue.");
    }

    public Task<bool> LibraryCodeExistsAsync(string libraryCode, int? excludeId = null)
    {
        var code = Normalise(libraryCode);
        return _context.Items.AnyAsync(i => i.LibraryCode.ToUpper() == code && (excludeId == null || i.Id != excludeId));
    }

    private static string Normalise(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
