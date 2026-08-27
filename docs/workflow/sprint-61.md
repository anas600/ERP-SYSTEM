# 📋 Sprint 61 — Engineer's Report + 5 Permanent Fixes (2026-08-27)

> **Sprint Hand-off Document** — Contract for Workers (Jimis) and Mavis Local
>
> **Status:** 🟡 In Progress (M1-Local phase)
> **Branch:** `feature/sprint-61-engineer-report` (off `develop @ 9728d17`)
> **Mode:** LOCAL-ONLY (Mode 1) — no push, no PR until Anas says "ادفع"
> **Owner:** Anas (Project Owner) | **Lead:** Muhammad (Mavis — Admin)
> **Duration:** 4 days (27-30 Aug 2026)

---

## 🎯 Sprint Goal

إضافة **Engineer's Report** module (DEC-192..195) — تقارير المهندس اليومية للمشاريع، مع الصور والاعتماد الإلكتروني. هذا هو الموديول اللي طلبه العميل (CEO + محاسب) في اجتماع 22-Aug-2026.

**+ 5 Permanent Fixes** من Sprint 60 (DEC-196..198 + L175 fix):
- L47: Phase6 VersionInfo recreation
- L48: EnsureDefaultRolesAsync in bootstrap
- L49: AuthService connection visibility fix
- L51: CI no-tenant-id.yml update
- L175: /api/auth/admin-bootstrap endpoint

---

## 📦 DECs in Scope (6 + 1 = 7)

| DEC | الوصف | Wave | الحجم |
|-----|-------|------|-------|
| **DEC-192** | Engineer's Daily Report (CRUD + status) | 1 + 2 | كبير |
| **DEC-193** | Photos (upload + display + storage) | 1 + 2 | متوسط |
| **DEC-194** | Sign-off (electronic approval workflow) | 1 + 2 | متوسط |
| **DEC-195** | Tests + Documentation | 3 | متوسط |
| **DEC-196** | L51 + L47 (CI fix + Phase6 fix) | 1 | صغير |
| **DEC-197** | L48 + L49 (bootstrap + auth fix) | 1 | صغير |
| **DEC-198** | L175 (/api/auth/admin-bootstrap) | 1 | متوسط |

---

## 🌊 Wave Structure (3 waves)

### Wave 1 — Foundation (Schema + Entities + 5 Fixes)
**Target:** 1-2 hours per worker. **Workers:** 2 (parallel via git worktrees per L173)

#### Worker 1A — Sprint 61 Schema + Entities
**Scope (files):**
- `src/backend/Shared/Migrations/Sprint61_EngineerReportSchema_20260827_120000.cs` (new) — 3 new tables
- `src/backend/Shared/Migrations/Sprint61_EngineerReportSeed_20260827_130000.cs` (new) — optional seed
- `src/backend/Modules/Projects/Entities/EngineerReport.cs` (new) — main entity + EngineerReportStatus enum
- `src/backend/Modules/Projects/Entities/EngineerReportPhoto.cs` (new) — photo entity
- `src/backend/Modules/Projects/Entities/EngineerReportSignoff.cs` (new) — signoff entity
- `src/backend/Modules/Projects/Application/Dtos/EngineerReportDtos.cs` (new) — DTOs (Create, Update, Response, SignoffRequest, PhotoResponse)
- `src/backend/Shared/data-types/engineer_reports.json` (new) — DataTypeMigrator schema
- `src/backend/Shared/data-types/engineer_report_photos.json` (new)
- `src/backend/Shared/data-types/engineer_report_signoffs.json` (new)

**Schema:**
```sql
CREATE TABLE engineer_reports (
  id UUID PRIMARY KEY,
  company_id UUID NOT NULL REFERENCES companies(id),
  project_id UUID NOT NULL REFERENCES projects(id),
  report_date DATE NOT NULL,
  engineer_id UUID NOT NULL REFERENCES users(id),
  status TEXT NOT NULL DEFAULT 'Draft',  -- Draft | Submitted | Approved | Rejected
  weather TEXT,
  work_done TEXT NOT NULL,
  issues TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (project_id, report_date)  -- one report per project per day
);
CREATE INDEX idx_engineer_reports_company_project ON engineer_reports(company_id, project_id);
CREATE INDEX idx_engineer_reports_status ON engineer_reports(company_id, status);

CREATE TABLE engineer_report_photos (
  id UUID PRIMARY KEY,
  report_id UUID NOT NULL REFERENCES engineer_reports(id) ON DELETE CASCADE,
  company_id UUID NOT NULL REFERENCES companies(id),  -- denormalized for FK performance
  file_path TEXT NOT NULL,
  caption TEXT,
  uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_engineer_report_photos_report ON engineer_report_photos(report_id);

CREATE TABLE engineer_report_signoffs (
  id UUID PRIMARY KEY,
  report_id UUID NOT NULL REFERENCES engineer_reports(id) ON DELETE CASCADE,
  company_id UUID NOT NULL REFERENCES companies(id),
  signer_id UUID NOT NULL REFERENCES users(id),
  signer_role TEXT NOT NULL,  -- 'PM' | 'Client' | 'Engineer'
  signed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  signature_text TEXT,  -- optional text signature
  comment TEXT,
  approved BOOLEAN NOT NULL  -- true=approved, false=rejected
);
CREATE INDEX idx_engineer_report_signoffs_report ON engineer_report_signoffs(report_id);
```

**Migration version:** `[Migration(20260827120000)]` (14-digit format per L046)

**Tests required (15+):**
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint61EngineerReportSchemaMigrationTests.cs` (8 tests)
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint61EngineerReportEntitiesTests.cs` (7 tests)

**Out of scope (for Wave 2):** Repositories, Services, Controllers, Frontend.

#### Worker 1B — 5 Permanent Fixes from Sprint 60
**Scope (files):**

- **L47:** `src/backend/Shared/Migrations/Phase6_InitialSchema_20260101_000000.cs` — add `Execute.Sql("CREATE TABLE VersionInfo...")` AFTER the `DROP SCHEMA IF EXISTS public CASCADE;` line.

- **L48:** `src/backend/Shared/Identity/DefaultHoldingBootstrapHostedService.cs` — add call to `EnsureDefaultRolesAsync` after creating the Holding Company. Move logic from `RoleRepository.EnsureDefaultRolesAsync` to be invoked from the bootstrap.

- **L49:** `src/backend/Modules/Identity/Application/Services/AuthService.cs` line 191 — change `_users.GetUserCompaniesAsync(user.Id, ct)` to `_users.GetUserCompaniesAsync(user.Id, conn, tx, ct)` (use the connection-aware overload).

- **L51:** `.github/workflows/no-tenant-id.yml` — add exception for `NotContain.*tenant_id` patterns (test assertion literals). Use ripgrep's `--ignore-file` or similar pattern. Also exclude `*.Tests/**` from the grep target.

- **L175:** New endpoint `POST /api/auth/admin-bootstrap` in `src/backend/Host/Controllers/AuthController.cs` (or new `AdminAuthController`). Accepts `{email, password, fullName}` and creates the first admin user + user_role + user_company link. Bypasses the broken register flow. Should be idempotent (returns existing if already bootstrapped). Add DI registration + 1 test.

**Tests required (5):**
- `src/backend/Tests/ERPSystem.Tests/Identity/Sprint61L47L48L49L175FixesTests.cs` (5 tests)
- Update existing CI to verify no-tenant-id.yml still triggers correctly (1 test for L51)

**Out of scope:** No Engineer's Report work.

### Wave 2 — API + Frontend (parallel)
**Target:** 2-3 hours per worker. **Workers:** 2 (BE + FE in parallel)

#### Worker 2A — Backend API (Repositories + Services + Controllers)
**Scope (files):**
- `src/backend/Modules/Projects/Infrastructure/EngineerReportRepository.cs` (new)
- `src/backend/Modules/Projects/Infrastructure/EngineerReportPhotoRepository.cs` (new)
- `src/backend/Modules/Projects/Infrastructure/EngineerReportSignoffRepository.cs` (new)
- `src/backend/Modules/Projects/Infrastructure/IRepositories.cs` (modify — add 3 new interfaces)
- `src/backend/Modules/Projects/Application/Services/EngineerReportService.cs` (new)
- `src/backend/Host/Controllers/EngineerReportsController.cs` (new)
- `src/backend/Host/Controllers/EngineerReportPhotosController.cs` (new) — handles file upload
- `src/backend/Host/Program.cs` (modify — add DI for new services + repos)

**Endpoints (8 new):**
| Method | Path | Purpose |
|--------|------|---------|
| GET | /api/projects/{id}/engineer-reports | List by project |
| GET | /api/engineer-reports/{id} | Details |
| POST | /api/projects/{id}/engineer-reports | Create (Draft) |
| PUT | /api/engineer-reports/{id} | Update (only Draft) |
| POST | /api/engineer-reports/{id}/submit | Transition Draft → Submitted |
| GET | /api/engineer-reports/{id}/photos | List photos |
| POST | /api/engineer-reports/{id}/photos | Upload photo (multipart) |
| POST | /api/engineer-reports/{id}/signoff | Approve/Reject (PM or Client role) |

**Storage strategy:** Photos stored locally under `wwwroot/uploads/engineer-reports/{reportId}/` (gitignored). Return public URL `/uploads/engineer-reports/{reportId}/{filename}`.

**Tests (10+):**
- `src/backend/Tests/ERPSystem.Tests/Projects/EngineerReportServiceTests.cs` (5 tests)
- `src/backend/Tests/ERPSystem.Tests/Projects/EngineerReportsControllerTests.cs` (5 tests)

**Out of scope:** Frontend pages, no Project entity changes.

#### Worker 2B — Frontend Pages
**Scope (files):**
- `src/frontend/app/(authenticated)/projects/[id]/engineer-reports/page.tsx` (new) — list of reports
- `src/frontend/app/(authenticated)/projects/[id]/engineer-reports/new/page.tsx` (new) — create form
- `src/frontend/app/(authenticated)/engineer-reports/[id]/page.tsx` (new) — report detail + sign-off
- `src/frontend/components/engineer-report/ReportForm.tsx` (new)
- `src/frontend/components/engineer-report/PhotoUploader.tsx` (new)
- `src/frontend/components/engineer-report/SignoffPanel.tsx` (new)
- `src/frontend/lib/api.ts` (modify — add 8 new API methods)
- `src/frontend/app/(authenticated)/projects/[id]/page.tsx` (modify — add "Engineer Reports" tab)

**UI requirements (bilingual AR/EN):**
- Reports list: filter by date range + status
- Create form: date picker (defaults to today), weather, work_done (textarea), issues (textarea), photo upload (multiple)
- Detail page: read-only view, sign-off button (PM/Client only), photo gallery

**Tests (5+):**
- `src/frontend/__tests__/engineer-report/ReportForm.test.tsx` (2 tests)
- `src/frontend/__tests__/engineer-report/PhotoUploader.test.tsx` (2 tests)
- `src/frontend/__tests__/engineer-report/SignoffPanel.test.tsx` (1 test)

**Out of scope:** Backend, no new design system components (use existing shadcn/ui).

### Wave 3 — Integration + Verification (M3-Trust by Muhammad)
**Target:** 1-2 hours.

#### Tasks (by Muhammad, no worker):
1. `dotnet build` + `dotnet test` — verify all Sprint 61 + 5 fix tests pass
2. `npm run typecheck` + `npm run build` — verify FE
3. Update `src/backend/Modules/Projects/AGENTS.md` — add EngineerReport section
4. Update root `AGENTS.md` — add Sprint 61 lessons
5. Update `docs/CHANGELOG.md` — Sprint 61 entry
6. Open browser (Trust Mode) — verify 3-5 pages work
7. Run Sprint Closure Checklist (13 steps in Notion)

---

## 🛡️ Quality Gates (per Worker contract)

```
[ ] dotnet build → 0 errors
[ ] dotnet test → 0 regressions (existing + new)
[ ] npm run typecheck → 0 errors
[ ] npm run build → OK
[ ] No tenant_id (use "ten" + "ant_id" string concat if testing the literal)
[ ] No secrets
[ ] company_id in every new entity
[ ] AGENTS.md (nearest owning) updated
[ ] CHANGELOG.md entry added
[ ] 1 test per endpoint (per Article 11)
[ ] Conventional Commits format
[ ] Report back to Muhammad with summary
```

---

## 🎯 Success Criteria (Sprint 61 "Done" Definition)

- [ ] All 7 DECs delivered (DEC-192..198)
- [ ] 35+ new tests pass (15 Wave 1 + 10 Wave 2 BE + 5 Wave 2 FE + 5 fix tests)
- [ ] Trust Mode verification: 3-5 pages work in browser
- [ ] No regressions in existing 444+ tests
- [ ] AGENTS.md + CHANGELOG.md updated
- [ ] Sprint Closure Checklist (13 steps) complete in Notion
- [ ] Waiting for Anas "ادفع" (Mode 2)

---

## 📞 Escalation Path

Workers → Muhammad (M1-Exec) → Anas (M2-Discussion).
- **Architecture conflict:** Muhammad escalates immediately
- **Out-of-scope discovery:** Report, don't absorb
- **Blocked on dependency:** Report + wait
- **CI failure you can't fix:** Report + work around if safe

---

## 🔗 Related Documents

- **AGENTS.md (root):** Sprint 60 lessons (L046..L052 + L175) — relevant for fix patterns
- **.mavis/AGENTS.md:** Worker contract (rules 1-9)
- **src/backend/Modules/Projects/AGENTS.md:** Module context
- **Notion:** [Sprint 61 in Sprints DB](https://app.notion.com/p/0d5ed7488a9c436bb13878d103414266) + [DEC Log](https://app.notion.com/p/53a5106a37db4e26afd09296a07b0e34) + [Tasks](https://app.notion.com/p/4a715010a0284f468173563a19aa2419)

---

**Written by:** Muhammad (Mavis — M1-Exec) | 2026-08-27
**Approver:** Anas (Project Owner)
**Status:** 🟡 Active — Workers may begin
