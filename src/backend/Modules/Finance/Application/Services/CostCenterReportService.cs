using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface ICostCenterReportService
{
    Task<CostCenterPerformanceReport> GetAsync(Guid companyId, DateTime? from, DateTime? to, CancellationToken ct);
}

public sealed class CostCenterReportService : ICostCenterReportService
{
    private readonly IDbConnectionFactory _db;
    public CostCenterReportService(IDbConnectionFactory db) => _db = db;

    public async Task<CostCenterPerformanceReport> GetAsync(Guid companyId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Get all cost centers for this company
        const string ccSql = @"
            SELECT id, code, name FROM cost_centers
            WHERE company_id = @CompanyId AND is_active = true
            ORDER BY code";
        var costCenters = (await conn.QueryAsync<CostCenterSummary>(new CommandDefinition(ccSql,
            new { CompanyId = companyId }, cancellationToken: ct))).AsList();

        // For each cost center, sum revenue and expenses from journal lines
        const string metricsSql = @"
            SELECT
              COALESCE(SUM(CASE WHEN a.type = 4 THEN jl.credit - jl.debit ELSE 0 END), 0) AS revenue,
              COALESCE(SUM(CASE WHEN a.type = 5 THEN jl.debit - jl.credit ELSE 0 END), 0) AS expense
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.cost_center_id = @CostCenterId
              AND je.company_id = @CompanyId
              AND je.status = 2
              AND (@From::timestamptz IS NULL OR je.entry_date >= @From)
              AND (@To::timestamptz IS NULL OR je.entry_date <= @To)";

        var rows = new List<CostCenterPerformanceRow>();
        foreach (var cc in costCenters)
        {
            var m = await conn.QueryFirstAsync<(decimal revenue, decimal expense)>(new CommandDefinition(metricsSql,
                new { CostCenterId = cc.id, CompanyId = companyId, From = from, To = to }, cancellationToken: ct));
            rows.Add(new CostCenterPerformanceRow
            {
                CostCenterId = cc.id,
                CostCenterCode = cc.code,
                CostCenterName = cc.name,
                Revenue = m.revenue,
                Expense = m.expense
            });
        }

        return new CostCenterPerformanceReport
        {
            From = from,
            To = to,
            TotalRevenue = rows.Sum(r => r.Revenue),
            TotalExpense = rows.Sum(r => r.Expense),
            Rows = rows
        };
    }

    private sealed class CostCenterSummary
    {
        public Guid id { get; set; }
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }
}
