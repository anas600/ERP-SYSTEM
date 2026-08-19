# Sprint 41+ Plan: UI Exploration — 3 Frontend Versions

**Status:** DRAFT — pending Anas approval
**Created:** 2026-08-06
**Owner:** Muhammad (planning) + Admin (execution)
**Mode:** 1 (development) — Mode 2 postponed until UI is approved

## Background

Anas has flagged multiple UI/UX pain points while browsing the live system (Sprint 40). Key issues:
- Chart of accounts (CoA) tree view: folder icons don't actually expand/collapse — `flattenTree()` ignores `expanded` state
- Add-child account form: FK violation because `ChartOfAccountsService.CreateAsync` doesn't inject `ICompanyContext` and writes `CompanyId = Guid.Empty`
- Form UX is primitive: no proper focus states, no friendly error mapping, raw SQL error leaked to user
- Generic "list page + modal" pattern feels dated; sidebar nav is rigid

Rather than patching the current stack, Anas wants **3 radically different frontend redesigns** running simultaneously, so he can compare and pick.

## Guiding Principles

1. **Same backend** — all 3 versions consume `http://localhost:5001` (BE unchanged)
2. **Same scope** — each version covers at least: login, dashboard, accounts (CoA + tree), sales invoices, receipts, customers, vendors, items, POs/GRs/Bills, HR, projects, posting rules
3. **Different stack / philosophy** — each version is a distinct approach, not a theme swap
4. **Different port** — so user can browse all 3 side-by-side
5. **Production-quality** — no demo-grade code, no mock data, real Sprint 40 data

## Version Stacks

### Version A — "Modern ERP" (current stack, polished)
- **URL:** http://localhost:3001
- **Stack:** Next.js 14 + Tailwind 3.4 + shadcn/ui + Lucide + Framer Motion
- **Design philosophy:** Calm authority. Soft brand colors, generous whitespace, side-by-side panels, smooth micro-interactions
- **Key features:**
  - Sidebar collapse (icon-only mode)
  - Real tree view (TanStack Table tree or shadcn Tree)
  - Floating-label forms with inline validation
  - Toast + ConfirmDialog everywhere (L66)
  - Full RTL Arabic + light/dark mode
- **Best for:** Production continuity, low risk

### Version B — "DataDesk Pro" (power user / high-volume)
- **URL:** http://localhost:3002
- **Stack:** Next.js 14 + AG-Grid Community + Radix UI + Zustand + TanStack Query
- **Design philosophy:** High density. Bloomberg/Linear inspired. Dark mode default. Monospace numbers.
- **Key features:**
  - Sidebar tree navigation (module + records)
  - AG-Grid with grouping/pivot/virtualization (handle 10k+ rows)
  - Inline editable cells (no modals)
  - Ctrl+K command palette (search actions, jump to entity)
  - Full keyboard navigation
  - Minimal animations (instant feedback)
- **Best for:** Accountants, daily high-volume data entry

### Version C — "CardFlow" (consumer-grade)
- **URL:** http://localhost:3003
- **Stack:** Next.js 14 + ShadCN + Radix UI + Framer Motion + Tremor
- **Design philosophy:** Light & airy. Vercel/Linear/Stripe inspired. Lots of white space. Pastel accents.
- **Key features:**
  - Card-based hierarchies (no tables by default)
  - Step-by-step wizards for multi-step forms
  - Smooth bouncy animations (every interaction has motion)
  - Monospace numbers for amounts
  - Light mode default + dark mode toggle
  - Master/detail pattern (split view)
  - Prominent search/filter
- **Best for:** Managers, decision-makers, occasional users

## Critical P0 Fixes (Phase 0 — independent of 3 versions)

These must be fixed **before** building the 3 versions, so the demo data is clean and the same fixes apply to all 3.

### Fix 1: FK violation on account creation
- **File:** `src/backend/Modules/Finance/Application/Services/ChartOfAccountsService.cs`
- **Issue:** `CreateAsync` does not inject `ICompanyContext`; `Account.CompanyId` defaults to `Guid.Empty`
- **Fix:** Inject `ICompanyContext` and set `acc.CompanyId = _companyContext.CompanyId`
- **Pattern reference:** `AccountService.cs` (DEC-116 in Sprint 34)
- **Effort:** 30 min

### Fix 2: Tree view doesn't filter children
- **File:** `src/frontend/app/(authenticated)/finance/accounts/page.tsx:121-129`
- **Issue:** `flattenTree(roots)` walks all children regardless of `expanded` state
- **Fix:** `flattenTree(roots, expanded: Set<string>)` — only walk children whose parent is in `expanded`
- **Effort:** 15 min

### Fix 3: Friendly FK error message
- **File:** BE exception handler middleware + FE error display
- **Issue:** Raw SQL error "fk_accounts_company_id" leaks to user
- **Fix:** Map FK violations to friendly Arabic message ("لا يمكن إكمال العملية — بيانات الشركة مطلوبة")
- **Effort:** 1h

## Sprint Sequence

| Sprint | Scope | Effort | Output |
|--------|-------|--------|--------|
| **41** | Phase 0: 3 P0 bug fixes (single commit, on `feature/sprint-41-p0-fixes`) | 2h | System bug-free baseline |
| **42** | Version A scaffold + 3 critical pages (login + dashboard + CoA) | 2h | http://localhost:3001 browseable |
| **43** | Version A full — 12 more pages | 2h | Version A complete |
| **44** | Version B scaffold + 3 critical pages | 2h | http://localhost:3002 browseable |
| **45** | Version B full — 12 more pages | 2h | Version B complete |
| **46** | Version C scaffold + 3 critical pages | 2h | http://localhost:3003 browseable |
| **47** | Version C full — 12 more pages | 2h | Version C complete |
| **48** | User picks + retrospective + decision on which version to refine | 1h | Decision recorded |
| **49** | Refine picked version (Sprint +X) | 2h | Polished version |
| **50** | Finalize: design tokens extracted, docs, prepare for Mode 2 | 2h | Ready for "ادفع" |
| 51+ | **Mode 2** — push 50+ commits to remote in batched PRs | — | CI green + tags + Telegram |

**Total:** ~20h, 10 sprints. Can be parallelized with 3 Jimis (one per version) for Sprints 42-47.

## Parallelization Plan (Sprints 42-47)

- **Jimi A** (BE-adjacent FE): Builds Version A on port 3001, refines existing components
- **Jimi B** (new stack): Builds Version B on port 3002, AG-Grid + Radix UI integration
- **Jimi C** (new stack): Builds Version C on port 3003, Vercel-style cards + animations
- **Max 3 parallel Jimis** (per L constraint). Each owns one port, one design system, one set of pages.
- **FE wins on type conflicts** (per project convention). All 3 must share `lib/api.ts` types (auto-generated from swagger.json).

## Verification Per Version

Per version, before "done":
- [ ] `npm run typecheck` — 0 errors
- [ ] `npm run build` — production build succeeds
- [ ] Playwright smoke: login + dashboard + 5 key pages render, 0 JS errors
- [ ] Real data populated (52 accounts, 10 POs, etc.) — no mocks
- [ ] All 3 versions running simultaneously without port conflicts
- [ ] User can switch between http://localhost:3001/3002/3003 in different browser tabs

## Decision Criteria (When User Picks)

| Criterion | Weight |
|-----------|--------|
| Speed of common tasks (create invoice, post receipt) | 30% |
| Visual polish + consistency | 25% |
| Information density + screen real estate | 15% |
| Keyboard-friendly / power-user features | 10% |
| RTL Arabic rendering quality | 10% |
| Code maintainability (we own it after pick) | 10% |

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| 3 versions × 15 pages = 45 pages in 6 sprints is too aggressive | Reduce scope to 8 critical pages per version (login, dashboard, accounts, sales, receipts, customers, items, POs) — sprint count 42-47 → 42-45 |
| AG-Grid Community license is restrictive | AG-Grid Community is MIT-licensed for non-Enterprise features (grouping/pivot are Enterprise; skip them) |
| 3 separate Next.js apps = 3× deps to maintain | Share a common lib via npm/pnpm workspace; each app has its own components/ |
| Browser memory: 3 dev servers + 1 BE | 8GB+ RAM. Anas already has 16GB+. Should be fine. |
| User's laptop was locked when he came back | Servers should auto-restart. Add to AGENTS.md post-sprint. |

## Out of Scope (For These Sprints)

- BE changes beyond P0 fixes
- New features (only UI redesign)
- Mobile-first redesign (responsive is nice-to-have, not required)
- i18n (Arabic only, English fallback only)
- Migration of existing data (uses same DB)

## Open Questions (for Anas)

1. **Scope per version** — should I cover all 15 modules, or focus on 8 critical ones (login + dashboard + accounts + sales + receipts + customers + items + POs)?
2. **Demo data** — keep current Sprint 40 seed (52 accounts, etc.), or use a fresh minimal dataset?
3. **Version naming** — keep A/B/C internally, but what to call them in the URL? `/v1`, `/v2`, `/v3` or names like `/modern`, `/datadesk`, `/cardflow`?
4. **Parallelization** — run 3 Jimis in parallel for Sprints 42-47 (fast, 3x compute), or sequence them (slower, easier to review)?
5. **Mvp-docker rebuild** — should we fix the port 3000 conflict (the root cause of the daily rebuild failure), or just keep mvp-docker broken since we use local FE?
