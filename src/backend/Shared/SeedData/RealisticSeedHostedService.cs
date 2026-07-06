using System.Data;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Realistic 2-year seed for a Libyan holding company (Sprint-4.5 / DEC-064 / DEC-067).
///
/// DEC-067: Converted from IHostedService → BackgroundService so it does NOT
/// block app startup. The app starts listening on port 7860 immediately,
/// health checks pass, and seeding runs in the background.
///
/// DEC-067 fixes:
/// - Yields every 50 records (Task.Yield + small Delay) so HTTP requests don't starve
/// - Reuses single connection per seed step (avoids pool exhaustion)
/// - 5-second initial delay (let app start + health check pass)
/// - Progress logging per batch
/// - All-or-nothing per step: if a step fails, log + continue (don't crash background)
/// </summary>
public sealed class RealisticSeedHostedService : BackgroundService
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly ILogger<RealisticSeedHostedService> _logger;
    private readonly IConfiguration _config;

    private static readonly DateTime ScenarioStart = new(2024, 7, 1);
    private static readonly DateTime ScenarioEnd = new(2026, 7, 1);
    private const int TotalMonths = 24;
    private const int CompaniesCount = 5;
    private const int VendorsCount = 15;
    private const int CustomersCount = 20;
    private const int ProjectsCount = 8;
    private const int GoodsReceiptsCount = 100;
    private const int BillsCount = 100;
    private const int SalesInvoicesCount = 50;
    private const int JournalEntriesCount = 200;
    private const int InitialDelayMs = 5000;     // let app start
    private const int YieldEveryRecords = 50;     // yield after every N inserts
    private const int YieldSleepMs = 50;          // small delay for HTTP to breathe

    public RealisticSeedHostedService(
        IServiceProvider rootServiceProvider,
        ILogger<RealisticSeedHostedService> logger,
        IConfiguration config)
    {
        _rootServiceProvider = rootServiceProvider;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seedEnabled = _config.GetValue<bool?>("Database:SeedRealisticScenario") ?? false;
        if (!seedEnabled)
        {
            _logger.LogInformation("RealisticSeed: disabled (Database:SeedRealisticScenario = false)");
            return;
        }

        _logger.LogInformation("RealisticSeed: background mode — letting app start first ({Delay}ms)", InitialDelayMs);
        try
        {
            await Task.Delay(InitialDelayMs, stoppingToken);
        }
        catch (OperationCanceledException) { return; }

        _logger.LogInformation("RealisticSeed: starting 2-year scenario...");
        _logger.LogInformation("  Period: {Start:yyyy-MM} → {End:yyyy-MM}", ScenarioStart, ScenarioEnd);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _rootServiceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var factory = services.GetRequiredService<IDbConnectionFactory>();

            var tenantId = await GetOrCreateTenantAsync(factory, stoppingToken);
            if (tenantId == Guid.Empty)
            {
                _logger.LogError("RealisticSeed: failed to get/create tenant");
                return;
            }

            await StepAsync("Companies",        () => SeedCompaniesAsync(factory, tenantId, stoppingToken),        CompaniesCount,       stoppingToken);
            var vendorIds = await GetExistingIdsAsync(factory, tenantId, "vendors", stoppingToken);
            if (vendorIds.Count < VendorsCount)
            {
                vendorIds = await StepAsync("Vendors",        () => SeedVendorsAsync(factory, tenantId, stoppingToken),         VendorsCount,       stoppingToken);
            }
            var customerIds = await GetExistingIdsAsync(factory, tenantId, "customers", stoppingToken);
            if (customerIds.Count < CustomersCount)
            {
                customerIds = await StepAsync("Customers",     () => SeedCustomersAsync(factory, tenantId, stoppingToken),      CustomersCount,      stoppingToken);
            }
            await StepAsync("Projects",          () => SeedProjectsAsync(factory, tenantId, stoppingToken),        ProjectsCount,        stoppingToken);
            var itemIds = await GetExistingIdsAsync(factory, tenantId, "items", stoppingToken);
            if (itemIds.Count < 10)
            {
                itemIds = await StepAsync("Items",        () => SeedItemsAsync(factory, tenantId, stoppingToken),          10,                  stoppingToken);
            }
            await StepAsync("GoodsReceipts",     () => SeedGoodsReceiptsAsync(factory, tenantId, vendorIds, itemIds, stoppingToken), GoodsReceiptsCount,  stoppingToken);
            await StepAsync("Bills",             () => SeedBillsAsync(factory, tenantId, vendorIds, itemIds, stoppingToken),             BillsCount,         stoppingToken);
            await StepAsync("SalesInvoices",     () => SeedSalesInvoicesAsync(factory, tenantId, customerIds, itemIds, stoppingToken), SalesInvoicesCount, stoppingToken);
            await StepAsync("JournalEntries",    () => SeedJournalEntriesAsync(factory, tenantId, stoppingToken),          JournalEntriesCount, stoppingToken);

            sw.Stop();
            _logger.LogInformation("========================================");
            _logger.LogInformation("RealisticSeed: DONE in {Sec:F1}s", sw.Elapsed.TotalSeconds);
            _logger.LogInformation("  TenantId: {TenantId}", tenantId);
            _logger.LogInformation("========================================");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RealisticSeed: cancelled (app shutting down)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RealisticSeed: background task failed");
        }
    }

    /// <summary>
    /// Wraps a seed step with: progress log + exception isolation.
    /// Returns whatever the step returned (or empty list for void steps).
    /// Yields to the thread pool between steps so HTTP requests can be served.
    /// </summary>
    private async Task<List<Guid>> StepAsync(
        string stepName,
        Func<Task<List<Guid>>> stepFn,
        int expectedCount,
        CancellationToken ct)
    {
        _logger.LogInformation("[RealisticSeed] → {Step} (target: {Count} records)", stepName, expectedCount);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await Task.Yield(); // let HTTP requests breathe
            var result = await stepFn();
            sw.Stop();
            _logger.LogInformation("[RealisticSeed] ✓ {Step} done in {Sec:F1}s ({Count} records)",
                stepName, sw.Elapsed.TotalSeconds, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[RealisticSeed] ✗ {Step} failed after {Sec:F1}s — continuing",
                stepName, sw.Elapsed.TotalSeconds);
            return new List<Guid>();
        }
    }

    private async Task<List<Guid>> GetExistingIdsAsync(
        IDbConnectionFactory factory, Guid tenantId, string table, CancellationToken ct)
    {
        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var sql = $"SELECT id FROM {table} WHERE tenant_id = @T";
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { T = tenantId }, cancellationToken: ct));
        return ids.ToList();
    }

    // ==================== Tenant ====================

    private async Task<Guid> GetOrCreateTenantAsync(IDbConnectionFactory factory, CancellationToken ct)
    {
        using var conn = await factory.CreateOltpConnectionAsync(ct);
        const string findSql = "SELECT id FROM tenants WHERE subdomain = @sub LIMIT 1";
        var existing = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            findSql, new { sub = "alfajr" }, cancellationToken: ct));
        if (existing.HasValue)
        {
            _logger.LogInformation("[RealisticSeed] Using existing tenant: {TenantId}", existing.Value);
            return existing.Value;
        }
        var newId = Guid.NewGuid();
        const string insertSql = @"
            INSERT INTO tenants (id, name, subdomain, is_active, created_at)
            VALUES (@Id, 'AlFajr Holding', 'alfajr', true, @Now)";
        await conn.ExecuteAsync(new CommandDefinition(insertSql, new
        {
            Id = newId,
            Now = DateTime.UtcNow
        }, cancellationToken: ct));
        return newId;
    }

    // ==================== Companies (5) ====================

    private async Task<List<Guid>> SeedCompaniesAsync(
        IDbConnectionFactory factory, Guid tenantId, CancellationToken ct)
    {
        var companies = new (string code, string name)[]
        {
            ("ALF", "AlFajr Trading & Contracting"),
            ("ALB", "AlBurj Building Materials"),
            ("ALN", "AlNoor Office Supplies"),
            ("ALK", "AlKawn Food Services"),
            ("ALKH", "AlNakhla Tourism & Cleaning")
        };

        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM companies WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= CompaniesCount) return await GetExistingIdsAsync(factory, tenantId, "companies", ct);

        var companyIds = new List<Guid>();
        for (int i = 0; i < companies.Length; i++)
        {
            var id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO companies (id, tenant_id, code, name, currency, is_active, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @Code, @Name, 'LYD', true, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = id,
                T = tenantId,
                Code = companies[i].code,
                Name = companies[i].name,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            companyIds.Add(id);

            if ((i + 1) % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return companyIds;
    }

    // ==================== Vendors (15) ====================

    private async Task<List<Guid>> SeedVendorsAsync(
        IDbConnectionFactory factory, Guid tenantId, CancellationToken ct)
    {
        var sectors = new[] { "مواد بناء", "مكاتب", "خدمات", "نقل", "صيانة", "كهرباء", "سباكة", "دهان", "تغذية", "تنظيف" };
        var firstNames = new[] { "عبدالله", "محمد", "سالم", "فاطمة", "ليلى", "أحمد", "سعد", "نورة", "خالد", "منى" };

        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM vendors WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= VendorsCount) return await GetExistingIdsAsync(factory, tenantId, "vendors", ct);

        var vendorIds = new List<Guid>();
        for (int i = 1; i <= VendorsCount; i++)
        {
            var id = Guid.NewGuid();
            var code = $"V-{i:D3}";
            var name = $"Vendor {i} ({sectors[i % sectors.Length]})";
            var contact = $"{firstNames[i % firstNames.Length]} المبيعات";
            var email = $"vendor{i}@example.ly";
            var phone = $"+21891{i:D7}";

            const string sql = @"
                INSERT INTO vendors (id, tenant_id, code, name, contact_name, email, phone, balance, currency, is_active, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @Code, @Name, @Contact, @Email, @Phone, @Balance, 'LYD', true, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = id,
                T = tenantId,
                Code = code,
                Name = name,
                Contact = contact,
                Email = email,
                Phone = phone,
                Balance = 5000m + (i * 1000m),
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            vendorIds.Add(id);

            if (i % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return vendorIds;
    }

    // ==================== Customers (20) ====================

    private async Task<List<Guid>> SeedCustomersAsync(
        IDbConnectionFactory factory, Guid tenantId, CancellationToken ct)
    {
        var customerTypes = new[] { "Government", "Private", "Mixed" };
        var orgs = new[] { "وزارة الإسكان", "شركة الإنماء", "مؤسسة النفط", "بلدية طرابلس", "هيئة الطرق", "مصرف ليبيا", "شركة البريقة", "مجمع الفاتح", "فندق كورنثيا", "مستشفى طرابلس المركزي" };

        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM customers WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= CustomersCount) return await GetExistingIdsAsync(factory, tenantId, "customers", ct);

        var customerIds = new List<Guid>();
        for (int i = 1; i <= CustomersCount; i++)
        {
            var id = Guid.NewGuid();
            var code = $"C-{i:D3}";
            var name = i <= 10 ? orgs[i - 1] : $"Customer {i} (Private)";
            var type = customerTypes[i % customerTypes.Length];

            const string sql = @"
                INSERT INTO customers (id, tenant_id, code, name, type, balance, currency, is_active, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @Code, @Name, @Type, @Balance, 'LYD', true, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = id,
                T = tenantId,
                Code = code,
                Name = name,
                Type = type,
                Balance = 3000m + (i * 750m),
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            customerIds.Add(id);

            if (i % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return customerIds;
    }

    // ==================== Projects (8) ====================

    private async Task<List<Guid>> SeedProjectsAsync(
        IDbConnectionFactory factory, Guid tenantId, CancellationToken ct)
    {
        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM projects WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= ProjectsCount) return await GetExistingIdsAsync(factory, tenantId, "projects", ct);

        var statuses = new[] { 0, 0, 1, 1, 2, 2, 3, 3 };
        var projectNames = new[] {
            "مشروع طريق المطار", "تطوير مجمع السكني", "صيانة المدارس",
            "بناء مستشفى الأطفال", "تحديث البنية التحتية للمياه",
            "مشروع الإسكان الاجتماعي", "مجمع تجاري الشط", "صيانة الطرق السريعة"
        };

        var projectIds = new List<Guid>();
        var rng = new Random(42);
        for (int i = 0; i < ProjectsCount; i++)
        {
            var id = Guid.NewGuid();
            var code = $"P-{2024 + (i / 4)}-{i + 1:D3}";
            var startOffset = rng.Next(0, TotalMonths - 6);
            var startDate = ScenarioStart.AddMonths(startOffset);
            var endDate = startDate.AddMonths(rng.Next(3, 12));
            if (endDate > ScenarioEnd) endDate = ScenarioEnd.AddDays(-rng.Next(1, 30));
            var budget = 50_000m + (i * 25_000m);

            const string sql = @"
                INSERT INTO projects (id, tenant_id, company_id, cost_center_id, code, name, description, status, budget, start_date, end_date, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @CompanyId, @CCId, @Code, @Name, @Desc, @Status, @Budget, @Start, @End, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = id,
                T = tenantId,
                CompanyId = Guid.NewGuid(),
                CCId = Guid.NewGuid(),
                Code = code,
                Name = projectNames[i],
                Desc = $"مشروع {projectNames[i]} (نشط)",
                Status = statuses[i],
                Budget = budget,
                Start = startDate,
                End = endDate,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            projectIds.Add(id);

            if ((i + 1) % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return projectIds;
    }

    // ==================== Items ====================

    private async Task<List<Guid>> SeedItemsAsync(
        IDbConnectionFactory factory, Guid tenantId, CancellationToken ct)
    {
        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM items WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= 10) return await GetExistingIdsAsync(factory, tenantId, "items", ct);

        var itemNames = new[] { "إسمنت", "حديد", "رمل", "حصى", "بلاط", "دهان", "أجهزة مكتبية", "قرطاسية", "معدات نظافة", "مواد غذائية" };
        var itemIds = new List<Guid>();
        for (int i = 0; i < itemNames.Length; i++)
        {
            var id = Guid.NewGuid();
            var sku = $"SKU-{i + 1:D4}";
            const string sql = @"
                INSERT INTO items (id, tenant_id, company_id, sku, name, item_type, costing_method, unit_of_measure_id, is_active, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @CompanyId, @Sku, @Name, 'Stock', 'Average', @UoM, true, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = id,
                T = tenantId,
                CompanyId = Guid.NewGuid(),
                Sku = sku,
                Name = itemNames[i],
                UoM = Guid.NewGuid(),
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            itemIds.Add(id);

            if ((i + 1) % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return itemIds;
    }

    // ==================== GRs (100) ====================

    private async Task<List<Guid>> SeedGoodsReceiptsAsync(
        IDbConnectionFactory factory, Guid tenantId, List<Guid> vendorIds, List<Guid> itemIds, CancellationToken ct)
    {
        if (vendorIds.Count == 0 || itemIds.Count == 0) return new List<Guid>();

        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM goods_receipts WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= GoodsReceiptsCount) return new List<Guid>();

        var rng = new Random(123);
        var grIds = new List<Guid>();
        for (int i = 1; i <= GoodsReceiptsCount; i++)
        {
            var id = Guid.NewGuid();
            var monthOffset = rng.Next(0, TotalMonths);
            var day = rng.Next(1, 28);
            var date = ScenarioStart.AddMonths(monthOffset).AddDays(day);
            if (date > DateTime.UtcNow) date = DateTime.UtcNow.AddDays(-rng.Next(1, 30));

            var vendor = vendorIds[rng.Next(vendorIds.Count)];
            const string sql = @"
                INSERT INTO goods_receipts (id, tenant_id, gr_number, vendor_id, status, receipt_date, total_amount, currency, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @GrNumber, @Vendor, 2, @Date, @Amount, 'LYD', @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = id,
                T = tenantId,
                GrNumber = $"GR-{i:D5}",
                Vendor = vendor,
                Date = date,
                Amount = 1000m + (i * 200m) + (rng.Next(0, 1000) * 1m),
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            grIds.Add(id);

            if (i % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return grIds;
    }

    // ==================== Bills (100, with line items) ====================

    private async Task<List<Guid>> SeedBillsAsync(
        IDbConnectionFactory factory, Guid tenantId, List<Guid> vendorIds, List<Guid> itemIds, CancellationToken ct)
    {
        if (vendorIds.Count == 0 || itemIds.Count == 0) return new List<Guid>();

        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM vendor_bills WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= BillsCount) return new List<Guid>();

        var rng = new Random(456);
        var billIds = new List<Guid>();
        for (int i = 1; i <= BillsCount; i++)
        {
            var id = Guid.NewGuid();
            var monthOffset = rng.Next(0, TotalMonths);
            var day = rng.Next(1, 28);
            var date = ScenarioStart.AddMonths(monthOffset).AddDays(day);
            if (date > DateTime.UtcNow) date = DateTime.UtcNow.AddDays(-rng.Next(1, 30));

            var vendor = vendorIds[rng.Next(vendorIds.Count)];
            var item = itemIds[rng.Next(itemIds.Count)];
            var qty = 1m + (rng.Next(1, 10) * 1m);
            var unitCost = 50m + (rng.Next(0, 500) * 1m);
            var lineTotal = qty * unitCost;

            const string insertBill = @"
                INSERT INTO vendor_bills (id, tenant_id, bill_number, vendor_id, status, bill_date, total_amount, currency, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @BillNumber, @Vendor, 2, @Date, @Total, 'LYD', @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(insertBill, new
            {
                Id = id,
                T = tenantId,
                BillNumber = $"B-{i:D5}",
                Vendor = vendor,
                Date = date,
                Total = lineTotal,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));

            var lineId = Guid.NewGuid();
            const string insertLine = @"
                INSERT INTO vendor_bill_lines (id, tenant_id, bill_id, item_id, quantity, unit_cost, line_total, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @Bill, @Item, @Qty, @UnitCost, @LineTotal, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(insertLine, new
            {
                Id = lineId,
                T = tenantId,
                Bill = id,
                Item = item,
                Qty = qty,
                UnitCost = unitCost,
                LineTotal = lineTotal,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            billIds.Add(id);

            if (i % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return billIds;
    }

    // ==================== Sales Invoices (50) ====================

    private async Task<List<Guid>> SeedSalesInvoicesAsync(
        IDbConnectionFactory factory, Guid tenantId, List<Guid> customerIds, List<Guid> itemIds, CancellationToken ct)
    {
        if (customerIds.Count == 0 || itemIds.Count == 0) return new List<Guid>();

        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM sales_invoices WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= SalesInvoicesCount) return new List<Guid>();

        var rng = new Random(789);
        var invIds = new List<Guid>();
        for (int i = 1; i <= SalesInvoicesCount; i++)
        {
            var id = Guid.NewGuid();
            var monthOffset = rng.Next(0, TotalMonths);
            var day = rng.Next(1, 28);
            var date = ScenarioStart.AddMonths(monthOffset).AddDays(day);
            if (date > DateTime.UtcNow) date = DateTime.UtcNow.AddDays(-rng.Next(1, 30));

            var customer = customerIds[rng.Next(customerIds.Count)];
            var item = itemIds[rng.Next(itemIds.Count)];
            var amount = 2000m + (rng.Next(0, 5000) * 1m);

            const string insertInvoice = @"
                INSERT INTO sales_invoices (id, tenant_id, invoice_number, customer_id, status, invoice_date, total_amount, currency, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @InvoiceNumber, @Customer, 2, @Date, @Amount, 'LYD', @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(insertInvoice, new
            {
                Id = id,
                T = tenantId,
                InvoiceNumber = $"INV-{i:D5}",
                Customer = customer,
                Date = date,
                Amount = amount,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));

            var lineId = Guid.NewGuid();
            const string insertLine = @"
                INSERT INTO sales_invoice_lines (id, tenant_id, invoice_id, item_id, quantity, unit_price, line_total, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @Invoice, @Item, @Qty, @UnitPrice, @LineTotal, @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(insertLine, new
            {
                Id = lineId,
                T = tenantId,
                Invoice = id,
                Item = item,
                Qty = 1m,
                UnitPrice = amount,
                LineTotal = amount,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            invIds.Add(id);

            if (i % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return invIds;
    }

    // ==================== Journal Entries (200+, balanced) ====================

    private async Task<List<Guid>> SeedJournalEntriesAsync(
        IDbConnectionFactory factory, Guid tenantId, CancellationToken ct)
    {
        using var conn = await factory.CreateOltpConnectionAsync(ct);
        var existing = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM journal_entries WHERE tenant_id = @T", new { T = tenantId }, cancellationToken: ct));
        if (existing >= JournalEntriesCount) return new List<Guid>();

        var rng = new Random(2024);
        var jeIds = new List<Guid>();
        for (int i = 1; i <= JournalEntriesCount; i++)
        {
            var id = Guid.NewGuid();
            var monthOffset = rng.Next(0, TotalMonths);
            var day = rng.Next(1, 28);
            var date = ScenarioStart.AddMonths(monthOffset).AddDays(day);
            if (date > DateTime.UtcNow) date = DateTime.UtcNow.AddDays(-rng.Next(1, 30));

            var amount = 1000m + (rng.Next(0, 9000) * 1m);
            var reference = $"JV-{i:D5}";

            const string insertJE = @"
                INSERT INTO journal_entries (id, tenant_id, entry_number, reference, entry_date, status, total_debit, total_credit, currency, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @EntryNumber, @Reference, @Date, 2, @Amount, @Amount, 'LYD', @Now, @Now, @User, @User)";
            await conn.ExecuteAsync(new CommandDefinition(insertJE, new
            {
                Id = id,
                T = tenantId,
                EntryNumber = reference,
                Reference = $"Auto-generated JV {i}",
                Date = date,
                Amount = amount,
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));

            const string insertLine = @"
                INSERT INTO journal_entry_lines (id, tenant_id, journal_entry_id, account_id, type, amount, description, created_at, updated_at, created_by, updated_by)
                VALUES (@Id, @T, @JE, @Account, @Type, @Amount, @Desc, @Now, @Now, @User, @User)";
            // Debit
            await conn.ExecuteAsync(new CommandDefinition(insertLine, new
            {
                Id = Guid.NewGuid(),
                T = tenantId,
                JE = id,
                Account = Guid.NewGuid(),
                Type = "Debit",
                Amount = amount,
                Desc = $"Debit for {reference}",
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            // Credit
            await conn.ExecuteAsync(new CommandDefinition(insertLine, new
            {
                Id = Guid.NewGuid(),
                T = tenantId,
                JE = id,
                Account = Guid.NewGuid(),
                Type = "Credit",
                Amount = amount,
                Desc = $"Credit for {reference}",
                Now = DateTime.UtcNow,
                User = Guid.Empty
            }, cancellationToken: ct));
            jeIds.Add(id);

            if (i % YieldEveryRecords == 0) await YieldAsync(ct);
        }
        return jeIds;
    }

    /// <summary>
    /// Yields to thread pool + small delay so HTTP requests can be served.
    /// Prevents the seed from starving the Kestrel listener.
    /// </summary>
    private async Task YieldAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(YieldSleepMs, ct);
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
    }
}