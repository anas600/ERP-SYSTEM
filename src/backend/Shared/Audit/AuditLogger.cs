using System.Text.Json;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.Audit;

/// <summary>
/// Default implementation of <see cref="IAuditLogger"/> using Dapper.
/// Stores audit entries in the `audit_log` table (DEC-056).
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(
        IDbConnectionFactory dbFactory,
        ICompanyContext companyContext,
        ILogger<AuditLogger> logger)
    {
        _dbFactory = dbFactory;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid companyId,
        string entityType,
        Guid entityId,
        string action,
        Guid? userId = null,
        object? changes = null,
        string? ipAddress = null)
    {
        if (companyId == Guid.Empty)
        {
            _logger.LogWarning("AuditLog skipped: companyId is empty (entity: {EntityType}/{EntityId})", entityType, entityId);
            return;
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            _logger.LogWarning("AuditLog skipped: entityType is empty");
            return;
        }

        try
        {
            const string sql = @"
                INSERT INTO audit_log (
                    company_id, entity_type, entity_id, action, user_id, changes, ip_address
                ) VALUES (
                    @CompanyId, @EntityType, @EntityId, @Action, @UserId, @Changes::jsonb, @IpAddress
                )";

            var changesJson = changes != null
                ? JsonSerializer.Serialize(changes)
                : null;

            using var conn = await _dbFactory.CreateOltpConnectionAsync();
            await conn.ExecuteAsync(sql, new
            {
                CompanyId = companyId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                Changes = changesJson,
                IpAddress = ipAddress
            });
        }
        catch (Exception ex)
        {
            // Audit failures must NOT break the original operation.
            _logger.LogError(ex,
                "Failed to write audit log for {EntityType}/{EntityId} action={Action}",
                entityType, entityId, action);
        }
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        object? changes = null)
    {
        var companyId = _companyContext.CompanyId;
        var userId = _companyContext.UserId;

        if (companyId == null || companyId == Guid.Empty)
        {
            _logger.LogWarning("AuditLog (context) skipped: company context empty");
            return;
        }

        await LogAsync(companyId.Value, entityType, entityId, action, userId, changes);
    }
}
