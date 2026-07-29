// Sprint 5 (T4 / Phase 5) — GET /api/search
//
// Global search across customers, suppliers, sales_invoices, and accounts.
// The response is a flat list of SearchResultDto with a "type" discriminator
// so the FE can render a single unified dropdown with the right icon per row.
//
// Auth: any authenticated user (ReadAccess policy) — same as the dashboard
// because the search box is in the top bar, visible to every role.
//
// Validation:
//   - q: required, 1-100 chars after trim. Empty / whitespace → 400.
//   - limit: optional, default 20, clamped to [1, 50] by the service.

using ERPSystem.Modules.Search.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Modules.Search.Endpoints;

[ApiController]
[Route("api/search")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.ReadAccess)]
public class SearchController : ControllerBase
{
    private readonly IGlobalSearchService _service;
    public SearchController(IGlobalSearchService service) { _service = service; }

    /// <summary>
    /// GET /api/search?q=foo&amp;limit=20 — global search.
    /// Returns 200 with an empty array when no company context is resolved
    /// (the FE renders an empty state instead of a 401). Returns 400 only
    /// when `q` is missing or empty (no point running 4 LIKE queries).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int? limit,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "q is required." });
        }
        if (q.Length > 100)
        {
            return BadRequest(new { error = "q is too long (max 100 chars)." });
        }

        var results = await _service.SearchAsync(q, limit ?? 20, ct);
        return Ok(results);
    }
}
