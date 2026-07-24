using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ERPSystem.Shared.Infrastructure;

/// <summary>
/// تنفيذ IDbConnectionFactory باستخدام Npgsql مع إعدادات Resiliency افتراضية
/// (DEC-093: Command Timeout / Keepalive / Pool sizing — تُطبَّق على كل connection
/// حتى لو الـ connection string المُمرّر ما يحويها).
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
        var csb = new NpgsqlConnectionStringBuilder(_options.OltpConnectionString)
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
        var csb = new NpgsqlConnectionStringBuilder(cs)
        {
            CommandTimeout = _options.CommandTimeoutSeconds,
            Timeout = _options.ConnectionTimeoutSeconds,
            MinPoolSize = _options.MinPoolSize,
            MaxPoolSize = _options.MaxPoolSize,
            KeepAlive = _options.KeepaliveSeconds,
            ConnectionIdleLifetime = _options.ConnectionIdleLifetimeSeconds,
            ConnectionPruningInterval = 10,
        };
        var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        _logger.LogDebug("فُتح اتصال EventStore جديد (DB={Database}, Pool={Min}-{Max}, CmdTimeout={Cmd}s)",
            conn.Database, csb.MinPoolSize, csb.MaxPoolSize, csb.CommandTimeout);
        return conn;
    }
}

/// <summary>إعدادات الاتصال بقواعد البيانات + Resiliency baseline (DEC-093)</summary>
public sealed class NpgsqlConnectionOptions
{
    public string OltpConnectionString { get; set; } = string.Empty;
    public string? EventStoreConnectionString { get; set; }

    // Resiliency baseline (DEC-093, 2026-07-24)
    public int CommandTimeoutSeconds { get; set; } = 60;
    public int ConnectionTimeoutSeconds { get; set; } = 15;
    public int MaxPoolSize { get; set; } = 20;
    public int MinPoolSize { get; set; } = 1;
    public int KeepaliveSeconds { get; set; } = 30;
    public int ConnectionIdleLifetimeSeconds { get; set; } = 300;
}
