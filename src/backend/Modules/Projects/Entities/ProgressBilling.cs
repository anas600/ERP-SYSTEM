using System;

namespace ERPSystem.Modules.Projects.Entities;

public enum BillingStatus
{
    Draft = 1,
    Invoiced = 2,
    Cancelled = 3,
}

/// <summary>
/// مستخلص مشروع (Sprint 58 / DEC-164).
///
/// يحسب من العقد (contract_value × work_completed_percent / 100) مع
/// خصم الدفعة المقدمة (من أول مستخلص) والاحتجاز (من Billing #retention_start_billing).
///
/// عند الموافقة (Approve): يتحول لـ Invoiced ويُنشأ sales_invoice + journal_entry تلقائياً.
/// </summary>
public class ProgressBilling
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ContractId { get; set; }

    /// <summary>رقم المستخلص (مثال: "B-2026-001"). unique per company.</summary>
    public string BillingNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }

    /// <summary>نسبة الإنجاز التراكمية (CUMULATIVE). 0-100. لازم تكون monotonically increasing.</summary>
    public decimal WorkCompletedPercent { get; set; }

    /// <summary>المبلغ الإجمالي = contract_value × percent / 100.</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>الدفعة المقدمة المخصومة (مرة واحدة، total = contract × advance%).</summary>
    public decimal AdvanceDeducted { get; set; }

    /// <summary>الاحتجاز المخصوم (لكل مستخلص من Billing #N، amount = gross × retention%).</summary>
    public decimal RetentionDeducted { get; set; }

    /// <summary>الصافي = gross - advance - retention.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Sprint 62 / DEC-197 — Regional Premium deducted (NDB + CIT + SS) on this billing.
    /// Zero if the project has no active regional premium row. Computed as
    /// <c>gross × (Ndb% + Cit% + Ss%) / 100</c>.
    /// </summary>
    public decimal RegionalPremiumDeducted { get; set; }

    /// <summary>
    /// Sprint 62 / DEC-197 — Net amount after regional premium deduction.
    /// = <c>NetAmount - RegionalPremiumDeducted</c>. This is the actual cash the
    /// contractor expects to receive on this billing.
    /// </summary>
    public decimal NetAmountAfterPremium { get; set; }

    public BillingStatus Status { get; set; } = BillingStatus.Draft;

    /// <summary>الفاتورة اللي انولّدت عند الـ approve (nullable قبل الـ approve).</summary>
    public Guid? InvoiceId { get; set; }

    /// <summary>القيد اللي انولّد عند الـ approve (nullable قبل الـ approve).</summary>
    public Guid? JournalEntryId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
