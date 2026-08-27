using System.Security.Claims;
using ERPSystem.Modules.Projects.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 61 (DEC-193) — Photo upload endpoint for Engineer's Daily Reports.
///
/// <para><b>Route</b>: <c>POST /api/engineer-reports/{reportId}/photos</c> (multipart file upload).</para>
///
/// <para><b>Storage strategy</b>: photos are written to disk under
/// <c>wwwroot/uploads/engineer-reports/{reportId}/{guid}.{ext}</c> (gitignored). The
/// public URL is <c>/uploads/engineer-reports/{reportId}/{filename}</c> (handled by
/// ASP.NET static files middleware on <see cref="IWebHostEnvironment.WebRootPath"/>).</para>
///
/// <para><b>Atomicity</b>: the file is written first, then the DB row is inserted. If the
/// DB insert fails, the file is best-effort deleted (logged). This matches the pattern
/// used in <c>PhotoRepository</c>-style attachments elsewhere in the codebase.</para>
/// </summary>
[ApiController]
[Authorize]
public sealed class EngineerReportPhotosController : ControllerBase
{
    private readonly IEngineerReportService _service;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EngineerReportPhotosController> _logger;

    // Hard caps to avoid path-traversal and disk-fill attacks.
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic"
    };

    public EngineerReportPhotosController(
        IEngineerReportService service,
        IWebHostEnvironment env,
        ILogger<EngineerReportPhotosController> logger)
    {
        _service = service; _env = env; _logger = logger;
    }

    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")!.Value);

    /// <summary>رفع صورة مرفقة لتقرير المهندس (multipart/form-data; field name = "file").</summary>
    [HttpPost("api/engineer-reports/{reportId:guid}/photos")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(
        Guid reportId, IFormFile? file, [FromQuery] string? caption, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("file is required");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest($"file too large (>{MaxFileSizeBytes / 1024 / 1024} MB)");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest($"unsupported file type '{ext}'. Allowed: {string.Join(", ", AllowedExtensions)}");

        // Resolve upload dir: {WebRoot}/uploads/engineer-reports/{reportId}
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "engineer-reports", reportId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        // Sanitize: filename is always a fresh GUID + extension — ignore the client-provided name.
        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        // Public URL (relative path stored in the DB; the static-files middleware
        // serves it from wwwroot).
        var publicPath = $"/uploads/engineer-reports/{reportId}/{fileName}";

        try
        {
            var r = await _service.AddPhotoAsync(UserId, reportId, publicPath, caption, ct);
            if (!r.Succeeded)
            {
                // Best-effort cleanup on DB failure.
                try { System.IO.File.Delete(absolutePath); } catch { /* swallow */ }
                return r.ErrorCode == EngineerReportErrorCode.NotFound
                    ? NotFound(Problem(r))
                    : BadRequest(Problem(r));
            }
            return Created(publicPath, r.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo upload failed for report {ReportId}", reportId);
            try { System.IO.File.Delete(absolutePath); } catch { /* swallow */ }
            throw;
        }
    }

    private static ProblemDetails Problem<T>(EngineerReportResult<T> r) => new()
    {
        Title = "EngineerReport Photo Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
