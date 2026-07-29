# Modules/Search — DOX Rail

> Sprint 5 (T4 / Phase 5) — Global search across customers, vendors,
> sales_invoices, and accounts. Returns a flat list of `SearchResultDto`
> with a `type` discriminator so the FE renders a single unified dropdown
> with the right icon per row.

**Last updated:** 2026-07-29 (Sprint 5)

---

## Scope

| File | Role |
|------|------|
| `Application/DTOs/SearchDtos.cs` | `SearchResultDto` (one DTO for all 4 types) |
| `Application/Services/GlobalSearchService.cs` | 4 sub-queries (customers, vendors, invoices, accounts) + merge + cap |
| `Endpoints/SearchController.cs` | `GET /api/search?q=&limit=` |

**Route:** `/api/search` (under `[Authorize(ReadAccess)]`)

---

## Local Contracts (Module Rules)

### Multi-company (per Constitution Article 3)
- **Every** sub-query filters on `company_id = @CompanyId`.
- Empty / unresolved `ICompanyContext.CompanyId` → empty list (200), never 401.
- Per-type cap: 5 rows. Total cap: `limit` (default 20, max 50).

### Relevance ranking (3-tier, per type, applied in SQL)
1. **exact** (1.0) — `lower(col) = lower(@Q)`
2. **prefix** (0.7) — `lower(col) LIKE lower(@Q) || '%'`
3. **contains** (0.4) — `lower(col) LIKE '%' || lower(@Q) || '%'`

`@Q` is the trimmed query. The score is exposed on the DTO for tests and
future ranking tweaks; the FE ignores it and uses the SQL ordering.

### Empty `q`
- Empty / whitespace `q` → empty list, NO DB calls.
- Controller returns 400 for `q.Length > 100`.

### Dapper conventions
- `Id` is read as `Guid?` internally and converted to `string` in
  `Materialize` so both the FakeDb test path and real Postgres work
  (uuid → Guid is Dapper's default mapping; the FakeDb returns the
  raw Guid value from the source table).
- All SQL uses `company_id` (never `tenant_id`).
- Dapper + parameterized queries (no string concatenation of `@Q`).

### Auth
- `[Authorize(Policy = PolicyNames.ReadAccess)]` on the controller.
- The search box is in the top bar, visible to every role.

### Tests
- `src/backend/Tests/ERPSystem.Tests/Search/GlobalSearchServiceTests.cs`
  — 4 happy/error/edge tests (resolved, unresolved, empty `q`, huge limit).
  Plus 1 skipped integration test (requires real Postgres).

---

## Out of Scope
- Pagination (dropdown UX doesn't need it — the total cap truncates).
- Cross-company search (always company-scoped via `ICompanyContext`).
- Full-text search / fuzzy matching (LIKE is enough for V2; revisit if
  customers ask for typo tolerance).
- Search analytics (no query logging in V2).

---

_Last updated: 2026-07-29 by Backend Jimi (Sprint 5, T4)_
