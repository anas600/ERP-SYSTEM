using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 007 — Event Bus (Outbox Pattern) + Idempotency
///
/// - outbox_events: queue of integration events. Inserted in same transaction
///   as the business operation (atomic). Background processor reads + dispatches.
/// - processed_events: idempotency — if EventId already here, skip (duplicate).
/// </summary>
[Migration(20260615_110000)]
public class AddOutboxAndProcessedEvents : Migration
{
    public override void Up()
    {
        Create.Table("outbox_events")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("event_type").AsString(100).NotNullable()
            .WithColumn("aggregate_id").AsGuid().NotNullable()
            .WithColumn("aggregate_type").AsString(50).NotNullable()
            .WithColumn("payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("occurred_at").AsDateTime().NotNullable()
            .WithColumn("processed_at").AsDateTime().Nullable()
            .WithColumn("retry_count").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("max_retries").AsInt32().NotNullable().WithDefaultValue(3)
            .WithColumn("last_error").AsString(int.MaxValue).Nullable();
        // FIFO index for the processor (only unprocessed events)
        Create.Index("ix_outbox_unprocessed").OnTable("outbox_events")
            .OnColumn("occurred_at").Ascending();
        Create.Index("ix_outbox_tenant_type").OnTable("outbox_events")
            .OnColumn("tenant_id").Ascending().OnColumn("event_type").Ascending();

        Create.Table("processed_events")
            .WithColumn("event_id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("processed_at").AsDateTime().NotNullable();
        Create.Index("ix_processed_events_tenant").OnTable("processed_events")
            .OnColumn("tenant_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("processed_events");
        Delete.Table("outbox_events");
    }
}
