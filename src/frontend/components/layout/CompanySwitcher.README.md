# CompanySwitcher — Topbar dropdown (Phase 6.3)

> **Status:** Stable (Phase 6.3, 2026-07-26) — Cycle 5 polish
> **Location:** `src/frontend/components/layout/CompanySwitcher.tsx`
> **Component path:** imported into the topbar in `AppShell.tsx`

---

## What it does

The CompanySwitcher is the **Multi-Company** dropdown in the application topbar.
It lets the user pick which company the `X-Company-Id` HTTP header will route to —
so the same authenticated session can switch between companies the user is
assigned to without re-login.

**Visual location:** Top-right of the topbar, just before the user menu.

```
┌─────────────────────────────────────────────────────────────────────┐
│  [🏢 الفجر  ▾]                                  [🔔]  [👤]          │
│   [الشركة القابضة]                                                 │
└─────────────────────────────────────────────────────────────────────┘
```

> _(Screenshot placeholder — the button shows the company icon, the active
> company name, the "code" or "القابضة" subtitle, and a chevron that
> rotates 180° when the dropdown is open.)_

---

## How it works (UX flow)

1. **On mount**: the component calls `GET /api/auth/me/companies` and
   hydrates the list of companies the user is assigned to (via the
   `user_companies` table, post-Phase 6.1b).
2. **Active company**:
   - If `localStorage.currentCompanyId` is set and matches one of the
     companies → use it.
   - Otherwise → use `defaultCompanyId` from the user's profile (the
     backend sets this on register/login).
   - The chosen id is persisted to `localStorage` if it wasn't already.
3. **On open** (click the button): the dropdown shows all assigned
   companies with their code, holding flag, and default flag. The active
   one has a blue background + check icon.
4. **On select** (click an option):
   - `localStorage.currentCompanyId` is updated.
   - `router.refresh()` reloads the current route so server-side data
     re-renders against the new `X-Company-Id` header.
5. **Outside click** closes the dropdown (no state change).

---

## API contract

| Direction | Endpoint | Purpose |
|-----------|----------|---------|
| `GET` | `/api/auth/me/companies` | List the user's assigned companies (id, name, code, isHolding, isDefault) |

The component **does not** set the `X-Company-Id` header directly. The
`api.ts` Axios client reads `localStorage.currentCompanyId` on every
request and injects the header (see `lib/api.ts:38`).

---

## How to use in code

```tsx
import { CompanySwitcher } from '@/components/layout/CompanySwitcher';

// In your topbar / AppShell
<CompanySwitcher className="ms-2" />
```

The component is **zero-config** — it pulls everything from the
authenticated session. There are no props other than the optional
`className` for layout adjustments.

---

## Multi-Company model (why this exists)

Pre-Phase 6, the system was Multi-Tenant: each user was tied to one
`tenant_id`, and switching tenants required a re-login. **Phase 6**
collapsed the tenant concept entirely:

- Users are **global** (no `tenant_id`).
- Companies are **first-class** entities.
- The `user_companies` table maps users to one or more companies.
- The `X-Company-Id` header + JWT `company_ids[]` claim drive scoping.

The CompanySwitcher is the user-facing surface of that model. It
exists so the same user can work across Holding + subsidiaries without
re-authenticating.

---

## Edge cases handled

| Case | Behavior |
|------|----------|
| No companies assigned (shouldn't happen post-6.1c — every user has Holding) | Renders nothing (no button) |
| `localStorage.currentCompanyId` points to a company the user no longer has | Falls back to `defaultCompanyId` and overwrites localStorage |
| Network error fetching companies | Renders nothing (silent — 401s are caught upstream) |
| Click on the active option | Closes the dropdown, no state change |
| Click outside the dropdown | Closes via `mousedown` listener cleanup on unmount |

---

## Testing

| Test | File | Status |
|------|------|--------|
| Renders with active company name | `e2e/company-switcher.spec.ts` | ✅ Cycle 5 |
| Opens dropdown, shows ≥1 company | `e2e/company-switcher.spec.ts` | ✅ Cycle 5 |
| Selecting a company updates localStorage | `e2e/company-switcher.spec.ts` | ✅ Cycle 5 |

The Playwright test is **optional per DEC-070** — it runs on Playwright CI
but does not block the build.

---

## Related files

| File | Purpose |
|------|---------|
| `src/frontend/components/layout/CompanySwitcher.tsx` | The component itself |
| `src/frontend/components/layout/AppShell.tsx` | Where it's mounted (topbar) |
| `src/frontend/lib/api.ts` | `authApi.getUserCompanies()`, `setCurrentCompanyId()`, `X-Company-Id` header injection |
| `src/backend/Host/Controllers/AuthController.cs` | `GET /api/auth/me/companies` endpoint |
| `docs/PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md` | Full Multi-Company architecture analysis |

---

_Authored in Cycle 5 (2026-07-28) by Mavis Local as part of the
Phase 6.3 + Phase 6.4 polish sprint._
