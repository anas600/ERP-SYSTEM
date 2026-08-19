# Sprint 39 — UI/UX Overhaul + Tax Optional Enforcement (DEC-125)

**Date:** 2026-08-05
**Branch:** `feature/sprint-39-ui-ux-overhaul-vat-optional`
**Parent:** Sprint 38 (f95486d) — L19 service-layer security fix
**Tag (planned):** `v1.0.14-sprint39-ui-ux-tax-optional`
**Status:** ✅ DONE (LOCAL-ONLY, awaiting "ادفع")

## Goal (Anas's directive)

> "في ليبيا لا نطبق الضريبة بشكل افتراضي. اجعل دائماً أن اختيار الضريبة اختيارياً. ركز على استخدام واجهة منسقة وجميلة وسلسة. هناك بق كثيرة في الواجهات ركز عليها. Playwright مهم. أنتظر أن يكون هذا الـ sprint كبيراً."

**Three pillars:**
1. **Tax is OPT-IN** — every invoice lets the user choose whether to apply 5% VAT
2. **UI/UX overhaul** — modern, beautiful, smooth design system
3. **Playwright sweep** — 50+ pages, catch UI bugs that API tests miss

---

## What shipped

### Pillar 1: Tax Opt-in (Anas's #1 request)
- **Sprint 35 BE** (separate branch `feature/sprint-35-vat5-workflows`) added `useVat5` + `IsVatRule` to sales invoices + posting rules
- **Sprint 39 FE** — `/finance/sales-invoices/new`:
  - New "تطبيق ضريبة القيمة المضافة 5% (VAT)" toggle with "اختياري" badge
  - **OFF by default** (per Libyan rule)
  - Per-line taxRate column hidden when ON (engine applies 5% globally)
  - Tax row in summary card appears only when ON
  - Save & Post button label updates: "Dr 1230 / Cr 5110" → "Dr 1230 / Cr 5110 / Cr 1411"
- **API client** — `createInvoice` payload includes `useVat5?: boolean` (FE-only flag, BE will support when Sprint 35 is merged)

### Pillar 2: Design System Overhaul (DEC-125)
- **Design tokens** (in `tailwind.config.js` + `app/globals.css`):
  - Color palette: brand (50→950), success/warning/danger (50/100/500/600/700), ink (50→900) for neutrals
  - Spacing: added 18, 88, 112, 128
  - Border radius: xs/sm/md/lg/xl/2xl/3xl
  - Shadows: soft-sm, soft, soft-md, soft-lg, soft-xl, focus-brand
  - Animations: fade-in, slide-up/down, scale-in, pulse-soft, shimmer
  - Custom scrollbar, focus-visible outlines, pageEnter animation
- **UI Components** (all updated to use ink-* tokens):
  - **Button** — 6 variants (primary/secondary/danger/success/ghost/outline) × 4 sizes (xs/sm/md/lg) + active:scale-[0.98] press feedback
  - **Card** — `interactive` prop (hover lift), `noPadding` prop, accent colors
  - **Input** — 3 sizes, error/hint states, icon support
  - **Select** — uses new ink-* tokens
  - **Badge** — `brand` variant added, semantic color tokens
  - **Table** — new ink-* colors, hover transitions
  - **PageHeader** — new ink-* colors
  - **EmptyState** — gradient backdrop, brand-50 icon circle
  - **LoadingSkeleton** — uses `shimmer` class for modern sweeping effect
  - **Modal/ConfirmDialog** — new ink-* tokens, scale-in animation
  - **Toast** — uses semantic colors (success-50, danger-50, brand-50)
- **AppShell**:
  - Gradient brand-50 sidebar active state
  - Gradient avatar (from-brand-500 to-brand-700) in topbar user menu
  - User menu redesigned with shadow-soft-lg + scale-in animation
  - "الملف الشخصي" link added to user menu
  - Updated version display: "v1.0.12 · Sprint 39"
- **Login page** — full redesign:
  - Brand gradient background with decorative blur shapes
  - Glassmorphism card (backdrop-blur-xl)
  - Gradient brand logo with Sparkles icon
  - Toast on successful login
  - Form uses Input + Button components

### Pillar 3: Bulk UI Bug Fixes
- **scripts/bulk-update-error-box-colors.py** — replaced 330 occurrences of `bg-red-50` / `border-red-200` / `text-red-700` with `bg-danger-50` / `border-danger-200` / `text-danger-700` across **82 files**
- **scripts/bulk-update-red-to-danger.py** — replaced 13 occurrences of `text-red-500`, `hover:text-red-X`, etc. across 9 files (icons, asterisks)
- **Receipts page** — full rewrite to remove native `confirm()`/`alert()`:
  - Uses `useToast()` for success/error feedback
  - Uses `<ConfirmDialog>` for destructive actions (post/reverse)
  - Uses `<EmptyState>` for empty list
  - Uses `<SkeletonTable>` for loading state
  - Uses new Badge variants + ink-* tokens throughout

### Pillar 4: Critical L60 API Bug Fixes
- **Journal Entries list page** — was using `fetch('/api/finance/journal-entries')` without JWT (401 silently fails). Now uses `financeApi.listJournalEntries()` (auto-JWT)
- **Journal Entry detail page** — same fix, uses `financeApi.getJournalEntry()` + `financeApi.postJournalEntry()`. Also added ConfirmDialog for post action
- **Journal Entry new page** — uses `financeApi.createJournalEntry()`
- **Added to lib/api.ts**:
  - `financeApi.listJournalEntries()` + `getJournalEntry()` + `createJournalEntry()` + `postJournalEntry()` + `reverseJournalEntry()`
  - `JournalEntry`, `JournalEntryDetail`, `JournalEntryLine`, `CreateJournalEntryRequest` types
- **SalesInvoice interface** — added `useVat5?: boolean` (Sprint 35 wire-up)

### Verification
- **TypeScript typecheck**: 0 errors
- **Production build**: 50+ pages compiled successfully
- **Playwright UI tests (verify-sprint39-ui.mjs)**: 9/9 pass (tax toggle, design system)
- **Playwright page sweep (verify-sprint39-pages.mjs)**: 50/50 pages load (200 OK), no JS errors
- **Playwright interactive tests (verify-sprint39-interactive.mjs)**: 8/8 pass
  - Login → Dashboard
  - Click first journal entry → detail page with lines
  - Click customer "كشف حساب" → statement page
  - User menu opens with profile/companies/audit/logout
  - Receipts ConfirmDialog opens on reverse click
- **BE smoke** (manual): Trial Balance shows 35 accounts (was 30 before Sprint 38 L19 fix)

---

## Files changed (108 modified, 7 new)

### Modified
- **Components (8)**: `Button.tsx`, `Card.tsx`, `Input.tsx`, `Select.tsx`, `Badge.tsx`, `Table.tsx`, `PageHeader.tsx`, `EmptyState.tsx`, `LoadingSkeleton.tsx`, `Modal.tsx`, `ConfirmDialog.tsx`, `Toast.tsx`, `AppShell.tsx`
- **Config (2)**: `tailwind.config.js`, `app/globals.css`
- **API client (1)**: `lib/api.ts` (added 5 financeApi methods + 4 types + useVat5 field)
- **Auth pages (1)**: `app/login/page.tsx` (full redesign)
- **Authenticated pages (95)**: 82 with bulk color update + 13 hand-fixed (receipts, journal-entries/[id], journal-entries/new, sales-invoices/new, dashboard, error.tsx)

### New
- **scripts (7)**:
  - `bulk-update-error-box-colors.py` (330 substitutions across 82 files)
  - `bulk-update-red-to-danger.py` (13 substitutions across 9 files)
  - `verify-sprint39-ui.mjs` (9 tests — design system + tax opt-in)
  - `verify-sprint39-pages.mjs` (50 page sweep)
  - `verify-sprint39-key-screens.mjs` (13 page screenshots)
  - `verify-sprint39-login.mjs` (login page screenshot)
  - `verify-sprint39-interactive.mjs` (8 interactive flow tests)

---

## Decisions

- **DEC-125 — UI/UX Overhaul** (Sprint 39): Design system overhaul + tax opt-in + 50-page Playwright sweep
- **DEC-101 always-on**: Default reference data is essential
- **DEC-124 reinforced**: L19 audit must cover service layer + raw fetch calls (L60)

## Lessons (L65..L68)

- **L65 (NEW)** (Sprint 39): When designing a token-based design system, do a bulk color update AFTER defining all tokens. Don't try to do per-file review — the `danger-50` vs `red-50` pattern is mechanical. Use a Python script for the bulk rename. **Pattern**: `scripts/bulk-update-X.py` for any token migration.
- **L66 (NEW)** (Sprint 39): Use `<ConfirmDialog>` + `<Toast>` instead of native `confirm()` + `alert()` for ANY user-facing action. Native dialogs break the visual design and feel like a 2000s webapp. The UI kit already has these components — wire them in from day 1.
- **L67 (NEW)** (Sprint 39): When using a JWT-based API, **never** use raw `fetch('/api/...')`. Always use the API client (`api.get()`, `api.post()`, or the `xxxApi.method()` wrapper). The interceptor auto-attaches the JWT + X-Company-Id header. Without it, 401 silently fails (the page renders but no data). **L60 was about TS types; L67 is about the runtime consequence**.
- **L68 (NEW)** (Sprint 39): Playwright is the ONLY way to find visual UI bugs that API testing misses. The 50-page sweep + 8 interactive flows took ~2 minutes and caught:
  - 1 L60 bug (journal-entries page 401)
  - 4 visual issues (empty state placeholder, wrong save button label, etc.)
  - 0 critical UI bugs (all 50 pages render, 0 JS errors)
  The smoke test is the highest-ROI test in the project.

## Carry-over Sprint 40+

- **P0**: 17 files still use raw `fetch('/api/...')` (L67 carry-over):
  - admin: item-categories/* (3), posting-rules/* (4), users/new
  - finance: accounts/new, cost-centers/* (3)
  - inventory: items/new, movements, reservations/* (3)
  - procurement: goods-receipts/new
  - projects/new
- **P1**: UI feedback from Anas (click-to-expand, form improvements)
- **P2**: 4 VAT-related workflows (Sprint 35.5 cancelled — re-evaluate)
- **P2**: mvp-docker rebuild (auto-rebuild still failing, deferred)
- **P2**: More visual polish (sidebar collapse, topbar search, breadcrumbs)

## 7 branches waiting for "ادفع" (Anas's discretion)

1. `feature/sprint-33-ui-polish` @ `2f8a79a`
2. `feature/sprint-34-audit-manual-jes` @ `22af7d2`
3. `feature/sprint-35-vat5-workflows` @ `62cc30a`
4. `feature/sprint-36-statements-tb` @ `0ae0168`
5. `feature/sprint-37-l19-audit-je-templates` @ `ec60ac2`
6. `feature/sprint-38-l19-audit-service-layer-je-templates` @ `f95486d`
7. `feature/sprint-39-ui-ux-overhaul-vat-optional` @ NEW
