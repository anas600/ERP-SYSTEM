using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IVatReportService
{
    Task<VatReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct);
}

public sealed class VatReportService : IVatReportService
{
    private readonly IDbConnectionFactory _db;
    public VatReportService(IDbConnectionFactory db) => _db = db;

    public async Task<VatReport> GetAsync(Guid companyId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // VAT accounts: 2250 = Output VAT, 1255 = Input VAT (from default CoA)
        // Compute VAT from invoices and bills instead, using tax_amount field
        const string sql = @"
            SELECT
              COALESCE((SELECT SUM(si.tax_amount) FROM sales_invoices si
                        WHERE si.company_id = @CompanyId
                          AND si.invoice_date BETWEEN @From AND @To
                          AND si.status IN ('Posted', 'Partial', 'Paid')), 0) AS total_sales,
              COALESCE((SELECT SUM(vb.tax_amount) FROM vendor_bills vb
                        WHERE vb.company_id = @CompanyId
                          AND vb.bill_date BETWEEN @From AND @To
                          AND vb.status = 'Posted'), 0) AS total_purchases";

        var totals = await conn.QueryFirstAsync<(decimal total_sales, decimal total_purchases)>(new CommandDefinition(sql,
            new { CompanyId = companyId, From = from, To = to }, cancellationToken: ct));

        return new VatReport
        {
            From = from,
            To = to,
            VatRate = 0.15m,
            TotalSales = totals.total_sales / 0.15m,  // sales net of VAT
            OutputVat = totals.total_sales,
            TotalPurchases = totals.total_purchases / 0.15m,  // purchases net of VAT
            InputVat = totals.total_purchases,
            Details = new Dictionary<string, VatDetailRow>
            {
                ["output_vat"] = new VatDetailRow { AccountCode = "2250", AccountName = "ضريبة القيمة المضافة المستحقة", Credit = totals.total_sales },
                ["input_vat"] = new VatDetailRow { AccountCode = "1255", AccountName = "ضريبة مدفوعة مقدماً", Debit = totals.total_purchases }
            }
        };
    }
}
