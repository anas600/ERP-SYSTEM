namespace ERPSystem.Shared.MultiTenancy;

/// <summary>
/// سياق الشركة (Company) النشطة داخل الـ request
/// يُملأ من CompanyContextMiddleware بناءً على X-Company-Id header + JWT user id
///
/// Company context abstraction (Phase 6.1b: Multi-Company model, NOT Multi-Tenant).
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
