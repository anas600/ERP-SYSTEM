// Sprint 55 (DEC-145..147) — Path B: Seeder refactor
//
// ينشئ وثائق صحيحة (sales_invoices + vendor_bills + payments) مع ربطها بقيود اليومية.
// الهدف: تخزين الوثائق المصدرية في الجداول الصحيحة (sales_invoices, vendor_bills, payments)
// بدل كتابة journal_lines يدويًا فقط — يدعم AR/AP aging reports و drill-down للوثائق.
//
// Gating: requires `IsDevelopment()` AND `Bootstrap:SeedProperTransactional=true`.
// Idempotent: يفحص وجود الفواتير/الفواتير قبل الإدراج.

using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Shared.SeedData;

public sealed class ProperTransactionalSeederHostedService : IHostedService
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<ProperTransactionalSeederHostedService> _logger;

    public ProperTransactionalSeederHostedService(
        IHostEnvironment env, IConfiguration config, IDbConnectionFactory dbFactory,
        ILogger<ProperTransactionalSeederHostedService> logger)
    {
        _env = env; _config = config; _dbFactory = dbFactory; _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[SPRINT-55] env != Development — SKIPPED.");
            return;
        }
        var enabled = _config.GetValue("Bootstrap:SeedProperTransactional", false);
        if (!enabled)
        {
            _logger.LogInformation("[SPRINT-55] SeedProperTransactional=false (default) — SKIPPED.");
            return;
        }
        _logger.LogInformation("[SPRINT-55] SeedProperTransactional=true + env=Development — running…");
        try
        {
            using var conn = (NpgsqlConnection)await _dbFactory.CreateEphemeralOltpConnectionAsync(ct);
            var sysUserId = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1") ?? Guid.Empty;
            var holdingId = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM companies WHERE is_group = true AND is_active = true LIMIT 1");
            if (holdingId == null) { _logger.LogWarning("[SPRINT-55] No holding company — SKIPPED."); return; }

            // Accounts (L4 postable) — Libyan unified CoA
            // Sprint 55: unified CoA يفتقد 1230 (ذمم مدينة) و 1210 (نقدية).
            // ننشئها كـ AR/Cash إذا لم تكن موجودة.
            var arAccountId = await EnsureAccountAsync(conn, holdingId.Value, new EnsureAccountRequest
            {
                Code = "1230",
                Name = "ذمم مدينة (عملاء خارجيين)",
                Type = 1, // Asset
                NormalBalance = 1, // Debit
                ParentCode = "1200", // Current Assets
            }, ct);
            var apAccountId = await GetAccountIdAsync(conn, holdingId.Value, "2210");
            var cashAccountId = await EnsureAccountAsync(conn, holdingId.Value, new EnsureAccountRequest
            {
                Code = "1210",
                Name = "النقدية بالصندوق",
                Type = 1, // Asset
                NormalBalance = 1, // Debit
                ParentCode = "1200",
            }, ct);
            var revenueAccountId = await GetAccountIdAsync(conn, holdingId.Value, "5110");
            var expenseAccountId = await GetAccountIdAsync(conn, holdingId.Value, "5520"); // مستلزمات مكتبية
            if (arAccountId == null || apAccountId == null || cashAccountId == null
                || revenueAccountId == null || expenseAccountId == null)
            {
                _logger.LogWarning("[SPRINT-55] Required accounts missing — SKIPPED. " +
                    "AR={AR} AP={AP} Cash={Cash} Rev={Rev} Exp={Exp}",
                    arAccountId, apAccountId, cashAccountId, revenueAccountId, expenseAccountId);
                return;
            }

            // Customers + Vendors (نختار 5 من كل)
            var customers = (await conn.QueryAsync<(Guid Id, int TermsDays)>(new CommandDefinition(
                "SELECT id, payment_terms_days AS TermsDays FROM customers WHERE company_id = @Cid AND is_active = true ORDER BY code LIMIT 5",
                new { Cid = holdingId.Value }))).ToList();
            var vendors = (await conn.QueryAsync<Guid>(new CommandDefinition(
                "SELECT id FROM vendors WHERE company_id = @Cid AND is_active = true ORDER BY code LIMIT 5",
                new { Cid = holdingId.Value }))).ToList();
            if (customers.Count == 0 || vendors.Count == 0)
            {
                _logger.LogWarning("[SPRINT-55] No customers/vendors — SKIPPED.");
                return;
            }

            // Items (نختار 3-5 items) — items table يستخدم sku لا code
            var items = (await conn.QueryAsync<Guid>(new CommandDefinition(
                "SELECT id FROM items WHERE company_id = @Cid AND is_active = true ORDER BY sku LIMIT 5",
                new { Cid = holdingId.Value }))).ToList();

            // ============== Sales Invoices (DEC-145) ==============
            int salesInvoicesCreated = 0;
            var seq = await GetOrCreateSequenceAsync(conn, holdingId.Value, "SI", ct);
            var invoiceDates = new[] {
                new DateTime(2026, 1, 15), new DateTime(2026, 2, 10), new DateTime(2026, 3, 20),
                new DateTime(2026, 4, 12), new DateTime(2026, 5, 8), new DateTime(2026, 6, 5),
            };
            var invoiceAmounts = new[] { 8500m, 12000m, 6500m, 15000m, 9500m, 18000m };
            for (int i = 0; i < customers.Count && i < invoiceDates.Length; i++)
            {
                var cust = customers[i];
                var invDate = invoiceDates[i];
                var invAmount = invoiceAmounts[i];

                // Idempotency: لو في فاتورة بنفس الرقم
                var invNumber = $"SI-2026-{(seq + i + 1):D4}";
                var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT 1 FROM sales_invoices WHERE company_id = @Cid AND invoice_number = @Num LIMIT 1",
                    new { Cid = holdingId.Value, Num = invNumber }, cancellationToken: ct));
                if (existing > 0) continue;

                var invId = Guid.NewGuid();
                var jeId = Guid.NewGuid();
                var dueDate = invDate.AddDays(cust.TermsDays);

                // Journal Entry (DR AR / CR Revenue)
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference, status, created_by_user_id, posted_at, created_at, updated_at)
                    VALUES (@Id, @Cid, @Num, @Date, @Desc, @Ref, 2, @Uid, NOW(), NOW(), NOW())",
                    new { Id = jeId, Cid = holdingId.Value, Num = $"JE-{invNumber}", Date = invDate,
                          Desc = $"فاتورة مبيعات {invNumber} للعميل", Ref = invNumber, Uid = sysUserId },
                    cancellationToken: ct));

                // Journal Lines (DR AR + CR Revenue)
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines (id, journal_entry_id, company_id, account_id, debit, credit, description, line_number)
                    VALUES (gen_random_uuid(), @JEId, @Cid, @AccId, @Dr, @Cr, @Desc, @Ln)",
                    new { JEId = jeId, Cid = holdingId.Value, AccId = arAccountId, Dr = invAmount, Cr = 0m,
                          Desc = $"مدين — ذمم مدينة ({invNumber})", Ln = 1 }, cancellationToken: ct));
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines (id, journal_entry_id, company_id, account_id, debit, credit, description, line_number)
                    VALUES (gen_random_uuid(), @JEId, @Cid, @AccId, @Dr, @Cr, @Desc, @Ln)",
                    new { JEId = jeId, Cid = holdingId.Value, AccId = revenueAccountId, Dr = 0m, Cr = invAmount,
                          Desc = $"دائن — إيرادات ({invNumber})", Ln = 2 }, cancellationToken: ct));

                // Sales Invoice
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO sales_invoices (id, company_id, customer_id, invoice_number, invoice_date, due_date,
                        currency_code, exchange_rate, subtotal, tax_amount, total_amount, paid_amount, status,
                        is_deleted, posted_at, posted_by, journal_entry_id, created_at, created_by, updated_at)
                    VALUES (@Id, @Cid, @CustId, @Num, @Date, @Due, 'LYD', 1, @Sub, 0, @Total, 0, 'Posted',
                        false, NOW(), @Uid, @JEId, NOW(), @Uid, NOW())",
                    new { Id = invId, Cid = holdingId.Value, CustId = cust.Id, Num = invNumber, Date = invDate, Due = dueDate,
                          Sub = invAmount, Total = invAmount, Uid = sysUserId, JEId = jeId },
                    cancellationToken: ct));

                // Sales Invoice Lines (1-2 lines per invoice)
                // Sprint 55: sales_invoice_lines schema بسيط — لا company_id, لا created_at
                var lineDesc = items.Count > 0 ? "بضاعة مورّدة" : "خدمة مقدمة";
                if (items.Count > 0)
                {
                    var itemId = items[i % items.Count];
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO sales_invoice_lines (id, sales_invoice_id, item_id, description, line_number, quantity, unit_price, tax_rate, line_total)
                        VALUES (gen_random_uuid(), @InvId, @ItemId, @Desc, 1, @Qty, @Price, 0, @Total)",
                        new { InvId = invId, ItemId = itemId, Desc = lineDesc,
                              Qty = 1m, Price = invAmount, Total = invAmount },
                        cancellationToken: ct));
                }
                else
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO sales_invoice_lines (id, sales_invoice_id, description, line_number, quantity, unit_price, tax_rate, line_total)
                        VALUES (gen_random_uuid(), @InvId, @Desc, 1, 1, @Price, 0, @Total)",
                        new { InvId = invId, Desc = lineDesc, Price = invAmount, Total = invAmount },
                        cancellationToken: ct));
                }
                salesInvoicesCreated++;
            }
            if (salesInvoicesCreated > 0)
                await UpdateSequenceAsync(conn, holdingId.Value, "SI", seq + salesInvoicesCreated, ct);

            // ============== Vendor Bills (DEC-146) ==============
            int billsCreated = 0;
            var billSeq = await GetOrCreateSequenceAsync(conn, holdingId.Value, "BILL", ct);
            var billDates = new[] {
                new DateTime(2026, 1, 20), new DateTime(2026, 2, 15), new DateTime(2026, 3, 25),
                new DateTime(2026, 4, 18), new DateTime(2026, 5, 10),
            };
            var billAmounts = new[] { 7200m, 11000m, 5500m, 13500m, 8000m };
            for (int i = 0; i < vendors.Count && i < billDates.Length; i++)
            {
                var vendorId = vendors[i];
                var billDate = billDates[i];
                var billAmount = billAmounts[i];

                var billNumber = $"BILL-2026-{(billSeq + i + 1):D4}";
                var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT 1 FROM vendor_bills WHERE company_id = @Cid AND bill_number = @Num LIMIT 1",
                    new { Cid = holdingId.Value, Num = billNumber }, cancellationToken: ct));
                if (existing > 0) continue;

                var billId = Guid.NewGuid();
                var jeId = Guid.NewGuid();
                var dueDate = billDate.AddDays(30);

                // Journal Entry (DR Expense / CR AP)
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference, status, created_by_user_id, posted_at, created_at, updated_at)
                    VALUES (@Id, @Cid, @Num, @Date, @Desc, @Ref, 2, @Uid, NOW(), NOW(), NOW())",
                    new { Id = jeId, Cid = holdingId.Value, Num = $"JE-{billNumber}", Date = billDate,
                          Desc = $"فاتورة مشتريات {billNumber} من المورد", Ref = billNumber, Uid = sysUserId },
                    cancellationToken: ct));

                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines (id, journal_entry_id, company_id, account_id, debit, credit, description, line_number)
                    VALUES (gen_random_uuid(), @JEId, @Cid, @AccId, @Dr, @Cr, @Desc, @Ln)",
                    new { JEId = jeId, Cid = holdingId.Value, AccId = expenseAccountId, Dr = billAmount, Cr = 0m,
                          Desc = $"مدين — مصروفات ({billNumber})", Ln = 1 }, cancellationToken: ct));
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines (id, journal_entry_id, company_id, account_id, debit, credit, description, line_number)
                    VALUES (gen_random_uuid(), @JEId, @Cid, @AccId, @Dr, @Cr, @Desc, @Ln)",
                    new { JEId = jeId, Cid = holdingId.Value, AccId = apAccountId, Dr = 0m, Cr = billAmount,
                          Desc = $"دائن — ذمم دائنة ({billNumber})", Ln = 2 }, cancellationToken: ct));

                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO vendor_bills (id, company_id, vendor_id, bill_number, bill_date, due_date,
                        currency, sub_total, tax_amount, total_amount, status,
                        posted_at, journal_entry_id, created_at, created_by, updated_at)
                    VALUES (@Id, @Cid, @VendId, @Num, @Date, @Due, 'LYD', @Sub, 0, @Total, 'Posted',
                        NOW(), @JEId, NOW(), @Uid, NOW())",
                    new { Id = billId, Cid = holdingId.Value, VendId = vendorId, Num = billNumber, Date = billDate, Due = dueDate,
                          Sub = billAmount, Total = billAmount, Uid = sysUserId, JEId = jeId },
                    cancellationToken: ct));

                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO vendor_bill_lines (id, company_id, vendor_id, vendor_bill_id, item_id, quantity, unit_price, tax_rate, sub_total, line_order)
                    VALUES (gen_random_uuid(), @Cid, @VendId, @BillId, @ItemId, 1, @Price, 0, @Total, 1)",
                    new
                    {
                        Cid = holdingId.Value,
                        BillId = billId,
                        VendId = vendorId,
                        ItemId = items.Count > 0 ? (object?)items[i % items.Count] : null,
                        Price = billAmount,
                        Total = billAmount,
                    },
                    cancellationToken: ct));
                billsCreated++;
            }
            if (billsCreated > 0)
                await UpdateSequenceAsync(conn, holdingId.Value, "BILL", billSeq + billsCreated, ct);

            // ============== Payments (DEC-147) ==============
            int paymentsCreated = 0;
            var paySeq = await GetOrCreateSequenceAsync(conn, holdingId.Value, "PAY", ct);
            // 3 payments — تحصيلات من العملاء
            var receiptAmounts = new[] { 8500m, 12000m, 9500m };
            for (int i = 0; i < Math.Min(customers.Count, receiptAmounts.Length); i++)
            {
                var cust = customers[i];
                var payDate = invoiceDates[i].AddDays(cust.TermsDays);
                var payAmount = receiptAmounts[i];

                var payNumber = $"PAY-2026-{(paySeq + paymentsCreated + 1):D4}";
                var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT 1 FROM payments WHERE company_id = @Cid AND payment_number = @Num LIMIT 1",
                    new { Cid = holdingId.Value, Num = payNumber }, cancellationToken: ct));
                if (existing > 0) continue;

                var payId = Guid.NewGuid();
                var jeId = Guid.NewGuid();

                // Journal Entry (DR Cash / CR AR)
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference, status, created_by_user_id, posted_at, created_at, updated_at)
                    VALUES (@Id, @Cid, @Num, @Date, @Desc, @Ref, 2, @Uid, NOW(), NOW(), NOW())",
                    new { Id = jeId, Cid = holdingId.Value, Num = $"JE-{payNumber}", Date = payDate,
                          Desc = $"تحصيل من العميل ({payNumber})", Ref = payNumber, Uid = sysUserId },
                    cancellationToken: ct));
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines (id, journal_entry_id, company_id, account_id, debit, credit, description, line_number)
                    VALUES (gen_random_uuid(), @JEId, @Cid, @AccId, @Dr, @Cr, @Desc, @Ln)",
                    new { JEId = jeId, Cid = holdingId.Value, AccId = cashAccountId, Dr = payAmount, Cr = 0m,
                          Desc = $"مدين — نقدية ({payNumber})", Ln = 1 }, cancellationToken: ct));
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines (id, journal_entry_id, company_id, account_id, debit, credit, description, line_number)
                    VALUES (gen_random_uuid(), @JEId, @Cid, @AccId, @Dr, @Cr, @Desc, @Ln)",
                    new { JEId = jeId, Cid = holdingId.Value, AccId = arAccountId, Dr = 0m, Cr = payAmount,
                          Desc = $"دائن — ذمم مدينة ({payNumber})", Ln = 2 }, cancellationToken: ct));

                // Payment (party_type=Customer لأن العميل يدفع لنا)
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO payments (id, company_id, party_type, party_id, payment_number, payment_date, amount, currency_code,
                        payment_method, status, posted_at, posted_by, journal_entry_id, notes, created_at, created_by, updated_at)
                    VALUES (@Id, @Cid, 'Customer', @CustId, @Num, @Date, @Amount, 'LYD',
                        'BankTransfer', 2, NOW(), @Uid, @JEId, @Notes, NOW(), @Uid, NOW())",
                    new { Id = payId, Cid = holdingId.Value, CustId = cust.Id, Num = payNumber, Date = payDate, Amount = payAmount,
                          JEId = jeId, Notes = $"تحصيل من العميل", Uid = sysUserId },
                    cancellationToken: ct));

                // Payment Allocation (يربط الدفعة بالفاتورة)
                var invLookup = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                    @"SELECT id FROM sales_invoices WHERE company_id = @Cid AND customer_id = @CustId
                      AND invoice_number = @InvNum LIMIT 1",
                    new { Cid = holdingId.Value, CustId = cust.Id, InvNum = $"SI-2026-{(seq + i + 1):D4}" },
                    cancellationToken: ct));
                if (invLookup.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO payment_allocations (id, company_id, payment_id, ref_type, ref_id, amount_applied)
                        VALUES (gen_random_uuid(), @Cid, @PayId, 'SalesInvoice', @DocId, @Amount)",
                        new { Cid = holdingId.Value, PayId = payId, DocId = invLookup.Value, Amount = payAmount },
                        cancellationToken: ct));
                }
                paymentsCreated++;
            }
            if (paymentsCreated > 0)
                await UpdateSequenceAsync(conn, holdingId.Value, "PAY", paySeq + paymentsCreated, ct);

            _logger.LogInformation(
                "[SPRINT-55] ✓ Done. sales_invoices={SI}, vendor_bills={VB}, payments={P} (idempotent).",
                salesInvoicesCreated, billsCreated, paymentsCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SPRINT-55] Seeder failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static async Task<Guid?> GetAccountIdAsync(NpgsqlConnection conn, Guid companyId, string code)
    {
        return await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM accounts WHERE company_id = @Cid AND code = @Code LIMIT 1",
            new { Cid = companyId, Code = code }));
    }

    /// <summary>
    /// يضمن وجود حساب بالـ code المحدد. لو غير موجود، ينشئه تحت الحساب الأم المحدد.
    /// يستخدم لإصلاح فجوات في الـ unified CoA (مثل 1230 AR و 1210 Cash).
    /// </summary>
    private static async Task<Guid?> EnsureAccountAsync(NpgsqlConnection conn, Guid companyId, EnsureAccountRequest req, CancellationToken ct)
    {
        var existing = await GetAccountIdAsync(conn, companyId, req.Code);
        if (existing.HasValue) return existing;

        // جلب الـ parent
        var parentId = await GetAccountIdAsync(conn, companyId, req.ParentCode);
        if (parentId == null)
        {
            // لو الـ parent مش موجود (لا ينبغي أن يحدث في CoA صحيح)
            return null;
        }

        var newId = Guid.NewGuid();
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO accounts (id, company_id, code, name, type, normal_balance, parent_account_id,
                is_postable, is_active, level, created_at, updated_at)
            VALUES (@Id, @Cid, @Code, @Name, @Type, @NB, @Pid, true, true, 4, NOW(), NOW())",
            new { Id = newId, Cid = companyId, Code = req.Code, Name = req.Name, Type = req.Type, NB = req.NormalBalance, Pid = parentId },
            cancellationToken: ct));
        return newId;
    }

    private sealed class EnsureAccountRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public int NormalBalance { get; set; }
        public string ParentCode { get; set; } = string.Empty;
    }

    /// <summary>الـ prefixes و الـ tables: AR يستخدم ar_document_sequences ("SI")، Procurement يستخدم procurement_document_sequences ("BILL", "PAY").</summary>
    private static async Task<long> GetOrCreateSequenceAsync(NpgsqlConnection conn, Guid companyId, string prefix, CancellationToken ct)
    {
        var (table, _) = GetSequenceTableForPrefix(prefix);

        // ضمان وجود الجدول (الـ repos تنشئه عند أول استخدام)
        await EnsureSequenceTableAsync(conn, table, ct);

        var current = await conn.QueryFirstOrDefaultAsync<long?>(new CommandDefinition(
            $"SELECT last_number FROM {table} WHERE company_id = @Cid AND prefix = @Prefix LIMIT 1",
            new { Cid = companyId, Prefix = prefix }, cancellationToken: ct));
        if (current.HasValue) return current.Value;
        await conn.ExecuteAsync(new CommandDefinition($@"
            INSERT INTO {table} (company_id, prefix, last_number, created_at, updated_at)
            VALUES (@Cid, @Prefix, 0, NOW(), NOW())
            ON CONFLICT (company_id, prefix) DO NOTHING",
            new { Cid = companyId, Prefix = prefix }, cancellationToken: ct));
        return 0;
    }

    private static async Task EnsureSequenceTableAsync(NpgsqlConnection conn, string table, CancellationToken ct)
    {
        // Sprint 55: لو الجدول غير موجود، ننشئه
        await conn.ExecuteAsync(new CommandDefinition($@"
            CREATE TABLE IF NOT EXISTS {table} (
                company_id UUID NOT NULL,
                prefix VARCHAR(20) NOT NULL,
                last_number BIGINT NOT NULL DEFAULT 0,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (company_id, prefix)
            )", cancellationToken: ct));
    }

    private static async Task UpdateSequenceAsync(NpgsqlConnection conn, Guid companyId, string prefix, long newValue, CancellationToken ct)
    {
        var (table, _) = GetSequenceTableForPrefix(prefix);
        await conn.ExecuteAsync(new CommandDefinition($@"
            UPDATE {table} SET last_number = @NewVal, updated_at = NOW()
            WHERE company_id = @Cid AND prefix = @Prefix",
            new { NewVal = newValue, Cid = companyId, Prefix = prefix }, cancellationToken: ct));
    }

    private static (string Table, string PkCol) GetSequenceTableForPrefix(string prefix) => prefix switch
    {
        "SI" => ("ar_document_sequences", "(company_id, prefix)"),
        "BILL" or "PAY" => ("procurement_document_sequences", "(company_id, prefix)"),
        _ => ("procurement_document_sequences", "(company_id, prefix)"), // fallback
    };
}
