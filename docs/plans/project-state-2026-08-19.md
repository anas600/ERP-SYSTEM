# ERP-SYSTEM — حالة المشروع (Restore Memory Report)

> **تاريخ التقرير:** 2026-08-19
> **الهدف:** تقرير صفحة واحدة يعيد للذاكرة كل اللي محتاجه عشان تكمل العمل من جديد.
> **القارئ:** Anas (Project Owner) + Admin Team + Mavis (Muhammad).

---

## 1. 🏁 وين واصلين؟

**آخر Sprint مُنجَز محلياً:** **Sprint 58** — Mephisto merge + Professional 4-level CoA + 2026 Operational Scenario.
**HEAD commit:** `e47068c` على branch `feature/sprint-52-v0-polish`.
**Working tree:** ✅ نظيف (0 ملفات uncommitted).
**Status:** 🟡 **LOCAL-ONLY** — في انتظار "ادفع" من Anas.

**7 commits محلية جديدة (كلها على نفس branch `feature/sprint-52-v0-polish`):**

| Sprint | الموضوع | Commit | Retro |
|---|---|---|---|
| 53 | Year-End Closing Service (DEC-140..141) | `8e1bf59` | — |
| 54 | Reports Hierarchy — TB v2 + IS sections (DEC-142..144) | `200d2fd` | — |
| 55 | Seeder Refactor — Real Transactions (DEC-145..147) | `cf59720` | — |
| 56 | Top Customers/Items Reports (DEC-149..150) | `26481ef` | — |
| 57 | Executive Dashboard — 8 KPIs + 5 charts (DEC-152) | `2e73375` | ✅ [sprint-57-retro](./../team-charters/retrospectives/sprint-57-retro.md) |
| 58a | Mephisto Merge — Contracts + Billings + WIP (DEC-160..165) | `7f79ad9` | ✅ [sprint-58-retro](./../team-charters/retrospectives/sprint-58-retro.md) |
| 58b/c | Professional 4-level CoA + 2026 Scenario (DEC-Major) | `e47068c` | ✅ نفس الـ 58-retro |

**سابقاً (pushed to remote develop):** Sprints 1-39 (tags `v1.0.0` .. `v1.0.12`).

---

## 2. 🔄 الـ Workflow (Mode 1 + Mode 2 + 3 طبقات)

### Mode 1 — Development (الافتراضي)
- **الـ trigger:** ضمن العمل العادي (لا يوجد trigger خاص).
- **الـ Admin role:** Team lead + coordinator + executor (مع Jimis).
- **الـ scope:** Local work على `feature/*` branches — ممكن ندمج عدة sprints محلياً قبل الـ push.
- **Push to remote:** ❌ لا.
- **CI على GitHub:** ❌ لا (لا يوجد push).
- **mvp-docker rebuild:** ❌ لا.
- **Telegram notify:** ❌ لا.
- **Browser preview:** Layer 1 (local dev على :5001 BE + :3000 FE) مع dev/test data.

### Mode 2 — Release
- **الـ trigger:** Anas يقول "ادفع" (بس هو).
- **الـ Admin role:** Release engineer — `git push` + `gh pr create` + relax + merge + tag + restore.
- **الـ workflow:**
  1. `git push` للـ feature branch
  2. `gh pr create --base develop`
  3. انتظار CI monitor (6/6 checks green)
  4. relax develop protection → `gh pr merge --squash --admin` → tag `vX.Y.Z-sprintN` → restore protection
  5. Cron `mvp-auto-rebuild-on-develop-push` (5 min) يكتشف تغيير SHA → يبني mvp-docker → smoke test → Telegram ping
- **CI:** ✅ 6 checks (Backend Tests, Frontend Build, CodeQL csharp+js, TruffleHog, Architecture Guard — no tenant_id).
- **Browser preview:** Layer 2 (mvp-docker) بـ clean install بدون seed.

### 3-Layer Architecture

| Layer | الغرض | Setup | Branch | DB | Status |
|---|---|---|---|---|---|
| **1. Development** | تكرار سريع على local مع test data | `local-docker/` (مع seed) أو host runs | أي `feature/*` | Local Docker Postgres **أو** native Postgres على :5432 | ✅ Active |
| **2. Staging / MVP** | ميمَص حاويه نظيفة، browsable، بدون test data | `mvp-docker/` (production build، بدون seed) | `develop` بعد merge | Local Docker Postgres (clean) | ✅ Active |
| **3. Production** | Production عند العميل | (مؤجّل — FROZEN — "لا اهتم بيها الان") | `main` (LOCKED) | Supabase production | 🟡 FROZEN |

**Workflow بين الـ layers:**
1. Local Team يطوّر في **Layer 1** (سريع، مع test data على الهوست)
2. Sprint done → Local Team يدمج في `develop` عبر PR
3. **Admin Team (Mavis)** ياخذ الـ merge:
   - `cd mvp-docker && docker compose up -d --build`
   - `./smoke-test.ps1` (يتأكد إن clean MVP شغّال)
   - **ينبّه Anas** للتصفح
4. Anas يتصفح، يقرر: نكمل development، أو نسلّم للعميل
5. **Strategic Advisor محمد (Mavis)** يقرر متى Layer 1 → Layer 2

**ليش طبقتين؟** Layer 1 للسرعة (Local Team يكرر بسرعة مع test data). Layer 2 للنظافة (container جديد، schema حقيقي، بدون test data، يحاكي اللي راح يستلمه العميل).

**Layers القديمة (مُلغاة):** Local / Dev / Staging / Production with Supabase. الـ 3-Layer Model حلّ محلّها من Sprint 13.

### Two critical files:
- **Constitution:** [`../../CONSTITUTION.md`](../../CONSTITUTION.md) — governance v2.0 (active)
- **Architecture SSoT:** [`../architecture/holding-company-architecture.md`](../architecture/holding-company-architecture.md)

---

## 3. 📂 هيكل مجلد `docs/`

```
docs/
├── AGENTS.md                              Local contract لـ docs/ directory
├── CHANGELOG.md                           (76 KB) كل الـ DECs + lessons من Sprint 1
├── seed-sprint4-demo-data.sql             (54 KB) demo data seed
│
├── architecture/        ⭐ single source of truth للـ architecture
│   ├── holding-company-architecture.md   (44 KB) Master architecture
│   ├── REFACTOR-SPRINT-22.md             (9.6 KB) 15→9 modules refactor
│   ├── architecture-explained-2026-08-02.md
│   ├── system-architecture-sprint30.md
│   ├── admin-priorities-sprint31.md
│   └── state-summary-2026-08-02.html
│
├── plans/               Sprint plans
│   ├── sprint-41-ui-exploration-3-versions.md
│   ├── sprint-48-50-reports-and-demo-data.md
│   ├── sprint-53-56-abc-completion.md
│   ├── sprint-58a-coa-2026.md            ⭐ 17 KB — آخر plan
│   └── project-state-2026-08-19.md       ⭐ هذا الملف
│
├── team-charters/
│   └── retrospectives/   Sprint retros
│       ├── sprint-10..41-retro.md
│       ├── sprint-57-retro.md            ⭐ آخر retro قبل 58
│       └── sprint-58-retro.md            ⭐⭐ آخر retro
│
├── workflow/            Sprint hand-offs + workflow guides
│   ├── architecture.md
│   ├── demo-roadmap.md
│   ├── local-docker.md + local-docker-fixes-report.md
│   └── sprint-{0..6, 10..15}.md
│
├── workflows/           Per-module user workflows (13 ملف)
│   ├── README.md
│   ├── chart-of-accounts.md ⭐ CoA reference
│   ├── customer.md, employee.md, vendor.md
│   ├── item.md, journal-entry.md
│   ├── purchase-order.md, goods-receipt.md, vendor-bill.md
│   ├── sales-invoice.md, receipt.md
│   ├── project.md (Contracts + Billings + WIP)
│   └── payroll-run.md
│
├── notes/               محمد's analysis files
│   └── muhammad-sprint-18-analysis.md
│
├── client-materials/    Client-facing deliverables
│   ├── elevator-pitch-ar-en.md
│   └── slides/erp-demo-slides.pptx
│
└── screenshots/         Per-sprint visual evidence
    ├── sprint-52/        (13 صورة — تقارير + drill-down)
    ├── sprint-52a/       (13 صورة — CoA tree)
    ├── sprint-52b/       (13 صورة — statements + aging)
    ├── sprint-54/        (5 صور)
    ├── sprint-56/        (3 صور)
    ├── sprint-57/        (2 صور — executive dashboard)
    └── sprint-59-projects/  (7 صور)
```

### 📍 مواقع مهمة تحتاجها بسرعة:
- **Sprint 58 retro:** [`docs/team-charters/retrospectives/sprint-58-retro.md`](../team-charters/retrospectives/sprint-58-retro.md) (9.3 KB)
- **Sprint 58a plan:** [`docs/plans/sprint-58a-coa-2026.md`](./sprint-58a-coa-2026.md) (17 KB)
- **Sprint 58 CHANGELOG entries:** [`docs/CHANGELOG.md`](../CHANGELOG.md)
- **Architecture SSoT:** [`docs/architecture/holding-company-architecture.md`](../architecture/holding-company-architecture.md) (44 KB)

---

## 4. 🧱 الـ Stack + المشروع

- **Stack:** C# / .NET 9 + TypeScript / Next.js 14 + Dapper + FluentMigrator.
- **DB:** PostgreSQL 17.
- **Repo:** `github.com/anas600/ERP-SYSTEM`.
- **Deployment:** Hugging Face Space (`anas-assasket-erp-system.hf.space`).
- **3 Modules Folded (Sprint 22):** Activity, Notifications, Search, Reports — انضووا داخل Modules أخرى.

### Branch Architecture (مهم!)
| Branch | Role | Protection |
|---|---|---|
| `develop` | DEFAULT — active work | 6 checks + 1 review + linear history |
| `main` | FROZEN — anchored at `v1.0.0-archive` | LOCKED |
| `feature/sprint-52-v0-polish` | آخر work branch (Sprint 52 → 58) | لا protection (مفيش push) |
| `v0.0.0-pre-branch-reset` (tag) | Safety anchor | Immutable |
| `v1.0.0-archive` (tag) | Work anchor (Sprints 10-13) | Immutable |
| `vX.Y.Z-sprintN` (tags) | Per-sprint work anchors | Immutable |

### Default credentials (local dev)
- Email: `admin@erp.local`
- Password: `ChangeMe1234!`
- Tenant: Holding Enterprise (LYD)

### Local dev commands
```powershell
# Backend (يجب على :5001)
cd src\backend\Host
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run

# Frontend (يجب على :3000)
cd src\frontend
npm run dev

# DB connection (appsettings.Development.json — gitignored)
# Host=127.0.0.1;Port=5432;Database=erp_system;Username=erp;Password=erp_local_password
```

---

## 5. 🧩 الـ Modules (11) + الوظائف الجاهزة

> **Note:** AGENTS.md يذكر 9 modules لكن الـ code فعلياً فيه 11 (Companies + Payments مُضافة). هذه القائمة من الـ filesystem.

| # | Module | الوظائف الأساسية | Status |
|---|---|---|---|
| 1 | **Identity** | Auth + RBAC (JWT + refresh) | ✅ Complete |
| 2 | **Companies** | إدارة الـ Holding + N subsidiaries | ✅ Complete |
| 3 | **Finance** | CoA + Journal + Ledger + Posting Rules + Reports | ✅ Complete (Sprint 48-58) |
| 4 | **Inventory** | Items + Stock + Movements + Reservations + UoM (17 UoMs) | ✅ Complete |
| 5 | **Procurement** | Vendors + POs + GRs + Bills | ✅ Complete |
| 6 | **AccountsReceivable** | Customers + Sales Invoices + Receipts | ✅ Complete |
| 7 | **HR** | Employees + Departments + Attendance + Leave | ✅ Complete |
| 8 | **Payroll** | Payroll Runs + Salary Structures | ✅ Complete |
| 9 | **Projects** | Projects + Contracts + Billings + WIP (DEC-163..165) | ✅ Complete (Sprint 58) |
| 10 | **Payments** | Payment vouchers | ✅ Complete |
| 11 | **Dashboard** | Single page + Executive Dashboard (8 KPIs + 5 charts) | ✅ Complete (Sprint 57) |

### 📊 تقارير Finance الجاهزة (Sprint 48-58):
- ✅ Trial Balance v1 (flat) + v2 hierarchical (Sprint 54)
- ✅ Income Statement (with L2 sections)
- ✅ Balance Sheet
- ✅ Cash Flow (with L3 control account metadata)
- ✅ General Ledger
- ✅ AR Aging + AP Aging (with drill-down to customer/vendor statement)
- ✅ Top Customers + Top Items (Sprint 56)
- ✅ Aging Summary (AR + AP)
- ✅ Executive Dashboard (8 KPIs + Revenue Trend + Top Customers chart + Expense Breakdown + AR/AP Aging + 8-month trend)

### 💼 الـ 2026 Operational Scenario (Sprint 58c):
- **Master data:** Holding + 2 subsidiaries + 6 customers + 5 vendors + 10 items + 3 projects + 10 employees + 6 cost centers
- **Phase 2:** Opening balances (Capital 3M LYD + Long-term loan 500K + Furniture 80K + Prepaid rent 60K + Raw materials 150K)
- **Phase 3:** 6 sales invoices + 5 vendor bills + 4 receipts + 5 vendor payments + 7 payrolls + 6 progress billings + 4 project cost postings + 5 bank charges
- **Phase 4:** 8 monthly depreciation entries
- **Phase 5:** Income tax provision (80K)
- **Phase 6:** Year-end closing (3 entries)
- **Pre-close totals:** Rev 2,240,989 / Exp 669,496 / Net 1,571,493 LYD

### 🌳 Professional 4-level CoA (Sprint 58b):
- **153 accounts:** 10 L1 (account types) + 25 L2 (sub-classes) + 56 L3 (control) + 64 L4 (detail, postable)
- **L4-only posting rule:** L1/L2/L3 غير postable (enforced via `is_postable=false`)
- **L1 codes:** 0=Holding / 1=Assets / 2=Liab / 3=Equity / 4=Rev / 5=COGS / 6=OpEx / 7=Other / 8=Tax / 9=Closing
- **L3 examples:** 1101=Cash, 1102=Banks, 1201=AR, 2101=AP, 3101=Capital, 4301=Project Revenue control, 5201=Project Materials, 9201=WIP
- **L4 format:** `L3-serial` (e.g., 1101-001, 1201-001, 2101-001, 4301-001, 9201-001..003 per project)

### 📦 المميزات الإضافية (Sprints 47+):
- **Auth helper:** `authedFetch()` في `src/frontend/lib/api.ts` (يحل مشكلة bare fetch 401 — Sprint 58 hotfix)
- **Modern UI components:** PageHero, StatCard, StatusPill, ModernTable, chip-based tabs
- **3-Version UI:** V0 (port 3000) = الـ demo للعميل. V1/V2/V3 موجودة لكن **STOPPED** — متاحين لكن ما نشتغل عليهم (per Sprint 52 directive)
- **DEC-087 fix:** `ArabicDevSeederHostedService` (C# UTF-8) — لا تستخدم PowerShell ConvertTo-Json للـ Arabic data

---

## 6. 📍 آخر Worktree اشتغلنا عليه

```
C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21\
```

- **Branch:** `feature/sprint-52-v0-polish`
- **HEAD:** `e47068c` (Sprint 58: Professional 4-level CoA + 2026 Scenario + Plan)
- **Working tree:** clean

**worktrees أخرى موجودة عندك** (لكن ما مستّها في آخر 9 sprints):
- `ERP-Holding` (default workspace — main worktree, على `develop` @ `5f6709c1`)
- `ERP-Holding-sprint-9` .. `ERP-Holding-sprint-18` (قديمة)
- `ERP-Holding-sprint-58` (Mephisto's session، على `feature/sprint-58-contracts-billings` @ `ada78a4` — **اندمج في `e47068c`**)
- `ERP-Holding-sprint-57`, `ERP-Holding-sprint-21` (نفسه sprint-21)
- `erp-v2`, `pocket`, `user-manual`, `user-manual-assets`, `erp-extract`, `AnasSert-Accounting-FinTech-main`, `.opencode`

---

## 7. ⚙️ الـ State الحالي (live)

| Component | Port | Status | ملاحظة |
|---|---|---|---|
| **PostgreSQL 17 (native)** | 5432 | ✅ RUNNING (pid 5836) | DB: `erp_system` (owner: `erp`) |
| **Backend (ERPSystem.Host)** | 5000 (مش 5001) | ⚠️ OFF | آخر start أمس. لازم `ASPNETCORE_ENVIRONMENT=Development` |
| **Frontend (Next.js dev)** | 3000 | ⚠️ OFF | `.env.local` يصرّ على :5001، غيّرنا لـ :5000 أمس |
| **Docker Desktop** | — | ⚠️ OFF | mvp-docker ما شغّال |
| **mvp-docker stack** | 3000, 5432, 5000 | ⚠️ OFF | conflict مع local FE on :3000 |

### Known issues (documented):
- ⚠️ **BS variance ~720K LYD** — legacy unified CoA data (Sprint 50-55) + new 2026 scenario data coexist. Fix: disable legacy seeders OR add capital adjustment entry
- ⚠️ **mvp-docker stuck** in restart loop (port 3000/5432/5000 conflict with local dev) — Not blocking local dev
- ⚠️ **Sidebar shows "v1.0.12 · Sprint 39"** — version label not updated to Sprint 58, but routes/data are current
- ⚠️ **WIP endpoint `GetWipAsync` line 336** still throws 42883 varchar/integer (Sprint 58 hotfix1 fixed ApproveAsync but not GetWip)

---

## 8. 🚀 كيف نكمل من هنا؟

### Step 1: Resume (أنت في Layer 1)
```powershell
# 1) شغّل BE
cd C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21\src\backend\Host
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
# BE على :5001 (الـ FE يصرّ على 5001، رغم إن البيس config على 5000)

# 2) شغّل FE في tab ثاني
cd C:\Users\Anas\.minimax-agent\projects\ERP-Holding-sprint-21\src\frontend
npm run dev
# FE على :3000

# 3) تصفح
# http://localhost:3000/login
# admin@erp.local / ChangeMe1234!
```

### Step 2: قرّر الـ next move
- **(A) "ادفع"** → Mode 2: push كل الـ 7 commits → CI 6/6 → mvp-docker rebuild → Telegram ping → النسخة للعميل
- **(B) Sprint 58 follow-up** → disable legacy seeders → BS variance = 0 (يحتاج drop DB + re-seed)
- **(C) Sprint 59+** → V1/V2/V3 cleanup، Path D (production prep)، أو per-screen improvements

### Step 3: Follow workflow rules
- **كل سطر جديد في الكود** → DOX pass → update AGENTS.md → CHANGELOG entry
- **كل Sprint** → plan في `docs/plans/sprint-NN-*.md` + retro في `docs/team-charters/retrospectives/sprint-NN-retro.md`
- **DECs + Lessons** → CHANGELOG.md (مع رقم decision: DEC-NNN) + memory file
- **Mode 1 = default** → ادفع فقط لما تقول "ادفع"
- **3-Layer** → Layer 1 development، Layer 2 client demo (mvp-docker)، Layer 3 frozen

---

## 9. 📞 الـ Roles (من لا يحتاج مراجعة)

| Role | مين | شو يسوي |
|---|---|---|
| **Project Owner** | Anas | Constitution, staging/production, architecture changes |
| **Cloud Coordinator** | Siti (Mavis mode) | Plan, hand-offs, verify, merge, governance files |
| **Architect / Strategic Advisor** | Muhammad (Mavis mode) | Analysis, decisions, retrospectives — **أنت هنا** |
| **Tech Lead (Local)** | Mavis Local (Windows) | Implementation, Jimis, PRs, --admin merge on develop |
| **DevOps** | Dev (Mavis mode) | CI, infra, crons |
| **External Tech Lead (sandbox)** | Mephisto | مستقل على `feature/sprint-4-polish-demo-data` |
| **E2E team** | Abdo's team | Playwright verification on `feature/abdo-team` |

---

## 10. 📚 Lessons Learned الأخيرة (من الـ CHANGELOG)

- **L119:** Many FE pages call `fetch('/api/...')` directly — footgun. Use `api` axios instance OR `authedFetch()`.
- **L120:** BE serializes enums as STRINGS via `JsonStringEnumConverter`. FE must use `string` for status/type, NOT `number`.
- **L121:** `npm run build` kills `npm run dev` on :3000. After build, restart dev.
- **L122:** Playwright screenshot of authenticated page requires login flow first.
- **L123:** `[id]` directory segments in Next.js require literal `[` and `]` in paths.
- **L127:** When redesigning pages, always check actual API response format FIRST.
- **L128:** When a form field expects Guid FK, NEVER use free-text input. Use Select dropdown.
- **L130-L139:** Various seeders + Sprint 58 lessons documented in CHANGELOG.

---

**Last updated:** 2026-08-19 (by Muhammad/Mavis orchestrator)
**Status:** Ready to resume Mode 1 work
**Next action:** Awaiting Anas's decision (A) ادفع / (B) Sprint 58 follow-up / (C) Sprint 59+
