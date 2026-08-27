# 🤖 AGENTS.md — `src/frontend/app/(authenticated)/projects/`

> **Projects frontend routes.** Read `/src/frontend/AGENTS.md` first.

**Last updated:** 2026-08-27 (Sprint 61 — Engineer Reports)

---

## Purpose

Frontend routes for **Project Management** under the Holding ERP. This subtree contains:

- The project list page (`/projects`)
- The project create page (`/projects/new`)
- The project detail page with tabs (`/projects/[id]`)
- The project edit page (`/projects/[id]/edit`)
- The **Engineer Reports** sub-routes (Sprint 61 — DEC-192..194)
  - `/projects/[id]/engineer-reports` — list (filterable by date + status)
  - `/projects/[id]/engineer-reports/new` — create form (with photo upload)
- The **standalone** Engineer Report detail page (`/engineer-reports/[id]`)
  - Read-only view + Sign-off panel (PM/Client only) + Photo gallery

## Local Contracts

### Engineer Reports (Sprint 61 — DEC-192..194)

- **Bilingual UI** — every user-visible string is AR + EN.
- **Filter chips** on the list page use the existing `FilterChips` component
  with the standard tone palette (slate=Draft, amber=Submitted, green=Approved, red=Rejected).
- **Status pill colors** are sourced from `ENGINEER_REPORT_STATUS_LABELS` in `lib/api.ts`.
- **Photos** are uploaded via `PhotoUploader` (client-side previews using
  `URL.createObjectURL`). Maximum 10 photos per report.
- **Sign-off panel** is gated by user role:
  - PM or Client role → can approve/reject Submitted reports
  - Engineer role → can submit Draft reports
  - The backend enforces authorization; the FE gate is for UX only.

### Tabs in the project detail page

The `Tab` type is defined in `page.tsx`. Adding a new tab requires:
1. Adding the new tab key to the `Tab` union
2. Adding a `TAB_CHIPS` entry
3. Adding a `{tab === 'newtab' && <NewTab projectId={id} />}` line in the JSX
4. Implementing the `NewTab` function below (or in a separate file)

## Routes Index

| Path | File | Purpose |
|------|------|---------|
| `/projects` | `page.tsx` | List of all projects |
| `/projects/new` | `new/page.tsx` | Create form |
| `/projects/[id]` | `[id]/page.tsx` | Detail (tabs: details, pnl, contract, billings, boq, variations, **engineer-reports**) |
| `/projects/[id]/edit` | `[id]/edit/page.tsx` | Edit form |
| `/projects/[id]/engineer-reports` | `[id]/engineer-reports/page.tsx` | Engineer Reports list (DEC-192) |
| `/projects/[id]/engineer-reports/new` | `[id]/engineer-reports/new/page.tsx` | Create form (DEC-192 + DEC-193) |
| `/engineer-reports/[id]` | `(authenticated)/engineer-reports/[id]/page.tsx` | Detail + sign-off (DEC-192..194) |

## Components

| Component | Path | Purpose |
|-----------|------|---------|
| `ReportForm` | `components/engineer-report/ReportForm.tsx` | Reusable form (date / weather / work_done / issues / photos) |
| `PhotoUploader` | `components/engineer-report/PhotoUploader.tsx` | Multi-file photo picker with previews |
| `SignoffPanel` | `components/engineer-report/SignoffPanel.tsx` | PM/Client sign-off UI (Approve / Reject + comment) |

## Tests

`src/frontend/__tests__/engineer-report/`:
- `ReportForm.test.tsx` (2 tests)
- `PhotoUploader.test.tsx` (2 tests)
- `SignoffPanel.test.tsx` (1 test)

Run with: `npm test -- --testPathPattern=engineer-report`

## Verification

- [ ] `npm run type-check` — zero errors
- [ ] `npm run build` — production build succeeds
- [ ] `npm test -- --testPathPattern=engineer-report` — 5/5 pass
- [ ] No `tenant_id` in any new file
- [ ] Bilingual AR + EN on all user-visible strings

---

_Last updated: 2026-08-27 by Worker 2B (Sprint 61 Wave 2B)_
