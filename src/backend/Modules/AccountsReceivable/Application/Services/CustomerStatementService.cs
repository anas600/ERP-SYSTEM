using Dapper;
using ERPSystem.Modules.AccountsReceivable.Application;
using ERPSystem.Modules.AccountsReceivable.Entities;
using ERPSystem.Modules.AccountsReceivable.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.AccountsReceivable.Application.Services;

// ============== Customer Statement Service (Sprint 36, DEC-122) ==============

public interface ICustomerStatementService
{
    /// <summary>
    /// كشـف حساب عميل (Customer Statement): رصيد افتتاحي + كل الفواتير والمقبوضات في الفترة
    /// + رصيد ختامي. Posted invoices/receipts فقط.
    /// </summary>
    Task<ArResult<CustomerStatementResponse>> GetStatementAsync(
        Guid customerId, DateTime? from, DateTime? to, CancellationToken ct);
}

public sealed class CustomerStatementService : ICustomerStatementService
{
    private readonly ICustomerRepository _customers;
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IReceiptRepository _receipts;
    private readonly Shared.Infrastructure.IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<CustomerStatementService> _logger;

    public CustomerStatementService(
        ICustomerRepository customers,
        ISalesInvoiceRepository invoices,
        IReceiptRepository receipts,
        Shared.Infrastructure.IDbConnectionFactory db,
        ICompanyContext companyContext,
        ILogger<CustomerStatementService> logger)
    {
        _customers = customers;
        _invoices = invoices;
        _receipts = receipts;
        _db = db;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<ArResult<CustomerStatementResponse>> GetStatementAsync(
        Guid customerId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        // L19: companyId filter — customer must belong to current tenant
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        var customer = await _customers.GetByIdAsync(customerId, ct);
        if (customer == null || customer.CompanyId != companyId)
            return ArResult<CustomerStatementResponse>.Fail("العميل غير موجود.", ArErrorCode.NotFound);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 1) Opening Balance: مجموع المبالغ غير المسددة من الفواتير السابقة
        //    (المبلغ الكلي - المدفوع) ناقص المقبوضات المخصصة قبل `from`
        decimal opening = 0m;
        if (from.HasValue)
        {
            var p = new DynamicParameters();
            p.Add("CustomerId", customerId);
            p.Add("From", from.Value);
            const string openSql = @"
                SELECT
                  COALESCE(SUM(CASE WHEN si.status NOT IN ('Cancelled') THEN si.total_amount - si.paid_amount ELSE 0 END), 0) AS InvoiceOutstanding,
                  COALESCE((SELECT SUM(ra.amount_applied)
                            FROM receipt_allocations ra
                            INNER JOIN receipts r ON r.id = ra.receipt_id
                            WHERE ra.sales_invoice_id IN (SELECT id FROM sales_invoices WHERE customer_id = @CustomerId)
                              AND r.receipt_date < @From
                              AND r.posted_at IS NOT NULL), 0) AS PreReceipts
                FROM sales_invoices si
                WHERE si.customer_id = @CustomerId
                  AND si.invoice_date < @From
                  AND si.status NOT IN ('Cancelled', 'Draft')";
            var row = await conn.QueryFirstOrDefaultAsync<(decimal InvoiceOutstanding, decimal PreReceipts)>(
                new CommandDefinition(openSql, p, cancellationToken: ct));
            // Opening = outstanding invoices قبل from - receipts المخصصة لتلك الفواتير قبل from
            // لكن receipts المخصصة في receipt_allocations تكون دائماً مرتبطة بـ invoices
            // الـ outstanding المحسوب في sales_invoices.paid_amount يحسب الـ allocations
            // إذا الـ receipt posted، paid_amount يزيد. لو الـ receipt posted بعد from،
            // الـ outstanding قبل from يحسبه صحيح.
            opening = row.InvoiceOutstanding;
        }

        // 2) Period: كل الفواتير المُرحَّلة في الفترة + المقبوضات المُرحَّلة في الفترة
        var p2 = new DynamicParameters();
        p2.Add("CustomerId", customerId);
        var invSql = @"
            SELECT id, invoice_number AS InvoiceNumber, invoice_date AS Date,
                   total_amount AS TotalAmount, paid_amount AS PaidAmount, status,
                   COALESCE(notes, '') AS Notes
            FROM sales_invoices
            WHERE customer_id = @CustomerId AND status NOT IN ('Cancelled', 'Draft')";
        if (from.HasValue) { invSql += " AND invoice_date >= @From"; p2.Add("From", from.Value); }
        if (to.HasValue) { invSql += " AND invoice_date <= @To"; p2.Add("To", to.Value); }
        invSql += " ORDER BY invoice_date, invoice_number";
        var invoices = (await conn.QueryAsync<StatementInvoiceRow>(new CommandDefinition(invSql, p2, cancellationToken: ct))).ToList();

        var p3 = new DynamicParameters();
        p3.Add("CustomerId", customerId);
        var recSql = @"
            SELECT id, receipt_number AS ReceiptNumber, receipt_date AS Date,
                   amount AS Amount, status,
                   COALESCE(notes, '') AS Notes
            FROM receipts
            WHERE customer_id = @CustomerId AND posted_at IS NOT NULL";
        if (from.HasValue) { recSql += " AND receipt_date >= @From"; p3.Add("From", from.Value); }
        if (to.HasValue) { recSql += " AND receipt_date <= @To"; p3.Add("To", to.Value); }
        recSql += " ORDER BY receipt_date, receipt_number";
        var receipts = (await conn.QueryAsync<StatementReceiptRow>(new CommandDefinition(recSql, p3, cancellationToken: ct))).ToList();

        // 3) Build chronological lines + running balance
        var lines = new List<StatementLineResponse>();
        decimal running = opening;
        decimal totalInvoiced = 0m, totalReceived = 0m;

        // Open Balance line
        if (from.HasValue && Math.Abs(opening) > 0.0001m)
        {
            lines.Add(new StatementLineResponse
            {
                Date = from.Value,
                Type = "Opening",
                Reference = "",
                Description = "رصيد افتتاحي",
                Debit = opening > 0 ? opening : 0,
                Credit = opening < 0 ? -opening : 0,
                RunningBalance = opening
            });
        }

        // Merge invoices + receipts sorted by date
        var combined = invoices
            .Select(i => new { Date = i.Date, Kind = "Invoice", Ref = i.InvoiceNumber, Desc = i.Notes, Dr = i.TotalAmount, Cr = 0m, Inv = (object)i, Rec = (object?)null })
            .Concat(receipts.Select(r => new { Date = r.Date, Kind = "Receipt", Ref = r.ReceiptNumber, Desc = r.Notes, Dr = 0m, Cr = r.Amount, Inv = (object?)null, Rec = (object)r }))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Ref)
            .ToList();

        foreach (var x in combined)
        {
            running += x.Dr - x.Cr;
            if (x.Kind == "Invoice") totalInvoiced += x.Dr;
            else totalReceived += x.Cr;
            lines.Add(new StatementLineResponse
            {
                Date = x.Date,
                Type = x.Kind == "Invoice" ? "فاتورة" : "سند قبض",
                Reference = x.Ref,
                Description = x.Desc,
                Debit = x.Dr,
                Credit = x.Cr,
                RunningBalance = running
            });
        }

        return ArResult<CustomerStatementResponse>.Ok(new CustomerStatementResponse
        {
            CustomerId = customer.Id,
            CustomerCode = customer.Code,
            CustomerName = customer.Name,
            From = from,
            To = to,
            OpeningBalance = opening,
            TotalInvoiced = totalInvoiced,
            TotalReceived = totalReceived,
            ClosingBalance = running,
            Lines = lines
        });
    }

    private sealed class StatementInvoiceRow
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    private sealed class StatementReceiptRow
    {
        public Guid Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
