# 🎨 AGENTS.md — src/frontend/

> **Frontend (Next.js 14) source.** Read `/AGENTS.md` and `/src/AGENTS.md` first.

<<<<<<< HEAD
**Last updated:** 2026-08-27 (Sprint 63 Wave 3A — RBAC module visibility FE)
=======
**Last updated:** 2026-08-27 (Sprint 64 Wave 3A — DEC-225 + DEC-226 — SubStatement + subcontractor pages)
>>>>>>> 4036cf1 (docs(governance): Sprint 64 Wave 3A - CHANGELOG + AGENTS)

---

## Purpose

The Next.js 14 frontend for ERP-SYSTEM. User-facing pages, components, and API client. Arabic + RTL primary; English fallback.

## Ownership

| Subtree | Owner | Authority |
|---------|-------|-----------|
| `src/frontend/app/` | Jimi تحليلي (Frontend) | Routes (App Router) |
| `src/frontend/components/` | Jimi تحليلي (UI) | Reusable UI components |
| `src/frontend/lib/` | Jimi تحليلي (utils) | API client, helpers, types |
| `src/frontend/e2e/` | Abdo's team | Playwright E2E tests |

Tech Lead (Mavis Local) coordinates. Cloud (Siti) verifies.

## Local Contracts

### Stack
- **Next.js 14** (App Router) / **TypeScript** / **Tailwind** / **shadcn/ui** / **TanStack Query** / **React Hook Form** + **Zod** / **Jest**.

### Internationalization
- **Default locale:** `ar-LY` (Arabic, Libya).
- **Default direction:** `dir="rtl"`.
- **Numbers:** English (1, 2, 3) per Anas's preference.
- **Date format:** Arabic-Indic + Gregorian option.
- **Fonts:** Tajawal or Cairo (Google Fonts).

### API Integration
- **Base URL:** `process.env.NEXT_PUBLIC_API_URL` (defaults to `http://localhost:5001`).
- **Auth header:** `Authorization: Bearer <token>` (JWT).
- **Company header:** `X-Company-Id: <active-company-uuid>`.
- **All requests** must go through `lib/api.ts` (typed client).

### State Management
- **Auth:** Context + httpOnly cookie.
- **Active company:** Context + localStorage.
- **API cache:** TanStack Query.
- **Form state:** React Hook Form + Zod.
- **UI state:** Zustand (per-feature).

### Code Standards
- Components in `PascalCase.tsx`.
- Hooks in `useCamelCase.ts`.
- Types in `types/`.
- One component per file.
- All user-facing strings → AR + EN (in `i18n/`).
- Loading states: `loading.tsx` files in route segments.
- Error states: `error.tsx` files in route segments.

## Work Guidance

### Commands
```bash
cd src/frontend
npm install
npm run dev                       # :3000
npm run build                     # Production build
npm run typecheck                 # tsc --noEmit
npm run lint                      # ESLint
npm run test                      # Jest
```

### Adding a New Page
1. Create `src/frontend/app/(authenticated)/<route>/page.tsx`.
2. Add `loading.tsx` (skeleton) and `error.tsx` (Arabic + English).
3. Add navigation entry in sidebar/header.
4. Use `useCompany()` to get `activeCompanyId`.
5. Pass `X-Company-Id` header on all API calls.

### Adding a New Component
1. Create `src/frontend/components/<Name>/<Name>.tsx`.
2. Add `<Name>.stories.tsx` if using Storybook (optional).
3. Add `<Name>.test.tsx` (Jest + RTL).
4. Document props in `<Name>.mdx` (optional, but encouraged).

## Verification

- [ ] `npm run type-check` — zero errors.
- [ ] `npm run build` — production build succeeds.
- [ ] `npm run lint` — zero errors.
- [ ] `npm run test` — all green.
- [ ] No `tenant_id` in API calls: `grep -r "tenant" src/frontend/`.
- [ ] All user-facing strings have AR + EN.
- [ ] RTL works correctly (test in browser).
- [ ] X-Company-Id header set on all API calls.

<<<<<<< HEAD
## Sprint 63 Wave 3A — RBAC Module Visibility (FE)

**DECs delivered:** DEC-217 (module visibility service for FE), DEC-218 (sidebar + role-aware nav).

**L19 / DEC-095:** Every new FE client (`fetchVisibleModules`, `fetchMyPermissions`) sends **no** userId. The userId is resolved from the JWT by the BE — the axios request interceptor (`api.ts`) attaches the Bearer token, and the BE controllers read `User.FindFirst(ClaimTypes.NameIdentifier)`.

### New BE endpoints (Sprint 63 Wave 3A)

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/me/visible-modules` | Returns `{ modules: ModuleCode[] }` — sorted, deduped list of modules the current user can see. |
| `GET` | `/api/me/permissions` | Returns `{ permissions: string[] }` — sorted, deduped list of permission codes the current user holds. |

The wildcard `admin.all` is seeded on the `admin` role and grants every check via `usePermissions().hasPermission(code)`.

### New FE surface

| File | Purpose |
|------|---------|
| `lib/api/module-visibility.ts` | `fetchVisibleModules()` — typed transport for `/api/me/visible-modules`. |
| `lib/api/permissions.ts` | `fetchMyPermissions()` + `ADMIN_ALL_PERMISSION` constant. |
| `hooks/useVisibleModules.ts` | `useVisibleModules()` — `{ modules, loading, error, refetch }`. |
| `hooks/usePermissions.ts` | `usePermissions()` — `{ permissions, hasPermission, loading, refetch }`. |
| `components/layout/SmartSidebar.tsx` | Module-aware sidebar. Replaces the static `Sidebar` in `AppShell`. |
| `components/layout/PermissionGate.tsx` | Inline `<PermissionGate permission="...">` for create/edit/delete buttons. |
| `tests/components/SmartSidebar.test.tsx` | 5 jest tests: hidden modules, admin-all, loading state, active route highlight, empty state. |
| `tests/hooks/usePermissions.test.tsx` | 4 jest tests: missing/present/admin.all/raw Set + error path. |

### How to gate a button

```tsx
import { PermissionGate } from '@/components/layout/PermissionGate';

<PermissionGate permission="projects.create">
  <Button onClick={openNewProject}>مشروع جديد</Button>
</PermissionGate>
```

### How to check inside a component (programmatic)

```tsx
import { usePermissions } from '@/hooks/usePermissions';

const { hasPermission, loading } = usePermissions();
if (hasPermission('hr.employees.update')) {
  // show inline edit affordance
}
```

### Tests (Sprint 63 Wave 3A — 9 new FE tests)

```bash
cd src/frontend
npm test
```

| Suite | Tests | Covers |
|-------|-------|--------|
| `tests/hooks/usePermissions.test.tsx` | 4 | missing perm, present perm, `admin.all` wildcard, raw Set + error path |
| `tests/components/SmartSidebar.test.tsx` | 5 | hidden modules (HR user), admin sees all 10, loading skeleton, active route highlight, empty-state copy |

**Test runner:** Jest 29 (jsdom) + `@testing-library/react` + `@testing-library/jest-dom`. Config at `jest.config.js`. Run with `npm test`. Filter with `npm test -- SmartSidebar`.

### Anti-patterns (DON'T)

- ❌ Never read `userId` from a URL param / query string / component prop. The BE resolves it from the JWT — always has, always will (L19 / DEC-095).
- ❌ Never use `usePermissions` as the **security** layer. The BE's `[RequirePermission]` attribute is the real authority; the FE Gate is UX-only.
- ❌ Never add `tenant_id` to any new file. Multi-company model uses `company_id` only (Constitution Article 3).

### Files touched in this wave

**New (10):**
- `src/backend/Host/Controllers/ModuleVisibilityController.cs`
- `src/backend/Host/Controllers/MyPermissionsController.cs`
- `src/backend/Tests/ERPSystem.Tests/Identity/ModuleVisibilityControllerTests.cs` (4 tests)
- `src/backend/Tests/ERPSystem.Tests/Identity/MyPermissionsControllerTests.cs` (3 tests)
- `src/frontend/lib/api/module-visibility.ts`
- `src/frontend/lib/api/permissions.ts`
- `src/frontend/hooks/useVisibleModules.ts`
- `src/frontend/hooks/usePermissions.ts`
- `src/frontend/components/layout/SmartSidebar.tsx`
- `src/frontend/components/layout/PermissionGate.tsx`
- `src/frontend/jest.config.js`
- `src/frontend/tests/__mocks__/fileMock.js`
- `src/frontend/tests/setup.ts`
- `src/frontend/tests/components/SmartSidebar.test.tsx`
- `src/frontend/tests/hooks/usePermissions.test.tsx`

**Modified (6):**
- `src/frontend/lib/api-types.ts` — added `ModuleCode`, `VisibleModulesResponse`, `MyPermissionsResponse`.
- `src/frontend/components/layout/AppShell.tsx` — `<Sidebar>` swapped for `<SmartSidebar>`.
- `src/frontend/app/(authenticated)/projects/page.tsx` — wrapped "مشروع جديد" + "إنشاء مشروع جديد" with `<PermissionGate permission="projects.create">`.
- `src/frontend/app/(authenticated)/hr/employees/page.tsx` — wrapped "موظف جديد" with `<PermissionGate permission="hr.employees.create">`.
- `src/frontend/app/(authenticated)/finance/accounts/page.tsx` — wrapped "حساب جديد" with `<PermissionGate permission="finance.accounts.create">`.
- `src/frontend/package.json` — added `test`/`test:watch` scripts + Jest/RTL/babel deps.
=======
---

## Sprint 64 / Wave 3A (DEC-225 + DEC-226) — Subcontractor Module FE (2026-08-27)

> Worker 3A delivered the SubStatement visual layer + 5 subcontractor pages.

### Subcontractor pages (`app/(authenticated)/projects/[id]/subcontractors/`)

| Route | الغرض |
|-------|-------|
| `page.tsx` | قائمة المقاولين الفرعيين على المشروع (مع filter بالاسم/الكود/التخصص) |
| `[subId]/page.tsx` | تفاصيل المقاول: بيانات أساسية + قائمة عقوده + 4 stat cards (cross-contract summary) |
| `[subId]/contracts/page.tsx` | قائمة عقود الباطن لهذا المقاول على هذا المشروع |
| `[subId]/contracts/[contractId]/page.tsx` | تفاصيل العقد: SubStatement (main visual) + المستخلصات + المدفوعات |
| `[subId]/contracts/new/page.tsx` | نموذج عقد جديد (مع pre-selected subcontractor من URL) |
| `contracts/new/page.tsx` | نموذج عقد جديد على مستوى المشروع (subcontractor من dropdown) |

### Subcontractor components (`components/subcontractor/`)

- `SubStatement.tsx` — visual P&L with health badge (OK / OVERDUE / SETTLED)
- `SubcontractorCard.tsx` — list-page card with trade pill + contact info
- `SubcontractorForm.tsx` — create/edit form with hand-rolled validation
- `SubContractForm.tsx` — sub-contract create form (with subcontractor dropdown)

### API client (`lib/api/subcontractors.ts`)

Single typed client that wraps the entire Subcontractor module's surface:
subcontractors (CRUD), sub-contracts (CRUD), progress billings (CRUD), payments
(CRUD + retention release), and SubStatement (2 endpoints).

Uses the existing axios `api` instance — so every request automatically carries
the JWT (Authorization) and active company id (X-Company-Id) via the interceptor
in `lib/api.ts`. L19 / DEC-095: CompanyId is never sent in the request body.

### L19 / DEC-095 applied
- `CompanyId` is never in any request body or DTO
- All requests go through `api` (axios) which auto-attaches the JWT
- SubStatement response does NOT include `CompanyId` (the caller knows it from JWT)

### Test infrastructure gap (READ BEFORE RUNNING `npm run test`)
The `tests/` directory contains 4 RTL tests for `SubStatement.test.tsx`, but
the **Jest + RTL stack is not installed** in this repository (the `package.json`
has no `test` script and no `jest` dependency). The Worker contract claimed
Sprint 63 Wave 3A set this up, but that setup was not committed to this branch.

**To enable the tests** (one-time, after Wave 3A):
```bash
npm install --save-dev jest @testing-library/react @testing-library/jest-dom \
  jest-environment-jsdom ts-jest @types/jest
# add jest.config.js + jest.setup.js
# add "test": "jest" to package.json scripts
# add tsconfig test types
```

Until then, `tsconfig.json` excludes `tests/` from the main `tsc --noEmit`
check, so the build still passes. The test files are runnable as-is once
the stack is installed.
>>>>>>> 4036cf1 (docs(governance): Sprint 64 Wave 3A - CHANGELOG + AGENTS)

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`src/frontend/app/`](./app/) | Routes (App Router) | Active |
| [`src/frontend/components/`](./components/) | Reusable UI components | Active |
| [`src/frontend/lib/`](./lib/) | API client + utilities | Active |
| `src/frontend/hooks/` | Custom React hooks (Sprint 63+) | Active |
| `src/frontend/tests/` | Jest + RTL test suite (Sprint 63+) | Active |
| `src/frontend/e2e/` | Playwright tests (Abdo's team) | Active |

**Per-route AGENTS.md:** When a route segment becomes a durable boundary (e.g., `app/(authenticated)/admin/companies/`), it gets its own AGENTS.md.

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
