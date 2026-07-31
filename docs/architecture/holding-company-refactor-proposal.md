# 🏛️ Holding Company Refactor Proposal

> **Per Anas mandate 2026-07-31 04:55 UTC (محمد mode, Strategic Advisor).**
> An architectural review of the Holding Company model, identifying technical debt and proposing a refactor roadmap.

**Author:** Mavis (محمد mode) — Strategic Advisor
**Date:** 2026-07-31
**Status:** 🟡 PROPOSAL (awaiting Anas's approval to proceed)
**Scope:** Backend C# + DB schema + docs alignment
**Priority:** 🟡 MEDIUM (not blocking sprints, but blocks future scaling)

---

## 🎯 Why this refactor

The architecture document `docs/architecture/holding-company-architecture.md` (v1.0, 2026-07-29) describes a **two-table model**:
- `holdings` table (one row, the canonical Holding)
- `companies` table (1:N, with `holding_id` FK)

But the **actual code** uses a **single-table self-referencing model**:
- `companies` table with `parent_company_id` (self-FK) + `is_group` flag
- The Holding is a Company row with `is_group=true` AND `parent_company_id IS NULL`

This divergence creates **5 distinct technical debts** that block clean future scaling. Below is the analysis + refactor plan.

---

## 🔍 Findings (Architectural Debt)

### Finding #1 — Holding as separate entity vs self-referencing (CRITICAL)

**Architecture doc says:**
```sql
CREATE TABLE holdings (
  id UUID PRIMARY KEY...
  name TEXT...
  base_currency CHAR(3)...
);
-- + companies table with holding_id FK
```

**Actual code (JSON schema in `src/backend/Host/data-types/companies.json`):**
```json
{
  "table": "companies",
  "fields": [
    { "name": "id", ... },
    { "name": "code", ... },
    { "name": "name", ... },
    { "name": "parent_company_id", "type": "uuid", "nullable": true,
      "foreign_key": { "table": "companies", "column": "id" } },
    { "name": "is_group", "type": "boolean", "default": "false" }
  ]
}
```

**C# entity (`src/backend/Modules/Companies/Entities/Company.cs`):**
```csharp
public class Company {
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Slug { get; set; }
    public string? LegalName { get; set; }
    public Guid? ParentCompanyId { get; set; }  // ← self-ref
    public bool IsGroup { get; set; } = false;  // ← Holding flag
    public string BaseCurrency { get; set; } = "LYD";
    // NO holding_id, NO Holding navigation property
}
```

**Impact:**
- The architecture doc is **stale** — describes a model that doesn't exist in the code.
- All code treats the Holding as a "Company with `is_group=true`" (see `GetHoldingCompanyIdAsync` in `ICompanyRepository`).
- The seed bootstrap (`DefaultHoldingBootstrapHostedService`) creates a company row with `code='000'`, `is_group=true`, `parent_company_id=null`.
- **If anyone follows the architecture doc literally, they'll create a `holdings` table that no code reads/writes.**

---

### Finding #2 — `Companies/AGENTS.md` says `holding_id` but actual schema has `parent_company_id`

**`src/backend/Modules/Companies/AGENTS.md`:**
```markdown
### Schema
- `companies` — `id`, `holding_id` (FK), `name`, `name_ar`, `legal_name`, `tax_id`, `currency`, `is_active`.
- **Constraint:** `UNIQUE (holding_id, name)`.
- **Each company has exactly ONE `holding_id`.**
```

**Actual code (Company entity + JSON):**
- `parent_company_id` (not `holding_id`)
- `ix_companies_code` UNIQUE on `code` only (not `holding_id, name`)
- No `name_ar` field on `Company`
- No `tax_id` field on `Company`
- Has `slug` field (added Sprint 1) — not in AGENTS.md
- Has `base_currency` field (not in AGENTS.md)
- Has `is_group` flag (not in AGENTS.md)
- Has `legal_name` (matches)

**Impact:** **Major doc-vs-code drift.** A new developer reading `AGENTS.md` will:
- Look for `holding_id` → not found → confused
- Look for `name_ar` → not found → confused
- Apply a migration adding `holding_id` FK → breaks the existing self-ref model
- Not know about `is_group` flag

---

### Finding #3 — `Shared/MultiTenancy/` folder name is misleading (KNOWN ISSUE)

**AGENTS.md already flagged this:**
> ⚠️ **MISLEADING FOLDER:** `src/backend/Shared/MultiTenancy/` contains `CompanyContext.cs` files. **Folder name should be renamed to `CompanyContext/`** in a future refactor (out of scope for current sprints).

**Files in the folder (28 files reference this namespace):**
- `src/backend/Shared/MultiTenancy/CompanyContext.cs`
- `src/backend/Shared/MultiTenancy/ICompanyContext.cs`
- `src/backend/Shared/MultiTenancy/CompanyContextMiddleware.cs`

**Impact:**
- New developers see `MultiTenancy` and assume multi-tenancy is in use.
- Article 3 is violated in spirit (folder name suggests tenant model).
- Search for "MultiTenancy" returns `CompanyContext` (confusing).

---

### Finding #4 — `CompanyContext` uses `AsyncLocal` instead of per-request DI scope

**Current implementation (`src/backend/Shared/MultiTenancy/CompanyContext.cs`):**
```csharp
private static readonly AsyncLocal<CompanyHolder> _holder = new();

public Guid? CompanyId => _holder.Value?.CompanyId;
public Guid? UserId => _holder.Value?.UserId;
```

**Why this is a debt:**
1. **Static state** — testing requires `Clear()` calls in fixtures.
2. **AsyncLocal is per-execution-context** — works for typical request flow, but breaks for:
   - Background jobs (HostedServices) that have no HTTP context
   - Parallel async operations (e.g., `Task.WhenAll` with scoped data)
   - Test harnesses that run in different threads
3. **No DI awareness** — the middleware `Set()` / `Clear()` is a manual lifecycle, not enforced by the DI container.
4. **Already-flagged in seed data tests** — see `SeedDebugState.cs` for a workaround.

**Modern alternative:** Use `IHttpContextAccessor` + `IServiceScope` per request, or use the existing `Activity.Current` / `IUserContext` (if introduced).

---

### Finding #5 — Mixed naming conventions across layers

| Layer | Convention | Example |
|-------|-----------|---------|
| DB columns (JSON) | snake_case | `parent_company_id`, `is_group`, `is_active` |
| C# properties | PascalCase | `ParentCompanyId`, `IsGroup`, `IsActive` |
| SQL queries | snake_case + aliases | `a.user_id AS UserId` |
| JSON DataType | mixed | `data-types/companies.json` uses snake_case, but DTOs use PascalCase |
| URLs (REST) | kebab-case | `/api/holdings/{slug}` |
| File names | PascalCase | `CompanyService.cs`, `ICompanyRepository.cs` |

**Impact:** Mild but cumulative. New developers have to mentally translate between layers. The Sprint 8 T2 work (FakeDb AS alias enhancement) hit this exact issue — tests add columns in PascalCase, SQL references in snake_case, requires special handling.

---

## 🛠️ Refactor Plan (3 phases)

### Phase 1 — Documentation alignment (LOW RISK, 1-2 days)

**Goal:** Make docs match code so the team can trust them.

| # | Action | File | Effort |
|---|--------|------|--------|
| 1.1 | Rewrite `Companies/AGENTS.md` to match actual schema (parent_company_id, is_group, slug, base_currency) | `src/backend/Modules/Companies/AGENTS.md` | 1h |
| 1.2 | Update `docs/architecture/holding-company-architecture.md` Section 5 + 7 to reflect the self-ref model (NOT separate `holdings` table) | `docs/architecture/holding-company-architecture.md` | 2h |
| 1.3 | Add ERD diagram showing the self-ref model (Holding = company where `is_group=true AND parent_company_id IS NULL`) | `docs/architecture/holding-company-architecture.md` | 1h |
| 1.4 | Document the "Holding is a Company" convention as the canonical rule (not a separate entity) | `src/backend/Modules/Companies/AGENTS.md` | 0.5h |

**Risk:** Zero — docs only.

---

### Phase 2 — Rename `Shared/MultiTenancy/` → `Shared/CompanyContext/` (LOW RISK, 2-3 days)

**Goal:** Eliminate the misleading folder name + namespace.

| # | Action | Effort |
|---|--------|--------|
| 2.1 | Move 3 files to `src/backend/Shared/CompanyContext/` | 0.5h |
| 2.2 | Update `namespace ERPSystem.Shared.MultiTenancy;` → `namespace ERPSystem.Shared.CompanyContext;` in those 3 files | 0.5h |
| 2.3 | Find/replace `using ERPSystem.Shared.MultiTenancy;` → `using ERPSystem.Shared.CompanyContext;` across the 28 referencing files | 2h |
| 2.4 | Re-run all tests (must still pass) | 1h |
| 2.5 | Commit as `refactor(be): rename Shared/MultiTenancy → Shared/CompanyContext (align with Article 3)` | 0.5h |

**Risk:** Low — pure rename, no behavior change. CI catches any missed reference.

---

### Phase 3 — Replace `AsyncLocal` with scoped DI (MEDIUM RISK, 1 week)

**Goal:** Make CompanyContext properly scoped to the request via DI instead of static state.

**Current pattern (AsyncLocal + middleware Set/Clear):**
```csharp
public sealed class CompanyContext : ICompanyContext
{
    private static readonly AsyncLocal<CompanyHolder> _holder = new();
    public Guid? CompanyId => _holder.Value?.CompanyId;
    public void Set(Guid companyId, Guid userId, ...) { _holder.Value = ... }
    public void Clear() => _holder.Value = null!;
}
```

**Proposed pattern (scoped DI):**
```csharp
public sealed class CompanyContext : ICompanyContext
{
    private readonly IHttpContextAccessor _http;
    public CompanyContext(IHttpContextAccessor http) => _http = http;
    public Guid? CompanyId => _http.HttpContext?.Items["CompanyId"] as Guid?;
    public bool IsResolved => CompanyId.HasValue && UserId.HasValue;
    // No Set/Clear — middleware writes to HttpContext.Items
}

public sealed class CompanyContextMiddleware
{
    public async Task InvokeAsync(HttpContext context, ICompanyContext ctx)
    {
        // Resolve + write to context.Items
        if (guidFound) context.Items["CompanyId"] = guid;
        await _next(context);
        // No Clear needed — items scoped to request
    }
}
```

**Migration path:**
1. Add `IHttpContextAccessor` to DI (already in ASP.NET Core).
2. Make `ICompanyContext` a scoped service.
3. Middleware writes to `HttpContext.Items` instead of `AsyncLocal`.
4. Service reads from `_http.HttpContext?.Items`.
5. Remove `Set`/`Clear` from interface (breaking change, but internal).
6. Update 28 referencing files (mostly constructor injection changes).
7. Update tests to use a mock `IHttpContextAccessor` instead of `CompanyContext.Clear()`.

**Risk:** Medium — changes a core contract. Requires:
- Coordination with all 28 referencing files
- Test rewrite for any test that calls `companyContext.Set(...)` directly
- Possibly a feature flag for staged rollout

**Benefit:** Removes the static state fragility, plays nice with `Task.WhenAll`, easier to test, works with `BackgroundService` (uses `IServiceScopeFactory`).

---

## 📅 Recommended Sprint Plan

| Sprint | Phase | Effort | Risk |
|--------|-------|--------|------|
| **Sprint 9** | Phase 1 (docs) | 1-2 days | Zero |
| **Sprint 10** | Phase 2 (rename MultiTenancy) | 2-3 days | Low |
| **Sprint 11+** | Phase 3 (scoped DI) | 1 week | Medium (split into 2 sub-sprints) |

**Alternative:** Skip Phase 3 (accept the AsyncLocal debt) and only do Phases 1+2. The AsyncLocal works for the current single-deployment-per-Holding model — the debt only matters when we add background jobs or parallel request handling.

---

## 🚦 Decision Required

| Option | Scope | Effort | Recommendation |
|--------|-------|--------|----------------|
| **A** | All 3 phases (full refactor) | ~2 weeks | ⭐ Recommended — clean slate before next major feature |
| **B** | Phases 1+2 only (docs + rename) | ~1 week | Acceptable — fixes the doc drift, leaves the AsyncLocal for later |
| **C** | Phase 1 only (docs) | 1-2 days | Minimal — buy time, defer the rest |
| **D** | No refactor (status quo) | 0 | ❌ Not recommended — doc drift grows, AsyncLocal fragility bites later |

---

## 📎 Risks of Doing Nothing

1. **New developers will be confused** by the doc-vs-code mismatch → onboarding cost grows.
2. **Misleading folder name** violates Article 3 spirit (suggests Multi-Tenant).
3. **AsyncLocal fragility** will surface when we add background jobs (e.g., nightly consolidation reports).
4. **Architectural debt compounds** — every sprint adds more code on top of the inconsistent base.

---

## 🤝 Communication to the Team

Per the v2.0 governance:
- **Admin Team (Admin)**: Verify the proposal + propose sprint hand-offs
- **Local Team (me, taking Coordinator role)**: Execute the chosen phases
- **Anas (Owner)**: Final decision on scope (A/B/C/D) + amendment to constitution if Phase 3 is approved

---

## 🪧 Related Files

- `docs/architecture/holding-company-architecture.md` — current (stale) doc
- `src/backend/Modules/Companies/AGENTS.md` — current (stale) doc
- `src/backend/Modules/Companies/Entities/Company.cs` — actual entity
- `src/backend/Host/data-types/companies.json` — actual JSON schema
- `src/backend/Shared/MultiTenancy/` — misleading folder
- `src/backend/Shared/MultiTenancy/CompanyContext.cs` — AsyncLocal implementation
- `AGENTS.md` (root) — flags the misleading folder as known issue

---

_Author: Mavis (محمد mode) — Strategic Advisor_
_Date: 2026-07-31_
_Status: 🟡 PROPOSAL — awaiting Anas's decision_
