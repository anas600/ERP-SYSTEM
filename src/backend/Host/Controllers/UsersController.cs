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
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var users = await _users.ListAsync(skip, take, ct);
        var total = await _users.CountAsync(ct);

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

        return Ok(new { items = result, total, skip, take });
    }

    /// <summary>
    /// Get a single user with their roles.
    /// </summary>
    [HttpGet("{id:guid}")]
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
    public async Task<IActionResult> ListRoles(CancellationToken ct = default)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var roles = await conn.QueryAsync<RoleInfo>(new CommandDefinition(@"
            SELECT id, name, description FROM roles
            ORDER BY name",
            cancellationToken: ct));
        return Ok(roles);
    }
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
