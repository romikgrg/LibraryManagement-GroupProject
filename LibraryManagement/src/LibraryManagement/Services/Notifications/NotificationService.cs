using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Services.Notifications;

/// <summary>
/// Composes the message for an event, picks a channel the member can actually be
/// reached on, records the attempt and asks the channel to deliver it.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IReadOnlyList<INotificationChannel> _channels;
    private readonly IClock _clock;
    private readonly NotificationRules _rules;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Every registered channel is injected as a collection. The container hands
    /// over whichever implementations are registered, so adding a push channel
    /// later needs no change here.
    /// </summary>
    public NotificationService(
        ApplicationDbContext context,
        IEnumerable<INotificationChannel> channels,
        IClock clock,
        IOptions<NotificationRules> rules,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _channels = channels.ToList();
        _clock = clock;
        _rules = rules.Value;
        _logger = logger;
    }

    public async Task<Notification?> NotifyAsync(NotificationKind kind, int borrowerId, int? itemId = null,
        IReadOnlyDictionary<string, string>? facts = null)
    {
        if (!_rules.Enabled)
        {
            return null;
        }

        var borrower = await _context.Borrowers.FirstOrDefaultAsync(b => b.Id == borrowerId);
        if (borrower is null)
        {
            _logger.LogWarning("Notification {Kind} skipped: member {BorrowerId} not found.", kind, borrowerId);
            return null;
        }

        var item = itemId.HasValue
            ? await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId.Value)
            : null;

        var (subject, body) = NotificationComposer.Compose(kind, borrower, item, facts);
        var channel = ChooseChannel(borrower);

        var notification = new Notification
        {
            BorrowerId = borrower.Id,
            ItemId = item?.Id,
            Kind = kind,
            Channel = channel?.Channel ?? borrower.PreferredChannel,
            Subject = subject,
            Body = body,
            CreatedOn = _clock.Now,
            Recipient = channel?.AddressFor(borrower) ?? "(no contact details)"
        };

        if (channel is null)
        {
            notification.Status = NotificationStatus.Skipped;
            notification.FailureReason = "The member has no email address or mobile number on file.";
        }
        else
        {
            // A transport problem must not roll back the borrow or return that
            // triggered this message, so the failure is recorded rather than thrown.
            try
            {
                var result = await channel.SendAsync(notification, borrower);
                if (result.Succeeded)
                {
                    notification.Status = NotificationStatus.Sent;
                    notification.SentOn = _clock.Now;
                }
                else
                {
                    notification.Status = NotificationStatus.Failed;
                    notification.FailureReason = result.FailureReason;
                }
            }
            catch (Exception ex)
            {
                notification.Status = NotificationStatus.Failed;
                notification.FailureReason = ex.Message;
                _logger.LogError(ex, "Notification {Kind} to member {BorrowerId} threw.", kind, borrowerId);
            }
        }

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    /// <summary>
    /// The member's preference wins when they can be reached on it. Otherwise any
    /// channel that can reach them is better than none, which is why this falls
    /// through rather than giving up.
    /// </summary>
    private INotificationChannel? ChooseChannel(Borrower borrower)
    {
        var preferred = _channels.FirstOrDefault(c => c.Channel == borrower.PreferredChannel && c.CanReach(borrower));
        return preferred ?? _channels.FirstOrDefault(c => c.CanReach(borrower));
    }

    public async Task<NotificationSweepResult> RunDueDateSweepAsync()
    {
        var today = _clock.Today;
        var result = new NotificationSweepResult();

        var openLoans = await _context.Loans
            .Include(l => l.Item)
            .Where(l => l.ReturnedOn == null)
            .AsNoTracking()
            .ToListAsync();

        result.LoansExamined = openLoans.Count;

        foreach (var loan in openLoans)
        {
            var daysUntilDue = (loan.DueOn.Date - today).Days;

            var kind = daysUntilDue switch
            {
                < 0 => NotificationKind.Overdue,
                _ when daysUntilDue <= _rules.DueSoonDays => NotificationKind.DueSoon,
                _ => (NotificationKind?)null
            };

            if (kind is null)
            {
                continue;
            }

            // One message per loan per kind per day, so pressing the button twice
            // does not spam the member.
            var alreadySentToday = await _context.Notifications.AnyAsync(n =>
                n.BorrowerId == loan.BorrowerId &&
                n.ItemId == loan.ItemId &&
                n.Kind == kind.Value &&
                n.CreatedOn >= today);

            if (alreadySentToday)
            {
                result.Skipped++;
                continue;
            }

            var facts = new Dictionary<string, string>
            {
                ["dueOn"] = loan.DueOn.ToString("d MMMM yyyy"),
                ["days"] = Math.Abs(daysUntilDue).ToString()
            };

            await NotifyAsync(kind.Value, loan.BorrowerId, loan.ItemId, facts);

            if (kind == NotificationKind.Overdue)
            {
                result.OverdueSent++;
            }
            else
            {
                result.DueSoonSent++;
            }
        }

        _logger.LogInformation("Due-date sweep: {Summary}", result.Describe());
        return result;
    }

    public async Task<IReadOnlyList<Notification>> GetRecentAsync(int count = 50, NotificationKind? kind = null)
    {
        IQueryable<Notification> query = _context.Notifications
            .Include(n => n.Borrower)
            .Include(n => n.Item)
            .AsNoTracking();

        if (kind.HasValue)
        {
            query = query.Where(n => n.Kind == kind.Value);
        }

        return await query
            .OrderByDescending(n => n.CreatedOn)
            .ThenByDescending(n => n.Id)
            .Take(count)
            .ToListAsync();
    }

    public async Task<NotificationSummary> GetSummaryAsync()
    {
        var today = _clock.Today;

        var all = await _context.Notifications
            .AsNoTracking()
            .Select(n => new { n.Status, n.Channel, n.Kind, n.CreatedOn })
            .ToListAsync();

        return new NotificationSummary
        {
            Total = all.Count,
            Sent = all.Count(n => n.Status == NotificationStatus.Sent),
            Failed = all.Count(n => n.Status is NotificationStatus.Failed or NotificationStatus.Skipped),
            SentToday = all.Count(n => n.CreatedOn >= today),
            ByChannel = all.GroupBy(n => n.Channel)
                .Select(g => new ChannelCount(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList(),
            ByKind = all.GroupBy(n => n.Kind)
                .Select(g => new KindCount(g.Key, g.Count()))
                .OrderByDescending(k => k.Count)
                .ToList()
        };
    }
}
