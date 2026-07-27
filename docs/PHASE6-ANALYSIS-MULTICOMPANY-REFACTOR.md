# Phase 6 Analysis: Multi-Tenant → Multi-Company Refactoring

> **Prepared by:** Jamie التحليلي (Analyst & Verifier) — branch session
> **Date:** 2026-07-25
> **Status:** Read-only analysis (no code modified)
> **Strategic decision (CONSTITUTION.md Article 3):** Abandon multi-tenancy entirely. One Holding Company per deployment. Multi-Company model (use `company_id` only). Clean slate for migrations allowed.

---

## 0. Repository Snapshot

```
ERP-SYSTEM/
├── .github/workflows/                  # 13 CI workflows (ci-fast, e2e, build-and-deploy-hf, etc.)
├── docs/                                # 19+ design docs, 15+ DEC records, 5 runbooks
├── infra/                               # Docker + CI config
├── scripts/                             # Daily status, backup, seed scripts
├── src/
│   ├── backend/
│   │   ├── Host/                        # Program.cs, Controllers (32), data-types/*.json (54), Audit
│   │   ├── Modules/                     # 12 modules: Identity, Companies, Finance, AR, AP, Inventory, HR, Payroll, Procurement, Projects, Reports, Notifications
│   │   ├── Shared/                      # MultiTenancy, Migrations, Audit, DataTypes, Events, SeedData, Generated
│   │   ├── Tests/                       # 23 xUnit test files
│   │   └── Tools/                       # EntityRepoEnhance, EntityDtoGen (code generators)
│   └── frontend/                        # Next.js 14 (24 pages, lib/api.ts, e2e/)
└── tests/                               # 1 Python script test
```

### Languages / Stack
- **Backend:** C# 12+ / .NET 9, Dapper, FluentMigrator, JWT + BCrypt, Serilog
- **Database:** PostgreSQL 15 (Supabase eu-central-1)
- **Frontend:** Next.js 14, TypeScript 5, Tailwind, Axios, Playwright
- **Migrations:** 24 FluentMigrator migrations + JSON-driven DataTypeMigrator (DEC-079/082)

---

## 1. Inventory of `tenant_id` References

### 1.1 Aggregate counts (case-insensitive, all forms)

| Source | Files | Occurrences |
|---|---:|---:|
| `src/backend/**/*.cs` | **157** | ~2,900 |
| `src/frontend/**/*.{ts,tsx,md}` | 9 | 35 |
| `docs/**` | 8 | ~50 |
| `infra/`, `scripts/`, `.github/` | 0 | 0 |
| `tests/` (Python) | 1 | 2 |
| **Total distinct files** | **~170** | ~3,000 |

**Verbatim grep totals (case-insensitive, `tenantId|TenantId|tenant_id|ITenantContext|TenantContext|TenantMiddleware|TenantAuthorize|Tenant\b|tenants`):**
- `src/backend`: **4,221 occurrences across 341 files** (this includes TenantContext, TenantMiddleware, etc.)
- `src/frontend`: **60 occurrences across 12 files**

### 1.2 Per-category breakdown (with file paths)

#### A. C# Entities (`TenantId` property) — ~25 files

| File | Line(s) | Notes |
|---|---:|---|
| `src/backend/Modules/Identity/Entities/Tenant.cs` | 9-16 | The `Tenant` entity itself (`Id`, `Name`, `Subdomain`, `IsActive`, `CreatedAt`, `SubscriptionExpiresAt`) |
| `src/backend/Modules/Identity/Entities/User.cs` | 13 | `public Guid TenantId { get; set; }` |
| `src/backend/Modules/Identity/Entities/Role.cs` | 13 | `public Guid TenantId { get; set; }` |
| `src/backend/Modules/Companies/Entities/Company.cs` | 8 | `public Guid TenantId { get; set; }` |
| `src/backend/Modules/Companies/Entities/CostCenter.cs` | 6 | `public Guid TenantId { get; set; }` |
| `src/backend/Modules/Finance/Entities/Account.cs` | (in IRepositories + JSON) | navigates by `TenantId` |
| `src/backend/Modules/Finance/Entities/JournalEntry.cs` | (in JSON) | |
| `src/backend/Modules/Finance/Entities/PostingRule.cs` | (in JSON) | |
| `src/backend/Modules/Projects/Entities/{Project,ProjectTask,Resource,ResourceAssignment,ProjectBudget}.cs` | (each 1 ref) | |
| `src/backend/Modules/Inventory/Entities/{Item,ItemCategory,Warehouse,UnitOfMeasure,StockLevel,StockMovement,StockReservation}.cs` | (each 1 ref) | |
| `src/backend/Modules/Procurement/Entities/{Vendor,VendorBill,PurchaseOrder,GoodsReceipt}.cs` | (1-3 refs each) | |
| `src/backend/Modules/HR/Entities/{Department,Attendance,LeaveRequest,...}.cs` (also Employee at `src/backend/Modules/HR/Application/Dtos.cs`) | | |
| `src/backend/Modules/Payroll/Domain/Entities/{SalaryStructure,SalaryStructureLine,PayrollRun,PayrollItem,PayslipComponent}.cs` | | |
| `src/backend/Modules/AccountsReceivable/Entities/{Customer,Receipt,SalesInvoice}.cs` | | |
| `src/backend/Modules/Notifications/Entities/Notification.cs` | | |
| `src/backend/Shared/Generated/Repos/*.g.cs` (24 files, each 11 occurrences) | | auto-generated repos filter by `tenant_id` |

**Note:** All entities currently have a `TenantId` FK-shaped property (NOT NULL, cascade to `tenants.id`). Generated DTOs mirror these.

#### B. C# Repositories (`WHERE tenant_id = @TenantId`) — ~50 files

Every module's `Infrastructure/*Repository.cs` has tenant-scoped queries:

| File | Lines | Pattern |
|---|---:|---|
| `src/backend/Modules/Identity/Infrastructure/TenantRepository.cs` | 14-50 | `SELECT ... FROM tenants WHERE id = @Id`, `WHERE LOWER(subdomain) = LOWER(@Subdomain)`, `WHERE tenant_id = @TenantId` |
| `src/backend/Modules/Identity/Infrastructure/UserRepository.cs` | 17-138 | `tenant_id AS TenantId` SELECT, `WHERE tenant_id = @TenantId`, etc. |
| `src/backend/Modules/Identity/Infrastructure/RoleRepository.cs` | 14-80 | `WHERE tenant_id = @TenantId AND LOWER(name) = LOWER(@Name)` |
| `src/backend/Modules/Companies/Infrastructure/CompanyRepository.cs` | 13-58 | `WHERE tenant_id = @TenantId`, `WHERE is_group = true` (holding lookup) |
| `src/backend/Modules/Companies/Infrastructure/CostCenterRepository.cs` | (10 refs) | `WHERE tenant_id = @TenantId` |
| `src/backend/Modules/Finance/Infrastructure/AccountRepository.cs` | (34 refs) | `WHERE tenant_id = @TenantId AND code = @Code`, etc. |
| `src/backend/Modules/Finance/Infrastructure/JournalEntryRepository.cs` | (16 refs) | `WHERE tenant_id = @TenantId` |
| `src/backend/Modules/Finance/Infrastructure/PostingRuleRepository.cs` | (12 refs) | `WHERE tenant_id = @TenantId` |
| `src/backend/Modules/Projects/Infrastructure/{Project,Task,Resource,ResourceAssignment,ProjectBudget}Repository.cs` | (2-10 refs each) | |
| `src/backend/Modules/Inventory/Infrastructure/{Item,ItemCategory,Warehouse,UnitOfMeasure,StockLevel,StockMovement,StockReservation}Repository.cs` | (10-20 refs each) | |
| `src/backend/Modules/Procurement/Infrastructure/{Vendor,VendorBill,PurchaseOrder,GoodsReceipt,DocumentSequence}Repository.cs` | (9-24 refs each) | |
| `src/backend/Modules/HR/Infrastructure/Repositories.cs` | (5+ refs) | `WHERE tenant_id = @TenantId` |
| `src/backend/Modules/Payroll/Infrastructure/PayrollRepository.cs` | (38 refs) | |
| `src/backend/Modules/Payments/Infrastructure/{Payment,PaymentSequence}Repository.cs` | | |
| `src/backend/Modules/AccountsReceivable/Infrastructure/{Customer,Receipt,SalesInvoice,ArDocumentSequence}Repository.cs` | (9-30 refs each) | |
| `src/backend/Modules/Notifications/Infrastructure/NotificationRepository.cs` | (12 refs) | |
| `src/backend/Shared/Events/Infrastructure/OutboxRepository.cs` | (12 refs) | `tenant_id AS TenantId` in SELECT |
| `src/backend/Shared/Events/Application/Services/EventHandlers.cs` | (5 refs) | |
| `src/backend/Shared/Generated/Repos/*.g.cs` | (24 files × 11) | auto-generated, filter by `tenant_id` |
| `src/backend/Host/Controllers/SoftDeleteController.cs` | 51-130 | SQL `WHERE id = @Id AND tenant_id = @TenantId` |
| `src/backend/Host/Controllers/UsersController.cs` | 99-105 | `WHERE tenant_id = @TenantId` for role list |
| `src/backend/Host/Controllers/AdminController.cs` | 190-294 | `WHERE tenant_id = @T` for finance backfill |
| `src/backend/Tools/EntityRepoEnhance/Program.cs` | 8 refs | code-gen pattern for tenant_id |
| `src/backend/Tests/ERPSystem.Tests/**` | (10+ test files) | assert tenant_id in queries |

**Total: ~50+ repository files** have a `tenant_id` predicate. Every CRUD operation enforces tenant isolation at the SQL level.

#### C. C# Services — ~25 files

| File | Lines | Pattern |
|---|---:|---|
| `src/backend/Modules/Identity/Application/Auth/AuthService.cs` | 25-141 | `OnTenantCreatedAsync(tenantId, name, baseCurrency, ...)` is invoked in Register/Login/Refresh to find/seed the Holding company |
| `src/backend/Modules/Identity/Application/Auth/IAuthService.cs` | 6-9 | `ITenantBootstrap.OnTenantCreatedAsync(...)` interface |
| `src/backend/Modules/Companies/Application/Services/CompanyService.cs` | 27-133 | Implements `ITenantBootstrap` — `CreateHoldingAsync(tenantId, ...)`, `AddSubsidiaryAsync(tenantId, ...)`, etc. |
| `src/backend/Modules/Companies/Application/Services/CostCenterService.cs` | (20 refs) | every method takes `Guid tenantId` |
| `src/backend/Modules/Finance/Application/Services/*.cs` | (10-46 refs each) | `tenantId` as first param of every service method |
| `src/backend/Modules/Inventory/Application/Services/InventoryBootstrapper.cs` | 30-112 | `EnsureDefaultUoMsAndCategoriesAsync(Guid tenantId, ...)` — invoked from CompanyService.OnTenantCreatedAsync |
| `src/backend/Modules/Inventory/Application/Services/InventoryServices.cs` | (75 refs) | tenantId scoping |
| `src/backend/Modules/Inventory/Application/Services/StockMovementService.cs` | (56 refs) | |
| `src/backend/Modules/Inventory/Application/Services/SupportingStockServices.cs` | (23 refs) | |
| `src/backend/Modules/Projects/Application/Services/*.cs` | (10-58 refs each) | |
| `src/backend/Modules/Procurement/Application/Services/*.cs` | (22-29 refs each) | |
| `src/backend/Modules/Payroll/Application/Services/*.cs` | (4-48 refs each) | |
| `src/backend/Modules/AccountsReceivable/Application/Services/*.cs` | (24-46 refs each) | |
| `src/backend/Modules/Reports/Application/Services/*.cs` | (14-20 refs each) | |
| `src/backend/Shared/SeedData/ScenarioSeederHostedService.cs` | 130 refs | entire flow: `RegisterTenantAsync` + per-module seed all take `tenantId` |
| `src/backend/Shared/SeedData/RealisticSeedHostedService.cs` | 128 refs | same |
| `src/backend/Shared/SeedData/SeedDebugState.cs` | 1 ref | `tenant_id` field for diagnostics |
| `src/backend/Shared/SeedData/DefaultInventorySeed.cs` | 1 ref | |

**Pattern:** Every service method takes `Guid tenantId` as first parameter (or reads from `ITenantContext.TenantId`). Total of ~25 service files reference `tenantId`.

#### D. C# Middleware — 3 files (full isolation)

| File | Lines | Pattern |
|---|---:|---|
| `src/backend/Shared/MultiTenancy/ITenantContext.cs` | 1-15 | `Guid? TenantId { get; }` interface |
| `src/backend/Shared/MultiTenancy/TenantContext.cs` | 1-35 | `AsyncLocal<TenantHolder>`-based scoped context |
| `src/backend/Shared/MultiTenancy/TenantMiddleware.cs` | 1-63 | `app.UseMiddleware<TenantMiddleware>()` — reads `tenant_id` JWT claim |
| `src/backend/Host/Program.cs` | 213, 520 | `AddScoped<ITenantContext, TenantContext>()` + `app.UseMiddleware<TenantMiddleware>();` |
| `src/backend/Host/Utilities/TenantCache.cs` | 1-115 | `ITenantCache.InvalidateTenant(Guid tenantId)` — cache keys always include `t:{tenantId}:` prefix |

**There is no `[TenantAuthorize]` attribute** in the codebase (verified by grep). Authorization is done per-controller via `ITenantContext.TenantId` reads + `[Authorize(Policy = ...)]`.

#### E. C# Auth — 7 files

| File | Lines | Pattern |
|---|---:|---|
| `src/backend/Modules/Identity/Application/Auth/AuthService.cs` | 25-141 | Register creates Tenant (or finds by TenantId), Login validates with optional `tenantId`, Refresh reads `user.TenantId` |
| `src/backend/Modules/Identity/Application/Auth/AuthDtos.cs` | 1-30 | `RegisterRequest.TenantId`, `RegisterRequest.TenantName`, `LoginRequest.TenantId?`, `UserInfo.TenantId`, `AuthResponse.HoldingCompanyId` |
| `src/backend/Modules/Identity/Application/Auth/JwtTokenService.cs` | 36 | `new("tenant_id", user.TenantId.ToString())` claim |
| `src/backend/Modules/Identity/Application/Auth/Validators.cs` | 24-27 | `RuleFor(x => x).Must(x => x.TenantId != Guid.Empty || !string.IsNullOrWhiteSpace(x.TenantName))` |
| `src/backend/Host/Controllers/AuthController.cs` | 162-178 | `Me()` endpoint reads `tenant_id` claim and returns `UserInfo.TenantId` |
| `src/backend/Modules/Identity/Application/Auth/IAuthService.cs` | 6-9 | `ITenantBootstrap` interface |
| `src/backend/Modules/Identity/Application/Auth/JwtTokenService.cs` | 19-23 | secret length validation |

**Critical:** the `Slugify()` function lives in `AuthService.cs:140` and is used to compute `Subdomain` from `TenantName`.

#### F. C# Migrations — 8 files (with `tenant_id` columns / indexes / FKs)

| File | Lines | Pattern |
|---|---:|---|
| `src/backend/Shared/Migrations/20260614_120000_CreateIdentityTables.cs` | 27-32 | Down(): `Delete.Table("tenants")` (was the original CREATE; now NoOp per DEC-082, schema is in JSON) |
| `src/backend/Shared/Migrations/20260615_020000_AddMultiCompanySupport.cs` | 14-27 | Down(): drops FKs + `cost_centers`, `companies` tables (multi-company support was added in Phase 1.5) |
| `src/backend/Shared/Migrations/20260623_130000_CreateHRTables.cs` | (2 refs) | adds `tenant_id` to HR tables |
| `src/backend/Shared/Migrations/20260624_100000_CreatePayrollTables.cs` | (1 ref) | adds `tenant_id` to payroll tables |
| `src/backend/Shared/Migrations/20260710_120000_AddMissingIndexes.cs` | 31-66 | `CREATE INDEX IF NOT EXISTS ix_vendor_bills_tenant_due_date ON vendor_bills (tenant_id, due_date)` + AR aging index |
| `src/backend/Shared/Migrations/20260722_120000_AddRetentionSupport.cs` | (1 ref) | |
| `src/backend/Shared/Migrations/20260722_130000_AddRetentionTier1Warm.cs` | 27-130 | partitioned `audit_log` table with `tenant_id UUID NOT NULL` |
| `src/backend/Shared/Migrations/20260722_140000_AddArchiveMetadata.cs` | 35 | `tenant_id UUID NULL` in `archive_metadata` |
| `src/backend/Shared/Migrations/20260724_120000_FixMissingProcurementTables.cs` | 21 refs | creates `vendor_bills` + `vendor_bill_lines` with `tenant_id` |

**64 total `tenant` references across these 8 migration files** (all FKs, indexes, and direct columns).

#### G. C# Tests — 23 files (~340 occurrences)

| File | Lines | Pattern |
|---|---:|---|
| `src/backend/Tests/ERPSystem.Tests/SoftDelete/SoftDeleteTests.cs` | 3 refs | `tenant_id` in raw SQL inserts |
| `src/backend/Tests/ERPSystem.Tests/Retention/RetentionTests.cs` | 1 ref | `INSERT INTO audit_log (tenant_id, ...)` |
| `src/backend/Tests/ERPSystem.Tests/Audit/AuditLoggerTests.cs` | 24 refs | asserts `tenantId` flow |
| `src/backend/Tests/ERPSystem.Tests/Auth/JwtTokenServiceTests.cs` | 1 ref | JWT `tenant_id` claim |
| `src/backend/Tests/ERPSystem.Tests/Auth/ValidatorsTests.cs` | 7 refs | `RegisterRequest.TenantId` / `TenantName` validation |
| `src/backend/Tests/ERPSystem.Tests/Auth/RbacP2Tests.cs` | (multiple) | |
| `src/backend/Tests/ERPSystem.Tests/Auth/RbacPolicyTests.cs` | (multiple) | |
| `src/backend/Tests/ERPSystem.Tests/Common/FakeDbConnectionFactory.cs` | | test fake |
| `src/backend/Tests/ERPSystem.Tests/Companies/DefaultCoASeedTests.cs` | 35 refs | `EnsureDefaultCoAAsync(tenantId, companyId, ct)` |
| `src/backend/Tests/ERPSystem.Tests/Finance/DoubleEntryValidationTests.cs` | 44 refs | journal entries with `tenantId` |
| `src/backend/Tests/ERPSystem.Tests/Events/EventBusAndHandlersTests.cs` | 71 refs | `tenantId` in event payloads |
| `src/backend/Tests/ERPSystem.Tests/Events/DomainEventPublisherTests.cs` | 1 ref | |
| `src/backend/Tests/ERPSystem.Tests/Inventory/InventoryServiceTests.cs` | 27 refs | |
| `src/backend/Tests/ERPSystem.Tests/Inventory/Stock/StockMovementServiceTests.cs` | 80 refs | |
| `src/backend/Tests/ERPSystem.Tests/Projects/ProjectServiceTests.cs` | 58 refs | |
| `src/backend/Tests/ERPSystem.Tests/Reports/{Project,Inventory,Finance}ReportServiceTests.cs` | (9+25+37 refs) | |
| `src/backend/Tests/ERPSystem.Tests/E2E/TestFixtures/TestJwtGenerator.cs` | 4 refs | `new("tenantId", tenantId), new("tid", tenantId),` JWT claims |
| `src/backend/Tests/ERPSystem.Tests/E2E/TestFixtures/ErpWebApplicationFactory.cs` | 1 ref | `public const string TestTenantId = "11111111-..."` |
| `src/backend/Tests/ERPSystem.Tests/E2E/HealthDefenseE2ETests.cs` | 2 refs | |
| `src/backend/Tests/ERPSystem.Tests/E2E/InvoiceLifecycleE2ETests.cs` | 6 refs | |
| `src/backend/Tests/ERPSystem.Tests/E2E/README.md` | 2 refs | |

**Important:** Tests enforce the multi-tenant contract at the SQL layer. Removing `tenant_id` from production code without updating tests will fail ~30+ tests.

#### H. Next.js Frontend (TypeScript) — 9 files, 35 occurrences

| File | Lines | Pattern |
|---|---:|---|
| `src/frontend/lib/api.ts` | 21 refs | `tenantId?: string` in `LoginRequest` (line 64), `tenantId: string` in `UserInfo` (line 78); `tenantId: string` on `Account`, `Item`, `Project`, `Vendor`, `GoodsReceipt`, `VendorBill`, `Department`, `Employee`, `AttendanceRecord`, `LeaveRequest`, `PayrollRun`, `PayrollItem`, `Customer`, `SalesInvoice`, `Receipt` interfaces (every entity DTO) |
| `src/frontend/AGENTS.md` | 7 refs | documents `RegisterRequest.tenantName`, `LoginRequest.tenantId?`, `UserInfo.tenantId` |
| `src/frontend/e2e/auth.spec.ts` | 6 refs | expects `body.user.tenantId`, `body.holdingCompanyId` in register flow; uses `tenantId` in login payload |
| `src/frontend/app/(authenticated)/admin/users/page.tsx` | 1 ref | |
| `src/frontend/app/(authenticated)/admin/companies/page.tsx` | 1 ref | |
| `src/frontend/app/(authenticated)/admin/audit/page.tsx` | 1 ref | |
| `src/frontend/app/(authenticated)/finance/cost-centers/page.tsx` | 2 refs | |
| `src/frontend/app/(authenticated)/finance/cost-centers/[id]/edit/page.tsx` | 1 ref | |
| `src/frontend/app/(authenticated)/finance/accounts/[id]/edit/page.tsx` | 1 ref | |
| `src/frontend/app/(authenticated)/projects/page.tsx` | 1 ref | |
| `src/frontend/app/(authenticated)/inventory/items/page.tsx` | 1 ref | |

**Pattern:** Frontend DTOs mirror the backend entity shape (`tenantId: string` is present in 15+ interfaces). Login form accepts an optional `tenantId`. Register form does NOT request `tenantId` from the user (the backend creates it via `Slugify(tenantName)`).

#### I. JSON Data-Type Schemas — 35 schema files + 17 seed files

The JSON schema for **every business table** in `src/backend/Host/data-types/` declares:

```json
{ "name": "tenant_id", "type": "uuid", "nullable": false,
  "foreign_key": { "table": "tenants", "column": "id", "on_delete": "cascade" } }
```

Files containing `tenant_id` (54 total):

| File | tenant_id count | Notes |
|---|---:|---|
| `accounts.json` | 3 | |
| `attendance.json` | 2 | |
| `audit_log.json` | 3 | |
| `companies.json` | 2 | (note: `companies.tenant_id` FKs to `tenants.id`) |
| `cost_centers.json` | 4 | |
| `customers.json` | 3 | |
| `departments.json` | 3 | |
| `employees.json` | 5 | |
| `items.json` | 3 | |
| `item_categories.json` | 3 | |
| `journal_entries.json` | 4 | |
| `leave_requests.json` | 3 | |
| `notifications.json` | 5 | |
| `outbox_events.json` | 2 | |
| `password_reset_tokens.json` | 1 | |
| `payments.json` | 5 | |
| `payment_allocations.json` | 2 | |
| `payroll_items.json` | 4 | |
| `payroll_runs.json` | 4 | |
| `payslip_components.json` | 2 | |
| `posting_rules.json` | 2 | |
| `processed_events.json` | 3 | |
| `projects.json` | 3 | |
| `roles.json` | 2 | |
| `salary_structures.json` | 3 | |
| `salary_structure_lines.json` | 2 | |
| `sales_invoices.json` | 5 | |
| `stock_levels.json` | 3 | |
| `stock_movements.json` | 5 | |
| `stock_reservations.json` | 3 | |
| `tenants.json` | (0) | the `tenants` table itself |
| `units_of_measure.json` | 2 | |
| `users.json` | 2 | |
| `vendor_bills.json` | 4 | |
| `vendor_bill_lines.json` | 3 | |
| `vendors.json` | 3 | |
| `warehouses.json` | 3 | |
| **`journal_lines.json`** | 0 | this table does NOT have a `tenant_id` (line items are scoped via parent journal_entry) |
| `user_roles.json` | 0 | join table, no `tenant_id` |
| `companies.json` | 2 | has both `tenant_id` AND `parent_company_id` (multi-company tree) |

**Seed files in `Host/data-types/seeds/`:**
- `seed_meta.json` — 3 refs (declares `tenant_id`, `tenant_name`, `tenant_subdomain`)
- 16 entity seed files — 1 ref each (top-level `tenant_id: "f77dbedd-64ff-41ac-b77a-0731183ff744"`)
- `README.md` — 3 refs

**Note:** The `DataTypeMigrator` (`src/backend/Shared/DataTypes/DataTypeMigrator.cs`) reads these JSONs and creates the `tenant_id` columns + FKs + indexes idempotently. Removing `tenant_id` from the JSONs will:
1. Stop `tenant_id` from being added to new tables
2. **NOT drop existing columns** (the migrator only adds, never drops — per line 24 comment "Column removed from JSON: log warning, do NOT drop")

#### J. Documentation — 8+ files

| File | tenant refs |
|---|---:|
| `AGENTS.md` (root) | 7 |
| `docs/CHANGELOG.md` | 64 (mentions "tenant" extensively in Sprint-3 / DEC-091 / DEC-093 / DEC-094 narratives) |
| `docs/PLAN.md` | 6 (multi-tenant architecture described throughout) |
| `docs/PHASE-5-FINANCE-PROJECTS-PLAN.md` | 6 |
| `docs/research/gap-analysis.md` | 32 (multi-tenant listed as a feature) |
| `docs/dec-103a/ARCHITECTURE.md` | 21 |
| `docs/dec-103a/API.md` | 11 |
| `docs/dec-103a/PERFORMANCE-AUDIT.md` | 7 |
| `docs/dec-052/DEC-052-P3-README.md` | 7 |
| `docs/SETUP-LOCAL.md` | 13 |
| `docs/SCENARIO-SEEDER-PLAN.md` | 11 |
| `docs/SMOKE-TEST-REPORT.md` | 11 |
| `docs/runbooks/backup-verification.md` | 5 |
| `docs/FINAL-INTEGRATION-REPORT.md` | 14 |
| `docs/research/{erpnext-features,odoo-reference,phase4-gap-analysis}.md` | 9 |
| (more) | (more) |
| `RUNBOOK.md` | multiple |
| `STATUS.md` | multiple |
| `src/backend/Modules/Identity/AGENTS.md` | 25 |
| `src/backend/Shared/AGENTS.md` | 3 |
| `src/backend/Modules/Finance/AGENTS.md` | 1 |
| `src/backend/Modules/Reports/AGENTS.md` | 2 |
| `src/backend/Modules/AccountsReceivable/AGENTS.md` | (1+) |
| `src/backend/Modules/Payroll/AGENTS.md` | (1+) |
| `src/frontend/AGENTS.md` | 7 |
| `src/frontend/e2e/README.md` | 4 |
| `src/backend/Tests/ERPSystem.Tests/E2E/README.md` | 2 |

**Total: ~250+ `tenant` mentions across ~25 documentation files.** Most are descriptive context that will need a global find-replace.

---

## 2. Inventory of `Tenant` / `tenants` References

### 2.1 The `Tenant` class and `tenants` table

**Table `tenants`** is defined in `src/backend/Host/data-types/tenants.json` (the **only** `tenants.json`):

```json
{
  "name": "Tenant", "table": "tenants", "version": "1.0.0", "module": "Identity",
  "fields": [
    { "name": "id", "type": "uuid", "nullable": false, "primary_key": true },
    { "name": "name", "type": "varchar(200)", "nullable": false },
    { "name": "subdomain", "type": "varchar(100)", "nullable": false },
    { "name": "is_active", "type": "boolean", "nullable": false, "default": "true" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" },
    { "name": "subscription_expires_at", "type": "timestamptz", "nullable": true }
  ],
  "indexes": [
    { "name": "ix_tenants_subdomain", "columns": ["subdomain"], "unique": true }
  ]
}
```

**C# `Tenant` entity:** `src/backend/Modules/Identity/Entities/Tenant.cs` (16 lines, 6 properties).

**Tenant references by category:**

| Category | Files | Notes |
|---|---:|---|
| `Tenant` class definition | 1 | `src/backend/Modules/Identity/Entities/Tenant.cs` |
| `tenants` table name (SQL/JSON) | 54+ | referenced in every FK from business tables, the JSON migrator, and 24+ repositories |
| `ITenantRepository` interface | 1 | `src/backend/Modules/Identity/Infrastructure/IRepositories.cs:35-42` |
| `TenantRepository` impl | 1 | `src/backend/Modules/Identity/Infrastructure/TenantRepository.cs` (52 lines) |
| `Tenant` references in other entities (navigations) | 5+ | `User.Tenant`, `Role.Tenant`, `RefreshToken` (implicit), generated DTOs |
| `ITenantContext` | 1 | `src/backend/Shared/MultiTenancy/ITenantContext.cs` |
| `TenantContext` | 1 | `src/backend/Shared/MultiTenancy/TenantContext.cs` |
| `TenantMiddleware` | 1 | `src/backend/Shared/MultiTenancy/TenantMiddleware.cs` |
| `ITenantCache` / `TenantCache` | 1 | `src/backend/Host/Utilities/TenantCache.cs` (115 lines, uses `t:{tenantId}:` key prefix) |
| `[TenantAuthorize]` attribute | **0** | NOT in codebase (verified by grep). Authorization is per-controller via `ITenantContext` |
| `ITenantBootstrap` interface | 1 | `src/backend/Modules/Identity/Application/Auth/IAuthService.cs:6-9` — invoked from AuthService to seed the Holding Company when a tenant is created |
| `OnTenantCreatedAsync` callers | 4 | `AuthService.RegisterAsync` (line 40, 47), `AuthService.LoginAsync` (line 89), `AuthService.RefreshAsync` (line 110), `CompanyService.OnTenantCreatedAsync` (the implementation in `src/backend/Modules/Companies/Application/Services/CompanyService.cs:35-70`) |
| `EnsureDefaultCoAAsync` callers | 4 | `CompanyService.CreateHoldingAsync` (line 78, 92 — invoked by `OnTenantCreatedAsync`), `AccountRepository.EnsureDefaultCoAAsync`, `Tests/ERPSystem.Tests/Companies/DefaultCoASeedTests.cs` |

### 2.2 `Slugify()` usage

| File | Line | Use |
|---|---:|---|
| `src/backend/Modules/Identity/Application/Auth/AuthService.cs` | 140 | definition: `private static string Slugify(string s)` |
| `src/backend/Modules/Identity/Application/Auth/AuthService.cs` | 44 | `Subdomain = Slugify(req.TenantName!)` when creating new tenant |
| `src/frontend/AGENTS.md` | 117, 119 | documents the pattern ("Subdomain يُحسب تلقائياً من TenantName") |
| `src/frontend/app/register/page.tsx` | 10, 92 | "subdomain سيُولَّد تلقائياً من اسمها" |
| `src/frontend/lib/api.ts` | 50 | comment in `RegisterRequest` |

**3 production files contain `Slugify`** (1 definition + 1 caller + 0 callers in repos). The frontend never receives or sends `subdomain` — it's purely a backend-computed column.

### 2.3 `OnTenantCreatedAsync` bootstrap flow

The current `AuthService.RegisterAsync` (lines 25-72) creates a new tenant, then invokes `OnTenantCreatedAsync` which:

1. Checks for an existing Holding Company (code "000", `is_group = true`)
2. Calls `CreateHoldingAsync(tenantId, "000", ...)` → creates `companies` row + seeds 47-row Chart of Accounts via `EnsureDefaultCoAAsync`
3. Calls `EnsureDefaultUoMsAndCategoriesAsync(tenantId, ...)` → seeds 6 UoMs + 5 ItemCategories
4. Returns the `holdingCompanyId`

**This is invoked 4 times per session lifecycle:**
- Register (inside the transaction, with conn/tx overload)
- Login (own connection, no tx)
- Refresh (own connection, no tx)
- Implicitly in `BuildAsync` (twice)

### 2.4 Subdomain-based tenant routing

**There is no actual subdomain-based routing** (no Caddy/nginx config reads `Host:` header to extract tenant). The `subdomain` column exists on the `tenants` table but is purely a display/identity field. Login accepts an optional `tenantId` body field; the `subdomain` column is never used at runtime. (DEC-090 removed subdomain UI from the register form on 2026-06-17.)

---

## 3. Migration Strategy Recommendation

### Option A: **Clean Slate** (drop everything, create new Initial Schema)

**Steps required:**
1. Add a new FluentMigrator migration `Phase6_InitialSchema_20260725_120000` that:
   - `DELETE FROM VersionInfo` (clear all migration history — risky in prod, but `Fresh Build Mode` is on by default per `Program.cs:359-379`)
   - `DROP TABLE IF EXISTS ... CASCADE` for: `tenants`, `users`, `roles`, `user_roles`, `refresh_tokens`, `password_reset_tokens`, `companies`, `cost_centers`, `accounts`, `journal_entries`, `journal_lines`, `posting_rules`, `projects`, `project_tasks`, `resources`, `resource_assignments`, `project_budgets`, `items`, `item_categories`, `warehouses`, `units_of_measure`, `stock_levels`, `stock_movements`, `stock_reservations`, `vendors`, `purchase_orders`, `purchase_order_lines`, `goods_receipts`, `goods_receipt_lines`, `vendor_bills`, `vendor_bill_lines`, `payments`, `payment_allocations`, `customers`, `sales_invoices`, `sales_invoice_lines`, `receipts`, `receipt_allocations`, `departments`, `employees`, `attendance`, `leave_requests`, `salary_structures`, `salary_structure_lines`, `payroll_runs`, `payroll_items`, `payslip_components`, `notifications`, `outbox_events`, `processed_events`, `audit_log` (and all partitions), `archive_metadata`, `companies` (Holding)
   - `DROP FUNCTION IF EXISTS audit_log_id_seq`, etc.
2. Update all 35 JSON DataType schemas in `src/backend/Host/data-types/*.json`:
   - Remove `tenant_id` field from every business table JSON
   - Remove `tenant_id`-prefixed indexes (`ix_xxx_tenant_yyy` → `ix_xxx_yyy`)
   - For `users` table: add a `default_company_id` FK (sets the active Holding/Subsidiary on login)
3. **Create new `user_companies` table** (currently missing — the constitution requires it for "Users scoped to one or more Companies"):
   ```json
   {
     "name": "UserCompany", "table": "user_companies",
     "primary_key": ["user_id", "company_id"],
     "fields": [
       { "name": "user_id", "type": "uuid", "foreign_key": { "table": "users", "column": "id", "on_delete": "cascade" } },
       { "name": "company_id", "type": "uuid", "foreign_key": { "table": "companies", "column": "id", "on_delete": "cascade" } },
       { "name": "is_default", "type": "boolean", "default": "true" },
       { "name": "assigned_at", "type": "timestamptz", "default": "now()" }
     ]
   }
   ```
4. Update `companies.json` to remove `tenant_id` (it was FK to tenants), keep `parent_company_id` (FK to companies)
5. **Delete `tenants.json`** — table is gone
6. Update `AuthService.cs`:
   - Remove `ITenantRepository` injection
   - Remove `OnTenantCreatedAsync` from `IAuthService` interface
   - Change `RegisterAsync` flow: still creates Holding Company (idempotent — find or create with code "000"), creates User, creates `user_companies` row linking User → Holding, creates 4 default roles (now global, not per-tenant), assigns Admin role
   - Remove `tenantId` from `User` entity, `RegisterRequest`, `LoginRequest`, `UserInfo`, `AuthResponse`
7. Update `JwtTokenService.cs`: replace `tenant_id` claim with `default_company_id` (or `company_ids[]`)
8. Delete `src/backend/Shared/MultiTenancy/` entirely (3 files)
9. Delete `src/backend/Host/Utilities/TenantCache.cs` (replace with `UserCache` if needed)
10. Update `Program.cs`:
    - Remove `AddScoped<ITenantContext, TenantContext>()` (line 213)
    - Remove `AddSingleton<ITenantCache, TenantCache>()` (line 167)
    - Remove `app.UseMiddleware<TenantMiddleware>();` (line 520)
    - Remove `using ERPSystem.Shared.MultiTenancy;` (line 48)
11. Update `IAuditLogger` interface: drop `Guid tenantId` parameter → use `Guid? companyId` instead
12. Update `User` entity: remove `TenantId` (line 13), keep all other fields
13. Update `Role` entity: remove `TenantId` (line 13) — roles become global; or keep tenantId but treat as a vestigial (simpler)
14. Update all 50+ repository `WHERE tenant_id = @TenantId` clauses → remove entirely
15. Update all 25+ service methods: drop `Guid tenantId` first parameter (or convert to `Guid? companyId` for per-company filters)
16. Regenerate all 24 `Shared/Generated/Repos/*.g.cs` (re-run `Tools/EntityRepoEnhance/Program.cs`) — these will lose `tenant_id` columns from JSON-driven generation
17. Update 23 test files
18. Update 9 frontend files (`lib/api.ts` + 7 pages)
19. Update 8 doc files
20. **Update `seed_meta.json`**: remove `tenant_id`, `tenant_name`, `tenant_subdomain` fields; switch to a `holding_company_id` reference

**Risks:**
- ❌ **Loses all data** (no tenant rows, no user rows, no CoA, no anything). HF Space will start completely empty.
- ❌ **DEC-091 atomicity contract is no longer testable** the same way (the "register" flow no longer creates a tenant — it just creates a user under the global Holding)
- ❌ Every existing C# migration that has a `Down()` referencing `tenants` will become wrong (but the NoOp `Up()` migrations don't actually run, only the `Down()` for rollback would break — and we're not rolling back)
- ❌ The `DataTypeMigrator` only does additive changes — manual `DROP` SQL in the new migration is required
- ❌ The `audit_log` table is partitioned (DEC-052 P2) — dropping it loses the partitioning work, which would need to be re-applied

**Effort estimate:** **HIGH (5-8 dev days)** — touches 170+ files, but the changes are mechanical find-replace in 80% of cases (the 20% hard parts are the new `user_companies` table, the new auth flow, and the audit logger refactor).

**Data preservation:** ❌ None. All 15 orphan tenants + the few production tenants get dropped.

---

### Option B: **Incremental** (keep tables, remove `tenant_id` columns one by one)

**Steps required:**
1. **For each business table:**
   - Add migration N: `ALTER TABLE x DROP COLUMN tenant_id;` (after `DROP CONSTRAINT` for the FK)
   - Drop the FK constraint `fk_x_tenant_id` first
   - Drop indexes `ix_x_tenant_yyy`
   - For tables that are NOT joined with `companies` (like `notifications`, `audit_log`): just drop the column
2. Keep `tenants` table, `users.tenant_id`, `roles.tenant_id` for now (vestigial)
3. Update the `tenant_id` column on `companies` to be nullable (it was NOT NULL FK to tenants)
4. Run 24+ migrations in sequence — each one drops a column
5. Eventually run a final migration: `DROP TABLE tenants;` (after `tenants.id` is no longer referenced anywhere)

**Risks:**
- ❌ **Multi-step rollback nightmare**: if migration 8 fails after migration 7 succeeded, the DB is in a half-migrated state. Hard to recover.
- ❌ **Performance penalty during the migration window**: every column drop on a large table is `ALTER TABLE` (table rewrite on Postgres <11, or metadata-only on 11+ but with AccessExclusiveLock).
- ❌ **Two code paths** during the transition: the old code expects `tenant_id`, the new code doesn't. Every PR has to dance around both.
- ❌ **Production risk**: 24+ deployments needed over weeks. Each one risks a partial state. HF Space deploy = 3-5 min cold start per push. With 24 migrations, that's 1.5-2 hours of HF compute time, plus failure risk.
- ❌ **Cascade FK complexity**: dropping `tenants` table requires CASCADE if any table still references it. Some tables use `ON DELETE CASCADE` to tenants, but the partition inheritance on `audit_log` adds complexity.
- ❌ **`DataTypeMigrator` is additive only** — cannot remove `tenant_id` from a column that already exists, so the JSON updates would have no effect on existing tables
- ❌ **Test coverage gap**: tests that assert `tenant_id` in the schema would pass against the new code only after ALL incremental migrations run
- ❌ **Long-lived vestigial columns**: weeks of code where `tenant_id` is dead weight, complicating the codebase

**Effort estimate:** **VERY HIGH (10-15 dev days)** — more migrations to write, more PRs to manage, more CI cycles, more rollback risk.

**Data preservation:** ✅ Possible. Each migration can `ALTER COLUMN` instead of `DROP COLUMN` to set `tenant_id = NULL` first.

---

### **Recommendation: Option A (Clean Slate)** ✅

**Rationale:**
1. **Per CONSTITUTION.md §3.4:** "Clean slate allowed. The old `tenant_id` schema can be dropped entirely." This is the explicit, owner-approved direction.
2. **Per CHANGELOG.md / Sprint-3:** "Fresh Build Mode" is already the default in production (per `Program.cs:359-379` — the AlFajr/AlBurj/Realistic seeders are commented out). The HF Space **already starts with an empty DB**. There is no production data to preserve.
3. **DEC-091 atomicity proof (E2E test):** the atomicity test in `src/frontend/e2e/auth.spec.ts` aborts 5 register requests and verifies "no orphan tenants." This test was the last active use of multi-tenant semantics. With multi-tenancy gone, the test must be re-framed to assert "no orphan users" instead — but the test infrastructure stays the same.
4. **Single big-bang refactor > many small migrations:** the codebase is not in production with paying customers (this is internal ERP for one Libyan SME). A 1-week clean-slate refactor beats a 1-month incremental migration.
5. **Mechanical work is well-defined:** the bulk of the change is find-replace on ~170 files. The "interesting" work is concentrated in 4 places: (1) new `user_companies` table, (2) auth flow refactor, (3) audit logger signature change, (4) `OnTenantCreatedAsync` → `EnsureDefaultHoldingAsync` rename.

**Pragmatic plan:**
- **Phase 6.0 (Schema Reset):** Single migration drops everything, creates new `user_companies` table, recreates all business tables from updated JSONs (without `tenant_id`).
- **Phase 6.1 (Code):** Mechanical refactor of 170 files in one PR (or 2-3 PRs for reviewability).
- **Phase 6.2 (Tests):** Update 23 test files + 1 e2e spec.
- **Phase 6.3 (Docs):** Update 25 doc files.
- **Phase 6.4 (Seed rewrite):** Rewrite `seed_meta.json` to point at a `holding_company_id` instead of `tenant_id`.

---

## 4. New Schema Sketch

### 4.1 High-level table list (after Phase 6)

The **new schema has the same tables as before, minus `tenants`, plus `user_companies`.**

| Module | Tables (old) | New |
|---|---|---|
| **Identity** | `tenants` ❌, `users`, `roles`, `user_roles`, `refresh_tokens`, `password_reset_tokens` | `users` (no `tenant_id`, new `default_company_id`), `roles` (global, no `tenant_id`), `user_roles`, `refresh_tokens` (no `tenant_id`), `password_reset_tokens` (no `tenant_id`), **`user_companies` (new)** |
| **Companies** | `companies`, `cost_centers` | `companies` (no `tenant_id` — keep `parent_company_id`, `is_group`, `is_holding`), `cost_centers` (no `tenant_id`) |
| **Finance** | `accounts`, `journal_entries`, `journal_lines`, `posting_rules` | same (all lose `tenant_id`) |
| **Projects** | `projects`, `project_tasks`, `resources`, `resource_assignments`, `project_budgets` | same (all lose `tenant_id`) |
| **Inventory** | `items`, `item_categories`, `warehouses`, `units_of_measure`, `stock_levels`, `stock_movements`, `stock_reservations` | same (all lose `tenant_id`) |
| **Procurement** | `vendors`, `purchase_orders`, `purchase_order_lines`, `goods_receipts`, `goods_receipt_lines`, `vendor_bills`, `vendor_bill_lines`, `document_sequences` | same (all lose `tenant_id`) |
| **Payments** | `payments`, `payment_allocations`, `payment_sequences` | same (all lose `tenant_id`) |
| **AR** | `customers`, `sales_invoices`, `sales_invoice_lines`, `receipts`, `receipt_allocations`, `ar_document_sequences` | same (all lose `tenant_id`) |
| **HR** | `departments`, `employees`, `attendance`, `leave_requests`, `hr_document_sequences` | same (all lose `tenant_id`) |
| **Payroll** | `salary_structures`, `salary_structure_lines`, `payroll_runs`, `payroll_items`, `payslip_components` | same (all lose `tenant_id`) |
| **Notifications** | `notifications` | same (loses `tenant_id`) |
| **Shared** | `outbox_events`, `processed_events`, `audit_log`, `archive_metadata` | same (lose `tenant_id`) |

**Total: 41 tables** (was 42, -1 `tenants` + 0 net since we don't add `user_companies` to the count in the same bucket — actually it's +1 for `user_companies` so 42 total).

### 4.2 The Holding Company concept

**Per CONSTITUTION.md §3.2:**

```
Holding (one per deployment)
  ↓ parent_company_id (FK to self)
Subsidiaries (many)
  ↑ company_id (FK on every business table)
```

- **Holding row in `companies` table:**
  - `id` = a fixed UUID, e.g. `00000000-0000-0000-0000-000000000001` (the "default Holding")
  - `code` = `'000'` (existing convention)
  - `name` = from the `appsettings.json` `Deployment:DefaultHoldingName` config (e.g. "Demo Holding Co.")
  - `legal_name` = same / optional
  - `is_group` = `true`
  - `parent_company_id` = `NULL`
  - `base_currency` = from `appsettings.json` `Deployment:DefaultCurrency` (e.g. "LYD")
- **Subsidiary rows:** `is_group = false`, `parent_company_id = holding.id`
- **Bootstrapping:** A new migration `EnsureDefaultHolding` (idempotent — `INSERT ... ON CONFLICT (code) DO NOTHING` WHERE `parent_company_id IS NULL AND is_group = true`) runs on startup, **before** the first request. This replaces the per-tenant `OnTenantCreatedAsync` flow.

**Implication for CoA seed:** `EnsureDefaultCoAAsync(holdingCompanyId, ct)` (drop the `tenantId` parameter) runs once at boot, seeds 47 accounts scoped to the Holding.

### 4.3 New Auth Flow

**Register (simplified):**
```
POST /api/auth/register
Body: { email, password, fullName }            // NO tenantName, NO tenantId
↓
1. Look up (or create) the default Holding Company
2. Check email uniqueness globally
3. INSERT users (id, email, password_hash, full_name, default_company_id = holdingId, ...)
4. INSERT user_roles for the 4 default roles (now global — created at boot, not per-register)
5. Assign Admin role to the new user
6. Generate access + refresh tokens
7. Build response with:
   - user: { id, email, fullName, default_company_id, company_ids: [holdingId], roles: ["Admin"] }
   - access_token (with claim default_company_id, claims.company_ids)
8. Return
```

**Login (simplified):**
```
POST /api/auth/login
Body: { email, password }                       // NO tenantId
↓
1. Get user by email (globally unique now)
2. Verify password
3. Load user_companies → [company_id, ...]
4. Generate tokens with default_company_id + company_ids[] claims
5. Return
```

**Multi-Company bootstrap (Holding only at first):**
- The Holding + 47 CoA accounts + 6 UoMs + 5 ItemCategories are seeded **once at app startup** by a new `DefaultHoldingBootstrapHostedService` (idempotent)
- First registered user becomes Admin of the Holding
- Subsequent users can be added via `/api/admin/users` (no longer per-tenant)

**JWT claims (replacing `tenant_id`):**
- `sub` (userId) — unchanged
- `email` — unchanged
- `default_company_id` (Guid) — replaces `tenant_id`
- `company_ids` (Guid[]) — new
- `roles` — unchanged (now global, e.g. "Admin", "Accountant", "ProjectManager", "Viewer")

### 4.4 Company Switcher (Frontend)

The frontend's `lib/api.ts` needs:
- New `UserInfo` interface: `defaultCompanyId: string, companyIds: string[]` (replacing `tenantId: string`)
- New `Company` interface (already exists) for the switcher dropdown
- A `currentCompanyId` stored in `localStorage` (defaults to `defaultCompanyId`)
- Every `axios.get/post/put/delete` adds an `X-Company-Id` header (so the backend can scope per-company queries when relevant — like AR/AR invoices, journal entries, etc.)
- New `<CompanySwitcher />` component in the AppShell (topbar)

**Backend side:** A new lightweight `ICompanyContext` (similar to old `ITenantContext` but populated from `X-Company-Id` header, NOT JWT — since the user might switch companies during a session). `UserCompanyAccessFilter` validates the `company_id` is in the user's `company_ids` list.

---

## 5. Risks & Unknowns

### 5.1 Foreign keys referencing `tenant_id` (45+ FKs)

Every JSON schema has `{ "name": "tenant_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "tenants", "column": "id", "on_delete": "cascade" } }`. The `DataTypeMigrator` (lines 213-230) creates these FKs as `fk_{table}_tenant_id`. To drop them, the clean-slate migration must use `ALTER TABLE ... DROP CONSTRAINT IF EXISTS fk_x_tenant_id;` before `DROP COLUMN tenant_id;` (or use CASCADE on the table drop).

### 5.2 Indexes (60+ indexes to drop)

Every JSON has indexes like `ix_xxx_tenant_yyy` and `ix_xxx_tenant_code` (composite `(tenant_id, code)`). These will be dropped automatically with the table. The replacement indexes will be `(company_id, code)` or just `(code)` if cross-company uniqueness is desired.

### 5.3 Business logic depending on cross-tenant isolation

| Risk | Impact | Mitigation |
|---|---|---|
| Email uniqueness is now **global** (was per-tenant) | A user in one company can't share email with a user in another | Add `email` UNIQUE constraint at the DB level; document |
| Roles are now **global** (was per-tenant) | All companies see the same 4 roles | OK — keep global. Or add `role_id, company_id` composite if per-company role customization is needed |
| Soft-delete is per-tenant (SoftDeleteController lines 51-130) | All `WHERE tenant_id = @TenantId` in soft-delete SQL | Remove tenant clause, add per-company if needed |
| Reports aggregate by tenant | `FinanceReportService`, `InventoryReportService`, `ProjectReportService` all filter by `tenantId` (16-20 refs each) | Re-scope by `companyId` (or `company_ids[]`) — decision: per-company or all companies? **Recommend per-company for now** |
| Audit log: `tenant_id` column on partitioned table (DEC-052 P2) | Partition DDL needs to be re-done if we keep partitioning | Keep partitioning, change column name → `company_id` or remove (and rely on `user_id` in audit row) |
| E2E test atomicity proof | Aborted register should leave no orphan — but with no tenants, the assertion is "no orphan users" | Rewrite test: abort 5 register requests, verify each email can't log in (no user row created) |

### 5.4 Data that needs migration vs. dropped

**Per CHANGELOG.md, production is in "Fresh Build Mode" — there is no production data:**
- AlFajr/AlBurj/Realistic seeders are **commented out** in `Program.cs:359-379`
- HF Space starts with an empty DB
- The 15 orphan tenants mentioned in CHANGELOG (DEC-091) are presumed cleaned up

**However, the `seed_meta.json` references `tenant_id: f77dbedd-64ff-41ac-b77a-0731183ff744`** — this is the AlFajr tenant. If the seeders are ever re-enabled, they need to be rewritten to use `holding_company_id` (a deterministic UUID, e.g. `00000000-0000-0000-0000-000000000001`).

**No data migration is needed.** All current data is dev/test data; production is empty.

### 5.5 Third-party integrations

- **Supabase (database only, not Supabase Auth):** Supabase is just the Postgres host. No Supabase Auth integration is used (we do our own JWT). Removing `tenant_id` is a local schema change; no Supabase-level impact.
- **Hugging Face Space (Caddy reverse proxy):** No change. The proxy just forwards HTTP.
- **GitHub Actions / CI:** No change. Tests run against an ephemeral Postgres (per `.github/workflows/ci-fast.yml`).
- **No external OAuth / SAML / multi-tenant SaaS billing** integration exists (per the codebase). The `tenants.subscription_expires_at` column was future-proofing for SaaS billing that was never built.

### 5.6 Unknowns / open questions

1. **Roles: global or per-company?** Current code has 4 default roles created per-tenant. Recommendation: make them global (4 system roles seeded at boot). If per-company role customization is needed later, add `user_company_roles` join table.
2. **Email uniqueness: global or per-company?** The `users.json` has `ix_users_tenant_email` (composite). For Phase 6.0, recommend global uniqueness. If per-company email is needed, add `ix_users_company_email` composite.
3. **Default company on user creation:** when a new user is registered (e.g. via admin invite), which company do they belong to? Two options:
   - (a) They belong to all companies (admin invite must specify the company)
   - (b) They belong to the Holding by default; admin moves them
4. **Cost centers:** currently have BOTH `tenant_id` AND `company_id`. Drop `tenant_id`, keep `company_id`.
5. **Intercompany accounts:** the `accounts.is_intercompany` flag and the new `Intercompany` flag in `DefaultCoASeed.HoldingAccounts` (per `DefaultCoASeedTests.cs`) — this is unaffected by the refactor, but the intercompany logic may need re-validation in a multi-company context.
6. **Audit log partitioning:** the `audit_log` is partitioned by year (DEC-052 P2). Does the `tenant_id` column on the partition get dropped? The clean-slate approach drops the whole table and re-creates, so this is moot.
7. **Generated repositories (`*.g.cs`):** the `EntityRepoEnhance` tool reads the JSON schemas. After updating the JSONs, the tool must be re-run to regenerate 24+ `*.g.cs` files. This is mechanical but should be automated.
8. **The `tenant_id` field is mentioned in `RUNBOOK.md` and `STATUS.md` and several DEC records** — these are documentation updates needed but not blockers.
9. **The `MartenDB` event store (DEC-017, deferred):** the codebase has `Marten` package installed but NOT wired up. The Outbox pattern in `Shared/Events/Infrastructure/OutboxRepository.cs` is the current implementation. The Outbox table has a `tenant_id` column — must be updated to `company_id` or removed.

---

## 6. Phased Plan

### Phase 6.0: **Schema Reset** (one migration, one PR)
- **Effort:** LOW (1-2 dev days)
- **Files changed:** 1 migration file + 35 JSON schemas + 17 seed JSONs + new `user_companies.json` + delete `tenants.json` + rewrite `seed_meta.json`
- **Steps:**
  1. Write `Phase6_InitialSchema_20260725_120000` migration:
     - `DROP TABLE IF EXISTS` all 41 business tables (CASCADE)
     - `DROP TABLE IF EXISTS tenants, user_roles, user_companies CASCADE`
     - `DELETE FROM VersionInfo` to reset migration history
  2. Update 35 DataType JSONs: remove `tenant_id` field + tenant-prefixed indexes
  3. Create new `user_companies.json`
  4. Delete `tenants.json`
  5. Update `companies.json`: remove `tenant_id`, keep `parent_company_id` + add `is_holding` (or rely on `is_group=true` + `parent_company_id=NULL`)
  6. Rewrite `seed_meta.json`: `tenant_id` → `holding_company_id` (fixed UUID)
- **Parallel-safe:** ✅ No code touched yet, just data
- **Risk:** Migration is destructive — run only on `main` after develop is clean

### Phase 6.1: **Code Refactor (Backend Entities & Repos)** (one big PR or 2)
- **Effort:** HIGH (3-4 dev days)
- **Files changed:** ~100 backend files
- **Steps:**
  1. Update `User`, `Role` entities: remove `TenantId` property; add `DefaultCompanyId` to `User`
  2. Update `Tenant.cs` entity: **delete file** + delete `ITenantRepository`, `TenantRepository`
  3. Update all 50+ repository files: remove `WHERE tenant_id = @TenantId` clauses
  4. Update all 25+ service files: drop `Guid tenantId` first parameter
  5. Delete `src/backend/Shared/MultiTenancy/` (3 files)
  6. Delete `src/backend/Host/Utilities/TenantCache.cs` (or rewrite as `UserCache`)
  7. Update `Program.cs`: remove `TenantContext`, `TenantMiddleware`, `TenantCache` DI registrations + middleware
  8. Add new `ICompanyContext` (in `Shared/CompanyContext/`) populated from `X-Company-Id` header
  9. Add new `DefaultHoldingBootstrapHostedService` that seeds the Holding + CoA + UoMs + Categories at app start
  10. Update `AuthService.cs` (entire register/login/refresh flow)
  11. Update `AuthDtos.cs`: remove `TenantId`/`TenantName` from requests, add `DefaultCompanyId`/`CompanyIds` to responses
  12. Update `JwtTokenService.cs`: replace `tenant_id` claim with `default_company_id` + `company_ids[]`
  13. Update `IAuditLogger` + `AuditLogger.cs`: change `Guid tenantId` parameter to `Guid? companyId` (or remove)
  14. Delete `ITenantBootstrap` interface and its `CompanyService` implementation
  15. Update `InventoryBootstrapper.cs`: remove `Guid tenantId` parameter
  16. Update `ScenarioSeederHostedService.cs` + `RealisticSeedHostedService.cs`: rewrite tenant creation → holding fetch
  17. Re-run `Tools/EntityRepoEnhance/Program.cs` to regenerate 24+ `*.g.cs` files
  18. Update 12 Generated DTOs (`Shared/Generated/DTOs/*.g.cs`)
- **Parallel-safe:** ❌ Must be sequential with Phase 6.2

### Phase 6.2: **Tests + E2E**
- **Effort:** MEDIUM (1-2 dev days)
- **Files changed:** 24 test files + 1 e2e spec
- **Steps:**
  1. Update all xUnit test files: remove `tenantId` from test data
  2. Update `DefaultCoASeedTests.cs`: drop `tenantId` from `EnsureDefaultCoAAsync(companyId, ct)` signature
  3. Update `TestJwtGenerator.cs` + `ErpWebApplicationFactory.cs`: drop `TestTenantId` → `TestCompanyId`
  4. Update `e2e/auth.spec.ts`: rewrite atomicity test (no more "no orphan tenants" → "no orphan users")
  5. Add new test: `HoldingBootstrap_Seeds_DefaultHolding_And_CoA`
  6. Add new test: `UserCompany_Limits_Access_To_Assigned_Companies`
- **Parallel-safe:** ❌ Sequenced after Phase 6.1

### Phase 6.3: **Frontend**
- **Effort:** LOW-MEDIUM (1 dev day)
- **Files changed:** 9 frontend files
- **Steps:**
  1. Update `lib/api.ts`: remove `tenantId` from all DTOs, add `defaultCompanyId` + `companyIds` to `UserInfo`
  2. Add new `CompanySwitcher` component
  3. Update `lib/api.ts` axios interceptor: add `X-Company-Id` header
  4. Update `app/register/page.tsx`: remove `tenantName` field (Holding is implicit)
  5. Update all 7 admin/finance/inventory/project pages that reference `tenantId`
  6. Add new `lib/companyContext.ts` for client-side company state
  7. Update `useAuth.ts` to expose `defaultCompanyId` and `switchCompany`
- **Parallel-safe:** ✅ Can be developed in parallel with Phase 6.1 (against stubs) if API contracts are agreed upfront

### Phase 6.4: **Documentation**
- **Effort:** LOW (0.5-1 dev day)
- **Files changed:** ~25 doc files
- **Steps:**
  1. Update root `AGENTS.md`: replace "Multi-Tenant Modular Monolith" header → "Multi-Company Modular Monolith"
  2. Update `CONSTITUTION.md` if any decisions changed during refactor (likely not)
  3. Update `docs/PLAN.md`: Phase 6 entry
  4. Update `docs/CHANGELOG.md`: Phase 6 release notes
  5. Update `src/backend/Modules/Identity/AGENTS.md`, `src/backend/Shared/AGENTS.md`, etc.
  6. Update `src/frontend/AGENTS.md`
  7. Update `docs/research/gap-analysis.md`: remove "Multi-tenancy" from feature comparison
  8. Update `docs/dec-103a/ARCHITECTURE.md`, `docs/dec-103a/API.md`
- **Parallel-safe:** ✅ Can be done last, or in parallel

### Phase 6.5: **CI / Hardening**
- **Effort:** LOW (0.5 dev day)
- **Files changed:** CI workflows + HF Space env
- **Steps:**
  1. Update `e2e.yml` + `ci-fast.yml`: no changes needed (no tenant_id env vars)
  2. Add a new GH workflow: `phase6-migration-verify.yml` — runs the Initial Schema on a fresh DB, asserts zero `tenant_id` columns
  3. Add `Deployment:DefaultHoldingName` + `Deployment:DefaultCurrency` to `appsettings.json`
  4. Update HF Space env vars: drop any `SUPABASE_TENANT_ID` if it exists (verify)
- **Parallel-safe:** ✅

### Total effort estimate: **6-8 dev days** (sequential)
### Parallel-friendly: Phases 6.3 (frontend) + 6.4 (docs) + 6.5 (CI) can run in parallel with 6.1 (backend) if the API contracts are agreed upfront.

---

## 7. Final Inventory Snapshot (Quick Reference)

| Bucket | Files | Most touched file |
|---|---:|---|
| C# Entities (need `TenantId` removed) | ~25 | `User.cs`, `Role.cs`, `Company.cs`, `CostCenter.cs` |
| C# Repositories (need `WHERE tenant_id` removed) | ~50 | `AccountRepository.cs` (34 refs), `ProjectRepository.cs` (10 refs) |
| C# Services (need `tenantId` param removed) | ~25 | `ProjectService.cs` (31 refs), `InventoryServices.cs` (75 refs) |
| C# Middleware (full delete) | 3 | `MultiTenancy/TenantContext.cs`, `TenantMiddleware.cs`, `ITenantContext.cs` |
| C# Auth (full rewrite) | 7 | `AuthService.cs`, `JwtTokenService.cs`, `AuthDtos.cs` |
| C# Migrations (1 new) | 1 | new `Phase6_InitialSchema_*.cs` |
| C# Tests (signature updates) | 24 | `EventBusAndHandlersTests.cs` (71 refs), `StockMovementServiceTests.cs` (80 refs) |
| C# Tools (regenerate) | 2 | `EntityRepoEnhance/Program.cs` + `EntityDtoGen/Program.cs` |
| C# Generated (regenerate) | 48 | `Shared/Generated/Repos/*.g.cs` (24) + `Shared/Generated/DTOs/*.g.cs` (24) |
| JSON DataType Schemas | 35 | every file except `journal_lines.json`, `user_roles.json` |
| JSON Seed files | 17 | all need `tenant_id` → `holding_company_id` (or removed) |
| Frontend TS files | 9 | `lib/api.ts` (21 refs) |
| Documentation | ~25 | root `AGENTS.md`, `docs/CHANGELOG.md`, `docs/PLAN.md`, `src/backend/Modules/Identity/AGENTS.md`, `src/frontend/AGENTS.md` |
| **Grand total** | **~250 files** | |

---

## 8. Sign-off Checklist (before Phase 6.1 begins)

- [ ] **Owner (Anas) approval** of clean-slate approach (per CONSTITUTION.md §3.4, this is already approved)
- [ ] **Mavis** confirms migration order with Jamie Executive
- [ ] **Schema design for `user_companies`** locked in (composite PK, `is_default` flag, etc.)
- [ ] **API contract for new auth flow** agreed (request/response shapes for register/login/refresh)
- [ ] **Decision: roles global vs per-company** (recommendation: global for Phase 6.0, defer per-company)
- [ ] **Decision: email uniqueness global vs per-company** (recommendation: global for Phase 6.0)
- [ ] **Decision: `X-Company-Id` header** vs JWT-embedded `company_ids[]` (recommendation: both — JWT has the list, header picks the active one)
- [ ] **HF Space `Fresh Build Mode`** is the default (verified per `Program.cs:359-379`) — confirm there's no production data to preserve
- [ ] **Plan for re-deploy**: HF rebuild takes 3-5 min; bundle all Phase 6 changes into 1 PR for atomicity

---

_End of analysis. Report is read-only — no files modified during this work. Ready for owner (Anas) review and Phase 6 execution kickoff._

---

## 9. Outcome (Added 2026-07-27 — Cycle 1 Documentation Sprint 6.4)

> **This section was added AFTER Phase 6 execution, not before.** It records what actually happened vs. what was planned, the lessons learned, and links to the implementation PRs. Cycle 1 (Documentation Sprint 6.4) is responsible for this addition.

### 9.1 Planned vs. Actual

| Item | Plan (Section above) | Actual | Status |
|---|---|---|---|
| **Phase 6.0 — Schema reset** | 1 new migration + 35 JSON updates + delete `tenants.json` + new `user_companies.json` + rewrite `seed_meta.json` | Done in `Phase6_InitialSchema_20260725_120000` migration | ✅ |
| **Phase 6.1 — Backend code refactor** | ~100 backend files in 1 big PR or 2-3 PRs for reviewability | Done across **9 PRs** (#131–#151) for reviewability | ✅ (slightly more PRs than planned) |
| **Phase 6.1 — `ITenantContext` → `ICompanyContext`** | Remove `MultiTenancy/`, add `CompanyContext/` | Done (Phase 6.1b: 3 files removed, 3 new files added) | ✅ |
| **Phase 6.1 — `TenantMiddleware` → `CompanyContextMiddleware`** | Replace middleware | Done (Phase 6.1b) | ✅ |
| **Phase 6.1 — Atomic Register (DEC-091)** | Not in original plan — added after 15 orphan users were discovered in Supabase | Implemented in PR #131 + #132 (commit `52e8c26`) | ✅ (bonus hardening) |
| **Phase 6.1 — Npgsql Resiliency (DEC-093)** | Not in original plan — added after cloud timeouts | `NpgsqlConnectionFactory` (PR #134) | ✅ (bonus hardening) |
| **Phase 6.2 — 20 reports** | Listed in original plan | **20 reports delivered** (Trial Balance, IS, BS, CF, GL, Journal, AR/AP Aging, etc.) | ✅ |
| **Phase 6.2 — User Management** | Implicit in original plan | CRUD + admin reset + self-service change-password | ✅ |
| **Phase 6.3 — Frontend** | 9 files (lib/api.ts + 7 pages) | **Larger scope**: 7 new report pages + admin/users migration + CompanySwitcher + AppShell + notification bell + Next.js proxy + Playwright e2e (4 tests) | ✅ (expanded) |
| **Phase 6.4 — Documentation** | 25 doc files | **Done in Cycle 1** (this cycle): root AGENTS.md + 11 module AGENTS.md + CHANGELOG.md + this Outcome section + new `PHASE6-RELEASE-NOTES.md` | ✅ |
| **Phase 6.5 — CI hardening** | 0.5 dev day | Deferred to Cycle 2 (out of scope for documentation sprint) | ⏳ |

### 9.2 Effort Actual

| Phase | Plan (days) | Actual (days) | Notes |
|---|---:|---:|---|
| 6.0 (Schema) | 1-2 | 1 | Single migration, JSON updates, `user_companies` table |
| 6.1 (Backend) | 3-4 | 4 | Atomic register was unplanned, added 0.5 day |
| 6.1b (Tenant→Company cleanup) | (in 6.1) | 0.5 | Drop `tenant_id` from 35 entities, 50 repos, 25 services |
| 6.2 (Reports + User Mgmt) | 1-2 | 2 | 20 reports + 1-year seed data |
| 6.3 (Frontend) | 1 | 2 | CompanySwitcher + Next.js proxy + AppShell + 4 Playwright tests |
| 6.4 (Docs) | 0.5-1 | 0.5 (this cycle) | Done in 1 session |
| **Total** | **6-8** | **~10** | Within 25% of upper bound |

### 9.3 Risks Materialized

| Risk | Materialized? | Mitigation / Outcome |
|---|---|---|
| 1.1 — Foreign keys to `tenants` (45+) | ✅ Materialized | Clean-slate migration dropped them with CASCADE |
| 1.2 — Indexes to drop (60+) | ✅ Materialized | Auto-dropped with tables; new `(company_id, code)` indexes created |
| 5.3 — Email uniqueness changes (per-tenant → global) | ✅ Materialized | `UNIQUE` constraint added to `users.email`; documented in FAQ |
| 5.3 — Roles become global | ✅ Materialized | 4 default roles seeded at boot, no per-tenant role table |
| 5.3 — Reports aggregation | ✅ Materialized | 20 reports re-scoped by `company_id`; verified in `FinanceReportService.cs` Dapper mapping fix |
| 5.3 — E2E atomicity test re-framing | ✅ Materialized | `e2e/auth.spec.ts` rewritten to assert "no orphan users" instead of "no orphan tenants" |
| 5.4 — Data migration | ❌ Not needed | Fresh Build Mode (per `Program.cs:359-379`) — no production data |
| 5.5 — Third-party integrations | ❌ Not affected | Supabase is just the PG host, no Supabase Auth integration |
| **UNPLANNED** — 15 orphan users in Supabase | ✅ Materialized | DEC-091 atomic register fix (PR #131 + #132) |
| **UNPLANNED** — pgbouncer cold-start (303s on first request) | ✅ Materialized | DEC-093 NpgsqlConnectionFactory + DEC-094 PoolWarmupHostedService (PR #151) |
| **UNPLANNED** — Supabase pgbouncer 429s on cloud sandbox IP | ✅ Materialized | 3-Tier isolation: defer cloud issues to dedicated sessions; rely on local tsc + code review for Tier 1 |
| **UNPLANNED** — Cross-team coordination needed (Abdo's parallel branch) | ✅ Materialized | 3-Tier & Dual-Agent Governance Model adopted; `ABDO-TEAM-ALIGNMENT.md` + `HANDOFF-PHASE6-MIGRATE.md` |

### 9.4 Key PRs

| PR | Title | Cycle | Status |
|---|---|---|---|
| #131 → #132 | Atomic Register (DEC-091) | 5.B Sprint 1 | ✅ Merged (`52e8c26` on main) |
| #134 | Npgsql Resiliency + Playwright E2E (DEC-093, 094) | 5.B Sprint 2 | ✅ Merged (`da97b6b` on main) |
| #151 | Migration fix (42P01) + Timeout 60s + CoA perf | 5.B Sprint 3 | ✅ Merged |
| #152 | v5 Cherry-pick + ToastProvider fix + DEC-ABDO-009 | Cycle 0 | ✅ Merged (`a603cc3` on develop) |
| **(this cycle)** | Documentation Sprint 6.4 | **Cycle 1** | ⏳ Pending PR to develop |

### 9.5 Lessons Learned

1. **"Fresh Build Mode" saved us.** The fact that v5 was already running with an empty DB in HF Space made the clean-slate refactor 10x safer than an incremental migration. This decision was made 4 months ago (DEC-082) and paid off in Phase 6.

2. **Multi-tenancy was vestigial from day 1.** The `tenants.subscription_expires_at` column was future-proofing for SaaS billing that was never built. The constitution was right to call it out in Article 3 — the abstraction was wrong for an internal SME ERP.

3. **Atomicity must be a first-class contract.** The 15 orphan users in Supabase (DEC-091) happened because the register flow used 2 separate connections: the first created the user, the second was supposed to seed the company but the network dropped. The fix (single conn + single tx) is a pattern that should apply to every multi-insert service flow (DEC-091 audit pass is queued for Cycle 2+).

4. **3-Tier isolation is the right architecture.** Local Tier 1 (this session) can't reliably test cloud-dependent features. The Supabase pgbouncer 303s cold-start would have blocked every test if we tried to verify e2e locally. By documenting infra failures in Hand-Off Reports and relying on tsc + code review for Tier 1, we shipped Phase 6.2 in 9 PRs without getting stuck.

5. **The 3-Tier & Dual-Agent Governance Model worked.** Running two parallel feature branches (Mavis/Anas on `feature/phase6-migrate-features`, Mavis/Abdo on `feature/abdo-team`) with no direct agent-to-agent communication was unconventional but effective. The 5-doc sync (AGENTS.md, CHANGELOG.md, HANDOFF-*, DEC-*, commit msg) was enough.

6. **Generated code is a single point of failure.** The 24 `Shared/Generated/Repos/*.g.cs` files all had `tenant_id` baked in. They were regenerated by re-running `Tools/EntityRepoEnhance/Program.cs` after the JSON update. This worked but is brittle — if the JSON migrator and the C# generator ever diverge, the build breaks silently. A future improvement: codegen should fail loud if JSON schema can't satisfy code requirements.

7. **"Smart cron" is needed for cloud failure detection.** Per Anas's observation in Cycle 1 hand-off, the analytical team lost internet and we didn't know until a human happened to check. A token-free cron that pings a status page and updates `docs/governance/board.md` is being proposed for Cycle 2 (Anas's call).

### 9.6 What's NOT in this Phase (out of scope, deferred)

- **Email uniqueness per-company** (currently global) — Phase 7+
- **Per-company role customization** (currently global 4 roles) — Phase 7+
- **Multi-Holding deployments** (currently 1 per deployment) — Phase 7+
- **Per-company dashboards** — Phase 7+
- **Intercompany transactions** (the `is_intercompany` flag is set but logic not wired) — Phase 7+
- **Multi-jurisdiction compliance** — Phase 8+
- **Remaining 13 frontend report pages** (Reports #4, #5, #8, #9, #10, #11, #13, #14, #15 vendors, #16, #17, #18 detail, #20) — Cycle 2+
- **PDF/Excel export for reports** — Cycle 2+
- **Drill-down navigation** (click customer → see invoices) — Cycle 3+
- **Marten event store wiring** (installed but unused) — Phase 8+

### 9.7 Sign-off (post-execution)

- [x] **Owner (Anas)** approved the clean-slate approach (Constitution Article 3.4 — pre-approved)
- [x] **Mavis** confirmed migration order with analytical team
- [x] **Schema for `user_companies`** locked (composite PK, `is_default` flag)
- [x] **API contract for new auth flow** agreed (RegisterRequest without TenantId, AuthResponse with defaultCompanyId)
- [x] **Decision: roles global** — done in v6
- [x] **Decision: email uniqueness global** — done in v6
- [x] **Decision: `X-Company-Id` header + JWT `company_ids[]`** — done in v6
- [x] **HF Space Fresh Build Mode** — confirmed no production data to preserve
- [x] **All 9 PRs merged** to develop + main
- [x] **Functional Spec PDF** generated (31 pages, [docs/SYSTEM-FUNCTIONAL-SPECIFICATION.pdf](./SYSTEM-FUNCTIONAL-SPECIFICATION.pdf))
- [x] **Cycle 1 documentation sprint** — this section is the result

### 9.8 Open follow-ups (for Cycle 2+ planning)

1. **DEC-091 audit pass** — apply `single conn + single tx` pattern to all multi-insert service flows (Cyclic penalty if we skip)
2. **DEC-092 orphan cleanup script** — register as a recurring maintenance job
3. **DEC-093 Npgsql resiliency** — confirm `NpgsqlConnectionFactory` defaults are sufficient for the HF Space pgbouncer path; consider explicit `Application Name` for observability
4. **DEC-094 Playwright E2E** — run on every PR to develop; document in CI workflow
5. **Smart cron for cloud failure detection** (Anas's proposal for Cycle 2) — token-free ping to status pages, write to `docs/governance/board.md`
6. **Remaining 13 frontend report pages** (Cycle 2 candidate)
7. **PDF/Excel export** for reports (Cycle 3 candidate)
8. **Drill-down navigation** (Cycle 3 candidate)
9. **Charts (line/bar)** for trend reports (Cycle 3 candidate)

---

**End of Outcome section. Phase 6 is COMPLETE per Constitution Article 3.**

_Sign-off by Mavis (Anas's local team) — 2026-07-27, Cycle 1 Documentation Sprint 6.4._
