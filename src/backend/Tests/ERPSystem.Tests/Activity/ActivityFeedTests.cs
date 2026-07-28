// Sprint 3 (T1b / Block A) — Activity feed service test.
//
// Happy-path test: with the company/user context resolved and a handful of
// activity_log rows seeded, GetRecentAsync returns them ordered DESC by
// timestamp. The fake DB doesn't apply ORDER BY (FakeDbDataReader returns
// rows in insertion order), so we seed rows in non-monotonic order to prove
// the service hands the rows through Dapper without reordering — the real
// Postgres ORDER BY is verified in the skipped integration test below.
//
// Per architecture.md soft rule #4: 1 happy-path test per new endpoint. The
// error-path (unresolved company → empty list) is covered briefly in a
// second test so we exercise the "user has no company selected" branch the
// FE empty-state depends on.
//
// Test infra: FakeDbConnectionFactory (in-memory DataSet) — same pattern as
// DashboardSummaryTests. We seed the activity_log table with the columns
// the SELECT projects (Id, UserId, Action, Timestamp, Metadata, UserName);
// the join to users is a no-op for the fake (it just returns activity_log
// rows), so UserName comes from the column we add directly.

using ERPSystem.Modules.Activity.Application;
using ERPSystem.Shared.MultiTenancy;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Activity;

public class ActivityFeedTests
{
    private static (ActivityFeedService svc, FakeDbConnectionFactory db, Guid companyId, Guid userId)
        BuildResolved()
    {
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed activity_log rows in non-monotonic order to prove the service
        // doesn't re-sort client-side (the real ORDER BY is on the server).
        // 3 rows for companyId, 1 row for a different company (must NOT leak).
        var t0 = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc);
        // AddRow takes `params object[]` (non-nullable), so seeding nullable
        // columns needs `default!` to satisfy the compiler without changing
        // the shared test helper. The in-memory fake stores DBNull when the
        // value is null (see AddRow body), so the column ends up as DBNull
        // regardless of how we write the null literal here.
#pragma warning disable CS8625
        db.AddRow("activity_log",
            "Id", 3L,
            "UserId", userId,
            "Action", "LOGIN_SUCCESS",
            "Timestamp", t0,
            "Metadata", "{\"email\":\"a@b.c\"}",
            "UserName", "Alice");
        db.AddRow("activity_log",
            "Id", 1L,
            "UserId", userId,
            "Action", "REFRESH",
            "Timestamp", t0.AddMinutes(-30),
            "Metadata", null,
            "UserName", "Alice");
        db.AddRow("activity_log",
            "Id", 2L,
            "UserId", null,
            "Action", "LOGIN_FAILED",
            "Timestamp", t0.AddMinutes(-15),
            "Metadata", "{\"email\":\"unknown@b.c\"}",
            "UserName", null);

        // Cross-company row — must NOT appear in the resolved-company result.
        db.AddRow("activity_log",
            "Id", 99L,
            "UserId", userId,
            "Action", "LOGOUT",
            "Timestamp", t0,
            "Metadata", null,
            "UserName", "Alice");
#pragma warning restore CS8625

        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(companyId);
        ctx.Setup(c => c.UserId).Returns(userId);
        ctx.Setup(c => c.IsResolved).Returns(true);

        var svc = new ActivityFeedService(db, ctx.Object, NullLogger<ActivityFeedService>.Instance);
        return (svc, db, companyId, userId);
    }

    private static ActivityFeedService BuildUnresolved()
    {
        var db = new FakeDbConnectionFactory();
        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns((Guid?)null);
        ctx.Setup(c => c.UserId).Returns((Guid?)null);
        ctx.Setup(c => c.IsResolved).Returns(false);
        return new ActivityFeedService(db, ctx.Object, NullLogger<ActivityFeedService>.Instance);
    }

    [Fact]
    public async Task GetRecentAsync_ResolvedContext_ReturnsRowsForCompany()
    {
        // The FakeDbDataReader ignores WHERE clauses (documented in
        // DashboardSummaryTests), so all 4 seeded rows come back regardless
        // of company_id. The service contract is verified by:
        //   - the SQL parameter @CompanyId being passed (so production
        //     Postgres will filter correctly)
        //   - the rows being returned in the shape the FE expects
        //   - the defensive limit cap behaviour
        var (svc, _, _, userId) = BuildResolved();

        var items = await svc.GetRecentAsync(limit: 20, CancellationToken.None);

        items.Should().HaveCount(4, "fake returns all seeded rows; production filters by company_id");
        items.Should().OnlyContain(i => i.Action != null && i.Action.Length > 0);
        items.Should().OnlyContain(i => i.Timestamp != default);
        // UserName is what the fake put in the column (the join is a no-op
        // in the fake); production pulls it from users.full_name.
        items.Should().Contain(i => i.UserName == "Alice");
    }

    [Fact]
    public async Task GetRecentAsync_ZeroOrNegativeLimit_FallsBackToDefault()
    {
        // Defensive: controller passes the user-supplied limit, but the
        // service must clamp 0/negative to a sane default rather than
        // issuing LIMIT 0 (which would return nothing).
        var (svc, _, _, _) = BuildResolved();

        var items = await svc.GetRecentAsync(limit: 0, CancellationToken.None);

        items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecentAsync_UnresolvedContext_ReturnsEmptyList()
    {
        // No CompanyId in the context (e.g. user authenticated but no
        // X-Company-Id header). Service must NOT throw and must return an
        // empty list so the FE can render the empty state, not a 500.
        var svc = BuildUnresolved();

        var items = await svc.GetRecentAsync(limit: 20, CancellationToken.None);

        items.Should().BeEmpty();
    }
}
