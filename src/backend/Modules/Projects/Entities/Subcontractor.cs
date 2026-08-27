using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 64 / DEC-221 — Subcontractor (مقاول باطن).
///
/// <para>Master data for a third-party contractor (company or individual) that
/// performs a portion of a project's work under a sub-contract.</para>
///
/// <para><b>Article 3</b> + <b>L19 / DEC-095</b>: <c>CompanyId</c> is required
/// and NOT nullable. The service layer resolves the company from the JWT
/// context — never from the request DTO.</para>
///
/// <para><b>Soft delete</b>: <c>IsActive=false</c> marks the subcontractor as
/// retired. The row is preserved for historical reporting (sub-contracts +
/// progress billings + payments reference it).</para>
///
/// <para><b>UNIQUE</b>: <c>(company_id, code)</c> — each company has its own
/// code namespace. Enforced at the DB level (see migration).</para>
/// </summary>
public class Subcontractor
{
    public Guid Id { get; set; }

    /// <summary>Constitution Article 3 / L19 — required, NOT NULL.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Short business code (e.g. "ELEC-001"). UNIQUE within company.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name (English / transliterated).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Arabic name (optional — most fields stay NULL for non-Arabic names).</summary>
    public string? NameAr { get; set; }

    /// <summary>Primary contact person (foreman, owner, …).</summary>
    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Free-text trade category — "electrical", "plumbing", "carpentry", "masonry", …</summary>
    public string? TradeSpecialty { get; set; }

    public string? TaxId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
