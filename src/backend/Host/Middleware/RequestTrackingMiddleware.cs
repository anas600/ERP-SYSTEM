using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace ERPSystem.Host.Middleware;

/// <summary>
/// Middleware لتتبع الطلبات (Request Tracking):
///  - يولّد/يحفظ X-Request-ID لكل طلب
///  - يقيس وقت التنفيذ
///  - يضيف RequestId + CompanyId (إن وُجد) إلى LogContext
///
/// Sprint-4 Day 3 (DEC-045). Phase 6.1b: multi-company model — logs are
/// enriched with CompanyId (from default_company_id or company_id JWT claim)
/// instead of the obsolete tenant_id claim.
/// </summary>
public class RequestTrackingMiddleware
{
    public const string RequestIdHeader = "X-Request-ID";
    public const string RequestIdItemKey = "RequestId";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTrackingMiddleware> _logger;

    public RequestTrackingMiddleware(RequestDelegate next, ILogger<RequestTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Resolve or generate RequestId
        var requestId = context.Request.Headers.TryGetValue(RequestIdHeader, out var incoming) && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[RequestIdItemKey] = requestId;
        context.Response.Headers[RequestIdHeader] = requestId;

        // 2. Extract CompanyId from JWT claim if present (best-effort)
        // Phase 6.1b: multi-company model. Auth flow still emits tenant_id
        // claim with Guid.Empty (deferred to 6.1c) — we prefer the newer
        // default_company_id / company_id claims if they exist.
        string? companyId = null;
        string? userId = null;
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            companyId = user.FindFirst("default_company_id")?.Value
                        ?? user.FindFirst("company_id")?.Value;
            userId = user.FindFirst("sub")?.Value
                     ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        // 3. Push to Serilog LogContext (structured logging enrichment)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("CompanyId", companyId ?? "-"))
        using (LogContext.PushProperty("UserId", userId ?? "-"))
        using (LogContext.PushProperty("Method", context.Request.Method))
        using (LogContext.PushProperty("Path", context.Request.Path.Value ?? ""))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation(
                    "{Method} {Path} {StatusCode} in {ElapsedMs}ms (RequestId={RequestId})",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds,
                    requestId);
            }
        }
    }
}

/// <summary>
/// Extension methods for easy registration.
/// </summary>
public static class RequestTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestTracking(this IApplicationBuilder app)
        => app.UseMiddleware<RequestTrackingMiddleware>();
}
