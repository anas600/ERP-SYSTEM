# 🧪 AGENTS.md — src/backend/Tests/

> **xUnit tests.** Read `/AGENTS.md`, `/src/AGENTS.md`, and `/src/backend/AGENTS.md` first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

Unit + integration tests for the backend. xUnit framework. Per Constitution Article 11: **One test per endpoint (smoke) is the standard.**

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي (QA) |
| **Approval** | Mavis Local (verify before PR) |

## Local Contracts

- **One test per endpoint** (smoke). Not full coverage.
- **Test naming:** `<ClassName>Tests` (e.g., `CompaniesListTests`).
- **Use `FluentAssertions`** for readable assertions.
- **Fake DB** (`FakeDbConnectionFactory`) for unit tests.
- **Real DB** (Supabase dev) only for integration tests.
- **No `tenant_id` in tests.** Use `company_id`.

## Work Guidance

### Adding a Test
1. Create in `src/backend/Tests/ERPSystem.Tests/<Module>/<ClassName>Tests.cs`.
2. Test one happy-path scenario.
3. Use `FakeDb` for unit tests, real DB for integration.
4. Name should describe what's tested, not what's mocked.

### Test Pattern
```csharp
public class CompaniesListTests
{
    [Fact]
    public async Task GetCompanies_ReturnsPaginatedList()
    {
        // Arrange: setup FakeDb with seed data
        // Act: call the API
        // Assert: verify response shape
    }
}
```

## Verification

- [ ] `dotnet test` — all green.
- [ ] One test per endpoint.
- [ ] No flaky tests.
- [ ] No `tenant_id`: `grep -r "tenant" src/backend/Tests/`.

## Child DOX Index

| Path | Scope | Status |
|------|-------|--------|
| `src/backend/Tests/ERPSystem.Tests/` | All test classes | Active |

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
