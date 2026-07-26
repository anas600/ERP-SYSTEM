using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.AccountsReceivable.Application.Services;

public interface ISalesByItemService
{
    Task<SalesByItemReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct);
}

public sealed class SalesByItemService : ISalesByItemService
{
    private readonly IDbConnectionFactory _db;
    public SalesByItemService(IDbConnectionFactory db) => _db = db;

    public async Task<SalesByItemReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT i.id AS ItemId, i.sku AS Sku, i.name AS ItemName,
                   COALESCE(SUM(sil.quantity), 0) AS Quantity,
                   COALESCE(SUM(sil.sub_total), 0) AS Subtotal,
                   COALESCE(SUM(sil.tax_amount), 0) AS TaxAmount,
                   COALESCE(SUM(sil.sub_total + sil.tax_amount), 0) AS TotalAmount
            FROM items i
            INNER JOIN sales_invoice_lines sil ON sil.item_id = i.id
            INNER JOIN sales_invoices si ON si.id = sil.invoice_id
                AND si.invoice_date >= @From AND si.invoice_date <= @To
                AND si.status IN ('Posted', 'Partial', 'Paid')
            WHERE i.company_id = @CompanyId
            GROUP BY i.id, i.sku, i.name
            ORDER BY TotalAmount DESC";

        var rows = (await conn.QueryAsync<SalesByItemRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to }, cancellationToken: ct))).AsList();

        return new SalesByItemReport
        {
            From = from,
            To = to,
            GrandTotal = rows.Sum(r => r.TotalAmount),
            Rows = rows
        };
    }
}
