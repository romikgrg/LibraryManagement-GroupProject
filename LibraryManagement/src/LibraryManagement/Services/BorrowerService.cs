using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

public class BorrowerService : IBorrowerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BorrowerService> _logger;

    public BorrowerService(ApplicationDbContext context, ILogger<BorrowerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Borrower>> SearchAsync(string? query)
    {
        IQueryable<Borrower> borrowers = _context.Borrowers
            .Include(b => b.Loans)
            .Include(b => b.HomeBranch)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            borrowers = borrowers.Where(b =>
                b.FirstName.Contains(term) ||
                b.LastName.Contains(term) ||
                b.MembershipNumber.Contains(term) ||
                (b.Email != null && b.Email.Contains(term)) ||
                (b.Phone != null && b.Phone.Contains(term)));
        }

        return await borrowers
            .OrderBy(b => b.LastName).ThenBy(b => b.FirstName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Borrower>> GetActiveAsync() =>
        await _context.Borrowers
            .Include(b => b.HomeBranch)
            .Where(b => b.IsActive)
            .OrderBy(b => b.LastName).ThenBy(b => b.FirstName)
            .AsNoTracking()
            .ToListAsync();

    public Task<Borrower?> GetAsync(int id) =>
        _context.Borrowers.FirstOrDefaultAsync(b => b.Id == id);

    public Task<Borrower?> GetByMembershipNumberAsync(string membershipNumber)
    {
        var number = Normalise(membershipNumber);
        return _context.Borrowers
            .Include(b => b.HomeBranch)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.MembershipNumber.ToUpper() == number);
    }

    public Task<Borrower?> GetWithHistoryAsync(int id) =>
        _context.Borrowers
            .Include(b => b.HomeBranch)
            .Include(b => b.Loans.OrderByDescending(l => l.BorrowedOn))
                .ThenInclude(l => l.Item)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<ServiceResult<Borrower>> CreateAsync(Borrower borrower)
    {
        borrower.MembershipNumber = Normalise(borrower.MembershipNumber);

        if (await _context.Borrowers.AnyAsync(b => b.MembershipNumber.ToUpper() == borrower.MembershipNumber))
        {
            return ServiceResult<Borrower>.Fail($"Membership number {borrower.MembershipNumber} is already in use.");
        }

        _context.Borrowers.Add(borrower);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created borrower {Membership}.", borrower.MembershipNumber);
        return ServiceResult<Borrower>.Ok(borrower, $"{borrower.FullName} has been added.");
    }

    public async Task<ServiceResult> UpdateAsync(Borrower borrower)
    {
        var existing = await _context.Borrowers.FirstOrDefaultAsync(b => b.Id == borrower.Id);
        if (existing is null)
        {
            return ServiceResult.Fail("That member no longer exists.");
        }

        var membership = Normalise(borrower.MembershipNumber);
        if (await _context.Borrowers.AnyAsync(b => b.MembershipNumber.ToUpper() == membership && b.Id != borrower.Id))
        {
            return ServiceResult.Fail($"Membership number {membership} is already in use.");
        }

        if (!borrower.IsActive && await _context.Loans.AnyAsync(l => l.BorrowerId == borrower.Id && l.ReturnedOn == null))
        {
            return ServiceResult.Fail("This member still has items on loan, so their membership cannot be suspended yet.");
        }

        existing.MembershipNumber = membership;
        existing.FirstName = borrower.FirstName.Trim();
        existing.LastName = borrower.LastName.Trim();
        existing.Email = borrower.Email?.Trim();
        existing.Phone = borrower.Phone?.Trim();
        existing.Address = borrower.Address?.Trim();
        existing.IsActive = borrower.IsActive;
        existing.HomeBranchId = borrower.HomeBranchId;
        existing.PreferredChannel = borrower.PreferredChannel;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated borrower {Membership}.", existing.MembershipNumber);
        return ServiceResult.Ok($"{existing.FullName} has been updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var borrower = await _context.Borrowers.FirstOrDefaultAsync(b => b.Id == id);
        if (borrower is null)
        {
            return ServiceResult.Fail("That member no longer exists.");
        }

        if (await _context.Loans.AnyAsync(l => l.BorrowerId == id && l.ReturnedOn == null))
        {
            return ServiceResult.Fail("This member has items on loan and cannot be deleted.");
        }

        if (await _context.Loans.AnyAsync(l => l.BorrowerId == id))
        {
            return ServiceResult.Fail(
                "This member has borrowing history. Set them to inactive instead of deleting them.");
        }

        _context.Borrowers.Remove(borrower);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted borrower {Membership}.", borrower.MembershipNumber);
        return ServiceResult.Ok($"{borrower.FullName} has been removed.");
    }

    /// <summary>Suggests the next membership number so staff do not invent their own.</summary>
    public async Task<string> NextMembershipNumberAsync()
    {
        var numbers = await _context.Borrowers
            .Where(b => b.MembershipNumber.StartsWith("M-"))
            .Select(b => b.MembershipNumber)
            .ToListAsync();

        var highest = numbers
            .Select(n => int.TryParse(n[2..], out var value) ? value : 0)
            .DefaultIfEmpty(1000)
            .Max();

        return $"M-{highest + 1}";
    }

    private static string Normalise(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();
}
