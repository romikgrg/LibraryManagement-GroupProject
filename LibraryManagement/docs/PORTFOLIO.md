# Project portfolio — solo submission

**Submitted by:** [your name], [student number]
**Unit:** HIT339 Distributed Development
**Assessment:** Group Project — Management and Horizontal Feature Expansion
**Submitted:** [date]

---

## Note on solo submission

> I was not placed in a group for this assessment. I raised this with Rohan Hawthorne on
> [date], and was told to submit as much as I could as a solo student.
>
> This portfolio follows the required structure. Two sections — the team contract and the
> meeting minutes — describe agreements and events between team members, which did not
> happen, so rather than leave them blank or invent them I have substituted the honest
> equivalent of each and labelled it clearly. Everything else is complete.

**Put that note on the front page, not buried.** The person marking may not be the person
you emailed. Quote the date and keep the reply.

---

**This document is a set of forms. Every `[...]` is yours to fill in.**

Nothing is pre-filled, deliberately. Hours are hours you worked; a reflection is yours to
write. The unit's own Documentation lecture says it: *"extra work should not be 'made up'
at the end."* Your lecturer already knows you are solo, so there is nothing to disguise and
everything to gain from being straight about it.

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

### What each section is worth

| Rubric criterion | Points | Section below |
|---|---|---|
| Project Task Allocations | 10 | §5 |
| Strengths and Weaknesses | 8 | §7 |
| Installation and Running instructions | 5 | `INSTALL.md` |
| Project Plan Overview | 5 | §3 |
| Requirements | 4 | §2 |
| Team Approach | 4 | §4 |
| Team contracts | 2 | §1 (substituted) |
| Meeting Minutes | 2 | §6 (substituted) |

Sections 5 and 7 are worth 18 points between them — nearly as much as the entire software
component. Give them real time.

---

## 1. Working agreement *(substituted — no team)*

> **Not applicable: I worked alone on this project.** In place of a team contract, this is
> the working agreement I set for myself and held to.

Write a short, honest version. It is worth 2 points and takes ten minutes, and an honest
substitute scores better than a blank page.

> **My commitments**
>
> | Commitment | What I set | Did I hold to it? |
> |---|---|---|
> | Working sessions | [e.g. two evenings a week plus Sunday] | [honestly] |
> | Where the work lives | [repository URL] | |
> | How I tracked tasks | [board URL or file] | |
> | Definition of done | [e.g. builds clean, feature tested by hand, committed] | |
> | When I would ask for help | [e.g. stuck more than two hours → post on the unit forum] | |
>
> **What I did instead of a team:** [how you covered review, testing and second opinions
> with nobody to hand work to — did you leave code overnight and reread it? Test on a
> second machine? Ask on a forum?]

---

## 2. Requirements document and notes

Worth 4 points. Take each requirement from the brief and record what you decided it means,
because most of them need a decision before they can be built.

> | # | Requirement (from the brief) | My reading of it | Priority | Built | Tested |
> |---|---|---|---|---|---|
> | 1 | Public self-service kiosk view | [...] | [Must/Should/Could] | ☐ | ☐ |
> | 2 | Public read-only REST API | [...] | | ☐ | ☐ |
> | 3 | Automated notification system | [...] | | ☐ | ☐ |
> | 4 | Multi-branch inventory extension | [...] | | ☐ | ☐ |
> | 5 | Digital reservation and waitlist | [...] | | ☐ | ☐ |
> | 6 | External book/media importer | [...] | | ☐ | ☐ |
> | 7 | Expanded analytics and export | [...] | | ☐ | ☐ |

**Decisions and assumptions.** One row per question the brief did not answer. These are
real decisions this build had to make — confirm you agree with each, change any you don't,
and add your own:

> | Question | Decision | Why | Date |
> |---|---|---|---|
> | Does each branch get its own database? | No — one shared database | A transfer cannot work if neither site can see the other's stock | [...] |
> | Can an item be returned to a different branch? | Yes, and it joins the shelves there | That is what patrons actually do | [...] |
> | Who may take an item off the holds shelf? | Only the member it is held for | Otherwise a hold guarantees nothing | [...] |
> | How long does a hold wait to be collected? | 7 days, configurable | [...] | [...] |
> | Should a failed notification roll back the loan? | No — logged and the loan completes | A bounced message is ordinary, not exceptional | [...] |
> | Should the kiosk show staff-worded refusals? | No — softened, and sent to the desk | A patron alone at a machine should not be alarmed | [...] |
> | [...] | | | |

---

## 3. Project plan overview

Worth 5 points.

> **Scope:** [what you set out to deliver, and what you decided to leave out]
>
> **Timeline**
>
> | Week / dates | Planned | Actually happened |
> |---|---|---|
> | [...] | [...] | [...] |
>
> **Milestones**
>
> | Milestone | Target | Met on |
> |---|---|---|
> | Baseline retargeted to .NET 10 and building | [...] | [...] |
> | Multi-branch foundation complete | [...] | [...] |
> | All seven modules built | [...] | [...] |
> | Install instructions tested on a clean machine | [...] | [...] |
> | Portfolio assembled | [...] | [...] |
>
> **Risks I identified, and what I did about them**
>
> | Risk | Likelihood | Impact | What I did |
> |---|---|---|---|
> | Working alone — no one to review or cover for me | High | High | [...] |
> | Code written but never compiled | [...] | High | [...] |
> | Scope too large for one person | [...] | [...] | [...] |
> | [...] | | | |

Risk is a good section to be honest in. "I was one person doing a four-person project" is a
real risk with real consequences, and naming it is worth more than pretending it wasn't.

---

## 4. My approach

Worth 4 points. The brief says "your team's approach"; yours is a solo approach, so say so
and describe it.

> **Baseline codebase:** My own Assignment 2 submission. [Why: it was the only one
> available to me, and I already knew its structure — say so plainly.]
>
> **How I worked:** [One or two paragraphs. Did you work module by module? Build the
> foundation first? What changed partway through and why? Describe what actually happened,
> including the false starts.]
>
> **Order of work and why:** [e.g. multi-branch first because four of the seven features
> depend on branches existing; notifications before reservations because holds need to tell
> people. If that's the reasoning you used, say it — it shows you sequenced deliberately
> rather than picking features at random.]
>
> **Branching strategy:** [what you did in git]
>
> **How I kept quality up without a reviewer:** [this is the interesting part of a solo
> approach — be specific]
>
> **Tools:** [IDE, repository host, task tracking, anything else]

---

## 5. Task allocations and hours — **10 points**

The largest single documentation item on the rubric. Working alone, every row is allocated
to you, which makes this simpler, not less valuable: what is being marked is whether you
broke the work down properly and tracked it honestly.

The estimates below are what this build was planned against. **Replace the actual hours
with what you really spent and delete any row you did not do.** If you did not track as you
went, reconstruct from your commit history — `git log --pretty=format:"%ad %s" --date=short`
will give you dates and messages to work from, and say in a note that you reconstructed it.

> | # | Task | Est. | Actual | Date(s) | Status |
> |---|---|---|---|---|---|
> | | **Setup** | | | | |
> | 1.1 | Choose baseline, set up repository | 2 | [...] | [...] | [...] |
> | 1.2 | Retarget to .NET 10, verify build | 2 | | | |
> | 1.3 | Decide branch deployment model | 2 | | | |
> | | **Multi-branch inventory** | | | | |
> | 2.1 | Branch entity, DbContext config, seed data | 5 | | | |
> | 2.2 | Add branch to items, members and loans | 4 | | | |
> | 2.3 | Branch CRUD screens | 4 | | | |
> | 2.4 | Per-branch deployment config | 4 | | | |
> | | **Transfers** | | | | |
> | 3.1 | Transfer entity and state machine | 3 | | | |
> | 3.2 | TransferService — request, dispatch, receive, cancel | 5 | | | |
> | 3.3 | Transfers desk screens | 4 | | | |
> | | **Notifications** | | | | |
> | 4.1 | Channel abstraction and two simulated senders | 4 | | | |
> | 4.2 | NotificationService and composer | 4 | | | |
> | 4.3 | Hooks on borrow, return, fine, hold, transfer | 2 | | | |
> | 4.4 | Dashboard and due-date sweep | 4 | | | |
> | | **Reservations** | | | | |
> | 5.1 | Reservation entity and queue model | 3 | | | |
> | 5.2 | ReservationService — place, promote, expire | 6 | | | |
> | 5.3 | Holds desk screens | 4 | | | |
> | | **Kiosk** | | | | |
> | 6.1 | Kiosk controller and flow | 4 | | | |
> | 6.2 | Touch layout and stylesheet | 5 | | | |
> | | **Public API** | | | | |
> | 7.1 | Contracts and controller | 4 | | | |
> | 7.2 | API documentation | 2 | | | |
> | | **Import** | | | | |
> | 8.1 | CSV preview and commit pipeline | 6 | | | |
> | 8.2 | Mock metadata provider and import | 4 | | | |
> | 8.3 | Import screens | 3 | | | |
> | | **Reporting** | | | | |
> | 9.1 | Branch-scoped dashboard and network figures | 4 | | | |
> | 9.2 | CSV and PDF export | 6 | | | |
> | | **Documentation and testing** | | | | |
> | 10.1 | Installation instructions | 3 | | | |
> | 10.2 | Test install on a clean machine | 3 | | | |
> | 10.3 | User guide | 4 | | | |
> | 10.4 | Architecture document and diagrams | 4 | | | |
> | 10.5 | Portfolio assembly | 4 | | | |
> | 10.6 | End-to-end testing of every feature | 6 | | | |
> | | **Total** | **~116** | **[...]** | | |

> **Summary**
>
> | | Hours | Share |
> |---|---|---|
> | Code | [...] | [...]% |
> | Documentation | [...] | [...]% |
> | Testing | [...] | [...]% |
> | **Total** | | 100% |
>
> **Note on estimates versus actuals:** [where were you badly out, and why? That analysis
> is worth marks on its own — it shows you can read your own estimating.]

---

## 6. Work log *(substituted — no meetings)*

> **Not applicable: I worked alone, so there were no team meetings.** In place of minutes,
> this is a log of working sessions and the decisions made in each.

Worth 2 points. A dated log of sessions and decisions is the honest equivalent and takes
little effort, especially reconstructed from commits.

> ### [Date], [duration]
>
> **Worked on:** [...]
>
> **Decided:** [any decision that shaped what came next]
>
> **Blocked by / problems hit:** [...]
>
> **Next session:** [...]

Copy the block per session. Eight to twelve entries across the project is plenty.

---

## 7. Strengths and weaknesses — **8 points**

The second-largest documentation item, and the one most people under-write. It costs
nothing but honest thinking, and working alone on a group project is genuinely rich
material — you have something to say here that a team member does not.

Use the Working Together lecture headings as prompts, answered for a solo project:

> **What went well**
> - [...] — and the evidence: [...]
>
> **What I found hard**
> - **Working without a team.** [What did you actually lose? No one to review code, no one
>   to catch a bad assumption, no one to cover a week you were sick, nobody to test your
>   install instructions with fresh eyes. Which of those bit you?]
> - [...]
>
> **Where my estimates were wrong, and what that taught me**
> - [...]
>
> **What I would do differently**
> - [...]
>
> **What I learned**
> - About .NET and the technology: [...]
> - About planning and scoping work: [...]
> - About working alone versus in a team: [...]
>
> **What I would want from a team, having not had one**
> - [this is the question the rubric is really asking a solo student, and a thoughtful
>   answer here is worth more than a paragraph of filler about communication]

Be specific and be honest about the bad parts. A marker can tell the difference between a
reflection and a page of padding, and the honest version scores higher.

---

## 8. User journey maps

The brief asks for journey maps for the new breadth features. Four are worth mapping; the
first is started to show the level of detail.

> ### Journey: a patron borrows at the kiosk
>
> **Who:** A parent with a child, in a hurry, has never used the kiosk before.
> **Goal:** Take three picture books home without queueing at the desk.
>
> | Step | What they do | What the system does | How it feels | Where it could fail |
> |---|---|---|---|---|
> | 1 | Walks up to the terminal | Shows three large choices | Obvious | Screen asleep |
> | 2 | Presses "Borrow an item" | Asks for their card | Clear | [...] |
> | 3 | Scans card | Greets them by first name | Reassuring | Card not recognised → sent to desk |
> | 4 | Scans first book | Confirms, shows the due date | Quick | [...] |
> | 5 | Scans two more | Confirms each | [...] | [...] |
> | 6 | Presses Finish | Returns to the idle screen | Done | [...] |
>
> ### Journey: a patron reserves an item that is out
> [...]
>
> ### Journey: staff move an item between branches
> [...]
>
> ### Journey: an administrator bulk-imports a delivery
> [...]

---

## 9. Use of generative AI

Required by the unit, stated in three separate lectures. Be specific — a vague declaration
reads worse than a detailed one.

> Claude (Anthropic, [model], accessed [dates]) was used substantially in producing this
> assignment.
>
> **What the AI produced:** [name exactly what — which modules, which documents, which
> debugging sessions]
>
> **What I did:** [what you specified, what you tested, what you fixed, what you changed,
> what you wrote yourself]
>
> **What I can explain:** [the parts of the codebase you understand well enough to defend
> if asked — and make this true before you write it]
>
> **Limitations I am aware of:** [e.g. the code was not compiler-verified when generated
> and needed corrections before it ran; no automated tests are included]

Check the wording against the CDU guidance your lecturer linked:
<https://www.cdu.edu.au/library/language-and-learning-support/study-skills/learning-cdu/using-ai-tools-university>

---

## Submission checklist

- [ ] **The software builds and runs** — without this, 20 points are gone
- [ ] Every feature clicked through by hand against the walkthrough in `USER-GUIDE.md`
- [ ] Solo note on the front page, with the date you were given permission
- [ ] Portfolio assembled as one PDF, with front page and table of contents
- [ ] Installation instructions followed from a clean clone, by you, exactly as written
- [ ] Hours log completed with real numbers
- [ ] Strengths and weaknesses written properly, not padded
- [ ] Repository link works, with read access for the marker
- [ ] `README.md` explains the modules, branch seeding and testing the integrations
- [ ] AI declaration completed
- [ ] `library.db`, `bin/` and `obj/` are **not** in the repository
