# Design overview

Darwin Community Library, Assignment Two.

## 1. What the system is for

A branch library lends books, music and toys. Four groups of people use the system and they
want different things from it:

- **The public** want to know whether something is on the shelf, usually on a phone, standing
  in the aisle or on the bus. They should not have to sign in to find out.
- **Reception** work at a counter with a queue in front of them. Speed matters more than
  anything else: type a code, pick a member, done.
- **The admin** maintains the collection. They work in bursts when a delivery arrives.
- **The manager** wants figures, not screens full of buttons.

Those four jobs became four controllers, and the differences between them drive most of the
decisions below.

## 2. Architecture

The application follows the Model-View-Controller pattern that ASP.NET Core provides, with a
service layer between the controllers and Entity Framework.

```
Browser
   │  HTTP
   ▼
Controllers ──────► Services (interfaces) ──────► ApplicationDbContext ──► SQLite
   │                      ▲
   ▼                      │
Razor views          Fine policy, clock
```

Responsibilities are split like this:

| Layer | Does | Does not |
|-------|------|----------|
| Controller | Reads input, calls one service, picks a view | Contain rules or touch the database |
| Service | Owns the rules, talks to the context | Know about HTTP, ModelState or TempData |
| Model | Holds data and the behaviour that belongs to it | Know about storage or the web |
| View | Renders a view model | Make decisions or query anything |

The test for whether a piece of logic is in the right place: if the library replaced the web
front end with a desktop application tomorrow, the services would move across untouched. That
is why "can this item be borrowed" lives in `LoanService` and not in `CirculationController`.

## 3. The item hierarchy

The brief asks for an item superclass inherited by three classes.

```
            Item (abstract)
   Id, LibraryCode, Name, Description,
   Status, AddedOn, Loans
   abstract ItemType, abstract Summary
                  │
    ┌─────────────┼─────────────┐
  Book      MusicRecording      Toy
 Author         Artist        ToyType
 Genre       YearReleased    MinimumAge
 YearPublished  Format       PieceCount
 Isbn           Label
```

`Item` is abstract because a bare item is not a thing the library owns. Two members are
abstract rather than virtual with a default:

- `ItemType` gives each subclass its label. Views group and filter by it without asking what
  the runtime type is.
- `Summary` is the one line of type specific detail shown in search results. A book returns
  "by Jane Harper - Crime - 2016"; a toy returns "Board game - ages 8+ - 104 pieces".

That second one is the point of the inheritance. The catalogue view renders `@item.Summary`
once. Adding jigsaw puzzles as a fourth type means writing a new class and no change at all to
the search page, the admin list, or the loan logic. Without polymorphism, every one of those
places would need another branch in an `if` chain, and each new type would mean finding all of
them.

Entity Framework maps all three to a single `Items` table using Table Per Hierarchy, with a
discriminator column named `ItemKind`. A separate table per subclass was the alternative; it
was rejected because the three types share almost every column, loans point at items
generically, and the front desk's most common query - find one item by its code - would have
become a three way union.

## 4. Dependency inversion

The unit's Dependency Inversion Principle states that high level modules should not depend on
low level modules; both should depend on abstractions. The clearest example here is fines.

Calculating a fine is a detail. It is also the thing most likely to change: a new council
policy, an amnesty week, a concession rate for students. Lending, by contrast, is the stable
high level policy. So `LoanService` depends on `IFinePolicy`:

```csharp
public LoanService(ApplicationDbContext context, IFinePolicy finePolicy,
                   IClock clock, IOptions<LoanRules> rules, ILogger<LoanService> logger)
```

and asks it a question rather than doing arithmetic:

```csharp
loan.FineAmount = _finePolicy.CalculateFine(loan.DueOn, today);
```

`DailyRateFinePolicy` implements it with a grace period, a daily rate and a cap, all read from
`appsettings.json` through `IOptions<LoanRules>`. Changing the rate from 50c to 75c is an edit
to a configuration file. Changing to a flat fee is a new class and one line in `Program.cs`:

```csharp
builder.Services.AddSingleton<IFinePolicy, FlatFeeFinePolicy>();
```

`LoanService` does not change, does not recompile differently, and does not need retesting.

`IClock` exists for the same reason. Fines depend on today's date, and `DateTime.Today` is
impossible to control from a test. With the clock behind an interface, a test can park time on
a chosen date, run a return, and assert the fine that comes out. The container is wired in
`Program.cs`, where every service is registered against its interface:

| Registration | Lifetime | Why |
|--------------|----------|-----|
| `IClock` | Singleton | Stateless, no dependencies |
| `IFinePolicy` | Singleton | Reads configuration once |
| `IItemService`, `IBorrowerService`, `ILoanService`, `IStatisticsService` | Scoped | They use the DbContext, which is scoped to the request |

Registering a service that depends on the DbContext as a singleton would capture a disposed
context on the second request, which is why the lifetimes differ.

## 5. Guarding the rules

The business rules the system enforces, and where they live:

| Rule | Enforced in |
|------|-------------|
| Library codes and membership numbers are unique | `ItemService`, `BorrowerService`, plus a unique index |
| Only an available item can be borrowed | `LoanService.BorrowAsync` |
| An item cannot be lent twice at once | `LoanService.BorrowAsync`, checked against the loan table |
| A suspended member cannot borrow | `LoanService.BorrowAsync` |
| Five items per member | `LoanRules.MaximumOpenLoans` |
| Fines over the limit block borrowing | `LoanRules.FineLimitBeforeBlock` |
| Items and members with history cannot be deleted | `ItemService`, `BorrowerService` |
| An item on loan cannot be edited back to available | `ItemService.UpdateAsync` |

Two deliberate choices are worth pointing out.

**Loans are never deleted.** They are the borrowing history the manager's reports are built
from. Deleting an item that has been borrowed would silently change last month's figures, so
the service refuses and suggests marking it destroyed instead. The foreign keys use
`DeleteBehavior.Restrict` so the database enforces this too, not just the C#.

**The status column is a convenience, not the truth.** Whether an item is out is decided by
whether an open loan row exists. `BorrowAsync` checks both, because a status column can drift
after a crash but a row either exists or it does not.

## 6. Talking back to the user

Services return a `ServiceResult` rather than throwing exceptions for ordinary refusals:

```csharp
return ServiceResult<Loan>.Fail($"{item.Name} cannot be borrowed because it is marked as {item.Status}.");
```

A member owing a fine is not an exceptional condition; it is Tuesday. Exceptions are reserved
for genuine faults. The controller turns the result into either a `ModelState` error, so the
message appears above the form with what was typed still in place, or a `TempData` message
that survives the redirect after a successful post. Both patterns come from the Sessions
lecture: `TempData` lives for exactly one further request, which is what a redirect needs.

Session state itself is used for one thing: the public portal remembers the last five searches
so a visitor can step back to what they tried a minute ago. It holds nothing sensitive, which
matches the warning in the unit material that session cookies can outlive the person who
started them.

## 7. Security

| Threat | Handled by |
|--------|------------|
| Anyone reaching staff screens | `[Authorize(Roles = ...)]` on each controller class, not per action |
| Cross site request forgery | `@Html.AntiForgeryToken()` on every form, `[ValidateAntiForgeryToken]` on every POST |
| Cross site scripting | Razor encodes all output; `Html.Raw` is never used |
| SQL injection | Entity Framework parameterises everything; no raw SQL anywhere |
| Open redirect | `Url.IsLocalUrl` checked before the one redirect that takes a URL |
| Weak passwords, brute force | Identity: minimum eight characters, non alphanumeric required, lockout after five attempts |
| Leaking detail in errors | Developer page in development only; `UseExceptionHandler` and `UseStatusCodePagesWithReExecute` in production |

Authorisation is applied at the class level so that adding an action to `ItemsController` next
year cannot accidentally create an unprotected endpoint. The `Roles` class holds the three role
names as constants, so an attribute and the seeder can never disagree through a typo.

## 8. The public portal on a phone

The brief asks for a portal the public can search from their phone. The catalogue page is
built mobile first:

- One Bootstrap grid, `row-cols-1 row-cols-md-2 row-cols-xl-3`, so results are a single column
  of cards on a phone and three across on a desktop.
- Results are cards rather than a table, because a table with six columns is unusable at 375px.
- The search box is `form-control-lg` so it is a comfortable tap target.
- The navigation collapses behind a labelled toggle.
- Staff tables that do have to be tables are wrapped in `table-responsive`, which scrolls
  sideways rather than breaking the layout.

Nothing on this page requires a sign in, and nothing on it exposes member details.

## 9. Assumptions

1. One physical copy per library code. Two copies of the same book are two items with two
   codes, which is how a small branch library actually labels its stock.
2. Reception takes fine payments at the counter. There is no online payment.
3. Fines are recorded against the loan, not carried as a running member balance, so the history
   shows exactly which item caused which charge.
4. A returned item goes straight back on the shelf unless staff say it is damaged. There is no
   separate reshelving state.
5. Staff accounts are created by seeding. In a real deployment the manager would create them,
   which would be the obvious next feature.

## 10. If this were built out further

- Reservations and a waiting list, which is the first thing members ask for.
- Email notices before an item falls due, using `IEmailSender`, which Identity already expects.
- Automated tests. The service layer was written to be testable - the DbContext can be swapped
  for the in memory provider and `IClock` for a fixed date - but no tests are included here.
- Pushing the statistics grouping into SQL once the collection outgrows a single branch.

## 11. Use of generative AI

This section must be completed before submission, in line with the unit's requirement that all
AI use is referenced.

State which tool was used, what it was used for, and what you changed afterwards. For example:

> Claude (Anthropic, model version, date accessed) was used to draft the initial project
> structure and the first version of the service classes. I reviewed and modified [name the
> specific files and changes], and wrote [name what is yours] myself. All code was read,
> tested and understood before submission.

Adjust that to match what you actually did, and check it against the CDU referencing guidance
your lecturer linked in the Artificial Intelligence lecture.
