using System.Data;
using ERPSystem.Modules.Companies.Entities;
using ERPSystem.Modules.Companies.Infrastructure;
using ERPSystem.Modules.Finance.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Companies.Application.Services;

public interface ICompanyService
{
    Task<CompanyResult<Company>> CreateHoldingAsync(string code, string name, string legalName, string baseCurrency, CancellationToken ct);
    Task<CompanyResult<Company>> CreateHoldingAsync(string code, string name, string legalName, string baseCurrency, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<CompanyResult<Company>> AddSubsidiaryAsync(Guid parentCompanyId, string code, string name, string? legalName, CancellationToken ct);
    Task<CompanyResult<Company>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CompanyResult<IReadOnlyList<Company>>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<CompanyResult<IReadOnlyList<Company>>> GetSubsidiariesAsync(Guid parentCompanyId, CancellationToken ct);
    Task<CompanyResult<CompanyTreeNode>> GetTreeAsync(CancellationToken ct);
    Task<CompanyResult<bool>> DeactivateAsync(Guid id, CancellationToken ct);
    // Sprint 1 (T2 / Block A): slug-based Holding lookup, returns Holding + child companies.
    Task<CompanyResult<HoldingDetail>> GetHoldingBySlugAsync(string slug, CancellationToken ct);
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

public sealed class CompanyTreeNode { public Company Company { get; set; } = null!; public List<CompanyTreeNode> Children { get; set; } = new(); }
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
    public async Task<CompanyResult<IReadOnlyList<Company>>> GetSubsidiariesAsync(Guid parentCompanyId, CancellationToken ct) =>
        CompanyResult<IReadOnlyList<Company>>.Ok(await _companies.ListSubsidiariesAsync(parentCompanyId, ct));
    public async Task<CompanyResult<CompanyTreeNode>> GetTreeAsync(CancellationToken ct)
    {
        var all = await _companies.ListAsync(true, ct);
        var tree = all.Where(c => c.ParentCompanyId == null).Select(r => BuildTree(r, all)).ToList();
        return CompanyResult<CompanyTreeNode>.Ok(new CompanyTreeNode { Children = tree });
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
    private static CompanyTreeNode BuildTree(Company n, IReadOnlyList<Company> all) => new() { Company = n, Children = all.Where(c => c.ParentCompanyId == n.Id).Select(c => BuildTree(c, all)).ToList() };
}
