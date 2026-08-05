using Dapper;
using ERPSystem.Modules.AccountsReceivable.Application;
using ERPSystem.Modules.Procurement.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Procurement.Application.Services;

// ============== Vendor Statement Service (Sprint 36, DEC-122) ==============

public interface IVendorStatementService
{
    /// <summary>
    /// كشـف حساب مورّد (Vendor Statement): رصيد افتتاحي + كل فواتير المورد والمدفوعات في الفترة
    /// + رصيد ختامي. Posted bills/payments فقط.
    /// </summary>
    Task<ProcurementResult<VendorStatementResponse>> GetStatementAsync(
        Guid vendorId, DateTime? from, DateTime? to, CancellationToken ct);
}

public sealed class VendorStatementService : IVendorStatementService
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorBillRepository _bills;
    private readonly Shared.Infrastructure.IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<VendorStatementService> _logger;

    public VendorStatementService(
        IVendorRepository vendors,
        IVendorBillRepository bills,
        Shared.Infrastructure.IDbConnectionFactory db,
        ICompanyContext companyContext,
        ILogger<VendorStatementService> logger)
    {
        _vendors = vendors;
        _bills = bills;
        _db = db;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<ProcurementResult<VendorStatementResponse>> GetStatementAsync(
        Guid vendorId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        // L19: companyId filter
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        var vendor = await _vendors.GetByIdAsync(vendorId, ct);
        if (vendor == null || vendor.CompanyId != companyId)
            return ProcurementResult<VendorStatementResponse>.Fail("المورّد غير موجود.", ProcurementErrorCode.NotFound);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 1) Opening Balance: outstanding bills قبل `from` ناقص المدفوعات المخصصة
        //    (positive = we owe them)
        decimal opening = 0m;
        if (from.HasValue)
        {
            var p = new DynamicParameters();
            p.Add("VendorId", vendorId);
            p.Add("From", from.Value);
            const string openSql = @"
                SELECT COALESCE(SUM(CASE WHEN vb.status NOT IN ('Cancelled', 'Draft')
                                        THEN vb.total_amount - vb.paid_amount ELSE 0 END), 0) AS BillOutstanding
                FROM vendor_bills vb
                WHERE vb.vendor_id = @VendorId
                  AND vb.bill_date < @From
                  AND vb.status NOT IN ('Cancelled', 'Draft')";
            var row = await conn.QueryFirstOrDefaultAsync<decimal>(
                new CommandDefinition(openSql, p, cancellationToken: ct));
            opening = row;
        }

        // 2) Period: كل فواتير المورّد المُرحَّلة + كل المدفوعات (PartyType='Vendor') في الفترة
        var p2 = new DynamicParameters();
        p2.Add("VendorId", vendorId);
        var billSql = @"
            SELECT id, bill_number AS BillNumber, bill_date AS Date,
                   total_amount AS TotalAmount, paid_amount AS PaidAmount, status,
                   COALESCE(notes, '') AS Notes
            FROM vendor_bills
            WHERE vendor_id = @VendorId AND status NOT IN ('Cancelled', 'Draft')";
        if (from.HasValue) { billSql += " AND bill_date >= @From"; p2.Add("From", from.Value); }
        if (to.HasValue) { billSql += " AND bill_date <= @To"; p2.Add("To", to.Value); }
        billSql += " ORDER BY bill_date, bill_number";
        var bills = (await conn.QueryAsync<StatementBillRow>(new CommandDefinition(billSql, p2, cancellationToken: ct))).ToList();

        var p3 = new DynamicParameters();
        p3.Add("VendorId", vendorId);
        // payments حيث PartyType = 'Vendor' (مدفوعاتنا للمورد)
        var paySql = @"
            SELECT id, payment_number AS PaymentNumber, payment_date AS Date,
                   amount AS Amount, status,
                   COALESCE(notes, '') AS Notes
            FROM payments
            WHERE party_type = 'Vendor' AND party_id = @VendorId AND posted_at IS NOT NULL";
        if (from.HasValue) { paySql += " AND payment_date >= @From"; p3.Add("From", from.Value); }
        if (to.HasValue) { paySql += " AND payment_date <= @To"; p3.Add("To", to.Value); }
        paySql += " ORDER BY payment_date, payment_number";
        var payments = (await conn.QueryAsync<StatementPaymentRow>(new CommandDefinition(paySql, p3, cancellationToken: ct))).ToList();

        // 3) Build chronological lines + running balance (we owe them)
        //    Bills: Dr to us (we owe them — increases balance)
        //    Payments: Cr to us (we paid them — decreases balance)
        var lines = new List<StatementLineResponse>();
        decimal running = opening;
        decimal totalBilled = 0m, totalPaid = 0m;

        if (from.HasValue && Math.Abs(opening) > 0.0001m)
        {
            lines.Add(new StatementLineResponse
            {
                Date = from.Value,
                Type = "Opening",
                Reference = "",
                Description = "رصيد افتتاحي (مستحق)",
                Debit = 0,
                Credit = opening,
                RunningBalance = opening
            });
        }

        var combined = bills
            .Select(b => new { Date = b.Date, Kind = "Bill", Ref = b.BillNumber, Desc = b.Notes, Dr = b.TotalAmount, Cr = 0m })
            .Concat(payments.Select(p => new { Date = p.Date, Kind = "Payment", Ref = p.PaymentNumber, Desc = p.Notes, Dr = 0m, Cr = p.Amount }))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Ref)
            .ToList();

        foreach (var x in combined)
        {
            running += x.Dr - x.Cr;
            if (x.Kind == "Bill") totalBilled += x.Dr;
            else totalPaid += x.Cr;
            lines.Add(new StatementLineResponse
            {
                Date = x.Date,
                Type = x.Kind == "Bill" ? "فاتورة مورّد" : "دفعة",
                Reference = x.Ref,
                Description = x.Desc,
                Debit = x.Dr,
                Credit = x.Cr,
                RunningBalance = running
            });
        }

        return ProcurementResult<VendorStatementResponse>.Ok(new VendorStatementResponse
        {
            VendorId = vendor.Id,
            VendorCode = vendor.Code,
            VendorName = vendor.Name,
            From = from,
            To = to,
            OpeningBalance = opening,
            TotalBilled = totalBilled,
            TotalPaid = totalPaid,
            ClosingBalance = running,
            Lines = lines
        });
    }

    private sealed class StatementBillRow
    {
        public Guid Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    private sealed class StatementPaymentRow
    {
        public Guid Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
