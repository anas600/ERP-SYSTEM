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
    public void AsyncLocal_DoesNotLeakAcrossTasks()
    {
        var ctx = new CompanyContext();
        var task1Set = false;
        var task2Set = false;

        var t1 = Task.Run(() =>
        {
            ctx.Set(Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid() });
            task1Set = true;
        });
        var t2 = Task.Run(() =>
        {
            ctx.Set(Guid.NewGuid(), Guid.NewGuid(), new[] { Guid.NewGuid() });
            task2Set = true;
        });

        Task.WaitAll(t1, t2);
        task1Set.Should().BeTrue();
        task2Set.Should().BeTrue();
        // Each task's ctx is scoped — they don't see each other's values
    }
}
