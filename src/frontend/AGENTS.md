# 🎨 AGENTS.md — src/frontend/

> **Frontend (Next.js 14) source.** Read `/AGENTS.md` and `/src/AGENTS.md` first.

**Last updated:** 2026-07-29 (DOX framework applied)

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

- [ ] `npm run typecheck` — zero errors.
- [ ] `npm run build` — production build succeeds.
- [ ] `npm run lint` — zero errors.
- [ ] `npm run test` — all green.
- [ ] No `tenant_id` in API calls: `grep -r "tenant" src/frontend/`.
- [ ] All user-facing strings have AR + EN.
- [ ] RTL works correctly (test in browser).
- [ ] X-Company-Id header set on all API calls.

## Jimi Scope — 2026-07-29 (T4 FE Polish)

**Jimi type:** FE
**Sprint / Cycle:** Sprint 6 Wrap-up / Cycle 2
**T# tasks:** T4 (FE Polish)
**Branch:** `feature/sprint-6-wrap-up` (off `origin/develop @ 7943f68`)

**Files I will touch:**
- `src/frontend/app/(authenticated)/**/page.tsx` — fix `react-hooks/exhaustive-deps` warnings (the most common `load` missing dep pattern) in ~30+ files. Use `useCallback` to stabilize function references; never use `eslint-disable`.
- `src/frontend/app/(authenticated)/**/loading.tsx` — add skeletons (not spinners) for top-level routes missing them. AR + EN bilingual.
- `src/frontend/app/(authenticated)/**/error.tsx` — add bilingual error boundary where missing. AR + EN.
- `src/frontend/lib/errors.ts` — new file: bilingual error helper extracted from the `load` pattern (single source of truth for AR/EN messages used by `loading.tsx`/`error.tsx`).
- `src/frontend/components/ui/*` — add `aria-label` to icon-only buttons where missing (search, close, etc.).
- `CHANGELOG.md` — append to existing Sprint 6 entry.

**Files I will NOT touch:**
- `WORKFLOW.md`, `CONSTITUTION.md`, `CONSTITUTION-LOCAL-TEAM.md`, `INTER-TEAM-PROTOCOL.md` (governance, Anas-only)
- `src/backend/**` (BE Jimi's scope)
- `.github/workflows/mavis-coordination/state.json` (Mavis Local's job, never the Jimis')
- `src/frontend/e2e/**` (Abdo's team)

**Tests I will add:**
- No new component tests this sprint (Sprint 7+ candidate). The task is polish, not features.
- Verify with `npx tsc --noEmit` (0 errors) and `npx next build` (success).

**Constitution articles I must respect:**
- Article 3 (company_id only — no tenant_id anywhere)
- Article 8 Rule 9 (Frontend-First Errors — AR + EN)
- Article 8 Rule 10 (Document in AGENTS.md — this block IS that)
- Article 11 (One Test Per Endpoint — N/A, this is FE-only polish)

**Baseline (before this Jimi starts):**
- 40 `react-hooks/exhaustive-deps` warnings in `next build`
- 0 TypeScript errors
- 0 Next.js build errors
- 105 `page.tsx` files; 30 `loading.tsx`; 1 `error.tsx` (only at root of `(authenticated)`)
- 0 `lib/errors.ts` (task says "created in Sprint 4" but it doesn't exist — creating it now)

**Target (after this Jimi finishes):**
- ≤ 19 warnings (50%+ reduction)
- 0 new warnings introduced
- 0 TS errors
- 0 build errors
- All top-level authenticated routes have `loading.tsx` + `error.tsx`

---

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`src/frontend/app/`](./app/) | Routes (App Router) | Active |
| [`src/frontend/components/`](./components/) | Reusable UI components | Active |
| [`src/frontend/lib/`](./lib/) | API client + utilities | Active |
| `src/frontend/e2e/` | Playwright tests (Abdo's team) | Active |

**Per-route AGENTS.md:** When a route segment becomes a durable boundary (e.g., `app/(authenticated)/admin/companies/`), it gets its own AGENTS.md.

---

_Last updated: 2026-07-29 by FE Jimi (Sprint 6 T4) — Jimi scope block added; baseline 40 → target ≤ 19 warnings_
