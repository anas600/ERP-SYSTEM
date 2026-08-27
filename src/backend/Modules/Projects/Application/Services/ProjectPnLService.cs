using Dapper;
using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IProjectPnLService
{
    Task<ProjectResult<ProjectPnLResponse>> GetPnLAsync(
        Guid projectId, DateTime? from, DateTime? to, CancellationToken ct);
}

/// <summary>
/// Sprint 57 / DEC-161: Project Profit & Loss.
///
/// يقرأ من نفس الـ schema بدون ما يضيف حقول جديدة:
/// - Revenue: مجموع total_amount على sales_invoices (Posted, is_deleted=false) المُعلّقة على المشروع.
/// - Costs: مجموع (debit - credit) على journal_lines حيث:
///   * journal_entries.project_id = X
///   * journal_entries.status = 2 (Posted)
///   * accounts.type = 5 (Expense)
///   مُجمّعة حسب account code للعرض المفصّل.
///
/// ملاحظة مهمة: التكاليف تُحسب من الـ journal_lines المرتبطة بقيود مربوطة بالمشروع،
/// وليس من sales_invoices أو vendor_bills مباشرة. السبب: كل فاتورة مرحلة (مبيعات أو
/// مشتريات) تُنشئ journal entry تلقائياً. الحساب من الـ JE يضمن دقة الـ P&L ويمنع
/// الـ double-counting.
/// </summary>
public sealed class ProjectPnLService : IProjectPnLService
{
    private readonly IProjectRepository _projects;
    private readonly IDbConnectionFactory _db;
    private readonly IProjectCostService _projectCosts; // Sprint 65 / DEC-233

    public ProjectPnLService(
        IProjectRepository projects,
        IDbConnectionFactory db,
        IProjectCostService projectCosts)
    {
        _projects = projects;
        _db = db;
        _projectCosts = projectCosts;
    }

    public async Task<ProjectResult<ProjectPnLResponse>> GetPnLAsync(
        Guid projectId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return ProjectResult<ProjectPnLResponse>.Fail("المشروع غير موجود.", ProjectErrorCode.NotFound);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 1) Revenue — مجموع sales_invoices المربوطة بالمشروع (Posted, not deleted)
        var revenueSql = @"
            SELECT COALESCE(SUM(si.total_amount), 0) AS TotalRevenue,
                   COUNT(*) AS InvoiceCount
            FROM sales_invoices si
            WHERE si.project_id = @ProjectId
              AND si.status = 'Posted'
              AND si.is_deleted = false
              AND (@From IS NULL OR si.invoice_date >= @From)
              AND (@To IS NULL OR si.invoice_date <= @To);";
        var revRow = await conn.QueryFirstOrDefaultAsync<(decimal TotalRevenue, int InvoiceCount)>(
            new CommandDefinition(revenueSql,
                new { ProjectId = projectId, From = from, To = to },
                cancellationToken: ct));

        // 2) Costs — من journal_lines على Expense accounts، مربوطة بـ journal_entries.project_id
        //    نستخدم (debit - credit) لأن Expense accounts normal balance = Debit.
        var costsSql = @"
            SELECT a.code AS AccountCode,
                   a.name AS AccountName,
                   COALESCE(SUM(jl.debit - jl.credit), 0) AS Amount,
                   COUNT(DISTINCT je.id) AS EntryCount
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE je.project_id = @ProjectId
              AND je.status = 2
              AND a.type = 5
              AND (@From IS NULL OR je.entry_date >= @From)
              AND (@To IS NULL OR je.entry_date <= @To)
              AND (jl.debit - jl.credit) > 0
            GROUP BY a.code, a.name
            ORDER BY a.code;";

        var costRows = (await conn.QueryAsync<(string AccountCode, string AccountName, decimal Amount, int EntryCount)>(
            new CommandDefinition(costsSql,
                new { ProjectId = projectId, From = from, To = to },
                cancellationToken: ct))).ToList();

        // 3) Sprint 65 / DEC-233: تكاليف المقاولين الفرعيين من sub_payments (Sprint 64 schema).
        //    قبل Sprint 64 merge = 0 (NoOpSubPaymentRepository). يُضاف إلى TotalCosts.
        decimal subcontractorCost = 0m;
        var subResult = await _projectCosts.GetSubcontractorCostAsync(projectId, ct);
        if (subResult.Succeeded)
            subcontractorCost = subResult.Value;

        var totalRevenue = revRow.TotalRevenue;
        var journalCosts = costRows.Sum(r => r.Amount);
        var totalCosts = journalCosts + subcontractorCost;
        var grossProfit = totalRevenue - totalCosts;
        var margin = totalRevenue > 0
            ? Math.Round(grossProfit / totalRevenue * 100, 2)
            : 0m;
        var entryCount = costRows.Sum(r => r.EntryCount);

        return ProjectResult<ProjectPnLResponse>.Ok(new ProjectPnLResponse
        {
            ProjectId = projectId,
            ProjectCode = project.Code,
            ProjectName = project.Name,
            From = from,
            To = to,
            TotalRevenue = totalRevenue,
            InvoiceCount = revRow.InvoiceCount,
            CostsByAccount = costRows.Select(r => new ProjectPnLLine
            {
                AccountCode = r.AccountCode,
                AccountName = r.AccountName,
                Amount = r.Amount
            }).ToList(),
            TotalCosts = totalCosts,
            SubcontractorCost = subcontractorCost,
            GrossProfit = grossProfit,
            ProfitMarginPercent = margin,
            CostEntryCount = entryCount,
        });
    }
}
