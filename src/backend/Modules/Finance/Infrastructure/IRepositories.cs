using System.Data;
using ERPSystem.Modules.Finance.Entities;

namespace ERPSystem.Modules.Finance.Infrastructure;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Account?> GetByCodeAsync(string code, CancellationToken ct);
    Task<IReadOnlyList<Account>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<IReadOnlyList<Account>> ListChildrenAsync(Guid parentId, CancellationToken ct);
    Task<IReadOnlyList<Account>> ListByCompanyAsync(Guid? companyId, CancellationToken ct);
    Task InsertAsync(Account account, CancellationToken ct);
    Task InsertAsync(Account account, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload (called by EnsureDefaultCoAAsync inside the tx)
    Task UpdateAsync(Account account, CancellationToken ct);
    Task<int> CountPostingsAsync(Guid accountId, CancellationToken ct);
    Task EnsureDefaultCoAAsync(Guid companyId, CancellationToken ct);
    Task EnsureDefaultCoAAsync(Guid companyId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task CloneCoAFromCompanyAsync(Guid targetCompanyId, Guid sourceCompanyId, CancellationToken ct);
}

public interface IJournalEntryRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<JournalEntry?> GetWithLinesAsync(Guid id, CancellationToken ct);
    Task<bool> EntryNumberExistsAsync(string entryNumber, CancellationToken ct);
    Task<string> GetNextEntryNumberAsync(CancellationToken ct);
    Task InsertAsync(JournalEntry entry, CancellationToken ct);
    Task UpdateAsync(JournalEntry entry, CancellationToken ct);
    Task<IReadOnlyList<JournalEntry>> ListAsync(DateTime? from, DateTime? to, JournalEntryStatus? status, int skip, int take, CancellationToken ct);
}

public interface IPostingRuleRepository
{
    Task<PostingRule?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PostingRule>> ListActiveByEventAsync(TriggeringEvent eventType, CancellationToken ct);
    Task<IReadOnlyList<PostingRule>> ListAsync(CancellationToken ct);
    Task InsertAsync(PostingRule rule, CancellationToken ct);
    Task UpdateAsync(PostingRule rule, CancellationToken ct);
}
