# Sprint 58a — CoA 4 Levels + 2026 Operational Scenario

**Owner:** Muhammad (Mavis) — strategic + accounting consultant
**Mode:** 1 (local dev, no push)
**Branch:** `feature/sprint-52-v0-polish` ← merge in `feature/sprint-58-contracts-billings`
**Status:** IN PROGRESS (2026-08-08)

---

## Goal (per Anas's directive, 2026-08-08 ~09:35 UTC+2)

> Build a clean, demonstrable 2026 fiscal year scenario that:
> 1. Uses a **new 4-level CoA** (L1 type / L2 sub-class / L3 control / L4 detail; **only L4 postable**).
> 2. Replaces the current illogical seeder accounts with a **professional chart** that serves both general accounting and project/cost-center accounting.
> 3. Posts a **realistic 2026 scenario** (Jan–Aug): sales invoices, vendor bills, customer receipts, vendor payments, payroll, bank transactions, project progress billings, depreciation, closing entries.
> 4. All postings go to **L4 detail accounts** with accurate distribution (cash/bank/AR/AP, revenue/expense split, project cost-centers).
> 5. Verifies the **financial reports engine** end-to-end: TB balanced, IS accurate, BS balanced, CF reconciles, AR/AP aging correct.
> 6. System runs cleanly on local host — Anas can browse the scenario to present to the client (an accountant).

---

## Background

### Mephisto's work (in `feature/sprint-58-contracts-billings`)
- **Sprint 57 (DEC-160..162)** — Project P&L Foundation (`ProjectPnLService.cs`, 118 lines)
- **Sprint 58 (DEC-163..165)** — Contracts CRUD + Progress Billings + WIP
  - `ContractService` (157 lines), `ContractRepository` (103 lines), `Contract` entity (49 lines)
  - `BillingService` (406 lines), `BillingRepository` (128 lines), `ProgressBilling` entity (62 lines)
  - `data-types/contracts.json` (32 lines), `data-types/progress_billings.json` (41 lines)
- **Sprint 58 hotfix** — `authedFetch()` helper in `lib/api.ts` + status enum string fix
- **Sprint 58 hotfix 2** — `JournalEntry.status` + `CostCenter.type` string typing
- **Sprint 59** — Modern inventory dashboard (Items, Movements, Reservations, Stock Levels)
- **Sprint 59 v2** — Modern Projects + Dashboard redesign (DEC-170..172)
- **Sprint 60** — Comprehensive UoM (h, d, km, ton, set…) + Select dropdown in item form

**Hardcoded account codes Mephisto used (need updating to new CoA):**
- `1103` = "المدينون" (Receivables) → should be `1201` in new CoA
- `4101` = "إيرادات المبيعات" (Sales Revenue) → should be `4101` (still valid)

### My work (in `feature/sprint-52-v0-polish`)
- **Sprint 53 (DEC-140..141)** — Year-End Closing + Retained Earnings Roll
- **Sprint 54 (DEC-142..144)** — Reports Hierarchy (TB v2, IS with L2 sections, CF with L3 lines)
- **Sprint 55 (DEC-145..147)** — Seeder Refactor (Path B: SI/Bills/Payments with proper journal entries)
- **Sprint 56 (DEC-149..150)** — Top Customers + Top Items reports
- **Sprint 57 (DEC-152)** — Executive Dashboard (8 KPIs + 5 charts)

### CoA issues with current state
The seeder CoA accounts (set by Sprint 50 + DEC-100..106) are insufficient:
- Missing many standard accounts (VAT, bank reconciliation, accumulated depreciation, etc.)
- No proper L4 detail accounts
- Project accounting has no dedicated accounts (WIP, project cost, project revenue)
- Posting rules reference inconsistent codes

---

## The 4-Level CoA Design (per Anas's spec)

### L1 — Account Type (1 char)
| Code | Type | Arabic |
|------|------|--------|
| 0 | Holding | حسابات الشركة القابضة |
| 1 | Assets | الأصول |
| 2 | Liabilities | الخصوم |
| 3 | Equity | حقوق الملكية |
| 4 | Revenue | الإيرادات |
| 5 | COGS | تكلفة المبيعات |
| 6 | Operating Expenses | المصروفات التشغيلية |
| 7 | Other | إيرادات ومصروفات أخرى |
| 8 | Tax | الضرائب |
| 9 | Closing | حسابات الإقفال |

### L2 — Sub-classification (1 char)
**1 (Assets):**
- 11 Current Assets (أصول متداولة)
- 12 Receivables (المدينون)
- 13 Inventory (المخزون)
- 14 Prepaid Expenses (مصروفات مقدمة)
- 15 Fixed Assets (أصول ثابتة)
- 16 Intangible Assets (أصول غير ملموسة)

**2 (Liabilities):**
- 21 Current Liabilities (خصوم متداولة)
- 22 Long-term Liabilities (خصوم طويلة الأجل)

**3 (Equity):**
- 31 Capital (رأس المال)
- 32 Retained Earnings (أرباح مرحلة)
- 33 Reserves (احتياطيات)

**4 (Revenue):**
- 41 Sales Revenue (إيرادات المبيعات)
- 42 Service Revenue (إيرادات الخدمات)
- 43 Project Revenue (إيرادات المشاريع)
- 49 Other Revenue (إيرادات أخرى)

**5 (COGS):**
- 51 Cost of Goods Sold (تكلفة المبيعات)
- 52 Project Costs (تكاليف المشاريع)

**6 (Expenses):**
- 61 Administrative (مصاريف إدارية وعمومية)
- 62 Selling (مصاريف بيعية وتسويقية)
- 63 Financial (مصاريف مالية)

**7 (Other):**
- 71 Other Income (إيرادات أخرى)
- 72 Other Expenses (مصروفات أخرى)

**8 (Tax):**
- 81 Income Tax (ضريبة الدخل)

**9 (Closing):**
- 91 Income Summary (ملخص الدخل)
- 92 WIP (أعمال تحت التنفيذ)

### L3 — Control Accounts (2 chars, aggregate)

| Code | Name | Postable? |
|------|------|-----------|
| 1101 | النقدية في الصندوق | NO |
| 1102 | البنوك | NO |
| 1103 | عهدة نقدية | NO |
| 1201 | المدينون (AR) | NO |
| 1202 | أوراق القبض | NO |
| 1203 | سلف الموظفين | NO |
| 1301 | المخزون | NO |
| 1401 | مصروفات مقدمة | NO |
| 1402 | ضريبة مدخلات (VAT Input) | NO |
| 1501 | أثاث ومعدات مكتبية | NO |
| 1502 | سيارات | NO |
| 1503 | معدات ثقيلة | NO |
| 1504 | مباني | NO |
| 1505 | أراضي | NO |
| 1590 | مجمع الإهلاك | NO |
| 1601 | أصول غير ملموسة (برامج) | NO |
| 2101 | الدائنون (AP) | NO |
| 2102 | قروض قصيرة الأجل | NO |
| 2103 | مصروفات مستحقة | NO |
| 2104 | ضريبة مخرجات (VAT Output) | NO |
| 2105 | رواتب مستحقة | NO |
| 2201 | قروض طويلة الأجل | NO |
| 3101 | رأس المال | NO |
| 3102 | المساهمون / الشركاء | NO |
| 3201 | أرباح مرحلة | NO |
| 3202 | صافي دخل السنة | NO |
| 3301 | احتياطي قانوني | NO |
| 3302 | احتياطي اختياري | NO |
| 4101 | إيرادات المبيعات | NO |
| 4201 | إيرادات الخدمات | NO |
| 4301 | إيرادات المستخلصات (المشاريع) | NO |
| 4302 | إيرادات أعمال تحت التنفيذ (WIP) | NO |
| 4901 | إيرادات أخرى | NO |
| 5101 | تكلفة البضاعة المباعة | NO |
| 5201 | تكلفة المواد المباشرة (مشاريع) | NO |
| 5202 | تكلفة العمالة المباشرة (مشاريع) | NO |
| 5203 | مصروفات المشاريع غير المباشرة | NO |
| 6101 | رواتب وأجور | NO |
| 6102 | إيجار | NO |
| 6103 | كهرباء ومياه | NO |
| 6104 | اتصالات وإنترنت | NO |
| 6105 | مستلزمات مكتبية | NO |
| 6106 | مصروف إهلاك | NO |
| 6107 | صيانة | NO |
| 6108 | تأمين | NO |
| 6201 | تسويق وإعلان | NO |
| 6202 | عمولات مبيعات | NO |
| 6301 | رسوم بنكية | NO |
| 6302 | مصروف فائدة | NO |
| 6303 | فروقات عملة | NO |
| 7101 | إيرادات استثمارات | NO |
| 7102 | إيرادات متنوعة | NO |
| 7201 | خسائر متنوعة | NO |
| 8101 | ضريبة الدخل | NO |
| 9101 | ملخص الدخل (إقفال) | NO |
| 9201 | أعمال تحت التنفيذ (WIP) | NO |

### L4 — Detail Accounts (L3 + "-" + serial, 4 digits + suffix)

Examples:
- `1101-001` = النقدية في الصندوق الرئيسي (Office Main Cash)
- `1102-001` = بنك ABC - حساب جاري
- `1102-002` = بنك XYZ - حساب توفير
- `1201-001` = عميل شركة النور (Customer A)
- `1201-002` = عميل شركة الأمل (Customer B)
- ...
- `2101-001` = مورد شركة الفجر (Vendor A)
- `2101-002` = مورد مؤسسة النجم (Vendor B)
- ...
- `4101-001` = مبيعات بضاعة
- `4201-001` = خدمات استشارية
- `4301-001` = مستخلصات مشروع X
- `4301-002` = مستخلصات مشروع Y
- `5201-001` = مواد مشروع X
- `5201-002` = مواد مشروع Y
- ...

**Critical accounting rule:** L1, L2, L3 are **NOT postable** — only L4 accounts accept journal entries. The UI and posting engine must enforce this.

---

## 2026 Scenario — Operational Data Plan

### Master data
- **Holding:** مجموعة الفجر القابضة (Al-Fajr Holding Group)
- **Subsidiaries (2):**
  - شركة الفجر للمقاولات (Al-Fajr Construction LLC)
  - شركة الفجر للتجارة (Al-Fajr Trading LLC)
- **Customers (6):** real-looking Libyan companies
- **Vendors (5):** real-looking Libyan companies
- **Employees (10):** 5 in construction, 3 in trading, 2 in holding
- **Items (10):** 5 raw materials, 3 finished goods, 2 services
- **Projects (3):**
  - P-001: بناء مدرسة (School Construction) — 18 months, 2M LYD
  - P-002: توريد مواد (Material Supply) — 6 months, 800K LYD
  - P-003: صيانة طرق (Road Maintenance) — 12 months, 1.2M LYD
- **Banks (2):** مصرف الجمهورية (CDBL) + مصرف الوحدة
- **Warehouses (2):** WH-MAIN, WH-CONST
- **Cost centers (6):** HQ, Construction, Trading, Project-001, Project-002, Project-003

### 2026 Transactions (Jan–Aug 2026)

| Period | Type | Count | Total LYD | Notes |
|--------|------|-------|-----------|-------|
| Jan | Capital injection (3101) | 1 | 3,000,000 | Opening balance |
| Jan | Bank loan (2201) | 1 | 500,000 | Long-term loan for construction |
| Jan | Office rent deposit (1102-001 → 6102-001) | 1 | 60,000 | Advance for office |
| Jan | Office furniture purchase (1501-001) | 1 | 80,000 | Cash purchase |
| Jan | Sales invoice SI-2026-0001 (1230) | 1 | 250,000 | B2B sale, 30-day terms |
| Jan | Purchase bill BILL-2026-0001 (2101) | 1 | 150,000 | Raw materials |
| Feb | Payroll run (6101) | 1 | 85,000 | 10 employees |
| Feb | Customer receipt R-2026-0001 (1230 → 1102-001) | 1 | 250,000 | SI-0001 paid in full |
| Feb | Vendor payment V-2026-0001 (2101 → 1102-001) | 1 | 80,000 | Partial bill payment |
| Mar | Project billing PRB-2026-0001 (4301-001) | 1 | 200,000 | Project 001, 10% complete |
| Mar | Project cost (5201-001) | 1 | 80,000 | Materials for project 001 |
| Mar | Bank charge (6301-001) | 1 | 250 | Monthly bank fee |
| Apr | Sales invoice SI-2026-0002 | 1 | 180,000 | |
| Apr | Depreciation (6106-001) | 1 | 5,000 | Monthly depreciation |
| May | Project billing PRB-2026-0002 (4301-001) | 1 | 350,000 | Project 001, 25% complete (cumulative 35%) |
| May | Payroll | 1 | 85,000 | |
| May | Vendor bill (additional materials) | 1 | 120,000 | |
| Jun | Project billing PRB-2026-0003 (4301-001) | 1 | 400,000 | Project 001, 20% (cumulative 55%) |
| Jun | Customer receipt for SI-0002 | 1 | 180,000 | |
| Jul | Sales invoice SI-2026-0003 (services) | 1 | 95,000 | |
| Jul | Payroll + bank charges | 2 | 85,250 | |
| Jul | Vendor payment | 1 | 100,000 | |
| Aug | Project billing PRB-2026-0004 (4301-001) | 1 | 500,000 | Project 001, 25% (cumulative 80%) |
| Aug | Sales invoice SI-2026-0004 | 1 | 320,000 | |
| Aug | Customer receipt | 1 | 150,000 | Partial |
| Aug | Vendor bill | 1 | 90,000 | |
| Aug | Depreciation | 1 | 5,000 | |
| Aug | Income tax provision (8101-001) | 1 | 15,000 | Estimated |
| Aug | Closing entry (YTD) | 1 | — | Net income → 3201 |

**Totals (rough estimate):**
- Revenue YTD: ~1.6M (sales 845K + services 95K + project billings 1.45M = ~2.4M, less cancellations)
- Expenses YTD: ~1.0M (payroll 510K + COGS 440K + opex 50K)
- Net Income YTD: ~1.4M
- AR ending: ~210K (1 unpaid invoice)
- AP ending: ~180K (1 unpaid bill)
- Cash + Bank ending: ~1.8M
- Fixed Assets: ~80K (less 30K accumulated depreciation)
- WIP (project): ~520K (billed 1.45M, costs 520K, retention ~50K)

---

## Implementation Plan

### Step 1: Merge Mephisto's branch into mine
- Use `git merge feature/sprint-58-contracts-billings` from my worktree
- Resolve conflicts (likely in: `lib/api.ts`, `Program.cs`, `ProjectsController.cs`, projects pages, journal_entries.json, `app/(authenticated)/projects/page.tsx`)
- Test build after merge

### Step 2: New CoA Seeder (Sprint 58a)
- New file: `src/backend/Shared/SeedData/ProfessionalCoASeederHostedService.cs`
- Gated by `Bootstrap:SeedProfessionalCoA=true` + `IsDevelopment()`
- Creates:
  - 10 L1 entries (account types) — `is_postable=false`
  - ~25 L2 entries (sub-classes) — `is_postable=false`
  - ~50 L3 entries (control accounts) — `is_postable=false`
  - ~50 L4 entries (detail accounts) — `is_postable=true`
- Sets `parent_id` for tree structure
- Idempotent (skip if already exists for company)
- Updates Mephisto's hardcoded `1103` → `1201` in `BillingService.cs`

### Step 3: Update Posting Rules (Sprint 58a)
- `DefaultPostingRulesSeeder` already seeds 5 rules
- Update account codes to new CoA:
  - Sales: DR 1201-001 (AR) / CR 4101-001 (Sales)
  - COGS: DR 5101-001 (COGS) / CR 1301-001 (Inventory)
  - Customer receipt: DR 1102-001 (Bank) / CR 1201-001 (AR)
  - Vendor bill: DR 5101-001 (Expense) / CR 2101-001 (AP)
  - Vendor payment: DR 2101-001 (AP) / CR 1102-001 (Bank)

### Step 4: 2026 Scenario Seeder (Sprint 58b)
- New file: `src/backend/Shared/SeedData/Scenario2026SeederHostedService.cs`
- Gated by `Bootstrap:SeedScenario2026=true` + `IsDevelopment()`
- Phases:
  1. Master data (companies, customers, vendors, items, banks, projects, employees, cost centers)
  2. Opening balances (capital + loan + assets)
  3. Monthly transactions (Jan → Aug, posted atomically)
  4. Depreciation
  5. Year-end closing
- Each phase logs progress + counts

### Step 5: Verification (Sprint 58d)
- Curl the report endpoints
- TB must balance (Σ DR = Σ CR)
- IS: Rev − Exp = Net (must match CF Net change)
- BS: Assets = Liab + Equity + Net Income (after closing)
- CF: 3 sections (Op/Inv/Fin) sum to Net change in cash
- AR Aging: 5 buckets, sums to total AR
- AP Aging: 5 buckets, sums to total AP
- Per-account ledger: pick 3 L4 accounts, verify opening/closing

### Step 6: Frontend Smoke Tests (Sprint 58e)
- Hit each major page
- No 500 errors
- Data displays correctly
- Drill-downs work
- Arabic RTL correct

---

## Files to Add/Modify

### New files
- `src/backend/Shared/SeedData/ProfessionalCoASeederHostedService.cs` (~600 lines)
- `src/backend/Shared/SeedData/Scenario2026SeederHostedService.cs` (~1500 lines)
- `docs/plans/sprint-58a-coa-2026.md` (this file)
- `scripts/sprint-58a-verify.ps1` (verification script)
- `scripts/sprint-58a-smoke-test.mjs` (Playwright smoke)

### Modified files
- `src/backend/Host/Program.cs` (register new seeders + update DI)
- `src/backend/Host/appsettings.Development.json` (enable new seeders)
- `src/backend/Modules/Projects/Application/Services/BillingService.cs` (update hardcoded `1103` → `1201`)
- `src/backend/Shared/SeedData/DefaultPostingRulesSeeder.cs` (update account codes)
- `src/frontend/lib/api.ts` (already has `authedFetch` from Mephisto's hotfix)
- `src/frontend/app/(authenticated)/projects/page.tsx` (Mephisto's modern version)
- `src/frontend/app/(authenticated)/projects/[id]/page.tsx` (Mephisto's modern version)

---

## Risks

1. **Merge conflicts:** Mephisto's work + my Sprints 53-57 may have overlapping changes in `Program.cs`, `lib/api.ts`, projects pages. Need careful conflict resolution.
2. **Posting rule cascade:** If account codes change, all the existing JEs + reports break. Need to nuke the DB and re-seed.
3. **Seeded data volume:** 8 months × ~10 transactions/month = ~80 records. Each with multiple lines + JE = ~250 records total. Should run in <2 min.
4. **L4 enforcement:** Currently the schema allows posting to L1/L2/L3. Need to add validation OR add `is_postable` column. The cleanest fix is a CHECK at the schema level.

---

## Success Criteria

- [ ] All services start cleanly on :5000 (BE) + :3000 (FE) + :5432 (DB)
- [ ] New CoA has ~130 accounts across 4 levels
- [ ] 2026 scenario produces ~80 JEs with ~250 lines, all to L4 accounts
- [ ] TB balanced (Σ DR = Σ CR to the cent)
- [ ] IS: Rev - Exp = Net
- [ ] BS: Assets = Liab + Equity + NI
- [ ] CF: Op + Inv + Fin = Net Δ Cash
- [ ] AR aging: 5 buckets, total = AR ledger
- [ ] AP aging: 5 buckets, total = AP ledger
- [ ] Top Customers report shows correct data
- [ ] Top Items report shows correct data
- [ ] Executive Dashboard shows 8 KPIs
- [ ] All FE pages load (no 500s, no broken links)
- [ ] Smoke test 24+ pages passes
- [ ] Anas can log in and browse the scenario

---

**Estimated time:** 6-10 hours of focused work (this is a multi-sprint undertaking).
**Communication:** Telegram updates every ~30 min, no in-session check-ins.
