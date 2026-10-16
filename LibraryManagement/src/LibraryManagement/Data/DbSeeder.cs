using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

/// <summary>
/// Creates the database and fills it with the staff logins plus enough sample
/// data that every screen has something to show on a fresh checkout: three
/// branches, stock spread across them, loans, fines, a reservation queue, an
/// in-flight transfer and a notification history.
/// </summary>
public static class DbSeeder
{
    public const string DefaultPassword = "Library@2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await context.Database.EnsureCreatedAsync();
        await GuardAgainstStaleSchemaAsync(context, logger);

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Branches have to exist before anything else, because items, members and
        // staff all point at one.
        var branches = await EnsureBranchesAsync(context, logger);

        var darwin = branches.Single(b => b.Code == "DRW");
        var casuarina = branches.Single(b => b.Code == "CAS");
        var palmerston = branches.Single(b => b.Code == "PAL");

        await EnsureUserAsync(userManager, logger, "admin@library.test", "Alex Admin", Roles.Admin, darwin.Id);
        await EnsureUserAsync(userManager, logger, "reception@library.test", "Riley Reception", Roles.Reception, darwin.Id);
        await EnsureUserAsync(userManager, logger, "casuarina@library.test", "Cass Desk", Roles.Reception, casuarina.Id);
        await EnsureUserAsync(userManager, logger, "manager@library.test", "Morgan Manager", Roles.Manager, branchId: null);

        if (await context.Items.AnyAsync())
        {
            return;
        }

        await SeedCatalogueAsync(context, logger, darwin, casuarina, palmerston);
    }

    /// <summary>
    /// Stops the application with a readable explanation when it finds a database
    /// left over from Assignment Two.
    /// </summary>
    /// <remarks>
    /// <c>EnsureCreated</c> builds the schema only when there is no database at
    /// all. Pointed at an older one it does nothing and reports success, and the
    /// first query against a table that was added since fails somewhere deep in
    /// the stack with a message nobody can act on. Catching it here costs one
    /// query and turns a baffling run-time error into an instruction.
    ///
    /// A production system would use EF Core migrations instead, which upgrade an
    /// existing database in place. That is the right answer and the obvious next
    /// step; for an assignment that is marked by deleting the file and running
    /// again, this check is the honest trade.
    /// </remarks>
    private static async Task GuardAgainstStaleSchemaAsync(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            await context.Branches.AnyAsync();
        }
        catch (Exception ex)
        {
            var message =
                "The database on disk was built by an earlier version of this application and does " +
                "not have the branch, transfer, reservation or notification tables. Stop the " +
                "application, delete the .db file beside the project (library.db and any " +
                "library.db-wal / library.db-shm next to it), then run it again. It will rebuild " +
                "and reseed itself.";

            logger.LogCritical(ex, "{Message}", message);
            throw new InvalidOperationException(message, ex);
        }
    }

    /// <summary>
    /// Creates the three Northern Territory branches if they are not already
    /// there. Separate from the catalogue seed because branches must exist even
    /// when a database already holds stock.
    /// </summary>
    private static async Task<List<Branch>> EnsureBranchesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Branches.AnyAsync())
        {
            return await context.Branches.ToListAsync();
        }

        var branches = new List<Branch>
        {
            new()
            {
                Code = "DRW",
                Name = "Darwin City",
                Address = "Harry Chan Ave, Darwin NT 0800",
                Phone = "08 8930 0300",
                OpeningHours = "Mon-Fri 9am-6pm, Sat 10am-4pm"
            },
            new()
            {
                Code = "CAS",
                Name = "Casuarina",
                Address = "Bradshaw Tce, Casuarina NT 0810",
                Phone = "08 8930 0400",
                OpeningHours = "Mon-Fri 9am-5pm, Sat 10am-2pm"
            },
            new()
            {
                Code = "PAL",
                Name = "Palmerston",
                Address = "Civic Plaza, Palmerston NT 0830",
                Phone = "08 8930 0500",
                OpeningHours = "Mon-Fri 9am-5pm"
            }
        };

        context.Branches.AddRange(branches);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} branches.", branches.Count);
        return branches;
    }

    private static async Task SeedCatalogueAsync(ApplicationDbContext context, ILogger logger,
        Branch darwin, Branch casuarina, Branch palmerston)
    {
        var items = new List<Item>
        {
            NewBook(darwin, "BK-0001", "Clean Architecture", "A craftsman's guide to software structure and design.", "Robert C. Martin", "Computing", 2017, "9780134494166"),
            NewBook(darwin, "BK-0002", "The Dry", "A crime novel set in drought stricken rural Australia.", "Jane Harper", "Crime", 2016, "9781925266580"),
            NewBook(casuarina, "BK-0003", "Carpentaria", "An epic of the Gulf country told across generations.", "Alexis Wright", "Literary fiction", 2006, "9781920882174"),
            NewBook(casuarina, "BK-0004", "Storm Boy", "A boy and a pelican on the Coorong.", "Colin Thiele", "Children", 1964, "9780143307109"),
            NewBook(palmerston, "BK-0005", "Too Much Lip", "A sharp, funny novel about family and country.", "Melissa Lucashenko", "Literary fiction", 2018, "9780702259968"),

            NewMusic(darwin, "MU-0001", "Treaty", "The single that carried Yolngu language onto national radio.", "Yothu Yindi", 1991, "CD", "Mushroom"),
            NewMusic(casuarina, "MU-0002", "Diesel and Dust", "Recorded after the Blackfella/Whitefella tour.", "Midnight Oil", 1987, "Vinyl", "Sprint"),
            NewMusic(palmerston, "MU-0003", "The Four Seasons", "Four violin concertos, recorded by the Northern Chamber Orchestra.", "Antonio Vivaldi", 1725, "CD", "Naxos"),

            NewToy(darwin, "TY-0001", "Wooden train set", "Beech track, six carriages and a station.", "Construction", 3, 64),
            NewToy(casuarina, "TY-0002", "Scrabble", "Standard Australian edition.", "Board game", 8, 104),
            NewToy(palmerston, "TY-0003", "Microscope kit", "400x microscope with prepared slides.", "Science", 10, 22)
        };

        context.Items.AddRange(items);

        var borrowers = new List<Borrower>
        {
            NewBorrower(darwin, "M-1001", "Priya", "Nair", "priya.nair@example.com", "0400 111 222", "12 Mitchell St, Darwin NT 0800", 14, NotificationChannel.Email),
            NewBorrower(casuarina, "M-1002", "Tom", "Bakewell", "tom.bakewell@example.com", "0400 333 444", "5 Trower Rd, Casuarina NT 0810", 9, NotificationChannel.Sms),
            NewBorrower(casuarina, "M-1003", "Sione", "Tupou", "sione.tupou@example.com", "0400 555 666", "88 Bagot Rd, Millner NT 0810", 5, NotificationChannel.Email),
            NewBorrower(palmerston, "M-1004", "Grace", "O'Halloran", "grace.ohalloran@example.com", "0400 777 888", "2 Gardens Rd, The Gardens NT 0820", 2, NotificationChannel.Sms)
        };

        context.Borrowers.AddRange(borrowers);
        await context.SaveChangesAsync();

        var today = DateTime.Today;

        var loans = new List<Loan>
        {
            // Closed loans, returned on time.
            NewLoan(items[0], borrowers[0], darwin, today.AddDays(-120), returnedAfter: 9),
            NewLoan(items[5], borrowers[1], darwin, today.AddDays(-95), returnedAfter: 13),
            NewLoan(items[8], borrowers[2], darwin, today.AddDays(-80), returnedAfter: 6),
            NewLoan(items[1], borrowers[0], darwin, today.AddDays(-60), returnedAfter: 12),
            NewLoan(items[0], borrowers[3], darwin, today.AddDays(-45), returnedAfter: 10),
            NewLoan(items[9], borrowers[1], casuarina, today.AddDays(-33), returnedAfter: 8),

            // Closed loans, returned late with a fine.
            NewLoan(items[2], borrowers[1], casuarina, today.AddDays(-70), returnedAfter: 21, fine: 3.50m, finePaid: true),
            NewLoan(items[6], borrowers[2], casuarina, today.AddDays(-40), returnedAfter: 25, fine: 5.50m, finePaid: false),
            NewLoan(items[10], borrowers[0], palmerston, today.AddDays(-30), returnedAfter: 18, fine: 2.00m, finePaid: false),

            // Open loans: one comfortably inside the period, two overdue.
            NewLoan(items[3], borrowers[3], casuarina, today.AddDays(-4)),
            NewLoan(items[7], borrowers[1], palmerston, today.AddDays(-25)),
            NewLoan(items[4], borrowers[2], palmerston, today.AddDays(-19))
        };

        // Items that are still out are not on the shelf.
        items[3].Status = ItemStatus.Borrowed;
        items[7].Status = ItemStatus.Borrowed;
        items[4].Status = ItemStatus.Borrowed;
        // One item came back in poor condition and is waiting on repair.
        items[10].Status = ItemStatus.Damaged;

        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();

        await SeedReservationsAsync(context, items, borrowers, today);
        await SeedTransferAsync(context, items, darwin, casuarina, today);
        await SeedNotificationsAsync(context, items, borrowers, today);

        logger.LogInformation("Seeded {Items} items, {Borrowers} members and {Loans} loans across 3 branches.",
            items.Count, borrowers.Count, loans.Count);
    }

    /// <summary>
    /// Two people waiting on the one borrowed book, so the queue screen and the
    /// "next in line" logic both have something real to show.
    /// </summary>
    private static async Task SeedReservationsAsync(ApplicationDbContext context,
        List<Item> items, List<Borrower> borrowers, DateTime today)
    {
        context.Reservations.AddRange(
            new Reservation
            {
                ItemId = items[3].Id,
                BorrowerId = borrowers[0].Id,
                CollectionBranchId = borrowers[0].HomeBranchId,
                PlacedOn = today.AddDays(-3),
                Status = ReservationStatus.Waiting
            },
            new Reservation
            {
                ItemId = items[3].Id,
                BorrowerId = borrowers[2].Id,
                CollectionBranchId = borrowers[2].HomeBranchId,
                PlacedOn = today.AddDays(-1),
                Status = ReservationStatus.Waiting
            },
            new Reservation
            {
                ItemId = items[10].Id,
                BorrowerId = borrowers[1].Id,
                CollectionBranchId = borrowers[1].HomeBranchId,
                PlacedOn = today.AddDays(-6),
                Status = ReservationStatus.Waiting,
                Notes = "Waiting on repair."
            });

        await context.SaveChangesAsync();
    }

    /// <summary>One transfer already on the road, so the transfers desk is not empty.</summary>
    private static async Task SeedTransferAsync(ApplicationDbContext context,
        List<Item> items, Branch darwin, Branch casuarina, DateTime today)
    {
        items[9].Status = ItemStatus.InTransit;

        context.Transfers.Add(new Transfer
        {
            ItemId = items[9].Id,
            FromBranchId = casuarina.Id,
            ToBranchId = darwin.Id,
            Status = TransferStatus.InTransit,
            RequestedOn = today.AddDays(-2),
            DispatchedOn = today.AddDays(-1),
            Reason = "Requested for a school holiday games session."
        });

        await context.SaveChangesAsync();
    }

    /// <summary>A short message history so the notification dashboard opens with data.</summary>
    private static async Task SeedNotificationsAsync(ApplicationDbContext context,
        List<Item> items, List<Borrower> borrowers, DateTime today)
    {
        context.Notifications.AddRange(
            new Notification
            {
                BorrowerId = borrowers[3].Id,
                ItemId = items[3].Id,
                Kind = NotificationKind.Borrowed,
                Channel = NotificationChannel.Sms,
                Status = NotificationStatus.Sent,
                Recipient = borrowers[3].Phone!,
                Subject = "Item borrowed",
                Body = $"You borrowed {items[3].Name}. It is due back on {today.AddDays(10):d MMM yyyy}.",
                CreatedOn = today.AddDays(-4),
                SentOn = today.AddDays(-4)
            },
            new Notification
            {
                BorrowerId = borrowers[1].Id,
                ItemId = items[7].Id,
                Kind = NotificationKind.Overdue,
                Channel = NotificationChannel.Sms,
                Status = NotificationStatus.Sent,
                Recipient = borrowers[1].Phone!,
                Subject = "Item overdue",
                Body = $"{items[7].Name} was due on {today.AddDays(-11):d MMM yyyy}. Fines are accruing daily.",
                CreatedOn = today.AddDays(-1),
                SentOn = today.AddDays(-1)
            },
            new Notification
            {
                BorrowerId = borrowers[2].Id,
                ItemId = items[6].Id,
                Kind = NotificationKind.FineCharged,
                Channel = NotificationChannel.Email,
                Status = NotificationStatus.Sent,
                Recipient = borrowers[2].Email!,
                Subject = "Fine charged",
                Body = $"A fine of {5.50m:C} has been charged for the late return of {items[6].Name}.",
                CreatedOn = today.AddDays(-15),
                SentOn = today.AddDays(-15)
            });

        await context.SaveChangesAsync();
    }

    // ---------------------------------------------------------------------
    // Builders. Each one fills in the branch columns every item and member now
    // needs, so the sample data above stays readable.
    // ---------------------------------------------------------------------

    private static Book NewBook(Branch branch, string code, string name, string description,
        string author, string genre, int year, string isbn) => new()
        {
            LibraryCode = code,
            Name = name,
            Description = description,
            Author = author,
            Genre = genre,
            YearPublished = year,
            Isbn = isbn,
            BranchId = branch.Id,
            OwningBranchId = branch.Id,
            Branch = branch,
            OwningBranch = branch
        };

    private static MusicRecording NewMusic(Branch branch, string code, string name, string description,
        string artist, int year, string format, string label) => new()
        {
            LibraryCode = code,
            Name = name,
            Description = description,
            Artist = artist,
            YearReleased = year,
            Format = format,
            Label = label,
            BranchId = branch.Id,
            OwningBranchId = branch.Id,
            Branch = branch,
            OwningBranch = branch
        };

    private static Toy NewToy(Branch branch, string code, string name, string description,
        string toyType, int minimumAge, int pieces) => new()
        {
            LibraryCode = code,
            Name = name,
            Description = description,
            ToyType = toyType,
            MinimumAge = minimumAge,
            PieceCount = pieces,
            BranchId = branch.Id,
            OwningBranchId = branch.Id,
            Branch = branch,
            OwningBranch = branch
        };

    private static Borrower NewBorrower(Branch branch, string membership, string first, string last,
        string email, string phone, string address, int monthsAgo, NotificationChannel channel) => new()
        {
            MembershipNumber = membership,
            FirstName = first,
            LastName = last,
            Email = email,
            Phone = phone,
            Address = address,
            JoinedOn = DateTime.Today.AddMonths(-monthsAgo),
            HomeBranchId = branch.Id,
            HomeBranch = branch,
            PreferredChannel = channel
        };

    private static Loan NewLoan(Item item, Borrower borrower, Branch branch, DateTime borrowedOn,
        int? returnedAfter = null, decimal fine = 0m, bool finePaid = false, int periodDays = 14) => new()
        {
            Item = item,
            Borrower = borrower,
            BranchId = branch.Id,
            ReturnBranchId = returnedAfter.HasValue ? branch.Id : null,
            BorrowedOn = borrowedOn,
            DueOn = borrowedOn.AddDays(periodDays),
            ReturnedOn = returnedAfter.HasValue ? borrowedOn.AddDays(returnedAfter.Value) : null,
            FineAmount = fine,
            FinePaid = finePaid
        };

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, ILogger logger,
        string email, string fullName, string role, int? branchId)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                BranchId = branchId
            };

            var created = await userManager.CreateAsync(user, DefaultPassword);
            if (!created.Succeeded)
            {
                logger.LogError("Could not create {Email}: {Errors}", email,
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Created the {Role} account {Email}.", role, email);
        }
        else
        {
            // The account survived from an earlier run. Put the password back to the
            // documented one and clear any lockout, so the demo logins in the README
            // always work no matter what state the database was left in.
            if (!await userManager.CheckPasswordAsync(user, DefaultPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await userManager.ResetPasswordAsync(user, token, DefaultPassword);

                logger.LogWarning("Reset the password for {Email}: {Outcome}", email,
                    reset.Succeeded ? "done" : string.Join("; ", reset.Errors.Select(e => e.Description)));
            }

            if (user.BranchId != branchId)
            {
                user.BranchId = branchId;
                await userManager.UpdateAsync(user);
            }

            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Put {Email} into the {Role} role.", email, role);
        }
    }
}
