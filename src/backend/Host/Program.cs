// ERP-SYSTEM Backend Entry Point
// Phase 0: Foundation + Identity Module
// Phase 1: Finance Core
// Phase 1.5: Multi-Company Foundation
// Phase 2.1: Projects Module

using System.Text;
using System.Text.Json.Serialization;
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
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Modules.Notifications.Application.Services;
using ERPSystem.Modules.Notifications.Infrastructure;
using ERPSystem.Modules.Finance.Application.EventHandlers;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.Events;
using ERPSystem.Shared.Events.Application.Services;
using ERPSystem.Shared.Events.Infrastructure;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.Migrations;
using ERPSystem.Shared.MultiTenancy;
using FluentMigrator.Runner;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ============ Logging ============
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ============ Configuration ============
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings غير معرّف.");
builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtSettings));

builder.Services.Configure<NpgsqlConnectionOptions>(opts =>
{
    opts.OltpConnectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("ConnectionStrings:Postgres غير معرّف.");
    opts.EventStoreConnectionString = builder.Configuration.GetSection("Marten")["ConnectionString"];
});

// ============ Infrastructure ============
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
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
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
builder.Services.AddScoped<IPostingRuleRepository, PostingRuleRepository>();
builder.Services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();
builder.Services.AddScoped<IProcessedEventsRepository, ProcessedEventsRepository>();

// ============ Multi-tenancy ============
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ============ Services ============
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<ITenantBootstrap>(sp => sp.GetRequiredService<CompanyService>());
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
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
builder.Services.AddScoped<IVendorBillService, VendorBillService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
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
        configOptions.ConnectTimeout = 2000;       // timeout قصير (2s) عشان ما نطوّل startup
        return ConnectionMultiplexer.Connect(configOptions);
    });
}

// ============ FluentMigrator ============
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("Postgres"))
        .ScanIn(typeof(CreateIdentityTables).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddSerilog());
builder.Services.AddHostedService<MigrationRunnerHostedService>();
builder.Services.AddHostedService<OutboxProcessorHostedService>();

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
builder.Services.AddAuthorization();

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

app.UseSerilogRequestLogging();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
public partial class Program { }
