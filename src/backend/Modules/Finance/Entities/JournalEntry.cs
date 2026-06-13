using System;
using System.Collections.Generic;

namespace ERPSystem.Modules.Finance.Entities;

/// <summary>
/// JournalEntry - represents a financial transaction in double-entry bookkeeping
/// Phase 1: Finance Core
/// Event-sourced via MartenDB
/// </summary>
public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public JournalEntryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}

public class JournalLine
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
}

public enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3
}
