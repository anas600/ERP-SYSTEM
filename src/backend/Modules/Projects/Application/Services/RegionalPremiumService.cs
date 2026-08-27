using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

// =============================================================================
// Sprint 62 / DEC-197 — Regional Premium service.
//
// Manages CRUD on regional_premiums and exposes the DEC-197 calculation used by
// the billing flow. The service is the source of truth for "which premium applies
// to this project" — BillingService delegates to it via
// <see cref="CalculateDeductionAsync"/> rather than reading the DB itself.
// =============================================================================

/// <summary>Result envelope for RegionalPremium service calls.</summary>
public sealed class RegionalPremiumResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public RegionalPremiumErrorCode? ErrorCode { get; init; }
    public static RegionalPremiumResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static RegionalPremiumResult<T> Fail(string e, RegionalPremiumErrorCode c) =>
        new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum RegionalPremiumErrorCode
{
    NotFound, AlreadyExists, ValidationError, Internal
}

public interface IRegionalPremiumService
{
    Task<RegionalPremiumResult<RegionalPremiumResponse>> CreateAsync(
        Guid userId, Guid projectId, CreateRegionalPremiumRequest req, CancellationToken ct);

    Task<RegionalPremiumResult<RegionalPremiumResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateRegionalPremiumRequest req, CancellationToken ct);

    Task<RegionalPremiumResult<RegionalPremiumResponse>> GetByIdAsync(Guid id, CancellationToken ct);

    Task<RegionalPremiumResult<IReadOnlyList<RegionalPremiumResponse>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct);

    Task<RegionalPremiumResult<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct);

    /// <summary>
    /// DEC-197 — Calculate the regional premium deduction for a project on a gross amount.
    /// Returns 0 if the project has no active premium row. The BillingService calls
    /// this from <c>CreateAsync</c> and <c>PreviewAsync</c> to populate
    /// <c>RegionalPremiumDeducted</c> and <c>NetAmountAfterPremium</c>.
    /// </summary>
    Task<decimal> CalculateDeductionAsync(Guid projectId, decimal grossAmount, CancellationToken ct);
}

/// <summary>
/// Sprint 62 / DEC-197 — Regional Premium service.
///
/// <para><b>Validation rules</b>:</para>
/// <list type="bullet">
///   <item>Region must be one of the <see cref="RegionalPremiumRegions.All"/> labels.</item>
///   <item>Each percent must be in [0, 100]. Negative or &gt;100 are rejected.</item>
///   <item>Combined percent (Ndb+CIT+SS) capped at 100% (we don't allow a
///         configuration that would zero-out or negative the net amount).</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: CompanyId comes from <see cref="ICompanyContext"/>,
/// never from the request DTO. The list/get queries scope by company where applicable
/// (the repository filters on company_id for update/delete; list returns all rows
/// for the project regardless of company because the caller already knows the
/// project is in their JWT-scoped company).</para>
/// </summary>
public sealed class RegionalPremiumService : IRegionalPremiumService
{
    private readonly IRegionalPremiumRepository _premiums;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<RegionalPremiumService> _logger;

    public RegionalPremiumService(
        IRegionalPremiumRepository premiums,
        ICompanyContext companyContext,
        ILogger<RegionalPremiumService> logger)
    {
        _premiums = premiums;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<RegionalPremiumResult<RegionalPremiumResponse>> CreateAsync(
        Guid userId, Guid projectId, CreateRegionalPremiumRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        if (!RegionalPremiumRegions.IsValid(req.Region))
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                "region غير صالح. القيم المسموحة: Tripoli, Benghazi, Misrata, NDB-Oil, NDB-Gas, Other.",
                RegionalPremiumErrorCode.ValidationError);

        var validation = ValidatePercents(req.NdbPercent, req.CitPercent, req.SsPercent);
        if (validation != null)
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                validation, RegionalPremiumErrorCode.ValidationError);

        // UNIQUE (project_id, region) — check for an existing active row.
        var existing = await _premiums.ListByProjectAsync(projectId, ct);
        if (existing.Any(p => string.Equals(p.Region, req.Region, StringComparison.OrdinalIgnoreCase)))
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                $"يوجد بالفعل خصم منطقة لنفس الـ region ({req.Region}) على هذا المشروع.",
                RegionalPremiumErrorCode.AlreadyExists);

        var now = DateTime.UtcNow;
        var p = new RegionalPremium
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = projectId,
            Region = req.Region,
            NdbPercent = req.NdbPercent,
            CitPercent = req.CitPercent,
            SsPercent = req.SsPercent,
            IsActive = req.IsActive,
            CreatedAt = now
        };
        await _premiums.InsertAsync(p, ct);
        _logger.LogInformation("RegionalPremium created {Id} for project {ProjectId} region {Region}",
            p.Id, projectId, p.Region);
        return RegionalPremiumResult<RegionalPremiumResponse>.Ok(MapToResponse(p));
    }

    public async Task<RegionalPremiumResult<RegionalPremiumResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateRegionalPremiumRequest req, CancellationToken ct)
    {
        var p = await _premiums.GetByIdAsync(id, ct);
        if (p == null)
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                "غير موجود.", RegionalPremiumErrorCode.NotFound);

        if (!RegionalPremiumRegions.IsValid(req.Region))
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                "region غير صالح.", RegionalPremiumErrorCode.ValidationError);

        var validation = ValidatePercents(req.NdbPercent, req.CitPercent, req.SsPercent);
        if (validation != null)
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                validation, RegionalPremiumErrorCode.ValidationError);

        p.Region = req.Region;
        p.NdbPercent = req.NdbPercent;
        p.CitPercent = req.CitPercent;
        p.SsPercent = req.SsPercent;
        p.IsActive = req.IsActive;
        await _premiums.UpdateAsync(p, ct);
        _logger.LogInformation("RegionalPremium updated {Id}", p.Id);
        return RegionalPremiumResult<RegionalPremiumResponse>.Ok(MapToResponse(p));
    }

    public async Task<RegionalPremiumResult<RegionalPremiumResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var p = await _premiums.GetByIdAsync(id, ct);
        if (p == null)
            return RegionalPremiumResult<RegionalPremiumResponse>.Fail(
                "غير موجود.", RegionalPremiumErrorCode.NotFound);
        return RegionalPremiumResult<RegionalPremiumResponse>.Ok(MapToResponse(p));
    }

    public async Task<RegionalPremiumResult<IReadOnlyList<RegionalPremiumResponse>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct)
    {
        var rows = await _premiums.ListByProjectAsync(projectId, ct);
        var list = rows.Select(MapToResponse).ToList();
        return RegionalPremiumResult<IReadOnlyList<RegionalPremiumResponse>>.Ok(list);
    }

    public async Task<RegionalPremiumResult<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var p = await _premiums.GetByIdAsync(id, ct);
        if (p == null)
            return RegionalPremiumResult<bool>.Fail(
                "غير موجود.", RegionalPremiumErrorCode.NotFound);
        await _premiums.DeleteAsync(id, ct);
        _logger.LogInformation("RegionalPremium deleted {Id} by {UserId}", id, userId);
        return RegionalPremiumResult<bool>.Ok(true);
    }

    /// <summary>
    /// DEC-197 calculation: returns gross × (Ndb% + CIT% + SS%) / 100 for the project's
    /// first active premium row. Returns 0 if no active row exists or gross is non-positive.
    /// </summary>
    public async Task<decimal> CalculateDeductionAsync(Guid projectId, decimal grossAmount, CancellationToken ct)
    {
        if (grossAmount <= 0m) return 0m;

        var premiums = await _premiums.ListByProjectAsync(projectId, ct);
        var active = premiums.FirstOrDefault(p => p.IsActive);
        if (active == null) return 0m;

        var totalPct = active.NdbPercent + active.CitPercent + active.SsPercent;
        return Math.Round(grossAmount * (totalPct / 100m), 4);
    }

    // ===== Helpers =====

    private static string? ValidatePercents(decimal ndb, decimal cit, decimal ss)
    {
        if (ndb < 0m || ndb > 100m || cit < 0m || cit > 100m || ss < 0m || ss > 100m)
            return "النسب يجب أن تكون بين 0 و 100.";
        var total = ndb + cit + ss;
        if (total > 100m)
            return "مجموع النسب (NDB + CIT + SS) لا يجب أن يتجاوز 100%.";
        return null;
    }

    private static RegionalPremiumResponse MapToResponse(RegionalPremium p)
    {
        var combined = Math.Round(p.NdbPercent + p.CitPercent + p.SsPercent, 4);
        return new RegionalPremiumResponse(
            p.Id, p.ProjectId, p.Region,
            p.NdbPercent, p.CitPercent, p.SsPercent,
            p.IsActive, p.CreatedAt, combined);
    }
}
