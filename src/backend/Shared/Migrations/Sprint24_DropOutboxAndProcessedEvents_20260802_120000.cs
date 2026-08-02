using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 24 (DEC-082) — Drop the event-bus outbox tables.
///
/// Refs:
///   - CONSTITUTION.md §3 (Multi-Company, no Multi-Tenancy)
///   - docs/architecture/REFACTOR-SPRINT-22.md (Sprint 22: no event bus)
///   - docs/team-charters/retrospectives/sprint-22-retro.md (L3: direct calls > event bus)
///
/// Why: Sprint 22 deleted the event bus code (IOutboxRepository, IProcessedEventsRepository,
/// OutboxEvent, ProcessedEvent, OutboxProcessor, OutboxProcessorHostedService, etc.) and
/// the corresponding entities + tests + DI registrations. But it left the JSON data-type
/// definitions in place, so the tables are still being created on every fresh install
/// and still exist in production DBs. This is the final piece of the "no event bus"
/// cleanup — drop the tables, drop the JSON, drop the last code references.
///
/// Up():
///   1. DROP TABLE IF EXISTS outbox_events CASCADE;
///   2. DROP TABLE IF EXISTS processed_events CASCADE;
///      (CASCADE because no other table should reference them, but if any does
///      it'll cascade-drop the FK too — safe in this context)
///   3. Also drop the indexes that may have been created outside the table DDL:
///      - ix_outbox_unprocessed, ix_outbox_company_type, ix_outbox_processed_at
///      - ix_processed_events_company, ix_processed_events_company_processed
///      (FluentMigrator DROP TABLE CASCADE drops indexes automatically in Postgres,
///      so this is defensive only.)
///
/// Down(): not supported. To restore, re-add the JSON definitions (outbox_events.json
/// and processed_events.json) from git history — they will be re-created on next
/// startup. This is a one-way migration; the event bus is gone for good.
///
/// Idempotency: every DROP uses IF EXISTS. Safe to re-run.
/// </summary>
[Migration(20260802_120000, TransactionBehavior.None)]
public class Sprint24_DropOutboxAndProcessedEvents : Migration
{
    public override void Up()
    {
        // outbox_events — was the write-side queue for the deleted IEventBus.
        // The events used to be written here, then OutboxProcessorHostedService
        // polled and published them to handlers. With direct service calls
        // (Sprint 22), nothing writes here anymore.
        Execute.Sql("DROP TABLE IF EXISTS outbox_events CASCADE;");

        // processed_events — was the dedup table ("I already processed event X").
        // Without the event bus, dedup is no longer needed at this layer; if a
        // future cross-module flow needs idempotency, it can use the standard
        // (source_entity, source_id) unique index pattern from Finance.
        Execute.Sql("DROP TABLE IF EXISTS processed_events CASCADE;");
    }

    public override void Down()
    {
        // No-op. The tables are intentionally gone. To revert, restore the JSON
        // files from git history; the DataTypeMigrator will recreate the tables
        // on next startup with the original schema (no company_id, no FK constraints
        // to anything that depends on them — this is the legacy multi-tenant shape).
        throw new NotSupportedException(
            "Sprint 24 outbox + processed_events drop is one-way. " +
            "To restore the event bus, re-add the JSON data-type definitions " +
            "and the corresponding C# entities + repositories.");
    }
}
