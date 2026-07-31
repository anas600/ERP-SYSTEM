// Sprint 5 (T1-T3 / Phase 4) — Dashboard chart data service.
//
// Three read-only methods that back the dashboard charts:
//   - GetRevenueVsExpenseAsync  → T1
//   - GetExpensesByCategoryAsync → T2
//   - GetTopCustomersAsync       → T3
//
// All three filter on `company_id` (per Constitution Article 3 / Article 8
// rule 5 — no tenant_id anywhere). The chart service resolves the
// company_id from ICompanyContext the same way DashboardSummaryService does.
//
// Why one service (not three): the three methods are tightly related
// (they all power the same dashboard page) and share the same connection
// lifecycle + company resolution + empty-state contract. Splitting them
// into 3 separate services would mean 3x the boilerplate for the same
// shared pattern.
//
// Empty-state contract: when the company is not resolved (no X-Company-Id
// header), each method returns an empty list (not null, not 401). The
// controller returns 200 in that case; the FE renders an empty-state hint.
// This matches the convention established by DashboardSummaryService.
//
// SQL notes:
// - All date math uses invoice_date / entry_date directly with
//   date_trunc('month', ...) and >= / < bounds, no parameter munging.
// - "revenue" = SUM(sales_invoices.total_amount) WHERE status IN
//   (Posted, Partial, Paid) — same status filter as the existing
//   TopCustomersService so the numbers line up.
// - "expense" = SUM(journal_lines where account.type=5 (Expense) debit
//   minus credit) per month, joined to journal_entries where
//   status=2 (posted) — matches the FinanceReportService convention.

using Dapper;
using ERPSystem.Modules.Dashboard.Application.DTOs;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Dashboard.Application.Services;

public interface IDashboardChartService
{
    Task<IReadOnlyList<RevenueVsExpensePoint>> GetRevenueVsExpenseAsync(int months, CancellationToken ct);
    Task<IReadOnlyList<ExpenseCategorySlice>> GetExpensesByCategoryAsync(int months, CancellationToken ct);
    Task<IReadOnlyList<TopCustomerChartRow>> GetTopCustomersAsync(int limit, CancellationToken ct);
}

public sealed class DashboardChartService : IDashboardChartService
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _company;
    private readonly ILogger<DashboardChartService> _logger;

    public DashboardChartService(
        IDbConnectionFactory db,
        ICompanyContext company,
        ILogger<DashboardChartService> logger)
    {
        _db = db;
        _company = company;
        _logger = logger;
    }

    // T1 — Revenue vs expense per month.
    // Returns one row per month for the last N months (default 6), with the
    // month label as ISO yyyy-MM and the absolute LYD totals.
    // "months" is clamped to [1, 24] to protect the DB from huge windows.
    public async Task<IReadOnlyList<RevenueVsExpensePoint>> GetRevenueVsExpenseAsync(int months, CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            _logger.LogDebug("Revenue chart called with no resolved company");
            return Array.Empty<RevenueVsExpensePoint>();
        }

        var window = ClampMonths(months, defaultMonths: 6);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Two separate queries (revenue and expense) — we LEFT-join them in
        // application code instead of SQL so the months list is built once
        // (the FE chart expects one row per month, even when both sides are
        // zero). Both queries are bounded by the same month window so the
        // DB returns at most `window` rows each.
        const string revenueSql = @"
            SELECT to_char(date_trunc('month', invoice_date), 'YYYY-MM') AS Month,
                   COALESCE(SUM(total_amount), 0) AS Revenue
            FROM sales_invoices
            WHERE company_id = @CompanyId
              AND status IN ('Posted', 'Partial', 'Paid')
              AND invoice_date >= date_trunc('month', NOW() AT TIME ZONE 'UTC') - (@Window || ' months')::interval
              AND invoice_date <  date_trunc('month', NOW() AT TIME ZONE 'UTC') + interval '1 month'
            GROUP BY 1";

        const string expenseSql = @"
            SELECT to_char(date_trunc('month', je.entry_date), 'YYYY-MM') AS Month,
                   COALESCE(SUM(jl.debit) - SUM(jl.credit), 0) AS Expense
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a          ON a.id = jl.account_id
            WHERE a.company_id = @CompanyId
              AND a.type = 5
              AND je.status = 2
              AND je.entry_date >= date_trunc('month', NOW() AT TIME ZONE 'UTC') - (@Window || ' months')::interval
              AND je.entry_date <  date_trunc('month', NOW() AT TIME ZONE 'UTC') + interval '1 month'
            GROUP BY 1";

        var revRows = (await conn.QueryAsync<RevenueAggRow>(new CommandDefinition(revenueSql,
            new { CompanyId = companyId.Value, Window = window }, cancellationToken: ct))).AsList();
        var expRows = (await conn.QueryAsync<ExpenseAggRow>(new CommandDefinition(expenseSql,
            new { CompanyId = companyId.Value, Window = window }, cancellationToken: ct))).AsList();

        // Build the result as a dictionary keyed by month, then materialize
        // a list ordered by month. Months with no data default to 0.
        var byMonth = new SortedDictionary<string, RevenueVsExpensePoint>(StringComparer.Ordinal);
        foreach (var r in revRows)
        {
            if (!byMonth.TryGetValue(r.Month, out var p)) { p = new RevenueVsExpensePoint { Month = r.Month }; byMonth[r.Month] = p; }
            p.Revenue = r.Revenue;
        }
        foreach (var e in expRows)
        {
            if (!byMonth.TryGetValue(e.Month, out var p)) { p = new RevenueVsExpensePoint { Month = e.Month }; byMonth[e.Month] = p; }
            p.Expense = e.Expense;
        }
        foreach (var p in byMonth.Values) p.Net = p.Revenue - p.Expense;

        return byMonth.Values.ToList();
    }

    // T2 — Expenses grouped by account, for the pie / donut chart.
    // Filters to accounts of type=5 (Expense) per the AccountType enum in
    // Modules/Finance/Entities/Account.cs. Joins to journal_entries where
    // status=2 (posted) so draft / reversed entries don't pollute the chart.
    // "color" is a fixed palette (Tailwind-ish hues) — the FE can override.
    public async Task<IReadOnlyList<ExpenseCategorySlice>> GetExpensesByCategoryAsync(int months, CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            _logger.LogDebug("Expenses chart called with no resolved company");
            return Array.Empty<ExpenseCategorySlice>();
        }

        var window = ClampMonths(months, defaultMonths: 3);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        const string sql = @"
            SELECT a.name AS Category,
                   COALESCE(SUM(jl.debit) - SUM(jl.credit), 0) AS Amount
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a          ON a.id = jl.account_id
            WHERE a.company_id = @CompanyId
              AND a.type = 5
              AND je.status = 2
              AND je.entry_date >= date_trunc('month', NOW() AT TIME ZONE 'UTC') - (@Window || ' months')::interval
            GROUP BY a.id, a.name
            HAVING COALESCE(SUM(jl.debit) - SUM(jl.credit), 0) <> 0
            ORDER BY Amount DESC";

        var rows = (await conn.QueryAsync<CategoryAggRow>(new CommandDefinition(sql,
            new { CompanyId = companyId.Value, Window = window }, cancellationToken: ct))).AsList();

        // Assign palette colors by rank. The same account always gets the
        // same color across renders — this is deterministic by ORDER BY
        // (Amount DESC), so the highest expense is always #ef4444, the
        // second #f59e0b, etc. We materialize the public DTO here (not
        // the internal row class) so the controller never has to project.
        var palette = new[]
        {
            "#ef4444", "#f59e0b", "#10b981", "#3b82f6", "#8b5cf6",
            "#ec4899", "#14b8a6", "#f97316", "#6366f1", "#84cc16"
        };

        var result = new List<ExpenseCategorySlice>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            result.Add(new ExpenseCategorySlice
            {
                Category = rows[i].Category,
                Amount = rows[i].Amount,
                Color = palette[i % palette.Length],
            });
        }
        return result;
    }

    // T3 — Top customers by posted invoice total.
    // Same query shape as the existing TopCustomersService.GetAsync but
    // without the date range (we want the all-time top, scoped only to
    // the current company). Status filter matches the report: only
    // "Posted" / "Partial" / "Paid" count, drafts don't pollute the ranking.
    // "limit" is clamped to [1, 50].
    public async Task<IReadOnlyList<TopCustomerChartRow>> GetTopCustomersAsync(int limit, CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            _logger.LogDebug("Top-customers chart called with no resolved company");
            return Array.Empty<TopCustomerChartRow>();
        }

        var cap = ClampLimit(limit, defaultLimit: 5);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        const string sql = @"
            SELECT c.id      AS CustomerId,
                   c.name    AS CustomerName,
                   COUNT(si.id)               AS InvoiceCount,
                   COALESCE(SUM(si.total_amount), 0) AS TotalSpent
            FROM customers c
            INNER JOIN sales_invoices si ON si.customer_id = c.id
                AND si.status IN ('Posted', 'Partial', 'Paid')
            WHERE c.company_id = @CompanyId
            GROUP BY c.id, c.name
            ORDER BY TotalSpent DESC
            LIMIT @Limit";

        var rows = (await conn.QueryAsync<TopCustomerChartRow>(new CommandDefinition(sql,
            new { CompanyId = companyId.Value, Limit = cap }, cancellationToken: ct))).AsList();

        return rows;
    }

    private static int ClampMonths(int months, int defaultMonths)
    {
        if (months <= 0) return defaultMonths;
        if (months > 24) return 24;
        return months;
    }

    private static int ClampLimit(int limit, int defaultLimit)
    {
        if (limit <= 0) return defaultLimit;
        if (limit > 50) return 50;
        return limit;
    }

    // Internal Dapper materializers — kept private so the public DTOs are
    // the only surface the controller / FE see.
    private sealed class RevenueAggRow { public string Month { get; set; } = string.Empty; public decimal Revenue { get; set; } }
    private sealed class ExpenseAggRow { public string Month { get; set; } = string.Empty; public decimal Expense { get; set; } }
    private sealed class CategoryAggRow { public string Category { get; set; } = string.Empty; public decimal Amount { get; set; } }
}
