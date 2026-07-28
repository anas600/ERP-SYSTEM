// Activity action string constants (Cycle 6 / DEC-073).
//
// Why string constants and not enums: the audit_log.action column is
// varchar(20) in production and varchar(40) in activity_log — enums would
// require migration to add new values, while string constants can be added
// at any time without a schema change. This matches the existing
// AuditAction pattern in Shared/Audit/IAuditLogger.cs.

namespace ERPSystem.Modules.Activity.Application;

/// <summary>
/// ثوابت الـ action للـ activity_log. كل فعل يكون له معنى واضح ومميّز عن
/// الـ audit_log (الذي يتعقّب CRUD على entities، مش user actions).
/// </summary>
public static class ActivityAction
{
    /// <summary>Login attempt (success or failure). Metadata has "success": bool, "reason"?: string.</summary>
    public const string Login = "LOGIN";

    /// <summary>Successful login only. Use ActivityAction.Login + metadata.success=true.</summary>
    public const string LoginSuccess = "LOGIN_SUCCESS";

    /// <summary>Failed login only. Use ActivityAction.Login + metadata.success=false.</summary>
    public const string LoginFailed = "LOGIN_FAILED";

    /// <summary>Refresh token exchange. Metadata has token id (truncated).</summary>
    public const string Refresh = "REFRESH";

    /// <summary>Logout (refresh token revoked). Metadata has token id.</summary>
    public const string Logout = "LOGOUT";

    /// <summary>New user registration. Metadata has company id (Holding).</summary>
    public const string Register = "REGISTER";

    /// <summary>Password change. Metadata has "self_service": bool.</summary>
    public const string PasswordChange = "PASSWORD_CHANGE";

    /// <summary>User switched active company. Metadata has from_company_id, to_company_id.</summary>
    public const string CompanySwitch = "COMPANY_SWITCH";
}
