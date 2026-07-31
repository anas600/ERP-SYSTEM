// Sprint 1 (T2 / Block A) — GET /api/holdings/{slug}
// Returns the Holding details + its child companies.
//
// Route is /api/holdings/{slug} (not /api/companies/...) to give the
// front-end a clean entry point for the dedicated Holding landing page
// (app/holding/page.tsx).
//
// Auth: any authenticated user (ReadAccess policy).
// Why ReadAccess and not WriteMasterData: viewing the Holding tree is
// information shared with every user. Only the actual editing lives behind
// WriteMasterData (see CompaniesController POST endpoints).
//
// The Holding is identified by is_group=true AND parent_company_id IS NULL.
// The companies[] array contains rows where parent_company_id = holding.id.

using ERPSystem.Modules.Companies.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Modules.Companies.Endpoints;

[ApiController]
[Route("api/holdings")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class HoldingsController : ControllerBase
{
    private readonly ICompanyService _service;
    public HoldingsController(ICompanyService service) { _service = service; }

    /// <summary>
    /// GET /api/holdings/{slug} — Holding details + list of child companies.
    /// Returns 200 with the HoldingDetail payload, 404 if the slug is unknown,
    /// or 400 for invalid input (empty slug).
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(new { error = "ValidationError", message = "الـ slug مطلوب." });
        }

        var r = await _service.GetHoldingBySlugAsync(slug, ct);
        if (r.Succeeded)
        {
            return Ok(r.Value);
        }

        // Frontend-first errors: surface the user-friendly Arabic message
        // and let the FE decide how to present it (toast / inline / 404 page).
        if (r.ErrorCode == CompanyErrorCode.NotFound)
        {
            return NotFound(new { error = "HoldingNotFound", slug, message = r.Error });
        }

        return BadRequest(new { error = "HoldingError", message = r.Error });
    }
}
