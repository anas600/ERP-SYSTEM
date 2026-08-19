using ERPSystem.Modules.Finance.Application;

namespace ERPSystem.Modules.AccountsReceivable.Application;

// ============== Customer Statement DTOs (Sprint 36) ==============

public sealed class CustomerStatementResponse
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    /// <summary>الرصيد الافتتاحي (المتبقي من الفواتير - المقبوضات قبل From)</summary>
    public decimal OpeningBalance { get; set; }
    /// <summary>إجمالي الفواتير في الفترة (المبلغ الكلي)</summary>
    public decimal TotalInvoiced { get; set; }
    /// <summary>إجمالي المقبوضات في الفترة (المخصص للعميل)</summary>
    public decimal TotalReceived { get; set; }
    /// <summary>الرصيد الختامي (Opening + Invoiced - Received)</summary>
    public decimal ClosingBalance { get; set; }
    public List<StatementLineResponse> Lines { get; set; } = new();
}

public sealed class StatementLineResponse
{
    public DateTime Date { get; set; }
    /// <summary>"Invoice" | "Receipt" | "Opening"</summary>
    public string Type { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

// ============== Vendor Statement DTOs (Sprint 36) ==============

public sealed class VendorStatementResponse
{
    public Guid VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    /// <summary>الرصيد الافتتاحي (المتبقي من الفواتير - المدفوعات قبل From)</summary>
    public decimal OpeningBalance { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<StatementLineResponse> Lines { get; set; } = new();
}
