using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// عقد المشروع (Sprint 58 / DEC-163).
///
/// عقد مقاولات/توريدات/خدمات مرتبط بمشروع واحد (UNIQUE company_id+project_id).
/// يحتفظ بالقيمة الإجمالية + نسب الدفعة المقدمة والاحتجاز.
///
/// Progress Billings (DEC-164) ترجع على هذا العقد لحساب:
/// - work_completed_percent (CUMULATIVE)
/// - gross_amount
/// - advance_deducted
/// - retention_deducted
/// - net_amount (= gross - advance - retention)
/// </summary>
public class Contract
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>رقم العقد (اختياري — للقطاع العام قد يكون نص حر).</summary>
    public string? ContractNumber { get; set; }

    /// <summary>القيمة الإجمالية للعقد (LYD).</summary>
    public decimal ContractValue { get; set; }

    /// <summary>نسبة الدفعة المقدمة % (مثال: 10.00 = 10%). تُخصم مرة واحدة من أول مستخلص.</summary>
    public decimal AdvancePercent { get; set; }

    /// <summary>نسبة احتجاز ضمان % (مثال: 5.00 = 5%). تُخصم من كل مستخلص بدءاً من Billing #N.</summary>
    public decimal RetentionPercent { get; set; }

    /// <summary>رقم المستخلص الذي يبدأ عنده احتجاز الضمان (1 = من أول مستخلص، 2 = من الثاني...).</summary>
    public int RetentionStartBilling { get; set; } = 1;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }
}
