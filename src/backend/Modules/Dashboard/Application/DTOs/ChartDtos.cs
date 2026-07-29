// Sprint 5 (T1-T3 / Phase 4) — Chart data DTOs.
//
// Three DTOs that match the FE contract in app/(authenticated)/dashboard/page.tsx:
//   - RevenueVsExpensePoint  → 1 row per month for the revenue line chart
//   - ExpenseCategorySlice   → 1 slice per expense account for the pie chart
//   - TopCustomerRow         → 1 row for the top-customers bar chart
//
// Field names are camelCase (System.Text.Json default in ASP.NET Core) so
// the FE can read them directly without any transformation.

namespace ERPSystem.Modules.Dashboard.Application.DTOs;

/// <summary>
/// One month-bucket for the revenue-vs-expense line chart.
/// "month" is an ISO yyyy-MM string (UTC) — sortable and locale-independent.
/// "revenue" and "expense" are the absolute LYD totals for the bucket.
/// "net" = revenue - expense (positive = profit, negative = loss).
/// </summary>
public sealed class RevenueVsExpensePoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}

/// <summary>
/// One expense category slice for the pie / donut chart.
/// "category" is the account name (e.g. "Rent Expense", "Salaries Expense").
/// "amount" is the absolute LYD total for the slice.
/// "color" is a fixed palette index color in CSS hex (the FE can also ignore
/// it and use its own chart color scheme — we just need a stable value to
/// keep slice colors consistent across renders).
/// </summary>
public sealed class ExpenseCategorySlice
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Color { get; set; } = "#94a3b8";
}

/// <summary>
/// One row for the top-customers bar chart.
/// Same shape as the existing TopCustomersService report so the FE can
/// reuse the type; the chart endpoint however does NOT include a "Rank"
/// (the FE renders Rank via array index).
/// </summary>
public sealed class TopCustomerChartRow
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int InvoiceCount { get; set; }
}
