// Sprint 5 (T4 / Phase 5) — Global search DTOs.
//
// One DTO for all 4 result types (customer / supplier / invoice / account).
// The "type" discriminator + the FE-side switch lets the UI render a single
// unified dropdown with the right icon per row, instead of 4 separate
// result lists that the FE would have to merge on its own.
//
// Field names are camelCase (System.Text.Json default) and stable across
// versions; do not rename without bumping the FE type.

namespace ERPSystem.Modules.Search.Application.DTOs;

/// <summary>
/// One search result row.
///   type      — "customer" | "supplier" | "invoice" | "account" (drives FE icon)
///   id        — primary key in the source table (string, so the FE can
///                append it to the route as-is without re-encoding the GUID)
///   title     — primary label shown in the dropdown (customer name, vendor
///                name, invoice number, account name)
///   subtitle  — secondary line (email, code, customer name on invoices, …)
///   url       — frontend route to navigate to on click. We return a full
///                client-side path (e.g. "/sales/invoices/<id>") so the
///                FE just does router.push(url) without a switch.
///   score     — relevance score from GetSearchService (0..1, not surfaced
///                to the user; kept for tests and future ranking tweaks)
/// </summary>
public sealed class SearchResultDto
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public double Score { get; set; }
}
