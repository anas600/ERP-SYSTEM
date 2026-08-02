using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 24 (DEC-083) — Add company_id to document sequence tables.
///
/// Why: Constitution Article 3 requires every multi-tenant-scoped table to have
/// company_id. Three sequence tables were created at runtime (not in the JSON
/// data-types) and missed the company_id column:
///   - procurement_document_sequences (used by Procurement + Payments)
///   - hr_document_sequences (used by HR)
///   - (ar_document_sequences already has company_id, added in earlier work)
///
/// In a single-deployment-with-N-subsidiaries world, two companies sharing the
/// same prefix (e.g. "PO") would collide on the same sequence number. Adding
/// company_id to the PK solves this.
///
/// Up():
///   For each table:
///     1. ALTER TABLE ... ADD COLUMN company_id UUID (nullable first for safe migration)
///     2. Backfill: for existing rows, set company_id = (SELECT id FROM companies LIMIT 1)
///        — this is the only safe default in a non-multi-company legacy DB.
///     3. ALTER TABLE ... ALTER COLUMN company_id SET NOT NULL
///     4. DROP CONSTRAINT <old PK> (PK was just (prefix))
///     5. ADD PRIMARY KEY (company_id, prefix)
///
/// Idempotency:
///   - ADD COLUMN IF NOT EXISTS
///   - Backfill is a no-op if column is already populated
///   - ALTER COLUMN SET NOT NULL is guarded with a DO block (only fails if NULLs exist)
///   - DROP CONSTRAINT IF EXISTS
///   - ADD CONSTRAINT ... PRIMARY KEY (uses DO block to handle "already exists")
///
/// Down(): not supported. Reverting would require deleting rows for one company
/// to satisfy the old (prefix)-only PK.
/// </summary>
[Migration(20260802_121000, TransactionBehavior.None)]
public class Sprint24_DocumentSequencesAddCompanyId : Migration
{
    public override void Up()
    {
        // ============== procurement_document_sequences ==============
        Execute.Sql(@"
            ALTER TABLE procurement_document_sequences
                ADD COLUMN IF NOT EXISTS company_id UUID;
        ");

        // Backfill: assign existing rows to the first company (only safe default for legacy).
        Execute.Sql(@"
            UPDATE procurement_document_sequences
            SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1)
            WHERE company_id IS NULL;
        ");

        // Enforce NOT NULL + change PK to (company_id, prefix).
        // DO blocks because Postgres won't ALTER COLUMN SET NOT NULL if any NULLs remain
        // (we already backfilled, but defensive). And the PK swap is wrapped in DO
        // because the constraint may already exist in the new shape on fresh installs.
        Execute.Sql(@"
            DO $$
            BEGIN
                -- Set NOT NULL (will fail if NULLs exist; we backfilled above)
                BEGIN
                    ALTER TABLE procurement_document_sequences
                        ALTER COLUMN company_id SET NOT NULL;
                EXCEPTION WHEN OTHERS THEN
                    RAISE NOTICE 'procurement_document_sequences: company_id NOT NULL skipped (%): %',
                        SQLSTATE, SQLERRM;
                END;

                -- Drop the old single-column PK if it still exists
                BEGIN
                    ALTER TABLE procurement_document_sequences
                        DROP CONSTRAINT procurement_document_sequences_pkey;
                EXCEPTION WHEN OTHERS THEN
                    RAISE NOTICE 'procurement_document_sequences: old PK drop skipped (%): %',
                        SQLSTATE, SQLERRM;
                END;

                -- Add the new composite PK (idempotent: skip if already exists)
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'procurement_document_sequences_pkey'
                ) THEN
                    ALTER TABLE procurement_document_sequences
                        ADD PRIMARY KEY (company_id, prefix);
                END IF;
            END $$;
        ");

        // ============== hr_document_sequences ==============
        Execute.Sql(@"
            ALTER TABLE hr_document_sequences
                ADD COLUMN IF NOT EXISTS company_id UUID;
        ");

        Execute.Sql(@"
            UPDATE hr_document_sequences
            SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1)
            WHERE company_id IS NULL;
        ");

        Execute.Sql(@"
            DO $$
            BEGIN
                BEGIN
                    ALTER TABLE hr_document_sequences
                        ALTER COLUMN company_id SET NOT NULL;
                EXCEPTION WHEN OTHERS THEN
                    RAISE NOTICE 'hr_document_sequences: company_id NOT NULL skipped (%): %',
                        SQLSTATE, SQLERRM;
                END;

                BEGIN
                    ALTER TABLE hr_document_sequences
                        DROP CONSTRAINT hr_document_sequences_pkey;
                EXCEPTION WHEN OTHERS THEN
                    RAISE NOTICE 'hr_document_sequences: old PK drop skipped (%): %',
                        SQLSTATE, SQLERRM;
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'hr_document_sequences_pkey'
                ) THEN
                    ALTER TABLE hr_document_sequences
                        ADD PRIMARY KEY (company_id, prefix);
                END IF;
            END $$;
        ");
    }

    public override void Down()
    {
        // No-op. Reverting would risk data loss in multi-company deployments
        // (the old single-column PK cannot satisfy the backfilled rows).
        throw new NotSupportedException(
            "Sprint 24 document-sequences-add-company_id is one-way. " +
            "Reverting would orphan the rows that were backfilled to a single company.");
    }
}
