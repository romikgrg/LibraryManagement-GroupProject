# Darwin Community Library — multi-branch edition

A library management system built on ASP.NET Core MVC 10, Entity Framework Core and
ASP.NET Core Identity. Assignment Two delivered a single-site system; this version expands
it into a branch network with holds, transfers, notifications, bulk import, a public API
and a self-service kiosk.

Full documentation lives in [`docs/`](docs/):

| Document | What it covers |
|---|---|
| [INSTALL.md](docs/INSTALL.md) | Getting it running, including per-branch deployment. **Test this on the VM.** |
| [USER-GUIDE.md](docs/USER-GUIDE.md) | How each role uses the system, with a walkthrough |
| [API.md](docs/API.md) | The public developer API |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | How the modules fit together, with diagrams |
| [DESIGN-OVERVIEW.md](docs/DESIGN-OVERVIEW.md) | Design reasoning carried forward from Assignment Two |
| [PORTFOLIO.md](docs/PORTFOLIO.md) | Project management skeleton — **the team fills this in** |

## Quick start

```bash
git clone <repository-url>
cd LibraryManagement
dotnet run --project src/LibraryManagement
```

Then open <https://localhost:7141>. The database is created and seeded on first run.

> **Upgrading from Assignment Two?** Delete `library.db` (and any `library.db-wal` /
> `library.db-shm` beside it) first. The schema has changed and the application will stop
> with an explanation rather than fail halfway through.

### Running as a particular branch

The same build runs at every location. One setting decides which:

```bash
# Head office: sees the whole network
dotnet run --project src/LibraryManagement

# A branch terminal
dotnet run --project src/LibraryManagement --launch-profile "Casuarina branch"
```

Every branch shares one database. A network where each site had its own store could never
transfer an item between sites, because neither instance would be able to see the other's
stock. [INSTALL.md](docs/INSTALL.md) has the full detail, including server deployment.

### Sign in details

| Role | Email | Password | Branch |
|---|---|---|---|
| Admin | admin@library.test | Library@2026 | Darwin City |
| Reception | reception@library.test | Library@2026 | Darwin City |
| Reception | casuarina@library.test | Library@2026 | Casuarina |
| Manager | manager@library.test | Library@2026 | All branches |

## The modules

| Module | Where to find it | Code |
|---|---|---|
| Multi-branch inventory | Admin → Branches | `Models/Branch.cs`, `Services/BranchService.cs` |
| Inter-branch transfers | Reception → Transfers | `Models/Transfer.cs`, `Services/TransferService.cs` |
| Reservations and waitlist | Reception → Holds | `Models/Reservation.cs`, `Services/ReservationService.cs` |
| Notifications | Reception → Notices | `Services/Notifications/` |
| Bulk import | Admin → Import | `Services/Import/` |
| Report exports | Manager → Reports | `Services/Reporting/` |
| Public API | `/api/v1/items` | `Controllers/Api/` |
| Self-service kiosk | `/Kiosk` | `Controllers/KioskController.cs` |

## Project layout

```
src/LibraryManagement
├── Controllers
│   └── Api              The public read-only REST API
├── Data                 DbContext and first-run seeding
├── Models
│   ├── Api              Public API contracts
│   └── ViewModels       The shapes the views need
├── Services
│   ├── Import           CSV and external metadata loading
│   ├── Notifications    Channels, composer and the send log
│   └── Reporting        CSV and PDF export
├── Views                One folder per controller
└── wwwroot              Stylesheets and the one small script
```

## Notes

- Bootstrap, jQuery and the validation scripts load from a CDN, so the first run needs an
  internet connection. To work offline, download them into `wwwroot/lib` and point the
  script blocks at the local copies.
- `library.db` is created at runtime and is not committed.
- QuestPDF is used under its Community licence, declared in `Program.cs`.
