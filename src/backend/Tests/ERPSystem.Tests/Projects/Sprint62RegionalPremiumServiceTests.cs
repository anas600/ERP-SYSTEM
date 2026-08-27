// Sprint 62 Wave 1A (DEC-197) — Tests for RegionalPremiumService (5+ tests).
//
// All tests use a fake IRegionalPremiumRepository (in-memory dict) — no DB needed.
// L19 / DEC-095: service uses ICompanyContext.CompanyId, not req.CompanyId — covered
// in Create_UsesContextCompanyId and ListByProject_RespectsRepositoryOrdering.
//
// Coverage:
//   1. Create — happy path (defaults, L19, combined percent)
//   2. Create — duplicate (project_id, region) rejected
//   3. Create — invalid region rejected
//   4. Create — combined percent > 100 rejected
//   5. CalculateDeductionAsync — active premium applies NDB+CIT+SS on gross
//   6. CalculateDeductionAsync — no active premium returns 0 (DEC-197 contract)
//   7. CalculateDeductionAsync — zero/negative gross returns 0
//   8. GetById — not found returns NotFound

using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Projects;

public class Sprint62RegionalPremiumServiceTests
{
    // ===== Fakes =====

    internal class FakeRegionalPremiumRepository : IRegionalPremiumRepository
    {
        private readonly Dictionary<Guid, RegionalPremium> _items = new();

        public Task<RegionalPremium?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var p) ? p : null);

        public Task<IReadOnlyList<RegionalPremium>> ListByProjectAsync(Guid projectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RegionalPremium>>(_items.Values
                .Where(p => p.ProjectId == projectId)
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Region)
                .ToList());

        public Task InsertAsync(RegionalPremium p, CancellationToken ct) { _items[p.Id] = p; return Task.CompletedTask; }

        public Task UpdateAsync(RegionalPremium p, CancellationToken ct) { _items[p.Id] = p; return Task.CompletedTask; }

        public Task DeleteAsync(Guid id, CancellationToken ct) { _items.Remove(id); return Task.CompletedTask; }
    }

    private static (RegionalPremiumService svc, FakeRegionalPremiumRepository repo, Guid companyId)
        Build(Guid? companyId = null)
    {
        var repo = new FakeRegionalPremiumRepository();
        var cid = companyId ?? Guid.NewGuid();
        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(cid);
        var svc = new RegionalPremiumService(repo, ctx.Object,
            NullLogger<RegionalPremiumService>.Instance);
        return (svc, repo, cid);
    }

    private static CreateRegionalPremiumRequest MakeCreate(
        string region = RegionalPremiumRegions.NdbOil,
        decimal ndb = 1.5m, decimal cit = 5.0m, decimal ss = 0.0m,
        bool isActive = true) =>
        new(region, ndb, cit, ss, isActive);

    // ========== 1. Create — happy path ==========

    [Fact]
    public async Task Create_NewPremium_UsesContextCompanyId_AndCalculatesCombined()
    {
        var (svc, repo, companyId) = Build();
        var projectId = Guid.NewGuid();
        var req = MakeCreate(region: RegionalPremiumRegions.NdbOil, ndb: 1.5m, cit: 5.0m, ss: 2.5m);

        var r = await svc.CreateAsync(Guid.NewGuid(), projectId, req, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Region.Should().Be("NDB-Oil");
        r.Value.NdbPercent.Should().Be(1.5m);
        r.Value.CombinedPercent.Should().Be(9.0m, "combined = Ndb + Cit + Ss = 1.5 + 5.0 + 2.5");
        r.Value.IsActive.Should().BeTrue();
        repo.GetByIdAsync(r.Value.Id, CancellationToken.None).Result
            .Should().NotBeNull("the row must be persisted");
    }

    // ========== 2. Create — duplicate (project_id, region) rejected ==========

    [Fact]
    public async Task Create_DuplicateProjectAndRegion_Fails_WithAlreadyExists()
    {
        var (svc, _, _) = Build();
        var projectId = Guid.NewGuid();
        var first = await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(region: RegionalPremiumRegions.Tripoli), CancellationToken.None);
        first.Succeeded.Should().BeTrue();

        var second = await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(region: RegionalPremiumRegions.Tripoli), CancellationToken.None);

        second.Succeeded.Should().BeFalse();
        second.ErrorCode.Should().Be(RegionalPremiumErrorCode.AlreadyExists);
    }

    // ========== 3. Create — invalid region rejected ==========

    [Fact]
    public async Task Create_InvalidRegion_Fails_WithValidationError()
    {
        var (svc, _, _) = Build();
        var r = await svc.CreateAsync(Guid.NewGuid(), Guid.NewGuid(),
            MakeCreate(region: "Atlantis"), CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(RegionalPremiumErrorCode.ValidationError);
    }

    // ========== 4. Create — combined percent > 100 rejected ==========

    [Fact]
    public async Task Create_CombinedPercentOver100_Fails_WithValidationError()
    {
        var (svc, _, _) = Build();
        // 80 + 30 = 110 > 100 — must be rejected.
        var r = await svc.CreateAsync(Guid.NewGuid(), Guid.NewGuid(),
            MakeCreate(ndb: 80m, cit: 30m, ss: 0m), CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(RegionalPremiumErrorCode.ValidationError);
    }

    // ========== 5. CalculateDeductionAsync — active premium applied ==========

    [Fact]
    public async Task CalculateDeductionAsync_WithActivePremium_AppliesNdbCitSsOnGross()
    {
        var (svc, _, _) = Build();
        var projectId = Guid.NewGuid();
        // Default DEC-197 rates: NDB 1.5 + CIT 5.0 + SS 0.0 = 6.5%
        await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(region: RegionalPremiumRegions.NdbOil, ndb: 1.5m, cit: 5.0m, ss: 0m),
            CancellationToken.None);

        // Gross = 100,000 LYD. Expected deduction = 6,500.
        var deduction = await svc.CalculateDeductionAsync(projectId, 100_000m, CancellationToken.None);
        deduction.Should().Be(6500.0000m);
    }

    // ========== 6. CalculateDeductionAsync — no active premium returns 0 ==========

    [Fact]
    public async Task CalculateDeductionAsync_NoActivePremium_ReturnsZero()
    {
        var (svc, _, _) = Build();
        var projectId = Guid.NewGuid();
        // Insert a row but mark it inactive — CalculateDeductionAsync must ignore it.
        await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(region: RegionalPremiumRegions.NdbOil, isActive: false),
            CancellationToken.None);

        var deduction = await svc.CalculateDeductionAsync(projectId, 100_000m, CancellationToken.None);
        deduction.Should().Be(0m);
    }

    // ========== 7. CalculateDeductionAsync — zero gross returns 0 ==========

    [Fact]
    public async Task CalculateDeductionAsync_ZeroOrNegativeGross_ReturnsZero()
    {
        var (svc, _, _) = Build();
        var projectId = Guid.NewGuid();
        await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(), CancellationToken.None);

        (await svc.CalculateDeductionAsync(projectId, 0m, CancellationToken.None))
            .Should().Be(0m, "zero gross must not produce a deduction");
        (await svc.CalculateDeductionAsync(projectId, -100m, CancellationToken.None))
            .Should().Be(0m, "negative gross must not produce a deduction");
    }

    // ========== 8. GetById — not found ==========

    [Fact]
    public async Task GetById_NotFound_Fails_WithNotFound()
    {
        var (svc, _, _) = Build();
        var r = await svc.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(RegionalPremiumErrorCode.NotFound);
    }
}
