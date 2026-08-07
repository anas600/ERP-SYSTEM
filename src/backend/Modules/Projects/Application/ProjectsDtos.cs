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
