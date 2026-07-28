// Sprint 1 (T1 / Block A) — Dashboard summary service.
//
// Returns 4 KPIs for the dashboard landing page:
//   - companies     : number of companies the current user has access to
//                     (rows in user_companies for the current user_id)
//   - users         : number of users in the current company
//                     (rows in user_companies for the current company_id)
//   - activities_today : activity_log rows for the current user/company where
//                        created_at >= today (UTC date boundary)
//   - transactions  : journal_entries for the current company
//                     (all statuses — drafts, posted, reversed — gives a
//                     "how much activity has the company seen" signal)
//
// Why a service and not a controller-with-Dapper directly:
// - Mirrors the existing FinanceReportService / InventoryReportService pattern
// - Keeps the controller thin and makes the queries unit-testable via
//   the FakeDbConnectionFactory in the test project.
// - Allows the same payload to be reused by future endpoints (e.g. the
//   Holding dashboard in Sprint 2+).
//
// All four queries go through IDbConnectionFactory.CreateOltpConnectionAsync
// (no DbContext / EF) and use the multi-company scope (company_id only,
// never tenant_id) per Constitution Article 3.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Dashboard.Application.Services;

public interface IDashboardSummaryService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct);
}

public sealed class DashboardSummary
{
    public int Companies { get; set; }
    public int Users { get; set; }
    public int ActivitiesToday { get; set; }
    public int Transactions { get; set; }
}

public sealed class DashboardSummaryService : IDashboardSummaryService
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _company;
    private readonly ILogger<DashboardSummaryService> _logger;

    public DashboardSummaryService(
        IDbConnectionFactory db,
        ICompanyContext company,
        ILogger<DashboardSummaryService> logger)
    {
        _db = db;
        _company = company;
        _logger = logger;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken ct)
    {
        var userId = _company.UserId;
        var companyId = _company.CompanyId;

        // Empty summary is the safe default when no company is resolved.
        // The controller returns 200 in that case (the FE handles the empty
        // state with a "select a company" hint), rather than 401.
        if (companyId == null || userId == null)
        {
            _logger.LogDebug("Dashboard summary called with no resolved company/user (company={Cid} user={Uid})",
                companyId, userId);
            return new DashboardSummary();
        }

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 4 small COUNTs in one connection (sequential, not parallel — keeps
        // the FakeDbConnectionFactory test path simple and the connection
        // budget at 1). 4 round-trips is acceptable for a dashboard load
        // that runs once per page-mount; if it becomes hot we can collapse
        // to 1 query with sub-selects later.
        var companies = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1) FROM user_companies WHERE user_id = @UserId",
            new { UserId = userId.Value }, cancellationToken: ct));

        var users = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1) FROM user_companies WHERE company_id = @CompanyId",
            new { CompanyId = companyId.Value }, cancellationToken: ct));

        // "Today" = UTC date boundary. The activity_log.created_at column is
        // timestamptz, so >= date_trunc('day', NOW() AT TIME ZONE 'UTC')
        // matches the convention used in Phase 6 for date-based activity
        // reports. We compare against NOW() at the call site, not a
        // parameter, so the result is stable for a single request.
        var activitiesToday = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1) FROM activity_log
              WHERE user_id = @UserId
                AND company_id = @CompanyId
                AND created_at >= date_trunc('day', NOW() AT TIME ZONE 'UTC')",
            new { UserId = userId.Value, CompanyId = companyId.Value },
            cancellationToken: ct));

        // "Transactions" = all journal entries for the current company,
        // regardless of status. Drafts, posted, and reversed entries all
        // count — the KPI is "how much activity has the company seen",
        // not "how much has been finalised". The FinanceReportService
        // already filters to status=2 (posted) for accounting reports;
        // the dashboard intentionally uses a broader definition.
        var transactions = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1) FROM journal_entries WHERE company_id = @CompanyId",
            new { CompanyId = companyId.Value }, cancellationToken: ct));

        return new DashboardSummary
        {
            Companies = companies,
            Users = users,
            ActivitiesToday = activitiesToday,
            Transactions = transactions,
        };
    }
}
