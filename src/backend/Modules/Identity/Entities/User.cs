using System;
using System.Collections.Generic;

namespace ERPSystem.Modules.Identity.Entities;

/// <summary>
/// User entity — Phase 6.1c: Multi-Company model.
/// A user is global; their company access is tracked in the <c>user_companies</c>
/// join table (via <see cref="IUserRepository.GetUserCompaniesAsync"/>).
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
