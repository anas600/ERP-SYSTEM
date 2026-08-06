using Dapper;
using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ERPSystem.Tests.Finance;

/// <summary>
/// Sprint 48 (DEC-130..132) — اختبارات التحقق المحاسبي للمعادلات الأساسية:
/// - ميزان المراجعة: ΣDebit = ΣCredit
/// - الميزانية: ΣAssets = ΣLiab + ΣEquity
/// - قائمة الدخل: Revenue − Expenses = NetIncome
/// - دفتر الأستاذ: Opening + Movements = Closing
/// </summary>
public class FinancialReportsTests
{
    private static (NpgsqlConnectionFactory db, IAccountRepository accounts, IJournalEntryRepository journalRepo)
        CreateRealDb()
    {
        // نستخدم قاعدة البيانات المحلية (appsettings.Development.json)
        // ملاحظة: هذا الـ test يفترض أن الـ DB شغّال ومُهيّأ — يتم تخطيه لو غير متاح.
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=erp_system;Username=erp;Password=erp";
        var opts = Options.Create(new NpgsqlConnectionOptions { OltpConnectionString = connStr });
        var db = new NpgsqlConnectionFactory(opts, NullLogger<NpgsqlConnectionFactory>.Instance);
        var accounts = new AccountRepository(db);
        var journalRepo = new JournalEntryRepository(db);
        return (db, accounts, journalRepo);
    }

    private static async Task<Guid> EnsureHoldingAsync(IDbConnectionFactory db, IAccountRepository accounts, IJournalEntryRepository jr)
    {
        // 1) احصل على أول Holding company أو أنشئها
        using var conn = await db.CreateOltpConnectionAsync(CancellationToken.None);
        var holding = await conn.QueryFirstOrDefaultAsync<(Guid Id, string Name)>(
            "SELECT id, name FROM companies WHERE is_holding = true LIMIT 1");
        if (holding.Id != Guid.Empty) return holding.Id;

        // 2) أنشئ Holding + انسخ CoA الافتراضي
        var newId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO companies (id, code, name, name_en, is_holding, is_active, created_at, updated_at)
            VALUES (@Id, 'TEST-HOLD', 'شركة اختبار', 'Test Holding', true, true, NOW(), NOW())",
            new { Id = newId });
        await accounts.EnsureDefaultCoAAsync(newId, CancellationToken.None);
        return newId;
    }

    [Fact(Skip = "Integration test — needs running Postgres + seeded data. Run manually with dotnet test --filter 'FullyQualifiedName~FinancialReportsTests'")]
    public async Task BalanceSheet_BalancesEquation_AssetsEqualsLiabPlusEquity()
    {
        var (db, accounts, jr) = CreateRealDb();
        var companyId = await EnsureHoldingAsync(db, accounts, jr);
        var svc = new GeneralLedgerReportService(db, accounts);

        var asOf = new DateTime(2026, 6, 30);
        var r = await svc.GetBalanceSheetAsync(companyId, asOf, CancellationToken.None);

        Assert.True(r.Succeeded, r.Error);
        var bs = r.Value!;
        // ΣAssets = ΣLiab + ΣEq (within 0.01 tolerance)
        Assert.True(bs.IsBalanced,
            $"BS not balanced: A={bs.TotalAssets:N4}  L+E={bs.TotalLiabilitiesAndEquity:N4}  var={bs.Variance:N4}");
    }

    [Fact(Skip = "Integration test — needs running Postgres + seeded data")]
    public async Task IncomeStatement_NetIncomeEqualsRevenueMinusExpenses()
    {
        var (db, accounts, jr) = CreateRealDb();
        var companyId = await EnsureHoldingAsync(db, accounts, jr);
        var svc = new GeneralLedgerReportService(db, accounts);

        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2026, 6, 30);
        var r = await svc.GetIncomeStatementAsync(companyId, from, to, CancellationToken.None);

        Assert.True(r.Succeeded, r.Error);
        var pl = r.Value!;
        Assert.Equal(pl.TotalRevenue - pl.TotalExpenses, pl.NetIncome);
    }

    [Fact(Skip = "Integration test — needs running Postgres + seeded data")]
    public async Task CashFlow_NetChangeMatchesCashDelta()
    {
        var (db, accounts, jr) = CreateRealDb();
        var companyId = await EnsureHoldingAsync(db, accounts, jr);
        var svc = new GeneralLedgerReportService(db, accounts);

        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2026, 6, 30);
        var r = await svc.GetCashFlowAsync(companyId, from, to, CancellationToken.None);

        Assert.True(r.Succeeded, r.Error);
        // TODO: قارن r.Value.NetChangeInCash مع الفرق الفعلي لرصيد الحسابات النقدية
    }

    [Fact(Skip = "Integration test — needs running Postgres + seeded data")]
    public async Task TrialBalance_DebitsEqualCredits()
    {
        var (db, accounts, jr) = CreateRealDb();
        var companyId = await EnsureHoldingAsync(db, accounts, jr);
        var svc = new GeneralLedgerService(db, accounts);

        var r = await svc.GetTrialBalanceAsync(companyId, null, CancellationToken.None);
        Assert.True(r.Succeeded, r.Error);
        var totalDr = r.Value!.Sum(x => x.TotalDebit);
        var totalCr = r.Value!.Sum(x => x.TotalCredit);
        Assert.True(Math.Abs(totalDr - totalCr) < 0.01m, $"ΣDr={totalDr:N4}  ΣCr={totalCr:N4}");
    }
}
