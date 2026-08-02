// Sprint 28: Arabic Procurement Dev-Environment Seeder (POC #3 for the seeder pattern).
// Why this exists: same rationale as ArabicDevSeederHostedService (Sprint 26) +
// ArabicHrDevSeederHostedService (Sprint 27), but for Procurement. Reads UTF-8 JSON +
// UPSERTs POs via Dapper. Idempotent. Dev environment only.
//
// POC #3: validates the seeder framework once more. Per L17, "established pattern
// threshold = 2 implementations" — this is the 3rd implementation. After this
// sprint, the seeder framework is permanently established.
//
// Scope (Sprint 28, simplified): Purchase orders + lines only (10 POs).
// Why not GRs + bills: those require a warehouse_id (NOT NULL FK), and the
// Sprint 26 seeder didn't create warehouses. The seeder logs a warning and
// skips them. A future sprint can add a default warehouse + GR/bill seeder.

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
        int poUpdated = 0, poInserted = 0, grSkipped = 0, billSkipped = 0;

        // ===== Pass 1: UPSERT purchase orders + lines =====
        foreach (var po in data.PurchaseOrders ?? new())
        {
            var vendorId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM vendors WHERE code = @Code LIMIT 1",
                new { Code = po.VendorCode }, cancellationToken: cancellationToken));
            if (!vendorId.HasValue)
            {
                _logger.LogWarning("[Sprint28] PO {PoNumber}: vendor {Code} not found — skipping", po.PoNumber, po.VendorCode);
                continue;
            }

            var existing = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM purchase_orders WHERE po_number = @Po LIMIT 1",
                new { Po = po.PoNumber }, cancellationToken: cancellationToken));

            Guid poId;
            if (existing.HasValue)
            {
                poId = existing.Value;
                // Sprint 28 (DEC-095): include company_id in UPDATE.
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE purchase_orders SET vendor_id = @VendorId, order_date = @OrderDate,
                                                expected_date = @Expected, currency = @Currency,
                                                notes = @Notes, updated_at = @Now, updated_by = @UpdatedBy
                    WHERE id = @Id",
                    new
                    {
                        Id = poId,
                        VendorId = vendorId.Value,
                        OrderDate = DateTime.Parse(po.PoDate, System.Globalization.CultureInfo.InvariantCulture),
                        Expected = DateTime.Parse(po.ExpectedDeliveryDate, System.Globalization.CultureInfo.InvariantCulture),
                        po.Currency, po.Notes,
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
                         'Draft', @Currency, 0, 0, 0, @Notes,
                         @Now, @CreatedBy, @Now, @CreatedBy)",
                    new
                    {
                        Id = poId,
                        HoldingId = holdingId,
                        Po = po.PoNumber,
                        VendorId = vendorId.Value,
                        OrderDate = DateTime.Parse(po.PoDate, System.Globalization.CultureInfo.InvariantCulture),
                        Expected = DateTime.Parse(po.ExpectedDeliveryDate, System.Globalization.CultureInfo.InvariantCulture),
                        po.Currency, po.Notes,
                        Now = now, CreatedBy = adminUserId
                    }, cancellationToken: cancellationToken));
                poInserted++;
            }

            // UPSERT PO lines
            int lineOrder = 1;
            foreach (var line in po.Lines ?? new())
            {
                var itemId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM items WHERE sku = @Sku LIMIT 1",
                    new { Sku = line.ItemSku }, cancellationToken: cancellationToken));
                if (!itemId.HasValue)
                {
                    _logger.LogWarning("[Sprint28] PO {Po} line: item {Sku} not found — skipping", po.PoNumber, line.ItemSku);
                    continue;
                }

                var existingLine = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM purchase_order_lines WHERE purchase_order_id = @PoId AND item_id = @ItemId LIMIT 1",
                    new { PoId = poId, ItemId = itemId.Value }, cancellationToken: cancellationToken));

                if (existingLine.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE purchase_order_lines SET quantity = @Quantity, unit_price = @UnitPrice,
                                                     line_order = @LineOrder
                        WHERE id = @Id",
                        new
                        {
                            Id = existingLine.Value,
                            line.Quantity, line.UnitPrice, LineOrder = lineOrder
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
                             0, @LineOrder)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            PoId = poId,
                            ItemId = itemId.Value,
                            line.Quantity, line.UnitPrice, LineOrder = lineOrder
                        }, cancellationToken: cancellationToken));
                }
                lineOrder++;
            }
        }

        // ===== Pass 2: Goods Receipts — skip if no warehouse exists =====
        var hasWarehouse = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM warehouses WHERE company_id = @HoldingId",
            new { HoldingId = holdingId }, cancellationToken: cancellationToken)) > 0;
        if (!hasWarehouse && (data.GoodsReceipts?.Count ?? 0) > 0)
        {
            _logger.LogWarning("[Sprint28] No warehouse exists for Holding — skipping {Count} goods receipts. Add a default warehouse to enable GR seeder.",
                data.GoodsReceipts?.Count ?? 0);
            grSkipped = data.GoodsReceipts?.Count ?? 0;
        }
        // (GR seeder intentionally not implemented in Sprint 28 — see class doc.)

        // ===== Pass 3: Vendor Bills — same constraint =====
        if (!hasWarehouse && (data.VendorBills?.Count ?? 0) > 0)
        {
            _logger.LogWarning("[Sprint28] No warehouse exists — skipping {Count} vendor bills. Add a default warehouse to enable bill seeder.",
                data.VendorBills?.Count ?? 0);
            billSkipped = data.VendorBills?.Count ?? 0;
        }

        _logger.LogInformation(
            "[Sprint28] ArabicProcurementDevSeeder: completed — POs updated={PU} inserted={PI}, GRs skipped={GS} (no warehouse), Bills skipped={BS}",
            poUpdated, poInserted, grSkipped, billSkipped);
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
