using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

/// <summary>
/// The single Entity Framework context. It carries the library tables and the
/// ASP.NET Core Identity tables, so staff logins and library data live in one
/// database and one transaction scope.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<MusicRecording> MusicRecordings => Set<MusicRecording>();
    public DbSet<Toy> Toys => Set<Toy>();
    public DbSet<Borrower> Borrowers => Set<Borrower>();
    public DbSet<Loan> Loans => Set<Loan>();

    // Added by the group project expansion.
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureItems(builder);
        ConfigureBorrowers(builder);
        ConfigureLoans(builder);
        ConfigureBranches(builder);
        ConfigureTransfers(builder);
        ConfigureReservations(builder);
        ConfigureNotifications(builder);
    }

    private static void ConfigureItems(ModelBuilder builder)
    {
        // Table Per Hierarchy: Book, MusicRecording and Toy share the Items table
        // and are told apart by the ItemKind discriminator column.
        builder.Entity<Item>()
            .HasDiscriminator<string>("ItemKind")
            .HasValue<Book>("Book")
            .HasValue<MusicRecording>("Music")
            .HasValue<Toy>("Toy");

        // Reception looks items up by the code on the label, so it must be unique
        // and indexed.
        builder.Entity<Item>()
            .HasIndex(i => i.LibraryCode)
            .IsUnique();

        // Filtering the catalogue by branch is the commonest query once the
        // network has more than one location.
        builder.Entity<Item>()
            .HasIndex(i => new { i.BranchId, i.Status });

        builder.Entity<Item>()
            .HasOne(i => i.Branch)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // The owning branch is a second relationship to the same table, so it
        // needs its own navigation and no inverse collection.
        builder.Entity<Item>()
            .HasOne(i => i.OwningBranch)
            .WithMany()
            .HasForeignKey(i => i.OwningBranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBorrowers(ModelBuilder builder)
    {
        builder.Entity<Borrower>()
            .HasIndex(b => b.MembershipNumber)
            .IsUnique();

        builder.Entity<Borrower>()
            .HasOne(b => b.HomeBranch)
            .WithMany(br => br.Members)
            .HasForeignKey(b => b.HomeBranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLoans(ModelBuilder builder)
    {
        // Loans are the borrowing history. Deleting an item or a member must never
        // silently erase it, so both relationships are restricted rather than
        // cascading.
        builder.Entity<Loan>()
            .HasOne(l => l.Item)
            .WithMany(i => i.Loans)
            .HasForeignKey(l => l.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Loan>()
            .HasOne(l => l.Borrower)
            .WithMany(b => b.Loans)
            .HasForeignKey(l => l.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Loan>()
            .HasOne(l => l.Branch)
            .WithMany()
            .HasForeignKey(l => l.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Loan>()
            .HasOne(l => l.ReturnBranch)
            .WithMany()
            .HasForeignKey(l => l.ReturnBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Finding the open loan for an item is the hottest query at the front desk.
        builder.Entity<Loan>()
            .HasIndex(l => new { l.ItemId, l.ReturnedOn });

        // The due-soon sweep scans open loans by due date every time it runs.
        builder.Entity<Loan>()
            .HasIndex(l => new { l.ReturnedOn, l.DueOn });
    }

    private static void ConfigureBranches(ModelBuilder builder)
    {
        builder.Entity<Branch>()
            .HasIndex(b => b.Code)
            .IsUnique();
    }

    private static void ConfigureTransfers(ModelBuilder builder)
    {
        builder.Entity<Transfer>()
            .HasOne(t => t.Item)
            .WithMany(i => i.Transfers)
            .HasForeignKey(t => t.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transfer>()
            .HasOne(t => t.FromBranch)
            .WithMany()
            .HasForeignKey(t => t.FromBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transfer>()
            .HasOne(t => t.ToBranch)
            .WithMany()
            .HasForeignKey(t => t.ToBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transfer>()
            .HasOne(t => t.Reservation)
            .WithMany()
            .HasForeignKey(t => t.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);

        // The transfers desk lists everything still open, oldest first.
        builder.Entity<Transfer>()
            .HasIndex(t => new { t.Status, t.RequestedOn });
    }

    private static void ConfigureReservations(ModelBuilder builder)
    {
        builder.Entity<Reservation>()
            .HasOne(r => r.Item)
            .WithMany(i => i.Reservations)
            .HasForeignKey(r => r.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Reservation>()
            .HasOne(r => r.Borrower)
            .WithMany(b => b.Reservations)
            .HasForeignKey(r => r.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Reservation>()
            .HasOne(r => r.CollectionBranch)
            .WithMany()
            .HasForeignKey(r => r.CollectionBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Working out who is next in line reads the queue for one item in the
        // order people joined it.
        builder.Entity<Reservation>()
            .HasIndex(r => new { r.ItemId, r.Status, r.PlacedOn });
    }

    private static void ConfigureNotifications(ModelBuilder builder)
    {
        builder.Entity<Notification>()
            .HasOne(n => n.Borrower)
            .WithMany(b => b.Notifications)
            .HasForeignKey(n => n.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(n => n.Item)
            .WithMany()
            .HasForeignKey(n => n.ItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // The dashboard shows the newest first and filters by kind.
        builder.Entity<Notification>()
            .HasIndex(n => new { n.CreatedOn, n.Kind });
    }
}
