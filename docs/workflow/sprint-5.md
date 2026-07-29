# 🚀 Sprint 5: Demo V2 — The "Wow" Version

**Date:** 2026-07-29
**Architect:** سيتی (Mavis, Cloud Coordinator) + محمد (Strategic Advisor)
**Implementer:** Mavis Local (Tech Lead) + Jimis (BE + FE parallel)
**Owner:** Anas (Project Owner)
**Duration:** 4-6 hours (big sprint)
**Deliverable:** ONE PR (`feature/sprint-5-demo-v2` → develop)
**Goal:** Build an **impressive, polished Demo V2** that showcases the Holding + Multi-Company architecture with beautiful UI/UX, ready to amaze the client.

---

## 🎯 Vision (Demo V2 vs V1)

| Aspect | V1 (Sprint 1-4) | V2 (Sprint 5) |
|--------|----------------|---------------|
| **Dashboard** | Basic cards | Charts + KPIs + trends |
| **CoA** | Not visualized | **Tree view (hierarchical)** |
| **Reports** | None | **Trial Balance + P&L + Balance Sheet** |
| **Customers** | Schema only | Full CRUD + list + detail |
| **Suppliers** | Schema only | Full CRUD + list + detail |
| **Invoices** | Seeded data | CRUD + print PDF + email |
| **Bank** | Seeded data | Reconciliation view |
| **Charts** | None | Chart.js / Recharts |
| **Print/Export** | None | PDF + Excel |
| **Search** | None | Global + per-page |
| **Loading states** | Inconsistent | Skeletons + spinners |
| **Mobile responsive** | Partial | Full responsive |
| **Polish** | Basic | Production-grade |

**Bottom line:** V1 = "system works". V2 = "system impresses".

---

## 🏛️ Architectural Constraints (سيتی)

> **These are NON-NEGOTIABLE. Mavis Local has technical freedom WITHIN these constraints.**

### 1. Constitution Compliance (15 Articles)

| Article | Rule | How to verify |
|---------|------|---------------|
| **Article 3** | **Multi-Company, NO Multi-Tenant** | `grep -r tenant_id src/` → 0 |
| **Article 8 Rule 5** | `company_id` Only (no tenant_id) | All `company_id` filter present |
| **Article 8 Rule 3** | Idempotent Migrations | `IF NOT EXISTS` / `ON CONFLICT` |
| **Article 8 Rule 6** | No EF Core | Dapper + FluentMigrator only |
| **Article 8 Rule 9** | Frontend-First Errors (AR + EN) | All errors in both languages |
| **Article 8 Rule 10** | Document in AGENTS.md | Update nearest AGENTS.md |
| **Article 11** | One Test Per Endpoint | Smoke test each new endpoint |
| **Article 13** | Mephisto Role (if used) | External work = his own branch |

### 2. Stack Discipline

| Layer | Stack | Notes |
|-------|-------|-------|
| **Backend** | C# / .NET 9 / Dapper / FluentMigrator | **NO EF Core** |
| **Frontend** | TypeScript / Next.js 14 (App Router) / Tailwind / shadcn/ui | No new framework |
| **Database** | PostgreSQL 17 (Supabase for dev, Docker for local) | No new DB engine |
| **Auth** | JWT (HS256) + BCrypt | No new auth method |
| **Charts** | Recharts or Chart.js (Mavis Local chooses) | Use existing library |

### 3. Security

- **BCrypt cost 12** for any new passwords
- **No secrets** in code, chat, PRs
- **Env vars only** for sensitive config
- **All new endpoints** under `[Authorize]` + `CompanyContext` filter

### 4. Code Quality

- **One test per endpoint** (smoke test, per Article 11)
- **Idempotent SQL** (every migration, every seed)
- **Async/await** for all I/O
- **DTOs** in `Modules/<Module>/Application/DTOs/`
- **Repositories** via Dapper (no EF Core)

### 5. UI/UX Standards

- **Arabic primary** (default), English secondary
- **RTL direction** by default
- **English numerals** (1, 2, 3) per Anas's preference
- **Loading states:** skeleton + spinner (per page)
- **Empty states:** friendly message + icon + CTA
- **Error states:** Arabic + English, with retry option
- **Form validation:** inline + on submit
- **Buttons:** primary (action) + secondary (cancel) + danger (delete)
- **Modals:** for create/edit, not full pages
- **Tables:** sortable, searchable, paginated
- **Mobile responsive:** test on phone size (375px)

### 6. DOX Framework

- **Update nearest AGENTS.md** if contracts change
- **Update CHANGELOG.md** with Sprint 5 entry
- **Update root AGENTS.md** if global patterns change
- **No new modules** without AGENTS.md

---

## 🛠️ Technical Freedom (Mavis Local)

> **Within the constraints above, Mavis Local is FREE to:**

| Area | Freedom |
|------|---------|
| **Code structure** | Module organization, file layout, naming |
| **Library choice** | Recharts vs Chart.js, date-fns vs dayjs, etc. |
| **Component library** | shadcn/ui existing + new components OK |
| **Sprint sub-tasks** | How to split between Jimis (BE/FE) |
| **Page layout** | Visual design, component composition |
| **Database queries** | Optimization, batch queries, etc. |
| **Error handling** | Pattern, retry logic, fallbacks |
| **Performance** | Caching, lazy loading, code splitting |
| **Testing** | What's worth testing beyond smoke |

**Mavis Local = Mavis/Jimi Coordinator. He decides HOW. I (سيتی) define WHAT.**

---

## 📋 Feature List (5 Phases)

### Phase 1: Financial Core (1.5 hours)

#### 1.1 CoA Tree View (45 min)

**The example Anas mentioned — MUST HAVE.**

**Page:** `app/(authenticated)/accounting/accounts/page.tsx`

**Features:**
- Hierarchical tree (parent_account_id → children)
- Expand/collapse nodes (state persisted in localStorage)
- Account type badge (Asset / Liability / Equity / Revenue / Expense)
- Balance display (sum of transactions)
- Filter by type
- Search by name or code
- "Add child account" button (modal)
- Edit account (modal)
- Toggle: show/hide inactive accounts
- Multi-company support (switch in top bar)

**Backend endpoint:** `GET /api/accounts?companyId=&parentId=&isActive=`

**Backend:** `Jimi (BE)` — 30 min
**Frontend:** `Jimi (FE)` — 15 min (using tree component)

#### 1.2 General Ledger (45 min)

**Page:** `app/(authenticated)/accounting/ledger/page.tsx`

**Features:**
- Table of all journal entries
- Filters: date range, account, company
- Pagination (50 per page)
- Drill-down to transaction detail
- Export to CSV
- Print

**Backend:** `Jimi (BE)` — 25 min
**Frontend:** `Jimi (FE)` — 20 min

### Phase 2: Reports (1 hour)

#### 2.1 Trial Balance (20 min)

**Page:** `app/(authenticated)/accounting/reports/trial-balance/page.tsx`

**Features:**
- Date range selector
- Account | Debit | Credit columns
- Totals row at bottom
- Print + PDF export

**Backend:** `Jimi (BE)` — 10 min
**Frontend:** `Jimi (FE)` — 10 min

#### 2.2 Income Statement (P&L) (20 min)

**Page:** `app/(authenticated)/accounting/reports/income-statement/page.tsx`

**Features:**
- Date range selector
- Revenue → Expenses → Net Income
- Comparison to previous period (optional)
- Print + PDF export

#### 2.3 Balance Sheet (20 min)

**Page:** `app/(authenticated)/accounting/reports/balance-sheet/page.tsx`

**Features:**
- As-of date selector
- Assets | Liabilities | Equity
- Print + PDF export

**All reports:** 1 BE Jimi + 1 FE Jimi (parallel)

### Phase 3: Operations (1.5 hours)

#### 3.1 Customer CRUD (30 min)

**Pages:**
- `app/(authenticated)/admin/customers/page.tsx` (list)
- `app/(authenticated)/admin/customers/[id]/page.tsx` (detail)
- `app/(authenticated)/admin/customers/new/page.tsx` (create)
- `app/(authenticated)/admin/customers/[id]/edit/page.tsx` (edit)

**Features:**
- List: search, filter (active/inactive), paginated
- Detail: info + linked invoices
- Create: form (name, email, phone, address, tax_id)
- Edit: same form pre-filled
- Delete: soft (is_active = false)

**Backend:** `Jimi (BE)` — 15 min (5 endpoints)
**Frontend:** `Jimi (FE)` — 15 min (4 pages)

#### 3.2 Supplier CRUD (30 min)

**Same as Customer, but for suppliers.**

**Pages:** Same pattern, different paths:
- `app/(authenticated)/admin/suppliers/...`

#### 3.3 Invoice CRUD (30 min)

**Pages:**
- `app/(authenticated)/sales/invoices/page.tsx` (list)
- `app/(authenticated)/sales/invoices/[id]/page.tsx` (detail)
- `app/(authenticated)/sales/invoices/new/page.tsx` (create)

**Features:**
- List: filter by status (draft/sent/paid), customer, date
- Detail: line items, totals, status
- Create: dynamic line items (add/remove rows)
- Print PDF
- Mark as paid (button)

**Backend:** `Jimi (BE)` — 15 min
**Frontend:** `Jimi (FE)` — 15 min

### Phase 4: Dashboard Polish (45 min)

#### 4.1 Charts (30 min)

**Page:** `app/(authenticated)/dashboard/page.tsx` (enhance existing)

**Add:**
- Revenue chart (line, last 6 months)
- Expenses by category (pie chart)
- Top 5 customers (bar chart)
- KPI cards with trend indicators (↑ ↓)

**Library:** Recharts (Mavis Local chooses)

**Backend:** `Jimi (BE)` — 15 min (3 endpoints for chart data)
**Frontend:** `Jimi (FE)` — 15 min (4 charts)

#### 4.2 Holding Dashboard (15 min)

**Page:** `app/(authenticated)/holding/dashboard/page.tsx` (enhance existing)

**Add:**
- Consolidated charts (across all companies)
- Cross-company comparison table

### Phase 5: UX Polish (45 min)

#### 5.1 Global Search (15 min)

**Component:** Top bar search input

**Features:**
- Search across: customers, suppliers, invoices, accounts
- Live results (debounced 300ms)
- Keyboard shortcut: Cmd/Ctrl+K

**Backend:** `Jimi (BE)` — 10 min
**Frontend:** `Jimi (FE)` — 5 min

#### 5.2 Loading States (15 min)

**For ALL new pages:**
- Skeleton loaders (not spinners)
- Per-page `loading.tsx`
- Empty state component
- Error state component

#### 5.3 Print/Export Foundation (15 min)

**Setup:**
- Print CSS (already in CSS files)
- PDF library (jsPDF or react-pdf)
- Excel export (xlsx or csv)

**Apply to:** Invoices, Reports, Ledger, Customer list

---

## 🎯 Page List (Final — 16 pages)

| # | Path | Phase | Owner |
|---|------|-------|-------|
| 1 | `/accounting/accounts` | 1.1 | FE |
| 2 | `/accounting/ledger` | 1.2 | FE |
| 3 | `/accounting/reports/trial-balance` | 2.1 | FE |
| 4 | `/accounting/reports/income-statement` | 2.2 | FE |
| 5 | `/accounting/reports/balance-sheet` | 2.3 | FE |
| 6 | `/admin/customers` | 3.1 | FE |
| 7 | `/admin/customers/[id]` | 3.1 | FE |
| 8 | `/admin/customers/new` | 3.1 | FE |
| 9 | `/admin/customers/[id]/edit` | 3.1 | FE |
| 10 | `/admin/suppliers` (×4 pages) | 3.2 | FE |
| 11 | `/sales/invoices` | 3.3 | FE |
| 12 | `/sales/invoices/[id]` | 3.3 | FE |
| 13 | `/sales/invoices/new` | 3.3 | FE |
| 14 | `/dashboard` (enhance) | 4.1 | FE |
| 15 | `/holding/dashboard` (enhance) | 4.2 | FE |
| 16 | Global search (top bar) | 5.1 | FE |

**Plus:** Loading states, error states, empty states on ALL new pages.

---

## 🛡️ Workflow (Mavis Local + Jimis in parallel)

```
┌──────────────────────────────────────────────────────────────┐
│ MAVIS LOCAL (Tech Lead)                                      │
│  - Coordinate                                                │
│  - Verify BE + FE outputs                                    │
│  - Resolve conflicts                                         │
│  - Open PR (squash)                                          │
└──────────────────────────────────────────────────────────────┘
         │                              │
         ▼                              ▼
┌────────────────────┐         ┌────────────────────┐
│ JIMI (BE)          │         │ JIMI (FE)          │
│                    │         │                    │
│ - CoA endpoints    │         │ - 16 pages         │
│ - Ledger endpoint  │         │ - Charts           │
│ - Reports endpoints│         │ - Search           │
│ - Customer/Supplier│         │ - Polish           │
│ - Invoice endpoints│         │ - Print/PDF        │
│ - Search endpoint  │         │                    │
│ - Chart endpoints  │         │                    │
│ - Tests            │         │                    │
│ - Migrations       │         │                    │
│                    │         │                    │
│ Time: 4-5h         │         │ Time: 4-5h         │
└────────────────────┘         └────────────────────┘
         │                              │
         └──────────────┬───────────────┘
                        ▼
              ┌────────────────────┐
              │  ONE PR (Squash)   │
              │  feature/sprint-5  │
              │  → develop         │
              └────────────────────┘
```

---

## ✅ Quality Standards (Mavis Local's Checklist)

### Backend
- [ ] `dotnet build` — 0 errors
- [ ] `dotnet test` — all green (1 per new endpoint)
- [ ] All endpoints under `[Authorize]`
- [ ] All queries filter by `CompanyContext.CompanyId`
- [ ] All migrations idempotent
- [ ] No `tenant_id` (grep verify)
- [ ] No secrets in code (grep verify)
- [ ] No EF Core (grep verify)
- [ ] All DTOs in `Modules/<Module>/Application/DTOs/`
- [ ] All new endpoints have OpenAPI attributes

### Frontend
- [ ] `npm run typecheck` — 0 errors
- [ ] `npm run build` — production build succeeds
- [ ] `npm run lint` — 0 errors
- [ ] All user-facing strings in AR + EN
- [ ] RTL works correctly (test in browser at 375px width)
- [ ] Loading + empty + error states on every new page
- [ ] All API calls use `lib/api.ts` typed client
- [ ] X-Company-Id header on all API calls
- [ ] No hardcoded API URLs
- [ ] Forms have validation (Zod + React Hook Form)
- [ ] Tables are sortable + paginated

### Architecture
- [ ] `grep -r tenant_id src/` → 0
- [ ] `grep -r multi-tenant src/` → 0
- [ ] All `company_id` filters present
- [ ] DOX-rail read: root, src/, src/backend/, src/frontend/, src/backend/Modules/<new modules>/
- [ ] AGENTS.md updated if contracts changed
- [ ] CHANGELOG.md updated (Sprint 5 entry)

### Demo Ready
- [ ] Demo data seeded (existing 1 Holding + 3 subs + 10 users)
- [ ] All 5 logins work
- [ ] Browser test: 5 pages, 3 reports, all CRUD
- [ ] No 404s on any link
- [ ] No JS errors in console
- [ ] Charts render correctly
- [ ] PDF export works
- [ ] Print works (Ctrl+P)
- [ ] Mobile responsive (test on phone)

---

## 📋 Definition of Done

The Demo V2 is DONE when:

- [ ] All 16 pages work (16/16)
- [ ] All 3 reports generate correctly
- [ ] All CRUD operations work (Customer, Supplier, Invoice)
- [ ] All 5 logins work
- [ ] All charts render
- [ ] All loading/empty/error states present
- [ ] RTL works
- [ ] Mobile responsive
- [ ] Print/PDF works
- [ ] 0 source code regressions
- [ ] 0 tenant_id references
- [ ] 0 multi-tenant language
- [ ] 1 PR opened (`feature/sprint-5-demo-v2` → develop)
- [ ] All tests pass
- [ ] CHANGELOG.md updated
- [ ] AGENTS.md updated (if needed)
- [ ] Demo verified on Anas's machine via screenshots

---

## 🎬 Suggested Workflow (Mavis Local can adapt)

### Step 1: Setup (5 min)
```bash
git fetch origin
git checkout -b feature/sprint-5-demo-v2 origin/develop
# develop @ dd4aef03
```

### Step 2: Plan + Assign (15 min)
- Read all 5 phases
- Assign Phases 1+2 to BE Jimi (financial core + reports)
- Assign Phases 3+4+5 to FE Jimi (operations + dashboard + polish)
- BE works first, FE works in parallel

### Step 3: BE Work (2.5 hours)
- CoA tree endpoint
- Ledger endpoint
- 3 report endpoints
- Customer CRUD (5 endpoints)
- Supplier CRUD (5 endpoints)
- Invoice CRUD (3-5 endpoints)
- Search endpoint
- 3 chart data endpoints
- 1 test per endpoint
- Idempotent migrations

### Step 4: FE Work (3 hours, parallel with BE)
- 16 pages (skeleton first, then content)
- Charts (Recharts)
- Search (global)
- Loading/empty/error states
- Print/PDF
- Mobile responsive

### Step 5: Integration (1 hour)
- Mavis Local tests integration
- Smoke test all flows
- Visual check (screenshots)
- RTL check

### Step 6: PR (15 min)
```bash
git add -A
git commit -m "feat(sprint-5): demo V2 — CoA tree, reports, CRUD, charts, polish"
git push --force-with-lease origin feature/sprint-5-demo-v2
gh pr create --base develop --title "feat(sprint-5): Demo V2 — Impressive client demo" --body "..."
```

### Step 7: Hand-off
Send hand-off to سيتی with:
- ✅ or ❌ status
- PR URL
- Screenshots of key pages
- Test results
- Any architecture decisions made

---

## 🛑 Out of Scope (Explicit)

- Inventory module (Phase 7)
- HR/Payroll (Phase 7)
- Multi-currency (Phase 7)
- Advanced reports (custom report builder)
- E2E tests (per Article 11, not required)
- Dark mode
- Mobile app
- Multi-language beyond AR/EN
- Production deploy (per Article 10, FROZEN)
- Inventory stock movements UI (Phase 7)
- Bank reconciliation backend (only UI mockup)
- Email sending (mock only)
- Backup/restore
- Audit log UI (already in data, no UI for V2)

---

## 💡 Inspiration (What Makes "Wow")

| Feature | Why "Wow" |
|---------|-----------|
| **CoA tree** | Visualizes the chart of accounts hierarchy (key for Holding) |
| **P&L + Balance Sheet** | Standard financial reports (any client will recognize) |
| **Charts on dashboard** | Modern visual analytics (looks professional) |
| **PDF print** | Real-world ERP feature (clients expect it) |
| **Mobile responsive** | Modern standard |
| **Loading skeletons** | UX polish (no spinners) |
| **Global search** | Power-user feature |
| **Customer/Supplier CRUD** | Basic but essential |
| **Invoice print PDF** | Demonstrates full business flow |

---

## 🎯 Final Goal Statement

> **Demo V2 = "Holdings-level ERP that looks production-ready and impresses on first impression."**
>
> Client should see:
> - Beautiful Holding Dashboard with charts
> - Hierarchical CoA (unique to multi-company)
> - Standard financial reports (P&L, Balance Sheet, Trial Balance)
> - Full CRUD on customers/suppliers/invoices
> - Print/PDF for invoices and reports
> - Arabic + RTL throughout
> - Mobile-friendly
> - Fast (sub-second page loads)
> - Zero errors

**Mavis Local = make this happen. He has the tools, freedom, and time.**

---

## 🏷️ Tags

- `#Sprint-5` `#Demo-V2` `#Mavis-Local` `#Jimis-Parallel`
- `#Holding-Concept` `#Multi-Company` `#No-Tenant`
- `#CoA-Tree` `#Reports` `#CRUD` `#Charts` `#Polish`
- `#Architecture-Constraints` `#Technical-Freedom` `#One-PR`

---

— سيتی (Mavis, Cloud Coordinator) + محمد (Mavis, Strategic Advisor)
**Session:** 406067545768199
**التاريخ:** 2026-07-29
**Commit base:** `dd4aef03` (develop, after PR #171)
**For:** Anas (Project Owner) — your call to give Mavis Local the green light
