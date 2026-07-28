// Activity module (Cycle 6 / DEC-073) — user-action activity log.
//
// The activity_log table is intentionally separate from audit_log:
//
//   audit_log   — entity CRUD (Vendor.Create, JournalEntry.Post, ...).
//                 Schema: id, company_id, entity_type, entity_id, action, user_id,
//                         changes (jsonb), ip_address, created_at.
//                 See Shared/Audit/IAuditLogger.cs.
//
//   activity_log — user actions over time (LOGIN, REFRESH, LOGOUT, REGISTER, ...).
//                 Schema: id, company_id, user_id, action, ip_address,
//                         user_agent, metadata (jsonb), created_at.
//                 See Modules/Activity/Application/IActivityLogger.cs (this file).
//
// The split is intentional: auditors want "what changed in the data?"; admins
// want "what did this user do?". One table can't answer both efficiently.
//
// Failure-safe: activity log failures are LOGGED and do NOT break business
// logic. The same rationale as AuditLogger — a missed log entry is much less
// expensive than a failed login.

using ERPSystem.Modules.Activity.Application;

namespace ERPSystem.Modules.Activity.Application;

/// <summary>
/// يسجّل أفعال الـ user في جدول <c>activity_log</c>. يُستخدم في
/// <c>AuthService</c> لتسجيل الدخول/التحديث/الخروج/التسجيل.
/// </summary>
public interface IActivityLogger
{
    /// <summary>
    /// يسجّل فعل واحد (login, refresh, logout, ...). الـ failures تُسجَّل
    /// في الـ logger ولا تُرمي (failure-safe per DEC-053 + DEC-073).
    /// </summary>
    /// <param name="userId">معرّف الـ user (nullable — للنّفس-التسجيل الـ pre-user).</param>
    /// <param name="companyId">معرّف الـ company (nullable — للـ login قد لا يكون معروف بعد).</param>
    /// <param name="action">الفعل (انظر <see cref="ActivityAction"/> للثوابت).</param>
    /// <param name="metadata">بيانات إضافيّة (jsonb) — failure reason, refresh token id, ...</param>
    /// <param name="ct">Cancellation token.</param>
    Task LogAsync(
        Guid? userId,
        Guid? companyId,
        string action,
        object? metadata = null,
        CancellationToken ct = default);
}
