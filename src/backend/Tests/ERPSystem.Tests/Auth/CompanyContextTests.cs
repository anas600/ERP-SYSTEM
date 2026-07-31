// Sprint 10 (Phase 3) — CompanyContext tests rewritten for scoped DI.
//
// Before Phase 3: CompanyContext used AsyncLocal<CompanyHolder> for storage.
// Tests called `new CompanyContext()` (parameterless) and Set() / Clear() directly.
// AsyncLocal was global per execution context, so tests worked without HttpContext.
//
// After Phase 3: CompanyContext stores in HttpContext.Items via IHttpContextAccessor.
// Tests now build a DefaultHttpContext + Mock<IHttpContextAccessor> and verify
// that values written via Set() are visible via the property getters.
//
// Why this matters:
//   - The previous "AsyncLocal_DoesNotLeakAcrossTasks" test was testing an
//     implementation detail (AsyncLocal isolation), not a behavior contract.
//   - The new "Scoped_DoesNotLeakAcrossHttpContexts" test verifies the
//     BEHAVIOR we actually care about: one request's company does not leak
//     into another request's company.

using ERPSystem.Shared.CompanyContext;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Auth;

public class CompanyContextTests
{
    /// <summary>
    /// Helper: build a CompanyContext backed by a fresh DefaultHttpContext.
    /// Each test gets its own HttpContext (independent storage).
    /// </summary>
    private static (CompanyContext ctx, DefaultHttpContext http) Build()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);
        return (new CompanyContext(accessor.Object), http);
    }

    [Fact]
    public void Default_IsResolved_False()
    {
        var (ctx, _) = Build();
        ctx.IsResolved.Should().BeFalse();
        ctx.CompanyId.Should().BeNull();
        ctx.UserId.Should().BeNull();
        ctx.CompanyIds.Should().BeEmpty();
    }

    [Fact]
    public void Set_ThenRead_ReturnsValues()
    {
        var (ctx, _) = Build();
        var cid = Guid.NewGuid();
        var uid = Guid.NewGuid();
        var ids = new[] { cid, Guid.NewGuid() };

        ctx.Set(cid, uid, ids);

        ctx.CompanyId.Should().Be(cid);
        ctx.UserId.Should().Be(uid);
        ctx.CompanyIds.Should().BeEquivalentTo(ids);
        ctx.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesValues()
    {
        var (ctx, _) = Build();
        ctx.Set(Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid() });
        ctx.Clear();

        ctx.IsResolved.Should().BeFalse();
        ctx.CompanyId.Should().BeNull();
    }

    [Fact]
    public void Scoped_DoesNotLeakAcrossHttpContexts()
    {
        // Two separate request contexts (like two incoming HTTP requests).
        // Setting in one must NOT be visible in the other — this is the
        // request-isolation contract that AsyncLocal gave us, now provided
        // by HttpContext.Items.
        var httpA = new DefaultHttpContext();
        var httpB = new DefaultHttpContext();

        var accessorA = new Mock<IHttpContextAccessor>();
        accessorA.Setup(a => a.HttpContext).Returns(httpA);
        var ctxA = new CompanyContext(accessorA.Object);

        var accessorB = new Mock<IHttpContextAccessor>();
        accessorB.Setup(a => a.HttpContext).Returns(httpB);
        var ctxB = new CompanyContext(accessorB.Object);

        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        ctxA.Set(companyA, Guid.NewGuid(), new[] { companyA });
        ctxB.Set(companyB, Guid.NewGuid(), new[] { companyB });

        ctxA.CompanyId.Should().Be(companyA, "request A keeps its own value");
        ctxB.CompanyId.Should().Be(companyB, "request B keeps its own value");
    }

    [Fact]
    public void Clear_OnlyAffectsCurrentHttpContext()
    {
        // Defensive: Clear on one request must not affect another request's
        // HttpContext.Items. (HttpContext.Items is per-request, so this is
        // a behavior contract, not a side effect we need to worry about.)
        var httpA = new DefaultHttpContext();
        var httpB = new DefaultHttpContext();

        var accessorA = new Mock<IHttpContextAccessor>();
        accessorA.Setup(a => a.HttpContext).Returns(httpA);
        var accessorB = new Mock<IHttpContextAccessor>();
        accessorB.Setup(a => a.HttpContext).Returns(httpB);

        var ctxA = new CompanyContext(accessorA.Object);
        var ctxB = new CompanyContext(accessorB.Object);

        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        ctxA.Set(companyA, Guid.NewGuid(), new[] { companyA });
        ctxB.Set(companyB, Guid.NewGuid(), new[] { companyB });

        ctxA.Clear();

        ctxA.IsResolved.Should().BeFalse("request A was cleared");
        ctxB.CompanyId.Should().Be(companyB, "request B unaffected by A's Clear");
    }

    [Fact]
    public void Set_WithNullHttpContext_DoesNotThrow()
    {
        // Background work (e.g. HostedService, BackgroundService) has no
        // HttpContext. Set() must not throw — the context just stays empty
        // for that scope.
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        var ctx = new CompanyContext(accessor.Object);

        var act = () => ctx.Set(Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid() });

        act.Should().NotThrow();
        ctx.IsResolved.Should().BeFalse("no HttpContext means no Items to write to");
    }

    // ============== Cycle 2 (Phase 6.2) New Tests ==============

    /// <summary>
    /// Cycle 2 / T8: User can only access companies they are assigned to via
    /// user_companies (Phase 6 Multi-Company model). The CompanyContext's
    /// CompanyIds list is the source of truth for "what can this user see".
    /// </summary>
    [Fact]
    public void UserCompany_Limits_Access_To_Assigned_Companies()
    {
        var (ctx, _) = Build();
        var holdingId = Guid.NewGuid();
        var subA = Guid.NewGuid();
        var subB = Guid.NewGuid();
        var notAssigned = Guid.NewGuid();   // not in the list

        var assigned = new[] { holdingId, subA, subB };
        ctx.Set(holdingId, Guid.NewGuid(), assigned);

        // The 3 assigned companies should be visible
        ctx.CompanyIds.Should().Contain(holdingId);
        ctx.CompanyIds.Should().Contain(subA);
        ctx.CompanyIds.Should().Contain(subB);

        // An unassigned company must NOT be in the list
        ctx.CompanyIds.Should().NotContain(notAssigned);
        ctx.CompanyIds.Count.Should().Be(3);
    }

    /// <summary>
    /// Cycle 2 / T8: User switches active company via CompanySwitcher UI.
    /// The Set() call must update CompanyId, while CompanyIds (the list of
    /// allowed companies) stays the same. The middleware is what enforces
    /// that the new CompanyId is in CompanyIds.
    /// </summary>
    [Fact]
    public void CompanySwitcher_Switches_Active_Company_In_Context()
    {
        var (ctx, _) = Build();
        var uid = Guid.NewGuid();
        var holding = Guid.NewGuid();
        var subA = Guid.NewGuid();
        var subB = Guid.NewGuid();
        var allowed = new[] { holding, subA, subB };

        // Initial state: active = holding
        ctx.Set(holding, uid, allowed);
        ctx.CompanyId.Should().Be(holding);
        ctx.CompanyIds.Should().BeEquivalentTo(allowed);

        // Switch to subA (simulating CompanySwitcher click)
        ctx.Set(subA, uid, allowed);
        ctx.CompanyId.Should().Be(subA, "active company changed via Set()");
        ctx.CompanyIds.Should().BeEquivalentTo(allowed, "allowed list unchanged on switch");

        // Switch to subB
        ctx.Set(subB, uid, allowed);
        ctx.CompanyId.Should().Be(subB);
        ctx.CompanyIds.Should().BeEquivalentTo(allowed);

        // Switch back to holding
        ctx.Set(holding, uid, allowed);
        ctx.CompanyId.Should().Be(holding);
    }

    /// <summary>
    /// Cycle 2 / T8: Switching to a company NOT in the user's assigned list
    /// is the responsibility of the middleware (returns 403). The context
    /// itself is a passive holder — but we test the contract: if Set() is
    /// called with a companyId outside CompanyIds, the context still holds
    /// it (the middleware's job to validate upstream).
    /// This documents the boundary clearly.
    /// </summary>
    [Fact]
    public void CompanySwitcher_CanSet_OutOfListCompany_DocumentedAsMiddlewareResponsibility()
    {
        var (ctx, _) = Build();
        var holding = Guid.NewGuid();
        var subA = Guid.NewGuid();
        var rogue = Guid.NewGuid();   // not in allowed list
        var allowed = new[] { holding, subA };

        ctx.Set(holding, Guid.NewGuid(), allowed);
        // Pretend the middleware BUG allowed this through (it should reject with 403)
        ctx.Set(rogue, Guid.NewGuid(), allowed);

        // Context holds it (passive) — middleware MUST reject upstream
        ctx.CompanyId.Should().Be(rogue);
        ctx.CompanyIds.Should().NotContain(rogue, "the middleware is what enforces the check, not the context");
    }

    /// <summary>
    /// Sprint 10 Phase 3: a second request that runs while the first is
    /// still in flight (Task.WhenAll, parallel requests) must not see the
    /// first request's company. This is the AsyncLocal property we
    /// inherited, now provided by HttpContext.Items.
    /// </summary>
    [Fact]
    public async Task ParallelHttpContexts_DoNotLeakCompany()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        Guid? readA = null;
        Guid? readB = null;

        var tA = Task.Run(() =>
        {
            var (ctx, _) = Build();
            ctx.Set(companyA, Guid.NewGuid(), new[] { companyA });
            readA = ctx.CompanyId;
        });
        var tB = Task.Run(() =>
        {
            var (ctx, _) = Build();
            ctx.Set(companyB, Guid.NewGuid(), new[] { companyB });
            readB = ctx.CompanyId;
        });

        await Task.WhenAll(tA, tB);
        readA.Should().Be(companyA, "task A sees its own company");
        readB.Should().Be(companyB, "task B sees its own company");
    }
}
