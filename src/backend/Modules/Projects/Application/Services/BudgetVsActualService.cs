using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IBudgetVsActualService
{
    Task<BudgetVsActualReport> GetAsync(Guid companyId, Guid? projectId, DateTime from, DateTime to, CancellationToken ct);
}

public sealed class BudgetVsActualService : IBudgetVsActualService
{
    private readonly IDbConnectionFactory _db;
    public BudgetVsActualService(IDbConnectionFactory db) => _db = db;

    public async Task<BudgetVsActualReport> GetAsync(Guid companyId, Guid? projectId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Sum project expenses from journal lines linked via cost_center_id of the project
        // For each project: budget = projects.budget, actual = SUM(journal_lines where cost_center = project.cost_center AND type=5 (expense))
        const string sql = @"
            SELECT p.id AS ProjectId, p.name AS ProjectName, p.budget AS Budget,
                   COALESCE((
                     SELECT SUM(jl.debit - jl.credit)
                     FROM journal_lines jl
                     INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                     INNER JOIN accounts a ON a.id = jl.account_id
                     WHERE jl.cost_center_id = p.cost_center_id
                       AND je.company_id = @CompanyId
                       AND je.status = 2
                       AND je.entry_date BETWEEN @From AND @To
                       AND a.type = 5
                   ), 0) AS Actual
            FROM projects p
            WHERE p.company_id = @CompanyId
              AND (@ProjectId::uuid IS NULL OR p.id = @ProjectId)
            ORDER BY p.name";

        var rows = (await conn.QueryAsync<BudgetVsActualRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, ProjectId = projectId, From = from, To = to }, cancellationToken: ct))).AsList();

        return new BudgetVsActualReport
        {
            ProjectId = projectId,
            From = from,
            To = to,
            TotalBudget = rows.Sum(r => r.Budget),
            TotalActual = rows.Sum(r => r.Actual),
            Rows = rows
        };
    }
}
