// =====================================================================
// Sprint 9 (Jimi 2 — T2): BE-FE contract DTOs for the Companies module.
// =====================================================================
// Purpose: provide a clean, XML-doc-commented surface that the FE can
//   mirror in `src/frontend/lib/api-types.ts`. The existing request/response
//   shapes inside CompanyService.cs (CreateCompanyRequest, CompanyPage,
//   HoldingDetail, etc.) are still used by the controller — this file
//   introduces a parallel "Dto" shape so the FE has a stable contract to
//   import and a refactor can swap them in a later sprint without breaking
//   the runtime wire format.
//
// Multi-Company model (Constitution Article 3): no `tenant_id` anywhere.
//   All company-scoped payloads use `companyId` (Guid) as the discriminator.
// =====================================================================

using System;
using System.Collections.Generic;
using ERPSystem.Modules.Companies.Entities;

namespace ERPSystem.Modules.Companies.Application.Dtos;

/// <summary>
/// Public-facing <see cref="Company"/> projection returned by
/// <c>GET /api/companies/{id}</c> and friends. Mirrors the
/// <c>Company</c> entity (Phase 6.1b — multi-company model) but lives in the
/// <see cref="Dtos"/> namespace so the FE has a single, stable import path.
/// </summary>
public sealed class CompanyDto
{
    /// <summary>Stable identifier (UUID v4). Maps to <c>companies.id</c>.</summary>
    public Guid Id { get; set; }

    /// <summary>Short human-readable code (unique, case-insensitive). E.g. "ALF", "BRJ".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name (Arabic primary per Constitution; EN in <c>nameEn</c> if added later).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-friendly slug (auto-generated from name + code; unique per row). Nullable for back-compat.</summary>
    public string? Slug { get; set; }

    /// <summary>Official legal name (used on invoices, contracts, tax forms).</summary>
    public string? LegalName { get; set; }

    /// <summary>Self-reference to the parent company (Holding). <c>null</c> for the root Holding row.</summary>
    public Guid? ParentCompanyId { get; set; }

    /// <summary><c>true</c> for Holding rows (parent_company_id IS NULL). <c>false</c> for ordinary subsidiaries.</summary>
    public bool IsGroup { get; set; }

    /// <summary>ISO 4217 currency code (LYD, USD, EUR, …). Drives the CoA base currency.</summary>
    public string BaseCurrency { get; set; } = "LYD";

    /// <summary>Soft-delete flag. Inactive rows are hidden from the default list view.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp of row creation.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of last row update (any field).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Convenience factory — projects a <see cref="Company"/> entity to the FE-facing DTO.</summary>
    public static CompanyDto From(Company c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        Slug = c.Slug,
        LegalName = c.LegalName,
        ParentCompanyId = c.ParentCompanyId,
        IsGroup = c.IsGroup,
        BaseCurrency = c.BaseCurrency,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}

/// <summary>
/// Request body for <c>POST /api/companies</c> (top-level create, idempotent on
/// <see cref="Code"/>). <see cref="ParentCompanyId"/> = <c>null</c> means
/// "create as a root company"; setting it routes the new company as a
/// subsidiary of the given Holding (must be a row with <c>is_group = true</c>).
/// </summary>
public sealed class CreateCompanyRequestDto
{
    /// <summary>Short code (unique, case-insensitive). Required.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. Required.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional legal name. Falls back to <see cref="Name"/> when blank.</summary>
    public string? LegalName { get; set; }

    /// <summary>ISO 4217 currency. Defaults to "LYD" when blank.</summary>
    public string BaseCurrency { get; set; } = "LYD";

    /// <summary>Parent Holding id. <c>null</c> → root company.</summary>
    public Guid? ParentCompanyId { get; set; }
}

/// <summary>
/// Request body for <c>POST /api/companies/holding</c>. Always creates a
/// Holding row (is_group=true, parent_company_id=null) and seeds the
/// default Chart of Accounts (47 accounts) on it.
/// </summary>
public sealed class CreateHoldingRequestDto
{
    /// <summary>Short Holding code (defaults to "000" for the deployment's only Holding).</summary>
    public string Code { get; set; } = "000";

    /// <summary>Display name (e.g. "شركة الفجر القابضة"). Required.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional legal name. Falls back to <see cref="Name"/> when blank.</summary>
    public string? LegalName { get; set; }

    /// <summary>ISO 4217 base currency. Defaults to "LYD".</summary>
    public string BaseCurrency { get; set; } = "LYD";
}

/// <summary>Request body for <c>POST /api/companies/subsidiary</c>. Adds a child company to an existing Holding.</summary>
public sealed class AddSubsidiaryRequestDto
{
    /// <summary>The Holding id (must be a row with <c>is_group = true</c>).</summary>
    public Guid ParentCompanyId { get; set; }

    /// <summary>Short code for the new subsidiary. Required, unique.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name. Required.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional legal name.</summary>
    public string? LegalName { get; set; }
}

/// <summary>
/// Paged list response for <c>GET /api/companies</c>. <c>Page</c> is 1-based.
/// <c>PageSize</c> is clamped to [1, 100] by the service.
/// </summary>
public sealed class CompanyPageDto
{
    /// <summary>Companies for the current page (in the same order the repository returns).</summary>
    public IReadOnlyList<CompanyDto> Items { get; set; } = Array.Empty<CompanyDto>();

    /// <summary>Total number of matching rows (across all pages).</summary>
    public int Total { get; set; }

    /// <summary>1-based page index that was returned.</summary>
    public int Page { get; set; }

    /// <summary>Page size that was applied (after clamp).</summary>
    public int PageSize { get; set; }
}

/// <summary>
/// A single node in the company tree returned by <c>GET /api/companies/tree</c>.
/// The tree is built from <c>companies.parent_company_id</c> (self-ref, not a
/// separate join table) — see <c>CompanyRepository.BuildTree</c>.
/// </summary>
public sealed class CompanyTreeNodeDto
{
    /// <summary>The company at this node.</summary>
    public CompanyDto Company { get; set; } = null!;

    /// <summary>Immediate subsidiaries (recursive). Empty for leaf rows.</summary>
    public List<CompanyTreeNodeDto> Children { get; set; } = new();
}

/// <summary>
/// Holding detail returned by <c>GET /api/holdings/{slug}</c>. The Holding
/// is identified by (is_group=true, parent_company_id IS NULL).
/// </summary>
public sealed class HoldingDetailDto
{
    /// <summary>Holding id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short Holding code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>URL-friendly slug (Phase 6.4 / Sprint 1). Unique per row.</summary>
    public string? Slug { get; set; }

    /// <summary>ISO 4217 base currency for the Holding.</summary>
    public string BaseCurrency { get; set; } = "LYD";

    /// <summary>Soft-delete flag.</summary>
    public bool IsActive { get; set; }

    /// <summary>Immediate subsidiary companies (1 level deep, not the full tree).</summary>
    public List<HoldingCompanySummaryDto> Companies { get; set; } = new();
}

/// <summary>Summary projection of a subsidiary inside <see cref="HoldingDetailDto.Companies"/>.</summary>
public sealed class HoldingCompanySummaryDto
{
    /// <summary>Subsidiary id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>URL-friendly slug.</summary>
    public string? Slug { get; set; }

    /// <summary>Soft-delete flag.</summary>
    public bool IsActive { get; set; }
}
