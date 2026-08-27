using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 64 / DEC-224 — Sub-Payment (دفعة لمقاول باطن).
///
/// <para>An actual money-out event: a payment to a subcontractor against an
/// approved <see cref="SubProgressBilling"/>, OR a retention-release payment
/// (a separate payment with <c>RetentionReleased &gt; 0</c> created by
/// <c>SubPaymentService.ReleaseRetentionAsync</c>).</para>
///
/// <para><b>Two payment kinds</b>:</para>
/// <list type="bullet">
///   <item><b>Regular payment</b> — <c>RetentionReleased = 0</c>, <c>Amount</c> settles (part of) a billing.</item>
///   <item><b>Retention release</b> — <c>RetentionReleased &gt; 0</c>, <c>Amount = 0</c> (the released amount
///         is tracked in <c>RetentionReleased</c>). The service creates this when the user calls
///         <c>ReleaseRetentionAsync</c>.</item>
/// </list>
///
/// <para><b>Article 3</b> + <b>L19 / DEC-095</b>: <c>CompanyId</c> is required
/// and NOT nullable. The service layer resolves the company from the JWT
/// context — never from the request DTO.</para>
///
/// <para><b>UNIQUE</b>: <c>(sub_contract_id, payment_number)</c> — no duplicate
/// payment numbers within a sub-contract. Enforced at the DB level.</para>
/// </summary>
public class SubPayment
{
    public Guid Id { get; set; }

    /// <summary>Constitution Article 3 / L19 — required, NOT NULL.</summary>
    public Guid CompanyId { get; set; }

    public Guid SubContractId { get; set; }

    public Guid SubProgressBillingId { get; set; }

    /// <summary>Sequential payment number (e.g. "P-001"). UNIQUE within a sub-contract.</summary>
    public string PaymentNumber { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }

    /// <summary>Cash paid to the subcontractor (excludes retention release).</summary>
    public decimal Amount { get; set; }

    /// <summary>Retention released on this payment (0 for regular payments, &gt; 0 for release payments).</summary>
    public decimal RetentionReleased { get; set; } = 0m;

    /// <summary>"bank_transfer" | "check" | "cash" — free text for now.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Bank reference / check number / wire reference.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
