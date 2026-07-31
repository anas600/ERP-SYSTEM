using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Procurement.Application.Services;

public interface IPurchasesByVendorService
{
    Task<PurchasesByVendorReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct);
}

public sealed class PurchasesByVendorService : IPurchasesByVendorService
{
    private readonly IDbConnectionFactory _db;
    public PurchasesByVendorService(IDbConnectionFactory db) => _db = db;

    public async Task<PurchasesByVendorReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT v.id AS VendorId, v.code AS VendorCode, v.name AS VendorName,
                   COUNT(vb.id) AS BillCount,
                   COALESCE(SUM(vb.sub_total), 0) AS Subtotal,
                   COALESCE(SUM(vb.tax_amount), 0) AS TaxAmount,
                   COALESCE(SUM(vb.total_amount), 0) AS TotalAmount,
                   COALESCE(SUM(vb.total_amount - COALESCE(p.paid, 0)), 0) AS Outstanding
            FROM vendors v
            INNER JOIN vendor_bills vb ON vb.vendor_id = v.id
                AND vb.bill_date >= @From AND vb.bill_date <= @To
                AND vb.status = 'Posted'
            LEFT JOIN (
                SELECT party_id, SUM(amount) AS paid
                FROM payments
                WHERE party_type = 'Vendor' AND status = 2
                GROUP BY party_id
            ) p ON p.party_id = v.id
            WHERE v.company_id = @CompanyId
            GROUP BY v.id, v.code, v.name
            ORDER BY TotalAmount DESC";

        var rows = (await conn.QueryAsync<PurchasesByVendorRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to }, cancellationToken: ct))).AsList();

        return new PurchasesByVendorReport
        {
            From = from,
            To = to,
            GrandTotal = rows.Sum(r => r.TotalAmount),
            GrandOutstanding = rows.Sum(r => r.Outstanding),
            Rows = rows
        };
    }
}
