using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Sprint 62 (DEC-198) — Billing PDF export.
///
/// <para>One endpoint that streams a bilingual (AR + EN) Progress Billing
/// certificate for a single billing. The controller is a thin wrapper:
/// it reads the billing + contract + project via the existing repository
/// interfaces, builds a flat <see cref="BillingPdfModel"/>, and hands it
/// to <see cref="IPdfExportService"/> for rendering.</para>
///
/// <para><b>L19 / DEC-095</b>: the controller does not pass any userId
/// to the service — <c>IPdfExportService</c> is a pure renderer with no
/// company-scoping concerns. All scoping happens upstream in the
/// <see cref="IBillingRepository.GetByIdAsync"/> call (which is a
/// company-scoped query inside the repository).</para>
/// </summary>
[ApiController]
[Authorize]
public sealed class BillingPdfController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly IProjectRepository _projects;
    private readonly IContractRepository _contracts;
    private readonly IPdfExportService _pdf;
    private readonly ILogger<BillingPdfController> _logger;

    public BillingPdfController(
        IBillingService billingService,
        IProjectRepository projects,
        IContractRepository contracts,
        IPdfExportService pdf,
        ILogger<BillingPdfController> logger)
    {
        _billingService = billingService;
        _projects = projects;
        _contracts = contracts;
        _pdf = pdf;
        _logger = logger;
    }

    [HttpGet("api/projects/{projectId:guid}/billings/{id:guid}/pdf")]
    public async Task<IActionResult> Download(
        Guid projectId, Guid id, CancellationToken ct)
    {
        // 1) Load the billing
        var billingRes = await _billingService.GetByIdAsync(id, ct);
        if (!billingRes.Succeeded)
            return NotFound(ProblemDetailsFor("Billing not found.", StatusCodes.Status404NotFound));
        var billing = billingRes.Value!;

        // 2) Path consistency: the billing must belong to {projectId}
        if (billing.ProjectId != projectId)
        {
            _logger.LogWarning("Billing {Id} project mismatch: path={PathId} actual={ActualId}",
                id, projectId, billing.ProjectId);
            return BadRequest(ProblemDetailsFor(
                "Project mismatch: billing does not belong to the specified project.",
                StatusCodes.Status400BadRequest));
        }

        // 3) Load project + contract (best-effort — fall back to billing-only labels if missing)
        var project = await _projects.GetByIdAsync(billing.ProjectId, ct);
        var contract = await _contracts.GetByIdAsync(billing.ContractId, ct);

        var model = new BillingPdfModel(
            ProjectCode: project?.Code ?? billing.ProjectId.ToString(),
            ProjectName: project?.Name ?? string.Empty,
            ContractNumber: contract?.ContractNumber,
            BillingNumber: billing.BillingNumber,
            BillingDate: billing.BillingDate,
            PeriodFrom: billing.PeriodFrom,
            PeriodTo: billing.PeriodTo,
            WorkCompletedPercent: billing.WorkCompletedPercent,
            GrossAmount: billing.GrossAmount,
            AdvanceDeducted: billing.AdvanceDeducted,
            RetentionDeducted: billing.RetentionDeducted,
            RegionalPremiumDeducted: billing.RegionalPremiumDeducted,
            NetAmountAfterPremium: billing.NetAmountAfterPremium,
            Notes: billing.Notes
        );

        var bytes = _pdf.GenerateBillingPdf(model);
        var filename = $"billing-{Sanitise(billing.BillingNumber)}.pdf";
        _logger.LogInformation("Generated PDF for billing {Id} ({Bytes} bytes)", id, bytes.Length);
        return File(bytes, "application/pdf", filename);
    }

    private static string Sanitise(string s)
    {
        // Keep ASCII letters/digits/dot/dash/underscore so the filename works
        // in every browser + OS shell. Anything else is replaced with '-'.
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            bool keep = (c >= '0' && c <= '9')
                     || (c >= 'A' && c <= 'Z')
                     || (c >= 'a' && c <= 'z')
                     || c == '-' || c == '_' || c == '.';
            if (!keep) chars[i] = '-';
        }
        return new string(chars);
    }

    private static ProblemDetails ProblemDetailsFor(string detail, int status) => new()
    {
        Title = "Billing PDF Error",
        Status = status,
        Detail = detail,
    };
}
