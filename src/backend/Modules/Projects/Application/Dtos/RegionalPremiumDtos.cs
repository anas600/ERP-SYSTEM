using System;
using System.Collections.Generic;

namespace ERPSystem.Modules.Projects.Application.Dtos;

// =============================================================================
// Sprint 62 / DEC-197 — Regional Premium DTOs.
//
// Wire-format for the regional premium API (Wave 2A will add the controller).
// The service layer maps between the persistence entity (Modules/Projects/Entities/)
// and these DTOs at the controller boundary.
// =============================================================================

/// <summary>
/// Allowed region labels for <see cref="CreateRegionalPremiumRequest.Region"/>.
/// Stored as TEXT in the DB (DEC-197 — extensible without schema change).
/// </summary>
public static class RegionalPremiumRegions
{
    public const string Tripoli = "Tripoli";
    public const string Benghazi = "Benghazi";
    public const string Misrata = "Misrata";
    public const string NdbOil = "NDB-Oil";
    public const string NdbGas = "NDB-Gas";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Tripoli, Benghazi, Misrata, NdbOil, NdbGas, Other
    };

    public static bool IsValid(string? region) =>
        !string.IsNullOrWhiteSpace(region) && All.Contains(region);
}

/// <summary>
/// Read DTO returned by GET /api/projects/{id}/regional-premiums and
/// GET /api/projects/{id}/regional-premiums/{id}.
/// </summary>
public record RegionalPremiumResponse(
    Guid Id,
    Guid ProjectId,
    string Region,
    decimal NdbPercent,
    decimal CitPercent,
    decimal SsPercent,
    bool IsActive,
    DateTime CreatedAt,
    /// <summary>Combined percent = Ndb + Cit + Ss (rounded to 4 dp for display).</summary>
    decimal CombinedPercent
);

/// <summary>
/// Body for POST /api/projects/{id}/regional-premiums.
/// <c>CompanyId</c> is intentionally NOT in the request — the service resolves
/// it from the JWT context (L19 / L29 / L30).
/// </summary>
public record CreateRegionalPremiumRequest(
    string Region,
    decimal NdbPercent,
    decimal CitPercent,
    decimal SsPercent,
    bool IsActive
);

/// <summary>
/// Body for PUT /api/projects/{id}/regional-premiums/{id}.
/// All fields required — partial updates are not supported in Wave 1A (Wave 2A may add PATCH).
/// </summary>
public record UpdateRegionalPremiumRequest(
    string Region,
    decimal NdbPercent,
    decimal CitPercent,
    decimal SsPercent,
    bool IsActive
);
