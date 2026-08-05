using ERPSystem.Modules.AccountsReceivable.Application;

namespace ERPSystem.Modules.Procurement.Application;

// ============== Vendor Statement DTOs (Sprint 36) ==============
// Note: StatementLineResponse is reused from AccountsReceivable (cross-module DTO).

public sealed class VendorStatementResponse
{
    public Guid VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    /// <summary>الرصيد الافتتاحي (مستحقاتنا للمورّد قبل From) — موجب = لنا دين</summary>
    public decimal OpeningBalance { get; set; }
    /// <summary>إجمالي فواتير المورّد في الفترة (المبلغ الكلي)</summary>
    public decimal TotalBilled { get; set; }
    /// <summary>إجمالي المدفوعات في الفترة</summary>
    public decimal TotalPaid { get; set; }
    /// <summary>الرصيد الختامي (Opening + Billed - Paid) — موجب = لنا دين للمورّد</summary>
    public decimal ClosingBalance { get; set; }
    public List<StatementLineResponse> Lines { get; set; } = new();
}
