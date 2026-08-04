using System.Data;
using System.Text.Json;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Sprint 29 — Year-Scenario Dev Seeder (POC #4 of the seeder pattern).
///
/// Why: Anas wanted a full year of realistic operational data on the dev host
/// to discover bugs (and for the Mavis to discover bugs). The previous POCs
/// (Sprint 26 = master data, Sprint 27 = HR, Sprint 28 = procurement) gave
/// us the static data, but no transactional flow. This seeder adds:
///
///   1. Opening Balance Journal Entry (Jan 1, 2025) — initializes the books
///   2. 12 monthly sales invoices (Jan–Dec 2025) — generates AR + Revenue
///   3. 12 monthly vendor bills (Jan–Dec 2025) — generates AP + Inventory
///   4. 24 customer receipts (2/month) — partial payments on invoices
///   5. 24 vendor payments (2/month) — partial payments on bills
///
/// For each transaction, a "benchmark" Journal Entry is also inserted that
/// matches what the Posting Rules engine should produce. Any discrepancy
/// between the benchmark JE and what PostingRulesService would create is
/// a bug — that's how we discover them.
///
/// Scope: DEC-088/L17 seeder pattern. JSON + IHostedService + UPSERT + Dapper +
/// double-gate (env=Development + flag). Idempotent (UPSERT by document number).
///
/// Gating: requires `IsDevelopment()` AND `Bootstrap:SeedYearScenario=true`.
/// </summary>
public sealed class ArabicYearScenarioDevSeederHostedService : IHostedService
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<ArabicYearScenarioDevSeederHostedService> _logger;

    public ArabicYearScenarioDevSeederHostedService(
        IHostEnvironment env,
        IConfiguration config,
        IDbConnectionFactory dbFactory,
        ILogger<ArabicYearScenarioDevSeederHostedService> logger)
    {
        _env = env;
        _config = config;
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[SPRINT-29] SeedYearScenario=false (env=Production) — ArabicYearScenarioDevSeeder SKIPPED.");
            return;
        }

        var enabled = _config.GetValue("Bootstrap:SeedYearScenario", false);
        if (!enabled)
        {
            _logger.LogInformation("[SPRINT-29] SeedYearScenario=false (default) — ArabicYearScenarioDevSeeder SKIPPED.");
            return;
        }

        _logger.LogInformation("[SPRINT-29] SeedYearScenario=true + env=Development — ArabicYearScenarioDevSeeder running…");

        try
        {
            // Resolve the company. IHostedService is Singleton, so we can't inject the
            // scoped ICompanyContext. Query the first holding company directly from the DB.
            // L28 (Sprint 28): the column is `is_group` (not `is_holding`). A holding is a
            // top-level group company with `is_group = true` and `parent_company_id IS NULL`.
            Guid companyId;
            using (var conn0 = await _dbFactory.CreateEphemeralOltpConnectionAsync(cancellationToken))
            {
                companyId = await conn0.QuerySingleOrDefaultAsync<Guid>(
                    "SELECT id FROM companies WHERE is_group = true AND parent_company_id IS NULL ORDER BY created_at LIMIT 1");
            }
            if (companyId == Guid.Empty)
            {
                _logger.LogError("[SPRINT-29] No holding company found — DefaultHoldingBootstrap should have run first.");
                return;
            }

            // Load the JSON
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Shared", "SeedData", "ArabicYearScenarioDevData.json");
            if (!File.Exists(jsonPath))
            {
                _logger.LogError("[SPRINT-29] JSON not found at {Path} — ArabicYearScenarioDevSeeder SKIPPED.", jsonPath);
                return;
            }
            var json = await File.ReadAllTextAsync(jsonPath, cancellationToken);
            var data = JsonSerializer.Deserialize<YearScenarioData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data == null)
            {
                _logger.LogError("[SPRINT-29] JSON deserialization failed — ArabicYearScenarioDevSeeder SKIPPED.");
                return;
            }

            using var conn = await _dbFactory.CreateEphemeralOltpConnectionAsync(cancellationToken);

            // ===== Build lookup maps =====
            var accountMap = (await conn.QueryAsync<(Guid id, string code)>(
                "SELECT id, code FROM accounts WHERE company_id = @CompanyId",
                new { CompanyId = companyId }))
                .ToDictionary(t => t.code, t => t.id);

            var customerMap = (await conn.QueryAsync<(Guid id, string code)>(
                "SELECT id, code FROM customers WHERE company_id = @CompanyId",
                new { CompanyId = companyId }))
                .ToDictionary(t => t.code, t => t.id);

            var vendorMap = (await conn.QueryAsync<(Guid id, string code)>(
                "SELECT id, code FROM vendors WHERE company_id = @CompanyId",
                new { CompanyId = companyId }))
                .ToDictionary(t => t.code, t => t.id);

            var itemMap = (await conn.QueryAsync<(Guid id, string sku)>(
                "SELECT id, sku FROM items WHERE company_id = @CompanyId",
                new { CompanyId = companyId }))
                .ToDictionary(t => t.sku, t => t.id);

            // ===== Pass 1: Opening Balance Journal Entry =====
            if (data.OpeningBalance != null)
            {
                await SeedOpeningBalanceAsync(conn, data.OpeningBalance, companyId, accountMap, cancellationToken);
            }

            // ===== Pass 2: Sales Invoices =====
            if (data.SalesInvoices != null)
            {
                foreach (var inv in data.SalesInvoices)
                {
                    await SeedSalesInvoiceAsync(conn, inv, companyId, accountMap, customerMap, itemMap, cancellationToken);
                }
            }

            // ===== Pass 3: Vendor Bills =====
            if (data.VendorBills != null)
            {
                foreach (var bill in data.VendorBills)
                {
                    await SeedVendorBillAsync(conn, bill, companyId, accountMap, vendorMap, itemMap, cancellationToken);
                }
            }

            // ===== Pass 4: Customer Receipts =====
            if (data.Receipts != null)
            {
                foreach (var rct in data.Receipts)
                {
                    await SeedReceiptAsync(conn, rct, companyId, accountMap, customerMap, cancellationToken);
                }
            }

            // ===== Pass 5: Vendor Payments =====
            if (data.Payments != null)
            {
                foreach (var pay in data.Payments)
                {
                    await SeedPaymentAsync(conn, pay, companyId, accountMap, vendorMap, cancellationToken);
                }
            }

            _logger.LogInformation("[SPRINT-29] ArabicYearScenarioDevSeeder done.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SPRINT-29] ArabicYearScenarioDevSeeder FAILED — app will continue but year-scenario data may be incomplete.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ==================== Pass implementations ====================

    private async Task SeedOpeningBalanceAsync(IDbConnection conn, OpeningBalance ob, Guid companyId,
        Dictionary<string, Guid> accountMap, CancellationToken ct)
    {
        // Idempotency: skip if a JE with this entry_number already exists
        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM journal_entries WHERE company_id = @CompanyId AND entry_number = @EntryNumber",
            new { CompanyId = companyId, EntryNumber = ob.Reference });
        if (existing > 0)
        {
            _logger.LogInformation("[SPRINT-29] Opening Balance JE {Ref} already exists — skipping.", ob.Reference);
            return;
        }

        var entryId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference, status, created_by_user_id, created_at, updated_at)
            VALUES (@Id, @CompanyId, @EntryNumber, @EntryDate, @Description, @Reference, 2, (SELECT id FROM users WHERE email = 'admin@erp.local' LIMIT 1), now(), now())",
            new
            {
                Id = entryId,
                CompanyId = companyId,
                EntryNumber = ob.Reference,
                EntryDate = DateTime.Parse(ob.EntryDate).ToUniversalTime(),
                Description = ob.Description,
                Reference = ob.Reference
            });

        int lineNo = 1;
        foreach (var line in ob.Lines)
        {
            if (!accountMap.TryGetValue(line.AccountCode, out var accountId))
            {
                _logger.LogWarning("[SPRINT-29] Account {Code} not found — opening balance line skipped.", line.AccountCode);
                continue;
            }
            await conn.ExecuteAsync(@"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id)
                VALUES (@Id, @JournalEntryId, @AccountId, @Debit, @Credit, @Description, @LineNumber, @CompanyId)",
                new
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = entryId,
                    AccountId = accountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Description = line.Description,
                    LineNumber = lineNo++,
                    CompanyId = companyId
                });
        }

        _logger.LogInformation("[SPRINT-29] Opening Balance JE {Ref} inserted ({Lines} lines, total debits={Debits}).",
            ob.Reference, ob.Lines.Count, ob.Lines.Sum(l => l.Debit));
    }

    private async Task SeedSalesInvoiceAsync(IDbConnection conn, SalesInvoice inv, Guid companyId,
        Dictionary<string, Guid> accountMap, Dictionary<string, Guid> customerMap, Dictionary<string, Guid> itemMap,
        CancellationToken ct)
    {
        if (!customerMap.TryGetValue(inv.CustomerCode, out var customerId))
        {
            _logger.LogWarning("[SPRINT-29] Customer {Code} not found — sales invoice {Num} skipped.", inv.CustomerCode, inv.InvoiceNumber);
            return;
        }

        // Idempotency: skip if invoice already exists
        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sales_invoices WHERE company_id = @CompanyId AND invoice_number = @Num",
            new { CompanyId = companyId, Num = inv.InvoiceNumber });
        if (existing > 0) return;

        // Compute totals
        decimal subtotal = 0;
        var lineRows = new List<object>();
        int lineNo = 1;
        foreach (var l in inv.Lines)
        {
            var lineTotal = l.Quantity * l.UnitPrice;
            subtotal += lineTotal;
            lineRows.Add(new
            {
                Id = Guid.NewGuid(),
                ItemId = itemMap.TryGetValue(l.ItemSku, out var iid) ? (Guid?)iid : null,
                Description = l.ItemSku,
                LineNumber = lineNo++,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = lineTotal
            });
        }

        var invoiceId = Guid.NewGuid();
        var userId = await GetAdminUserIdAsync(conn, companyId, ct);

        await conn.ExecuteAsync(@"
            INSERT INTO sales_invoices (id, company_id, customer_id, invoice_number, invoice_date, due_date,
                currency_code, exchange_rate, subtotal, tax_amount, total_amount, paid_amount, status, is_deleted,
                notes, created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @CompanyId, @CustomerId, @InvoiceNumber, @InvoiceDate, @DueDate,
                'LYD', 1, @Subtotal, 0, @Total, 0, 'Posted', false,
                @Notes, now(), @UserId, now(), @UserId)",
            new
            {
                Id = invoiceId,
                CompanyId = companyId,
                CustomerId = customerId,
                InvoiceNumber = inv.InvoiceNumber,
                InvoiceDate = DateTime.Parse(inv.InvoiceDate).ToUniversalTime(),
                DueDate = DateTime.Parse(inv.DueDate).ToUniversalTime(),
                Subtotal = subtotal,
                Total = subtotal,
                Notes = inv.Notes,
                UserId = userId
            });

        foreach (var l in lineRows)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO sales_invoice_lines (id, sales_invoice_id, item_id, description, line_number, quantity, unit_price, tax_rate, line_total)
                VALUES (@Id, @InvoiceId, @ItemId, @Description, @LineNumber, @Quantity, @UnitPrice, 0, @LineTotal)",
                new
                {
                    Id = ((dynamic)l).Id,
                    InvoiceId = invoiceId,
                    ItemId = ((dynamic)l).ItemId,
                    Description = ((dynamic)l).Description,
                    LineNumber = ((dynamic)l).LineNumber,
                    Quantity = ((dynamic)l).Quantity,
                    UnitPrice = ((dynamic)l).UnitPrice,
                    LineTotal = ((dynamic)l).LineTotal
                });
        }

        // Benchmark JE for the sales invoice: DR AR (1230) / CR Sales Revenue (5110)
        var jeId = await InsertBenchmarkJeAsync(conn, companyId, $"BENCH-INV-{inv.InvoiceNumber}",
            inv.InvoiceDate, $"فاتورة مبيعات {inv.InvoiceNumber} — {inv.Notes}",
            new[]
            {
                (AccountCode: "1230", Debit: subtotal, Credit: 0m, Description: "إثبات ذمم مدينة"),
                (AccountCode: "5110", Debit: 0m, Credit: subtotal, Description: "إثبات إيراد مبيعات")
            }, accountMap, userId, ct);

        await conn.ExecuteAsync(
            "UPDATE sales_invoices SET journal_entry_id = @JeId, posted_at = now(), posted_by = @UserId WHERE id = @Id",
            new { JeId = jeId, UserId = userId, Id = invoiceId });
    }

    private async Task SeedVendorBillAsync(IDbConnection conn, VendorBill bill, Guid companyId,
        Dictionary<string, Guid> accountMap, Dictionary<string, Guid> vendorMap, Dictionary<string, Guid> itemMap,
        CancellationToken ct)
    {
        if (!vendorMap.TryGetValue(bill.VendorCode, out var vendorId))
        {
            _logger.LogWarning("[SPRINT-29] Vendor {Code} not found — vendor bill {Num} skipped.", bill.VendorCode, bill.BillNumber);
            return;
        }

        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM vendor_bills WHERE company_id = @CompanyId AND bill_number = @Num",
            new { CompanyId = companyId, Num = bill.BillNumber });
        if (existing > 0) return;

        decimal subtotal = 0;
        var lineRows = new List<(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal SubTotal, int LineOrder)>();
        int order = 1;
        foreach (var l in bill.Lines)
        {
            if (!itemMap.TryGetValue(l.ItemSku, out var itemId))
            {
                _logger.LogWarning("[SPRINT-29] Item {Sku} not found — bill line skipped.", l.ItemSku);
                continue;
            }
            var sub = l.Quantity * l.UnitPrice;
            subtotal += sub;
            lineRows.Add((Guid.NewGuid(), itemId, l.Quantity, l.UnitPrice, sub, order++));
        }

        var billId = Guid.NewGuid();
        var userId = await GetAdminUserIdAsync(conn, companyId, ct);

        await conn.ExecuteAsync(@"
            INSERT INTO vendor_bills (id, company_id, bill_number, vendor_id, status, bill_date, due_date,
                currency, sub_total, tax_amount, total_amount, notes, created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @CompanyId, @BillNumber, @VendorId, 'Posted', @BillDate, @DueDate,
                'LYD', @Subtotal, 0, @Total, @Notes, now(), @UserId, now(), @UserId)",
            new
            {
                Id = billId,
                CompanyId = companyId,
                BillNumber = bill.BillNumber,
                VendorId = vendorId,
                BillDate = DateTime.Parse(bill.BillDate).ToUniversalTime(),
                DueDate = DateTime.Parse(bill.DueDate).ToUniversalTime(),
                Subtotal = subtotal,
                Total = subtotal,
                Notes = bill.Notes,
                UserId = userId
            });

        foreach (var l in lineRows)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO vendor_bill_lines (id, company_id, vendor_id, vendor_bill_id, item_id, quantity, unit_price, tax_rate, sub_total, line_order)
                VALUES (@Id, @CompanyId, @VendorId, @BillId, @ItemId, @Quantity, @UnitPrice, 0, @SubTotal, @LineOrder)",
                new
                {
                    Id = l.Id, CompanyId = companyId, VendorId = vendorId, BillId = billId,
                    ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                    SubTotal = l.SubTotal, LineOrder = l.LineOrder
                });
        }

        // Benchmark JE: DR Inventory (1240) / CR AP (2210)
        var jeId = await InsertBenchmarkJeAsync(conn, companyId, $"BENCH-BILL-{bill.BillNumber}",
            bill.BillDate, $"فاتورة مشتريات {bill.BillNumber} — {bill.Notes}",
            new[]
            {
                (AccountCode: "1240", Debit: subtotal, Credit: 0m, Description: "إثبات مشتريات (مخزون)"),
                (AccountCode: "2210", Debit: 0m, Credit: subtotal, Description: "إثبات ذمم دائنة")
            }, accountMap, userId, ct);

        await conn.ExecuteAsync(
            "UPDATE vendor_bills SET journal_entry_id = @JeId, posted_at = now() WHERE id = @Id",
            new { JeId = jeId, Id = billId });
    }

    private async Task SeedReceiptAsync(IDbConnection conn, Receipt rct, Guid companyId,
        Dictionary<string, Guid> accountMap, Dictionary<string, Guid> customerMap, CancellationToken ct)
    {
        if (!customerMap.TryGetValue(rct.CustomerCode, out var customerId))
        {
            _logger.LogWarning("[SPRINT-29] Customer {Code} not found — receipt {Num} skipped.", rct.CustomerCode, rct.ReceiptNumber);
            return;
        }

        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM receipts WHERE company_id = @CompanyId AND receipt_number = @Num",
            new { CompanyId = companyId, Num = rct.ReceiptNumber });
        if (existing > 0) return;

        var userId = await GetAdminUserIdAsync(conn, companyId, ct);
        var receiptId = Guid.NewGuid();

        await conn.ExecuteAsync(@"
            INSERT INTO receipts (id, company_id, customer_id, receipt_number, receipt_date, amount,
                currency_code, payment_method, notes, created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @CompanyId, @CustomerId, @ReceiptNumber, @ReceiptDate, @Amount,
                'LYD', @PaymentMethod, @Notes, now(), @UserId, now(), @UserId)",
            new
            {
                Id = receiptId, CompanyId = companyId, CustomerId = customerId,
                ReceiptNumber = rct.ReceiptNumber,
                ReceiptDate = DateTime.Parse(rct.ReceiptDate).ToUniversalTime(),
                Amount = rct.Amount, PaymentMethod = rct.PaymentMethod, Notes = rct.Notes,
                UserId = userId
            });

        // Benchmark JE: DR Cash (1210) / CR AR (1230)
        var jeId = await InsertBenchmarkJeAsync(conn, companyId, $"BENCH-RCT-{rct.ReceiptNumber}",
            rct.ReceiptDate, $"سند قبض {rct.ReceiptNumber} — {rct.Notes}",
            new[]
            {
                (AccountCode: "1210", Debit: rct.Amount, Credit: 0m, Description: "تحصيل نقدية"),
                (AccountCode: "1230", Debit: 0m, Credit: rct.Amount, Description: "تخفيض ذمم مدينة")
            }, accountMap, userId, ct);

        await conn.ExecuteAsync(
            "UPDATE receipts SET journal_entry_id = @JeId, posted_at = now(), posted_by = @UserId WHERE id = @Id",
            new { JeId = jeId, UserId = userId, Id = receiptId });
    }

    private async Task SeedPaymentAsync(IDbConnection conn, Payment pay, Guid companyId,
        Dictionary<string, Guid> accountMap, Dictionary<string, Guid> vendorMap, CancellationToken ct)
    {
        if (!vendorMap.TryGetValue(pay.VendorCode, out var vendorId))
        {
            _logger.LogWarning("[SPRINT-29] Vendor {Code} not found — payment {Num} skipped.", pay.VendorCode, pay.PaymentNumber);
            return;
        }

        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payments WHERE company_id = @CompanyId AND payment_number = @Num",
            new { CompanyId = companyId, Num = pay.PaymentNumber });
        if (existing > 0) return;

        var userId = await GetAdminUserIdAsync(conn, companyId, ct);
        var paymentId = Guid.NewGuid();

        await conn.ExecuteAsync(@"
            INSERT INTO payments (id, company_id, party_type, party_id, payment_number, payment_date, amount,
                currency_code, payment_method, status, is_deleted, notes, created_at, created_by, updated_at, updated_by)
            VALUES (@Id, @CompanyId, @PartyType, @PartyId, @PaymentNumber, @PaymentDate, @Amount,
                'LYD', @PaymentMethod, 1, false, @Notes, now(), @UserId, now(), @UserId)",
            new
            {
                Id = paymentId, CompanyId = companyId, PartyType = pay.PartyType, PartyId = vendorId,
                PaymentNumber = pay.PaymentNumber,
                PaymentDate = DateTime.Parse(pay.PaymentDate).ToUniversalTime(),
                Amount = pay.Amount, PaymentMethod = pay.PaymentMethod, Notes = pay.Notes,
                UserId = userId
            });

        // Benchmark JE: DR AP (2210) / CR Cash (1210)
        var jeId = await InsertBenchmarkJeAsync(conn, companyId, $"BENCH-PAY-{pay.PaymentNumber}",
            pay.PaymentDate, $"سند دفع {pay.PaymentNumber} — {pay.Notes}",
            new[]
            {
                (AccountCode: "2210", Debit: pay.Amount, Credit: 0m, Description: "تخفيض ذمم دائنة"),
                (AccountCode: "1210", Debit: 0m, Credit: pay.Amount, Description: "دفع نقدية")
            }, accountMap, userId, ct);

        await conn.ExecuteAsync(
            "UPDATE payments SET journal_entry_id = @JeId, posted_at = now(), posted_by = @UserId WHERE id = @Id",
            new { JeId = jeId, UserId = userId, Id = paymentId });
    }

    private async Task<Guid> InsertBenchmarkJeAsync(IDbConnection conn, Guid companyId, string entryNumber,
        string entryDate, string description,
        (string AccountCode, decimal Debit, decimal Credit, string Description)[] lines,
        Dictionary<string, Guid> accountMap, Guid userId, CancellationToken ct)
    {
        // Idempotency: skip if already exists
        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM journal_entries WHERE company_id = @CompanyId AND entry_number = @Num",
            new { CompanyId = companyId, Num = entryNumber });
        if (existing > 0)
        {
            return await conn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM journal_entries WHERE company_id = @CompanyId AND entry_number = @Num",
                new { CompanyId = companyId, Num = entryNumber });
        }

        var entryId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, status, created_by_user_id, created_at, updated_at)
            VALUES (@Id, @CompanyId, @EntryNumber, @EntryDate, @Description, 2, @UserId, now(), now())",
            new
            {
                Id = entryId, CompanyId = companyId, EntryNumber = entryNumber,
                EntryDate = DateTime.Parse(entryDate).ToUniversalTime(),
                Description = description, UserId = userId
            });

        int lineNo = 1;
        foreach (var line in lines)
        {
            if (!accountMap.TryGetValue(line.AccountCode, out var accountId))
            {
                _logger.LogWarning("[SPRINT-29] Account {Code} not found — benchmark JE line skipped.", line.AccountCode);
                continue;
            }
            await conn.ExecuteAsync(@"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id)
                VALUES (@Id, @JournalEntryId, @AccountId, @Debit, @Credit, @Description, @LineNumber, @CompanyId)",
                new
                {
                    Id = Guid.NewGuid(), JournalEntryId = entryId, AccountId = accountId,
                    Debit = line.Debit, Credit = line.Credit,
                    Description = line.Description, LineNumber = lineNo++, CompanyId = companyId
                });
        }
        return entryId;
    }

    private async Task<Guid> GetAdminUserIdAsync(IDbConnection conn, Guid companyId, CancellationToken ct)
    {
        var result = await conn.ExecuteScalarAsync<Guid>(
            "SELECT id FROM users WHERE email = 'admin@erp.local' LIMIT 1");
        return result == Guid.Empty ? Guid.Empty : result;
    }
}

// ==================== JSON DTOs ====================

public class YearScenarioData
{
    public string? Scenario { get; set; }
    public string? Description { get; set; }
    public string? DefaultCurrency { get; set; }
    public OpeningBalance? OpeningBalance { get; set; }
    public List<SalesInvoice>? SalesInvoices { get; set; }
    public List<VendorBill>? VendorBills { get; set; }
    public List<Receipt>? Receipts { get; set; }
    public List<Payment>? Payments { get; set; }
}

public class OpeningBalance
{
    public string? EntryDate { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public List<OpeningBalanceLine>? Lines { get; set; }
}

public class OpeningBalanceLine
{
    public string? AccountCode { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

public class SalesInvoice
{
    public string? CustomerCode { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? InvoiceDate { get; set; }
    public string? DueDate { get; set; }
    public string? Notes { get; set; }
    public List<SalesInvoiceLine>? Lines { get; set; }
}

public class SalesInvoiceLine
{
    public string? ItemSku { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class VendorBill
{
    public string? VendorCode { get; set; }
    public string? BillNumber { get; set; }
    public string? BillDate { get; set; }
    public string? DueDate { get; set; }
    public string? Notes { get; set; }
    public List<VendorBillLine>? Lines { get; set; }
}

public class VendorBillLine
{
    public string? ItemSku { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Receipt
{
    public string? CustomerCode { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? ReceiptDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class Payment
{
    public string? PartyType { get; set; }
    public string? VendorCode { get; set; }
    public string? PaymentNumber { get; set; }
    public string? PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
