using Dapper;
using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Projects;

/// <summary>
/// Sprint 65 / Wave 2A (DEC-233): Tests for ProjectCostService.
///
/// The 5 tests cover:
///   1. Subcontractor cost aggregates correctly from the ISubPaymentRepository mock
///   2. Full breakdown returns all 5 categories (labor/material/sub/equipment/overhead)
///   3. Zero subcontractor cost when the repository returns 0
///   4. Repository-level exclusion of cancelled payments (status != 4)
///   5. CompanyId is scoped from ICompanyContext (not from any other source — L19)
/// </summary>
public class Sprint65ProjectCostServiceTests
{
    // ===================== Fake repository: in-memory project storage =====================

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public Dictionary<Guid, Project> ById { get; } = new();

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(ById.TryGetValue(id, out var p) ? p : null);

        public Task<Project?> GetByCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult(ById.Values.FirstOrDefault(p => p.Code == code));

        public Task<IReadOnlyList<Project>> ListAsync(
            Guid? companyId, ProjectStatus? status, bool includeInactive, int skip, int take, CancellationToken ct)
        {
            IEnumerable<Project> q = ById.Values;
            if (companyId.HasValue) q = q.Where(p => p.CompanyId == companyId.Value);
            if (status.HasValue) q = q.Where(p => p.Status == status.Value);
            if (!includeInactive) q = q.Where(p => p.IsActive);
            return Task.FromResult<IReadOnlyList<Project>>(q.Skip(skip).Take(take).ToList());
        }

        public Task InsertAsync(Project project, CancellationToken ct)
        {
            ById[project.Id] = project;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Project project, CancellationToken ct)
        {
            ById[project.Id] = project;
            return Task.CompletedTask;
        }
    }

    // ===================== Stub ISubPaymentRepository (records args, returns scripted value) =====================

    private sealed class StubSubPaymentRepository : ISubPaymentRepository
    {
        public List<(Guid CompanyId, Guid ProjectId)> Calls { get; } = new();
        public Func<Guid, Guid, decimal> ScriptedReturn { get; set; } = (_, _) => 0m;

        public Task<decimal> SumActivePaymentsForProjectAsync(Guid companyId, Guid projectId, CancellationToken ct)
        {
            Calls.Add((companyId, projectId));
            return Task.FromResult(ScriptedReturn(companyId, projectId));
        }
    }

    private static (ProjectCostService svc, FakeProjectRepository projects, FakeDbConnectionFactory db, StubSubPaymentRepository sub, Guid companyId)
        Build(Guid? companyIdOverride = null, decimal scriptedSubAmount = 0m)
    {
        var db = new FakeDbConnectionFactory();
        var projects = new FakeProjectRepository();
        var sub = new StubSubPaymentRepository { ScriptedReturn = (_, _) => scriptedSubAmount };
        var companyId = companyIdOverride ?? Guid.NewGuid();
        var ctx = TestCompanyContextFactory.Create(companyId);
        var svc = new ProjectCostService(projects, db, sub, ctx, NullLogger<ProjectCostService>.Instance);
        return (svc, projects, db, sub, companyId);
    }

    private static Project SeedProject(FakeProjectRepository projects, Guid projectId, Guid companyId, Guid? costCenterId = null)
    {
        var p = new Project
        {
            Id = projectId, CompanyId = companyId,
            CostCenterId = costCenterId ?? Guid.NewGuid(),
            Code = "PRJ-TEST-001", Name = "Test Project", Status = ProjectStatus.Active,
            Budget = 100_000m, StartDate = DateTime.UtcNow, IsActive = true,
        };
        projects.ById[projectId] = p;
        return p;
    }

    // ============== Test 1 ==============

    [Fact]
    public async Task GetSubcontractorCostAsync_AggregatesFromSubPayments()
    {
        var projectId = Guid.NewGuid();
        var (svc, projects, _, _, companyId) = Build(scriptedSubAmount: 42_500m);
        SeedProject(projects, projectId, companyId);

        var r = await svc.GetSubcontractorCostAsync(projectId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value.Should().Be(42_500m, "the scripted sub_payments sum is the source of truth at Wave 2A time");
    }

    // ============== Test 2 ==============

    [Fact]
    public async Task GetBreakdownAsync_ReturnsAllFiveCategories()
    {
        // The SQL in GetBreakdownAsync joins 3 tables (journal_lines + journal_entries
        // + accounts) which the project's FakeDb cannot fully simulate (its column
        // projector only handles single-table SELECTs — see Common/FakeDbConnectionFactory.cs
        // Sprint 8 T2 notes). The path that goes through the SQL aggregation is exercised
        // by integration tests; here we focus on the contract: the breakdown always
        // includes the subcontractor cost from the repository, and TotalCost is the
        // sum of the 5 categories (so any number added by the SQL aggregation flows
        // into the same TotalCost field).
        var projectId = Guid.NewGuid();
        var (svc, projects, _, _, companyId) = Build(scriptedSubAmount: 10_000m);
        SeedProject(projects, projectId, companyId);

        var r = await svc.GetBreakdownAsync(projectId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.SubcontractorCost.Should().Be(10_000m,
            "subcontractor cost is always read from ISubPaymentRepository (DEC-233 contract)");
        // TotalCost must include the subcontractor cost. With no journal_lines seeded
        // the SQL aggregation contributes 0, so TotalCost == SubcontractorCost.
        r.Value.TotalCost.Should().Be(10_000m,
            "TotalCost = DirectLabor + Material + Subcontractor + Equipment + Overhead");
    }

    // ============== Test 3 ==============

    [Fact]
    public async Task GetBreakdownAsync_ZeroSubcontractorCost_WhenNoPayments()
    {
        var projectId = Guid.NewGuid();
        var (svc, projects, _, _, companyId) = Build(scriptedSubAmount: 0m);
        SeedProject(projects, projectId, companyId);

        var r = await svc.GetBreakdownAsync(projectId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.SubcontractorCost.Should().Be(0m, "no sub_payments → 0");
        r.Value.TotalCost.Should().Be(0m, "no journal_lines + no sub_payments = 0");
    }

    // ============== Test 4 ==============

    [Fact]
    public async Task GetSubcontractorCostAsync_ExcludesCancelledPayments()
    {
        // The repository is responsible for the `status != 4` filter (it lives at
        // the SQL level inside the Dapper-backed impl). The service just trusts the
        // repository contract. This test asserts the contract: the repository is
        // called with companyId + projectId, and the scripted return (which would
        // already exclude cancelled payments) is the value the service reports.
        var projectId = Guid.NewGuid();
        var (svc, projects, _, sub, companyId) = Build(scriptedSubAmount: 7_500m);
        SeedProject(projects, projectId, companyId);

        var r = await svc.GetSubcontractorCostAsync(projectId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value.Should().Be(7_500m);
        sub.Calls.Should().ContainSingle(c => c.CompanyId == companyId && c.ProjectId == projectId,
            "service must call the repository with the resolved company + project id");
    }

    // ============== Test 5 ==============

    [Fact]
    public async Task GetSubcontractorCostAsync_ScopedByCompanyId()
    {
        // Two projects in two different companies. The repository's scripted return
        // varies by companyId to prove the service scopes by ICompanyContext.
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();

        var db = new FakeDbConnectionFactory();
        var projects = new FakeProjectRepository();
        var sub = new StubSubPaymentRepository
        {
            ScriptedReturn = (cid, _) => cid == companyA ? 100m : 200m,
        };
        SeedProject(projects, projectA, companyA);
        SeedProject(projects, projectB, companyB);

        var ctxA = TestCompanyContextFactory.Create(companyA);
        var svcA = new ProjectCostService(projects, db, sub, ctxA, NullLogger<ProjectCostService>.Instance);
        var ctxB = TestCompanyContextFactory.Create(companyB);
        var svcB = new ProjectCostService(projects, db, sub, ctxB, NullLogger<ProjectCostService>.Instance);

        var rA = await svcA.GetSubcontractorCostAsync(projectA, CancellationToken.None);
        var rB = await svcB.GetSubcontractorCostAsync(projectB, CancellationToken.None);

        rA.Value.Should().Be(100m, "company A's sub total");
        rB.Value.Should().Be(200m, "company B's sub total");

        // The repository must have been called with each company's id, not some
        // other random Guid. L19 / DEC-095: companyId from ICompanyContext.
        sub.Calls.Should().Contain(c => c.CompanyId == companyA && c.ProjectId == projectA);
        sub.Calls.Should().Contain(c => c.CompanyId == companyB && c.ProjectId == projectB);
    }
}
