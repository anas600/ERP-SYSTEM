using System;

namespace ERPSystem.Modules.Finance.Entities;

/// <summary>
/// نوع الحساب في دليل الحسابات
/// - Asset: الأصول (مدين يزيد، دائن ينقص)
/// - Liability: الخصوم (عكس الأصول)
/// - Equity: حقوق الملكية
/// - Revenue: الإيرادات (دائن عند الاعتراف)
/// - Expense: المصروفات (مدين عند الاعتراف)
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5,
}

/// <summary>
/// نوع الحساب من حيث طبيعة الرصيد
/// - Debit: طبيعته مدينة (أصول، مصروفات)
/// - Credit: طبيعته دائنة (خصوم، حقوق ملكية، إيرادات)
/// </summary>
public enum NormalBalance
{
    Debit = 1,
    Credit = 2,
}

/// <summary>
/// حساب في دليل الحسابات (Chart of Accounts).
/// يدعم الهيراركية (parent_id) لبناء شجرة حسابات.
/// </summary>
public class Account
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>كود الحساب — فريد داخل المستأجر (مثال: "1100", "4100-SALES")</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public NormalBalance NormalBalance { get; set; }

    /// <summary>للهيراركية: الحسابات الفرعية ترتبط بحساب أب</summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>هل يقبل قيود مباشرة (leaf account) أو فقط حسابات تجميعية؟</summary>
    public bool IsPostable { get; set; } = true;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
