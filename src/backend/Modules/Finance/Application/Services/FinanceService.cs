// Sprint 11 T2 (BE Jimi) — FinanceService.
//
// New demo-grade methods for the Holding dashboard / recent transactions /
// flat Chart of Accounts list. These back the new FE endpoints under
// /api/holdings/dashboard, /api/transactions/recent, and /api/accounts.
//
// The methods are intentionally consolidated into one service (rather than
// scattered across ChartOfAccountsService / JournalEntryService) because:
//   - They all serve the same "demo polish" surface, the Holding dashboard
//     page on the demo.
//   - They all need a similar empty-state contract: 200 OK with a default
//     (zeroes / empty list), never 401, so the FE can render the demo even
//     when the holding-level context is not resolved.
//   - They all run read-only aggregation queries against the OLTP DB
//     (no transactions, no locks).
//
// Multi-company scope:
//   - Holding-level queries (the consolidated dashboard) are NOT scoped to a
//     single company; they aggregate across all sub-companies of the
//     Holding (the row where is_group=true AND parent_company_id IS NULL).
//   - Per-company queries (the CoA list, the recent transactions) are
//     scoped to the active company via ICompanyContext.
//
// Article 3: company_id only. No tenant_id anywhere.
//
// Empty-state contract:
//   - When the Holding is not seeded yet, return a default (zeroes / empty
//     list), not 404. The FE renders the empty state cleanly.

using Dapper;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IFinanceService
{
    Task<FinanceResult<HoldingDashboardDto>> GetConsolidatedKpisAsync(CancellationToken ct);
    Task<FinanceResult<IReadOnlyList<TransactionDto>>> GetRecentTransactionsAsync(int limit, CancellationToken ct);
    Task<FinanceResult<IReadOnlyList<AccountDto>>> ListAccountsAsync(bool includeInactive, CancellationToken ct);
    Task<FinanceResult<AccountDto>> GetAccountByIdAsync(Guid id, CancellationToken ct);
}

public sealed class FinanceService : IFinanceService
{
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _company;
    private readonly IAccountRepository _accounts;
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(
        IDbConnectionFactory db,
        ICompanyContext company,
        IAccountRepository accounts,
        ILogger<FinanceService> logger)
    {
        _db = db;
        _company = company;
        _accounts = accounts;
        _logger = logger;
    }

    // Holding-level consolidated KPIs. Aggregates across all sub-companies.
    // "Recent transactions" is a side query (last 10 journal lines). Currency
    // defaults to LYD (the demo currency per the seed).
    public async Task<FinanceResult<HoldingDashboardDto>> GetConsolidatedKpisAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // Find the Holding: is_group=true AND parent_company_id IS NULL.
        var holdingId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            @"SELECT id FROM companies
              WHERE is_group = true
                AND parent_company_id IS NULL
              ORDER BY code
              LIMIT 1", cancellationToken: ct));

        // Empty state: no Holding yet, return a zero-filled dashboard. The FE
        // renders the "no holding seeded yet" hint cleanly.
        if (holdingId == null)
        {
            _logger.LogDebug("Holding dashboard: no Holding found, returning empty KPIs");
            return FinanceResult<HoldingDashboardDto>.Ok(new HoldingDashboardDto
            {
                TotalRevenue = 0m,
                TotalExpenses = 0m,
                NetProfit = 0m,
                CompanyCount = 0,
                EmployeeCount = 0,
                TreasuryBalance = 0m,
                RecentTransactions = Array.Empty<TransactionDto>(),
                AsOf = DateTime.UtcNow,
                Currency = "LYD",
            });
        }

        // 1) Revenue = sum of posted sales invoices across all sub-companies.
        //    The Holding itself has no invoices (it's a group), so we filter
        //    on parent_company_id IS NOT NULL (= subsidiaries only).
        //    Status filter matches DashboardChartService: Posted / Partial / Paid.
        //    NOTE: the demo seed uses an integer status (2 = posted) in some
        //    tables and a string status in others. The SalesInvoice model
        //    uses string enum (per the Sprints 1-2 work), so we filter on
        //    the string set. If the table is empty (no sales yet), the sum
        //    is 0 by default.
        const string revenueSql = @"
            SELECT COALESCE(SUM(si.total_amount), 0)::numeric(18,4)
            FROM sales_invoices si
            INNER JOIN companies c ON c.id = si.company_id
            WHERE c.parent_company_id IS NOT NULL
              AND si.status IN ('Posted', 'Partial', 'Paid')";

        // 2) Expenses = sum of posted journal lines on accounts of type=5
        //    (Expense). Same query as DashboardChartService, but at the
        //    Holding level (no per-company filter; c.parent_company_id IS NOT NULL).
        const string expenseSql = @"
            SELECT COALESCE(SUM(jl.debit) - SUM(jl.credit), 0)::numeric(18,4)
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a          ON a.id = jl.account_id
            INNER JOIN companies c         ON c.id = je.company_id
            WHERE c.parent_company_id IS NOT NULL
              AND a.type = 5
              AND je.status = 2";

        // 3) Company count = number of sub-companies (parent_company_id IS NOT NULL).
        const string companyCountSql = @"
            SELECT COUNT(*)::int FROM companies
            WHERE parent_company_id IS NOT NULL AND is_active = true";

        // 4) Employee count = number of active employees across all sub-companies.
        //    The employees table has a company_id; the Holding itself has no
        //    employees so we don't need an explicit parent_company_id filter
        //    (the Holding's employee_count is always 0 by definition).
        const string employeeCountSql = @"
            SELECT COUNT(*)::int FROM employees
            WHERE is_active = true AND termination_date IS NULL";

        // 5) Treasury balance = sum of bank_accounts.balance across all
        //    sub-companies. If the bank_accounts table is missing (Phase 1
        //    deployments), the query returns NULL → 0 by COALESCE.
        //    We don't fail the whole dashboard if this table is missing.
        decimal treasuryBalance = 0m;
        try
        {
            treasuryBalance = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(@"
                SELECT COALESCE(SUM(b.balance), 0)::numeric(18,4)
                FROM bank_accounts b
                INNER JOIN companies c ON c.id = b.company_id
                WHERE c.parent_company_id IS NOT NULL", cancellationToken: ct)) ?? 0m;
        }
        catch (Exception ex)
        {
            // bank_accounts table may not exist on older deployments; log + continue.
            _logger.LogDebug(ex, "Holding dashboard: bank_accounts query failed (table may not exist); defaulting to 0");
        }

        // Run the 4 main counts in a single round-trip each (sequential;
        // the dashboard is page-mount latency, not hot path, and FakeDb
        // doesn't parallelize).
        var totalRevenue = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(revenueSql, cancellationToken: ct));
        var totalExpenses = await conn.ExecuteScalarAsync<decimal>(new CommandDefinition(expenseSql, cancellationToken: ct));
        var companyCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(companyCountSql, cancellationToken: ct));
        var employeeCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(employeeCountSql, cancellationToken: ct));

        // 6) Recent transactions = last 10 journal lines across all sub-companies,
        //    joined to accounts for the display code/name. The FE uses this
        //    on the Holding dashboard "recent activity" card.
        const string recentSql = @"
            SELECT jl.id               AS Id,
                   jl.company_id       AS CompanyId,
                   jl.account_id       AS AccountId,
                   a.code              AS AccountCode,
                   a.name              AS AccountName,
                   jl.debit            AS Debit,
                   jl.credit           AS Credit,
                   COALESCE(jl.description, '') AS Description,
                   jl.created_at       AS CreatedAt,
                   je.entry_number     AS Reference
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a          ON a.id = jl.account_id
            INNER JOIN companies c         ON c.id = jl.company_id
            WHERE c.parent_company_id IS NOT NULL
            ORDER BY jl.created_at DESC
            LIMIT 10";

        var recent = (await conn.QueryAsync<TransactionDto>(new CommandDefinition(recentSql, cancellationToken: ct))).AsList();

        return FinanceResult<HoldingDashboardDto>.Ok(new HoldingDashboardDto
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            NetProfit = totalRevenue - totalExpenses,
            CompanyCount = companyCount,
            EmployeeCount = employeeCount,
            TreasuryBalance = treasuryBalance,
            RecentTransactions = recent,
            AsOf = DateTime.UtcNow,
            Currency = "LYD",
        });
    }

    // Recent transactions across the active company (per-company, not
    // Holding-level). Used by the demo /transactions page.
    // "limit" is clamped to [1, 200] to protect the DB.
    public async Task<FinanceResult<IReadOnlyList<TransactionDto>>> GetRecentTransactionsAsync(int limit, CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            _logger.LogDebug("Recent transactions: no resolved company");
            return FinanceResult<IReadOnlyList<TransactionDto>>.Ok(Array.Empty<TransactionDto>());
        }

        var cap = limit <= 0 ? 20 : Math.Min(limit, 200);

        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT jl.id               AS Id,
                   jl.company_id       AS CompanyId,
                   jl.account_id       AS AccountId,
                   a.code              AS AccountCode,
                   a.name              AS AccountName,
                   jl.debit            AS Debit,
                   jl.credit           AS Credit,
                   COALESCE(jl.description, '') AS Description,
                   jl.created_at       AS CreatedAt,
                   je.entry_number     AS Reference
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a          ON a.id = jl.account_id
            WHERE jl.company_id = @CompanyId
            ORDER BY jl.created_at DESC
            LIMIT @Limit";

        var rows = (await conn.QueryAsync<TransactionDto>(new CommandDefinition(
            sql, new { CompanyId = companyId.Value, Limit = cap }, cancellationToken: ct))).AsList();
        return FinanceResult<IReadOnlyList<TransactionDto>>.Ok(rows);
    }

    // Flat Chart of Accounts list (per-company). String enums for the
    // new demo contract.
    public async Task<FinanceResult<IReadOnlyList<AccountDto>>> ListAccountsAsync(bool includeInactive, CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        IReadOnlyList<Account> accounts;
        if (companyId.HasValue)
        {
            accounts = await _accounts.ListByCompanyAsync(companyId, ct);
        }
        else
        {
            // No resolved company: return the empty list. The FE renders
            // the empty state.
            return FinanceResult<IReadOnlyList<AccountDto>>.Ok(Array.Empty<AccountDto>());
        }

        if (!includeInactive)
        {
            accounts = accounts.Where(a => a.IsActive).ToList();
        }
        return FinanceResult<IReadOnlyList<AccountDto>>.Ok(accounts.Select(MapToAccountDto).ToList());
    }

    public async Task<FinanceResult<AccountDto>> GetAccountByIdAsync(Guid id, CancellationToken ct)
    {
        var acc = await _accounts.GetByIdAsync(id, ct);
        if (acc == null)
        {
            return FinanceResult<AccountDto>.Fail("الحساب غير موجود.", FinanceErrorCode.NotFound);
        }
        return FinanceResult<AccountDto>.Ok(MapToAccountDto(acc));
    }

    // Map the entity to the demo DTO. String enum mapping follows the
    // AccountType / NormalBalance enums in Finance/Entities/Account.cs:
    //   AccountType:        Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5
    //   NormalBalance:      Debit=1, Credit=2
    private static AccountDto MapToAccountDto(Account a) => new()
    {
        Id = a.Id,
        CompanyId = a.CompanyId,
        Code = a.Code,
        Name = a.Name,
        Description = a.Description,
        Type = a.Type switch
        {
            AccountType.Asset => "Asset",
            AccountType.Liability => "Liability",
            AccountType.Equity => "Equity",
            AccountType.Revenue => "Revenue",
            AccountType.Expense => "Expense",
            _ => "Asset",
        },
        NormalBalance = a.NormalBalance switch
        {
            NormalBalance.Debit => "Debit",
            NormalBalance.Credit => "Credit",
            _ => "Debit",
        },
        ParentAccountId = a.ParentAccountId,
        IsPostable = a.IsPostable,
        IsActive = a.IsActive,
    };
}
