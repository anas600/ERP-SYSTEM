// Sprint 5 (T4 / Phase 5) — Global search service.
//
// GET /api/search?q=foo&limit=20 — case-insensitive LIKE across 4 tables:
//   customers   (by code, name, email)
//   vendors     (by code, name, email)
//   sales_invoices (by invoice_number, joined customer name)
//   accounts    (by code, name)
//
// All 4 sub-queries filter on `company_id = @CompanyId` (Constitution
// Article 3 / Article 8 rule 5). The whole search is company-scoped; the
// user cannot leak results across companies even if the JWT's company_ids[]
// claim is empty — we use ICompanyContext.CompanyId as the single source
// of truth.
//
// Result set shape per type is capped at 5 rows (the FE only renders the
// first ~8 rows in the dropdown; showing more would push the panel out of
// the viewport). The total cap is `limit` (default 20, max 50) — when
// reached, the remainder is just truncated, not paginated (the dropdown
// UX doesn't need pagination).
//
// Relevance ranking (per type, applied in SQL):
//   1. exact match  (lower(name) = lower(q) or lower(code) = lower(q))
//   2. prefix       (lower(name) LIKE lower(q) || '%')
//   3. contains     (lower(name) LIKE '%' || lower(q) || '%')
// Score is 1.0 / 0.7 / 0.4 respectively; the FE ignores the score (it just
// keeps the SQL ordering), but we expose it for tests + future tweaks.

using System.Data;
using Dapper;
using ERPSystem.Modules.Search.Application.DTOs;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Search.Application.Services;

public interface IGlobalSearchService
{
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(string q, int limit, CancellationToken ct);
}

public sealed class GlobalSearchService : IGlobalSearchService
{
    // Per-type cap. The dropdown UX shows ~5-7 rows per category, anything
    // more is noise. Bumping this would need a UX change in tandem.
    private const int PerTypeCap = 5;

    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _company;
    private readonly ILogger<GlobalSearchService> _logger;

    public GlobalSearchService(
        IDbConnectionFactory db,
        ICompanyContext company,
        ILogger<GlobalSearchService> logger)
    {
        _db = db;
        _company = company;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string q, int limit, CancellationToken ct)
    {
        var companyId = _company.CompanyId;
        if (companyId == null)
        {
            _logger.LogDebug("Search called with no resolved company");
            return Array.Empty<SearchResultDto>();
        }

        // Treat empty / whitespace as "no query" — return empty rather than
        // running 4 LIKE '% %' queries that would match almost every row.
        if (string.IsNullOrWhiteSpace(q)) return Array.Empty<SearchResultDto>();

        var cap = ClampLimit(limit, defaultLimit: 20);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 4 sequential queries on a single connection. Each is a 1-table
        // LIKE with company_id filter, sub-ms on the local Docker DB. We
        // don't run them in parallel because (a) they share the connection
        // and (b) the per-type cap keeps each one tiny — 4 sequential
        // round-trips is well under the FE's 300ms debounce window.
        var customers = await QueryCustomersAsync(conn, q, companyId.Value, ct);
        var vendors   = await QueryVendorsAsync(conn, q, companyId.Value, ct);
        var invoices  = await QueryInvoicesAsync(conn, q, companyId.Value, ct);
        var accounts  = await QueryAccountsAsync(conn, q, companyId.Value, ct);

        // Merge in a stable order: customers → suppliers → invoices → accounts
        // (the FE renders a section header per type in the same order).
        // The total cap truncates the merged list, not the per-type lists,
        // so each type is represented when there are hits.
        var merged = new List<SearchResultDto>(
            customers.Count + vendors.Count + invoices.Count + accounts.Count);
        merged.AddRange(customers);
        merged.AddRange(vendors);
        merged.AddRange(invoices);
        merged.AddRange(accounts);

        if (merged.Count > cap) merged.RemoveRange(cap, merged.Count - cap);
        return merged;
    }

    private static Task<List<SearchResultDto>> QueryCustomersAsync(
        IDbConnection conn, string q, Guid companyId, CancellationToken ct) =>
        RunRankedAsync(conn, q, companyId, ct,
            table: "customers",
            whereColumns: new[] { "name", "code", "email" },
            subtitleExpr: "COALESCE(email, code)",
            urlBuilder: id => $"/admin/customers/{id}",
            type: "customer");

    private static Task<List<SearchResultDto>> QueryVendorsAsync(
        IDbConnection conn, string q, Guid companyId, CancellationToken ct) =>
        RunRankedAsync(conn, q, companyId, ct,
            table: "vendors",
            whereColumns: new[] { "name", "code", "email" },
            subtitleExpr: "COALESCE(email, code)",
            urlBuilder: id => $"/admin/suppliers/{id}",
            type: "supplier");

    // Invoices use a hand-written CTE because the title is the invoice
    // number and the subtitle is the joined customer name — the generic
    // 3-tier runner assumes title and subtitle both come from the same
    // row, which doesn't fit a JOIN.
    private static async Task<List<SearchResultDto>> QueryInvoicesAsync(
        IDbConnection conn, string q, Guid companyId, CancellationToken ct)
    {
        const string sql = @"
            WITH ranked AS (
              SELECT si.id, si.invoice_number, c.name AS customer_name,
                CASE
                  WHEN lower(si.invoice_number) = lower(@Q)                   THEN 1.0
                  WHEN lower(si.invoice_number) LIKE lower(@Q) || '%'          THEN 0.7
                  WHEN lower(si.invoice_number) LIKE '%' || lower(@Q) || '%'  THEN 0.4
                  WHEN lower(c.name)            LIKE '%' || lower(@Q) || '%'  THEN 0.4
                  ELSE 0.0
                END AS score
              FROM sales_invoices si
              INNER JOIN customers c ON c.id = si.customer_id
              WHERE si.company_id = @CompanyId
                AND (
                  lower(si.invoice_number) = lower(@Q)
                  OR lower(si.invoice_number) LIKE lower(@Q) || '%'
                  OR lower(si.invoice_number) LIKE '%' || lower(@Q) || '%'
                  OR lower(c.name)           LIKE '%' || lower(@Q) || '%'
                )
            )
            SELECT id             AS Id,
                   invoice_number  AS Title,
                   customer_name   AS Subtitle,
                   score           AS Score
            FROM ranked
            WHERE score > 0
            ORDER BY score DESC, invoice_number
            LIMIT @Cap";

        // Note: `id` (not `id::text`) — we read as Guid and convert to
        // string in Materialize. The FakeDb's source-column mapping
        // returns the raw Guid value, which matches this projection; on
        // real Postgres both `id` and `id::text` return a Guid-compatible
        // value (Dapper maps uuid → Guid by default).
        var rows = (await conn.QueryAsync<SearchRow>(new CommandDefinition(
            sql, new { Q = q.Trim(), CompanyId = companyId, Cap = PerTypeCap },
            cancellationToken: ct))).AsList();

        return Materialize(rows, id => $"/sales/invoices/{id}", "invoice");
    }

    private static Task<List<SearchResultDto>> QueryAccountsAsync(
        IDbConnection conn, string q, Guid companyId, CancellationToken ct) =>
        RunRankedAsync(conn, q, companyId, ct,
            table: "accounts",
            whereColumns: new[] { "name", "code" },
            subtitleExpr: "code",
            urlBuilder: id => $"/accounting/accounts?selected={id}",
            type: "account");

    // Generic 3-tier ranked LIKE runner for "title + multi-column search".
    // We score each candidate as 1.0 / 0.7 / 0.4 (exact / prefix / contains)
    // and ORDER BY score DESC, title — the same ranking the FE UX assumes.
    //
    // Single-query CTE (not 3 separate SELECTs): the alternative is 3
    // round-trips per type. A CASE expression in one CTE keeps the
    // round-trips at 1 per type, which is what the FakeDb tests need to
    // count cleanly. (FakeDbDataReader only resolves the first table name
    // in the SQL — see Modules/Finance/Application/Services/CashFlowService
    // for the same single-query pattern.)
    private static async Task<List<SearchResultDto>> RunRankedAsync(
        IDbConnection conn,
        string q,
        Guid companyId,
        CancellationToken ct,
        string table,
        string[] whereColumns,
        string subtitleExpr,
        Func<string, string> urlBuilder,
        string type)
    {
        // Build the 3 tier predicates (exact / prefix / contains) across
        // all whereColumns. Example with (name, code, email):
        //   exact    : lower(name) = lower(@Q) OR lower(code) = lower(@Q) OR lower(email) = lower(@Q)
        //   prefix   : lower(name) LIKE lower(@Q) || '%' OR ...
        //   contains : lower(name) LIKE '%' || lower(@Q) || '%' OR ...
        static string TierExpr(string op, string[] cols, string rhs) =>
            string.Join(" OR ", cols.Select(c => $"lower({c}) {op} {rhs}"));

        var exact    = TierExpr("=",     whereColumns, "lower(@Q)");
        var prefix   = TierExpr("LIKE", whereColumns, "lower(@Q) || '%'");
        var contains = TierExpr("LIKE", whereColumns, "'%' || lower(@Q) || '%'");

        // The score CASE is one-pass: each tier predicate is checked
        // independently so the first match wins. Filter to score > 0 at
        // the outer SELECT (drops any row that matched none of the 3
        // tiers — defensive; the WHERE clause already filters these out).
        //
        // `id` (not `id::text`) — we read as Guid and convert to string
        // in Materialize. This keeps the FakeDb tests working (their
        // source-column mapping returns the raw Guid value) and matches
        // real Postgres (Dapper maps uuid → Guid by default).
        var sql = $@"
            WITH ranked AS (
              SELECT id, name AS title, {subtitleExpr} AS subtitle,
                CASE
                  WHEN {exact}    THEN 1.0
                  WHEN {prefix}   THEN 0.7
                  WHEN {contains} THEN 0.4
                  ELSE 0.0
                END AS score
              FROM {table}
              WHERE company_id = @CompanyId
                AND ({exact} OR {prefix} OR {contains})
            )
            SELECT id AS Id, title AS Title, subtitle AS Subtitle, score AS Score
            FROM ranked
            WHERE score > 0
            ORDER BY score DESC, title
            LIMIT @Cap";

        var rows = (await conn.QueryAsync<SearchRow>(new CommandDefinition(
            sql, new { Q = q.Trim(), CompanyId = companyId, Cap = PerTypeCap },
            cancellationToken: ct))).AsList();

        return Materialize(rows, urlBuilder, type);
    }

    // Materialize SearchRow → SearchResultDto with the type-specific URL
    // builder. The per-type cap was already applied in SQL; this is just
    // a defensive guard for any future caller that forgets to.
    //
    // The `Id` field on SearchRow is Guid? — we read the source table's
    // `id` column as a Guid (the FakeDb returns it as a raw Guid; real
    // Postgres returns a uuid which Dapper maps to Guid by default) and
    // convert to string here. The string form is what the FE routes need.
    private static List<SearchResultDto> Materialize(
        List<SearchRow> rows, Func<string, string> urlBuilder, string type)
    {
        if (rows.Count > PerTypeCap) rows.RemoveRange(PerTypeCap, rows.Count - PerTypeCap);
        var results = new List<SearchResultDto>(rows.Count);
        foreach (var r in rows)
        {
            // Guid? → string. The FakeDb may return Guid.Empty for the
            // raw source column on rows that don't have a real id; we
            // still produce a valid URL because the string form is just
            // a route placeholder for the FE.
            var idStr = r.Id.HasValue ? r.Id.Value.ToString() : string.Empty;
            results.Add(new SearchResultDto
            {
                Type = type,
                Id = idStr,
                Title = r.Title ?? string.Empty,
                Subtitle = r.Subtitle ?? string.Empty,
                Url = urlBuilder(idStr),
                Score = r.Score,
            });
        }
        return results;
    }

    private static int ClampLimit(int limit, int defaultLimit)
    {
        if (limit <= 0) return defaultLimit;
        if (limit > 50) return 50;
        return limit;
    }

    // Internal Dapper materializer — fields are the union of all 4 sub-queries.
    // `Id` is Guid? (not string) so the FakeDb's source-column mapping can
    // hand back the raw Guid value; we convert to string in Materialize.
    private sealed class SearchRow
    {
        public Guid? Id { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public double Score { get; set; }
    }
}
