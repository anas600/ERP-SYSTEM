# 📊 src/backend/Modules/Projects/AGENTS.md

> Projects Module — ✅ Phase 2.1 + Sprint 57 (Project P&L) + Sprint 61 Wave 1A (Engineer Report schema)
>
> محدّث: 2026-08-27 — Sprint 61 Wave 1A / DEC-192..194 (Engineer Report schema + entities)
>
> **Phase 6 (2026-07-27) — Multi-Company update:** Per Constitution Article 3, this module now uses `ICompanyContext` (instead of removed `ITenantContext`). All queries filter by `company_id` (instead of removed `tenant_id`). Users are global, companies are many. JWT carries `default_company_id` + `company_ids[]`. See root [AGENTS.md](../../../../AGENTS.md#-multi-company-convention-per-constitution-article-3) and [docs/PHASE6-RELEASE-NOTES.md](../../../../PHASE6-RELEASE-NOTES.md) for migration guide.
>
> **Sprint 57 (2026-08-07) — Project P&L:** أضفنا `project_id` على journal_entries (DEC-160) + ProjectPnLService (DEC-161) + UI tab "الأرباح والخسائر" (DEC-162). الـ P&L يقرأ من sales_invoices (revenue) + journal_lines على Expense accounts (costs).
>
> **Sprint 61 Wave 1A (2026-08-27) — Engineer's Daily Report (DEC-192..194 foundation):** أضفنا 3 جداول (engineer_reports + engineer_report_photos + engineer_report_signoffs) + 3 entities + DTOs. Wave 2A will add الـ repositories / services / controllers.

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
│       └── SupportingServices.cs        # Task, Resource, Budget, Assignment
└── Infrastructure/
    ├── IRepositories.cs
    ├── ProjectRepository.cs
    ├── TaskRepository.cs
    ├── ResourceRepository.cs
    ├── ProjectBudgetRepository.cs      # + RecalculateSpentAsync (SQL agg)
    └── ResourceAssignmentRepository.cs
```

## 🆕 Sprint 61 — Engineer's Daily Report (DEC-192..194 foundation)

> **Status:** Wave 1A ✅ DONE (schema + entities + DTOs). Wave 2A: Repositories + Services + Controllers (next worker). Wave 2B: Frontend. Wave 3: Integration + verification.

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

### Out of scope for Wave 1A (Wave 2A will do)
- `EngineerReportRepository` / `EngineerReportPhotoRepository` / `EngineerReportSignoffRepository`
- `EngineerReportService` (CRUD + submit + signoff logic)
- `EngineerReportsController` + `EngineerReportPhotosController` (8 endpoints)
- `Program.cs` DI registration (Admin will do after merge per Worker contract)

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
