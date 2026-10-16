using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

/// <summary>
/// The rules for moving an item between branches.
/// </summary>
/// <remarks>
/// The item's location only changes on receipt. Between dispatch and receipt the
/// item sits in <see cref="ItemStatus.InTransit"/> and still reads as belonging
/// to the sending branch, which means two desks can never both believe they have
/// it. That single decision is what keeps the network's stock figures honest.
/// </remarks>
public class TransferService : ITransferService
{
    private readonly ApplicationDbContext _context;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;
    private readonly ILogger<TransferService> _logger;

    public TransferService(
        ApplicationDbContext context,
        IClock clock,
        INotificationService notifications,
        ILogger<TransferService> logger)
    {
        _context = context;
        _clock = clock;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ServiceResult<Transfer>> RequestAsync(string libraryCode, int toBranchId,
        string? reason, int? reservationId = null)
    {
        var code = Normalise(libraryCode);

        var item = await _context.Items.FirstOrDefaultAsync(i => i.LibraryCode.ToUpper() == code);
        if (item is null)
        {
            return ServiceResult<Transfer>.Fail($"No item was found with the library code {code}.");
        }

        if (item.BranchId == toBranchId)
        {
            return ServiceResult<Transfer>.Fail($"{item.Name} is already at that branch.");
        }

        var destination = await _context.Branches.FirstOrDefaultAsync(b => b.Id == toBranchId);
        if (destination is null)
        {
            return ServiceResult<Transfer>.Fail("That branch could not be found.");
        }

        if (!destination.IsOpen)
        {
            return ServiceResult<Transfer>.Fail($"{destination.Name} is closed and cannot receive stock.");
        }

        if (await _context.Transfers.AnyAsync(t => t.ItemId == item.Id &&
                (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit)))
        {
            return ServiceResult<Transfer>.Fail($"{item.Name} already has a transfer in progress.");
        }

        if (item.Status is ItemStatus.Destroyed)
        {
            return ServiceResult<Transfer>.Fail($"{item.Name} has been destroyed and cannot be moved.");
        }

        var transfer = new Transfer
        {
            ItemId = item.Id,
            FromBranchId = item.BranchId,
            ToBranchId = toBranchId,
            ReservationId = reservationId,
            Status = TransferStatus.Requested,
            RequestedOn = _clock.Today,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
        };

        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer requested for {Code}: branch {From} to {To}.",
            item.LibraryCode, transfer.FromBranchId, transfer.ToBranchId);

        return ServiceResult<Transfer>.Ok(transfer,
            $"{item.Name} has been requested from its current branch for {destination.Name}.");
    }

    public async Task<ServiceResult<Transfer>> DispatchAsync(int transferId, string? notes)
    {
        var transfer = await LoadAsync(transferId);
        if (transfer is null)
        {
            return ServiceResult<Transfer>.Fail("That transfer could not be found.");
        }

        if (transfer.Status != TransferStatus.Requested)
        {
            return ServiceResult<Transfer>.Fail($"This transfer is already marked as {transfer.Status}.");
        }

        var item = transfer.Item!;

        // An item cannot be put in the van while a member has it.
        if (item.Status == ItemStatus.Borrowed)
        {
            return ServiceResult<Transfer>.Fail(
                $"{item.Name} is out on loan. Take the return before dispatching it.");
        }

        if (item.Status == ItemStatus.AwaitingCollection)
        {
            return ServiceResult<Transfer>.Fail(
                $"{item.Name} is on the holds shelf for another member and cannot be sent away.");
        }

        transfer.Status = TransferStatus.InTransit;
        transfer.DispatchedOn = _clock.Today;
        transfer.Notes = Append(transfer.Notes, notes);

        item.Status = ItemStatus.InTransit;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer {Id} dispatched ({Code}).", transfer.Id, item.LibraryCode);
        return ServiceResult<Transfer>.Ok(transfer,
            $"{item.Name} is in transit to {transfer.ToBranch?.Name}.");
    }

    public async Task<ServiceResult<Transfer>> ReceiveAsync(int transferId, string? notes)
    {
        var transfer = await LoadAsync(transferId);
        if (transfer is null)
        {
            return ServiceResult<Transfer>.Fail("That transfer could not be found.");
        }

        if (transfer.Status != TransferStatus.InTransit)
        {
            return ServiceResult<Transfer>.Fail(
                transfer.Status == TransferStatus.Requested
                    ? "This item has not been dispatched yet."
                    : $"This transfer is already marked as {transfer.Status}.");
        }

        var item = transfer.Item!;

        transfer.Status = TransferStatus.Received;
        transfer.ReceivedOn = _clock.Today;
        transfer.Notes = Append(transfer.Notes, notes);

        // This is the only place an item's location changes.
        item.BranchId = transfer.ToBranchId;

        // A transfer raised to satisfy a hold puts the item straight onto the
        // holds shelf rather than back into general stock.
        var reservation = transfer.ReservationId.HasValue
            ? await _context.Reservations.FirstOrDefaultAsync(r => r.Id == transfer.ReservationId.Value)
            : null;

        if (reservation is not null && reservation.IsActive)
        {
            item.Status = ItemStatus.AwaitingCollection;
        }
        else
        {
            item.Status = ItemStatus.Available;
        }

        await _context.SaveChangesAsync();

        if (reservation is not null && reservation.IsActive)
        {
            await _notifications.NotifyAsync(
                NotificationKind.TransferArrived,
                reservation.BorrowerId,
                item.Id,
                new Dictionary<string, string> { ["branch"] = transfer.ToBranch?.Name ?? "your branch" });
        }

        _logger.LogInformation("Transfer {Id} received at branch {Branch} ({Code}).",
            transfer.Id, transfer.ToBranchId, item.LibraryCode);

        return ServiceResult<Transfer>.Ok(transfer,
            $"{item.Name} has arrived at {transfer.ToBranch?.Name} and is now on the shelf there.");
    }

    public async Task<ServiceResult> CancelAsync(int transferId, string? reason)
    {
        var transfer = await LoadAsync(transferId);
        if (transfer is null)
        {
            return ServiceResult.Fail("That transfer could not be found.");
        }

        if (!transfer.IsOpen)
        {
            return ServiceResult.Fail($"This transfer is already marked as {transfer.Status}.");
        }

        // An item already on the road has to arrive somewhere, so cancelling after
        // dispatch would strand it with no owner.
        if (transfer.Status == TransferStatus.InTransit)
        {
            return ServiceResult.Fail(
                "This item has already been dispatched. Receive it at the destination, then raise a return transfer.");
        }

        transfer.Status = TransferStatus.Cancelled;
        transfer.Notes = Append(transfer.Notes, reason);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer {Id} cancelled.", transfer.Id);
        return ServiceResult.Ok($"The transfer of {transfer.Item?.Name} has been cancelled.");
    }

    public async Task<IReadOnlyList<Transfer>> GetOpenAsync(int? branchId = null)
    {
        IQueryable<Transfer> query = BaseQuery()
            .Where(t => t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit);

        if (branchId.HasValue)
        {
            query = query.Where(t => t.FromBranchId == branchId.Value || t.ToBranchId == branchId.Value);
        }

        return await query.OrderBy(t => t.RequestedOn).AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<Transfer>> GetIncomingAsync(int branchId) =>
        await BaseQuery()
            .Where(t => t.ToBranchId == branchId &&
                        (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit))
            .OrderBy(t => t.RequestedOn)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IReadOnlyList<Transfer>> GetOutgoingAsync(int branchId) =>
        await BaseQuery()
            .Where(t => t.FromBranchId == branchId &&
                        (t.Status == TransferStatus.Requested || t.Status == TransferStatus.InTransit))
            .OrderBy(t => t.RequestedOn)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IReadOnlyList<Transfer>> GetRecentAsync(int count = 20) =>
        await BaseQuery()
            .OrderByDescending(t => t.ReceivedOn ?? t.DispatchedOn ?? t.RequestedOn)
            .ThenByDescending(t => t.Id)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();

    public Task<Transfer?> GetAsync(int id) =>
        BaseQuery().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

    private IQueryable<Transfer> BaseQuery() =>
        _context.Transfers
            .Include(t => t.Item)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch);

    /// <summary>Tracked load, used by the commands that change state.</summary>
    private Task<Transfer?> LoadAsync(int id) =>
        _context.Transfers
            .Include(t => t.Item)
            .Include(t => t.FromBranch)
            .Include(t => t.ToBranch)
            .FirstOrDefaultAsync(t => t.Id == id);

    /// <summary>Keeps the running note rather than overwriting what an earlier step wrote.</summary>
    private static string? Append(string? existing, string? addition)
    {
        if (string.IsNullOrWhiteSpace(addition))
        {
            return existing;
        }

        return string.IsNullOrWhiteSpace(existing)
            ? addition.Trim()
            : $"{existing}\n{addition.Trim()}";
    }

    private static string Normalise(string code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
