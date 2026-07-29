// Sprint 6 (T3 / Wrap-up) — CompanyService.GetTreeAsync test.
//
// Goal: cover the gap for GET /api/companies/tree (CompaniesController.Tree).
// The endpoint is exposed in the controller and the service method exists
// (CompanyService.GetTreeAsync), but the test folder has no coverage for it
// — only CompaniesListTests for ListPagedAsync. This file adds the missing
// smoke test per the worker contract "1 test per endpoint" rule.
//
// What this test asserts:
//   1. GetTreeAsync returns Succeeded=true.
//   2. The result has exactly 1 root (the Holding) — only rows with
//      ParentCompanyId == null are roots, even if the repo returns more.
//   3. The Holding root has 2 children (the two subsidiaries seeded
//      with parent_company_id = HoldingId).
//
// Test approach: same pattern as CompaniesListTests — FakeDbConnectionFactory
// + real CompanyRepository + Moq for IAccountRepository (Strict, since
// GetTreeAsync never touches accounts). The seeded table is "companies" with
// the exact columns the SELECT projects (see CompanyRepository.ListAsync —
// SelectColumns uses SQL aliases like `legal_name AS LegalName`, so the
// AddRow column names must be the projected names, not the raw DB column
// names). The FakeDb returns columns by their AddRow name and Dapper's
// name-based matching requires the column name to match the C# property
// name (case-insensitive, no underscore stripping).
//
// What this test does NOT assert (out of scope for a smoke test):
//   - The SQL `WHERE is_active` filter on includeInactive=false (FakeDb
//     ignores WHERE — integration test territory).
//   - Multi-level nesting (only 1 level of children is seeded; deeper
//     recursion is verified by integration tests on real Postgres).
//   - The order of siblings inside Children (FakeDb returns insertion
//     order; the real DB has its own ORDER BY; the FE doesn't depend on
//     a specific order).

using ERPSystem.Modules.Companies.Application.Services;
using ERPSystem.Modules.Companies.Entities;
using ERPSystem.Modules.Companies.Infrastructure;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Companies;

public class CompanyTreeTests
{
    /// <summary>
    /// Happy path: 1 Holding + 2 subsidiaries → tree has 1 root with 2
    /// children. The Holding is the only row with ParentCompanyId == null.
    /// </summary>
    [Fact]
    public async Task GetTreeAsync_HoldingAndTwoSubsidiaries_BuildsOneRootWithTwoChildren()
    {
        // Arrange — seed the FakeDb "companies" table with the canonical
        // multi-company fixture: 1 Holding (is_group=true) + 2 subsidiaries.
        var db = new FakeDbConnectionFactory();
        var holdingId = Guid.NewGuid();
        SeedCompany(db, holdingId, "000", "Holding Enterprise", parentCompanyId: null, isGroup: true);
        SeedCompany(db, Guid.NewGuid(), "ALF", "AlFajr Subsidiary", parentCompanyId: holdingId);
        SeedCompany(db, Guid.NewGuid(), "ALB", "AlBurj Subsidiary", parentCompanyId: holdingId);

        var svc = BuildService(db);

        // Act
        var r = await svc.GetTreeAsync(CancellationToken.None);

        // Assert — successful result, 1 root (the Holding), 2 children.
        r.Succeeded.Should().BeTrue("GetTreeAsync never fails — it just wraps the repo result");
        r.Value.Should().NotBeNull();

        var roots = r.Value!.Children;
        roots.Should().HaveCount(1, "only the Holding has ParentCompanyId == null");

        var root = roots[0];
        root.Company.Should().NotBeNull();
        root.Company.Code.Should().Be("000", "the root must be the Holding (code '000')");
        root.Company.IsGroup.Should().BeTrue("the root must be marked is_group=true");

        root.Children.Should().HaveCount(2, "the Holding has 2 subsidiaries seeded");
        root.Children.Select(c => c.Company.Code)
            .Should().BeEquivalentTo(new[] { "ALF", "ALB" },
                "the two subsidiaries are AlFajr and AlBurj");
        root.Children.Should().OnlyContain(c => c.Company.ParentCompanyId == holdingId,
            "every child's parent_company_id must equal the Holding's id");
    }

    /// <summary>
    /// Edge case: an empty repository returns a tree with 0 roots. The FE
    /// renders an empty state in this case (no Holding has been bootstrapped
    /// yet — the FE should show a setup wizard, not an error toast).
    /// </summary>
    [Fact]
    public async Task GetTreeAsync_EmptyRepository_ReturnsEmptyRootsList()
    {
        var db = new FakeDbConnectionFactory();
        var svc = BuildService(db);

        var r = await svc.GetTreeAsync(CancellationToken.None);

        r.Succeeded.Should().BeTrue("empty repo is not an error — it's the bootstrap-pre state");
        r.Value.Should().NotBeNull();
        r.Value!.Children.Should().BeEmpty("no rows in the repo → no roots in the tree");
    }

    // ============ Test helpers ============

    private static CompanyService BuildService(FakeDbConnectionFactory db)
    {
        // GetTreeAsync only touches ICompanyRepository — Strict mock for
        // IAccountRepository makes any accidental call from the tree path
        // fail loudly (the same guard CompaniesListTests uses).
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        var companies = new CompanyRepository(db);
        return new CompanyService(companies, accounts.Object, NullLogger<CompanyService>.Instance);
    }

    private static void SeedCompany(
        FakeDbConnectionFactory db,
        Guid id,
        string code,
        string name,
        Guid? parentCompanyId,
        bool isGroup = false)
    {
        // Column names must match the SQL projection in CompanyRepository.SelectColumns:
        //   id, code, name, slug, legal_name AS LegalName,
        //   parent_company_id AS ParentCompanyId, is_group AS IsGroup, ...
        // Dapper's name-based matching requires the FakeDb's column name to
        // match the C# property name; using the SQL alias here lets
        // ParentCompanyId / IsGroup / etc. actually populate.
        db.AddRow("companies",
            "Id", id,
            "Code", code,
            "Name", name,
            "Slug", code.ToLowerInvariant(),
            "LegalName", name,
            "ParentCompanyId", parentCompanyId,
            "IsGroup", isGroup,
            "BaseCurrency", "LYD",
            "IsActive", true,
            "CreatedAt", DateTime.UtcNow,
            "UpdatedAt", DateTime.UtcNow);
    }
}
