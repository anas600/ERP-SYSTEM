using System.Data;
using ERPSystem.Modules.Companies.Application.DTOs;
using ERPSystem.Modules.Companies.Entities;
using ERPSystem.Modules.Companies.Infrastructure;
using ERPSystem.Modules.Finance.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Companies.Application.Services;

public interface ICompanyService
{
    Task<CompanyResult<Company>> CreateHoldingAsync(string code, string name, string legalName, string baseCurrency, CancellationToken ct);
    Task<CompanyResult<Company>> CreateHoldingAsync(string code, string name, string legalName, string baseCurrency, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    // Sprint 2 (T3 / Block A): top-level create. parentCompanyId = null → root company.
    // Idempotent on `code`: returns the existing company (Succeeded=true, WasCreated=false)
    // if a row with the same code already exists, instead of failing. This matches
    // the dashboard UX where the user may re-submit the form after a flaky network.
    // On a fresh create, WasCreated=true so the controller can return 201 vs 200.
    Task<CompanyResult<CreateCompanyResult>> CreateAsync(CreateCompanyRequest req, CancellationToken ct);
    Task<CompanyResult<Company>> AddSubsidiaryAsync(Guid parentCompanyId, string code, string name, string? legalName, CancellationToken ct);
    Task<CompanyResult<Company>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CompanyResult<IReadOnlyList<Company>>> ListAsync(bool includeInactive, CancellationToken ct);
    // Sprint 2 (T1 / Block A): paged list. The `userId` (when non-null) restricts the
    // result to companies the user has access to via user_companies. When null, all
    // companies are returned (admin view).
    Task<CompanyResult<CompanyPage>> ListPagedAsync(int page, int pageSize, bool includeInactive, Guid? userId, CancellationToken ct);
    Task<CompanyResult<SubsidiaryListDto>> GetSubsidiariesAsync(Guid parentCompanyId, CancellationToken ct);
    // Sprint 11 T2 (BE Jimi): the FE contract expects a flat recursive DTO
    // (CompanyTreeNodeDto), not the legacy `CompanyTreeNode { Company, Children }`
    // wrapper. The Holding is the root (is_group=true AND parent_company_id IS NULL);
    // its children are the subsidiaries; each subsidiary may have its own children.
    // Returns the Holding's direct children so the FE can build a tree
    // (a Holding has exactly one row at the top by definition; the FE renders
    // it as a single root node).
    Task<CompanyResult<IReadOnlyList<CompanyTreeNodeDto>>> GetTreeAsync(CancellationToken ct);
    Task<CompanyResult<bool>> DeactivateAsync(Guid id, CancellationToken ct);
    // Sprint 1 (T2 / Block A): slug-based Holding lookup, returns Holding + child companies.
    Task<CompanyResult<HoldingDetail>> GetHoldingBySlugAsync(string slug, CancellationToken ct);
}

// Sprint 2 (T3 / Block A): request payload for the top-level POST /api/companies.
// parentCompanyId = null means "create as root company" (a Holding-like row that
// is not a group). The existing CreateHolding endpoint is a separate path that
// also seeds the default CoA; the top-level CreateAsync does NOT seed CoA
// (admin would explicitly set up a non-Holding company that doesn't need it).
public sealed class CreateCompanyRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string BaseCurrency { get; set; } = "LYD";
    public Guid? ParentCompanyId { get; set; }
}

// Sprint 2 (T3 / Block A): result wrapper for CreateAsync. WasCreated tells the
// controller whether to return 201 (newly created) or 200 (idempotent: a row
// with the same code already existed). The Company is always populated when
// Succeeded=true.
public sealed class CreateCompanyResult
{
    public Company Company { get; set; } = null!;
    public bool WasCreated { get; set; }
}

// Sprint 2 (T1 / Block A): paginated response shape — items + total + page + pageSize.
// Stable contract: page is 1-based (page=1 is the first page).
public sealed class CompanyPage
{
    public IReadOnlyList<Company> Items { get; set; } = Array.Empty<Company>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Sprint 1 (T2 / Block A): holding detail returned by /api/holdings/{slug}.
// The Holding is identified by (is_group=true, parent_company_id IS NULL).
// The companies list is the immediate children of the Holding.
public sealed class HoldingDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string BaseCurrency { get; set; } = "LYD";
    public bool IsActive { get; set; }
    public List<HoldingCompanySummary> Companies { get; set; } = new();
}

public sealed class HoldingCompanySummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CompanyResult<T> { public bool Succeeded { get; init; } public T? Value { get; init; } public string? Error { get; init; } public CompanyErrorCode? ErrorCode { get; init; } public static CompanyResult<T> Ok(T v) => new() { Succeeded = true, Value = v }; public static CompanyResult<T> Fail(string e, CompanyErrorCode c) => new() { Succeeded = false, Error = e, ErrorCode = c }; }
public enum CompanyErrorCode { NotFound, AlreadyExists, ValidationError, InUse, Internal }

public sealed class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companies;
    private readonly IAccountRepository _accounts;
    private readonly ILogger<CompanyService> _logger;
    public CompanyService(ICompanyRepository c, IAccountRepository a, ILogger<CompanyService> l) { _companies = c; _accounts = a; _logger = l; }

    public async Task<CompanyResult<Company>> CreateHoldingAsync(string code, string name, string legalName, string baseCurrency, CancellationToken ct)
    {
        if (await _companies.GetByCodeAsync(code, ct) != null) return CompanyResult<Company>.Fail("كود الشركة مستخدم.", CompanyErrorCode.AlreadyExists);
        var now = DateTime.UtcNow;
        var c = new Company { Id = Guid.NewGuid(), Code = code.Trim(), Name = name.Trim(), LegalName = legalName, IsGroup = true, BaseCurrency = baseCurrency.ToUpperInvariant(), IsActive = true, CreatedAt = now, UpdatedAt = now };
        await _companies.InsertAsync(c, ct);
        await _accounts.EnsureDefaultCoAAsync(c.Id, ct);
        return CompanyResult<Company>.Ok(c);
    }

    public async Task<CompanyResult<Company>> CreateHoldingAsync(string code, string name, string legalName, string baseCurrency, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        // P1-9: Tx-aware path. The GetByCodeAsync uniqueness check is on a non-tx
        // connection (safe — for a brand-new tenant the code does not exist), and
        // both the company insert and the CoA seed go through the caller-supplied
        // connection so they roll back together with the tenant insert.
        if (await _companies.GetByCodeAsync(code, ct) != null) return CompanyResult<Company>.Fail("كود الشركة مستخدم.", CompanyErrorCode.AlreadyExists);
        var now = DateTime.UtcNow;
        var c = new Company { Id = Guid.NewGuid(), Code = code.Trim(), Name = name.Trim(), LegalName = legalName, IsGroup = true, BaseCurrency = baseCurrency.ToUpperInvariant(), IsActive = true, CreatedAt = now, UpdatedAt = now };
        await _companies.InsertAsync(c, conn, tx, ct);
        await _accounts.EnsureDefaultCoAAsync(c.Id, conn, tx, ct);
        return CompanyResult<Company>.Ok(c);
    }

    public async Task<CompanyResult<Company>> AddSubsidiaryAsync(Guid parentCompanyId, string code, string name, string? legalName, CancellationToken ct)
    {
        var parent = await _companies.GetByIdAsync(parentCompanyId, ct);
        if (parent == null) return CompanyResult<Company>.Fail("الشركة الأم غير موجودة.", CompanyErrorCode.NotFound);
        if (!parent.IsGroup) return CompanyResult<Company>.Fail("ليست Holding.", CompanyErrorCode.ValidationError);
        if (await _companies.GetByCodeAsync(code, ct) != null) return CompanyResult<Company>.Fail("كود مستخدم.", CompanyErrorCode.AlreadyExists);
        var now = DateTime.UtcNow;
        var sub = new Company { Id = Guid.NewGuid(), Code = code.Trim(), Name = name.Trim(), LegalName = legalName, ParentCompanyId = parent.Id, IsGroup = false, BaseCurrency = parent.BaseCurrency, IsActive = true, CreatedAt = now, UpdatedAt = now };
        await _companies.InsertAsync(sub, ct);
        await _accounts.CloneCoAFromCompanyAsync(sub.Id, parent.Id, ct);
        return CompanyResult<Company>.Ok(sub);
    }

    public async Task<CompanyResult<Company>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var c = await _companies.GetByIdAsync(id, ct);
        if (c == null) return CompanyResult<Company>.Fail("غير موجودة.", CompanyErrorCode.NotFound);
        return CompanyResult<Company>.Ok(c);
    }
    public async Task<CompanyResult<IReadOnlyList<Company>>> ListAsync(bool includeInactive, CancellationToken ct) =>
        CompanyResult<IReadOnlyList<Company>>.Ok(await _companies.ListAsync(includeInactive, ct));

    // Sprint 2 (T1 / Block A): paged list. page is 1-based. pageSize is clamped to
    // [1, 100] — anything > 100 is treated as 100 (per task spec) to bound the
    // response payload. When userId is non-null, results are filtered via
    // user_companies so the caller only sees companies they have been assigned to.
    public async Task<CompanyResult<CompanyPage>> ListPagedAsync(int page, int pageSize, bool includeInactive, Guid? userId, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        var skip = (page - 1) * pageSize;

        IReadOnlyList<Company> items;
        int total;
        if (userId.HasValue)
        {
            items = await _companies.ListByUserAsync(userId.Value, skip, pageSize, includeInactive, ct);
            total = await _companies.CountByUserAsync(userId.Value, includeInactive, ct);
        }
        else
        {
            items = await _companies.ListPagedAsync(skip, pageSize, includeInactive, ct);
            total = await _companies.CountAsync(includeInactive, ct);
        }

        return CompanyResult<CompanyPage>.Ok(new CompanyPage
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
        });
    }

    // Sprint 2 (T3 / Block A): top-level create. Behavior:
    //  - Code is trimmed and upper-cased for the uniqueness check (codes are case-
    //    insensitive — see the unique index `ix_companies_code` in companies.json
    //    + GetByCodeAsync already uses LOWER(code)).
    //  - Idempotent on `code`: if a row with the same code already exists, return
    //    that row (Succeeded=true, WasCreated=false) instead of erroring. This
    //    lets the FE safely retry a POST after a flaky network without producing
    //    a duplicate.
    //  - When parentCompanyId is null, the company is a root (no parent, is_group=false).
    //  - When parentCompanyId is set, validates that the parent exists and is
    //    a Holding (is_group=true). Mirrors the AddSubsidiary guard so we don't
    //    accidentally allow a subsidiary under a subsidiary.
    //  - Slug is auto-generated from the company name (URL-friendly). It is
    //    appended with a short suffix on collision to keep it unique.
    public async Task<CompanyResult<CreateCompanyResult>> CreateAsync(CreateCompanyRequest req, CancellationToken ct)
    {
        if (req == null) return CompanyResult<CreateCompanyResult>.Fail("الطلب فارغ.", CompanyErrorCode.ValidationError);
        if (string.IsNullOrWhiteSpace(req.Code)) return CompanyResult<CreateCompanyResult>.Fail("كود الشركة مطلوب.", CompanyErrorCode.ValidationError);
        if (string.IsNullOrWhiteSpace(req.Name)) return CompanyResult<CreateCompanyResult>.Fail("اسم الشركة مطلوب.", CompanyErrorCode.ValidationError);

        var code = req.Code.Trim();
        var name = req.Name.Trim();
        var legalName = string.IsNullOrWhiteSpace(req.LegalName) ? name : req.LegalName.Trim();
        var baseCurrency = string.IsNullOrWhiteSpace(req.BaseCurrency) ? "LYD" : req.BaseCurrency.ToUpperInvariant();

        // Idempotency: if a company with this code already exists, return it.
        var existing = await _companies.GetByCodeAsync(code, ct);
        if (existing != null)
        {
            _logger.LogInformation("CreateAsync: company code {Code} already exists (id={Id}); returning existing row (idempotent).", code, existing.Id);
            return CompanyResult<CreateCompanyResult>.Ok(new CreateCompanyResult { Company = existing, WasCreated = false });
        }

        // Parent validation (only when supplied).
        Guid? parentId = null;
        bool isGroup = false;
        if (req.ParentCompanyId.HasValue && req.ParentCompanyId.Value != Guid.Empty)
        {
            var parent = await _companies.GetByIdAsync(req.ParentCompanyId.Value, ct);
            if (parent == null) return CompanyResult<CreateCompanyResult>.Fail("الشركة الأم غير موجودة.", CompanyErrorCode.NotFound);
            if (!parent.IsGroup) return CompanyResult<CreateCompanyResult>.Fail("الشركة الأم ليست Holding.", CompanyErrorCode.ValidationError);
            parentId = parent.Id;
            isGroup = false; // sub of a Holding is always a regular company
        }

        // Auto-slug. We don't ship a transliteration lib so the slug is built from
        // the code (always ASCII-friendly) and a hash of the name. This is a
        // stable, unique-enough default for the FE /api/holdings/{slug} page.
        var slug = await GenerateUniqueSlugAsync(name, code, ct);

        var now = DateTime.UtcNow;
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Slug = slug,
            LegalName = legalName,
            ParentCompanyId = parentId,
            IsGroup = isGroup,
            BaseCurrency = baseCurrency,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _companies.InsertAsync(c, ct);
        return CompanyResult<CreateCompanyResult>.Ok(new CreateCompanyResult { Company = c, WasCreated = true });
    }

    // Sprint 2 (T3 / Block A): slug generator. Returns a URL-friendly slug derived
    // from the name + code, with a short collision suffix when needed. The
    // uniqueness check uses the general GetBySlugAsync (not GetHoldingBySlugAsync,
    // which filters for is_group=true and parent_company_id IS NULL).
    private async Task<string> GenerateUniqueSlugAsync(string name, string code, CancellationToken ct)
    {
        var base_ = Slugify(name) + "-" + code.ToLowerInvariant();
        if (await _companies.GetBySlugAsync(base_, ct) == null) return base_;
        for (int i = 2; i < 100; i++)
        {
            var candidate = base_ + "-" + i;
            if (await _companies.GetBySlugAsync(candidate, ct) == null) return candidate;
        }
        // Fallback: append a short random hex suffix. 1-in-16^8 chance of collision.
        return base_ + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    // Sprint 2 (T3 / Block A): minimal ASCII slugifier. Strips diacritics, keeps
    // [a-z0-9-], collapses whitespace and consecutive dashes. We don't ship a
    // full transliteration lib (no NUGET dep) — non-ASCII names produce an
    // empty slug which is then padded with the company code, so the result is
    // always at least "<code>".
    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var lowered = s.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(lowered.Length);
        foreach (var ch in lowered)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
            else if (ch == ' ' || ch == '-' || ch == '_') sb.Append('-');
        }
        // Collapse consecutive dashes + trim.
        var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        return result;
    }
    public async Task<CompanyResult<SubsidiaryListDto>> GetSubsidiariesAsync(Guid parentCompanyId, CancellationToken ct)
    {
        var subs = await _companies.ListSubsidiariesAsync(parentCompanyId, ct);
        return CompanyResult<SubsidiaryListDto>.Ok(new SubsidiaryListDto
        {
            ParentCompanyId = parentCompanyId,
            Subsidiaries = subs,
        });
    }
    // Sprint 11 T2 (BE Jimi): Holding tree builder. The Holding is the single row
    // where is_group=true AND parent_company_id IS NULL. Its direct children are
    // the subsidiaries; each subsidiary may have nested children. We return the
    // list of direct children of the Holding (the FE renders the Holding as a
    // single root node wrapping the list). The DTO is flat (id/code/name/parentId/
    // isGroup/isActive/children) so the FE can drop it straight into a tree widget
    // without further projection. Includes inactive rows so the FE can show them
    // greyed-out (the admin tree shows all companies, not just active ones).
    public async Task<CompanyResult<IReadOnlyList<CompanyTreeNodeDto>>> GetTreeAsync(CancellationToken ct)
    {
        var all = await _companies.ListAsync(includeInactive: true, ct);
        // Index children by parent for O(N) recursion.
        var byParent = all
            .Where(c => c.ParentCompanyId.HasValue)
            .GroupBy(c => c.ParentCompanyId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        CompanyTreeNodeDto Build(Company n) => new(
            Id: n.Id,
            Code: n.Code,
            Name: n.Name,
            ParentCompanyId: n.ParentCompanyId,
            IsGroup: n.IsGroup,
            IsActive: n.IsActive,
            Children: byParent.TryGetValue(n.Id, out var kids)
                ? kids.Select(Build).ToList()
                : new List<CompanyTreeNodeDto>());
        // The Holding root: is_group=true AND parent_company_id IS NULL.
        var holding = all.FirstOrDefault(c => c.IsGroup && c.ParentCompanyId == null);
        if (holding == null)
        {
            // No Holding in the system — return an empty list. The FE renders the
            // tree empty state; we don't 404 because the route is collection-shaped
            // and the FE will need it to keep working before the bootstrap creates
            // the first Holding.
            return CompanyResult<IReadOnlyList<CompanyTreeNodeDto>>.Ok(new List<CompanyTreeNodeDto>());
        }
        return CompanyResult<IReadOnlyList<CompanyTreeNodeDto>>.Ok(byParent.TryGetValue(holding.Id, out var subs)
            ? subs.Select(Build).ToList()
            : new List<CompanyTreeNodeDto>());
    }
    public async Task<CompanyResult<bool>> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var c = await _companies.GetByIdAsync(id, ct);
        if (c == null) return CompanyResult<bool>.Fail("غير موجودة.", CompanyErrorCode.NotFound);
        c.IsActive = false; c.UpdatedAt = DateTime.UtcNow;
        await _companies.UpdateAsync(c, ct);
        return CompanyResult<bool>.Ok(true);
    }

    // Sprint 1 (T2 / Block A): Holding-by-slug lookup. Returns the Holding with its
    // immediate child companies. The Holding is identified by is_group=true AND
    // parent_company_id IS NULL. Slug is matched case-insensitively.
    public async Task<CompanyResult<HoldingDetail>> GetHoldingBySlugAsync(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return CompanyResult<HoldingDetail>.Fail("الـ slug مطلوب.", CompanyErrorCode.ValidationError);

        var holding = await _companies.GetHoldingBySlugAsync(slug.Trim(), ct);
        if (holding == null)
            return CompanyResult<HoldingDetail>.Fail("لم يتم العثور على الشركة القابضة.", CompanyErrorCode.NotFound);

        var subs = await _companies.ListSubsidiariesAsync(holding.Id, ct);
        var detail = new HoldingDetail
        {
            Id = holding.Id,
            Name = holding.Name,
            Code = holding.Code,
            Slug = holding.Slug,
            BaseCurrency = holding.BaseCurrency,
            IsActive = holding.IsActive,
            Companies = subs.Select(s => new HoldingCompanySummary
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Slug = s.Slug,
                IsActive = s.IsActive,
            }).ToList(),
        };
        return CompanyResult<HoldingDetail>.Ok(detail);
    }
}
