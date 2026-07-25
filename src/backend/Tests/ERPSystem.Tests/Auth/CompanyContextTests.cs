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
}
