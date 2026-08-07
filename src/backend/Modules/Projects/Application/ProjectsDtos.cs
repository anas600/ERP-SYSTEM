using System;
using ERPSystem.Modules.Projects.Entities;
using TaskStatus = ERPSystem.Modules.Projects.Entities.TaskStatus;

namespace ERPSystem.Modules.Projects.Application;

public sealed class CreateProjectRequest
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class ProjectResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CostCenterId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CustomerId { get; set; }
    public ProjectStatus Status { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

// ===== Tasks =====

public sealed class CreateTaskRequest
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedHours { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class UpdateTaskRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int ProgressPercent { get; set; }
}

public sealed class TaskResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ===== Resources =====

public sealed class CreateResourceRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public decimal HourlyRate { get; set; }
}

public sealed class UpdateResourceRequest
{
    public string Name { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ResourceResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ResourceType Type { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
}

// ===== Budget =====

public sealed class ProjectBudgetResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CostCenterId { get; set; }
    public Guid? AccountId { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public decimal UtilizationPercent { get; set; }
    public DateTime? LastRecalculatedAt { get; set; }
}

// ===== Assignments =====

public sealed class CreateAssignmentRequest
{
    public Guid ProjectId { get; set; }
    public Guid TaskId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public sealed class AssignmentResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid TaskId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal EstimatedHours { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== Sprint 57 / DEC-161: Project P&L =====

public sealed class ProjectPnLLine
{
    /// <summary>Account code (مثال: "5401").</summary>
    public string AccountCode { get; set; } = string.Empty;
    /// <summary>Account name (مثال: "مواد خام").</summary>
    public string AccountName { get; set; } = string.Empty;
    /// <summary>المبلغ الموجب (للتكلفة: debit - credit على حسابات Expense).</summary>
    public decimal Amount { get; set; }
}

public sealed class ProjectPnLResponse
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    /// <summary>إجمالي الإيرادات من sales_invoices (Posted فقط).</summary>
    public decimal TotalRevenue { get; set; }
    /// <summary>عدد فواتير البيع المربوطة بالمشروع (داخل النطاق الزمني).</summary>
    public int InvoiceCount { get; set; }
    /// <summary>تكاليف مفصّلة حسب الحساب (Expense accounts فقط، Posted JEs).</summary>
    public List<ProjectPnLLine> CostsByAccount { get; set; } = new();
    /// <summary>إجمالي التكاليف.</summary>
    public decimal TotalCosts { get; set; }
    /// <summary>إجمالي الربح = Revenue - Costs.</summary>
    public decimal GrossProfit { get; set; }
    /// <summary>هامش الربح % (0 إذا لا إيرادات).</summary>
    public decimal ProfitMarginPercent { get; set; }
    /// <summary>عدد القيود المربوطة بالمشروع (Posted، لها خطوط على Expense).</summary>
    public int CostEntryCount { get; set; }
}

// ===== Sprint 58 / DEC-163: Project Contract =====

public sealed class CreateContractRequest
{
    public string? ContractNumber { get; set; }
    public decimal ContractValue { get; set; }
    public decimal AdvancePercent { get; set; }
    public decimal RetentionPercent { get; set; }
    public int RetentionStartBilling { get; set; } = 1;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateContractRequest
{
    public string? ContractNumber { get; set; }
    public decimal ContractValue { get; set; }
    public decimal AdvancePercent { get; set; }
    public decimal RetentionPercent { get; set; }
    public int RetentionStartBilling { get; set; } = 1;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class ContractResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal ContractValue { get; set; }
    public decimal AdvancePercent { get; set; }
    public decimal RetentionPercent { get; set; }
    public int RetentionStartBilling { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

// ===== Sprint 58 / DEC-164: Progress Billing =====

public sealed class CreateBillingRequest
{
    public string BillingNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public decimal WorkCompletedPercent { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProgressBillingResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ContractId { get; set; }
    public string BillingNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
    public DateTime? PeriodFrom { get; set; }
    public DateTime? PeriodTo { get; set; }
    public decimal WorkCompletedPercent { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal AdvanceDeducted { get; set; }
    public decimal RetentionDeducted { get; set; }
    public decimal NetAmount { get; set; }
    /// <summary>1=Draft, 2=Invoiced, 3=Cancelled (BillingStatus enum int).</summary>
    public int Status { get; set; }
    public string StatusName => Status switch
    {
        1 => "مسودة",
        2 => "مُرحّل",
        3 => "ملغى",
        _ => "غير معروف"
    };
    public Guid? InvoiceId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Sprint 58 / DEC-164: Billing preview (يحسب الأرقام قبل الإنشاء).</summary>
public sealed class BillingPreviewResponse
{
    public decimal GrossAmount { get; set; }
    public decimal AdvanceDeducted { get; set; }
    public decimal RetentionDeducted { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PreviousMaxPercent { get; set; }
    public int NextBillingNumber { get; set; }
}

/// <summary>Sprint 58 / DEC-165: WIP (Work in Progress) — معيار محاسبي للمشاريع.
/// wip = totalCosts − totalBilledNet
/// wip > 0 = العمل جاري والفوترة متأخرة (costs exceed billed)
/// wip < 0 = فوترنا أكثر مما أنفقنا (billed exceeds costs)
/// wip = 0 = balanced
/// </summary>
public sealed class WipResponse
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    /// <summary>إجمالي التكاليف من journal_lines (posted JEs on Expense accounts).</summary>
    public decimal TotalCosts { get; set; }
    /// <summary>إجمالي المفوتر صافي (net_amount) من progress_billings INVOICED.</summary>
    public decimal TotalBilledNet { get; set; }
    /// <summary>إجمالي احتجاز الضمان المحتجز (retention_deducted من billings INVOICED).</summary>
    public decimal TotalRetentionHeld { get; set; }
    /// <summary>الفرق: totalCosts - totalBilledNet.</summary>
    public decimal Wip { get; set; }
    /// <summary>"COSTS_EXCEED_BILLED" | "BILLED_EXCEED_COSTS" | "BALANCED".</summary>
    public string Status { get; set; } = "BALANCED";
    public string StatusName => Status switch
    {
        "COSTS_EXCEED_BILLED" => "تكاليف جارية (الفوترة متأخرة)",
        "BILLED_EXCEED_COSTS" => "فوترة زائدة (مدفوع مقدماً)",
        "BALANCED" => "متوازن",
        _ => "غير معروف"
    };
}
