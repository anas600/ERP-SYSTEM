// Sprint 26: Arabic Dev-Environment Seeder.
// Why this exists: the Sprint 25 PowerShell demo scripts encoded Arabic as
// literal '?' (0x3F) because PowerShell 5.1's ConvertTo-Json + Invoke-RestMethod
// pipeline sends UTF-16-LE bytes that ASP.NET Core decodes as UTF-8. The result
// was a corrupted CUST-004..013, VEND-004..013, ITEM-006..020 (10+10+15 rows).
//
// Fix: a C# hosted service that reads UTF-8 JSON directly and UPSERTs via Dapper.
// .NET's `File.ReadAllText` + `JsonSerializer.Deserialize` is UTF-8 native, so
// Arabic passes through end-to-end. C# string literals are also UTF-8 native in
// .cs files (since .NET 5+).
//
// Scope (Sprint 26): master data only — 13 customers, 13 vendors, 20 items.
// Sales invoices + receipts + JEs are already created by Sprint 25 scripts and
// are referenced by FKs, so we don't recreate them. The seeder fixes the broken
// names while leaving transactions untouched.
//
// Idempotency: UPSERT by code (customer.code, vendor.code, item.sku). On re-run,
// existing rows are UPDATED (name + name_en + email + phone + tax_id + ...);
// new rows are INSERTED. No duplicates possible.
//
// Gate: Bootstrap:SeedArabicScenario=true AND ASPNETCORE_ENVIRONMENT=Development.
// Default OFF. Local dev only — never runs in production or mvp-docker.

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// خدمة تعمل مرة واحدة عند بدء التطبيق — تبذر master data (customers + vendors + items)
/// بأسماء عربية صحيحة، بشكل idempotent. مفعّلة فقط في بيئة التطوير (Development) وعند
/// ضبط <c>Bootstrap:SeedArabicScenario=true</c> في الـ appsettings.
/// <para>
/// <b>الهدف</b>: إصلاح مشكلة encoding في Sprint 25 (PowerShell scripts كانت تخزن
/// Arabic كـ <c>?</c> literal). الحل: قراءة JSON بـ UTF-8 ثم UPSERT عبر Dapper.
/// </para>
/// <para>
/// <b>الـ Idempotency</b>: لو الـ customer موجود بالـ code، نعمل UPDATE (name + name_en
/// + tax_id + email + phone + credit_limit + payment_terms). لو مش موجود، INSERT.
/// نفس النمط للـ vendors و items. الـ seeder آمن على بيانات موجودة — لا حذف ولا تكرار.
/// </para>
/// <para>
/// <b>ملفات الإعداد</b>:
/// <list type="bullet">
///   <item><c>Bootstrap:SeedArabicScenario</c> — flag تشغيل الـ seeder (الافتراضي: false).</item>
///   <item><c>ArabicSeeder:DataFile</c> — مسار JSON (الافتراضي: <c>Shared/SeedData/ArabicDevData.json</c>).</item>
/// </list>
/// </para>
/// </summary>
public sealed class ArabicDevSeederHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ArabicDevSeederHostedService> _logger;

    public ArabicDevSeederHostedService(
        IDbConnectionFactory db,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<ArabicDevSeederHostedService> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Gate 1: only Development environment (never production)
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[Sprint26] ArabicDevSeeder: skipped (env={Env}, Development only)", _env.EnvironmentName);
            return;
        }

        // Gate 2: explicit opt-in via config
        var enabled = _config.GetValue<bool>("Bootstrap:SeedArabicScenario", false);
        if (!enabled)
        {
            _logger.LogInformation("[Sprint26] ArabicDevSeeder: skipped (Bootstrap:SeedArabicScenario=false)");
            return;
        }

        _logger.LogInformation("[Sprint26] ArabicDevSeeder: starting (env=Development, flag=true)");

        // Resolve the JSON file path
        var dataFile = ResolveDataFile();
        if (dataFile == null || !File.Exists(dataFile))
        {
            _logger.LogError("[Sprint26] ArabicDevSeeder: data file not found (tried {File})", dataFile);
            return;
        }
        _logger.LogInformation("[Sprint26] ArabicDevSeeder: loading data from {File}", dataFile);

        // Load + parse
        ArabicDevData data;
        try
        {
            var json = await File.ReadAllTextAsync(dataFile, cancellationToken);
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            data = JsonSerializer.Deserialize<ArabicDevData>(json, opts)
                   ?? throw new InvalidOperationException("Empty or null JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sprint26] ArabicDevSeeder: failed to parse data file");
            return;
        }

        // Get the Holding ID (the same one DefaultHoldingBootstrap creates)
        var holdingId = await ResolveHoldingIdAsync(cancellationToken);
        if (holdingId == Guid.Empty)
        {
            _logger.LogError("[Sprint26] ArabicDevSeeder: no Holding found — run DefaultHoldingBootstrap first");
            return;
        }

        // Get an admin user (created_by / updated_by) — first user in the system
        var adminUserId = await ResolveAdminUserIdAsync(cancellationToken);
        if (adminUserId == Guid.Empty)
        {
            _logger.LogWarning("[Sprint26] ArabicDevSeeder: no user found — created_by will be empty GUID");
        }

        // Run the seed
        var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken);
        using (conn)
        {
            var now = DateTime.UtcNow;
            int custUpdated = 0, custInserted = 0, vendUpdated = 0, vendInserted = 0, itemUpdated = 0, itemInserted = 0;

            // ----- Customers (13) -----
            foreach (var c in data.Customers ?? new())
            {
                var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM customers WHERE company_id = @HoldingId AND code = @Code LIMIT 1",
                    new { HoldingId = holdingId, Code = c.Code }, cancellationToken: cancellationToken));

                if (existing.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE customers SET
                            name = @Name, name_en = @NameEn, tax_id = @TaxId,
                            email = @Email, phone = @Phone,
                            credit_limit = @CreditLimit, payment_terms_days = @PaymentTermsDays,
                            updated_at = @Now, updated_by = @UpdatedBy
                        WHERE id = @Id",
                        new
                        {
                            Id = existing.Value,
                            Name = c.Name,
                            NameEn = c.NameEn,
                            TaxId = c.TaxId,
                            Email = c.Email,
                            Phone = c.Phone,
                            CreditLimit = c.CreditLimit,
                            PaymentTermsDays = c.PaymentTermsDays,
                            Now = now,
                            UpdatedBy = adminUserId,
                        }, cancellationToken: cancellationToken));
                    custUpdated++;
                }
                else
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO customers
                            (id, company_id, code, name, name_en, tax_id, email, phone,
                             credit_limit, payment_terms_days, is_active,
                             created_at, created_by, updated_at, updated_by)
                        VALUES
                            (@Id, @HoldingId, @Code, @Name, @NameEn, @TaxId, @Email, @Phone,
                             @CreditLimit, @PaymentTermsDays, true,
                             @Now, @CreatedBy, @Now, @CreatedBy)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            HoldingId = holdingId,
                            c.Code, c.Name, c.NameEn, c.TaxId, c.Email, c.Phone,
                            c.CreditLimit, c.PaymentTermsDays,
                            Now = now,
                            CreatedBy = adminUserId,
                        }, cancellationToken: cancellationToken));
                    custInserted++;
                }
            }

            // ----- Vendors (13) -----
            foreach (var v in data.Vendors ?? new())
            {
                var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM vendors WHERE company_id = @HoldingId AND code = @Code LIMIT 1",
                    new { HoldingId = holdingId, Code = v.Code }, cancellationToken: cancellationToken));

                if (existing.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE vendors SET
                            name = @Name, tax_number = @TaxId,
                            email = @Email, phone = @Phone,
                            payment_terms = @PaymentTerms,
                            updated_at = @Now, updated_by = @UpdatedBy
                        WHERE id = @Id",
                        new
                        {
                            Id = existing.Value,
                            Name = v.Name,
                            TaxId = v.TaxId,
                            Email = v.Email,
                            Phone = v.Phone,
                            PaymentTerms = $"Net{v.PaymentTermsDays}",
                            Now = now,
                            UpdatedBy = adminUserId,
                        }, cancellationToken: cancellationToken));
                    vendUpdated++;
                }
                else
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO vendors
                            (id, company_id, code, name, tax_number, email, phone,
                             currency, payment_terms, is_active,
                             created_at, created_by, updated_at, updated_by)
                        VALUES
                            (@Id, @HoldingId, @Code, @Name, @TaxId, @Email, @Phone,
                             'LYD', @PaymentTerms, true,
                             @Now, @CreatedBy, @Now, @CreatedBy)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            HoldingId = holdingId,
                            v.Code, v.Name, v.TaxId, v.Email, v.Phone,
                            PaymentTerms = $"Net{v.PaymentTermsDays}",
                            Now = now,
                            CreatedBy = adminUserId,
                        }, cancellationToken: cancellationToken));
                    vendInserted++;
                }
            }

            // ----- Items (20) -----
            // Need category + UoM to create new items. Pick the first available.
            var firstCategory = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM item_categories WHERE company_id = @HoldingId ORDER BY code LIMIT 1",
                new { HoldingId = holdingId }, cancellationToken: cancellationToken));
            var firstUom = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM units_of_measure WHERE company_id = @HoldingId ORDER BY code LIMIT 1",
                new { HoldingId = holdingId }, cancellationToken: cancellationToken));

            foreach (var it in data.Items ?? new())
            {
                var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                    "SELECT id FROM items WHERE company_id = @HoldingId AND sku = @Sku LIMIT 1",
                    new { HoldingId = holdingId, Sku = it.Sku }, cancellationToken: cancellationToken));

                if (existing.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE items SET
                            name = @Name, barcode = @Barcode,
                            average_cost = @AverageCost, standard_cost = @StandardCost,
                            reorder_level = @ReorderLevel, reorder_quantity = @ReorderQuantity,
                            updated_at = @Now, updated_by = @UpdatedBy
                        WHERE id = @Id",
                        new
                        {
                            Id = existing.Value,
                            Name = it.Name,
                            it.Barcode,
                            it.AverageCost, it.StandardCost,
                            it.ReorderLevel, it.ReorderQuantity,
                            Now = now,
                            UpdatedBy = adminUserId,
                        }, cancellationToken: cancellationToken));
                    itemUpdated++;
                }
                else if (firstCategory.HasValue && firstUom.HasValue)
                {
                    // item_type: 1=RawMaterial, 2=FinishedGood, 3=Service (default in existing seed is 1=RawMaterial, but we accept the JSON value)
                    var itemType = it.ItemType?.ToLowerInvariant() switch
                    {
                        "rawmaterial" => 1,
                        "finishedgood" => 2,
                        "service" => 3,
                        _ => 1
                    };
                    // costing_method: 1=Standard, 2=FIFO, 3=Average
                    var costingMethod = it.CostingMethod?.ToLowerInvariant() switch
                    {
                        "standard" => 1,
                        "fifo" => 2,
                        "average" => 3,
                        _ => 3
                    };

                    await conn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO items
                            (id, company_id, sku, barcode, name, description,
                             category_id, unit_of_measure_id, item_type, costing_method,
                             average_cost, standard_cost,
                             reorder_level, reorder_quantity, is_active,
                             created_at, created_by, updated_at, updated_by)
                        VALUES
                            (@Id, @HoldingId, @Sku, @Barcode, @Name, @Name,
                             @CategoryId, @UomId, @ItemType, @CostingMethod,
                             @AverageCost, @StandardCost,
                             @ReorderLevel, @ReorderQuantity, true,
                             @Now, @CreatedBy, @Now, @CreatedBy)",
                        new
                        {
                            Id = Guid.NewGuid(),
                            HoldingId = holdingId,
                            it.Sku, it.Barcode, it.Name,
                            CategoryId = firstCategory.Value,
                            UomId = firstUom.Value,
                            ItemType = itemType,
                            CostingMethod = costingMethod,
                            it.AverageCost, it.StandardCost,
                            it.ReorderLevel, it.ReorderQuantity,
                            Now = now,
                            CreatedBy = adminUserId,
                        }, cancellationToken: cancellationToken));
                    itemInserted++;
                }
                else
                {
                    _logger.LogWarning(
                        "[Sprint26] Skipping item insert: no item_categories or units_of_measure found for holding {HoldingId}",
                        holdingId);
                }
            }

            _logger.LogInformation(
                "[Sprint26] ArabicDevSeeder: completed — customers updated={CU} inserted={CI}, vendors updated={VU} inserted={VI}, items updated={IU} inserted={II}",
                custUpdated, custInserted, vendUpdated, vendInserted, itemUpdated, itemInserted);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// يبحث عن ملف JSON في عدة مواقع متوقعة — مفيد للتطوير المحلي حيث مسار
    /// الـ source قد يختلف عن مسار الـ output. يعيد أول ملف موجود.
    /// </summary>
    private string? ResolveDataFile()
    {
        var configured = _config.GetValue<string>("ArabicSeeder:DataFile");
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
            // Default locations
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Shared", "SeedData", "ArabicDevData.json"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "ArabicDevData.json"));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Shared", "SeedData", "ArabicDevData.json"));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Shared", "SeedData", "ArabicDevData.json"));
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

public sealed class ArabicDevData
{
    [JsonPropertyName("customers")]
    public List<ArabicDevCustomer>? Customers { get; set; }

    [JsonPropertyName("vendors")]
    public List<ArabicDevVendor>? Vendors { get; set; }

    [JsonPropertyName("items")]
    public List<ArabicDevItem>? Items { get; set; }
}

public sealed class ArabicDevCustomer
{
    [JsonPropertyName("code")]              public string Code { get; set; } = "";
    [JsonPropertyName("name")]              public string Name { get; set; } = "";
    [JsonPropertyName("nameEn")]            public string? NameEn { get; set; }
    [JsonPropertyName("taxId")]             public string? TaxId { get; set; }
    [JsonPropertyName("email")]             public string? Email { get; set; }
    [JsonPropertyName("phone")]             public string? Phone { get; set; }
    [JsonPropertyName("creditLimit")]       public decimal CreditLimit { get; set; }
    [JsonPropertyName("paymentTermsDays")]  public int PaymentTermsDays { get; set; }
}

public sealed class ArabicDevVendor
{
    [JsonPropertyName("code")]              public string Code { get; set; } = "";
    [JsonPropertyName("name")]              public string Name { get; set; } = "";
    [JsonPropertyName("nameEn")]            public string? NameEn { get; set; }
    [JsonPropertyName("taxId")]             public string? TaxId { get; set; }
    [JsonPropertyName("email")]             public string? Email { get; set; }
    [JsonPropertyName("phone")]             public string? Phone { get; set; }
    [JsonPropertyName("paymentTermsDays")]  public int PaymentTermsDays { get; set; }
}

public sealed class ArabicDevItem
{
    [JsonPropertyName("sku")]               public string Sku { get; set; } = "";
    [JsonPropertyName("barcode")]           public string? Barcode { get; set; }
    [JsonPropertyName("name")]              public string Name { get; set; } = "";
    [JsonPropertyName("itemType")]          public string? ItemType { get; set; }
    [JsonPropertyName("costingMethod")]     public string? CostingMethod { get; set; }
    [JsonPropertyName("averageCost")]       public decimal AverageCost { get; set; }
    [JsonPropertyName("standardCost")]      public decimal StandardCost { get; set; }
    [JsonPropertyName("reorderLevel")]      public decimal ReorderLevel { get; set; }
    [JsonPropertyName("reorderQuantity")]   public decimal ReorderQuantity { get; set; }
}
