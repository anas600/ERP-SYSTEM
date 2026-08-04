// Sprint 28 + 30: Arabic Procurement Dev-Environment Seeder (POC #3 for the seeder pattern).
// Why this exists: same rationale as ArabicDevSeederHostedService (Sprint 26) +
// ArabicHrDevSeederHostedService (Sprint 27), but for Procurement. Reads UTF-8 JSON +
// UPSERTs POs/GRs/Bills via Dapper. Idempotent. Dev environment only.
//
// POC #3: validates the seeder framework once more. Per L17, "established pattern
// threshold = 2 implementations" — this is the 3rd implementation. After this
// sprint, the seeder framework is permanently established.
//
// Sprint 30 (DEC-105) update: previously only POs were seeded (GRs + Bills were
// stubbed out as "intentionally not implemented" because no warehouse existed).
// After Sprint 30, the default reference data seeder (DEC-101) creates
// WH-001 "المستودع الرئيسي" by default. This seeder now uses that warehouse
// and implements all three passes:
//   Pass 1: Purchase Orders (10) — with computed line sub_total + header totals
//   Pass 2: Goods Receipts (10) — one per PO, posted to default warehouse
//   Pass 3: Vendor Bills (10) — one per GR, with benchmark Journal Entries (L39)

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.SeedData;

public sealed class ArabicProcurementDevSeederHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ArabicProcurementDevSeederHostedService> _logger;

    public ArabicProcurementDevSeederHostedService(
        IDbConnectionFactory db,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<ArabicProcurementDevSeederHostedService> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[Sprint28] ArabicProcurementDevSeeder: skipped (env={Env}, Development only)", _env.EnvironmentName);
            return;
        }

        var enabled = _config.GetValue<bool>("Bootstrap:SeedProcurementScenario", false);
        if (!enabled)
        {
            _logger.LogInformation("[Sprint28] ArabicProcurementDevSeeder: skipped (Bootstrap:SeedProcurementScenario=false)");
            return;
        }

        _logger.LogInformation("[Sprint28] ArabicProcurementDevSeeder: starting (env=Development, flag=true)");

        var dataFile = ResolveDataFile();
        if (dataFile == null || !File.Exists(dataFile))
        {
            _logger.LogError("[Sprint28] ArabicProcurementDevSeeder: data file not found (tried {File})", dataFile);
            return;
        }
        _logger.LogInformation("[Sprint28] ArabicProcurementDevSeeder: loading data from {File}", dataFile);

        ArabicProcurementDevData data;
        try
        {
            var json = await File.ReadAllTextAsync(dataFile, cancellationToken);
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            data = JsonSerializer.Deserialize<ArabicProcurementDevData>(json, opts)
                   ?? throw new InvalidOperationException("Empty or null JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sprint28] ArabicProcurementDevSeeder: failed to parse data file");
            return;
        }

        var holdingId = await ResolveHoldingIdAsync(cancellationToken);
        if (holdingId == Guid.Empty)
        {
            _logger.LogError("[Sprint28] ArabicProcurementDevSeeder: no Holding found — run DefaultHoldingBootstrap first");
            return;
        }

        var adminUserId = await ResolveAdminUserIdAsync(cancellationToken);
        if (adminUserId == Guid.Empty)
        {
            _logger.LogWarning("[Sprint28] ArabicProcurementDevSeeder: no user found — created_by will be empty GUID");
        }

        using var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        int poUpdated = 0, poInserted = 0, grInserted = 0, billInserted = 0;

        // ===== Pre-flight: Default warehouse (DEC-101 always-on) =====
        // If no warehouse exists, log a warning and skip GR/Bill passes (POs still seeded).
        var warehouseId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM warehouses WHERE company_id = @HoldingId ORDER BY created_at LIMIT 1",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken));

        // Build vendor map (code → id) for fast lookup
        var vendorMap = (await conn.QueryAsync<(Guid Id, string Code)>(new CommandDefinition(
            "SELECT id, code FROM vendors WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken)))
            .ToDictionary(t => t.Code, t => t.Id);

        // Build item map (sku → id) for fast lookup
        var itemMap = (await conn.QueryAsync<(Guid Id, string Sku)>(new CommandDefinition(
            "SELECT id, sku FROM items WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken)))
            .ToDictionary(t => t.Sku, t => t.Id);

        // Build account map (code → id) for benchmark JEs
        var accountMap = (await conn.QueryAsync<(Guid Id, string Code)>(new CommandDefinition(
            "SELECT id, code FROM accounts WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken)))
            .ToDictionary(t => t.Code, t => t.Id);

        // ===== Pass 1: UPSERT purchase orders + lines (with computed totals) =====
        foreach (var po in data.PurchaseOrders ?? new())
        {
            if (!vendorMap.TryGetValue(po.VendorCode, out var vendorId))
            {
                _logger.LogWarning("[Sprint28] PO {PoNumber}: vendor {Code} not found — skipping", po.PoNumber, po.VendorCode);
                continue;
            }

            // Compute line sub_totals and header totals from the JSON.
            decimal headerSubTotal = 0m;
            var lineRows = new List<(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal SubTotal, int LineOrder)>();
            int lineOrder = 1;
            foreach (var line in po.Lines ?? new())
            {
                if (!itemMap.TryGetValue(line.ItemSku, out var itemId))
                {
                    _logger.LogWarning("[Sprint28] PO {Po} line: item {Sku} not found — skipping", po.PoNumber, line.ItemSku);
                    continue;
                }
                var sub = line.Quantity * line.UnitPrice;
                headerSubTotal += sub;
                lineRows.Add((Guid.NewGuid(), itemId, line.Quantity, line.UnitPrice, sub, lineOrder++));
            }
            // Tax = 0 for Libya default (DEC-101 / L17). Total = sub_total + tax.
            decimal headerTax = 0m;
            decimal headerTotal = headerSubTotal + headerTax;

            var existing = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM purchase_orders WHERE po_number = @Po LIMIT 1",
                new { Po = po.PoNumber }, cancellationToken: cancellationToken));

            Guid poId;
            if (existing.HasValue)
            {
                poId = existing.Value;
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE purchase_orders SET vendor_id = @VendorId, order_date = @OrderDate,
                                                expected_date = @Expected, currency = @Currency,
                                                sub_total = @SubTotal, tax_amount = @TaxAmount, total_amount = @TotalAmount,
                                                notes = @Notes, updated_at = @Now, updated_by = @UpdatedBy
                    WHERE id = @Id",
                    new
                    {
                        Id = poId,
                        VendorId = vendorId,
                        OrderDate = DateTime.Parse(po.PoDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                        Expected = DateTime.Parse(po.ExpectedDeliveryDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                        po.Currency,
                        SubTotal = headerSubTotal, TaxAmount = headerTax, TotalAmount = headerTotal,
                        po.Notes,
                        Now = now, UpdatedBy = adminUserId
                    }, cancellationToken: cancellationToken));
                poUpdated++;
            }
            else
            {
                poId = Guid.NewGuid();
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO purchase_orders
                        (id, company_id, po_number, vendor_id, order_date, expected_date,
                         status, currency, sub_total, tax_amount, total_amount, notes,
                         created_at, created_by, updated_at, updated_by)
                    VALUES
                        (@Id, @HoldingId, @Po, @VendorId, @OrderDate, @Expected,
                         'Draft', @Currency, @SubTotal, @TaxAmount, @TotalAmount, @Notes,
                         @Now, @CreatedBy, @Now, @CreatedBy)",
                    new
                    {
                        Id = poId,
                        HoldingId = holdingId,
                        Po = po.PoNumber,
                        VendorId = vendorId,
                        OrderDate = DateTime.Parse(po.PoDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                        Expected = DateTime.Parse(po.ExpectedDeliveryDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                        po.Currency,
                        SubTotal = headerSubTotal, TaxAmount = headerTax, TotalAmount = headerTotal,
                        po.Notes,
                        Now = now, CreatedBy = adminUserId
                    }, cancellationToken: cancellationToken));
                poInserted++;
            }

            // UPSERT PO lines (with computed sub_total)
            foreach (var l in lineRows)
            {
                var existingLine = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM purchase_order_lines WHERE purchase_order_id = @PoId AND item_id = @ItemId LIMIT 1",
                    new { PoId = poId, ItemId = l.ItemId }, cancellationToken: cancellationToken));

                if (existingLine.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE purchase_order_lines SET quantity = @Quantity, unit_price = @UnitPrice,
                                                     sub_total = @SubTotal, line_order = @LineOrder
                        WHERE id = @Id",
                        new
                        {
                            Id = existingLine.Value,
                            l.Quantity, l.UnitPrice, l.SubTotal, l.LineOrder
                        }, cancellationToken: cancellationToken));
                }
                else
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO purchase_order_lines
                            (id, purchase_order_id, item_id, quantity, unit_price, tax_rate,
                             sub_total, line_order)
                        VALUES
                            (@Id, @PoId, @ItemId, @Quantity, @UnitPrice, 0,
                             @SubTotal, @LineOrder)",
                        new
                        {
                            Id = l.Id, PoId = poId, ItemId = l.ItemId,
                            l.Quantity, l.UnitPrice, l.SubTotal, l.LineOrder
                        }, cancellationToken: cancellationToken));
                }
            }
        }

        // Build PO map (poNumber → poId) for GR + Bill passes
        var poMap = (await conn.QueryAsync<(Guid Id, string PoNumber)>(new CommandDefinition(
            "SELECT id, po_number FROM purchase_orders WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken)))
            .ToDictionary(t => t.PoNumber, t => t.Id);

        // ===== Pass 2: Goods Receipts (Sprint 30 DEC-105) =====
        if (warehouseId == null)
        {
            _logger.LogWarning("[Sprint28] No default warehouse exists — skipping {Count} goods receipts. DEC-101 should seed WH-001 by default.",
                data.GoodsReceipts?.Count ?? 0);
        }
        else
        {
            foreach (var gr in data.GoodsReceipts ?? new())
            {
                // Idempotency: skip if GR exists
                var existingGr = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM goods_receipts WHERE gr_number = @Gr LIMIT 1",
                    new { Gr = gr.GrNumber }, cancellationToken: cancellationToken));
                if (existingGr.HasValue) continue;

                if (!poMap.TryGetValue(gr.PoNumber, out var poId))
                {
                    _logger.LogWarning("[Sprint28] GR {Gr}: PO {Po} not found — skipping", gr.GrNumber, gr.PoNumber);
                    continue;
                }

                // Get unit cost from the PO line
                var grLines = new List<(Guid Id, Guid ItemId, decimal Quantity, decimal UnitCost, int LineOrder)>();
                int grLineOrder = 1;
                foreach (var line in gr.Lines ?? new())
                {
                    if (!itemMap.TryGetValue(line.ItemSku, out var itemId))
                    {
                        _logger.LogWarning("[Sprint28] GR {Gr} line: item {Sku} not found — skipping", gr.GrNumber, line.ItemSku);
                        continue;
                    }
                    var unitCost = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(
                        "SELECT unit_price FROM purchase_order_lines WHERE purchase_order_id = @PoId AND item_id = @ItemId LIMIT 1",
                        new { PoId = poId, ItemId = itemId }, cancellationToken: cancellationToken)) ?? 0m;
                    grLines.Add((Guid.NewGuid(), itemId, line.Quantity, unitCost, grLineOrder++));
                }

                var grId = Guid.NewGuid();
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO goods_receipts
                        (id, company_id, gr_number, purchase_order_id, status, received_date,
                         warehouse_id, notes, created_at, created_by, updated_at, updated_by)
                    VALUES
                        (@Id, @HoldingId, @Gr, @PoId, 'Received', @ReceivedDate,
                         @WarehouseId, @Notes, @Now, @CreatedBy, @Now, @CreatedBy)",
                    new
                    {
                        Id = grId,
                        HoldingId = holdingId,
                        Gr = gr.GrNumber,
                        PoId = poId,
                        ReceivedDate = DateTime.Parse(gr.ReceiptDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                        WarehouseId = warehouseId.Value,
                        gr.Notes,
                        Now = now, CreatedBy = adminUserId
                    }, cancellationToken: cancellationToken));

                foreach (var l in grLines)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO goods_receipt_lines
                            (id, goods_receipt_id, item_id, quantity, unit_cost, line_order)
                        VALUES
                            (@Id, @GrId, @ItemId, @Quantity, @UnitCost, @LineOrder)",
                        new
                        {
                            Id = l.Id, GrId = grId, ItemId = l.ItemId,
                            l.Quantity, l.UnitCost, l.LineOrder
                        }, cancellationToken: cancellationToken));
                }
                grInserted++;
            }
        }

        // Build GR map (poNumber → grId) for Bill pass
        var grMap = (await conn.QueryAsync<(Guid Id, string GrNumber)>(new CommandDefinition(
            "SELECT id, gr_number FROM goods_receipts WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken)))
            .ToDictionary(t => t.GrNumber, t => t.Id);

        // ===== Pass 3: Vendor Bills (Sprint 30 DEC-105) — with benchmark JEs =====
        foreach (var bill in data.VendorBills ?? new())
        {
            // Idempotency: skip if bill exists
            var existingBill = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM vendor_bills WHERE bill_number = @Num LIMIT 1",
                new { Num = bill.BillNumber }, cancellationToken: cancellationToken));
            if (existingBill.HasValue) continue;

            if (!vendorMap.TryGetValue(bill.VendorCode, out var vendorId))
            {
                _logger.LogWarning("[Sprint28] Bill {Num}: vendor {Code} not found — skipping", bill.BillNumber, bill.VendorCode);
                continue;
            }
            // Find the GR for this bill (via PO link)
            Guid? grId = null;
            if (poMap.TryGetValue(bill.PoNumber, out var billPoId))
            {
                // Find GR for this PO
                grId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM goods_receipts WHERE purchase_order_id = @PoId LIMIT 1",
                    new { PoId = billPoId }, cancellationToken: cancellationToken));
            }

            // Compute bill totals from lines
            decimal billSubTotal = 0m;
            var billLines = new List<(Guid Id, Guid ItemId, decimal Quantity, decimal UnitPrice, decimal SubTotal, int LineOrder)>();
            int billLineOrder = 1;
            foreach (var line in bill.Lines ?? new())
            {
                if (!itemMap.TryGetValue(line.ItemSku, out var itemId))
                {
                    _logger.LogWarning("[Sprint28] Bill {Num} line: item {Sku} not found — skipping", bill.BillNumber, line.ItemSku);
                    continue;
                }
                var sub = line.Quantity * line.UnitPrice;
                billSubTotal += sub;
                billLines.Add((Guid.NewGuid(), itemId, line.Quantity, line.UnitPrice, sub, billLineOrder++));
            }
            decimal billTax = 0m;
            decimal billTotal = billSubTotal + billTax;

            var billId = Guid.NewGuid();
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO vendor_bills
                    (id, company_id, bill_number, goods_receipt_id, vendor_id, status,
                     bill_date, due_date, currency, sub_total, tax_amount, total_amount, notes,
                     created_at, created_by, updated_at, updated_by)
                VALUES
                    (@Id, @HoldingId, @Num, @GrId, @VendorId, 'Posted',
                     @BillDate, @DueDate, @Currency, @SubTotal, @TaxAmount, @TotalAmount, @Notes,
                     @Now, @CreatedBy, @Now, @CreatedBy)",
                new
                {
                    Id = billId,
                    HoldingId = holdingId,
                    Num = bill.BillNumber,
                    GrId = grId,
                    VendorId = vendorId,
                    BillDate = DateTime.Parse(bill.BillDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                    DueDate = DateTime.Parse(bill.DueDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                    bill.Currency,
                    SubTotal = billSubTotal, TaxAmount = billTax, TotalAmount = billTotal,
                    bill.Notes,
                    Now = now, CreatedBy = adminUserId
                }, cancellationToken: cancellationToken));

            foreach (var l in billLines)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO vendor_bill_lines
                        (id, company_id, vendor_id, vendor_bill_id, item_id,
                         quantity, unit_price, tax_rate, sub_total, line_order)
                    VALUES
                        (@Id, @HoldingId, @VendorId, @BillId, @ItemId,
                         @Quantity, @UnitPrice, 0, @SubTotal, @LineOrder)",
                    new
                    {
                        Id = l.Id, HoldingId = holdingId, VendorId = vendorId, BillId = billId,
                        ItemId = l.ItemId, l.Quantity, l.UnitPrice, l.SubTotal, l.LineOrder
                    }, cancellationToken: cancellationToken));
            }

            // Benchmark JE for the bill (L39): DR Inventory (1240) / CR AP (2210)
            if (accountMap.ContainsKey("1240") && accountMap.ContainsKey("2210") && billSubTotal > 0)
            {
                var benchEntryId = Guid.NewGuid();
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_entries
                        (id, company_id, entry_number, entry_date, description, reference, status,
                         created_by_user_id, created_at, updated_at)
                    VALUES
                        (@Id, @HoldingId, @EntryNumber, @EntryDate, @Description, @Reference, 2,
                         @UserId, @Now, @Now)",
                    new
                    {
                        Id = benchEntryId,
                        HoldingId = holdingId,
                        EntryNumber = $"BENCH-BILL-{bill.BillNumber}",
                        EntryDate = DateTime.Parse(bill.BillDate, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime(),
                        Description = $"فاتورة مشتريات {bill.BillNumber} — {bill.Notes}",
                        Reference = bill.BillNumber,
                        UserId = adminUserId, Now = now
                    }, cancellationToken: cancellationToken));

                // DR Inventory
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines
                        (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id)
                    VALUES
                        (@Id, @JournalEntryId, @AccountId, @Debit, 0, @Description, 1, @HoldingId)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = benchEntryId,
                        AccountId = accountMap["1240"],
                        Debit = billSubTotal,
                        Description = "إثبات مشتريات (مخزون)",
                        HoldingId = holdingId
                    }, cancellationToken: cancellationToken));
                // CR AP
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO journal_lines
                        (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id)
                    VALUES
                        (@Id, @JournalEntryId, @AccountId, 0, @Credit, @Description, 2, @HoldingId)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        JournalEntryId = benchEntryId,
                        AccountId = accountMap["2210"],
                        Credit = billSubTotal,
                        Description = "إثبات ذمم دائنة",
                        HoldingId = holdingId
                    }, cancellationToken: cancellationToken));

                // Link the bill to its benchmark JE
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE vendor_bills SET journal_entry_id = @JeId, posted_at = @Now WHERE id = @Id",
                    new { JeId = benchEntryId, Now = now, Id = billId }, cancellationToken: cancellationToken));
            }
            billInserted++;
        }

        _logger.LogInformation(
            "[Sprint28] ArabicProcurementDevSeeder: completed — POs updated={PU} inserted={PI}, GRs inserted={GI}, Bills inserted={BI} (benchmark JEs included)",
            poUpdated, poInserted, grInserted, billInserted);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private string? ResolveDataFile()
    {
        var configured = _config.GetValue<string>("ArabicSeeder:ProcurementDataFile");
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(configured);
            if (!Path.IsPathRooted(configured))
            {
                candidates.Add(Path.Combine(AppContext.BaseDirectory, configured));
                candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), configured));
            }
        }
        else
        {
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Shared", "SeedData", "ArabicProcurementDevData.json"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "ArabicProcurementDevData.json"));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Shared", "SeedData", "ArabicProcurementDevData.json"));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Shared", "SeedData", "ArabicProcurementDevData.json"));
        }
        foreach (var path in candidates)
        {
            if (File.Exists(path)) return Path.GetFullPath(path);
        }
        return null;
    }

    private async Task<Guid> ResolveHoldingIdAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            @"SELECT id FROM companies
              WHERE is_group = true
                AND parent_company_id IS NULL
                AND code = '000'
              LIMIT 1",
            cancellationToken: ct));
        return id ?? Guid.Empty;
    }

    private async Task<Guid> ResolveAdminUserIdAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM users ORDER BY created_at LIMIT 1",
            cancellationToken: ct));
        return id ?? Guid.Empty;
    }
}

// ============ DTOs ============

public sealed class ArabicProcurementDevData
{
    [JsonPropertyName("purchaseOrders")]
    public List<ArabicProcurementDevPO>? PurchaseOrders { get; set; }

    [JsonPropertyName("goodsReceipts")]
    public List<ArabicProcurementDevGR>? GoodsReceipts { get; set; }

    [JsonPropertyName("vendorBills")]
    public List<ArabicProcurementDevBill>? VendorBills { get; set; }
}

public sealed class ArabicProcurementDevPO
{
    [JsonPropertyName("poNumber")]              public string PoNumber { get; set; } = "";
    [JsonPropertyName("vendorCode")]            public string VendorCode { get; set; } = "";
    [JsonPropertyName("poDate")]                public string PoDate { get; set; } = "";
    [JsonPropertyName("expectedDeliveryDate")]  public string ExpectedDeliveryDate { get; set; } = "";
    [JsonPropertyName("currency")]              public string Currency { get; set; } = "LYD";
    [JsonPropertyName("notes")]                 public string? Notes { get; set; }
    [JsonPropertyName("lines")]                 public List<ArabicProcurementDevLine>? Lines { get; set; }
}

public sealed class ArabicProcurementDevGR
{
    [JsonPropertyName("grNumber")]     public string GrNumber { get; set; } = "";
    [JsonPropertyName("poNumber")]     public string PoNumber { get; set; } = "";
    [JsonPropertyName("receiptDate")]  public string ReceiptDate { get; set; } = "";
    [JsonPropertyName("notes")]        public string? Notes { get; set; }
    [JsonPropertyName("lines")]        public List<ArabicProcurementDevLine>? Lines { get; set; }
}

public sealed class ArabicProcurementDevBill
{
    [JsonPropertyName("billNumber")]  public string BillNumber { get; set; } = "";
    [JsonPropertyName("vendorCode")]  public string VendorCode { get; set; } = "";
    [JsonPropertyName("poNumber")]    public string PoNumber { get; set; } = "";
    [JsonPropertyName("billDate")]    public string BillDate { get; set; } = "";
    [JsonPropertyName("dueDate")]     public string DueDate { get; set; } = "";
    [JsonPropertyName("currency")]    public string Currency { get; set; } = "LYD";
    [JsonPropertyName("notes")]       public string? Notes { get; set; }
    [JsonPropertyName("lines")]       public List<ArabicProcurementDevLine>? Lines { get; set; }
}

public sealed class ArabicProcurementDevLine
{
    [JsonPropertyName("itemSku")]     public string ItemSku { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("quantity")]    public decimal Quantity { get; set; }
    [JsonPropertyName("unitPrice")]   public decimal UnitPrice { get; set; }
}
