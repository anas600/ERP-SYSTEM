using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.AccountsReceivable.Application.Services;

public interface ITopCustomersService
{
    Task<TopCustomersReport> GetAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct);
}

public sealed class TopCustomersService : ITopCustomersService
{
    private readonly IDbConnectionFactory _db;
    public TopCustomersService(IDbConnectionFactory db) => _db = db;

    public async Task<TopCustomersReport> GetAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT c.id AS CustomerId, c.code AS CustomerCode, c.name AS CustomerName,
                   COUNT(si.id) AS InvoiceCount,
                   COALESCE(SUM(si.total_amount), 0) AS TotalAmount
            FROM customers c
            INNER JOIN sales_invoices si ON si.customer_id = c.id
                AND si.invoice_date >= @From AND si.invoice_date <= @To
                AND si.status IN ('Posted', 'Partial', 'Paid')
            WHERE c.company_id = @CompanyId
            GROUP BY c.id, c.code, c.name
            ORDER BY TotalAmount DESC
            LIMIT @Limit";

        var rows = (await conn.QueryAsync<TopCustomerRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to, Limit = limit }, cancellationToken: ct))).AsList();

        for (int i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;

        return new TopCustomersReport { From = from, To = to, Limit = limit, Rows = rows };
    }
}
