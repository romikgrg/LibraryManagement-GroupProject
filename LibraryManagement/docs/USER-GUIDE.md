# User guide

How to use the Darwin Community Library system. Written for the people who use it rather
than the people who built it.

- [For library patrons](#for-library-patrons)
- [For the front desk](#for-the-front-desk)
- [For the administrator](#for-the-administrator)
- [For the manager](#for-the-manager)
- [A ten minute walkthrough](#a-ten-minute-walkthrough)

---

## For library patrons

Nothing in this section needs an account.

### Searching from your phone

Go to the library's web address and you land on the catalogue. Type anything you remember —
a title, an author, part of a code — and the matching items come back as cards.

Three ways to narrow it down:

- **Type**: books, music or toys.
- **Only show items on the shelf right now**: hides anything out on loan.
- Tap a result to see the full record, including which branch holds it.

The page is built for a phone first. On a small screen results become a single column of
cards and the menu folds into a button. Your last few searches are remembered while your
browser session lasts, so you can jump back to one with a tap.

### Borrowing at the self-service kiosk

The kiosk stands in the foyer. It does three things.

**Borrow an item**

1. Press **Borrow an item**.
2. Scan your library card, or type the number on it.
3. Scan the item. The screen confirms what you took and when it is due.
4. Scan another item, or press **Finish**.

**Check my account**

Scan your card to see what you have out, what is overdue, what you owe and anything being
held for you.

**Look up an item**

Scan or type a code to find out whether it is on the shelf and at which branch.

If the kiosk sends you to the desk, it is not a fault — something on the account needs a
person, and staff can sort it out in a moment.

### Reserving something that is out

Ask at the desk. Staff put you in the queue, and you are told automatically when the item
comes back and is ready for you. Holds are kept on the shelf for seven days.

---

## For the front desk

Sign in with your reception account. Everything below is under the menu at the top.

### Lending an item

**Front desk → Borrow an item.** Enter the library code, choose the member, and lend it.
The due date is worked out for you and the member is sent a confirmation.

The system refuses, with a reason, when:

- the item is not available — it may be out, damaged, in transit, or held for someone else
- the member's card is not active
- they already have five items out
- they owe $10 or more in fines

### Taking a return

**Front desk → Return an item.** Enter the code and say what condition it came back in.

- **Good** puts it back on the shelf.
- **Damaged** takes it out of circulation until somebody repairs it.
- **Beyond repair** retires it permanently.

If it is late, the fine is worked out and shown on a receipt with a button to take payment.
If somebody is waiting for the item, it goes straight onto the holds shelf and that member
is told.

An item can be returned to any branch, not only the one that lent it. It joins the shelves
wherever it is handed in.

### Holds

**Holds** shows everyone waiting for something.

- **Place a hold** puts a member in the queue for an item that is out or being repaired.
  Something sitting on the shelf can simply be borrowed instead.
- **Ready for collection** is the holds shelf. Each one shows the date it has to be
  collected by.
- **See the queue** shows who is waiting for one item, in order.
- **Clear uncollected holds** closes anything past its date and offers the item to the next
  person in line.

Queue position is decided by when somebody joined, so cancelling a hold never disturbs
anyone else's place.

### Transfers between branches

**Transfers** handles stock moving around the network.

To get something from another branch: **Request an item**, enter the code, and choose where
it should go.

Each transfer runs in three steps:

1. **Requested** — raised, waiting for the holding branch to act.
2. **In transit** — taken off the shelf and sent. Neither branch can lend it.
3. **Received** — checked in at the far end and on the shelf there.

Your screen shows two lists: things going out that you need to pick off the shelf, and
things coming in that you need to check in when the van arrives.

A transfer cannot be cancelled once it has been dispatched; receive it at the other end and
send it back.

### Members

**Members** holds the membership records: add, edit, look up and suspend.

Each member record shows their borrowing history, what they currently owe, their home
branch and how they prefer to be contacted.

Members with borrowing history cannot be deleted, because the manager's reports are built
from it. Clear the **active member** tick to suspend them instead.

### Notices

**Notices** shows every message the system has written to a member and whether it reached
them. Useful when someone says they were never told.

Nothing actually leaves the building — this is a simulation. Each message is written to the
application log and recorded here.

**Run the due-date sweep** writes reminders for everything due soon or overdue. In a live
system this would happen overnight; the button is here so it can be demonstrated.

---

## For the administrator

Sign in with the admin account.

### The collection

**Collection** lists everything the library owns. Filter by text, type, status or branch.

- **Add a book** (with the dropdown for music or toys) creates one item. Choose the type
  first; the form then asks only for the fields that type needs.
- **Edit** changes the details.
- **Delete** removes an item that has never been borrowed. Anything with history must be
  marked destroyed instead, so the reports stay accurate.

An item's branch cannot be changed on the edit form. Stock moves through a transfer, so
that the shelves and the records cannot drift apart.

### Branches

**Branches** lists every location with its stock position: how much it holds, how much is
available, what is in transit and how many holds are waiting.

Adding a branch needs a short code (two to six letters, used in URLs and on the kiosk) and
a name. Opening hours appear on the public portal and in the API.

A branch holding stock, with members registered to it, or appearing in the borrowing
history cannot be deleted — clear the **open to the public** tick to close it instead. A
closed branch keeps its catalogue visible but accepts no loans, holds or transfers.

### Importing stock in bulk

**Import** adds many items at once.

**From a CSV file**

1. **Download a template** to get a correctly shaped file.
2. Fill it in, one item per row.
3. Upload it and choose which branch the stock belongs to.
4. Check the preview. Every row gets a verdict: ready, duplicate, or a problem with the
   reason spelled out.
5. Confirm. Only the rows marked ready are written.

Nothing is saved until you confirm, so a bad file costs nothing.

The columns are `LibraryCode, Kind, Name, Description, Creator, Genre, Year, Isbn, Format,
MinimumAge, PieceCount`. `Kind` is Book, Music or Toy. `Creator` is the author or artist.
Toys use `Genre` or `Format` for the toy type. Codes already in the catalogue are skipped,
never overwritten.

**From the metadata provider**

**Search the provider** queries an outside catalogue. Tick the records you want, choose a
branch, and import. Library codes are generated for you.

---

## For the manager

Sign in with the manager account. These screens are read only.

### Reports

**Reports** shows four sets of figures. Use the picker at the top right to read them for
the whole network or one branch.

- **Borrowing**: loans all time and this month, what is out now, what is overdue, average
  days kept, the busiest items and members, and six months of history.
- **Item status**: how the collection splits across available, borrowed, damaged,
  destroyed, in transit and awaiting collection.
- **Fines**: charged, collected and outstanding, with every unpaid fine listed.
- **Across the network**: items on the road, open transfers, holds on the shelf, holds
  nobody collected, and how loans split between branches.

Each section has **CSV** and **PDF** buttons. The export covers whichever branch scope is
selected, and both formats are built from the same figures so they cannot disagree.

### Inventory

**Inventory** puts every branch side by side.

- **Held** is what is physically there, whoever owns it.
- **Visiting** is stock belonging to another branch.
- **Away** is stock this branch owns that is sitting elsewhere.
- **Availability** is the share of what is held that a member could borrow today.

---

## A ten minute walkthrough

Touches every feature. Start with a freshly seeded database.

**As a member of the public** (no sign in)

1. Search the catalogue for `Yothu Yindi`. Narrow the browser to phone width and watch the
   layout fold.
2. Open `/Kiosk`, press **Look up an item** and enter `BK-0004`. It is out on loan.
3. Press **Check my account** and enter `M-1002`. Tom has items out and a fine owing.

**As reception** (`reception@library.test`)

4. **Front desk**: borrow `BK-0001` for Priya Nair. Try to borrow it again — refused.
5. Try to borrow anything for Sione Tupou. Refused: he owes fines from the seeded data.
6. **Holds**: two people are already queued for `BK-0004`.
7. **Front desk → Return an item**: return `BK-0004`. It is late, so a fine appears, and
   because somebody is waiting it goes onto the holds shelf and Priya is notified.
8. **Notices**: that message is now in the log. Press **Run the due-date sweep** and watch
   reminders appear for everything overdue.
9. **Transfers**: one transfer is already in transit from Casuarina. Check it in and watch
   the item join Darwin's shelves.

**As the admin** (`admin@library.test`)

10. **Branches**: open Casuarina and see its stock position.
11. **Import**: download the template, then search the provider for `Dark Emu` and import
    it to Palmerston.
12. **Collection**: filter to Palmerston and find the item you just imported.

**As the manager** (`manager@library.test`)

13. **Reports**: read them for all branches, then switch to Darwin City only.
14. Export the fine revenue audit as a PDF.
15. **Inventory**: compare availability across the three branches.

**The developer API** (no sign in)

16. Open `/api/v1/status` for branch details and opening hours.
17. Open `/api/v1/items?type=Book&availableOnly=true` for the books on the shelf.
