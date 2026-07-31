// Sprint 11 T2 (BE Jimi) — Company DTOs.
//
// Contract for the FE (api-types.ts):
//   - CompanyTreeNodeDto: flat recursive shape with all Company fields needed
//     by the tree widget, no `Company` wrapper. `Children` is the nested list.
//   - SubsidiaryListDto: wrapper for /api/companies/{id}/subsidiaries — the
//     FE wants `{ parentCompanyId, subsidiaries: Company[] }`, not a bare
//     list, so it can validate the parent id without an extra request.
//
// Why new DTOs and not reuse `Company` directly:
// - The wire shape is what the FE consumes, and we want a stable contract
//   independent of the entity (the entity may grow new fields the FE doesn't need).
// - `Company` has nav properties (`Parent`) and a different default JSON
//   shape that would force a custom converter on the FE.
// - Holding tree is a dashboard widget, not a list view, so we want the data
//   dense and predictable.
//
// Field casing: PascalCase here, camelCase on the wire (default System.Text.Json
// config on Host/Program.cs).
//
// Article 3: no `tenant_id` anywhere. Multi-company model only.

using System;
using System.Collections.Generic;
using ERPSystem.Modules.Companies.Entities;

namespace ERPSystem.Modules.Companies.Application.DTOs;

public sealed record CompanyTreeNodeDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentCompanyId,
    bool IsGroup,
    bool IsActive,
    IReadOnlyList<CompanyTreeNodeDto> Children);

// Sprint 11 T2: wrapper for /api/companies/{id}/subsidiaries. The legacy
// service returned a bare list; the FE expects an object that carries the
// parent id alongside the children so the dashboard can render breadcrumbs
// without a second request.
public sealed class SubsidiaryListDto
{
    public Guid ParentCompanyId { get; set; }
    public IReadOnlyList<Company> Subsidiaries { get; set; } = Array.Empty<Company>();
}
