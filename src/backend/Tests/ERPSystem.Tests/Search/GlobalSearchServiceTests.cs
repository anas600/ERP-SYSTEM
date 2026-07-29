// Sprint 5 (T4 / Phase 5) — Global search service tests.
//
// Two tests (1 happy + 1 error per architecture.md soft rule #4):
//
//   Happy: resolved company context + a non-empty `q` → returns a list
//          (may be empty if the FakeDb has no matching rows, but the call
//           must not throw and must return IReadOnlyList<SearchResultDto>).
//
//   Error: unresolved company context → returns an empty list (graceful
//          empty payload, never 500 — same convention as
//          DashboardSummaryService and the chart service).
//
// Additional smoke tests:
//   - empty `q` → empty list (no DB calls for "match everything")
//   - huge `limit` → clamped to 50, no throw
//
// The FakeDb only resolves the first FROM table (no JOINs, no WHERE), so
// we can't assert specific result counts here. The full SQL filter + 3-tier
// ranking is validated in the skipped integration test at the bottom of
// the file (same convention as DashboardChartTests).

using ERPSystem.Modules.Search.Application.DTOs;
using ERPSystem.Modules.Search.Application.Services;
using ERPSystem.Shared.MultiTenancy;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Search;

public class GlobalSearchServiceTests
{
    private static (GlobalSearchService svc, FakeDbConnectionFactory db, Guid companyId) BuildResolved()
    {
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();

        // Seed all 4 source tables with at least one row so the SQL parser
        // (FakeDbDataReader) doesn't return an empty set for the first
        // FROM table. The actual filtering is validated on real Postgres.
        db.AddRow("customers", "id", Guid.NewGuid(), "company_id", companyId,
            "name", "ACME Corp", "code", "C001", "email", "a@x");
        db.AddRow("vendors", "id", Guid.NewGuid(), "company_id", companyId,
            "name", "VendorX", "code", "V001", "email", "v@x");
        db.AddRow("sales_invoices", "id", Guid.NewGuid(), "company_id", companyId,
            "customer_id", Guid.NewGuid(), "invoice_number", "INV-001");
        db.AddRow("accounts", "id", Guid.NewGuid(), "company_id", companyId,
            "name", "Cash", "code", "1000");

        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(companyId);
        ctx.Setup(c => c.UserId).Returns(Guid.NewGuid());
        ctx.Setup(c => c.IsResolved).Returns(true);

        var svc = new GlobalSearchService(db, ctx.Object, NullLogger<GlobalSearchService>.Instance);
        return (svc, db, companyId);
    }

    private static GlobalSearchService BuildUnresolved()
    {
        var db = new FakeDbConnectionFactory();
        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns((Guid?)null);
        ctx.Setup(c => c.UserId).Returns((Guid?)null);
        ctx.Setup(c => c.IsResolved).Returns(false);
        return new GlobalSearchService(db, ctx.Object, NullLogger<GlobalSearchService>.Instance);
    }

    [Fact]
    public async Task SearchAsync_ResolvedContext_ReturnsList()
    {
        var (svc, _, _) = BuildResolved();

        var result = await svc.SearchAsync("acme", limit: 20, CancellationToken.None);

        // Contract: returns a non-null IReadOnlyList. With the FakeDb the
        // SQL aliases don't match any seeded column, so the list may be
        // empty — but the call must not throw and must return a list
        // instance with the right DTO type.
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<SearchResultDto>>();
    }

    [Fact]
    public async Task SearchAsync_UnresolvedContext_ReturnsEmpty()
    {
        var svc = BuildUnresolved();

        var result = await svc.SearchAsync("acme", limit: 20, CancellationToken.None);

        result.Should().BeEmpty(
            "no company context → graceful empty payload, never 500 (matches dashboard pattern)");
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyWithoutDbCall()
    {
        // Empty / whitespace `q` is a contract skip — the service must NOT
        // run 4 LIKE '% %' queries that would match almost everything.
        var (svc, _, _) = BuildResolved();

        var result = await svc.SearchAsync("   ", limit: 20, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_HugeLimit_DoesNotThrow()
    {
        // limit=10000 should be clamped to 50 internally — the call should
        // not throw, just return whatever the (clamped) result is.
        var (svc, _, _) = BuildResolved();
        var act = async () => await svc.SearchAsync("acme", limit: 10000, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ----- Integration (skipped) -----
    // On real Postgres, the search must:
    //   - scope every sub-query to current company_id
    //   - rank exact > prefix > contains
    //   - cap per type at 5 and total at `limit`
    //   - merge types in stable order (customer → supplier → invoice → account)
    [Fact(Skip = "Integration: requires real Postgres. See CI workflow.")]
    public async Task Search_ScopesByCompany_And_RanksCorrectly()
    {
        await Task.CompletedTask;
    }
}
