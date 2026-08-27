using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 64 / DEC-223 — Sub-ProgressBilling (مستخلص مقاول باطن).
///
/// <para>A monthly (or periodic) progress claim from a subcontractor against a
/// <see cref="SubContract"/>. The subcontractor reports the cumulative
/// <c>WorkCompletedPercent</c>; the service layer computes <c>GrossAmount</c>,
/// <c>RetentionDeducted</c>, and <c>NetPayable</c> per the
/// <c>SubContract.ContractValue</c> + <c>RetentionPercent</c> + <c>RetentionReleaseBilling</c>.</para>
///
/// <para><b>Computation algorithm</b> (lives in <c>SubProgressBillingService.CreateAsync</c>):</para>
/// <code>
/// gross = sub_contract.contract_value × (work_completed_percent / 100)
/// previousBillingsAmount = SUM(gross) of all prior billings
/// retentionDeducted = (billing_count &lt;= sub_contract.retention_release_billing)
///                       ? gross × retention_percent / 100
///                       : 0
/// netPayable = gross - retentionDeducted
/// </code>
///
/// <para><b>Status</b> (int, mirrors <see cref="SubProgressBillingStatus"/>):</para>
/// <list type="number">
///   <item>Draft — editable, not yet billable.</item>
///   <item>Approved — locked, can receive payments.</item>
///   <item>Paid — fully paid (set by Wave 3A Statement calc; here it stays at Approved).</item>
///   <item>Cancelled — discarded, excluded from balance.</item>
/// </list>
///
/// <para><b>Article 3</b> + <b>L19 / DEC-095</b>: <c>CompanyId</c> is required
/// and NOT nullable. The service layer resolves the company from the JWT
/// context — never from the request DTO.</para>
///
/// <para><b>UNIQUE</b>: <c>(sub_contract_id, billing_number)</c> — no duplicate
/// billing numbers within a sub-contract. Enforced at the DB level.</para>
/// </summary>
public class SubProgressBilling
{
    public Guid Id { get; set; }

    /// <summary>Constitution Article 3 / L19 — required, NOT NULL.</summary>
    public Guid CompanyId { get; set; }

    public Guid SubContractId { get; set; }

    /// <summary>Sequential billing number (e.g. "B-001"). UNIQUE within a sub-contract.</summary>
    public string BillingNumber { get; set; } = string.Empty;

    public DateTime BillingDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }

    /// <summary>Cumulative work completed % (0-100). Must be monotonically non-decreasing.</summary>
    public decimal WorkCompletedPercent { get; set; }

    /// <summary>sub_contract.contract_value × (WorkCompletedPercent / 100).</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>Retention withheld on THIS billing (0 if past retention_release_billing).</summary>
    public decimal RetentionDeducted { get; set; }

    /// <summary>SUM(gross) of all PRIOR billings (snapshot for audit).</summary>
    public decimal PreviousBillingsAmount { get; set; }

    /// <summary>GrossAmount - RetentionDeducted.</summary>
    public decimal NetPayable { get; set; }

    /// <summary>1=Draft, 2=Approved, 3=Paid, 4=Cancelled (see <see cref="SubProgressBillingStatus"/>).</summary>
    public int Status { get; set; } = 1;

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Sprint 64 / DEC-223 — Sub-ProgressBilling lifecycle states.
/// Mirrors the int value stored in <see cref="SubProgressBilling.Status"/>.
/// </summary>
public enum SubProgressBillingStatus
{
    Draft = 1,
    Approved = 2,
    Paid = 3,
    Cancelled = 4,
}
