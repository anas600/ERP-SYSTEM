using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 019 — DEC-052 P2: T2 archive metadata table.
///
/// Tracks what has been archived to R2 (or any S3-compatible cold storage).
/// Each row = one batch of records archived in one run.
/// </summary>
[Migration(20260722_140000)]
public class AddArchiveMetadata : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS archive_metadata (
                id BIGSERIAL PRIMARY KEY,
                table_name VARCHAR(100) NOT NULL,
                -- Date range archived
                period_start TIMESTAMPTZ NOT NULL,
                period_end TIMESTAMPTZ NOT NULL,
                -- Records archived
                record_count BIGINT NOT NULL,
                -- Compressed size in bytes
                size_bytes BIGINT NOT NULL,
                -- SHA256 of the archive file
                sha256 VARCHAR(64) NOT NULL,
                -- Where the archive lives
                storage_backend VARCHAR(50) NOT NULL DEFAULT 'r2',  -- r2, s3, local
                storage_path VARCHAR(500) NOT NULL,                -- e.g., archive/audit_log/2025.jsonl.gz
                -- Metadata
                archived_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                archived_by VARCHAR(100) NOT NULL DEFAULT 'cron',   -- cron, manual, job
                tenant_id UUID NULL,                                -- NULL = cross-tenant
                notes TEXT NULL,
                CONSTRAINT chk_period CHECK (period_end > period_start)
            );

            CREATE INDEX IF NOT EXISTS ix_archive_metadata_table_period
                ON archive_metadata (table_name, period_end);
            CREATE INDEX IF NOT EXISTS ix_archive_metadata_archived_at
                ON archive_metadata (archived_at);");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS archive_metadata CASCADE;");
    }
}
