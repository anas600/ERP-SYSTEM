# 📋 Sprint 62 — Progress Billing Refinement (2026-08-27)

> **Sprint Hand-off Document** — Contract for Workers (Jimis) and Mavis Local
>
> **Status:** 🟡 In Progress (Autonomous mode — Anas sleeping)
> **Branch:** `feature/sprint-62-progress-billing` (off `develop`)
> **Mode:** LOCAL-ONLY (M1-Local — no push until Anas's "ادفع")
> **Duration:** 4 days (planned 31 Aug - 3 Sep, but executing in autonomous run)

---

## 🎯 Sprint Goal

Refine **Progress Billing** module (Sprint 58/59 work) to support **Libyan construction context**:
- DEC-196 ✅ Done in Sprint 58 (NetAmount field)
- DEC-197 **NEW**: Regional premium — NDB (1.5%) + CIT + SS automatic deductions in NDB regions
- DEC-198 **NEW**: PDF export endpoint for billing documents
- DEC-199 ⏸ Deferred: Client portal (Sprint 65)

---

## 📦 DECs in Scope (2 + 1 skip)

| DEC | الوصف | Wave | الحجم |
|-----|-------|------|-------|
| **DEC-196** | net_amount calculated | (skip) | done in Sprint 58 |
| **DEC-197** | Regional premium (NDB + CIT + SS) | 1 + 2 | متوسط |
| **DEC-198** | PDF export endpoint | 2 | متوسط |
| **DEC-199** | Client portal | (defer) | كبير |

---

## 🌊 Wave Structure (2 waves)

### Wave 1 — Foundation (Schema + Entities + Service logic)
**Target:** 45-60 min. **Worker:** 1.

#### Worker 1A — Regional Premium Schema + Service
**Scope (files):**

**New files:**
1. `src/backend/Shared/Migrations/Sprint62_RegionalPremium_20260827_160000.cs` — FluentMigrator migration
2. `src/backend/Host/data-types/regional_premiums.json` — DataTypeMigrator schema
3. `src/backend/Modules/Projects/Entities/RegionalPremium.cs` — entity (or add to existing schema)
4. `src/backend/Modules/Projects/Application/Services/RegionalPremiumService.cs` — calculation service
5. `src/backend/Modules/Projects/Application/Dtos/RegionalPremiumDtos.cs` — DTOs

**Modified files:**
6. `src/backend/Modules/Projects/Entities/ProgressBilling.cs` — add `RegionalPremiumDeducted` + `NetAmountAfterPremium` fields
7. `src/backend/Modules/Projects/Application/Services/BillingService.cs` — apply regional premium in calculation

**Schema:**
```sql
CREATE TABLE regional_premiums (
  id UUID PRIMARY KEY,
  company_id UUID NOT NULL REFERENCES companies(id),
  project_id UUID NOT NULL REFERENCES projects(id),
  region TEXT NOT NULL,  -- 'Tripoli', 'Benghazi', 'Misrata', 'NDB-Oil', 'NDB-Gas'
  ndb_percent DECIMAL(5,2) DEFAULT 1.5,    -- NDB deduction
  cit_percent DECIMAL(5,2) DEFAULT 5.0,    -- Corporate Income Tax
  ss_percent DECIMAL(5,2) DEFAULT 0.0,     -- Social Security (varies)
  is_active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Algorithm in BillingService.CreateAsync:**
```
gross = contract.contract_value × (work_completed_percent / 100)
advance_deducted = MIN(gross, MAX(0, total_advance - sum_prev))
retention_deducted = (n >= contract.retention_start_billing) ? gross × retention% : 0
regional_premium_deducted = (project.is_ndb_region)
    ? gross × (ndb% + cit% + ss%) / 100
    : 0
net = gross - advance_deducted - retention_deducted - regional_premium_deducted
```

**Tests (8+):**
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint62RegionalPremiumMigrationTests.cs` (3 tests)
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint62RegionalPremiumServiceTests.cs` (5 tests)

### Wave 2 — API + PDF Export (1 worker)
**Target:** 45-60 min.

#### Worker 2A — API Endpoints + PDF Export
**Scope (files):**

**New files:**
1. `src/backend/Host/Controllers/RegionalPremiumsController.cs` — 4 endpoints (CRUD)
2. `src/backend/Host/Controllers/BillingPdfController.cs` — 1 endpoint
3. `src/backend/Modules/Projects/Application/Services/PdfExportService.cs` — PDF generation
4. `src/backend/Tests/ERPSystem.Tests/Projects/RegionalPremiumsControllerTests.cs` (4 tests)
5. `src/backend/Tests/ERPSystem.Tests/Projects/BillingPdfControllerTests.cs` (2 tests)

**Modified files:**
6. `src/backend/Modules/Projects/Application/ProjectsDtos.cs` — add RegionalPremium CRUD DTOs, PDF response
7. `src/backend/Host/Program.cs` — DI for new services

**Endpoints (5 new):**
| Method | Path | Purpose |
|--------|------|---------|
| GET | /api/projects/{id}/regional-premiums | List |
| POST | /api/projects/{id}/regional-premiums | Create |
| PUT | /api/projects/{id}/regional-premiums/{id} | Update |
| DELETE | /api/projects/{id}/regional-premiums/{id} | Delete |
| GET | /api/projects/{id}/billings/{id}/pdf | PDF export (HTML rendered) |

**PDF approach:** Generate HTML → use simple HTML-to-PDF library or return HTML for browser print.

### Out of Scope
- DEC-199 Client portal (defer to Sprint 65)
- Sprint 65 Finance↔Projects integration

---

## 🛡️ Quality Gates

```
[ ] dotnet build → 0 errors
[ ] dotnet test → 0 regressions
[ ] Sprint62 tests pass
[ ] No tenant_id, no secrets, no EF Core
[ ] CHANGELOG.md entry added
[ ] AGENTS.md updated
[ ] Conventional Commits
```

---

## 🎯 Success Criteria

- DEC-197 + DEC-198 delivered
- 10+ new tests pass
- E2E: Create project → Set regional premium → Create billing → Verify net amount = gross - all deductions
- PDF endpoint returns valid PDF

---

**Written by:** محمد (Mavis — M1-Exec, autonomous mode) | 2026-08-27
**Awaiting:** Anas "ادفع" for Mode 2 push (Sprint 61 first)
