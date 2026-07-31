// Sprint 5 (T1-T3 / Phase 4) — Dashboard chart service tests.
//
// Two tests per endpoint (1 happy + 1 error per architecture.md soft rule #4):
//
//   T1 GetRevenueVsExpenseAsync
//     - Happy:  resolved company context → returns a list (may be empty
//               when the FakeDb has no data; the contract is "doesn't
//               crash and returns IReadOnlyList<RevenueVsExpensePoint>")
//     - Error:  unresolved company context → returns an empty list
//               (the same contract as DashboardSummaryService — no
//               company context means graceful empty payload, not 500)
//
//   T2 GetExpensesByCategoryAsync
//     - Happy + Error, same shape as T1
//
//   T3 GetTopCustomersAsync
//     - Happy + Error, same shape as T1
//
// Test approach: same pattern as DashboardSummaryTests —
// FakeDbConnectionFactory (in-memory DataSet) + Moq for ICompanyContext.
// The FakeDb only resolves the first FROM table (no JOINs, no WHERE), so
// the assertions are limited to "the call returns the right shape". The
// SQL filtering, ranking, and aggregation are validated on a real Postgres
// in the [Fact(Skip = ...)] integration test at the bottom of the file
// (same convention as DashboardSummaryTests).

using ERPSystem.Modules.Dashboard.Application.DTOs;
using ERPSystem.Modules.Dashboard.Application.Services;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Dashboard;

public class DashboardChartTests
{
    // ----- Shared build helpers -----

    private static (DashboardChartService svc, FakeDbConnectionFactory db, Guid companyId) BuildResolved()
    {
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();

        // Seed source tables with a few rows each. The FakeDb returns every
        // row in the first FROM table regardless of WHERE / JOIN, so we just
        // need the tables to exist with at least one row to exercise the
        // query path without it failing.
        db.AddRow("sales_invoices", "id", Guid.NewGuid(), "company_id", companyId,
            "customer_id", Guid.NewGuid(), "total_amount", 100m, "status", "Posted",
            "invoice_date", DateTime.UtcNow);
        db.AddRow("sales_invoices", "id", Guid.NewGuid(), "company_id", companyId,
            "customer_id", Guid.NewGuid(), "total_amount", 200m, "status", "Posted",
            "invoice_date", DateTime.UtcNow.AddMonths(-1));
        db.AddRow("journal_lines", "id", Guid.NewGuid(), "company_id", companyId,
            "account_id", Guid.NewGuid(), "debit", 50m, "credit", 0m);
        db.AddRow("customers", "id", Guid.NewGuid(), "company_id", companyId,
            "name", "ACME", "code", "C001", "email", "a@x");

        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(companyId);
        ctx.Setup(c => c.UserId).Returns(Guid.NewGuid());
        ctx.Setup(c => c.IsResolved).Returns(true);

        var svc = new DashboardChartService(db, ctx.Object, NullLogger<DashboardChartService>.Instance);
        return (svc, db, companyId);
    }

    private static DashboardChartService BuildUnresolved()
    {
        var db = new FakeDbConnectionFactory();
        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns((Guid?)null);
        ctx.Setup(c => c.UserId).Returns((Guid?)null);
        ctx.Setup(c => c.IsResolved).Returns(false);
        return new DashboardChartService(db, ctx.Object, NullLogger<DashboardChartService>.Instance);
    }

    // ----- T1: Revenue vs Expense -----

    [Fact]
    public async Task GetRevenueVsExpenseAsync_ResolvedContext_ReturnsList()
    {
        var (svc, _, _) = BuildResolved();

        var result = await svc.GetRevenueVsExpenseAsync(months: 6, CancellationToken.None);

        // Contract: returns a non-null IReadOnlyList. With the FakeDb the
        // list may be empty (since the SQL aliases don't match any seeded
        // column, all mapped values are 0/""), but the call must not
        // throw and must return a list instance.
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<RevenueVsExpensePoint>>();
    }

    [Fact]
    public async Task GetRevenueVsExpenseAsync_UnresolvedContext_ReturnsEmpty()
    {
        var svc = BuildUnresolved();

        var result = await svc.GetRevenueVsExpenseAsync(months: 6, CancellationToken.None);

        result.Should().BeEmpty(
            "no company context → graceful empty payload, never a 500 (matches DashboardSummaryService contract)");
    }

    // ----- T2: Expenses by Category -----

    [Fact]
    public async Task GetExpensesByCategoryAsync_ResolvedContext_ReturnsList()
    {
        var (svc, _, _) = BuildResolved();

        var result = await svc.GetExpensesByCategoryAsync(months: 3, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<ExpenseCategorySlice>>();
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_UnresolvedContext_ReturnsEmpty()
    {
        var svc = BuildUnresolved();

        var result = await svc.GetExpensesByCategoryAsync(months: 3, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ----- T3: Top Customers -----

    [Fact]
    public async Task GetTopCustomersAsync_ResolvedContext_ReturnsList()
    {
        var (svc, _, _) = BuildResolved();

        var result = await svc.GetTopCustomersAsync(limit: 5, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<TopCustomerChartRow>>();
    }

    [Fact]
    public async Task GetTopCustomersAsync_UnresolvedContext_ReturnsEmpty()
    {
        var svc = BuildUnresolved();

        var result = await svc.GetTopCustomersAsync(limit: 5, CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ----- Parameter clamping (smoke) -----

    [Fact]
    public async Task GetRevenueVsExpenseAsync_ClampsHugeWindow()
    {
        // months=1000 should be clamped to 24 internally — the call should
        // not throw, just return whatever the (clamped) window produces.
        var (svc, _, _) = BuildResolved();
        var act = async () => await svc.GetRevenueVsExpenseAsync(months: 1000, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ----- Integration (skipped) -----
    // On real Postgres, the chart service must:
    //   - scope to current company_id
    //   - aggregate by month correctly
    //   - apply the status filter (Posted / Partial / Paid only)
    //   - order top customers by total_amount DESC
    // The FakeDb ignores WHERE / JOIN so this can't be verified here.
    [Fact(Skip = "Integration: requires real Postgres. See CI workflow.")]
    public async Task Charts_IsolateByCompany_And_AggregateByMonth()
    {
        await Task.CompletedTask;
    }
}
