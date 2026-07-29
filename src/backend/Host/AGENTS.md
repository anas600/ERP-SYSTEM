# 🚪 AGENTS.md — src/backend/Host/

> **Entry point + controllers + middleware.** Read `/AGENTS.md`, `/src/AGENTS.md`, and `/src/backend/AGENTS.md` first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

ASP.NET Core host: `Program.cs`, controllers, middleware, hosted services. The "front door" of the API.

## Ownership

| Subtree | Owner | Authority |
|---------|-------|-----------|
| `src/backend/Host/Controllers/` | Jimi تنفيذي | All API endpoints |
| `src/backend/Host/Middleware/` | Jimi تنفيذي | Cross-cutting middleware (JWT, Company, Logging, Errors) |
| `src/backend/Host/Bootstrap/` | Jimi تنفيذي | Hosted services (e.g., DemoDataSeeder) |
| `src/backend/Host/Auth/`, `Audit/`, `Utilities/`, `data-types/` | Jimi تنفيذي | Sub-features |

## Local Contracts

- **All endpoints** filter by `company_id` from `CompanyContext`.
- **All controllers** use `[Authorize]` + `[CompanyAuthorize]`.
- **All DTOs** are in `Modules/<Module>/Application/DTOs/`, not in Host.
- **All business logic** is in `Modules/<Module>/Application/Services/`, not in Host.
- **Middleware order** (in `Program.cs`):
  1. GlobalException
  2. RequestLogging
  3. JWT Auth
  4. CompanyContext
  5. Authorization

## Work Guidance

### Adding a Controller
1. Create `src/backend/Host/Controllers/<Resource>Controller.cs`.
2. Inject the appropriate service from `Modules/<Module>/Application/Services/`.
3. Use `[Authorize]` on the controller.
4. Filter by `CompanyContext.CompanyId` in every action.
5. Add OpenAPI attributes for documentation.

### Adding Middleware
1. Create in `src/backend/Host/Middleware/<Name>Middleware.cs`.
2. Register in `Program.cs` pipeline.
3. Document order in this file.

## Verification

- [ ] `dotnet build` — zero errors.
- [ ] All controllers have `[Authorize]`.
- [ ] All actions filter by `CompanyContext.CompanyId`.
- [ ] No business logic in controllers.
- [ ] No `tenant_id` references: `grep -r "tenant" src/backend/Host/`.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| `src/backend/Host/Controllers/` | API controllers | **TO CREATE** |
| `src/backend/Host/Middleware/` | Cross-cutting middleware | **TO CREATE** |
| `src/backend/Host/Bootstrap/` | Hosted services | **TO CREATE** |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
