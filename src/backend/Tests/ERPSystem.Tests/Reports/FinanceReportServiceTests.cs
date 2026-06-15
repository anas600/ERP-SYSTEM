using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Tests.Common;
using FluentAssertions;

namespace ERPSystem.Tests.Reports;

/// <summary>اختبارات خدمة تقارير Finance — تستخدم in-memory FakeDbConnectionFactory (لا تحتاج DB خارجي)</summary>
public class FinanceReportServiceTests
{
    private static (FinanceReportService svc, FakeDbConnectionFactory db, Guid tenantId) Build()
    {
        var tenant = Guid.NewGuid();
        var db = new FakeDbConnectionFactory();
        return (new FinanceReportService(db), db, tenant);
    }

    [Fact]
    public async Task GetTrialBalance_BalancedPostedEntries_ReturnsBalancedReport()
    {
        var (svc, db, tenant) = Build();
        var cash = Guid.NewGuid();
        var revenue = Guid.NewGuid();
        var date = new DateTime(2026, 1, 31);

        // حسابان + قيد مرحّل متوازن 500/500
        db.AddRow("accounts", "id", cash, "tenant_id", tenant, "code", "1110", "name", "الصندوق", "type", (int)AccountType.Asset);
        db.AddRow("accounts", "id", revenue, "tenant_id", tenant, "code", "4100", "name", "إيرادات", "type", (int)AccountType.Revenue);
        var je = Guid.NewGuid();
        db.AddRow("journal_entries", "id", je, "tenant_id", tenant, "status", (int)JournalEntryStatus.Posted, "entry_date", date);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", cash, "debit", 500m, "credit", 0m);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", revenue, "debit", 0m, "credit", 500m);

        var report = await svc.GetTrialBalanceAsync(tenant, null, date, CancellationToken.None);

        report.Rows.Should().HaveCount(2);
        report.TotalDebit.Should().Be(500);
        report.TotalCredit.Should().Be(500);
        report.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetTrialBalance_EmptyTenant_ReturnsEmptyReport()
    {
        var (svc, db, tenant) = Build();

        var report = await svc.GetTrialBalanceAsync(tenant, null, DateTime.UtcNow, CancellationToken.None);

        report.Rows.Should().BeEmpty();
        report.TotalDebit.Should().Be(0);
        report.TotalCredit.Should().Be(0);
        report.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public async Task GetIncomeStatement_RevenueAndCogs_CalculatesCorrectly()
    {
        var (svc, db, tenant) = Build();
        var revenue = Guid.NewGuid();
        var cogs = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);

        db.AddRow("accounts", "id", revenue, "tenant_id", tenant, "code", "4100", "name", "إيرادات", "type", (int)AccountType.Revenue);
        db.AddRow("accounts", "id", cogs, "tenant_id", tenant, "code", "5110", "name", "تكلفة مبيعات", "type", (int)AccountType.Expense);
        var je = Guid.NewGuid();
        db.AddRow("journal_entries", "id", je, "tenant_id", tenant, "status", (int)JournalEntryStatus.Posted, "entry_date", to);
        // Revenue 1000, COGS 600
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", revenue, "debit", 0m, "credit", 1000m);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", cogs, "debit", 600m, "credit", 0m);

        var income = await svc.GetIncomeStatementAsync(tenant, null, from, to, CancellationToken.None);

        income.Revenue.Should().Be(1000);
        income.Cogs.Should().Be(600);
        income.GrossProfit.Should().Be(400, "إجمالي الربح = الإيرادات - COGS");
    }

    [Fact]
    public async Task GetIncomeStatement_OpEx_ClassifiedAsOperatingExpenses()
    {
        var (svc, db, tenant) = Build();
        var opex = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);

        db.AddRow("accounts", "id", opex, "tenant_id", tenant, "code", "5200", "name", "إيجار", "type", (int)AccountType.Expense);
        var je = Guid.NewGuid();
        db.AddRow("journal_entries", "id", je, "tenant_id", tenant, "status", (int)JournalEntryStatus.Posted, "entry_date", to);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", opex, "debit", 200m, "credit", 0m);

        var income = await svc.GetIncomeStatementAsync(tenant, null, from, to, CancellationToken.None);

        income.OperatingExpenses.Should().Be(200);
        income.Cogs.Should().Be(0, "حساب 5200 ليس COGS");
    }

    [Fact]
    public async Task GetBalanceSheet_AssetsEqualsLiabilitiesPlusEquity_IsBalanced()
    {
        var (svc, db, tenant) = Build();
        var cash = Guid.NewGuid();
        var ap = Guid.NewGuid();
        var equity = Guid.NewGuid();
        var date = new DateTime(2026, 1, 31);

        // أصول 1000 = خصوم 400 + حقوق ملكية 600
        db.AddRow("accounts", "id", cash, "tenant_id", tenant, "code", "1110", "name", "الصندوق", "type", (int)AccountType.Asset);
        db.AddRow("accounts", "id", ap, "tenant_id", tenant, "code", "2110", "name", "دائنون", "type", (int)AccountType.Liability);
        db.AddRow("accounts", "id", equity, "tenant_id", tenant, "code", "3100", "name", "رأس المال", "type", (int)AccountType.Equity);
        var je = Guid.NewGuid();
        db.AddRow("journal_entries", "id", je, "tenant_id", tenant, "status", (int)JournalEntryStatus.Posted, "entry_date", date);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", cash, "debit", 1000m, "credit", 0m);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", ap, "debit", 0m, "credit", 400m);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", equity, "debit", 0m, "credit", 600m);

        var bs = await svc.GetBalanceSheetAsync(tenant, null, date, CancellationToken.None);

        bs.TotalAssets.Should().Be(1000);
        bs.TotalLiabilities.Should().Be(400);
        bs.TotalEquity.Should().Be(600);
        bs.IsBalanced.Should().BeTrue("المعادلة المحاسبية يجب أن تتوازن");
    }

    [Fact]
    public async Task TrialBalanceRow_Dto_NetDebit_AndNetCredit_CalculateCorrectly()
    {
        // اختبار حسابات الـ DTO (لا يحتاج DB)
        var row = new TrialBalanceRow { Debit = 800, Credit = 300 };
        row.NetDebit.Should().Be(500);
        row.NetCredit.Should().Be(0);
    }

    [Fact]
    public async Task IncomeStatement_Dto_NetIncome_AndGrossProfit_CalculateCorrectly()
    {
        var income = new IncomeStatement
        {
            Revenue = 1000, Cogs = 400,
            OperatingExpenses = 200, OtherIncome = 50, OtherExpenses = 30
        };
        income.GrossProfit.Should().Be(600);
        income.NetIncome.Should().Be(420, "(1000-400) - 200 + 50 - 30");
    }
}
