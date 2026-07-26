using System;
using System.Collections.Generic;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Inventory.Entities;
using ERPSystem.Modules.Projects.Entities;

namespace ERPSystem.Modules.Reports.Application;

// ===== Project Reports =====

public sealed class ProjectPnL
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal Revenue { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal SubcontractorCost { get; set; }
    public decimal AllocatedOverhead { get; set; }
    public decimal DirectCosts => MaterialCost + LaborCost + SubcontractorCost;
    public decimal NetProfit => Revenue - DirectCosts - AllocatedOverhead;
    public decimal MarginPercent => Revenue > 0 ? (NetProfit / Revenue) * 100 : 0;
}

public sealed class ProjectBudgetVsActual
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal AvailableAmount => BudgetAmount - SpentAmount - CommittedAmount;
    public decimal Variance => BudgetAmount - SpentAmount;
    public decimal VariancePercent => BudgetAmount > 0 ? (Variance / BudgetAmount) * 100 : 0;
    public decimal UtilizationPercent => BudgetAmount > 0 ? (SpentAmount / BudgetAmount) * 100 : 0;
    public DateTime? LastRecalculatedAt { get; set; }
}

public sealed class ProjectSummary
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public decimal Budget { get; set; }
    public decimal Spent { get; set; }
    public decimal MarginPercent { get; set; }
    public DateTime? LastActivity { get; set; }
}

// ===== Inventory Reports =====

public sealed class StockValuation
{
    public Guid ItemId { get; set; }
    public string ItemSku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue => QuantityOnHand * AverageCost;
}

public sealed class StockMovementHistory
{
    public Guid MovementId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public StockMovementType Type { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class LowStockItem
{
    public Guid ItemId { get; set; }
    public string ItemSku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public decimal Shortfall => ReorderLevel - QuantityAvailable;
    public string Status => QuantityOnHand == 0
        ? "Critical"
        : (QuantityOnHand < ReorderLevel / 2 ? "Warning" : "Low");
}

public sealed class StockAging
{
    public Guid ItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public DateTime? LastMovementAt { get; set; }
    public int? DaysInStock { get; set; }
    public string AgeBucket => DaysInStock switch
    {
        null => string.Empty,
        int d when d <= 30 => "0-30",
        int d when d <= 60 => "31-60",
        int d when d <= 90 => "61-90",
        _ => "90+"
    };
}

// ===== Finance Reports =====

public sealed class TrialBalanceRow
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal NetDebit => Debit - Credit;
    public decimal NetCredit => Credit - Debit;
}

public sealed class TrialBalanceReport
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceRow> Rows { get; set; } = new();
    public decimal TotalDebit => Rows.Sum(r => r.Debit);
    public decimal TotalCredit => Rows.Sum(r => r.Credit);
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
    public decimal Variance => TotalDebit - TotalCredit;
}

public sealed class IncomeStatement
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit => Revenue - Cogs;
    public decimal OperatingExpenses { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal NetIncome => GrossProfit - OperatingExpenses + OtherIncome - OtherExpenses;
}

public sealed class BalanceSheet
{
    public DateTime AsOfDate { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;
    public decimal Variance => TotalAssets - TotalLiabilitiesAndEquity;
}

// ===== Journal Entry Report =====
public sealed class JournalEntryLineDto
{
    public Guid JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public int Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? PostedAt { get; set; }
}

public sealed class JournalEntryReport
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? Status { get; set; }
    public int TotalEntries { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<JournalEntryLineDto> Lines { get; set; } = new();
}

// ===== Account Activity / Cardex =====
public sealed class AccountActivityTransaction
{
    public Guid JournalLineId { get; set; }
    public DateTime EntryDate { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public sealed class AccountActivityResponse
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int NormalBalance { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<AccountActivityTransaction> Transactions { get; set; } = new();
}

// ===== Collections =====
public sealed class CollectionsRow
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class CollectionsReport
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
    public List<CollectionsRow> Rows { get; set; } = new();
}

// ===== Cost Center Performance =====
public sealed class CostCenterPerformanceRow
{
    public Guid CostCenterId { get; set; }
    public string CostCenterCode { get; set; } = string.Empty;
    public string CostCenterName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expense { get; set; }
    public decimal Net => Revenue - Expense;
    public decimal Margin => Revenue == 0 ? 0 : (Net / Revenue) * 100;
}

public sealed class CostCenterPerformanceReport
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalNet => TotalRevenue - TotalExpense;
    public List<CostCenterPerformanceRow> Rows { get; set; } = new();
}

// ===== VAT Report =====
public sealed class VatDetailRow
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Net { get; set; }
}

public sealed class VatReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal VatRate { get; set; } = 0.15m;
    public decimal TotalSales { get; set; }
    public decimal OutputVat { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal InputVat { get; set; }
    public decimal NetVatPayable => OutputVat - InputVat;
    public Dictionary<string, VatDetailRow> Details { get; set; } = new();
}

// ===== Sales by Customer =====
public sealed class SalesByCustomerRow
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
}

public sealed class SalesByCustomerReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal GrandOutstanding { get; set; }
    public List<SalesByCustomerRow> Rows { get; set; } = new();
}

// ===== Sales by Item =====
public sealed class SalesByItemRow
{
    public Guid ItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class SalesByItemReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal GrandTotal { get; set; }
    public List<SalesByItemRow> Rows { get; set; } = new();
}

// ===== Purchases by Vendor =====
public sealed class PurchasesByVendorRow
{
    public Guid VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int BillCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
}

public sealed class PurchasesByVendorReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal GrandOutstanding { get; set; }
    public List<PurchasesByVendorRow> Rows { get; set; } = new();
}

// ===== Top Trading Partners =====
public sealed class TopCustomerRow
{
    public int Rank { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
}

public sealed class TopCustomersReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Limit { get; set; } = 10;
    public List<TopCustomerRow> Rows { get; set; } = new();
}

public sealed class TopVendorRow
{
    public int Rank { get; set; }
    public Guid VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int BillCount { get; set; }
}

public sealed class TopVendorsReport
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Limit { get; set; } = 10;
    public List<TopVendorRow> Rows { get; set; } = new();
}

// ===== Budget vs Actual =====
public sealed class BudgetVsActualRow
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Budget - Actual;
    public decimal VariancePercent => Budget == 0 ? 0 : (Variance / Budget) * 100;
}

public sealed class BudgetVsActualReport
{
    public Guid? ProjectId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalVariance => TotalBudget - TotalActual;
    public decimal TotalVariancePercent => TotalBudget == 0 ? 0 : (TotalVariance / TotalBudget) * 100;
    public List<BudgetVsActualRow> Rows { get; set; } = new();
}
