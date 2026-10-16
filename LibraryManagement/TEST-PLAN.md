# Test plan and results

**System:** Darwin Community Library, multi-branch edition
**Tested by:** Romik Gurung
**Date run:** 14 October 2026
**Build:** .NET 10, clean clone, freshly seeded database

---

## 1. Purpose

This is a manual acceptance test covering every requirement in Part B of the brief. Each
test states what was done, what was expected and what actually happened. It was run end to
end in one sitting against a database created from scratch by the seeder, so no test
depended on data left behind by an earlier run.

It is not an automated test suite. The service layer was written to be testable — the
`DbContext` can be swapped for the in-memory provider and `IClock` for a fixed date — but
no automated tests ship with this version. That is noted honestly in section 6.

---

## 2. Environment

| | |
|---|---|
| Runtime | .NET 10 |
| Database | SQLite, created and seeded on first run |
| Browser | [which one you used] |
| Deployment mode | Head office (`BranchDeployment:Code = ALL`), plus one branch-scoped run |
| Seed data | 3 branches, 11 items, 4 members, 12 loans, 3 reservations, 1 transfer in progress |

---

## 3. Requirement coverage

Every requirement in Part B, mapped to the tests that exercise it.

| Brief requirement | Tests | Result |
|---|---|---|
| Public self-service kiosk view | 3, 4 | Pass |
| Public read-only REST API | 16 | Pass |
| Automated notification system | 7, 8 | Pass |
| Multi-branch inventory extension | 1, 9, 10, 15, 17 | Pass |
| Digital reservation and waitlist queue | 6, 7 | Pass |
| External book/media data importer | 11, 12 | Pass |
| Expanded manager analytics and export | 13, 14, 15 | Pass |
| Responsive public portal (carried from Assignment 2) | 1, 2 | Pass |
| Role-based access control (carried from Assignment 2) | 5, 10, 13 | Pass |

---

## 4. Test steps and results

### Public access — no sign in

| # | Test | Expected | Actual |
|---|---|---|---|
| 1 | Search the catalogue for `BK-0004` | One result, showing status and holding branch | Pass — *Storm Boy*, Borrowed, at Casuarina |
| 2 | Narrow the browser to phone width | Cards reflow to one column, menu collapses | Pass |
| 3 | Kiosk → Look up an item → `BK-0004` | Plain-language availability message | Pass |
| 4 | Kiosk → Check my account → `M-1002` | Items out, overdue count, fines owing | Pass |

### Reception — `reception@library.test`

| # | Test | Expected | Actual |
|---|---|---|---|
| 5 | Sign in | Lands on the front desk, role-correct menu | Pass (after defect D2 was fixed) |
| 6 | Holds | Existing queue visible, in the order people joined | Pass |
| 7 | Return `BK-0004` | Fine calculated; item moves to the holds shelf rather than the open shelf; member notified | Pass — all three effects observed |
| 8 | Notices → Run the due-date sweep | Reminders written for overdue loans; a second run skips what it already sent today | Pass |
| 9 | Transfers → Check in the in-transit item | Item joins the receiving branch and returns to Available | Pass |

Test 7 is the most important in this plan. It is the only point where three modules
interact in one transaction: `LoanService.ReturnAsync` calculates the fine, asks
`ReservationService.PromoteNextAsync` whether anybody is waiting, and the promotion asks
`NotificationService` to tell them. All three were confirmed by observing the fine on the
return receipt, the item appearing under *Ready for collection*, and the matching message
on the notices dashboard.

### Administration — `admin@library.test`

| # | Test | Expected | Actual |
|---|---|---|---|
| 10 | Branches → Casuarina | Stock position, availability rate, items owned but held elsewhere | Pass |
| 11 | Import → provider search → import a record | Item created with a generated library code at the chosen branch | Pass |
| 12 | Import → CSV with a deliberately invalid `Kind` | Bad row flagged with a reason and skipped; valid rows still import | Pass |

Test 12 checks the thing a bulk import tool most needs to get right: that it refuses bad
data loudly rather than writing it quietly.

### Management — `manager@library.test`

| # | Test | Expected | Actual |
|---|---|---|---|
| 13 | Reports, all branches then one branch | Figures change with the branch scope | Pass |
| 14 | Export fine revenue as PDF | Landscape PDF with a totals row | Pass |
| 15 | Inventory | Three branches side by side with availability | Pass |

### Public API — no sign in

| # | Test | Expected | Actual |
|---|---|---|---|
| 16 | `GET /api/v1/status` and `/api/v1/items?type=Book&availableOnly=true` | Valid JSON, catalogue data only, no borrower information | Pass |

### Per-branch deployment

| # | Test | Expected | Actual |
|---|---|---|---|
| 17 | Run with the "Darwin City branch" launch profile | Same build on a different port, branch code shown in the header and footer | Pass |

Test 17 is the one the installation instructions depend on: the same build, the same
database, one configuration value different.

---

## 5. Defects found and resolved

Found during this testing cycle and fixed before the result above was recorded.

| ID | Where | Problem | Resolution |
|---|---|---|---|
| D1 | `ReportExportService` | Build failure: `AsNoTracking()` is constrained to reference types and was applied after a projection to `int` | Moved `AsNoTracking()` ahead of the projection; made the placement consistent across all four similar queries |
| D2 | `Program.cs` routing | Signing in landed staff on the public catalogue. The role-based routing in `HomeController.Index` was never reached, because the default route went straight to the catalogue | Default route changed to `Home`, which redirects each role to its own desk and anonymous visitors to the catalogue |
| D3 | `TransfersController` | Action method named `Request` hid `ControllerBase.Request`, the inherited property holding the incoming HTTP request | Method renamed to `RequestItem` with `[ActionName("Request")]`, keeping the route and all existing links |
| D4 | `Views/Manager/Inventory.cshtml` | Possible null dereference: the null-forgiving operator was used on the first access to a nullable branch but not on the ones that followed | Branch captured in a local and null-checked once |
| D5 | Footer | Showed the wrong unit code and named the wrong assessment | Corrected |

### Outstanding, not fixed

| Where | Problem | Why it is acceptable for now |
|---|---|---|
| NuGet audit | Two transitive packages carry published vulnerability advisories | They arrive through Entity Framework's SQLite provider rather than being referenced directly. Traced with `dotnet list package --vulnerable --include-transitive`. [State what you decided: pinned patched versions, or accepted with a reason.] |

---

## 6. What this plan does not cover

Stated plainly, because a test plan that implies more coverage than it has is worse than one
that admits its limits.

- **No automated tests.** Every result above was produced by hand. A regression would not be
  caught without re-running this plan.
- **No load or performance testing.** The statistics and inventory screens aggregate in
  memory, which suits a branch-sized collection and was not tested at scale.
- **No concurrent-use testing.** Two staff members borrowing the same item at the same
  instant was not exercised. The loan service guards against it by checking the loan table
  rather than trusting the status column, but that guard is unproven under real contention.
- **No cross-browser testing.** Tested in one browser only.
- **No accessibility audit.** The kiosk was built to large touch targets and visible focus
  states, but was not tested with a screen reader.
- **Security testing is by inspection, not by attack.** Antiforgery tokens, role attributes,
  output encoding and local-only redirects were confirmed present in code review, not probed.

---

## 7. How to re-run this plan

1. Delete `library.db` and any `library.db-wal` / `library.db-shm` beside it.
2. `dotnet run --project src/LibraryManagement`
3. Work through section 4 in order. The tests are sequenced so each one sets up the next —
   test 7 depends on the queue confirmed in test 6, and test 10 depends on the transfer
   received in test 9.
