using FluentMigrator.Runner;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// خدمة تعمل مع بدء التطبيق — تنفّذ كل الـ migrations المعلّقة على قاعدة OLTP.
/// في الإنتاج: نفضّل تشغيلها كخطوة منفصلة (job) قبل نشر الـ pods.
/// في dev: نشغّلها تلقائياً لراحة المطور.
/// </summary>
public sealed class MigrationRunnerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationRunnerHostedService> _logger;
    private readonly IConfiguration _config;

    public MigrationRunnerHostedService(
        IServiceProvider serviceProvider,
        ILogger<MigrationRunnerHostedService> logger,
        IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // نسمح بتعطيلها في الإنتاج
        var autoMigrate = _config.GetValue("Database:AutoMigrate", true);
        if (!autoMigrate)
        {
            _logger.LogInformation("Auto-migrate معطّل — يجب تشغيل الـ migrations يدوياً.");
            return Task.CompletedTask;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            _logger.LogInformation("بدء تنفيذ الـ migrations المعلّقة…");
            runner.MigrateUp();
            _logger.LogInformation("تم تنفيذ جميع الـ migrations بنجاح.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل تنفيذ الـ migrations!");
            // في dev: نسمح للخدمة بالاستمرار حتى يمكن فحصها
            // في prod: الأفضل أن نرمي exception لمنع البدء
            throw;
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
