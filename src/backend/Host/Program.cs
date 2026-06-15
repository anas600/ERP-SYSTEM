// ERP-SYSTEM Backend Entry Point
// Phase 0: Foundation + Identity Module

using System.Text;
using System.Text.Json.Serialization;
using ERPSystem.Host.Swagger;
using ERPSystem.Modules.Identity.Application.Auth;
using ERPSystem.Modules.Identity.Infrastructure;
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

// ============ Logging (Serilog) ============
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ============ Configuration Bindings ============
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings غير معرّف في appsettings.");
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

// ============ Multi-tenancy ============
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ============ Application Services ============
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ============ Redis (اختياري) ============
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConn));
}

// ============ FluentMigrator ============
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("Postgres"))
        .ScanIn(typeof(CreateIdentityTables).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddSerilog());

builder.Services.AddHostedService<MigrationRunnerHostedService>();

// ============ Auth Pipeline ============
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // dev فقط — يُشدد في prod
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });
builder.Services.AddAuthorization();

// ============ API Docs & MVC ============
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP-SYSTEM API",
        Version = "v1",
        Description = "API الخاصة بنظام ERP-SYSTEM (Phase 0: Identity)"
    });
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل JWT Token مباشرة (بدون كلمة Bearer)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.OperationFilter<SwaggerTenantHeaderFilter>();
});

var app = builder.Build();

// ============ Middleware Pipeline ============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP-SYSTEM v1");
        c.DocumentTitle = "ERP-SYSTEM API";
    });
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();
app.MapControllers();

app.Run();

// Make the implicit Program class visible to integration tests
public partial class Program { }
