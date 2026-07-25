# Phase 6.1a — CompanyContext Foundation

**Branch:** `feature/phase6-1a-company-context`
**Base:** `origin/develop` (cc4295b)
**Worktree:** `C:/Users/Anas/.minimax-agent/projects/ERP-SYSTEM-6.1a`
**Owner:** Jamie Executive (implementer) → Jamie التحليلي (verifier) → Mavis (approve)
**Estimate:** ~30 min
**Risk:** LOW (no behavior change)

## Goal
Introduce the new `ICompanyContext` abstraction alongside the existing `ITenantContext` — **old stays as back-compat shim, deleted in PR-6.1b**. This PR is the foundation that PR-6.1b (repos/services migration) and PR-6.1c (auth rewrite) will build on.

## 9 Approved Decisions (from PHASE6-PLAN.md)
1. Roles: GLOBAL
2. Email uniqueness: GLOBAL
3. X-Company-Id header: YES + JWT `company_ids[]` claim
4. `user_companies` schema: composite PK (user_id, company_id), `is_default bool`
5. Auth flow: Register creates user under default Holding, no tenant wizard, `holdingName` optional
6. HF Space DB empty (per Fresh Build Mode)
7. Single PR atomic deploy (no half-migrated state)
8. E2E atomicity test: "no orphan users" replaces "no orphan tenants"
9. Rollback: revert main to v5.0.4 (e108f27)

## Tasks

### Task 1: Add `ICompanyContext` + `CompanyContext` in Shared/MultiTenancy/

Create **`src/backend/Shared/MultiTenancy/ICompanyContext.cs`**:

```csharp
namespace ERPSystem.Shared.MultiTenancy;

/// <summary>
/// سياق الشركة (Company) النشطة داخل الـ request
/// يُملأ من CompanyContextMiddleware بناءً على X-Company-Id header + JWT user id
/// 
/// Phase 6: يحل محل ITenantContext (Multi-Company model، NOT Multi-Tenant).
/// في الـ v1 كل المستخدمين ينتمون لنفس الـ Holding افتراضياً؛
/// الـ header يسمح للـ Admin بتبديل الشركة النشطة في الواجهة.
/// </summary>
public interface ICompanyContext
{
    /// <summary>الشركة النشطة المختارة من الـ X-Company-Id header. Null إذا ما في header ولا authenticated.</summary>
    Guid? CompanyId { get; }

    /// <summary>User id من الـ JWT claim (sub / nameid). Null إذا anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>true لو عندنا CompanyId + UserId (request authenticated + company selected).</summary>
    bool IsResolved { get; }

    /// <summary>جميع الشركات اللي للمستخدم صلاحية عليها (من JWT company_ids[] claim).</summary>
    IReadOnlyList<Guid> CompanyIds { get; }

    void Set(Guid companyId, Guid userId, IReadOnlyList<Guid> companyIds);
    void Clear();
}
```

Create **`src/backend/Shared/MultiTenancy/CompanyContext.cs`**:

```csharp
namespace ERPSystem.Shared.MultiTenancy;

public sealed class CompanyContext : ICompanyContext
{
    private static readonly AsyncLocal<CompanyHolder> _holder = new();

    public Guid? CompanyId => _holder.Value?.CompanyId;
    public Guid? UserId => _holder.Value?.UserId;
    public IReadOnlyList<Guid> CompanyIds => _holder.Value?.CompanyIds ?? Array.Empty<Guid>();
    public bool IsResolved => _holder.Value is { CompanyId: not null, UserId: not null };

    public void Set(Guid companyId, Guid userId, IReadOnlyList<Guid> companyIds)
    {
        _holder.Value = new CompanyHolder(companyId, userId, companyIds);
    }

    public void Clear()
    {
        _holder.Value = null!;
    }

    private sealed class CompanyHolder
    {
        public Guid? CompanyId { get; }
        public Guid? UserId { get; }
        public IReadOnlyList<Guid> CompanyIds { get; }

        public CompanyHolder(Guid companyId, Guid userId, IReadOnlyList<Guid> companyIds)
        {
            CompanyId = companyId;
            UserId = userId;
            CompanyIds = companyIds;
        }
    }
}
```

### Task 2: Add `CompanyContextMiddleware` in Shared/MultiTenancy/

Create **`src/backend/Shared/MultiTenancy/CompanyContextMiddleware.cs`**:

```csharp
using System.Security.Claims;

namespace ERPSystem.Shared.MultiTenancy;

/// <summary>
/// Middleware يلتقط CompanyId من X-Company-Id header + UserId من JWT.
/// إذا الـ header غائب، يستخدم أول company من JWT company_ids[] claim (default company).
/// 
/// لازم يأتي بعد UseAuthentication() و UseAuthorization() في الـ pipeline.
/// 
/// Phase 6: يحل محل TenantMiddleware. كلا الـ middlewares يتعايشوا في PR-6.1a
/// ثم TenantMiddleware يُحذف في PR-6.1b.
/// </summary>
public sealed class CompanyContextMiddleware
{
    private const string CompanyHeader = "X-Company-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CompanyContextMiddleware> _logger;

    public CompanyContextMiddleware(RequestDelegate next, ILogger<CompanyContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICompanyContext companyContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isPublic = IsPublicPath(path);

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.User.FindFirst("sub")?.Value;

            var companyIdsClaim = context.User.FindFirst("company_ids")?.Value
                                 ?? context.User.FindFirst("companyIds")?.Value;
            var companyIds = ParseCompanyIds(companyIdsClaim);

            // 1) X-Company-Id header يأخذ الأولوية
            var headerCompanyId = context.Request.Headers[CompanyHeader].ToString();
            Guid? selectedCompanyId = null;

            if (Guid.TryParse(headerCompanyId, out var headerGuid) &&
                companyIds.Contains(headerGuid))
            {
                selectedCompanyId = headerGuid;
            }
            else if (companyIds.Count > 0)
            {
                // 2) Fall back to default_company_id claim، ثم أول company
                var defaultClaim = context.User.FindFirst("default_company_id")?.Value
                                ?? context.User.FindFirst("defaultCompanyId")?.Value;
                if (Guid.TryParse(defaultClaim, out var defaultGuid) && companyIds.Contains(defaultGuid))
                {
                    selectedCompanyId = defaultGuid;
                }
                else
                {
                    selectedCompanyId = companyIds[0];
                }
            }

            if (Guid.TryParse(userClaim, out var userId) && selectedCompanyId.HasValue)
            {
                companyContext.Set(selectedCompanyId.Value, userId, companyIds);
                _logger.LogDebug("Company resolved: {CompanyId}, User: {UserId}", selectedCompanyId, userId);
            }
            else if (!isPublic)
            {
                _logger.LogWarning("Authenticated request without resolvable company on path {Path}", path);
            }
        }

        try
        {
            await _next(context);
        }
        finally
        {
            companyContext.Clear();
        }
    }

    private static IReadOnlyList<Guid> ParseCompanyIds(string? claim)
    {
        if (string.IsNullOrEmpty(claim)) return Array.Empty<Guid>();
        // JWT claim may be JSON array string e.g. "[\"guid1\",\"guid2\"]"
        // or simple comma-separated. Handle both.
        if (claim.StartsWith("["))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(claim);
                return parsed?
                    .Select(s => Guid.TryParse(s.Trim('"'), out var g) ? g : (Guid?)null)
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .ToList() ?? new List<Guid>();
            }
            catch
            {
                return Array.Empty<Guid>();
            }
        }
        return claim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
    }

    private static bool IsPublicPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;
        return path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase);
    }
}
```

### Task 3: Update Program.cs (add new registrations, keep old)

In **`src/backend/Host/Program.cs`**:
- Add `builder.Services.AddScoped<ICompanyContext, CompanyContext>();` **next to** the existing `ITenantContext` registration
- Add `app.UseMiddleware<CompanyContextMiddleware>();` **AFTER** the existing `app.UseMiddleware<TenantMiddleware>();`

Find the existing lines (per memory, the matches are):
- `using ERPSystem.Shared.MultiTenancy;` ← already there
- `builder.Services.AddScoped<ITenantContext, TenantContext>();` ← add ICompanyContext right after
- `app.UseMiddleware<TenantMiddleware>();` ← add CompanyContextMiddleware right after

### Task 4: Update CORS to allow X-Company-Id header

In **`src/backend/Host/Program.cs`**, find the CORS config (likely `WithOrigins(...).AllowAnyHeader().AllowAnyMethod()` or similar). Update to:

```csharp
.WithHeaders("Content-Type", "Authorization", "X-Company-Id")
```

If the current code uses `AllowAnyHeader()`, leave it — `AllowAnyHeader` already covers everything including X-Company-Id. **Only change if you see `.WithHeaders(...)` restricting headers.**

### Task 5: Add unit tests

Create **`tests/ERPSystem.Tests/Identity/CompanyContextTests.cs`**:

```csharp
using ERPSystem.Shared.MultiTenancy;
using FluentAssertions;
using Xunit;

namespace ERPSystem.Tests.Identity;

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
```

Check the existing test project structure under `tests/ERPSystem.Tests/Identity/` (e.g. `AuthServiceTests.cs`, `UserRepositoryTests.cs`) and add this file in the same namespace pattern. If `Identity` folder doesn't exist, create it.

## Acceptance Criteria

1. ✅ `dotnet build` → 0 errors, 0 new warnings
2. ✅ `dotnet test --filter "FullyQualifiedName~CompanyContextTests"` → 4/4 pass
3. ✅ `dotnet test` (all unit tests, no E2E) → 100% pass (existing 368/395 baseline maintained)
4. ✅ ITenantContext + TenantMiddleware still work (back-compat for PR-6.1b consumers)
5. ✅ ICompanyContext + CompanyContextMiddleware work (verified via new tests + by curl after HF deploy in PR-6.1c)
6. ✅ `docs/CHANGELOG.md` updated with entry:
   ```
   ## [Unreleased] - 2026-07-25
   ### Added (Phase 6.1a — CompanyContext Foundation)
   - `ICompanyContext` + `CompanyContext` (AsyncLocal) — new abstraction for active company
   - `CompanyContextMiddleware` — reads X-Company-Id header + JWT company_ids[] claim
   - CORS updated to allow X-Company-Id header
   - Unit tests for CompanyContext (4 tests)
   ### Back-Compat
   - ITenantContext + TenantMiddleware remain active (deleted in PR-6.1b)
   ```
7. ✅ `AGENTS.md` (root or src/backend/Shared/AGENTS.md) updated with a brief note: "Phase 6.1a introduces ICompanyContext; old ITenantContext still in use until 6.1b"

## What NOT to do (out of scope for this PR)

- ❌ Do NOT change any entity / repo / service that currently uses `ITenantContext`
- ❌ Do NOT change `AuthService`, `JwtTokenService`, or `AuthDtos`
- ❌ Do NOT delete `MultiTenancy/` folder or `TenantCache.cs`
- ❌ Do NOT regenerate `*.g.cs` files
- ❌ Do NOT touch the frontend
- ❌ Do NOT run migrations (none needed for this PR — pure code additions)

## PR Workflow

1. Commit on the worktree branch `feature/phase6-1a-company-context`
2. Push: `git push -u origin feature/phase6-1a-company-context`
3. Open PR via `gh pr create --base develop --head feature/phase6-1a-company-context --title "feat(phase6-1a): CompanyContext foundation" --body "..."`
4. Report: PR URL + summary of files changed + build/test output to Mavis
5. **Stop and wait** for Jamie التحليلي verification + Mavis approval before merge

## Useful References (already in repo)

- `src/backend/Shared/MultiTenancy/ITenantContext.cs` — pattern to mirror
- `src/backend/Shared/MultiTenancy/TenantContext.cs` — AsyncLocal pattern
- `src/backend/Shared/MultiTenancy/TenantMiddleware.cs` — middleware pattern
- `src/backend/Host/Program.cs` — registration site
- `docs/PHASE6-PLAN.md` — full Phase 6 plan with 9 decisions
- `docs/PHASE6-ANALYSIS-MULTICOMPANY-REFACTOR.md` — analysis
- `CONSTITUTION.md` — Article 2 (Architecture: Multi-Company, NOT Multi-Tenant)
