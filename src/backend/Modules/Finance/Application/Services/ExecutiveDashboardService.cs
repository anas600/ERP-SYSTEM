// Sprint 57 (DEC-152..154) — Path C.2: Dashboards + Charts
//
// خدمة تجمع KPIs و chart data للوحة Executive Dashboard:
// - Revenue YTD, Expenses YTD, Net Income YTD
// - Cash position, AR total, AP total
// - Revenue trend (12 months)
// - Top customers (top 5)
// - AR/AP aging breakdown
// - Expense breakdown by account

using Dapper;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IExecutiveDashboardService
{
    Task<ExecutiveDashboardResponse> GetAsync(Guid companyId, CancellationToken ct);
}

public sealed class ExecutiveDashboardResponse
{
    public DateTime AsOfDate { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public ExecutiveKpis Kpis { get; set; } = new();
    public List<MonthlySeriesPoint> RevenueTrend12Months { get; set; } = new();
    public List<ChartDataPoint> TopCustomers { get; set; } = new();
    public List<ChartDataPoint> ExpenseBreakdown { get; set; } = new();
    public AgingChartData ArAgingBuckets { get; set; } = new();
    public AgingChartData ApAgingBuckets { get; set; } = new();
}

public sealed class ExecutiveKpis
{
    public decimal RevenueYtd { get; set; }
    public decimal ExpensesYtd { get; set; }
    public decimal NetIncomeYtd { get; set; }
    public decimal CashPosition { get; set; }
    public decimal ArTotal { get; set; }
    public decimal ApTotal { get; set; }
    public int OpenSalesInvoices { get; set; }
    public int OpenVendorBills { get; set; }
}

public sealed class MonthlySeriesPoint
{
    public string Month { get; set; } = string.Empty; // "2025-01"
    public string MonthLabel { get; set; } = string.Empty; // "يناير"
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
}

public sealed class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class AgingChartData
{
    public decimal Current { get; set; }      // 0-30
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days91Plus { get; set; }
    public decimal Total => Current + Days31To60 + Days61To90 + Days91Plus;
}

public sealed class ExecutiveDashboardService : IExecutiveDashboardService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<ExecutiveDashboardService> _logger;

    public ExecutiveDashboardService(IDbConnectionFactory db, ILogger<ExecutiveDashboardService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<ExecutiveDashboardResponse> GetAsync(Guid companyId, CancellationToken ct)
    {
        using var conn = (NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);
        var asOf = DateTime.UtcNow.Date;
        var yearStart = new DateTime(asOf.Year, 1, 1);

        // اسم الشركة
        var companyName = await conn.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT name FROM companies WHERE id = @Cid",
            new { Cid = companyId }, cancellationToken: ct)) ?? "";

        // ===== KPIs =====
        // Revenue YTD = مجموع 4xxx في السنة الحالية
        var revenueYtd = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(jl.credit - jl.debit), 0)
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.company_id = @Cid AND je.company_id = @Cid
              AND je.status = 2 AND je.entry_date >= @YearStart AND je.entry_date <= @AsOf
              AND a.type = 4", // Revenue
            new { Cid = companyId, YearStart = yearStart, AsOf = asOf },
            cancellationToken: ct)) ?? 0m;

        var expensesYtd = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.company_id = @Cid AND je.company_id = @Cid
              AND je.status = 2 AND je.entry_date >= @YearStart AND je.entry_date <= @AsOf
              AND a.type = 5", // Expense
            new { Cid = companyId, YearStart = yearStart, AsOf = asOf },
            cancellationToken: ct)) ?? 0m;

        // Cash position = مجموع أرصدة حسابات 11xx (Cash & Bank)
        var cashPosition = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(CASE WHEN a.normal_balance = 1 THEN jl.debit - jl.credit ELSE jl.credit - jl.debit END), 0)
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.company_id = @Cid AND je.company_id = @Cid
              AND je.status = 2 AND je.entry_date <= @AsOf
              AND a.code LIKE '11%' AND a.is_postable = true",
            new { Cid = companyId, AsOf = asOf },
            cancellationToken: ct)) ?? 0m;

        // AR total (1230)
        var arTotal = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.company_id = @Cid AND je.company_id = @Cid
              AND je.status = 2 AND je.entry_date <= @AsOf
              AND a.code = '1230'", new { Cid = companyId, AsOf = asOf },
            cancellationToken: ct)) ?? 0m;

        // AP total (2210)
        var apTotal = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(jl.credit - jl.debit), 0)
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.company_id = @Cid AND je.company_id = @Cid
              AND je.status = 2 AND je.entry_date <= @AsOf
              AND a.code = '2210'", new { Cid = companyId, AsOf = asOf },
            cancellationToken: ct)) ?? 0m;

        // Open SI / Bills
        var openSi = await conn.ExecuteScalarAsync<int>(new CommandDefinition(@"
            SELECT COUNT(*) FROM sales_invoices
            WHERE company_id = @Cid AND is_deleted = false AND status = 'Posted'",
            new { Cid = companyId }, cancellationToken: ct));
        var openBills = await conn.ExecuteScalarAsync<int>(new CommandDefinition(@"
            SELECT COUNT(*) FROM vendor_bills
            WHERE company_id = @Cid AND deleted_at IS NULL AND status = 'Posted'",
            new { Cid = companyId }, cancellationToken: ct));

        var kpis = new ExecutiveKpis
        {
            RevenueYtd = revenueYtd,
            ExpensesYtd = expensesYtd,
            NetIncomeYtd = revenueYtd - expensesYtd,
            CashPosition = cashPosition,
            ArTotal = arTotal,
            ApTotal = apTotal,
            OpenSalesInvoices = openSi,
            OpenVendorBills = openBills,
        };

        // ===== Revenue trend 12 months =====
        var trend = new List<MonthlySeriesPoint>();
        var monthLabels = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
                                  "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
        for (int m = 0; m < 12; m++)
        {
            var monthStart = new DateTime(asOf.Year, m + 1, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            if (monthEnd > asOf) monthEnd = asOf;
            if (monthStart > asOf) break;

            var revM = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
                SELECT COALESCE(SUM(jl.credit - jl.debit), 0)
                FROM journal_lines jl
                INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                INNER JOIN accounts a ON a.id = jl.account_id
                WHERE jl.company_id = @Cid AND je.company_id = @Cid
                  AND je.status = 2 AND je.entry_date >= @MS AND je.entry_date <= @ME
                  AND a.type = 4",
                new { Cid = companyId, MS = monthStart, ME = monthEnd },
                cancellationToken: ct)) ?? 0m;
            var expM = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
                SELECT COALESCE(SUM(jl.debit - jl.credit), 0)
                FROM journal_lines jl
                INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
                INNER JOIN accounts a ON a.id = jl.account_id
                WHERE jl.company_id = @Cid AND je.company_id = @Cid
                  AND je.status = 2 AND je.entry_date >= @MS AND je.entry_date <= @ME
                  AND a.type = 5",
                new { Cid = companyId, MS = monthStart, ME = monthEnd },
                cancellationToken: ct)) ?? 0m;
            trend.Add(new MonthlySeriesPoint
            {
                Month = monthStart.ToString("yyyy-MM"),
                MonthLabel = monthLabels[m],
                Revenue = revM,
                Expenses = expM,
                NetIncome = revM - expM,
            });
        }

        // ===== Top Customers (top 5 by sales) =====
        var topCustRows = (await conn.QueryAsync<(string Code, string Name, decimal Total)>(new CommandDefinition(@"
            SELECT c.code, c.name, COALESCE(SUM(si.total_amount), 0) AS total
            FROM customers c
            INNER JOIN sales_invoices si ON si.customer_id = c.id
                AND si.company_id = c.company_id
                AND si.invoice_date >= @YearStart AND si.invoice_date <= @AsOf
                AND si.is_deleted = false
            WHERE c.company_id = @Cid
            GROUP BY c.id, c.code, c.name
            ORDER BY total DESC
            LIMIT 5",
            new { Cid = companyId, YearStart = yearStart, AsOf = asOf },
            cancellationToken: ct))).ToList();
        var topCustomers = topCustRows.Select(r => new ChartDataPoint
        {
            Label = $"{r.Code} {r.Name}",
            Value = r.Total,
        }).ToList();

        // ===== Expense breakdown by account (top 5) =====
        var expRows = (await conn.QueryAsync<(string Code, string Name, decimal Total)>(new CommandDefinition(@"
            SELECT a.code, a.name, COALESCE(SUM(jl.debit - jl.credit), 0) AS total
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE jl.company_id = @Cid AND je.company_id = @Cid
              AND je.status = 2 AND je.entry_date >= @YearStart AND je.entry_date <= @AsOf
              AND a.type = 5 AND a.is_postable = true
            GROUP BY a.id, a.code, a.name
            ORDER BY total DESC
            LIMIT 5",
            new { Cid = companyId, YearStart = yearStart, AsOf = asOf },
            cancellationToken: ct))).ToList();
        var expenseBreakdown = expRows.Select(r => new ChartDataPoint
        {
            Label = $"{r.Code} {r.Name}",
            Value = r.Total,
        }).ToList();

        // ===== AR / AP Aging Buckets =====
        // Sprint 57: نحسب الـ aging buckets من sales_invoices و vendor_bills
        var arBuckets = await ComputeAgingBucketsAsync(conn, companyId, "sales_invoices", "invoice_date", "due_date",
            "total_amount - paid_amount", asOf, ct);
        var apBuckets = await ComputeAgingBucketsAsync(conn, companyId, "vendor_bills", "bill_date", "due_date",
            "total_amount - 0", asOf, ct); // vendor_bills لا paid_amount — نحسب الرصيد المتبقي

        _logger.LogInformation("[SPRINT-57] Dashboard loaded for {Company}: Rev={Rev}, Exp={Exp}, Net={Net}",
            companyName, revenueYtd, expensesYtd, kpis.NetIncomeYtd);

        return new ExecutiveDashboardResponse
        {
            AsOfDate = asOf,
            CompanyId = companyId,
            CompanyName = companyName,
            Kpis = kpis,
            RevenueTrend12Months = trend,
            TopCustomers = topCustomers,
            ExpenseBreakdown = expenseBreakdown,
            ArAgingBuckets = arBuckets,
            ApAgingBuckets = apBuckets,
        };
    }

    private static async Task<AgingChartData> ComputeAgingBucketsAsync(
        NpgsqlConnection conn, Guid companyId,
        string table, string dateCol, string dueCol,
        string amountExpr, DateTime asOf, CancellationToken ct)
    {
        // Sprint 57: نستخدم EXTRACT(DAY FROM ...)::int لتحويل interval إلى integer للمقارنة
        // الـ buckets: 0-30 يوم (current) / 31-60 / 61-90 / 91+
        var sql = $@"
            SELECT
                COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM (@AsOf::date - {dueCol}))::int BETWEEN 0 AND 30 THEN ({amountExpr}) ELSE 0 END), 0) AS Current,
                COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM (@AsOf::date - {dueCol}))::int BETWEEN 31 AND 60 THEN ({amountExpr}) ELSE 0 END), 0) AS Days31To60,
                COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM (@AsOf::date - {dueCol}))::int BETWEEN 61 AND 90 THEN ({amountExpr}) ELSE 0 END), 0) AS Days61To90,
                COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM (@AsOf::date - {dueCol}))::int > 90 THEN ({amountExpr}) ELSE 0 END), 0) AS Days91Plus
            FROM {table}
            WHERE company_id = @Cid
              AND ({amountExpr}) > 0
              AND status = 'Posted'";
        var filterClause = table switch
        {
            "sales_invoices" => "AND is_deleted = false",
            "vendor_bills" => "AND deleted_at IS NULL",
            _ => "",
        };
        sql = sql + " " + filterClause;

        var row = await conn.QueryFirstOrDefaultAsync<AgingChartData>(new CommandDefinition(sql,
            new { Cid = companyId, AsOf = asOf }, cancellationToken: ct));
        return row ?? new AgingChartData();
    }
}
