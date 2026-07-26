using Dapper;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public interface IJournalEntryReportService
{
    Task<JournalEntryReport> GetAsync(Guid companyId, DateTime? from, DateTime? to, int? status, int skip, int take, CancellationToken ct);
}

public sealed class JournalEntryReportService : IJournalEntryReportService
{
    private readonly IDbConnectionFactory _db;
    public JournalEntryReportService(IDbConnectionFactory db) => _db = db;

    public async Task<JournalEntryReport> GetAsync(Guid companyId, DateTime? from, DateTime? to, int? status, int skip, int take, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);

        var where = new List<string> { "je.company_id = @CompanyId" };
        if (from.HasValue) where.Add("je.entry_date >= @From");
        if (to.HasValue) where.Add("je.entry_date <= @To");
        if (status.HasValue) where.Add("je.status = @Status");
        var whereClause = "WHERE " + string.Join(" AND ", where);

        // Aggregate lines per entry
        var summarySql = @"
            SELECT je.id, je.entry_number, je.entry_date, je.description, je.reference,
                   je.status, je.created_by_user_id, je.posted_at,
                   COALESCE(SUM(jl.debit), 0) AS total_debit,
                   COALESCE(SUM(jl.credit), 0) AS total_credit
            FROM journal_entries je
            LEFT JOIN journal_lines jl ON jl.journal_entry_id = je.id
            " + whereClause + @"
            GROUP BY je.id, je.entry_number, je.entry_date, je.description, je.reference,
                     je.status, je.created_by_user_id, je.posted_at
            ORDER BY je.entry_date DESC
            LIMIT @Take OFFSET @Skip";

        var rows = (await conn.QueryAsync<JournalEntryLineDto>(new CommandDefinition(summarySql,
            new { CompanyId = companyId, From = from, To = to, Status = status, Skip = skip, Take = take },
            cancellationToken: ct))).AsList();

        var totalsSql = @"
            SELECT COUNT(DISTINCT je.id) AS cnt,
                   COALESCE(SUM(jl.debit), 0) AS dr,
                   COALESCE(SUM(jl.credit), 0) AS cr
            FROM journal_entries je
            LEFT JOIN journal_lines jl ON jl.journal_entry_id = je.id
            " + whereClause;

        var totals = await conn.QueryFirstAsync<(int total_entries, decimal total_debit, decimal total_credit)>(new CommandDefinition(totalsSql,
            new { CompanyId = companyId, From = from, To = to, Status = status },
            cancellationToken: ct));

        return new JournalEntryReport
        {
            From = from,
            To = to,
            Status = status,
            TotalEntries = totals.total_entries,
            TotalDebit = totals.total_debit,
            TotalCredit = totals.total_credit,
            Lines = rows
        };
    }
}
