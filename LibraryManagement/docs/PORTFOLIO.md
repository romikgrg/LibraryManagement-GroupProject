# Project portfolio — solo submission

**Submitted by:** Romik Gurung, S374535
**Unit:** HIT339 Distributed Development
**Assessment:** Group Project — Management and Horizontal Feature Expansion

---

## Note on solo submission

> I was not placed in a group for this assessment so I am submitting as much as I could as a solo student.
>
> This portfolio follows the required structure. Two sections — the team contract and the
> meeting minutes — describe agreements and events between team members, which did not
> happen, so rather than leave them blank or invent them I have substituted the honest
> equivalent of each and labelled it clearly. Everything else is complete.


---


The technical documents are different and already written: `INSTALL.md`, `USER-GUIDE.md`,
`API.md` and `ARCHITECTURE.md` describe software that exists and can be verified by running
it.

### Assembly order

One PDF, in this order:

1. Front page (include the solo note above)
2. Table of contents
3. Installation and running instructions — `INSTALL.md`
4. User guide — `USER-GUIDE.md`
5. Project plan — sections 1 to 7 below
6. Integration diagrams and user journey maps — `ARCHITECTURE.md` plus section 8
7. Reflection — section 9


---

## 1. Working agreement *(substituted — no team)*

> **Not applicable: I worked alone on this project.** In place of a team contract, this is
> the working agreement I set for myself and held to.


## 1. Working agreement *(substituted — no team)*

> **Not applicable: I worked alone on this project.** In place of a team contract, this is
> the working agreement I set for myself and held to.


> **My commitments**
>
> | Commitment | What I set | Did I hold to it? |
> |---|---|---|
> | Working sessions | two hours per day | Most of the day, yes |
> | Where the work lives | https://github.com/romikgrg/LibraryManagement-GroupProject |
> | Definition of done | builds clean, feature tested by hand, committed | 
> | When I would ask for help | stuck more than two hours → check the lecturer slide, ask AI| |
>
> **What I did instead of a team:** I tested every code myself in main device and again in another device.
> 
---

## 2. Requirements document and notes

Each requirement from Part B of the brief, with the reading I settled on before building it.
Most of them needed a decision first; the brief states the goal, not the mechanics.

| # | Requirement (from the brief) | My reading of it | Priority | Built | Tested |
|---|---|---|---|---|---|
| 1 | Public self-service kiosk view | A separate touch-first route with its own layout and no staff navigation. A patron identifies with their card number and can borrow, check their account, or look an item up. Deliberately not a skin over the reception screens | Must | Yes | T3, T4 |
| 2 | Public read-only REST API | Versioned JSON endpoints for items, categories and branch status. Read-only enforced structurally by having no write actions, and no borrower data in any response | Must | Yes | T16 |
| 3 | Automated notification system | Simulated email and SMS written to a log table and the application log, triggered on borrow, due-soon, overdue, fine, hold-ready and transfer-arrived events, with a staff dashboard | Must | Yes | T7, T8 |
| 4 | Multi-branch inventory extension | Items belong to a branch and can move between branches via a tracked transfer. Staff accounts belong to a desk. One shared database across all branches | Must | Yes | T1, T9, T10, T15, T17 |
| 5 | Digital reservation and waitlist queue | A queue per item for anything borrowed or damaged. When the item returns, the first in line is promoted automatically, the item is set aside, and the member is notified | Must | Yes | T6, T7 |
| 6 | External book/media data importer | CSV upload with a preview-then-commit flow, plus a mock external metadata provider behind an interface | Must | Yes | T11, T12 |
| 7 | Expanded manager analytics and export | Branch-scoped dashboard plus CSV and PDF export of borrowing, fine revenue and inventory health | Must | Yes | T13, T14, T15 |

Test references are to [TEST-PLAN.md](TEST-PLAN.md).

### Decisions and assumptions

The brief leaves these open. Each had to be settled before the relevant module could be
built, and each is defended in [ARCHITECTURE.md](ARCHITECTURE.md).

| Question | Decision | Why |
|---|---|---|
| Does each branch get its own database? | No — one shared database, with per-branch configuration | A transfer between two sites is impossible if neither instance can see the other's stock. Splitting the data would break the feature the brief asks for |
| What does "run on the multi-branch locations" mean? | One build, one database, a `BranchDeployment:Code` setting per site | Makes the installation instructions identical everywhere except the line naming the location, which is what the brief asks for |
| Does an item have one branch or two? | Two: where it physically is, and which branch owns it | Without the second, an item lent to another branch vanishes from its home branch's inventory and the stock figures stop adding up |
| When does a transfer change an item's location? | Only on receipt | Between dispatch and receipt the item is `InTransit` and still reads as the sender's, so two desks can never both believe they hold it |
| Can an item be returned to a different branch? | Yes, and it joins the shelves there | That is what patrons actually do, and the loan already records where it went out from |
| Is queue position stored or derived? | Derived from the time the hold was placed | A stored position must be renumbered whenever anyone cancels, and one missed update puts two people in the same place |
| Who may take an item off the holds shelf? | Only the member it is held for | Otherwise a hold guarantees nothing |
| How long does a hold wait to be collected? | 7 days, configurable | Long enough to be fair, short enough to keep the queue moving |
| Should a failed notification roll back the loan? | No — recorded as failed, the loan completes | A bounced message is an ordinary event, not an exception that should abandon somebody's transaction |
| Should the kiosk show staff-worded refusals? | No — softened, and the patron is sent to the desk | "You owe $12.50 in fines" is fine at a counter and alarming on a screen in a foyer |
| What stays out of the public API? | Everything about members, loans and fines | An outside caller sees the catalogue as a person standing at the shelves sees it |

---

## 3. Project plan overview

### Scope

**In scope:** all seven Part B modules, built on my own Assignment 2 codebase, retargeted to
.NET 10; the supporting documentation; and a manual acceptance test of every requirement.

**Deliberately out of scope**, with reasons:

| Left out | Why |
|---|---|
| Automated test suite | No time to write one and still finish seven modules. The service layer was built to be testable — the context can be swapped for the in-memory provider, `IClock` for a fixed date — but the tests themselves are not written. Covered honestly in the test plan |
| EF Core migrations | `EnsureCreated` is used instead, with a guard that detects an out-of-date database and says what to do. Migrations are the correct answer and the first thing I would add |
| Real email and SMS providers | The brief asks for simulation. The channel abstraction means a real provider is one new class and one line in `Program.cs` |
| A scheduled background job | The due-date sweep and hold expiry are buttons standing in for a nightly job, so the behaviour can be demonstrated on request |

### Timeline

| When | Planned | What actually happened |
|---|---|---|
| Before 11 Oct | Read the brief, decide scope | Read the brief, the rubric and the Working Together, Documentation and DDD lectures. Chose my own Assignment 2 as the baseline |
| Sun 11 Oct | Start building | Built all seven modules and the supporting documentation in one long session |
| 11–14 Oct | Review | Read through the code module by module to understand what had been produced |
| Wed 14 Oct | Build and test | Retargeted to .NET 10, fixed one build error and three warnings, ran the 17-step acceptance test, found and fixed five defects |
| Fri 16 Oct | Documentation | Set up version control, published the repository, completed the hours log |
| Fri 16 Oct | Finish the portfolio | |

### Milestones

| Milestone | Target | Met |
|---|---|---|
| Baseline chosen and scope agreed | 11 Oct | 11 Oct |
| All seven modules built | 11 Oct | 11 Oct |
| Solution builds on .NET 10 | 14 Oct | 14 Oct |
| Every requirement verified by test | 14 Oct | 14 Oct |
| Source under version control and published | 16 Oct | 16 Oct |
| Portfolio complete | 16 Oct | |
| Installation instructions tested on a clean machine | 16 Oct| |

### Risks

| Risk | Likelihood | Impact | What I did about it |
|---|---|---|---|
| Working alone on a project scoped for three to four people | Certain | High | Kept scope to exactly what the brief asks and nothing more |
| No second person to review code or test instructions | Certain | Medium | Ran a scripted 17-step acceptance pass instead of relying on ad-hoc clicking. Still leaves the install instructions untested by fresh eyes — the one gap I could not close alone |
| Code produced faster than I could read it | High | High | Budgeted 5 hours purely for reading and understanding the code. It became 17% of the project |
| Nothing under version control until late | Occurred | Medium | Published to GitHub on 16 Oct. The cost was being unable to reconstruct my hours from commit history — see section 7 |
| Code never compiled when first written | Occurred | High | One build error and three warnings, all resolved on 14 Oct. Caught because the build was treated as a separate step from running |
| Known vulnerabilities in transitive packages | Occurred | Low | Flagged by NuGet Audit, traced with `dotnet list package --vulnerable --include-transitive`. [State what you decided] |

---

## 4. My approach

The brief says "your team's approach". Mine was a solo approach, so this describes how one
person actually ran it.

**Baseline codebase.** My own Assignment 2 submission. It was the only one available to me,
and I already knew its structure — the item hierarchy, the service layer behind interfaces,
and the dependency inversion around `IFinePolicy` and `IClock`. That existing shape is the
reason seven new modules could be added without rebuilding anything: each new service
followed the pattern already there.

**Sequencing.** I did not build the modules in the order the brief lists them. I mapped the
dependencies first and built foundation-up:

```
Multi-branch  ──┬──► Kiosk
                ├──► Public API
                ├──► Import
                └──► Reporting

Notifications ──────► Reservations
```

Multi-branch came first because four of the seven features need branches to exist before
they can be built — the kiosk shows a branch, the API filters by one, the importer assigns
stock to one, the reports scope to one. Notifications came before reservations because a
hold is useless if nobody is told it is ready. Building in the brief's order would have
meant revisiting finished work three times.

**How I worked.** In long sessions rather than short daily ones, which suited a solo project
with no one to hand work to, but meant a single bad session cost a lot. The 11 October
session produced all seven modules; the following three days were spent reading what had
been produced rather than writing more.

**Quality control without a reviewer.** This is the part a solo project has to solve
deliberately, because every safeguard that normally comes from another person is missing.
What I used instead:

- **A scripted acceptance test** rather than ad-hoc clicking. Seventeen steps in a fixed
  order, each with a stated expectation, so a wrong result was obvious rather than a vague
  sense that something looked off. It found five defects.
- **Treating `dotnet build` as a separate step from `dotnet run`.** A clean list of compile
  errors, uncluttered by startup logs.
- **A deliberate gap between writing and reading.** The code was produced on the 11th and
  read over the following days, which is a poor substitute for a reviewer but better than
  reading it in the same hour it was written.
- **Testing the negative cases, not just the happy path.** Borrowing an item twice,
  importing a CSV with a deliberately invalid row, borrowing for a member who owes fines.
  A test suite that only proves things work proves very little.

**Tools.** Visual Studio Code, .NET 10 SDK, SQLite, Git and GitHub, and Claude for code
production and documentation drafting — declared in full in section 9.

---

## 5. Task allocations and hours — 

Working alone, every task is allocated to one person, which makes this simpler rather
than less valuable: what is being assessed is whether the work was broken down properly
and the time tracked honestly.

**Total: 29.9 hours**, across five working sessions between 1 october and 16 October 2026.

### How these hours were reconstructed

Version control was not set up until 16 October, three days before submission, so there is
no commit history to reconstruct from. The session totals below come from my own record of
when I worked; two of the sessions are corroborated by timestamps in my AI tool
conversation logs. The per-task split within each session is my reconstruction.

| Session | Hours | Source |
|---|---|---|
| Before 11 October | 6.0 | My own record |
| Sunday 11 October, from 7:15pm | 5.0 | My own record; session start corroborated |
| 11–14 October | 7.0 | My own record |
| Wednesday 14 October, 9:31–11:08pm | 1.6 | Corroborated by timestamps |
| Other solo work | 10.0 | My own record |
| Friday 16 October | 0.3 | Corroborated by timestamps |
| **Total** | **29.9** | |

### A note on what these hours represent

The estimates in the right-hand column are how long each task would take to write by hand.
The actual hours are the time I spent: scoping, specifying, reviewing, testing and
debugging, with generative AI used substantially for code production (see section 9).

The gap between the two columns is therefore large and is not an error. Reporting the
estimate as though it were time worked would be dishonest, and reporting the actual hours
without the AI declaration would be incomplete. Both are given.

### The tasks

| # | Task | Est. | Actual | Status |
|---|---|---|---|---|
| | **1. Setup and scoping** | | **5.5** | |
| 1.1 | Read the brief, rubric and relevant lectures; scope the work | 2 | 3.0 | Done |
| 1.2 | Choose the baseline codebase and set up the project | 2 | 0.8 | Done |
| 1.3 | Retarget to .NET 10, resolve build errors, verify | 2 | 1.0 | Done |
| 1.4 | Decide the branch deployment model (shared vs per-branch database) | 2 | 0.7 | Done |
| | **2. Multi-branch inventory** | | **3.0** | |
| 2.1 | Branch entity, DbContext configuration, seed data | 5 | 1.1 | Done |
| 2.2 | Add branch to items, members and loans | 4 | 0.8 | Done |
| 2.3 | Branch CRUD screens | 4 | 0.6 | Done |
| 2.4 | Per-branch deployment configuration and launch profiles | 4 | 0.5 | Done |
| | **3. Inter-branch transfers** | | **2.0** | |
| 3.1 | Transfer entity and state machine | 3 | 0.6 | Done |
| 3.2 | TransferService: request, dispatch, receive, cancel | 5 | 0.8 | Done |
| 3.3 | Transfers desk screens | 4 | 0.6 | Done |
| | **4. Notifications** | | **2.2** | |
| 4.1 | Channel abstraction and two simulated senders | 4 | 0.7 | Done |
| 4.2 | NotificationService and message composer | 4 | 0.7 | Done |
| 4.3 | Hooks on borrow, return, fine, hold and transfer events | 2 | 0.3 | Done |
| 4.4 | Notification dashboard and due-date sweep | 4 | 0.5 | Done |
| | **5. Reservations and waitlist** | | **2.0** | |
| 5.1 | Reservation entity and queue model | 3 | 0.5 | Done |
| 5.2 | ReservationService: place, promote, expire | 6 | 0.9 | Done |
| 5.3 | Holds desk screens | 4 | 0.6 | Done |
| | **6. Self-service kiosk** | | **1.5** | |
| 6.1 | Kiosk controller and flow | 4 | 0.7 | Done |
| 6.2 | Touch layout and stylesheet | 5 | 0.8 | Done |
| | **7. Public developer API** | | **1.0** | |
| 7.1 | Response contracts and controller | 4 | 0.7 | Done |
| 7.2 | API documentation | 2 | 0.3 | Done |
| | **8. Bulk import** | | **2.0** | |
| 8.1 | CSV preview and commit pipeline | 6 | 0.9 | Done |
| 8.2 | Mock external metadata provider | 4 | 0.6 | Done |
| 8.3 | Import screens | 3 | 0.5 | Done |
| | **9. Reporting and export** | | **1.5** | |
| 9.1 | Branch-scoped dashboard and network figures | 4 | 0.7 | Done |
| 9.2 | CSV and PDF export | 6 | 0.8 | Done |
| | **10. Documentation and testing** | | **4.2** | |
| 10.1 | Installation and running instructions | 3 | 0.7 | Done |
| 10.2 | User guide | 4 | 0.9 | Done |
| 10.3 | Architecture document and diagrams | 4 | 0.8 | Done |
| 10.4 | Test plan | 2 | 0.5 | Done |
| 10.5 | Portfolio assembly | 4 | 0.3 | In progress |
| 10.6 | End-to-end acceptance testing (17 steps, 5 defects found) | 6 | 1.0 | Done |
| | **11. Code review and comprehension** | | **5.0** | |
| 11.1 | Reading and understanding the service layer | — | 2.5 | Done |
| 11.2 | Reading and understanding the data model and controllers | — | 2.5 | Done |
| | **Total** | **~116** | **29.9** | |

### Where the time actually went

| Category | Hours | Share |
|---|---|---|
| Building the seven modules | 13.2 | 44% |
| Reading and understanding the code | 5.0 | 17% |
| Setup, scoping and planning | 5.5 | 18% |
| Documentation | 3.2 | 11% |
| Testing and defect fixing | 1.5 | 5% |
| Version control and submission | 1.5 | 5% |

### Estimate versus actual

**Where the estimates were furthest out, and why.**

*Task 1.1, scoping* — estimated 2 hours, took 3. The brief leaves several questions open
that have to be answered before anything can be built: whether branches share a database,
what "multi-branch locations" means for deployment, who may take an item off the holds
shelf. Deciding those took longer than reading the brief did.

*Task 11, code review* — not in the original estimate at all, and it became 17% of the
total. That is the clearest lesson in this table. When code is produced faster than it can
be read, understanding it becomes its own task, and leaving it out of the plan means
budgeting for a project you are not actually running.

*Task 10.6, acceptance testing* — estimated 6 hours, took 1. The estimate assumed writing
tests; what actually happened was a scripted manual pass. Faster, and much weaker: section
6 of the test plan is honest about what that leaves uncovered.

*The 116 against the 29.9* — the single largest gap, and it is the AI declaration in
numbers. Without that context the actual column looks like an understatement of the work;
with it, the two columns measure two different things and both are true.

---

## 6. Work log *(substituted — no meetings)*

> **Not applicable: I worked alone, so there were no team meetings.** In place of minutes,
> this is a log of working sessions and the decisions made in each.


### Before 11 October — scoping, 6.0 hrs

**Worked on:** Reading the brief, the rubric and the Working Together, Documentation and
Domain Driven Design lectures. Deciding which codebase to use as the baseline.

**Decided:** Use my own Assignment 2 as the baseline. Treat the brief's "horizontal
expansion" instruction literally — add breadth, change no existing architecture.

**Problems:** Several requirements are ambiguous and had to be interpreted before anything
could be built. Recorded in section 2.

---

### Sunday 11 October, from 7:15pm — build, 5.0 hrs 

**Worked on:** Mapping module dependencies, then building foundation-up: multi-branch,
transfers, notifications, reservations, kiosk, API, import, reporting. Supporting
documentation drafted in the same session.

**Decided:** One shared database rather than one per branch. An item carries both a current
branch and an owning branch. Transfers change location only on receipt. Queue position
derived from time placed, not stored.

**Problems:** No .NET SDK available during the session, so none of the code was compiled.
That deferred every build error to the 14th.

**Next:** Build it and find out what breaks.

---

### 11–14 October — code review, 7.0 hrs

**Worked on:** Reading the generated code module by module. The service layer first, then
the data model and controllers.

**Decided:** Keep the structure as produced. The patterns matched the Assignment 2 codebase
closely enough that changing them would have cost more than it gained.

**Problems:** Reading code written faster than I could follow it turned out to be its own
substantial task, and it was not in my original plan at all.

---

### Wednesday 14 October, 9:31–11:08pm — build and test, 1.6 hrs 

**Worked on:** Retargeting to .NET 10. Fixing one build error and three code warnings.
Running the full 17-step acceptance test.

**Decided:** Treat `dotnet build` as a separate step from `dotnet run`, so compile errors
arrive as a clean list.

**Problems:** Five defects found — detailed in [TEST-PLAN.md](TEST-PLAN.md) section 5. The
build error was a generic constraint violation; the most interesting one was the sign-in
redirect, where role-based routing existed but was unreachable dead code.

**Next:** Version control, then documentation.

---

### Friday 16 October — version control and documentation

**Worked on:** Git repository initialised and published to GitHub. Task allocation and
hours log completed. Portfolio sections drafted.

**Decided:** Make the repository public, which satisfies the brief's "at least read
credentials" requirement without depending on the marker's GitHub username.

**Problems:** With no commit history to reconstruct from, the hours log had to be rebuilt
from memory and from timestamps in my tool conversation logs. See section 7.

---


## 7. Strengths and weaknesses — **8 points**

> **What went well**
- Foundation-first sequencing. Branches before the four features that need them. Result: zero rework. Would building in the brief’s listed order have cost you three revisits?
- The scripted 17-step test. It found the dead routing code — and clicking around would never have found it, because the front desk was reachable from the menu. The feature “worked”. Only a test that said “sign in → expect to land on the front desk” caught that the code behind it never ran.
- Separating dotnet build from dotnet run. Clean error list instead of errors buried in startup logs.
- Testing the refusals, not just the successes. Borrowing an item twice. A CSV row with Bookk. A member who owes fines. What does a test suite that only proves things work actually prove?

> **What I found hard**
- No reviewer. The sign-in bug is the evidence: code that looked right, read fine, and never executed. A second person would plausibly have asked “when does Home/Index actually run?” You can’t ask yourself that question.
- The install instructions are still untested by fresh eyes. The lecture says twice that untested instructions lose marks. This is the one gap being solo genuinely cannot close — is there anything you could still do about it?
- Code arriving faster than you could read it. Five hours, 17% of the project, on a task that wasn’t in the plan.
- No version control for three days. You couldn’t reconstruct your own hours from your own work.

> **Where my estimates were wrong, and what that taught me**
- 116 estimated, 29.9 actual
- Task 11 wasn’t in the original plan at all and became the third-largest item
- Testing: 6 estimated, 1 actual — but why? The method changed. Is faster better here?
- Scoping: 2 estimated, 3 actual — the only task that ran over. What does that say about where the real difficulty was?

> **What I would do differently**
- Version control from the first commit
- Build after each module rather than at the end — the CS0452 error would have surfaced on day one instead of day four
- Put “understand the code” on the plan as a task with hours
- The honest one: what would a second person actually have given you?

> **What I learned**
> - About .NET and the technology:
- Table Per Hierarchy: three item types, one table, a discriminator column. Why not a table each?
- Dependency inversion in practice, three different ways. IFinePolicy (swap the rule), IClock (control time in a test), -           INotificationChannel (two implementations registered against one interface, one picked at runtime by the member’s preference). Which of  those three is the most useful idea?
- AsNoTracking<TEntity>() is constrained where TEntity : class — a generic constraint you only notice when you violate it with int
- Method hiding (CS0108): a method can compile, work, and quietly shadow something inherited
- One build, many locations, through configuration — you now know what “deployment” means that you probably didn’t before
- Transitive dependencies carry vulnerabilities you never chose. dotnet list package --vulnerable --include-transitive
- Two-phase operations: preview, then commit. Why does a bulk import need that when a single-item form doesn’t?
> - About planning and scoping work: 
- Map dependencies before sequencing. Four of seven features needed branches first — that wasn’t in the brief, you had to work it out
- The task that wasn’t in the plan became 17% of the project. What else gets left off plans?
- The only task that ran over was scoping. Deciding what to build cost more than building. Does that surprise you?
- When the method changes, the estimate becomes meaningless — 6 hours of writing tests versus 1 hour of running them manually aren’t comparable numbers
> - About working alone versus in a team:
- What you lose isn’t the labour. It’s the feedback loop.
- Everything still got done. What degraded was your confidence that it was right.

> **What I would want from a team, having not had one**
- Someone to read code they didn’t write. The dead sign-in routing is the proof, it looked correct, and the only thing that found it was a test that stated an expectation out loud.
- Someone to run the install instructions cold. The lecture says twice that untested instructions lose marks. This is the single gap being solo cannot close, and you should say so plainly.
- Someone to argue with. Nobody challenged “one shared database”. It’s probably right, but it was never made to survive resistance, and a decision that’s never been argued with is only a guess that happened to be yours.
- Someone waiting on you. Deadlines you set alone are the easiest ones to move.
- Articulating a problem out loud. Half of debugging is explaining it to someone who hasn’t seen it.

> **What did being alone actually give you?**
- No coordination cost. 
- No waiting. No merge conflicts. 
- Decisions in seconds instead of a thread. 
- Zero time lost to anyone else’s schedule.
---

## 8. User journey maps

Four journeys covering the new breadth features, one per new audience or channel.

### Journey 1: a patron borrows at the kiosk

**Who:** A parent with a small child, in a hurry, has never used the kiosk before.
**Goal:** Take three picture books home without queueing at the desk.

| Step | What they do | What the system does | How it feels | Where it could fail |
|---|---|---|---|---|
| 1 | Walks up to the terminal | Shows three large choices | Obvious | Screen asleep or showing a stale session |
| 2 | Presses "Borrow an item" | Asks only for the card — the item box is not shown yet | One thing at a time | Two empty fields would invite filling the wrong one |
| 3 | Scans their card | Greets them by first name | Reassuring: the machine knows them | Card not recognised → sent to the desk |
| 4 | Scans the first book | Confirms the title and due date in large type | Quick, no ambiguity | Item on hold for someone else → softened message, sent to the desk |
| 5 | Scans two more | Confirms each, keeps the card in session | No need to re-scan the card | — |
| 6 | Presses Finish | Returns to the idle screen | Done in under a minute | Walking away without pressing Finish leaves the session open |

**Design consequence:** the kiosk is a separate controller with its own layout, because a
patron has no account, no training and no mouse. Staff refusal wording is softened before it
reaches the screen.

---

### Journey 2: a patron reserves an item that is out

**Who:** A member who wants a book another member currently has.
**Goal:** Get hold of it without checking back every few days.

| Step | What they do | What the system does | How it feels | Where it could fail |
|---|---|---|---|---|
| 1 | Searches the catalogue, finds it is on loan | Shows status and holding branch | Disappointing but clear | — |
| 2 | Asks at the desk | Staff place a hold, collection defaults to the member's home branch | Handled in seconds | Member already has it out → refused with a reason |
| 3 | Is told their queue position | Position derived from the time they joined | Fair and explicable | — |
| 4 | Waits | — | Invisible; nothing to do | A long queue with no estimated date |
| 5 | The item is returned | The first in line is promoted, the item goes to the holds shelf, a message is sent | The system acted without being asked | Member has no contact details → logged as skipped |
| 6 | Collects within seven days | The hold closes as collected when the item is borrowed | Complete | Not collected → the hold expires and passes to the next person |

---

### Journey 3: staff move an item between branches

**Who:** Reception staff at two different branches.
**Goal:** Get an item from where it is to where it is wanted, without either desk losing track.

| Step | Who | What happens | Where it could fail |
|---|---|---|---|
| 1 | Destination | Raises a request against the library code | Item already has a transfer in progress → refused |
| 2 | Holding branch | Sees it under "Going out", takes it off the shelf, marks it dispatched | Item is out on loan → refused until returned |
| 3 | — | Item becomes `InTransit`: neither branch can lend it | — |
| 4 | Destination | Receives it when the van arrives | Cancelling after dispatch is refused — the item has to arrive somewhere |
| 5 | — | Location changes, item becomes available, or goes to the holds shelf if the transfer was for a hold | — |

**Design consequence:** location changes only on receipt. This is the single decision that
keeps two desks from both believing they hold the same item.

---

### Journey 4: an administrator bulk-imports a delivery

**Who:** The administrator, with a supplier spreadsheet of forty new items.
**Goal:** Get them catalogued without forty manual forms, and without importing rubbish.

| Step | What they do | What the system does | Where it could fail |
|---|---|---|---|
| 1 | Downloads the template | Supplies a correctly shaped CSV | Using their own column names |
| 2 | Fills it in, uploads it, picks a branch | Reads the file and judges every row — writes nothing | File is not CSV, or is over 2 MB → rejected with a reason |
| 3 | Reads the preview | Each row marked Ready, Duplicate or Problem, with the reason | — |
| 4 | Fixes the bad rows and re-uploads, or proceeds | Only rows marked Ready are written | Preview expired → re-upload |
| 5 | Confirms | Items created, redirected to the collection | — |

**Design consequence:** two passes, not one. A bulk tool that writes first and reports
afterwards leaves somebody cleaning up by hand.

---

## 9. Use of generative AI

Claude (Anthropic) was used in  this assignment, across sessions on 11, 14 and 16 October 2026.

- What the AI produced: the seven Part B modules in — multi-branch inventory, inter-branch transfers, notifications, reservations, the kiosk, the public API, bulk import and report exports — built onto my Assignment 2 codebase. It also helped me to draft the  user guide, architecture document and test plan and It proposed the module sequencing and the design decisions recorded in section 2.

- What I did: chose the baseline codebase; decided scope; ran every build and test myself; reported each failure; applied every fix; verified all seven requirements against the running application; set up version control and published the repository; and supplied the hours in section 5 from my own record.

Limitations I am aware of: the code was not compiled when first generated and contained one build error and three warnings that only surfaced when I built it. The AI also stated that the build had succeeded when it had not, which I caught by reading the actual build output. Two further defects — the sign-in routing and an incorrect footer — were found by my own testing rather than by review. No automated tests are included.


---
