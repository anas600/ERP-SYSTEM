using ERPSystem.Modules.Finance.Entities;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IPostingRulesService
{
    /// <summary>إنشاء قاعدة جديدة</summary>
    Task<FinanceResult<PostingRule>> CreateAsync(CreatePostingRuleRequest request, CancellationToken ct);

    /// <summary>قائمة القواعد</summary>
    Task<FinanceResult<IReadOnlyList<PostingRule>>> ListAsync(CancellationToken ct);

    /// <summary>تطبيق كل القواعد النشطة لحدث معين</summary>
    /// <returns>عدد القيود المُنشأة</returns>
    Task<int> ApplyRulesAsync(Guid userId, TriggeringEvent eventType, EventPayload payload, CancellationToken ct);

    /// <summary>Seed القواعد الافتراضية</summary>
    Task EnsureDefaultRulesAsync(CancellationToken ct);
}

/// <summary>البيانات المحمولة في الحدث (event payload) — تُمرَّر لـ template</summary>
public sealed class EventPayload
{
    /// <summary>المبلغ الأساسي (مثال: قيمة المخزون المستلم)</summary>
    public decimal Amount { get; set; }

    /// <summary>عملة (افتراضياً SAR)</summary>
    public string Currency { get; set; } = "SAR";

    /// <summary>وصف القيد</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>مرجع خارجي (رقم فاتورة استلام، إلخ)</summary>
    public string? Reference { get; set; }

    /// <summary>تاريخ الحركة (افتراضياً الآن)</summary>
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
}
