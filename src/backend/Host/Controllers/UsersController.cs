// DEC-067-C: Users admin controller
// Admin operations for user management (list, view, assign roles).

using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using ERPSystem.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.WriteMasterData)]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IDbConnectionFactory _db;

    public UsersController(IUserRepository users, IDbConnectionFactory db)
    {
        _users = users;
        _db = db;
    }

    /// <summary>
    /// List users (Phase 6.1b: users are global, not tenant-scoped).
    /// Sprint 2 (T4 / Block A): optional ?company_id={guid} filter — when present,
    /// only users assigned to that company (via user_companies) are returned.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UsersListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery(Name = "company_id")] Guid? companyId = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<ERPSystem.Modules.Identity.Entities.User> users;
        int total;

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            // Sprint 2 (T4): company-scoped list. The join is on user_companies,
            // so the result is the set of users who have been assigned to the
            // given company. Multi-company model (Constitution Article 3).
            users = await _users.ListByCompanyAsync(companyId.Value, skip, take, ct);
            total = await _users.CountByCompanyAsync(companyId.Value, ct);
        }
        else
        {
            users = await _users.ListAsync(skip, take, ct);
            total = await _users.CountAsync(ct);
        }

        // Enrich with role names (1 extra query per user — could be optimized)
        var result = new List<UserWithRoles>();
        foreach (var u in users)
        {
            var roleNames = await _users.GetRoleNamesAsync(u.Id, ct);
            result.Add(new UserWithRoles
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                IsActive = u.IsActive,
                TwoFactorEnabled = u.TwoFactorEnabled,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                LastLoginAt = u.LastLoginAt,
                Roles = roleNames.ToList(),
            });
        }

        return Ok(new UsersListResponse { Items = result, Total = total, Skip = skip, Take = take });
    }

    /// <summary>
    /// Get a single user with their roles.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserWithRoles), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();

        var roleNames = await _users.GetRoleNamesAsync(user.Id, ct);
        return Ok(new UserWithRoles
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roleNames.ToList(),
        });
    }

    /// <summary>
    /// List all available roles.
    /// </summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListRoles(CancellationToken ct = default)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var roles = await conn.QueryAsync<RoleInfo>(new CommandDefinition(@"
            SELECT id, name, description FROM roles
            ORDER BY name",
            cancellationToken: ct));
        return Ok(roles);
    }

    /// <summary>
    /// Sprint 2 (T5 / Block A): list the companies a user has access to.
    /// Returns 200 with { items: UserCompanyInfo[] } when the user exists,
    /// 404 when the user does not exist. The companies are returned in the
    /// same order the UserCompanyLink rows come back (default first, then by code).
    /// </summary>
    [HttpGet("{id:guid}/companies")]
    [ProducesResponseType(typeof(UserCompaniesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompanies(Guid id, CancellationToken ct = default)
    {
        // Existence check first — distinguishes "no companies assigned" (200, empty)
        // from "user does not exist" (404). The UserRepository.GetUserCompaniesAsync
        // returns an empty list for users with no company assignments, so without
        // the existence probe we would silently return [] for non-existent users.
        var user = await _users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();

        var links = await _users.GetUserCompaniesAsync(id, ct);
        var items = links.Select(l => new UserCompanyInfo
        {
            CompanyId = l.CompanyId,
            CompanyCode = l.CompanyCode,
            CompanyName = l.CompanyName,
            IsDefault = l.IsDefault,
            IsHolding = l.IsHolding,
        }).ToList();

        return Ok(new UserCompaniesResponse { Items = items });
    }
}

/// <summary>Sprint 9 (Jimi 2 — T2): typed response shape for <c>GET /api/users</c>.</summary>
public sealed class UsersListResponse
{
    public IReadOnlyList<UserWithRoles> Items { get; set; } = Array.Empty<UserWithRoles>();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

/// <summary>Sprint 9 (Jimi 2 — T2): typed response shape for <c>GET /api/users/{id}/companies</c>.</summary>
public sealed class UserCompaniesResponse
{
    public IReadOnlyList<UserCompanyInfo> Items { get; set; } = Array.Empty<UserCompanyInfo>();
}

public sealed class UserCompanyInfo
{
    public Guid CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsHolding { get; set; }
}

public sealed class UserWithRoles
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public sealed class RoleInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
