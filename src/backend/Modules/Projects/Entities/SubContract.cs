using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 64 / DEC-222 — Sub-Contract (عقد الباطن).
///
/// <para>A sub-contract ties a <see cref="Subcontractor"/> to a parent
/// <c>Project</c> with a defined <c>ScopeOfWork</c> and monetary
/// <c>ContractValue</c>. It governs subsequent <b>progress billings</b> (DEC-223)
/// and <b>payments</b> (DEC-224).</para>
///
/// <para><b>Retention</b>:</para>
/// <list type="bullet">
///   <item><c>RetentionPercent</c> (0-100) — % withheld on each billing until release.</item>
///   <item><c>RetentionReleaseBilling</c> — release retention after N approved billings.</item>
/// </list>
///
/// <para><b>Status</b> (int, mirrors the <see cref="SubContractStatus"/> enum):</para>
/// <list type="number">
///   <item>Active — billings can still be created.</item>
///   <item>Completed — all billings settled, contract closed.</item>
///   <item>Cancelled — contract terminated (no further billings).</item>
/// </list>
///
/// <para><b>Article 3</b> + <b>L19 / DEC-095</b>: <c>CompanyId</c> is required
/// and NOT nullable. The service layer resolves the company from the JWT
/// context — never from the request DTO.</para>
///
/// <para><b>UNIQUE</b>: <c>(project_id, contract_number)</c> — no duplicate
/// contract numbers within a project. Enforced at the DB level.</para>
/// </summary>
public class SubContract
{
    public Guid Id { get; set; }

    /// <summary>Constitution Article 3 / L19 — required, NOT NULL.</summary>
    public Guid CompanyId { get; set; }

    public Guid ProjectId { get; set; }
    public Guid SubcontractorId { get; set; }

    public string ContractNumber { get; set; } = string.Empty;
    public string ScopeOfWork { get; set; } = string.Empty;

    /// <summary>Total contract value (LYD). Defaults to 0; must be &gt;= 0.</summary>
    public decimal ContractValue { get; set; }

    /// <summary>Retention percent (0-100). Default 10% per Libyan construction norm.</summary>
    public decimal RetentionPercent { get; set; } = 10.0m;

    /// <summary>Release retention after this many approved billings. Default 3.</summary>
    public int RetentionReleaseBilling { get; set; } = 3;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>1=Active, 2=Completed, 3=Cancelled (see <see cref="SubContractStatus"/>).</summary>
    public int Status { get; set; } = 1;

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Sprint 64 / DEC-222 — Sub-Contract lifecycle states.
/// Mirrors the int value stored in <see cref="SubContract.Status"/>.
/// </summary>
public enum SubContractStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,
}
