# 📊 src/backend/Modules/Projects/AGENTS.md

> Projects Module — ✅ Phase 2.1 + Sprint 57 (Project P&L) + Sprint 61 (Engineer Report: Wave 1A schema + Wave 2A API)
>
> محدّث: 2026-08-27 — Sprint 61 Wave 2A / DEC-192..194 (API: 3 repos + service + 8 endpoints)
>
> **Phase 6 (2026-07-27) — Multi-Company update:** Per Constitution Article 3, this module now uses `ICompanyContext` (instead of removed `ITenantContext`). All queries filter by `company_id` (instead of removed `tenant_id`). Users are global, companies are many. JWT carries `default_company_id` + `company_ids[]`. See root [AGENTS.md](../../../../AGENTS.md#-multi-company-convention-per-constitution-article-3) and [docs/PHASE6-RELEASE-NOTES.md](../../../../PHASE6-RELEASE-NOTES.md) for migration guide.
>
> **Sprint 57 (2026-08-07) — Project P&L:** أضفنا `project_id` على journal_entries (DEC-160) + ProjectPnLService (DEC-161) + UI tab "الأرباح والخسائر" (DEC-162). الـ P&L يقرأ من sales_invoices (revenue) + journal_lines على Expense accounts (costs).
>
> **Sprint 61 (2026-08-27) — Engineer's Daily Report (DEC-192..194):** Wave 1A أضاف schema/entities/DTOs (3 tables + 3 entities + DTOs + 15 tests). Wave 2A أضاف الـ API layer (3 repositories + service + 2 controllers = 8 endpoints + 22 tests). Wave 2B will add the frontend.

## شو فيه

```
Projects/
├── Entities/
│   ├── Project.cs          # Project + ProjectStatus
│   ├── ProjectTask.cs      # ProjectTask + TaskStatus
│   ├── Resource.cs         # Resource + ResourceType
│   ├── ProjectBudget.cs    # SpentAmount/CommittedAmount/AvailableAmount
│   ├── ResourceAssignment.cs # HourlyRate snapshot + computed EstimatedCost
│   ├── EngineerReport.cs          # Sprint 61 / DEC-192 — EngineerReport + EngineerReportStatus
│   ├── EngineerReportPhoto.cs     # Sprint 61 / DEC-193 — photo attachment (file_path + caption)
│   └── EngineerReportSignoff.cs   # Sprint 61 / DEC-194 — electronic signoff (PM/Client/Engineer)
├── Application/
│   ├── ProjectsDtos.cs     # كل الـ DTOs (+ ProjectPnLResponse, ProjectPnLLine من Sprint 57)
│   ├── EngineerReportDtos.cs      # Sprint 61 — Create/Update/Signoff/Photo/Response
│   ├── Validators.cs       # FluentValidation
│   └── Services/
│       ├── ProjectService.cs           # CRUD + status workflow + auto-bootstrap
│       ├── ProjectPnLService.cs        # Sprint 57 / DEC-161: P&L aggregation
│       ├── EngineerReportService.cs    # Sprint 61 Wave 2A / DEC-192..194 — CRUD + submit + signoff
│       └── SupportingServices.cs        # Task, Resource, Budget, Assignment
└── Infrastructure/
    ├── IRepositories.cs
    ├── ProjectRepository.cs
    ├── TaskRepository.cs
    ├── ResourceRepository.cs
    ├── ProjectBudgetRepository.cs      # + RecalculateSpentAsync (SQL agg)
    ├── ResourceAssignmentRepository.cs
    ├── EngineerReportRepository.cs        # Sprint 61 / DEC-192
    ├── EngineerReportPhotoRepository.cs   # Sprint 61 / DEC-193
    └── EngineerReportSignoffRepository.cs # Sprint 61 / DEC-194
```

## 🆕 Sprint 61 — Engineer's Daily Report (DEC-192..194 foundation)

> **Status:** Wave 1A ✅ DONE (schema + entities + DTOs). Wave 2A ✅ DONE (repositories + service + 8 endpoints). Wave 2B: Frontend. Wave 3: Integration + verification.

### Schema (3 tables)
- **`engineer_reports`** — تقرير المهندس اليومي. One report per project per day (`UNIQUE (project_id, report_date)`). Status: Draft | Submitted | Approved | Rejected.
- **`engineer_report_photos`** — صور مرفقة. `file_path` على القرص + `caption` اختياري. `ON DELETE CASCADE` من engineer_reports.
- **`engineer_report_signoffs`** — اعتماد إلكتروني. `signer_role` = 'PM' | 'Client' | 'Engineer' + `approved` bool. `ON DELETE CASCADE`.

### Workflow (DEC-194 state machine)
```
Draft → Submitted → Approved
                ↘ Rejected → (engineer revises) → Draft → Submitted → …
```
- **Draft** — engineer is still writing; editable
- **Submitted** — locked; PM/Client can sign off
- **Approved** — final; immutable
- **Rejected** — engineer revises and resubmits

### Files added in Wave 1A
- `src/backend/Shared/Migrations/Sprint61_EngineerReportSchema_20260827_120000.cs` (3 tables, idempotent)
- `src/backend/Shared/Migrations/Sprint61_EngineerReportSeed_20260827_130000.cs` (no-op placeholder)
- `src/backend/Host/data-types/{engineer_reports,engineer_report_photos,engineer_report_signoffs}.json`
- `src/backend/Modules/Projects/Entities/{EngineerReport,EngineerReportPhoto,EngineerReportSignoff}.cs`
- `src/backend/Modules/Projects/Application/Dtos/EngineerReportDtos.cs`
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint61EngineerReportSchemaMigrationTests.cs` (8 tests)
- `src/backend/Tests/ERPSystem.Tests/Projects/Sprint61EngineerReportEntitiesTests.cs` (7 tests)

## 🆕 Sprint 61 Wave 2A — Engineer's Report API (DEC-192..194)

> **Status:** ✅ DONE. Repositories + Service + 2 controllers (8 endpoints) + 22 tests.

### Files added in Wave 2A
- `src/backend/Modules/Projects/Infrastructure/EngineerReportRepository.cs` — Dapper repo (list/get/insert/update + UNIQUE check).
- `src/backend/Modules/Projects/Infrastructure/EngineerReportPhotoRepository.cs` — Dapper repo (list/get/insert/delete + count).
- `src/backend/Modules/Projects/Infrastructure/EngineerReportSignoffRepository.cs` — Dapper repo (list/get/insert).
- `src/backend/Modules/Projects/Infrastructure/IRepositories.cs` — modified (added 3 new interfaces).
- `src/backend/Modules/Projects/Application/Services/EngineerReportService.cs` — service with state machine (Draft → Submitted → Approved/Rejected), L19 company-safety, photo upload, and signoff workflow.
- `src/backend/Host/Controllers/EngineerReportsController.cs` — 7 endpoints (list/get/create/update/submit/listPhotos/signoff).
- `src/backend/Host/Controllers/EngineerReportPhotosController.cs` — 1 endpoint (multipart file upload, 10MB cap, allowed extensions).
- `src/backend/Host/Program.cs` — modified (DI registration + `EnumStringTypeHandler<EngineerReportStatus>` for the TEXT→enum mapping).
- `src/backend/Tests/ERPSystem.Tests/Projects/EngineerReportServiceTests.cs` — 10 service tests (fake repos, no DB).
- `src/backend/Tests/ERPSystem.Tests/Projects/EngineerReportsControllerTests.cs` — 12 controller tests (mocked service, real multipart for upload).

### Endpoints (8)

| Method | Path | Purpose | Auth |
|--------|------|---------|------|
| GET    | /api/projects/{id}/engineer-reports | List (with from/to/status filters) | `[Authorize]` |
| GET    | /api/engineer-reports/{id} | Details (incl. photos + signoffs) | `[Authorize]` |
| POST   | /api/projects/{id}/engineer-reports | Create Draft (UNIQUE on project+date) | `[Authorize]` |
| PUT    | /api/engineer-reports/{id} | Update (only Draft) | `[Authorize]` |
| POST   | /api/engineer-reports/{id}/submit | Draft → Submitted | `[Authorize]` |
| GET    | /api/engineer-reports/{id}/photos | List photos | `[Authorize]` |
| POST   | /api/engineer-reports/{id}/photos | Upload (multipart, 10MB cap) | `[Authorize]` |
| POST   | /api/engineer-reports/{id}/signoff | Approve / Reject (PM/Client) | `[Authorize]` |

### L19 / DEC-095 compliance
- `ICompanyContext.CompanyId` is the single source of truth for companyId. The request DTOs do NOT carry a `CompanyId` (verified in `CreateEngineerReportRequest` / `UpdateEngineerReportRequest` / `SignoffRequest`).
- Photos copy `company_id` from the parent report (denormalized per DEC-193 design note) to avoid 2-table JOINs.
- Every WHERE / INSERT in the 3 repositories includes `company_id`.

### Photo storage
- Files written to `wwwroot/uploads/engineer-reports/{reportId}/{guid}.{ext}` (gitignored).
- Public URL `/uploads/engineer-reports/{reportId}/{filename}` is returned.
- Hard cap 10 MB per file. Allowed extensions: jpg/jpeg/png/gif/webp/heic.
- Best-effort file cleanup if the DB INSERT fails after the disk write succeeded.

## Domain Model

### Project Lifecycle
```
Planning → Active → OnHold → Completed
    ↓        ↓      ↓
  Cancelled ← Cancelled
```

Forward-only (لا يمكن الرجوع من Completed). Transition invalid → 400 BadRequest.

### ProjectBudget ↔ CostCenter (1:1)
- عند إنشاء Project → CostCenter تلقائياً (type=Project, code=CC-{projectCode})
- ProjectBudget يحمل `cost_center_id` + `account_id` (اختياري)
- **SpentAmount**: يحسب من `journal_lines` (WHERE cost_center_id=... AND je.status=Posted)
  - عبر `RecalculateSpentAsync` — aggregation SQL (debit - credit)
- **AvailableAmount** = BudgetAmount - SpentAmount - CommittedAmount

### ResourceAssignment
- يلتقط `HourlyRate` snapshot وقت التعيين (حتى لو تغير Resource.HourlyRate لاحقاً)
- `EstimatedHours` = (To - From).TotalHours
- `EstimatedCost` = EstimatedHours × HourlyRate

## Endpoints (17)

| Method | Path | الغرض |
|--------|------|-------|
| GET    | /api/projects | قائمة + filter بـ companyId/status |
| GET    | /api/projects/{id} | تفاصيل |
| POST   | /api/projects | إنشاء (auto CostCenter + Budget) |
| PUT    | /api/projects/{id} | تحديث |
| POST   | /api/projects/{id}/status | تغيير الحالة (workflow validation) |
| DELETE | /api/projects/{id} | soft-delete |
| GET    | /api/projects/{id}/tasks | قائمة المهام |
| GET    | /api/projects/{id}/budget | ميزانية |
| POST   | /api/projects/{id}/budget/recalculate | إعادة حساب Spent |
| **GET**| **/api/projects/{id}/pnl** | **Sprint 57: الأرباح والخسائر (Revenue − Costs)** |
| GET    | /api/projects/{id}/assignments | تعيينات الموارد |
| POST   | /api/projects/{id}/assignments | تعيين مورد |
| DELETE | /api/projects/{id}/assignments/{aid} | إزالة |
| GET/POST/PUT/DELETE | /api/tasks, /api/resources | CRUD |
| **GET/POST** | **/api/projects/{id}/engineer-reports** | **Sprint 61 Wave 2A: قائمة / إنشاء تقرير مهندس** |
| **GET/PUT** | **/api/engineer-reports/{id}** | **تفاصيل / تحديث (Draft فقط)** |
| **POST** | **/api/engineer-reports/{id}/submit** | **Draft → Submitted** |
| **GET** | **/api/engineer-reports/{id}/photos** | **قائمة الصور** |
| **POST** | **/api/engineer-reports/{id}/photos** | **رفع صورة (multipart)** |
| **POST** | **/api/engineer-reports/{id}/signoff** | **اعتماد / رفض (PM/Client)** |

## لما تشتغل هنا

- إضافة status جديد: عدّل `ProjectStatus` + `ProjectService.ChangeStatusAsync` (الـ validTransitions dict)
- إضافة field: migration جديدة + entity + DTO + service + repo
- حساب Spent: يستدعى `IBudgetService.RecalculateSpentAsync` (الآن يدوي — في Phase 2.4 يُستدعى تلقائياً على PostAsync لـ journal entry)
- **حساب P&L (Sprint 57)**: `IProjectPnLService.GetPnLAsync(projectId, from, to)` — يقرأ من sales_invoices (revenue) + journal_lines على Expense accounts. الـ Cost يحسب من الـ JE (مش من الفواتير مباشرة) لتجنّب double-counting لأن كل فاتورة مرحلة تولد JE تلقائياً.

## بعد التعديل

- شغّل `dotnet test` (20 tests جديد في Phase 2.1)
- إذا غيّرت workflow status: حدّث `ProjectService.ChangeStatusAsync` + tests

## تكامل مع الموديولات الأخرى

- **Finance** (Phase 1): `CostCenter` يُنشأ تلقائياً. `ProjectBudget.SpentAmount` يحسب من `journal_lines` المُرحّلة.
- **Inventory** (Phase 2.2-2.3): `StockMovement.ProjectId` و `ProjectMaterialRequested` event (مستقبلي)
- **Reporting** (Phase 2.5): P&L per Project عبر JOIN على CostCenter

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../Identity/AGENTS.md`](../Identity/AGENTS.md)
- [`../Finance/AGENTS.md`](../Finance/AGENTS.md) — cost_center integration
- [`../Companies/AGENTS.md`](../Companies/AGENTS.md) — CostCenter + Company link
- [`../Procurement/AGENTS.md`](../Procurement/AGENTS.md) — Phase 3 (PO/GR per project)
- [`../HR/AGENTS.md`](../HR/AGENTS.md) — Phase 3.5 (Resource Assignment → Employee)
- [`../Payroll/AGENTS.md`](../Payroll/AGENTS.md) — Phase 4 (Resource cost → Salary)


---

## 🤝 Cross-Team Coordination (Brainstorming Lab)

This project works with an analytical team via the **Brainstorming Lab**.

- **When to read from hub**: ONLY when explicitly instructed by the analytical team
- **Default**: Work from local context (this file + root `AGENTS.md` + source code)
- **Hub repo**: https://github.com/anas600/brainstorming-lab/tree/main/portals/02-session-002/

See root [`AGENTS.md`](../../../../AGENTS.md) for full cross-team protocol.

Token-efficient: ~50 tokens per cross-team directive (vs 500+ for full re-paste).
