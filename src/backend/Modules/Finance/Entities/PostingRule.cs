using System;
using System.Collections.Generic;

namespace ERPSystem.Modules.Finance.Entities;

/// <summary>
/// نوع حدث الـ Event Bus الذي يستقبله الـ Rules Engine.
///
/// Sprint 21: expanded to cover the 4 P0 business events:
///   - SalesInvoicePosted (Dr AR / Cr Sales [+ optional Cr VAT])
///   - VendorBillPosted   (Dr Inventory / Cr AP [+ optional Dr VAT])
///   - ReceiptPosted      (Dr Cash / Cr AR — payment from customer)
///   - PaymentPosted      (Dr AP / Cr Cash — payment to vendor)
///
/// Original MVP events kept for backward compat:
///   - StockReceived / StockIssued (Inventory)
///   - InvoiceCreated / PaymentReceived (legacy names)
/// </summary>
public enum TriggeringEvent
{
    // === Inventory (Sprint 11-12) ===
    StockReceived = 1,
    StockIssued = 2,

    // === Sales cycle (Sprint 21) ===
    SalesInvoicePosted = 3,   // a sales invoice was posted
    ReceiptPosted = 4,        // an AR receipt was posted (customer paid us)

    // === Procurement cycle (Sprint 21) ===
    VendorBillPosted = 5,     // a vendor bill was posted
    PaymentPosted = 6,        // a payment to vendor was posted

    // === Legacy aliases (kept for backward compat with Sprint 11-12 rules) ===
    InvoiceCreated = 3,       // alias for SalesInvoicePosted
    PaymentReceived = 4,      // alias for ReceiptPosted
}

/// <summary>
/// محرك القواعد (Rules Engine) — MVP.
///
/// كل قاعدة تقول: "عند ورود حدث X بقيم Y، أنشئ Journal Entry بهذه السطور".
/// حالياً: قواعيد بسيطة (1 حدث → N سطور debit/credit).
/// مستقبلياً: لوغين متقدم (شروط، حسابات، إلخ).
/// </summary>
public class PostingRule
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TriggeringEvent EventType { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>قالب الـ Journal Entry المُنشأ عند تفعيل القاعدة (JSON).</summary>
    public string TemplateJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// قالب القيد داخل PostingRule.TemplateJson.
/// مصمّم كـ POCO ليكون typed عند الـ deserialization.
/// </summary>
public class PostingRuleTemplate
{
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }

    /// <summary>السطور: كل سطر يقول debit/credit + الـ account code (يحلّ لـ ID وقت التشغيل)</summary>
    public List<PostingRuleLineTemplate> Lines { get; set; } = new();
}

public class PostingRuleLineTemplate
{
    /// <summary>كود الحساب (Account.Code) — يُحلّ لـ AccountId وقت التشغيل</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>نوع الحركة: "debit" أو "credit"</summary>
    public string Side { get; set; } = "debit";

    /// <summary>صيغة المبلغ — يدعم متغيرات من الحدث (مثال: "{amount}")</summary>
    public string AmountFormula { get; set; } = "0";
}
