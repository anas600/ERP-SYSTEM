using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface ICollectionsService
{
    Task<CollectionsReport> GetAsync(Guid companyId, DateTime? from, DateTime? to, CancellationToken ct);
}

public sealed class CollectionsService : ICollectionsService
{
    private readonly IDbConnectionFactory _db;
    public CollectionsService(IDbConnectionFactory db) => _db = db;

    public async Task<CollectionsReport> GetAsync(Guid companyId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT r.id AS ReceiptId, r.receipt_number AS ReceiptNumber, r.receipt_date AS ReceiptDate,
                   c.code AS CustomerCode, c.name AS CustomerName,
                   r.payment_method AS PaymentMethod, r.amount AS Amount, r.currency_code AS Currency,
                   COALESCE(r.notes, '') AS Notes
            FROM receipts r
            INNER JOIN customers c ON c.id = r.customer_id
            WHERE r.company_id = @CompanyId
              AND (@From::timestamptz IS NULL OR r.receipt_date >= @From)
              AND (@To::timestamptz IS NULL OR r.receipt_date <= @To)
            ORDER BY r.receipt_date DESC";

        var rows = (await conn.QueryAsync<CollectionsRow>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to }, cancellationToken: ct))).AsList();

        return new CollectionsReport
        {
            From = from,
            To = to,
            TotalAmount = rows.Sum(r => r.Amount),
            Count = rows.Count,
            Rows = rows
        };
    }
}
