// Sprint 53 (DEC-140) — Year-End Closing Hosted Service
//
// يتفقّد السنوات السابقة على بداية التشغيل ويقفلها تلقائيًا لو لم تكن مقفلة بعد.
// idempotent — لو السنة مقفلة بالفعل، لا يعمل شي.
//
// يُشغَّل مرة واحدة عند بدء التطبيق، بعد كل الـ seeders وقبل HTTP.

using Dapper;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Host.Bootstrap;

public sealed class YearEndClosingHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<YearEndClosingHostedService> _logger;

    public YearEndClosingHostedService(
        IServiceScopeFactory scopeFactory,
        IDbConnectionFactory db,
        ILogger<YearEndClosingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _db = db;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Find all companies
            List<Guid> companies;
            using (var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken))
            {
                companies = (await conn.QueryAsync<Guid>(new CommandDefinition(
                    "SELECT id FROM companies",
                    cancellationToken: cancellationToken))).ToList();
            }

            var currentYear = DateTime.UtcNow.Year;
            // Close the previous year if it's not closed (most common case)
            var yearsToCheck = new[] { currentYear - 1, currentYear };

            foreach (var companyId in companies)
            {
                foreach (var year in yearsToCheck)
                {
                    if (year < 2000) continue;

                    // Create a scope per (company, year) so IYearEndClosingService (Scoped) can be resolved
                    using var scope = _scopeFactory.CreateScope();
                    var yearEnd = scope.ServiceProvider.GetRequiredService<IYearEndClosingService>();

                    var status = await yearEnd.GetStatusAsync(companyId, year, cancellationToken);
                    if (status.IsClosed)
                    {
                        _logger.LogInformation(
                            "[Sprint53] Year {Year} already closed for company {CompanyId} — skipping",
                            year, companyId);
                        continue;
                    }
                    _logger.LogInformation(
                        "[Sprint53] Auto-closing year {Year} for company {CompanyId}...",
                        year, companyId);
                    var r = await yearEnd.CloseYearAsync(companyId, year, cancellationToken);
                    if (r.Success)
                    {
                        _logger.LogInformation(
                            "[Sprint53] ✓ Year {Year} closed for company {CompanyId}. NetIncome={NI:N2}. Msg={Msg}",
                            year, companyId, r.NetIncome, r.Message);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Sprint53] ✗ Year {Year} close failed for company {CompanyId}. Msg={Msg}",
                            year, companyId, r.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sprint53] Year-end closing hosted service failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
