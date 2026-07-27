using ERPSystem.Shared.MultiTenancy;
using FluentAssertions;
using Xunit;

namespace ERPSystem.Tests.Auth;

public class CompanyContextTests
{
    [Fact]
    public void Default_IsResolved_False()
    {
        var ctx = new CompanyContext();
        ctx.IsResolved.Should().BeFalse();
        ctx.CompanyId.Should().BeNull();
        ctx.UserId.Should().BeNull();
        ctx.CompanyIds.Should().BeEmpty();
    }

    [Fact]
    public void Set_ThenRead_ReturnsValues()
    {
        var ctx = new CompanyContext();
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
        var ctx = new CompanyContext();
        ctx.Set(Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid() });
        ctx.Clear();

        ctx.IsResolved.Should().BeFalse();
        ctx.CompanyId.Should().BeNull();
    }

    [Fact]
    public async Task AsyncLocal_DoesNotLeakAcrossTasks()
    {
        var ctx = new CompanyContext();
        var company1 = Guid.NewGuid();
        var company2 = Guid.NewGuid();
        Guid? t1Read = null;
        Guid? t2Read = null;

        var t1 = Task.Run(() =>
        {
            ctx.Set(company1, Guid.NewGuid(), new[] { company1 });
            t1Read = ctx.CompanyId;   // must see its own value, not t2's
        });
        var t2 = Task.Run(() =>
        {
            ctx.Set(company2, Guid.NewGuid(), new[] { company2 });
            t2Read = ctx.CompanyId;
        });

        await Task.WhenAll(t1, t2);
        t1Read.Should().Be(company1);
        t2Read.Should().Be(company2);
        ctx.CompanyId.Should().BeNull();   // main thread unaffected by either task
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
        var ctx = new CompanyContext();
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
        var ctx = new CompanyContext();
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
        var ctx = new CompanyContext();
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
}
