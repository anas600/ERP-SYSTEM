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

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| [`src/frontend/app/`](./app/) | Routes (App Router) | Active |
| [`src/frontend/components/`](./components/) | Reusable UI components | Active |
| [`src/frontend/lib/`](./lib/) | API client + utilities | Active |
| `src/frontend/e2e/` | Playwright tests (Abdo's team) | Active |

**Per-route AGENTS.md:** When a route segment becomes a durable boundary (e.g., `app/(authenticated)/admin/companies/`), it gets its own AGENTS.md.

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
