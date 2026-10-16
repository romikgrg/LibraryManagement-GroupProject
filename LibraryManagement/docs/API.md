# Public developer API

A read-only view of the catalogue for outside applications: a council website, a mobile
app, a discovery service.

**Base URL** `https://localhost:7141/api/v1` (or your deployed address)

No authentication. Every endpoint is a `GET`, and there are no write endpoints to call, so
the API cannot change anything regardless of what a caller sends.

---

## Why it looks like this

**Versioned in the path.** `/api/v1/` means a later version can change shape without
breaking callers who built against this one.

**Purpose-built response objects, not entities.** Serialising the database entities
directly would leak borrower names through navigation properties and tie the public
contract to the schema, so that the next migration becomes a breaking API change. The
shapes below are defined in `Models/Api/ApiContracts.cs` and change only when the API
deliberately changes.

**No member data anywhere.** A caller sees the catalogue the way somebody standing in front
of the shelves sees it. Loans, fines and borrowers are not reachable.

---

## `GET /items`

Lists catalogue items.

| Parameter | Type | Default | Notes |
|---|---|---|---|
| `query` | string | — | Free text across titles, creators, descriptions and codes |
| `type` | string | — | `Book`, `Music` or `Toy` |
| `branch` | string | — | Branch code, e.g. `DRW` |
| `availableOnly` | bool | `false` | Only items on the shelf right now |
| `page` | int | `1` | 1-based |
| `pageSize` | int | `25` | Maximum 100 |

```
GET /api/v1/items?query=treaty&availableOnly=true
```

```json
{
  "page": 1,
  "pageSize": 25,
  "totalItems": 1,
  "totalPages": 1,
  "items": [
    {
      "libraryCode": "MU-0001",
      "title": "Treaty",
      "type": "Music",
      "description": "The single that carried Yolngu language onto national radio.",
      "creator": "Yothu Yindi",
      "genre": "CD",
      "year": 1991,
      "status": "Available",
      "available": true,
      "branchCode": "DRW",
      "branchName": "Darwin City"
    }
  ]
}
```

**400** when `page` is below 1, `pageSize` is outside 1–100, `type` is not one of the three
values, or `branch` is not a known code:

```json
{ "message": "type must be Book, Music or Toy." }
```

---

## `GET /items/{libraryCode}`

One item by its code.

```
GET /api/v1/items/BK-0001
```

Returns the same object as a row of `/items`. **404** when no item has that code.

---

## `GET /categories`

The types and genres in use, for building a filter interface.

```json
{
  "types": [
    { "name": "Book", "count": 5 },
    { "name": "Music", "count": 3 },
    { "name": "Toy", "count": 3 }
  ],
  "genres": [
    { "name": "Literary fiction", "count": 2 },
    { "name": "Crime", "count": 1 }
  ]
}
```

---

## `GET /status`

Service and branch information, including opening hours.

```json
{
  "service": "Darwin Community Library",
  "apiVersion": "1.0",
  "generatedAt": "2026-10-11T09:30:00Z",
  "lendingTerms": "Items are due after 14 days. Late returns are charged $0.50 per day up to a maximum of $20.00.",
  "branches": [
    {
      "code": "DRW",
      "name": "Darwin City",
      "address": "Harry Chan Ave, Darwin NT 0800",
      "phone": "08 8930 0300",
      "openingHours": "Mon-Fri 9am-6pm, Sat 10am-4pm",
      "isOpen": true
    }
  ]
}
```

---

## Trying it

In a browser, just open the URL — the responses are JSON and every modern browser formats
them.

```bash
curl https://localhost:7141/api/v1/status

# -k skips certificate checking against the local development certificate
curl -k "https://localhost:7141/api/v1/items?type=Toy"
```

```javascript
const response = await fetch("https://localhost:7141/api/v1/items?availableOnly=true");
const { items } = await response.json();
items.forEach(i => console.log(`${i.title} — ${i.branchName}`));
```

---

## Status codes

| Code | Means |
|---|---|
| 200 | Fine |
| 400 | A parameter was out of range or not understood; the body says which |
| 404 | No item with that library code |

---

## Known limits

Honest about what this does not do yet, since a developer integrating against it would find
out the hard way otherwise:

- **No rate limiting.** A public deployment would need it.
- **No caching headers.** Every request hits the database.
- **Paging reads the whole result set** before slicing it, which is fine at branch scale
  and would need pushing into SQL for a large consortium.
- **No search relevance ranking** — results come back alphabetically, not by how well they
  match.
