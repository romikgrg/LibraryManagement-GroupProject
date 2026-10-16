# Installation and running instructions

Everything needed to get the Darwin Community Library system running, on a developer
machine or at a branch.

> **Before you submit:** follow these instructions yourself on a clean virtual machine,
> from the clone onwards, with nobody helping. If a step does not work exactly as written,
> fix the step, not your own machine.

---

## 1. What you need

| Requirement | Version | Check it with |
|---|---|---|
| .NET SDK | 10.0 or later | `dotnet --version` |
| Git | any recent | `git --version` |
| A browser | any modern | |

Visual Studio 2022 (17.12+) or Visual Studio Code are optional. The commands below work
from any terminal without an IDE.

No database server is needed. The system uses SQLite, which is a single file created
automatically on first run. There is nothing to install, configure or start.

### If you do not have .NET 10

Download it from <https://dotnet.microsoft.com/download/dotnet/10.0> and install the SDK
(not just the runtime). Close and reopen your terminal afterwards so `dotnet` is on the
path.

---

## 2. Get the code

```bash
git clone <repository-url>
cd LibraryManagement
```

Replace `<repository-url>` with the address from the repository page.

To check you have the right place:

```bash
ls
# You should see: LibraryManagement.sln  README.md  docs  src
```

---

## 3. Run it

```bash
dotnet run --project src/LibraryManagement
```

The first run restores packages and builds, which takes a minute or two. When it is ready
the terminal prints:

```
Now listening on: https://localhost:7141
Now listening on: http://localhost:5141
Application started. Press Ctrl+C to shut down.
```

**Open <https://localhost:7141> in your browser.** The terminal does not open it for you.

The application stays running until you press `Ctrl+C`. That is normal; a web server does
not return you to the prompt.

### If the browser warns about the certificate

Run this once, accept the prompt, then start the application again:

```bash
dotnet dev-certs https --trust
```

Or use the plain HTTP address, <http://localhost:5141>, instead.

### What happens on first run

The database file is created and filled with sample data: three branches, eleven items
spread across them, four members, a borrowing history with fines, a queue of holds, one
transfer already in progress and a short notification log. Every screen has something on it
straight away.

---

## 4. Running as a particular branch

The same build runs at every location in the network. One setting decides which branch an
instance is serving. That setting changes the branding, pre-selects the branch on every
staff form, and scopes the front desk to that site's own queues.

**Every branch shares one database.** This is deliberate. An inter-branch transfer cannot
work if the two sites cannot see each other's stock, so splitting the data per branch would
break the feature the brief asks for. What differs per branch is the configuration, not the
data.

### From a terminal, using the launch profiles

```bash
# Head office — the whole network is visible
dotnet run --project src/LibraryManagement --launch-profile "Head office (all branches)"

# Darwin City branch                    https://localhost:7142
dotnet run --project src/LibraryManagement --launch-profile "Darwin City branch"

# Casuarina branch                      https://localhost:7143
dotnet run --project src/LibraryManagement --launch-profile "Casuarina branch"

# Palmerston branch                     https://localhost:7144
dotnet run --project src/LibraryManagement --launch-profile "Palmerston branch"
```

Each profile listens on its own port, so you can run several at once in separate terminals
and watch a transfer move between them.

### From Visual Studio

The same four profiles appear in the dropdown beside the green run button. Choose one and
press F5.

### On a real server

Set the environment to the branch name. The matching `appsettings.<Branch>.json` is then
picked up automatically:

```bash
# Linux / macOS
export ASPNETCORE_ENVIRONMENT=Casuarina
dotnet src/LibraryManagement/bin/Release/net10.0/LibraryManagement.dll

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Casuarina"
dotnet .\src\LibraryManagement\bin\Release\net10.0\LibraryManagement.dll
```

Or override the single setting without any file at all:

```bash
dotnet run --project src/LibraryManagement --BranchDeployment:Code=PAL
```

### Adding a new branch location

1. Sign in as the admin and add the branch under **Branches**, giving it a short code.
2. Copy `src/LibraryManagement/appsettings.Casuarina.json` to
   `appsettings.<YourBranch>.json` and change `Code` to the new branch code.
3. Add a matching profile to `Properties/launchSettings.json` with a free port.

No code changes are needed.

### Using git branches per location

If your deployment process prefers a branch per location, create one from `main` with only
the location's `appsettings` file changed:

```bash
git checkout -b deploy/casuarina
# edit appsettings.json so BranchDeployment:Code is CAS
git commit -am "Casuarina deployment settings"
```

Each site then clones the repository and checks out its own branch, and these instructions
work unchanged from step 2 onwards. Keep the difference between those branches to
configuration only, and merge `main` into each one when the application changes.

---

## 5. Signing in

| Role | Email | Password | Works at |
|---|---|---|---|
| Admin | `admin@library.test` | `Library@2026` | Darwin City |
| Reception | `reception@library.test` | `Library@2026` | Darwin City |
| Reception | `casuarina@library.test` | `Library@2026` | Casuarina |
| Manager | `manager@library.test` | `Library@2026` | All branches |

Each account holds exactly one role, which is the quickest way to show the access rules
working. Signing in as the manager and opening `/Items` produces the access denied page.

The public catalogue and the kiosk need no sign in at all.

---

## 6. Starting over

Stop the application with `Ctrl+C`, delete the database, and run again:

```bash
rm src/LibraryManagement/library.db*          # Linux / macOS
del src\LibraryManagement\library.db*         # Windows
dotnet run --project src/LibraryManagement
```

It rebuilds and reseeds itself.

---

## 7. Configuration reference

All settings live in `src/LibraryManagement/appsettings.json`.

| Setting | Default | What it does |
|---|---|---|
| `BranchDeployment:Code` | `ALL` | Branch code this instance serves, or `ALL` for head office |
| `BranchDeployment:LockToBranch` | `false` | Lock staff screens to that branch |
| `LoanRules:LoanPeriodDays` | `14` | How long an item can be kept |
| `LoanRules:DailyFineRate` | `0.50` | Charged per day late |
| `LoanRules:MaximumFine` | `20.00` | Cap on a single fine |
| `LoanRules:GraceDays` | `0` | Days of lateness forgiven |
| `LoanRules:MaximumOpenLoans` | `5` | Items one member may hold |
| `LoanRules:FineLimitBeforeBlock` | `10.00` | Unpaid total that blocks borrowing |
| `Notifications:Enabled` | `true` | Master switch for all messages |
| `Notifications:DueSoonDays` | `3` | How early a reminder goes out |
| `Notifications:HoldShelfDays` | `7` | How long a hold waits to be collected |

### Using SQL Server instead of SQLite

Two changes. In `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LibraryManagement;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

And in `Program.cs`, replace `options.UseSqlite(connectionString)` with
`options.UseSqlServer(connectionString)`, adding the
`Microsoft.EntityFrameworkCore.SqlServer` package. Nothing else in the application knows
which database is behind it.

---

## 8. When something goes wrong

**`You must install or update .NET`** — the SDK installed is older than 10.0. Install .NET
10 from the link in section 1.

**`Address already in use`** — another copy is already running on that port. Close the
other terminal, or use a different launch profile.

**`The database on disk was built by an earlier version`** — exactly what it says. Delete
the `.db` files as in section 6 and run again.

**The page loads but has no styling** — Bootstrap loads from a CDN and the machine is
offline. The application still works; see the note at the end of the README for running
fully offline.

**Login says "Invalid login attempt"** — check the password is `Library@2026` with a
capital L and the `@` symbol. If it still fails, delete the database and run again; the
seeder resets the demo passwords on every start.
