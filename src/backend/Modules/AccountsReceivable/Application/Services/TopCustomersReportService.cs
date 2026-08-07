// Sprint 56 (DEC-149 + DEC-150) — Path C.1: Top Customers + Top Items reports
//
// تقارير جديدة على الـ transactional tables:
// - Top Customers: أكبر العملاء حسب المبيعات (من sales_invoices) لفترة
// - Top Items: أكبر الأصناف حسب المبيعات (من sales_invoice_lines) لفترة
//
// الفترة الافتراضية: آخر 12 شهر من تاريخ asOf.
// Gating: ReadAccess policy.

using Dapper;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Modules.AccountsReceivable.Application.Services;

public interface ITopCustomersReportService
{
    Task<TopCustomersReportResponse> GetTopCustomersAsync(Guid companyId, DateTime from, DateTime to, int top, CancellationToken ct);
    Task<TopItemsReportResponse> GetTopItemsAsync(Guid companyId, DateTime from, DateTime to, int top, CancellationToken ct);
}

public sealed class TopCustomerRow
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding => TotalSales - TotalPaid;
    public decimal PercentOfTotal { get; set; }
}

public sealed class TopCustomersReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Top { get; set; }
    public List<TopCustomerRow> Rows { get; set; } = new();
    public decimal GrandTotalSales => Rows.Sum(r => r.TotalSales);
    public decimal GrandTotalPaid => Rows.Sum(r => r.TotalPaid);
    public decimal GrandOutstanding => Rows.Sum(r => r.Outstanding);
}

public sealed class TopItemRow
{
    public Guid ItemId { get; set; }
    public string? ItemSku { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalSales { get; set; }
    public int LineCount { get; set; }
    public decimal PercentOfTotal { get; set; }
}

public sealed class TopItemsReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Top { get; set; }
    public List<TopItemRow> Rows { get; set; } = new();
    public decimal GrandTotalSales => Rows.Sum(r => r.TotalSales);
    public decimal GrandTotalQuantity => Rows.Sum(r => r.TotalQuantity);
}

public sealed class TopCustomersReportService : ITopCustomersReportService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<TopCustomersReportService> _logger;

    public TopCustomersReportService(IDbConnectionFactory db, ILogger<TopCustomersReportService> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<TopCustomersReportResponse> GetTopCustomersAsync(
        Guid companyId, DateTime from, DateTime to, int top, CancellationToken ct)
    {
        using var conn = (NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);

        // Total sales لكل العملاء (لحساب النسبة)
        var totalRow = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(si.total_amount), 0)
            FROM sales_invoices si
            WHERE si.company_id = @Cid
              AND si.invoice_date >= @From AND si.invoice_date <= @To
              AND si.is_deleted = false
              AND si.status IN ('Posted','Paid','PartiallyPaid')",
            new { Cid = companyId, From = from.Date, To = to.Date },
            cancellationToken: ct)) ?? 0m;
        var grandTotal = totalRow;

        // Top N customers
        const string sql = @"
            SELECT
                c.id AS CustomerId, c.code AS CustomerCode, c.name AS CustomerName,
                COUNT(si.id) AS InvoiceCount,
                COALESCE(SUM(si.total_amount), 0) AS TotalSales,
                COALESCE(SUM(si.paid_amount), 0) AS TotalPaid
            FROM customers c
            INNER JOIN sales_invoices si ON si.customer_id = c.id
                AND si.company_id = c.company_id
                AND si.invoice_date >= @From AND si.invoice_date <= @To
                AND si.is_deleted = false
                AND si.status IN ('Posted','Paid','PartiallyPaid')
            WHERE c.company_id = @Cid AND c.is_active = true
            GROUP BY c.id, c.code, c.name
            ORDER BY TotalSales DESC
            LIMIT @Top";

        var rows = (await conn.QueryAsync<TopCustomerRaw>(new CommandDefinition(sql,
            new { Cid = companyId, From = from.Date, To = to.Date, Top = top },
            cancellationToken: ct))).ToList();

        var resp = new TopCustomersReportResponse
        {
            From = from.Date,
            To = to.Date,
            Top = top,
        };
        foreach (var r in rows)
        {
            resp.Rows.Add(new TopCustomerRow
            {
                CustomerId = r.CustomerId,
                CustomerCode = r.CustomerCode,
                CustomerName = r.CustomerName,
                InvoiceCount = r.InvoiceCount,
                TotalSales = r.TotalSales,
                TotalPaid = r.TotalPaid,
                PercentOfTotal = grandTotal > 0 ? Math.Round(r.TotalSales / grandTotal * 100m, 2) : 0m,
            });
        }
        _logger.LogInformation("[SPRINT-56] TopCustomers: {N} customers, total={Total}", rows.Count, grandTotal);
        return resp;
    }

    public async Task<TopItemsReportResponse> GetTopItemsAsync(
        Guid companyId, DateTime from, DateTime to, int top, CancellationToken ct)
    {
        using var conn = (NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);

        // Sprint 56: sales_invoice_lines لا يحتوي company_id (تم التأكد في Sprint 55)
        // الـ filter يكون على sales_invoices.company_id فقط
        var totalRow = await conn.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(@"
            SELECT COALESCE(SUM(sil.line_total), 0)
            FROM sales_invoice_lines sil
            INNER JOIN sales_invoices si ON si.id = sil.sales_invoice_id
            WHERE si.company_id = @Cid
              AND si.invoice_date >= @From AND si.invoice_date <= @To
              AND si.is_deleted = false
              AND si.status IN ('Posted','Paid','PartiallyPaid')",
            new { Cid = companyId, From = from.Date, To = to.Date },
            cancellationToken: ct)) ?? 0m;
        var grandTotal = totalRow;

        const string sql = @"
            SELECT ItemId, MAX(ItemSku) AS ItemSku, MAX(ItemName) AS ItemName,
                   SUM(TotalQuantity) AS TotalQuantity, SUM(TotalSales) AS TotalSales,
                   SUM(LineCount) AS LineCount
            FROM (
                SELECT
                    COALESCE(sil.item_id, '00000000-0000-0000-0000-000000000000'::uuid) AS ItemId,
                    MAX(i.sku) AS ItemSku,
                    COALESCE(MAX(sil.description), '— بدون وصف —') AS ItemName,
                    COALESCE(SUM(sil.quantity), 0) AS TotalQuantity,
                    COALESCE(SUM(sil.line_total), 0) AS TotalSales,
                    COUNT(*) AS LineCount
                FROM sales_invoice_lines sil
                INNER JOIN sales_invoices si ON si.id = sil.sales_invoice_id
                LEFT JOIN items i ON i.id = sil.item_id
                WHERE si.company_id = @Cid
                  AND si.invoice_date >= @From AND si.invoice_date <= @To
                  AND si.is_deleted = false
                  AND si.status IN ('Posted','Paid','PartiallyPaid')
                GROUP BY sil.item_id, sil.description
            ) t
            GROUP BY ItemId
            ORDER BY TotalSales DESC
            LIMIT @Top";

        var rows = (await conn.QueryAsync<TopItemRaw>(new CommandDefinition(sql,
            new { Cid = companyId, From = from.Date, To = to.Date, Top = top },
            cancellationToken: ct))).ToList();

        var resp = new TopItemsReportResponse
        {
            From = from.Date,
            To = to.Date,
            Top = top,
        };
        foreach (var r in rows)
        {
            resp.Rows.Add(new TopItemRow
            {
                ItemId = r.ItemId,
                ItemSku = r.ItemSku,
                ItemName = r.ItemName,
                TotalQuantity = r.TotalQuantity,
                TotalSales = r.TotalSales,
                LineCount = r.LineCount,
                PercentOfTotal = grandTotal > 0 ? Math.Round(r.TotalSales / grandTotal * 100m, 2) : 0m,
            });
        }
        _logger.LogInformation("[SPRINT-56] TopItems: {N} items, total={Total}", rows.Count, grandTotal);
        return resp;
    }

    private sealed class TopCustomerRaw
    {
        public Guid CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
    }

    private sealed class TopItemRaw
    {
        public Guid ItemId { get; set; }
        public string? ItemSku { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalSales { get; set; }
        public int LineCount { get; set; }
    }
}
