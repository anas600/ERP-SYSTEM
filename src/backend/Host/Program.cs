// ERP-SYSTEM Backend Entry Point
// Phase 0: Foundation + Identity Module
// Phase 1: Finance Core
// Phase 1.5: Multi-Company Foundation
// Phase 2.1: Projects Module

using System.Text;
using System.Text.Json.Serialization;
using Dapper;
using ERPSystem.Host.Bootstrap;
using ERPSystem.Host.Middleware;
using ERPSystem.Shared.Audit;
using ERPSystem.Shared.DataTypes;
using ERPSystem.Modules.Companies.Application.Services;
using ERPSystem.Modules.Companies.Infrastructure;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Identity.Application.Auth;
using ERPSystem.Modules.Identity.Infrastructure;
using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Modules.Inventory.Application;
using ERPSystem.Modules.Inventory.Application.Services;
using ERPSystem.Modules.Inventory.Infrastructure;
using ERPSystem.Modules.Procurement.Application;
using ERPSystem.Modules.Procurement.Application.Services;
using ERPSystem.Modules.Procurement.Infrastructure;
using ERPSystem.Modules.HR.Application;
using ERPSystem.Modules.HR.Application.Services;
using ERPSystem.Modules.HR.Infrastructure;
using ERPSystem.Modules.Payroll.Application;
using ERPSystem.Modules.Payroll.Application.Services;
using ERPSystem.Modules.Payroll.Domain.Calculators;
using ERPSystem.Modules.Payroll.Infrastructure;
using ERPSystem.Modules.AccountsReceivable.Application;
using ERPSystem.Modules.AccountsReceivable.Application.Services;
using ERPSystem.Modules.AccountsReceivable.Infrastructure;
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Modules.Notifications.Application.Services;
using ERPSystem.Modules.Notifications.Infrastructure;
using ERPSystem.Modules.Dashboard.Application.Services;
using ERPSystem.Modules.Search.Application.Services;
using ERPSystem.Modules.Finance.Application.EventHandlers;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.Events;
using ERPSystem.Shared.Events.Application.Services;
using ERPSystem.Shared.Events.Infrastructure;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.Migrations;
using ERPSystem.Shared.SeedData;
using ERPSystem.Shared.CompanyContext;
using FluentMigrator.Runner;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ============ Error tracking (Sentry — optional) ============
// Sprint-4 Day 3 (DEC-045). Disabled unless Sentry__Dsn env var is set.
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? builder.Configuration["Sentry__Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = sentryDsn;
        o.Environment = builder.Environment.EnvironmentName;
        o.Release = "erp-system@1.0.0";
        o.TracesSampleRate = 0.2; // 20% of transactions (HF free tier)
        o.SendDefaultPii = false; // GDPR-safe default
        o.AttachStacktrace = true;
    });
    Console.WriteLine($"[SENTRY] Error tracking enabled (env={builder.Environment.EnvironmentName})");
}
else
{
    Console.WriteLine("[SENTRY] Error tracking disabled (no Sentry__Dsn env var)");
}

// ============ Logging ============
// Sprint-4 Day 3 (DEC-045): structured JSON logging.
// In Development: human-readable output. In Production: JSON for log aggregation.
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .Enrich.WithThreadId()
      .Enrich.WithProperty("Application", "ERP-SYSTEM")
      .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName);

    if (ctx.HostingEnvironment.IsDevelopment())
    {
        lc.WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
    }
    else
    {
        // Production: Compact JSON for log shippers (Loki, Elasticsearch, CloudWatch, etc.)
        lc.WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());
    }

    // DEC-111: Sentry DSN logged (sink not added due to package API mismatch).
    // Future: Sentry.AspNetCore SDK integration via UseSentry() in pipeline.
    var sentryDsn = ctx.Configuration["Sentry:Dsn"];
    if (!string.IsNullOrWhiteSpace(sentryDsn))
    {
        lc.Enrich.WithProperty("SentryDsn", sentryDsn);
    }
});

// ============ Configuration ============
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings غير معرّف.");
builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtSettings));

builder.Services.Configure<NpgsqlConnectionOptions>(opts =>
{
    opts.OltpConnectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres غير معرّف.");
    opts.EventStoreConnectionString = builder.Configuration.GetSection("Marten")["ConnectionString"];
    // Phase 6.3 hotfix (PR #149): direct connection (port 5432) للـ migrations.
    // اختياري — لو مش معرّف، الـ migrators يستخدمون الـ OLTP ephemeral (مع تحذير).
    opts.MigrationsConnectionString = builder.Configuration.GetConnectionString("Migrations");

    // Resiliency baseline (DEC-093, 2026-07-24): values من appsettings.json،
    // الـ defaults في NpgsqlConnectionFactory تأخذ الأولوية لو الـ keys ناقصة.
    var db = builder.Configuration.GetSection("Database");
    opts.CommandTimeoutSeconds = db.GetValue<int?>("CommandTimeoutSeconds") ?? opts.CommandTimeoutSeconds;
    opts.ConnectionTimeoutSeconds = db.GetValue<int?>("ConnectionTimeoutSeconds") ?? opts.ConnectionTimeoutSeconds;
    opts.MaxPoolSize = db.GetValue<int?>("MaxPoolSize") ?? opts.MaxPoolSize;
    opts.MinPoolSize = db.GetValue<int?>("MinPoolSize") ?? opts.MinPoolSize;
    opts.KeepaliveSeconds = db.GetValue<int?>("KeepaliveSeconds") ?? opts.KeepaliveSeconds;
    opts.ConnectionIdleLifetimeSeconds = db.GetValue<int?>("ConnectionIdleLifetimeSeconds") ?? opts.ConnectionIdleLifetimeSeconds;
});

// ============================================
// TODO: Enable Marten in Sprint-5 (DEC-017)
// ============================================
// Marten package is installed and configured but NOT yet wired up.
// Event store using PostgreSQL LISTEN/NOTIFY planned for Sprint-5+.
//
// When enabling:
//   1. Uncomment below
//   2. Add IDocumentSession to OutboxEventPublisher
//   3. Create projections for materialized views
//
// Why deferred: Event sourcing adds complexity. We need feature flag
// infrastructure first (Sprint-4) before activating Marten.
//
// Reference: DEC-017 (2026-07-05)
// ============================================
// builder.Services.AddMarten(opts =>
//     opts.Connection(builder.Configuration["Marten:ConnectionString"]!));

// ============ Infrastructure ============
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
// DEC-053: HttpContextAccessor (for audit IP/user extraction) + AuditLogger
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ERPSystem.Host.Audit.IAuditLogger, ERPSystem.Host.Audit.AuditLogger>();
// DEC-073: ActivityLogger (user-action log: login, refresh, logout, register)
builder.Services.AddScoped<ERPSystem.Modules.Activity.Application.IActivityLogger, ERPSystem.Modules.Activity.Application.ActivityLogService>();
// Sprint 3 (T1 / Block A): Activity feed reader (GET /api/activity/recent).
builder.Services.AddScoped<ERPSystem.Modules.Activity.Application.IActivityFeedService, ERPSystem.Modules.Activity.Application.ActivityFeedService>();
// Dapper TypeHandlers: تخزين الـ enums كـ string في DB + قراءة صحيحة
SqlMapper.AddTypeHandler(new EnumStringTypeHandler<ERPSystem.Modules.HR.Entities.LeaveStatus>());
SqlMapper.AddTypeHandler(new EnumStringTypeHandler<ERPSystem.Modules.Procurement.Entities.PurchaseOrderStatus>());
SqlMapper.AddTypeHandler(new EnumStringTypeHandler<ERPSystem.Modules.Procurement.Entities.GoodsReceiptStatus>());
SqlMapper.AddTypeHandler(new EnumStringTypeHandler<ERPSystem.Modules.Procurement.Entities.VendorBillStatus>());
SqlMapper.AddTypeHandler(new EnumStringTypeHandler<ERPSystem.Modules.Payroll.Domain.Entities.PayrollRunStatus>());
SqlMapper.AddTypeHandler(new EnumStringTypeHandler<ERPSystem.Modules.Payroll.Domain.Entities.PayrollItemStatus>());
// DEC-107 / DL 82: Response caching
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IUserRepository, UserRepository>();

// Phase 6.2: User CRUD + 20 mandatory reports
builder.Services.AddScoped<IJournalEntryReportService, JournalEntryReportService>();
builder.Services.AddScoped<IFinanceReportService, FinanceReportService>();
builder.Services.AddScoped<IGeneralLedgerReportService, GeneralLedgerReportService>();
builder.Services.AddScoped<IBalanceSheetService, BalanceSheetService>();
builder.Services.AddScoped<ICashFlowService, CashFlowService>();
builder.Services.AddScoped<IAPAgingService, APAgingService>();
builder.Services.AddScoped<IAccountActivityService, AccountActivityService>();
builder.Services.AddScoped<ICollectionsService, CollectionsService>();
builder.Services.AddScoped<ICostCenterReportService, CostCenterReportService>();
builder.Services.AddScoped<IVatReportService, VatReportService>();
builder.Services.AddScoped<ISalesByCustomerService, SalesByCustomerService>();
builder.Services.AddScoped<ISalesByItemService, SalesByItemService>();
builder.Services.AddScoped<ITopCustomersService, TopCustomersService>();
builder.Services.AddScoped<IPurchasesByVendorService, PurchasesByVendorService>();
builder.Services.AddScoped<ITopVendorsService, TopVendorsService>();
builder.Services.AddScoped<IBudgetVsActualService, BudgetVsActualService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
// Phase 6.1c: ITenantRepository removed — multi-company model has no tenants.
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICostCenterRepository, CostCenterRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<IProjectBudgetRepository, ProjectBudgetRepository>();
builder.Services.AddScoped<IResourceAssignmentRepository, ResourceAssignmentRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
builder.Services.AddScoped<IItemCategoryRepository, ItemCategoryRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<IStockLevelRepository, StockLevelRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
builder.Services.AddScoped<IVendorBillRepository, VendorBillRepository>();
builder.Services.AddScoped<IDocumentSequenceRepository, DocumentSequenceRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
builder.Services.AddScoped<IHRDocumentSequenceRepository, HRDocumentSequenceRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<ISalaryStructureRepository, SalaryStructureRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
builder.Services.AddScoped<IPostingRuleRepository, PostingRuleRepository>();
// AR module (Phase 5 Sprint 1 — Finance AR)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IArDocumentSequenceRepository, ArDocumentSequenceRepository>();
builder.Services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();
builder.Services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

// ============ Multi-tenancy (Phase 6.1b) ===========
// ICompanyContext is the active abstraction.
builder.Services.AddScoped<ICompanyContext, CompanyContext>();

// ============ Audit (Sprint-4.5 / DEC-056) ============
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

// ============ Domain Events (Sprint-4.5 T-010 / DEC-057) ============
// In-process Pub/Sub — lightweight cross-module integration without outbox overhead.
builder.Services.AddSingleton<IDomainEventPublisher, DomainEventPublisher>();

// ============ Services ============
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CompanyService>();
// Phase 6.1c: ITenantBootstrap removed — multi-company model. Holding is auto-seeded at startup.
builder.Services.AddScoped<ICompanyService>(sp => sp.GetRequiredService<CompanyService>());
builder.Services.AddScoped<ICostCenterService, CostCenterService>();
builder.Services.AddScoped<IChartOfAccountsService, ChartOfAccountsService>();
builder.Services.AddScoped<IJournalEntryService, JournalEntryService>();
builder.Services.AddScoped<IGeneralLedgerService, GeneralLedgerService>();
builder.Services.AddScoped<IPostingRulesService, PostingRulesService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IResourceAssignmentService, ResourceAssignmentService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
builder.Services.AddScoped<IItemCategoryService, ItemCategoryService>();
builder.Services.AddScoped<IInventoryBootstrapper, InventoryBootstrapper>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IStockLevelService, StockLevelService>();
builder.Services.AddScoped<IStockReservationService, StockReservationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// Sprint 1 (T1 / Block A): dashboard summary KPIs (4-count payload).
builder.Services.AddScoped<IDashboardSummaryService, DashboardSummaryService>();
// Sprint 5 (T1-T3 / Phase 4): dashboard chart data (revenue/expenses/top-customers).
builder.Services.AddScoped<IDashboardChartService, DashboardChartService>();
// Sprint 5 (T4 / Phase 5): global search across customers/vendors/invoices/accounts.
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();

// DEC-100 / DL 69: Register Payments services (was missing → 500 on /api/payments)
builder.Services.AddScoped<ERPSystem.Modules.Payments.Application.Services.IPaymentService, ERPSystem.Modules.Payments.Application.Services.PaymentService>();
builder.Services.AddScoped<ERPSystem.Modules.Payments.Infrastructure.IPaymentRepository, ERPSystem.Modules.Payments.Infrastructure.PaymentRepository>();
builder.Services.AddScoped<ERPSystem.Modules.Payments.Infrastructure.IPaymentSequenceRepository, ERPSystem.Modules.Payments.Infrastructure.PaymentSequenceRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<ERPSystem.Modules.Payments.Application.CreatePaymentRequestValidator>();
builder.Services.AddScoped<IVendorBillService, VendorBillService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
// AR module (Phase 5 Sprint 1 — Finance AR)
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IEosService, EosService>();
builder.Services.AddScoped<ILibyaTaxCalculator, LibyaTaxCalculator>();
builder.Services.AddScoped<IEosCalculator, EosCalculator>();
builder.Services.AddScoped<ISocialInsuranceCalculator, SocialInsuranceCalculator>();
builder.Services.AddScoped<IProjectReportService, ProjectReportService>();
builder.Services.AddScoped<IInventoryReportService, InventoryReportService>();
builder.Services.AddScoped<IFinanceReportService, FinanceReportService>();
builder.Services.AddScoped<IEventBus, EventBus>();
builder.Services.AddScoped<IIntegrationEventHandler<StockReceivedEvent>, StockReceivedEventHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<StockIssuedEvent>, StockIssuedEventHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<StockTransferredEvent>, StockTransferredEventHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<StockAdjustedEvent>, StockAdjustedEventHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateItemRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ReceiveStockRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVendorRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePurchaseOrderRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateGoodsReceiptRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateVendorBillRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateDepartmentRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEmployeeRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CheckInOutRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateLeaveRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePayrollRunRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateSalesInvoiceRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateReceiptRequestValidator>();

// ============ Redis ============
// Redis اختياري في dev. لو connection string فاضي، ما نسجّل IConnectionMultiplexer
// (HealthController يطلبه اختيارياً: `IConnectionMultiplexer?`).
// لو connection string موجود لكن Redis مش شغّال، نستخدم `AbortOnConnectFail=false`
// عشان الـ multiplexer يستمر في إعادة المحاولة بدل رمي exception عند أول request.
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var configOptions = ConfigurationOptions.Parse(redisConn);
        configOptions.AbortOnConnectFail = false;  // لا تفشل عند أول connect — استمر في إعادة المحاولة
        configOptions.ConnectRetry = 3;
        configOptions.ConnectTimeout = 1000;       // timeout قصير (1s) عشان ما نطوّل startup
        configOptions.SyncTimeout = 500;           // PingAsync / sync ops ترجع بسرعة
        configOptions.AsyncTimeout = 500;          // async ops ترجع بسرعة
        return ConnectionMultiplexer.Connect(configOptions);
    });
}

// ============ FluentMigrator ============
// DEC-096: URL-decode the Postgres connection string if it contains URL-encoded chars.
// Npgsql 8.0.5 does NOT URL-decode the Password in connection strings, so when
// appsettings.json (or .Development.json) has Password=QZYn8S%26%2Fif%21%23i%26e
// the literal URL-encoded string is sent to Postgres, which fails with 28P01
// "password authentication failed for user postgres".
// On Linux/HF, env vars usually have the raw password so the bug doesn't manifest.
// We fix it centrally here so both the migration runner AND NpgsqlConnectionFactory
// get a usable connection string.
var postgresConn = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrEmpty(postgresConn) && postgresConn.Contains("Password=") && postgresConn.Contains("%"))
{
    try
    {
        var csb = new Npgsql.NpgsqlConnectionStringBuilder(postgresConn);
        if (!string.IsNullOrEmpty(csb.Password) && csb.Password.Contains('%'))
        {
            var decoded = System.Web.HttpUtility.UrlDecode(csb.Password);
            if (!string.IsNullOrEmpty(decoded) && decoded != csb.Password)
            {
                csb.Password = decoded;
                postgresConn = csb.ConnectionString;
                // Also update Configuration so downstream services (NpgsqlConnectionFactory) see the decoded version
                builder.Configuration["ConnectionStrings:Postgres"] = postgresConn;
            }
        }
    }
    catch (Exception ex)
    {
        // Best effort: log but don't fail startup
        Console.WriteLine($"[Program.cs] Warning: failed to URL-decode Postgres connection string: {ex.Message}");
    }
}

// Phase 6.3 hotfix (PR #149): URL-decode the Migrations connection string the same
// way (Npgsql 8.0.5 quirk). Use it for FluentMigrator if available — bypasses
// Supavisor/pgbouncer transaction mode (the root cause of PR #149's 6 DDL errors).
var migrationsConn = builder.Configuration.GetConnectionString("Migrations");
if (!string.IsNullOrEmpty(migrationsConn) && migrationsConn.Contains("Password=") && migrationsConn.Contains("%"))
{
    try
    {
        var mcsb = new Npgsql.NpgsqlConnectionStringBuilder(migrationsConn);
        if (!string.IsNullOrEmpty(mcsb.Password) && mcsb.Password.Contains('%'))
        {
            var decoded = System.Web.HttpUtility.UrlDecode(mcsb.Password);
            if (!string.IsNullOrEmpty(decoded) && decoded != mcsb.Password)
            {
                mcsb.Password = decoded;
                migrationsConn = mcsb.ConnectionString;
                builder.Configuration["ConnectionStrings:Migrations"] = migrationsConn;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Program.cs] Warning: failed to URL-decode Migrations connection string: {ex.Message}");
    }
}
var migrationsRunner = !string.IsNullOrWhiteSpace(migrationsConn) ? migrationsConn : postgresConn;
Console.WriteLine($"[Migrations] FluentMigrator using: {(string.IsNullOrWhiteSpace(migrationsConn) ? "pgbouncer (port 6543) — fallback" : "direct connection (port 5432)")}");

builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(migrationsRunner)
        .ScanIn(typeof(Phase6_InitialSchema).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddSerilog());
// Phase 6.0 order (P6-0b) — الترتيب حرج: Phase 6 migration يحذف كل الجداول القديمة
// (مع tenant_id)، بعدها DataTypeMigrator يعيد بناء الـ schema من JSON بدون tenant_id،
// بعدها DefaultHolding يبذر الـ Holding + CoA على الـ schema النظيف.
builder.Services.AddHostedService<MigrationRunnerHostedService>();  // Phase 6.0 (P6-0): Phase6_InitialSchema_20260725_120000 drops every old business table (Clean Slate) so the JSON migrator can rebuild without tenant_id
builder.Services.AddHostedService<DataTypeHostedService>();  // DEC-079 + DEC-096: JSON-driven schema migrator recreates all tables (no tenant_id) per the new model
builder.Services.AddHostedService<DefaultHoldingBootstrapHostedService>();  // Phase 6.0b (P6-0b): seeds the default Holding + 47-account CoA + 6 UoMs + 5 categories on the clean schema
builder.Services.AddHostedService<PoolWarmupHostedService>();  // PR #149 follow-up #2: تسخين الـ pool بعد bootstrap عشان أول user request ما يعلّقش 30+ ثانية
builder.Services.AddHostedService<OutboxProcessorHostedService>();

// ============ Seeders DISABLED for fresh-build deployments (2026-07-23, Mavis) ============
// Per the owner's "fresh build on HF Space" vision: the deploy starts with an empty DB
// (only DefaultCoASeed + DefaultInventorySeed as reference data). The owner registers the
// first real user via /api/auth/register.
//
// To re-enable seeders in a future environment (e.g., a demo/staging space), flip the
// flags in appsettings.json + revert this block. The seeder files (ScenarioSeederHostedService
// + RealisticSeedHostedService) are kept for re-enable but currently not registered in DI.
var seedAlFajr = builder.Configuration.GetValue<bool>("Database:SeedAlFajrScenario", false);
var seedAlBurj = builder.Configuration.GetValue<bool>("Database:SeedAlBurjScenario", false);
var seedRealistic = builder.Configuration.GetValue<bool>("Database:SeedRealisticScenario", false);
if (seedAlFajr) { builder.Services.AddHostedService<ScenarioSeederHostedService>(); Console.WriteLine("[SPRINT-4] SeedAlFajrScenario=true — AlFajr seeder registered."); } else { Console.WriteLine("[FRESH-BUILD] SeedAlFajrScenario=false — AlFajr seeder SKIPPED."); }
if (seedRealistic) { builder.Services.AddHostedService<RealisticSeedHostedService>(); Console.WriteLine("[SPRINT-4.5] SeedRealisticScenario=true — RealisticSeed registered."); } else { Console.WriteLine("[FRESH-BUILD] SeedRealisticScenario=false — RealisticSeed SKIPPED."); }
if (seedAlBurj)
{
    Console.Error.WriteLine("[SPRINT-4] WARNING: SeedAlBurjScenario=true — AlBurj seeder would run if registered (30K records; ensure DB is clean). NOT registered automatically (use manual endpoint).");
}
else
{
    Console.WriteLine("[SPRINT-4] SeedAlBurjScenario=false (SAFE default, DEC-009 prevention).");
}

// ============ Auth ============
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer, ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });
builder.Services.AddAuthorization(options =>
{
    // ============ DEC-053: RBAC Policies ============
    // Admin only
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.AdminOnly, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin));
    // Admin or Accountant
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.AdminOrAccountant, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant));
    // Admin or ProjectManager
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.AdminOrProjectManager, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.ProjectManager));
    // Any authenticated user
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.AnyAuthenticated, p =>
        p.RequireAuthenticatedUser());
    // Read access (all roles)
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.ReadAccess, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant,
            ERPSystem.Host.Auth.Roles.ProjectManager, ERPSystem.Host.Auth.Roles.Viewer));
    // Write finance
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.WriteFinance, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant));
    // Write projects
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.WriteProjects, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.ProjectManager));
    // Write stock
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.WriteStock, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant,
            ERPSystem.Host.Auth.Roles.ProjectManager));
    // Write master data (Admin only)
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.WriteMasterData, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin));
    // Write admin (Admin only)
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.WriteAdmin, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin));
    // ============ DEC-053 P1.5: Module-level aliases ============
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.HRWrite, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin));
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.FinanceWrite, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant));
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.ProcurementWrite, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant));
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.InventoryWrite, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant,
            ERPSystem.Host.Auth.Roles.ProjectManager));
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.EventsWrite, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant));
    options.AddPolicy(ERPSystem.Host.Auth.PolicyNames.AuditRead, p =>
        p.RequireRole(ERPSystem.Host.Auth.Roles.Admin, ERPSystem.Host.Auth.Roles.Accountant));
});

// ============ CORS ============
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ============ MVC + Swagger ============
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ERP-SYSTEM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DEC-100 / DL 69: Surface unhandled exceptions with JSON body in production
// (default behavior is empty 500). Catch exceptions anywhere in the pipeline.
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception | path={Path} method={Method}", ctx.Request.Path, ctx.Request.Method);
        if (!ctx.Response.HasStarted)
        {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                error = "UnhandledException",
                message = ex.Message,
                detail = ex.InnerException?.Message,
                path = ctx.Request.Path.Value,
                type = ex.GetType().Name,
            });
        }
    }
});

// Sprint-4 Day 3 (DEC-045): request tracking FIRST so all downstream logs have RequestId.
app.UseRequestTracking();
app.UseSerilogRequestLogging();
app.UseMiddleware<RequestTimingMiddleware>(); // DEC-111
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// Phase 6.1b: CompanyContextMiddleware is the sole context pipeline.
app.UseMiddleware<CompanyContextMiddleware>();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
public partial class Program { }
