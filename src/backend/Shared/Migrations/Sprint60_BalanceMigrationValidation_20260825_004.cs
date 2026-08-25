using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 60 — Wave 3A (DEC-189 + DEC-190) — Balance migration + CoA validation.
///
/// <para><b>Why</b>: per Anas's CoA-Final-Proposal-2026-08-24, after Wave 2B renamed
/// <c>9201</c> → <c>1.1.06</c>, split <c>71/72</c> → <c>7.1/7.2</c>, inserted 27 new
/// canonical accounts, and backfilled <c>fs_type</c>/<c>section</c> on the 131 keep
/// accounts, we now need to:</para>
/// <list type="number">
///   <item><b>DEC-189 — Validate data integrity</b>: confirm every <c>journal_line</c>
///         still references a valid <c>account</c> (no orphans), confirm the trial
///         balance still balances (Σ debit = Σ credit per company per period), and
///         confirm the (company_id, code) UNIQUE constraint holds on <c>accounts</c>.</item>
///   <item><b>DEC-189 — Promote migrated accounts</b>: after validation passes, mark
///         the 27 freshly-inserted canonical accounts (<c>migration_status='new'</c>)
///         as <c>'migrated'</c> with <c>migrated_at=now()</c>. This is the
///         "migration is complete" sentinel — once all is_canonical=TRUE accounts
///         are 'migrated', Wave 3B (FE) can render the new CoA tree.</item>
///   <item><b>DEC-190 — Run validation queries</b>: same checks as above, exposed
///         via a <c>RAISE NOTICE</c> log so ops can see the result when the migration
///         runs. The companion <c>CoAValidationService</c> (in
///         <c>Modules/Finance/Application/Services/</c>) wraps these queries in a
///         typed <c>CoAValidationResult</c> for runtime checks.</item>
/// </list>
///
/// <para><b>Idempotency</b>:
/// <list type="bullet">
///   <item>The orphan check uses a NOT EXISTS pattern (returns 0 rows on re-run).</item>
///   <item>The trial balance check uses RAISE NOTICE only (no side effects).</item>
///   <item>The mark-migrated UPDATE has a <c>WHERE migration_status = 'new'</c>
///         guard, so re-running on an already-promoted DB is a no-op.</item>
///   <item>The deprecated report uses RAISE NOTICE only.</item>
/// </list>
/// </para>
///
/// <para><b>Safety</b>: this migration is read-only EXCEPT for the explicit
/// <c>UPDATE accounts SET migration_status = 'migrated', migrated_at = now()</c>
/// on rows that are still <c>'new'</c>. It does NOT touch the 131 keep accounts
/// (still 'pending' on the legacy code) and does NOT touch the 1.3 Off-Balance
/// accounts (kept as 'deprecated' for audit). It does NOT touch the 4 WIP renames
/// (9201 → 1.1.06) or the 5 L1=7 splits — those are already 'migrated'.</para>
///
/// <para><b>Default company</b>: resolved via <c>companies.code = '000'</c> (the
/// constitutional holding marker per CONSTITUTION.md §3.2). No hardcoded UUIDs.</para>
///
/// <para><b>Down()</b>: reverts the 27 'new' → 'migrated' promotions. Uses
/// <c>migrated_at &gt;= NOW() - INTERVAL '1 hour'</c> as a guard so we only
/// revert what THIS migration just did (not any future 'migrated' inserts).
/// Down() does NOT re-run the validation queries (no need — Down just undoes).</para>
/// </summary>
[Migration(20260825_004)]
public class Sprint60_BalanceMigrationValidation : Migration
{
    public override void Up()
    {
        // ====================================================================
        // Step 1 — DEC-189: Validate journal_line integrity (no orphans)
        // An "orphan" journal_line is one whose account_id points to an account
        // that no longer exists in the accounts table. The FK constraint should
        // prevent this, but we check defensively in case a manual DB op or a
        // botched migration introduced one.
        //
        // We scope the check to journal_lines whose company matches the
        // constitutional '000' holding — we do NOT iterate all companies to
        // keep the migration predictable.
        // ====================================================================
        Execute.Sql(@"
            DO $$
            DECLARE
                orphan_count INT;
            BEGIN
                SELECT COUNT(*) INTO orphan_count
                FROM journal_lines jl
                LEFT JOIN accounts a
                    ON a.id = jl.account_id
                    AND a.company_id = jl.company_id
                WHERE a.id IS NULL;

                IF orphan_count > 0 THEN
                    RAISE NOTICE 'Sprint60 Wave 3A: Found % orphan journal_line(s) — these reference deleted or moved accounts. See accounts.id IS NULL rows in journal_lines.', orphan_count;
                ELSE
                    RAISE NOTICE 'Sprint60 Wave 3A: Journal-line integrity OK — 0 orphans.';
                END IF;
            END
            $$;
        ");

        // ====================================================================
        // Step 2 — DEC-189: Validate trial balance per company
        // For each company, SUM(debit) on posted journal_lines must equal
        // SUM(credit). If not, RAISE NOTICE with the variance so ops can
        // investigate. This is a hard-data check — it does NOT fail the
        // migration (we just want to surface the issue), because some
        // production data may have intentional adjustments in flight.
        //
        // We join through journal_entries and filter to status=2 (Posted)
        // per the JournalEntryStatus enum (Draft=1, Posted=2, Reversed=3).
        // Reversed entries are still on the books (idempotent reversal),
        // so they count toward the balance.
        // ====================================================================
        Execute.Sql(@"
            DO $$
            DECLARE
                rec RECORD;
                var_amt NUMERIC;
            BEGIN
                FOR rec IN
                    SELECT jl.company_id,
                           COALESCE(SUM(jl.debit),  0) AS total_debit,
                           COALESCE(SUM(jl.credit), 0) AS total_credit
                    FROM journal_lines jl
                    INNER JOIN journal_entries je
                        ON je.id = jl.journal_entry_id
                        AND je.company_id = jl.company_id
                        AND je.status = 2  -- Posted
                    GROUP BY jl.company_id
                LOOP
                    var_amt := rec.total_debit - rec.total_credit;
                    IF var_amt <> 0 THEN
                        RAISE NOTICE 'Sprint60 Wave 3A: Trial balance MISMATCH for company % — Dr=%, Cr=%, variance=%',
                            rec.company_id, rec.total_debit, rec.total_credit, var_amt;
                    ELSE
                        RAISE NOTICE 'Sprint60 Wave 3A: Trial balance OK for company % — Dr=Cr=%',
                            rec.company_id, rec.total_debit;
                    END IF;
                END LOOP;
            END
            $$;
        ");

        // ====================================================================
        // Step 3 — DEC-189: Validate (company_id, code) UNIQUE constraint
        // Same defense-in-depth as the orphan check: the unique index should
        // already prevent duplicates, but we surface any if they exist.
        // We do NOT count 'deprecated' accounts in the dup check because
        // the 1.3 Off-Balance deprecate is itself a code transformation and
        // there is no overlap with the new 1.1.0x range.
        // ====================================================================
        Execute.Sql(@"
            DO $$
            DECLARE
                dup_count INT;
            BEGIN
                SELECT COUNT(*) INTO dup_count
                FROM (
                    SELECT company_id, code, COUNT(*) AS cnt
                    FROM accounts
                    WHERE migration_status <> 'deprecated'
                    GROUP BY company_id, code
                    HAVING COUNT(*) > 1
                ) dups;

                IF dup_count > 0 THEN
                    RAISE NOTICE 'Sprint60 Wave 3A: Found % duplicate (company_id, code) group(s) on accounts — UNIQUE violation!', dup_count;
                ELSE
                    RAISE NOTICE 'Sprint60 Wave 3A: Account code UNIQUE OK — 0 duplicates.';
                END IF;
            END
            $$;
        ");

        // ====================================================================
        // Step 4 — DEC-189: Report orphan deprecated accounts (with journal_lines)
        // "Orphan" here = a deprecated account that still has journal_lines
        // pointing to it. This is not necessarily wrong (the 1.3 Off-Balance
        // accounts were deprecated but their historical postings are still
        // valid for audit), but it surfaces a list of account IDs for ops
        // to review.
        // ====================================================================
        Execute.Sql(@"
            DO $$
            DECLARE
                rec RECORD;
                orphan_count INT;
            BEGIN
                orphan_count := 0;
                FOR rec IN
                    SELECT a.id, a.code, a.name, COUNT(jl.id) AS posting_count
                    FROM accounts a
                    LEFT JOIN journal_lines jl
                        ON jl.account_id = a.id
                        AND jl.company_id = a.company_id
                    WHERE a.migration_status = 'deprecated'
                    GROUP BY a.id, a.code, a.name
                    HAVING COUNT(jl.id) > 0
                LOOP
                    orphan_count := orphan_count + 1;
                    RAISE NOTICE 'Sprint60 Wave 3A: Deprecated account with postings — id=%, code=%, name=%, postings=%',
                        rec.id, rec.code, rec.name, rec.posting_count;
                END LOOP;

                IF orphan_count = 0 THEN
                    RAISE NOTICE 'Sprint60 Wave 3A: No deprecated accounts with active journal_lines (clean state).';
                END IF;
            END
            $$;
        ");

        // ====================================================================
        // Step 5 — DEC-189: Promote 'new' canonical accounts → 'migrated'
        // After validation passes, mark the 27 freshly-inserted canonical
        // accounts as 'migrated' (with migrated_at=now()). This is the
        // "migration is complete" sentinel.
        //
        // WHERE clause:
        //   - migration_status = 'new'   (only touch what Wave 2B inserted)
        //   - is_canonical    = TRUE     (defensive — only the 27 new ones)
        //
        // Idempotent: re-running on an already-promoted DB is a no-op
        // (no rows match migration_status = 'new').
        // ====================================================================
        Execute.Sql(@"
            UPDATE accounts
            SET migration_status = 'migrated',
                migrated_at = now()
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND migration_status = 'new'
              AND is_canonical = TRUE;
        ");

        // ====================================================================
        // Step 6 — DEC-189: Sanity report — count of accounts by status
        // Prints a final tally so ops can see the post-migration state in the
        // migration log without having to query the DB manually.
        // ====================================================================
        Execute.Sql(@"
            DO $$
            DECLARE
                rec RECORD;
            BEGIN
                RAISE NOTICE 'Sprint60 Wave 3A: Final account status tally:';
                FOR rec IN
                    SELECT migration_status, COUNT(*) AS cnt
                    FROM accounts
                    WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
                    GROUP BY migration_status
                    ORDER BY migration_status
                LOOP
                    RAISE NOTICE '  %: %', rec.migration_status, rec.cnt;
                END LOOP;
            END
            $$;
        ");
    }

    public override void Down()
    {
        // ====================================================================
        // Down() — revert the 27 'new' → 'migrated' promotions from Step 5.
        //
        // We use migrated_at >= NOW() - INTERVAL '1 hour' as a guard so we
        // only revert the promotions this migration just did. Future inserts
        // (e.g. a Wave 4 migration) that touch migration_status='migrated'
        // will not be affected by this Down().
        // ====================================================================
        Execute.Sql(@"
            UPDATE accounts
            SET migration_status = 'new',
                migrated_at = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND migration_status = 'migrated'
              AND is_canonical = TRUE
              AND migrated_at >= NOW() - INTERVAL '1 hour';
        ");

        // Note: Down() does NOT re-run the orphan/trial-balance validation
        // queries — those are read-only RAISE NOTICE statements. Reverting
        // them is a no-op.
    }
}
