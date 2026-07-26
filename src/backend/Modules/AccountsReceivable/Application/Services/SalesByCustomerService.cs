using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.AccountsReceivable.Application.Services;

public interface ISalesByCustomerService
{
    Task<SalesByCustomerReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct);
}

public sealed class SalesByCustomerService : ISalesByCustomerService
{
    private readonly IDbConnectionFactory _db;
    public SalesByCustomerService(IDbConnectionFactory db) => _db = db;

    public async Task<SalesByCustomerReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT c.id AS CustomerId, c.code AS CustomerCode, c.name AS CustomerName,
                   COUNT(si.id) AS InvoiceCount,
                   COALESCE(SUM(si.subtotal), 0) AS Subtotal,
                   COALESCE(SUM(si.tax_amount), 0) AS TaxAmount,
                   COALESCE(SUM(si.total_amount), 0) AS TotalAmount,
                   COALESCE(SUM(si.paid_amount), 0) AS PaidAmount,
                   COALESCE(SUM(si.total_amount - si.paid_amount), 0) AS Outstanding
            FROM customers c
            INNER JOIN sales_invoices si ON si.customer_id = c.id
                AND si.invoice_date >= @From AND si.invoice_date <= @To
                AND si.status IN ('Posted', 'Partial', 'Paid')
            WHERE c.company_id = @CompanyId
            GROUP BY c.id, c.code, c.name
            ORDER BY TotalAmount DESC";

        var rows = (await conn.QueryAsync<SalesByCustomerRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to }, cancellationToken: ct))).AsList();

        return new SalesByCustomerReport
        {
            From = from,
            To = to,
            GrandTotal = rows.Sum(r => r.TotalAmount),
            GrandOutstanding = rows.Sum(r => r.Outstanding),
            Rows = rows
        };
    }
}
