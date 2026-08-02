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

    /// <summary>
    /// نتيجة تطبيق القواعد — مرجع الـ Service للـ invoice Id.
    /// يربط القيد المُنشأ بـ eventType (مثلاً SalesInvoice.JournalEntryId).
    /// </summary>
    Task<FinanceResult<ApplyRulesResult>> ApplyRulesAndReturnAsync(Guid userId, TriggeringEvent eventType, EventPayload payload, CancellationToken ct);

    /// <summary>Seed القواعد الافتراضية</summary>
    Task EnsureDefaultRulesAsync(Guid holdingId, CancellationToken ct);
}

/// <summary>نتيجة ApplyRulesAndReturnAsync — list of JE IDs created</summary>
public sealed class ApplyRulesResult
{
    public int EntriesCreated { get; set; }
    /// <summary>أول قيد مُنشأ (للربط بالـ invoice / bill). Null لو ما في قواعد نشطة.</summary>
    public Guid? FirstJournalEntryId { get; set; }
    /// <summary>رقم القيد الأول (للعرض).</summary>
    public string? FirstEntryNumber { get; set; }
}

/// <summary>البيانات المحمولة في الحدث (event payload) — تُمرَّر لـ template</summary>
public sealed class EventPayload
{
    /// <summary>المبلغ الأساسي (مثال: قيمة المخزون المستلم، أو إجمالي دفعة).</summary>
    public decimal Amount { get; set; }

    /// <summary>المبلغ قبل الضريبة (subtotal) — فاتورة: مجموع السطور قبل الضريبة.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>مبلغ الضريبة (0 إذا ليبيا بدون ضريبة).</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>العملة (افتراضياً LYD).</summary>
    public string Currency { get; set; } = "LYD";

    /// <summary>وصف القيد.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>مرجع خارجي (رقم فاتورة، رقم سند، إلخ).</summary>
    public string? Reference { get; set; }

    /// <summary>تاريخ الحركة (افتراضياً الآن).</summary>
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
}
