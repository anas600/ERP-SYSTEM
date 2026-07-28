// Sprint 2 (T6 / Block A) — User → Companies access tests.
//
// Two tests, one happy + one error path (per architecture.md soft rule #4):
//
// 1. Happy path — UserRepository.GetUserCompaniesAsync returns 2 companies
//    for a user with 2 user_companies rows. The order is "is_default DESC,
//    code ASC" (per the SQL in UserRepository.GetUserCompaniesAsync).
//
// 2. Error path — GetByIdAsync(Guid.Empty) returns null (user not found).
//    The controller uses this to return 404 from GET /api/users/{id}/companies
//    when the user does not exist. We test the repository contract
//    directly; the controller-level 404 mapping is a one-line check.
//
// Test approach: same FakeDbConnectionFactory pattern as the rest of
// the suite. The UserRepository reads via the FakeDbDataReader, which
// extracts the first FROM/JOIN table name. For the user_companies query
// (which joins on companies), the FROM table is user_companies — so
// we seed both tables with the right rows so the result is meaningful.

using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Xunit;

namespace ERPSystem.Tests.Auth;

public class UserCompanyAccessTests
{
    /// <summary>
    /// Happy path: a user with 2 user_companies rows gets 2 UserCompanyLink
    /// rows back from GetUserCompaniesAsync. The first row is the default
    /// (is_default = true) per the SQL ORDER BY is_default DESC.
    /// </summary>
    [Fact]
    public async Task GetUserCompaniesAsync_HappyPath_ReturnsBothAssignedCompanies()
    {
        var db = new FakeDbConnectionFactory();
        var userId = Guid.NewGuid();
        var holdingId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        // Seed the user so existence check (used by the controller) passes.
        SeedUser(db, userId, "admin@holding.local", "Admin");

        // Seed 2 companies. The query joins user_companies + companies and
        // FakeDb returns the user_companies rows (it picks the FIRST FROM
        // table name = "user_companies"). We need the join to be on
        // company_id, so we put the company_id in the user_companies row.
        // The "CompanyName" / "CompanyCode" columns are read from the JOIN
        // (companies table), but FakeDb doesn't actually do the join — it
        // just returns the FROM-table rows. The assertion here is on the
        // COUNT of the returned list (= 2), not on the joined columns.
        db.AddRow("user_companies",
            "user_id", userId,
            "company_id", holdingId,
            "is_default", true,
            "assigned_at", DateTime.UtcNow);
        db.AddRow("user_companies",
            "user_id", userId,
            "company_id", subId,
            "is_default", false,
            "assigned_at", DateTime.UtcNow);

        // Seed the companies themselves (the join target). Even though
        // FakeDb doesn't honor the join, we keep the in-memory tables
        // consistent so a future FakeDb improvement would just work.
        SeedCompany(db, holdingId, "HLD", "Holding Enterprise", isGroup: true);
        SeedCompany(db, subId, "SUB", "Demo Subsidiary", isGroup: false);

        var repo = new UserRepository(db);

        // Act
        var links = await repo.GetUserCompaniesAsync(userId, CancellationToken.None);

        // Assert
        links.Should().HaveCount(2, "the user is assigned to 2 companies via user_companies");
    }

    /// <summary>
    /// Error path: a user with no user_companies rows gets an empty list
    /// back. The controller treats this as a 200 with empty items (not a
    /// 404 — the user EXISTS, they just have no companies). The 404 is
    /// reserved for users that don't exist in the users table at all.
    /// </summary>
    [Fact]
    public async Task GetUserCompaniesAsync_NoAssignments_ReturnsEmpty()
    {
        var db = new FakeDbConnectionFactory();
        var userId = Guid.NewGuid();
        // User exists, but has no user_companies rows.
        SeedUser(db, userId, "lonely@holding.local", "Lonely User");

        var repo = new UserRepository(db);
        var links = await repo.GetUserCompaniesAsync(userId, CancellationToken.None);

        links.Should().BeEmpty("the user exists but has no user_companies rows");
    }

    /// <summary>
    /// Error path: GetByIdAsync(Guid.Empty) returns null. The controller's
    /// GET /api/users/{id}/companies uses this to distinguish 404 (user
    /// does not exist) from 200 (user exists with no companies). This test
    /// pins the repository contract that drives that 404 mapping.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull_DrivesController404()
    {
        var db = new FakeDbConnectionFactory();
        // No user seeded — an empty users table means GetById returns null.
        var repo = new UserRepository(db);

        var user = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        user.Should().BeNull("the users table is empty, so the lookup must return null (controller → 404)");
    }

    /// <summary>
    /// Bonus: a user with 2 companies sees the same 2 companies regardless
    /// of how many times we call GetUserCompaniesAsync. (Idempotency smoke
    /// test — no surprises in the data path.)
    /// </summary>
    [Fact]
    public async Task GetUserCompaniesAsync_CalledTwice_ReturnsSameResults()
    {
        var db = new FakeDbConnectionFactory();
        var userId = Guid.NewGuid();
        SeedUser(db, userId, "repeat@holding.local", "Repeat");
        db.AddRow("user_companies",
            "user_id", userId,
            "company_id", Guid.NewGuid(),
            "is_default", true,
            "assigned_at", DateTime.UtcNow);
        db.AddRow("user_companies",
            "user_id", userId,
            "company_id", Guid.NewGuid(),
            "is_default", false,
            "assigned_at", DateTime.UtcNow);

        var repo = new UserRepository(db);
        var first = await repo.GetUserCompaniesAsync(userId, CancellationToken.None);
        var second = await repo.GetUserCompaniesAsync(userId, CancellationToken.None);

        first.Should().HaveCount(2);
        second.Should().HaveCount(2);
        first.Select(l => l.CompanyId).Should().BeEquivalentTo(second.Select(l => l.CompanyId),
            "the result is deterministic across calls (no hidden state in the repo)");
    }

    // ============ Test helpers ============

    private static void SeedUser(FakeDbConnectionFactory db, Guid id, string email, string fullName)
    {
        db.AddRow("users",
            "id", id,
            "email", email,
            "password_hash", "bcrypt-fake-hash",
            "full_name", fullName,
            "is_active", true,
            "two_factor_enabled", false,
            "is_deleted", false,
            "created_at", DateTime.UtcNow,
            "updated_at", DateTime.UtcNow);
    }

    private static void SeedCompany(FakeDbConnectionFactory db, Guid id, string code, string name, bool isGroup)
    {
        db.AddRow("companies",
            "id", id,
            "code", code,
            "name", name,
            "slug", code.ToLowerInvariant(),
            "legal_name", name,
            "parent_company_id", null,
            "is_group", isGroup,
            "base_currency", "LYD",
            "is_active", true,
            "created_at", DateTime.UtcNow,
            "updated_at", DateTime.UtcNow);
    }
}
