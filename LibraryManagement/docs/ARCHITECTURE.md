# Architecture

How the expanded system fits together, and why it is shaped this way.

This document covers what the group project added. The reasoning behind the original
single-site design — the item hierarchy, dependency inversion, the service layer — is in
[DESIGN-OVERVIEW.md](DESIGN-OVERVIEW.md) and still holds.

---

## 1. The shape of the expansion

Assignment Two was one library. The brief for this project asked for breadth rather than
depth: more channels, more integrations, more functional domains. The existing layering
absorbed that without being rebuilt, which is the main thing worth reporting.

```
          ┌──────────────┐  ┌──────────┐  ┌───────────┐  ┌─────────────┐
 Channels │ Public portal│  │  Kiosk   │  │Staff views│  │ Developer   │
          │  (responsive)│  │ (touch)  │  │  (MVC)    │  │ API (JSON)  │
          └───────┬──────┘  └─────┬────┘  └─────┬─────┘  └──────┬──────┘
                  └───────────────┴─────────────┴───────────────┘
                                        │
                  ┌─────────────────────▼─────────────────────┐
   Controllers    │  Catalogue  Kiosk  Items  Circulation      │
                  │  Branches   Transfers  Reservations        │
                  │  Notifications  Import  Manager  Api       │
                  └─────────────────────┬─────────────────────┘
                                        │  (interfaces only)
                  ┌─────────────────────▼─────────────────────┐
   Services       │  IItemService      IBranchService          │
                  │  ILoanService      ITransferService        │
                  │  IBorrowerService  IReservationService     │
                  │  IStatisticsService INotificationService   │
                  │  IImportService    IReportExportService    │
                  │                                            │
                  │  supporting: IFinePolicy  IClock           │
                  │              IBranchContext IMetadataProvider│
                  │              INotificationChannel (xN)     │
                  └─────────────────────┬─────────────────────┘
                                        │
                  ┌─────────────────────▼─────────────────────┐
   Data           │        ApplicationDbContext (EF Core)      │
                  └─────────────────────┬─────────────────────┘
                                        │
                                   ┌────▼────┐
                                   │ SQLite  │
                                   └─────────┘
```

Four channels now sit on top of one service layer. None of them duplicates a rule: the
kiosk, the front desk and the API all ask the same `ILoanService` whether an item may go
out, so they cannot disagree about it.

---

## 2. What the data model gained

```
                    ┌──────────┐
          ┌─────────┤  Branch  ├─────────┐
          │         └────┬─────┘         │
     held at / owned by  │          home branch
          │              │               │
    ┌─────▼─────┐        │         ┌─────▼──────┐
    │   Item    │        │         │  Borrower  │
    │ (abstract)│        │         └─────┬──────┘
    └─────┬─────┘        │               │
   ┌──────┼──────┐       │               │
   │      │      │       │               │
 Book   Music   Toy      │               │
   │      │      │       │               │
   └──────┴──────┘       │               │
          │              │               │
          ├──────────────┼───────────────┤
          │              │               │
    ┌─────▼─────┐  ┌─────▼─────┐  ┌──────▼───────┐
    │   Loan    │  │ Transfer  │  │ Reservation  │
    └───────────┘  └─────┬─────┘  └──────┬───────┘
                         └───────────────┘
                          (a transfer may
                           satisfy a hold)
          │
    ┌─────▼────────┐
    │ Notification │
    └──────────────┘
```

Four new entities. Three decisions in there are worth defending.

**An item has two branches, not one.** `BranchId` is where it physically is;
`OwningBranchId` is whose stock it is. Without the second, an item lent to another branch
for a month would vanish from its home branch's inventory and the figures would quietly
stop adding up. With it, the manager's inventory report can show both "what is on my
shelves" and "what I own that is elsewhere", which is what a real network tracks.

**A loan records where it happened.** `Loan.BranchId` is stamped at lending time rather
than read from the item later, because the item may have moved on by the time anyone runs
the report. Per-branch borrowing figures have to credit the branch that did the work.
`ReturnBranchId` is separate because an item can come back anywhere.

**Queue position is not stored.** A `Reservation` has `PlacedOn` and the queue is ordered
by it. Storing a position number would mean renumbering every row below whenever somebody
cancels, and one missed update would leave two people holding the same place. Deriving it
cannot drift.

---

## 3. Module by module

### Multi-branch inventory

`Branch` is the new root. `IBranchContext` answers "which branch are we acting as right
now" from, in order: the deployment setting, then the signed-in user's own desk. That one
abstraction is what lets the same build run at every location with a one-line
configuration difference, which is what the installation instructions depend on.

Deliberately, **all branches share one database**. Splitting the data per site would make
an inter-branch transfer impossible, because neither instance could see the other's stock.
What differs per branch is configuration, not data.

### Inter-branch transfers

A three-step handshake: the destination requests, the holder dispatches, the destination
receives.

```
  Requested ──dispatch──► InTransit ──receive──► Received
      │
   cancel
      │
      ▼
  Cancelled
```

The item's location changes **only on receipt**. Between dispatch and receipt the item sits
in `ItemStatus.InTransit` and still reads as belonging to the sending branch, so two desks
can never both believe they have it. Cancelling after dispatch is refused, because an item
already on the road has to arrive somewhere.

### Reservations and waitlist

`PromoteNextAsync` is the heart of it, and it is called from `LoanService.ReturnAsync`
rather than from a controller. That placement matters: a returned item becomes available
inside the return transaction, so the promotion happens there too, and there is no window
where a reserved item sits on the open shelf for somebody else to walk off with.

```
   Waiting ──item comes back──► Ready ──borrowed──► Collected
      │                           │
   cancel                    not collected in time
      │                           │
      ▼                           ▼
  Cancelled                    Expired ──► next in queue promoted
```

### Notifications

The clearest dependency inversion in the expansion, and a direct application of the unit's
DIP material.

```
   LoanService ─┐
   ReservationService ─┼──► INotificationService ──► INotificationChannel
   TransferService ─┘            │                      ▲        ▲
                                 │                      │        │
                          NotificationComposer    Simulated  Simulated
                          (what to say)             Email      SMS
```

Three services raise events. None of them knows whether a message goes by email, by SMS or
nowhere at all. Both channels are registered against the same interface and
`NotificationService` receives the whole collection, picking one at run time by the
member's preference and falling back when the preferred channel has no contact details.
Adding a push channel is one class and one line in `Program.cs`.

`NotificationComposer` is separate and static: pure, no database, no transport, so the
wording can be reviewed or tested on its own.

A failed send never rolls back the transaction that triggered it. A bounced message is an
ordinary event that belongs in the log, not an exception that should abandon somebody's
loan.

### Bulk import

```
   Upload ──► PreviewCsvAsync ──► staff review ──► CommitAsync ──► catalogue
              (reads, judges,      (every row,     (writes only
               writes nothing)      with verdict)   valid rows)
```

Two passes on purpose. A bulk tool that writes first and reports afterwards leaves somebody
to clean up by hand. Every row is validated for all its faults at once, not just the first,
so a file can be fixed in one pass.

`IMetadataProvider` abstracts the outside catalogue. The mock answers from memory, which
keeps the system runnable on a marker's virtual machine with no internet; a real Open
Library or Trove lookup is another implementation of the same interface.

### Report exports

Each report is gathered once into a plain table of rows, and both the CSV writer and the
PDF renderer format that same table. The two formats therefore cannot disagree, which they
could if each ran its own queries.

---

## 4. Four channels, one rule set

This is the part of the expansion most at risk of going wrong, so it is worth being
explicit about how it was avoided.

| Channel | Audience | Authentication | Writes? |
|---|---|---|---|
| Public portal | Anyone, mostly on a phone | None | No |
| Kiosk | Patron standing at a terminal | Library card number | Borrowing only |
| Staff MVC | Trained staff | Identity + role | Yes |
| Developer API | Outside applications | None | No |

Each has its own controller and its own vocabulary, but none has its own copy of a rule.
The kiosk's `Borrow` action calls the same `ILoanService.BorrowAsync` the front desk calls.
Where the kiosk differs is only in presentation: it softens the handful of staff-facing
refusals that would alarm somebody standing alone at a machine, and sends them to the desk
instead.

The kiosk is a separate controller rather than a skin over the reception screens because
the audience differs in a way that changes every decision. A patron has no account, no
training and no mouse. They have a card, an item and about thirty seconds.

---

## 5. Security carried forward

Everything from Assignment Two still applies: role attributes at class level, antiforgery
tokens on every POST, Razor output encoding, parameterised queries throughout, local-only
redirects.

What the expansion added:

| Concern | How it is handled |
|---|---|
| Public API leaking member data | Purpose-built contract objects; no entity is ever serialised |
| API being used to write | No write endpoints exist to call |
| Kiosk reaching staff screens | Separate controller and layout; no staff route is linked or reachable |
| Kiosk exposing other members | Only the card presented is ever read |
| CSV upload abuse | 2 MB cap, extension check, parsed into a preview before anything is written |
| Branch scoping bypass | `LockToBranch` fixes a terminal to its site regardless of the signed-in user |

---

## 6. What we would do next

Honest about the edges, since the brief asked for breadth and breadth has costs.

- **EF Core migrations.** The system uses `EnsureCreated`, which cannot upgrade an existing
  database. A guard detects a stale schema and explains what to do, but migrations are the
  right answer and the first thing to add.
- **A real scheduler.** The due-date sweep and hold expiry are buttons standing in for a
  nightly job. A hosted background service would be the production shape.
- **Automated tests.** The service layer was built to be testable — the context can be
  swapped for the in-memory provider and `IClock` for a fixed date — but no tests ship with
  this.
- **Push statistics into SQL.** The dashboard aggregates in memory, which is fine at branch
  scale and would not be for a consortium.
- **Rate limiting and caching on the API.** Listed in [API.md](API.md) as known limits.
