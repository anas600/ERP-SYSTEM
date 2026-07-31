// Sprint 11 T2 (BE Jimi) — Company tree endpoint tests.
//
// Three tests for CompanyService.GetTreeAsync (matches the FE's
// `getCompanyTree()` contract in api-types.ts):
//
// 1. Holding with 2 subsidiaries returns 1 root + 2 children (the Holding
//    itself is the implicit root; the FE renders the list of children).
// 2. Deep hierarchy (Holding > Sub > SubSub) returns nested children
//    recursively built from parent_company_id.
// 3. Empty repository (no Holding seeded) returns an empty list (200 OK
//    with []), NOT 404 — the FE renders the empty state cleanly.
//
// Test approach: FakeDbConnectionFactory (in-memory DataSet). The
// CompanyRepository.ListAsync uses the SELECT ... FROM companies query
// shape, so we seed the `companies` table with the right column names
// and assert on the returned tree shape.

using Dapper;
using ERPSystem.Modules.Companies.Application.DTOs;
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
    /// Happy path: 1 Holding + 2 subsidiaries → the tree returns the 2
    /// subsidiaries as direct children of the (implicit) Holding root.
    /// The Holding itself is the root, identified by
    /// is_group=true AND parent_company_id IS NULL; it is NOT in the
    /// returned list (the list contains the Holding's children).
    /// </summary>
    [Fact]
    public async Task GetTreeAsync_OneHoldingTwoSubsidiaries_ReturnsOneRootWithTwoChildren()
    {
        var db = new FakeDbConnectionFactory();
        SeedCompany(db, "000", "MFA Holding", isGroup: true);
        SeedCompany(db, "ALF", "AlFajr Construction", isGroup: false, parentId: null /* set below */);
        SeedCompany(db, "ALB", "AlBurj Trading", isGroup: false, parentId: null /* set below */);

        // Patch parent_company_id to the Holding for the two subsidiaries.
        // (FakeDb.AddRow sets columns, but parent_company_id needs to be the
        // Holding's id, which we don't know until we seed it. Easiest: re-seed
        // the rows with explicit parent ids.)
        db = new FakeDbConnectionFactory();
        var holdingId = Guid.NewGuid();
        var alfId = Guid.NewGuid();
        var albId = Guid.NewGuid();
        SeedCompany(db, "000", "MFA Holding", id: holdingId, isGroup: true);
        SeedCompany(db, "ALF", "AlFajr Construction", id: alfId, isGroup: false, parentId: holdingId);
        SeedCompany(db, "ALB", "AlBurj Trading", id: albId, isGroup: false, parentId: holdingId);

        var svc = BuildService(db);
        var r = await svc.GetTreeAsync(CancellationToken.None);

        r.Succeeded.Should().BeTrue("a resolved DB with rows should succeed");
        r.Value.Should().NotBeNull();
        r.Value.Should().HaveCount(2, "the Holding has 2 direct subsidiaries");

        // Each child carries the Holding's id as parentCompanyId and the
        // expected flat DTO fields.
        var children = r.Value!.ToList();
        children.Should().AllSatisfy(c =>
        {
            c.ParentCompanyId.Should().Be(holdingId, "the Holding is the parent");
            c.IsGroup.Should().BeFalse("subsidiaries are not groups");
            c.Children.Should().BeEmpty("no nested children at depth 1");
        });

        children.Select(c => c.Code).Should().BeEquivalentTo(new[] { "ALF", "ALB" },
            "the 2 subsidiary codes are returned");
    }

    /// <summary>
    /// Deep hierarchy: Holding > Subsidiary A > Subsidiary B (B's parent
    /// is A, not the Holding). The tree builder must walk recursively and
    /// surface B as a nested child of A.
    /// </summary>
    [Fact]
    public async Task GetTreeAsync_DeepHierarchy_BuildsNestedChildren()
    {
        var db = new FakeDbConnectionFactory();
        var holdingId = Guid.NewGuid();
        var subAId = Guid.NewGuid();
        var subBId = Guid.NewGuid();
        SeedCompany(db, "000", "MFA Holding", id: holdingId, isGroup: true);
        SeedCompany(db, "ALF", "AlFajr Construction", id: subAId, isGroup: false, parentId: holdingId);
        SeedCompany(db, "ALF-NB", "AlFajr North Branch", id: subBId, isGroup: false, parentId: subAId);

        var svc = BuildService(db);
        var r = await svc.GetTreeAsync(CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value.Should().HaveCount(1, "only ALF is a direct child of the Holding; ALF-NB is nested");

        var alf = r.Value!.Single();
        alf.Code.Should().Be("ALF");
        alf.ParentCompanyId.Should().Be(holdingId);
        alf.Children.Should().HaveCount(1, "ALF has 1 nested child (ALF-NB)");

        var alfNb = alf.Children.Single();
        alfNb.Code.Should().Be("ALF-NB");
        alfNb.ParentCompanyId.Should().Be(subAId, "ALF-NB's parent is ALF, not the Holding");
        alfNb.Children.Should().BeEmpty("ALF-NB is a leaf in this hierarchy");
    }

    /// <summary>
    /// Empty repository: no Holding seeded → returns an empty list (200 OK
    /// with []). The FE renders the empty-state hint cleanly; we never 404
    /// on this route because the tree shape is collection-shaped and the
    /// dashboard needs it to work even before the bootstrap creates the
    /// first Holding.
    /// </summary>
    [Fact]
    public async Task GetTreeAsync_EmptyRepository_ReturnsEmptyList()
    {
        var db = new FakeDbConnectionFactory();
        // No rows seeded.

        var svc = BuildService(db);
        var r = await svc.GetTreeAsync(CancellationToken.None);

        r.Succeeded.Should().BeTrue("an empty repo is a valid state, not an error");
        r.Value.Should().NotBeNull();
        r.Value.Should().BeEmpty("no Holding → no children");
    }

    // ============ Test helpers ============

    private static CompanyService BuildService(FakeDbConnectionFactory db)
    {
        var accounts = new Mock<IAccountRepository>(MockBehavior.Strict);
        // GetTreeAsync does not call IAccountRepository. If a refactor
        // accidentally starts calling it, the strict mock will throw,
        // which is exactly the signal we want.
        var companies = new CompanyRepository(db);
        return new CompanyService(companies, accounts.Object, NullLogger<CompanyService>.Instance);
    }

    private static void SeedCompany(
        FakeDbConnectionFactory db,
        string code,
        string name,
        Guid? id = null,
        bool isGroup = false,
        Guid? parentId = null)
    {
        db.AddRow("companies",
            "id", id ?? Guid.NewGuid(),
            "code", code,
            "name", name,
            "slug", code.ToLowerInvariant(),
            "legal_name", name,
            "parent_company_id", parentId,
            "is_group", isGroup,
            "base_currency", "LYD",
            "is_active", true,
            "created_at", DateTime.UtcNow,
            "updated_at", DateTime.UtcNow);
    }
}
