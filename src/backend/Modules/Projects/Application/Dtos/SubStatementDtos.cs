using System;

namespace ERPSystem.Modules.Projects.Application.Dtos;

// =============================================================================
// Sprint 64 / DEC-225 — Sub-Statement DTOs (كشف حساب مقاول باطن).
//
// Wire-format for the Sub-Statement API. The service layer maps the rolled-up
// billing/payment aggregates into these DTOs at the controller boundary.
//
// CompanyId is intentionally NOT in the responses (L19 / DEC-095) — the caller
// already knows the active company from the JWT context, so we don't echo it
// back. This keeps the response small and prevents accidental leakage.
// =============================================================================

/// <summary>
/// Returned by GET /api/sub-contracts/{subContractId}/statement.
///
/// <para><b>Formula</b>:</para>
/// <code>
/// totalBilledGross       = SUM(sub_progress_billings.gross_amount       WHERE status != 4)
/// totalRetentionWithheld = SUM(sub_progress_billings.retention_deducted WHERE status != 4)
/// totalRetentionReleased = SUM(sub_payments.retention_released)
/// totalPaid              = SUM(sub_payments.amount + sub_payments.retention_released)
/// outstandingBalance     = totalBilledGross - totalPaid
/// workCompletedToDate    = MIN(100, SUM(work_completed_percent) of all billings)
/// healthStatus           = 'SETTLED' if outstanding == 0 AND totalBilledGross > 0
///                       | 'OVERDUE' if lastBillingDate &gt; 60 days ago AND outstanding &gt; 0
///                       | 'OK' otherwise
/// </code>
/// </summary>
public sealed class SubStatementResponse
{
    public Guid SubContractId { get; set; }
    public string SubcontractorName { get; set; } = string.Empty;
    public string SubContractorCode { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string ScopeOfWork { get; set; } = string.Empty;
    public decimal ContractValue { get; set; }
    public decimal TotalBilledGross { get; set; }
    public decimal TotalRetentionWithheld { get; set; }
    public decimal TotalRetentionReleased { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal WorkCompletedToDate { get; set; }
    public int BillingCount { get; set; }
    public DateTime? FirstBillingDate { get; set; }
    public DateTime? LastBillingDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }

    /// <summary>1=Active, 2=Completed, 3=Cancelled (mirrors <c>SubContract.Status</c>).</summary>
    public int Status { get; set; }

    /// <summary>Arabic label for <see cref="Status"/> (نشط / مكتمل / ملغى).</summary>
    public string StatusName => Status switch
    {
        1 => "نشط",
        2 => "مكتمل",
        3 => "ملغى",
        _ => "غير معروف",
    };

    /// <summary>'OK' | 'OVERDUE' | 'SETTLED'.</summary>
    public string HealthStatus { get; set; } = "OK";

    /// <summary>Arabic label for <see cref="HealthStatus"/>.</summary>
    public string HealthStatusName => HealthStatus switch
    {
        "OK" => "حالة جيدة",
        "OVERDUE" => "متأخر السداد",
        "SETTLED" => "مسوّى",
        _ => "غير معروف",
    };
}

/// <summary>
/// Returned by GET /api/subcontractors/{subcontractorId}/projects/{projectId}/summary.
///
/// Aggregates across ALL sub-contracts that link the given subcontractor to
/// the given project (typically one, but the data model allows several).
/// </summary>
public sealed class SubStatementSummaryResponse
{
    public Guid SubcontractorId { get; set; }
    public string SubcontractorName { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int SubContractCount { get; set; }
    public decimal TotalContractValue { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
}
