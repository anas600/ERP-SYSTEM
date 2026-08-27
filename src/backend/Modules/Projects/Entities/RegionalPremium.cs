using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 62 / DEC-197 — Regional Premium (خصم المنطقة).
///
/// <para>In Libyan construction projects, certain regions (NDB-Oil, NDB-Gas, parts of
/// Tripoli/Benghazi/Misrata) require the contractor to withhold three statutory
/// deductions on every progress billing:</para>
/// <list type="bullet">
///   <item><b>NDB</b> — National Development Budget (typically 1.5% of gross)</item>
///   <item><b>CIT</b> — Corporate Income Tax (typically 5% of gross)</item>
///   <item><b>SS</b> — Social Security (varies, often 0% for fixed-price contracts)</item>
/// </list>
///
/// <para>The table is keyed by (project_id, region) with <c>UNIQUE</c>, so each
/// project can have at most one active premium per region. A project typically has
/// a single active row; the <c>is_active</c> flag allows historical rows to be
/// preserved when rates change.</para>
///
/// <para><b>Constitution Article 3</b>: <c>CompanyId</c> is required and not nullable
/// (per L19 / L29 / L30 lessons). The service resolves the company from the JWT
/// context (never from the request DTO).</para>
/// </summary>
public class RegionalPremium
{
    public Guid Id { get; set; }

    /// <summary>Sprint 62 / DEC-197 — Constitution Article 3, L19. Not nullable.</summary>
    public Guid CompanyId { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>
    /// Region label — 'Tripoli' | 'Benghazi' | 'Misrata' | 'NDB-Oil' | 'NDB-Gas' | 'Other'.
    /// Stored as TEXT to allow new region labels without a schema change.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>NDB deduction percent (0-100). Default 1.5% per NDB regulation.</summary>
    public decimal NdbPercent { get; set; } = 1.5m;

    /// <summary>Corporate Income Tax percent (0-100). Default 5% per Libyan tax law.</summary>
    public decimal CitPercent { get; set; } = 5.0m;

    /// <summary>Social Security percent (0-100). Default 0% (most fixed-price contracts are exempt).</summary>
    public decimal SsPercent { get; set; } = 0.0m;

    /// <summary>Only the active row is applied in billing calculations. Historical rows can be retained.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
