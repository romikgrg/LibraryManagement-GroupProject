using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

/// <summary>
/// Branch records and stock positions. Like the other services, the controller
/// says what it wants and every rule about what is allowed lives here.
/// </summary>
public class BranchService : IBranchService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BranchService> _logger;

    public BranchService(ApplicationDbContext context, ILogger<BranchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync() =>
        await _context.Branches.OrderBy(b => b.Name).AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<Branch>> GetOpenAsync() =>
        await _context.Branches.Where(b => b.IsOpen).OrderBy(b => b.Name).AsNoTracking().ToListAsync();

    public Task<Branch?> GetAsync(int id) =>
        _context.Branches.FirstOrDefaultAsync(b => b.Id == id);

    public Task<Branch?> GetByCodeAsync(string code)
    {
        var normalised = Normalise(code);
        return _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code.ToUpper() == normalised);
    }

    public async Task<ServiceResult<Branch>> CreateAsync(Branch branch)
    {
        branch.Code = Normalise(branch.Code);

        if (await _context.Branches.AnyAsync(b => b.Code.ToUpper() == branch.Code))
        {
            return ServiceResult<Branch>.Fail($"Branch code {branch.Code} is already in use.");
        }

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created branch {Code} ({Name}).", branch.Code, branch.Name);
        return ServiceResult<Branch>.Ok(branch, $"{branch.Name} has been added to the network.");
    }

    public async Task<ServiceResult> UpdateAsync(Branch branch)
    {
        var existing = await _context.Branches.FirstOrDefaultAsync(b => b.Id == branch.Id);
        if (existing is null)
        {
            return ServiceResult.Fail("That branch no longer exists.");
        }

        var code = Normalise(branch.Code);
        if (await _context.Branches.AnyAsync(b => b.Code.ToUpper() == code && b.Id != branch.Id))
        {
            return ServiceResult.Fail($"Branch code {code} is already in use.");
        }

        // Closing a branch that still has items out would leave those loans with
        // nowhere to be returned to, so the stock has to be dealt with first.
        if (!branch.IsOpen && existing.IsOpen)
        {
            var openLoans = await _context.Loans
                .CountAsync(l => l.ReturnedOn == null && l.BranchId == branch.Id);

            if (openLoans > 0)
            {
                return ServiceResult.Fail(
                    $"{existing.Name} has {openLoans} item{(openLoans == 1 ? "" : "s")} still on loan. " +
                    "Take those returns or transfer them before closing the branch.");
            }
        }

        existing.Code = code;
        existing.Name = branch.Name.Trim();
        existing.Address = branch.Address?.Trim();
        existing.Phone = branch.Phone?.Trim();
        existing.OpeningHours = branch.OpeningHours?.Trim();
        existing.IsOpen = branch.IsOpen;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated branch {Code}.", existing.Code);
        return ServiceResult.Ok($"{existing.Name} has been updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == id);
        if (branch is null)
        {
            return ServiceResult.Fail("That branch no longer exists.");
        }

        if (await _context.Items.AnyAsync(i => i.BranchId == id || i.OwningBranchId == id))
        {
            return ServiceResult.Fail(
                "This branch still holds or owns stock. Transfer the items elsewhere before deleting it.");
        }

        if (await _context.Borrowers.AnyAsync(b => b.HomeBranchId == id))
        {
            return ServiceResult.Fail(
                "Members are registered at this branch. Move them to another home branch first.");
        }

        if (await _context.Loans.AnyAsync(l => l.BranchId == id || l.ReturnBranchId == id))
        {
            return ServiceResult.Fail(
                "This branch appears in the borrowing history the manager's reports are built from. " +
                "Close it instead of deleting it.");
        }

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted branch {Code}.", branch.Code);
        return ServiceResult.Ok($"{branch.Name} has been removed.");
    }

    public async Task<BranchInventory> GetInventoryAsync(int branchId)
    {
        var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId);

        // One read of the two columns that matter, counted in memory. At branch
        // scale this is a few hundred rows and it keeps the status arithmetic in
        // one readable place.
        var held = await _context.Items
            .AsNoTracking()
            .Where(i => i.BranchId == branchId)
            .Select(i => new { i.Status, i.OwningBranchId })
            .ToListAsync();

        return new BranchInventory
        {
            Branch = branch,
            HeldHere = held.Count,
            Available = held.Count(i => i.Status == ItemStatus.Available),
            OnLoan = held.Count(i => i.Status == ItemStatus.Borrowed),
            InTransit = held.Count(i => i.Status == ItemStatus.InTransit),
            AwaitingCollection = held.Count(i => i.Status == ItemStatus.AwaitingCollection),
            OutOfAction = held.Count(i => i.Status is ItemStatus.Damaged or ItemStatus.Destroyed),
            VisitingStock = held.Count(i => i.OwningBranchId != branchId),

            OwnedButAway = await _context.Items
                .CountAsync(i => i.OwningBranchId == branchId && i.BranchId != branchId),

            OpenReservations = await _context.Reservations
                .CountAsync(r => r.CollectionBranchId == branchId &&
                                 (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready))
        };
    }

    private static string Normalise(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
