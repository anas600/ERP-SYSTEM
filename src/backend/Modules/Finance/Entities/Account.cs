using System;

namespace ERPSystem.Modules.Finance.Entities;

public enum AccountType { Asset = 1, Liability = 2, Equity = 3, Revenue = 4, Expense = 5 }
public enum NormalBalance { Debit = 1, Credit = 2 }

public class Account
{
    public Guid Id { get; set; }
    // Sprint 28 (DEC-097): was Guid? — DB column is NOT NULL, so entity should match.
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public NormalBalance NormalBalance { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsPostable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsIntercompany { get; set; } = false;
    /// <summary>
    /// Sprint 52a: CoA hierarchy level (1=L1 Class, 2=L2 Sub-class, 3=L3 Control, 4=L4 Detail).
    /// Computed from parent chain by AccountLevelBackfillHostedService.
    /// Null for legacy accounts not yet backfilled (set automatically on first run).
    /// </summary>
    public short? Level { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
