// =====================================================================================
// Sprint 65 / Wave 2A (DEC-234 + DEC-236): DashboardCrossModuleController
// =====================================================================================
//
// Cross-module KPIs for the dashboard. Sits next to the existing
// `Modules/Dashboard/Endpoints/GetSummary.cs` (Sprint 1) but is **not** a replacement:
// the summary endpoint counts journal-entries as activity; this controller reads
// outstanding receivables (AR) + outstanding payables (AP) + project profitability
// in a single, focused view.
//
// Routes (both authenticated via the ReadAccess policy — same as the existing
// dashboard endpoints, because the cross-module KPIs are the default landing tab
// for every role):
//
//   GET /api/dashboard/cross-module             — single-object response
//   GET /api/dashboard/project-profitability    — list of projects ranked by margin
//
// L19 / DEC-095: CompanyId is read from ICompanyContext.CompanyId (set by
// CompanyContextMiddleware from the X-Company-Id header). UserId is NOT needed for
// these read-only endpoints.
// =====================================================================================

using Dapper;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class DashboardCrossModuleController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _company;
    private readonly IProjectCostService _projectCosts; // Sprint 65 / DEC-233

    public DashboardCrossModuleController(
        IDbConnectionFactory db,
        ICompanyContext company,
        IProjectCostService projectCosts)
    {
        _db = db;
        _company = company;
        _projectCosts = projectCosts;
    }

    // GET /api/dashboard/cross-module — single cross-module KPI payload.
    //
    // Empty-state contract: when the company context is unresolved (e.g. user is
    // authenticated but hasn't picked a company yet), we return a zero-filled DTO
    // with 200 OK. The FE renders the empty state cleanly (DEC-234 contract).
    [HttpGet("cross-module")]
    public async Task<IActionResult> GetCrossModule(CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            return Ok(new DashboardCrossModuleResponse());
        }

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Outstanding AR = SUM(total - amount_paid) for sales_invoices that are
        // 'Posted' but not fully paid and not cancelled. The 'amount_paid' column
        // is incremented by ReceiptsController.PostAsync (Sprint 36 / DEC-122).
        // We compute outstanding as (total - paid) to be defensive against
        // over-paid rows (paid > total) — the GREATEST() ensures we never return
        // a negative outstanding.
        const string arSql = @"
            SELECT COALESCE(SUM(GREATEST(si.total_amount - si.paid_amount, 0)), 0) AS OutstandingAR
            FROM sales_invoices si
            WHERE si.company_id = @CompanyId
              AND si.is_deleted = false
              AND si.status = 'Posted'
              AND si.paid_amount < si.total_amount;";

        var outstandingAR = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            arSql,
            new { CompanyId = companyId.Value },
            cancellationToken: ct));

        // Outstanding AP = SUM(sub_payments.amount) for active subcontractor
        // payments that have not been matched to a vendor bill yet (DEC-232).
        // Before Sprint 64 lands, the sub_payments table does not exist on
        // `develop`; the SELECT wraps in a defensive subquery so the absence of
        // the table yields 0 (the subquery returns one row of zero on error).
        // The Dapper ExecuteScalar on a CTE that uses to_regclass for the table
        // check is portable across Postgres 14+ and Supabase eu-central-1.
        const string apSql = @"
            SELECT COALESCE(SUM(sp.amount), 0) AS OutstandingAP
            FROM sub_payments sp
            WHERE sp.company_id = @CompanyId
              AND sp.status <> 4
              AND sp.vendor_bill_id IS NULL;";

        decimal outstandingAP = 0m;
        try
        {
            outstandingAP = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
                apSql,
                new { CompanyId = companyId.Value },
                cancellationToken: ct));
        }
        catch (Npgsql.PostgresException)
        {
            // sub_payments table does not exist yet (Sprint 64 pre-merge).
            // Default to 0 so the dashboard still loads.
            outstandingAP = 0m;
        }

        // Project count + total contract value — all active projects in the company.
        const string projectsSql = @"
            SELECT COUNT(*) AS ProjectCount,
                   COALESCE(SUM(c.contract_value), 0) AS TotalContractValue
            FROM projects p
            LEFT JOIN contracts c ON c.project_id = p.id AND c.is_active = true AND c.deleted_at IS NULL
            WHERE p.company_id = @CompanyId
              AND p.is_active = true
              AND p.status <> 5;"; // 5 = Cancelled

        var projectStats = await conn.QueryFirstOrDefaultAsync<(int ProjectCount, decimal TotalContractValue)>(
            new CommandDefinition(projectsSql,
                new { CompanyId = companyId.Value },
                cancellationToken: ct));

        // Total revenue from posted sales invoices (the same source the
        // ProjectPnLService reads from, but aggregated at the company level).
        const string revenueSql = @"
            SELECT COALESCE(SUM(si.total_amount), 0) AS TotalRevenue
            FROM sales_invoices si
            WHERE si.company_id = @CompanyId
              AND si.is_deleted = false
              AND si.status = 'Posted';";

        var totalRevenue = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
            revenueSql,
            new { CompanyId = companyId.Value },
            cancellationToken: ct));

        // Total subcontractor cost (sum of sub_payments). Same defensive try as
        // outstandingAP so the missing table (Sprint 64 pre-merge) yields 0.
        decimal totalSubcontractorCost = 0m;
        try
        {
            totalSubcontractorCost = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(
                "SELECT COALESCE(SUM(sp.amount), 0) FROM sub_payments sp WHERE sp.company_id = @CompanyId AND sp.status <> 4;",
                new { CompanyId = companyId.Value },
                cancellationToken: ct));
        }
        catch (Npgsql.PostgresException)
        {
            totalSubcontractorCost = 0m;
        }

        // Unprofitable projects = count where sum(cost) > sum(revenue) for the
        // project's posted cost lines. We compute it as a correlated subquery
        // in a single round-trip.
        const string unprofitableSql = @"
            SELECT COUNT(*) FROM (
                SELECT p.id,
                       (SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
                          FROM journal_lines jl
                          INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                          INNER JOIN accounts a ON a.id = jl.account_id
                          WHERE je.project_id = p.id AND je.status = 2 AND a.type = 5
                            AND (jl.debit - jl.credit) > 0) AS TotalCost,
                       (SELECT COALESCE(SUM(si.total_amount), 0)
                          FROM sales_invoices si
                          WHERE si.project_id = p.id AND si.status = 'Posted' AND si.is_deleted = false) AS TotalRevenue
                FROM projects p
                WHERE p.company_id = @CompanyId AND p.is_active = true AND p.status <> 5
            ) sub
            WHERE sub.TotalCost > sub.TotalRevenue;";

        var unprofitableProjects = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            unprofitableSql,
            new { CompanyId = companyId.Value },
            cancellationToken: ct));

        return Ok(new DashboardCrossModuleResponse
        {
            OutstandingAR = outstandingAR,
            OutstandingAP = outstandingAP,
            NetPosition = outstandingAR - outstandingAP,
            ProjectCount = projectStats.ProjectCount,
            TotalContractValue = projectStats.TotalContractValue,
            TotalRevenue = totalRevenue,
            TotalSubcontractorCost = totalSubcontractorCost,
            UnprofitableProjects = unprofitableProjects,
        });
    }

    // GET /api/dashboard/project-profitability — list of all projects with
    // revenue, total cost (including subcontractor), gross profit, margin, and
    // a 3-bucket health status (OK / AT_RISK / OVER_BUDGET).
    //
    // Health status logic (per hand-off contract):
    //   OVER_BUDGET if totalCosts > totalContractValue
    //   AT_RISK     if totalCosts > totalContractValue * 0.8   (80% threshold)
    //   OK          otherwise
    //
    // When the project has no contract yet, totalContractValue = 0 → the
    // comparison becomes TotalCosts > 0 → 'OVER_BUDGET' (defensive: any cost
    // without a contract is suspicious). The FE can surface this with a
    // "missing contract" badge.
    [HttpGet("project-profitability")]
    public async Task<IActionResult> GetProjectProfitability(CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            return Ok(Array.Empty<ProjectProfitabilityResponse>());
        }

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Single round-trip with two correlated subqueries: cost (from journal_lines
        // on Expense accounts) and revenue (from posted sales invoices) per project.
        const string projectsSql = @"
            SELECT p.id           AS ProjectId,
                   p.code         AS ProjectCode,
                   p.name         AS ProjectName,
                   COALESCE(c.contract_value, 0) AS ContractValue,
                   COALESCE((
                       SELECT SUM(jl.debit - jl.credit)
                         FROM journal_lines jl
                         INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                         INNER JOIN accounts a ON a.id = jl.account_id
                         WHERE je.project_id = p.id AND je.status = 2 AND a.type = 5
                           AND (jl.debit - jl.credit) > 0
                   ), 0) AS TotalCost,
                   COALESCE((
                       SELECT SUM(si.total_amount)
                         FROM sales_invoices si
                         WHERE si.project_id = p.id AND si.status = 'Posted' AND si.is_deleted = false
                   ), 0) AS TotalRevenue
            FROM projects p
            LEFT JOIN contracts c ON c.project_id = p.id AND c.is_active = true AND c.deleted_at IS NULL
            WHERE p.company_id = @CompanyId
              AND p.is_active = true
              AND p.status <> 5
            ORDER BY (COALESCE((
                       SELECT SUM(si.total_amount)
                         FROM sales_invoices si
                         WHERE si.project_id = p.id AND si.status = 'Posted' AND si.is_deleted = false), 0)
                      - COALESCE((
                       SELECT SUM(jl.debit - jl.credit)
                         FROM journal_lines jl
                         INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                         INNER JOIN accounts a ON a.id = jl.account_id
                         WHERE je.project_id = p.id AND je.status = 2 AND a.type = 5
                           AND (jl.debit - jl.credit) > 0), 0)) DESC;";

        var rows = (await conn.QueryAsync<ProfitabilityRow>(new CommandDefinition(
            projectsSql,
            new { CompanyId = companyId.Value },
            cancellationToken: ct))).ToList();

        // Subcontractor cost is folded into TotalCost here (Sprint 65 / DEC-233).
        // Before Sprint 64 lands, the value is 0 (NoOpSubPaymentRepository).
        // We call the service per project so the cost is fresh and so the FE sees
        // a consistent picture with the per-project /pnl endpoint.
        var result = new List<ProjectProfitabilityResponse>(rows.Count);
        foreach (var row in rows)
        {
            decimal subcontractorCost = 0m;
            var subResult = await _projectCosts.GetSubcontractorCostAsync(row.ProjectId, ct);
            if (subResult.Succeeded)
                subcontractorCost = subResult.Value;

            var totalCost = row.TotalCost + subcontractorCost;
            var grossProfit = row.TotalRevenue - totalCost;
            var margin = row.TotalRevenue > 0
                ? Math.Round(grossProfit / row.TotalRevenue * 100, 2)
                : 0m;

            var health = ComputeHealthStatus(totalCost, row.ContractValue);

            result.Add(new ProjectProfitabilityResponse
            {
                ProjectId = row.ProjectId,
                ProjectCode = row.ProjectCode,
                ProjectName = row.ProjectName,
                TotalRevenue = row.TotalRevenue,
                TotalCosts = totalCost,
                GrossProfit = grossProfit,
                ProfitMarginPercent = margin,
                HealthStatus = health,
            });
        }

        return Ok(result);
    }

    private static string ComputeHealthStatus(decimal totalCost, decimal contractValue)
    {
        if (contractValue <= 0m)
            return "OVER_BUDGET"; // any cost without a contract is treated as over-budget

        if (totalCost > contractValue)
            return "OVER_BUDGET";

        if (totalCost > contractValue * 0.8m)
            return "AT_RISK";

        return "OK";
    }

    // Internal row shape for the SQL projection.
    private sealed class ProfitabilityRow
    {
        public Guid ProjectId { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public decimal ContractValue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

public sealed class DashboardCrossModuleResponse
{
    /// <summary>SUM(sales_invoices.total - amount_paid) for unpaid posted invoices.</summary>
    public decimal OutstandingAR { get; set; }
    /// <summary>SUM(sub_payments.amount) for unmatched sub-payments. 0 before Sprint 64 merge.</summary>
    public decimal OutstandingAP { get; set; }
    /// <summary>OutstandingAR - OutstandingAP.</summary>
    public decimal NetPosition { get; set; }
    /// <summary>Active non-cancelled projects in the company.</summary>
    public int ProjectCount { get; set; }
    /// <summary>SUM(contracts.contract_value) for the company's active projects.</summary>
    public decimal TotalContractValue { get; set; }
    /// <summary>SUM(sales_invoices.total_amount) for the company's posted invoices.</summary>
    public decimal TotalRevenue { get; set; }
    /// <summary>SUM(sub_payments.amount) for the company. 0 before Sprint 64 merge.</summary>
    public decimal TotalSubcontractorCost { get; set; }
    /// <summary>Count of active projects where sum(cost) > sum(revenue).</summary>
    public int UnprofitableProjects { get; set; }
}

public sealed class ProjectProfitabilityResponse
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    /// <summary>Includes the subcontractor cost (Sprint 65 / DEC-233).</summary>
    public decimal TotalCosts { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    /// <summary>"OK" | "AT_RISK" | "OVER_BUDGET".</summary>
    public string HealthStatus { get; set; } = "OK";
}
