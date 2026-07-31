using System.Data;
using System.Web;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ERPSystem.Shared.Infrastructure;

/// <summary>
/// تنفيذ IDbConnectionFactory باستخدام Npgsql مع إعدادات Resiliency افتراضية
/// (DEC-093: Command Timeout / Keepalive / Pool sizing — تُطبَّق على كل connection
/// حتى لو الـ connection string المُمرّر ما يحويها).
///
/// DEC-096: Npgsql 8.0.5 لا يفك URL-encoding للـ Password في connection string.
///   appsettings.json:Password=QZYn8S%26%2Fif%21%23i%26e تبقى كما هي → Supabase
///   يرفض المصادقة (28P01) → "password authentication failed for user postgres".
///   على Linux/HF، env vars عادة تحتوي raw password فيتجاوز الـ bug.
///   نعمل URL-decode للـ Password هنا عشان نشتغل على Windows + Linux بنفس الـ config.
/// </summary>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlConnectionOptions _options;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;

    public NpgsqlConnectionFactory(IOptions<NpgsqlConnectionOptions> options, ILogger<NpgsqlConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IDbConnection> CreateOltpConnectionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OltpConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Postgres غير معرّف في الإعدادات.");
        }
        var csb = BuildBuilderWithResiliency(_options.OltpConnectionString);
        var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("فُتح اتصال OLTP جديد (DB={Database}, Pool={Min}-{Max}, CmdTimeout={Cmd}s)",
            conn.Database, csb.MinPoolSize, csb.MaxPoolSize, csb.CommandTimeout);
        return conn;
    }

    public async Task<IDbConnection> CreateEventStoreConnectionAsync(CancellationToken ct = default)
    {
        var cs = _options.EventStoreConnectionString ?? _options.OltpConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("ConnectionStrings:Marten غير معرّف في الإعدادات.");
        }
        var csb = BuildBuilderWithResiliency(cs);
        var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("فُتح اتصال EventStore جديد (DB={Database}, Pool={Min}-{Max}, CmdTimeout={Cmd}s)",
            conn.Database, csb.MinPoolSize, csb.MaxPoolSize, csb.CommandTimeout);
        return conn;
    }

    /// <summary>
    /// اتصال واحد مباشر بدون pool — مخصّص لـ DefaultHoldingBootstrap (DEC-093 + Phase 6.3).
    /// السبب: pgbouncer transaction-mode يقفل backend connections بعد كل transaction،
    /// فالـ acquire المتتالي من client pool يصبح بطيء جداً (5+ دقائق). bootstrap
    /// يحتاج N عمليات متتالية (SELECT + INSERT Holding + batched CoA + UoMs + Categories)
    /// على connection واحد، فالحل هو pooling=false + connection lifetime واحدة فقط.
    /// </summary>
    public async Task<IDbConnection> CreateEphemeralOltpConnectionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OltpConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Postgres غير معرّف في الإعدادات.");
        }
        var csb = BuildBuilderWithResiliency(_options.OltpConnectionString);
        // Force pooling=false. ConnectionPruningInterval/IdleLifetime ما يفيدون
        // لأننا نستخدم connection مرة واحدة ثم نرميها.
        csb.Pooling = false;
        var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("[P6-3 ephemeral] فُتح اتصال OLTP مؤقت (Pool=false, CmdTimeout={Cmd}s)",
            csb.CommandTimeout);
        return conn;
    }

    /// <summary>
    /// اتصال مباشر (port 5432, بدون Supavisor/pgbouncer) مخصّص لـ Schema migrations
    /// (FluentMigrator + DataTypeMigrator). السبب الجذري للـ PR #149: pgbouncer
    /// transaction-mode (port 6543) يـ release الـ backend بعد كل transaction، فـ
    /// DDL statements المتعاقبة ممكن توصل backends مختلفة → CREATE TABLE users على
    /// backend A، ALTER TABLE users ADD COLUMN على backend B ما شافش الـ table →
    /// "42P01: relation users does not exist". الحل الرسمي من Supabase docs:
    /// use direct connection (port 5432) for migrations. يُرجع null لو ما في
    /// ConnectionStrings:Migrations معرّف (fallback على ephemeral OLTP).
    /// </summary>
    public async Task<IDbConnection?> CreateEphemeralMigrationConnectionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.MigrationsConnectionString))
        {
            _logger.LogWarning("[Migration] ConnectionStrings:Migrations غير معرّف — لن يتم تشغيل الـ migrations عبر direct connection. الـ DataTypeMigrator سيستخدم الـ OLTP ephemeral (pgbouncer).");
            return null;
        }
        var csb = new NpgsqlConnectionStringBuilder(_options.MigrationsConnectionString);
        // Direct connection assumes Pooling=false already set in config; force it again defensively.
        csb.Pooling = false;
        // URL-decode password for Npgsql 8.0.5 (DEC-096)
        if (!string.IsNullOrEmpty(csb.Password) && csb.Password.Contains('%'))
        {
            try
            {
                var decoded = HttpUtility.UrlDecode(csb.Password);
                if (!string.IsNullOrEmpty(decoded) && decoded != csb.Password)
                {
                    csb.Password = decoded;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NpgsqlConnectionFactory] Failed to URL-decode Migration Password — using as-is");
            }
        }
        var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("[Migration] فُتح اتصال Migration مباشر (Host={Host}, Port={Port}, Db={Db})",
            csb.Host, csb.Port, csb.Database);
        return conn;
    }

    private NpgsqlConnectionStringBuilder BuildBuilderWithResiliency(string connectionString)
    {
        var csb = new NpgsqlConnectionStringBuilder(connectionString)
        {
            // Resiliency baseline (DEC-093). الـ values الموجودة في الـ connection string تأخذ أولوية
            // لو محتاجين override على مستوى environment.
            CommandTimeout = _options.CommandTimeoutSeconds,
            Timeout = _options.ConnectionTimeoutSeconds,
            MinPoolSize = _options.MinPoolSize,
            MaxPoolSize = _options.MaxPoolSize,
            KeepAlive = _options.KeepaliveSeconds,
            ConnectionIdleLifetime = _options.ConnectionIdleLifetimeSeconds,
            ConnectionPruningInterval = 10,
        };

        // DEC-096: URL-decode the password if it appears URL-encoded.
        // Npgsql 8.0.5 keeps the value as-is, which breaks Supabase auth on Windows.
        if (!string.IsNullOrEmpty(csb.Password) && csb.Password.Contains('%'))
        {
            try
            {
                var decoded = HttpUtility.UrlDecode(csb.Password);
                if (!string.IsNullOrEmpty(decoded) && decoded != csb.Password)
                {
                    csb.Password = decoded;
                    _logger.LogDebug("[NpgsqlConnectionFactory] URL-decoded Password (was {OldLen} chars, now {NewLen} chars)",
                        csb.Password.Length, decoded.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NpgsqlConnectionFactory] Failed to URL-decode Password — using as-is");
            }
        }

        return csb;
    }
}

/// <summary>إعدادات الاتصال بقواعد البيانات + Resiliency baseline (DEC-093)</summary>
public sealed class NpgsqlConnectionOptions
{
    public string OltpConnectionString { get; set; } = string.Empty;
    public string? EventStoreConnectionString { get; set; }

    /// <summary>
    /// اتصال مباشر (port 5432, بدون Supavisor/pgbouncer) للـ schema migrations
    /// فقط. لو غير معرّف، الـ migrators يستخدمون الـ OLTP ephemeral connection
    /// (مع كل المخاطر اللي شرحناها في PR #149).
    /// </summary>
    public string? MigrationsConnectionString { get; set; }

    // Resiliency baseline (DEC-093, 2026-07-24)
    // Phase 6.3 hotfix (PR #149 follow-up): CommandTimeout 60→180. السبب: في cold
    // start (CI runner → Supabase transatlantic) كان أول DB call بعد DataTypeMigrator
    // يصطدم بـ stale pooled connection ويعلّق 60s ثم يرمي TimeoutException. مع
    // الـ retry (3 محاولات) في DefaultHoldingBootstrapHostedService، نحتاج budget
    // كافٍ: 3 × 180s + 6s backoff = 546s ≤ 600s (e2e health check timeout).
    public int CommandTimeoutSeconds { get; set; } = 180;
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public int MaxPoolSize { get; set; } = 20;
    public int MinPoolSize { get; set; } = 1;
    public int KeepaliveSeconds { get; set; } = 30;
    // Phase 6.3 hotfix: 300→60. السبب: Supabase pgbouncer يغلق connection idle بعد
    // 60-120s على الـ transaction mode. Npgsql ما يكتشف هذا الـ server-side close إلا
    // عند أول read = 60s timeout. بتقصير الـ idle lifetime لـ 60s، الـ pool يتخلّص
    // من connections المشكوك فيها قبل أن يطلعها user.
    public int ConnectionIdleLifetimeSeconds { get; set; } = 60;
}
