using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Procurement.Application.Services;

public interface ITopVendorsService
{
    Task<TopVendorsReport> GetAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct);
}

public sealed class TopVendorsService : ITopVendorsService
{
    private readonly IDbConnectionFactory _db;
    public TopVendorsService(IDbConnectionFactory db) => _db = db;

    public async Task<TopVendorsReport> GetAsync(Guid companyId, DateTime from, DateTime to, int limit, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT v.id AS VendorId, v.code AS VendorCode, v.name AS VendorName,
                   COUNT(vb.id) AS BillCount,
                   COALESCE(SUM(vb.total_amount), 0) AS TotalAmount
            FROM vendors v
            INNER JOIN vendor_bills vb ON vb.vendor_id = v.id
                AND vb.bill_date >= @From AND vb.bill_date <= @To
                AND vb.status = 'Posted'
            WHERE v.company_id = @CompanyId
            GROUP BY v.id, v.code, v.name
            ORDER BY TotalAmount DESC
            LIMIT @Limit";

        var rows = (await conn.QueryAsync<TopVendorRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to, Limit = limit }, cancellationToken: ct))).AsList();

        for (int i = 0; i < rows.Count; i++) rows[i].Rank = i + 1;

        return new TopVendorsReport { From = from, To = to, Limit = limit, Rows = rows };
    }
}
