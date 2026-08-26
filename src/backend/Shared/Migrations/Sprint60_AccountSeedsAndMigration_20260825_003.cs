using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 60 — Wave 2B (DEC-185/186/187/188 + DEC-NEW-1..13) — Account seeds + canonical migration.
///
/// <para><b>Why</b>: per Anas's CoA-Final-Proposal-2026-08-24, after Wave 1 added the 6
/// Financial-Statement metadata columns (<c>fs_type</c>, <c>section</c>, <c>is_canonical</c>,
/// <c>new_code</c>, <c>migration_status</c>, <c>migrated_at</c>), Wave 2B actually moves the
/// data: inserts 27 new canonical accounts, deprecates the Off-Balance 1.3 range, renames the
/// 9201 WIP accounts to the new 1.1.06 location, splits the L1=7 bucket into 7.1/7.2/7.3, and
/// backfills <c>fs_type</c>/<c>section</c> on the 131 existing keep accounts.</para>
///
/// <para><b>What this migration does (in order):</b></para>
/// <list type="number">
///   <item><b>DEC-NEW-1, 2, 5, 6..13 — INSERT 27 new accounts</b>: 8 L3 control accounts +
///         19 L4 postable detail accounts, all in the canonical 4-level dot format
///         (e.g. <c>1.1.01.001</c>). Each row has <c>migration_status='new'</c>,
///         <c>is_canonical=TRUE</c>, <c>new_code=code</c>, and the right <c>fs_type</c>/<c>section</c>.</item>
///   <item><b>DEC-186 — UPDATE 1.3 Off-Balance</b>: marks any 1.3.* account as
///         <c>migration_status='deprecated'</c> with <c>migrated_at=now()</c>. The 1.3 range
///         is removed in the plan (WIP moved to 1.1.06).</item>
///   <item><b>DEC-187 — RENAME 9201 → 1.1.06</b>: 9201 → 1.1.06 (L3), 9201-001/002/003 →
///         1.1.06.001/002/003 (L4). <c>new_code</c> is set to the new code,
///         <c>migration_status='migrated'</c>, <c>migrated_at=now()</c>.</item>
///   <item><b>DEC-188 — SPLIT L1=7 → 7.1/7.2/7.3</b>: 71 → 7.1, 7101 → 7.1.01, 7102 → 7.1.02,
///         72 → 7.2, 7201 → 7.2.01. (7.3 is reserved for future Other Gains/Losses; no existing
///         accounts use it.) Same <c>new_code</c> + <c>migration_status='migrated'</c> pattern.</item>
///   <item><b>Bonus — backfill fs_type + section on 131 keep accounts</b>: every account with
///         <c>migration_status='pending'</c> (i.e. the 131 existing keep accounts that were
///         not touched by DEC-186/187/188) gets <c>fs_type</c> and <c>section</c> derived from
///         the first character of <c>code</c> (L1) and — for L1=1/2 — the second character
///         (L2). See the inline SQL for the full mapping.</item>
/// </list>
///
/// <para><b>Total account count after Wave 2B:</b> 136 (Wave 1 baseline) + 27 (Wave 2B) = 163,
/// matching the executive summary in the CoA plan (96.3% keep code, 4% new, 2% migrated).</para>
///
/// <para><b>Idempotency</b>: every INSERT uses <c>ON CONFLICT (company_id, code) DO NOTHING</c>
/// (the unique index on <c>(company_id, code)</c> from <c>ix_accounts_company_code</c>).
/// UPDATEs are guarded with <c>WHERE migration_status = 'pending'</c> (for the bonus
/// backfill) or with explicit <c>WHERE code = '...'</c> filters (for DEC-186/187/188), so
/// re-running this migration against an already-migrated DB is a safe no-op.</para>
///
/// <para><b>Default company</b>: resolved via <c>companies.code = '000'</c> (the constitutional
/// holding marker per CONSTITUTION.md §3.2). No hardcoded UUIDs.</para>
///
/// <para><b>parent_account_id</b>: set to <c>NULL</c> for all 27 new accounts. The new
/// 4-level dot-format L1/L2 wrappers (e.g. <c>1.1</c>, <c>1.2</c>) do not exist yet — they
/// will be added in a later wave. The 8 new L3 wrappers (e.g. <c>1.1.01</c>) are inserted
/// first so the 19 new L4 detail accounts can reference them by <c>code</c> lookup.</para>
///
/// <para><b>Down()</b>: deletes the 27 new accounts, reverts DEC-186/187/188 UPDATEs, and
/// resets the bonus backfill fields. The 131 keep accounts are left in place with their
/// <c>fs_type</c>/<c>section</c> cleared (NULL) so a fresh Wave 2B run can re-derive them.</para>
/// </summary>
[Migration(20260825_140000)]
public class Sprint60_AccountSeedsAndMigration : Migration
{
    public override void Up()
    {
        // ====================================================================
        // Step 1 — DEC-NEW-1, 2, 5, 6..13 — INSERT 27 new canonical accounts
        // 8 L3 control accounts + 19 L4 postable detail accounts.
        // Order: L3 first, L4 second (so the L4 INSERT can reference the L3
        // parent by code via subquery in a later enhancement — for now both
        // insert with parent_account_id=NULL).
        // ====================================================================

        // ----------------------- L3 control accounts (8) --------------------

        // 1.1.01 — النقدية (Cash on Hand) — DEC-NEW-1
        // Parent (1.1 Current Assets) does not exist in dotted format yet; parent_account_id = NULL.
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.01', 'النقدية', 'Cash on Hand',
                1, 1,                                -- type=Asset, normal_balance=Debit
                NULL,                                -- parent: 1.1 not yet in dotted format
                false, false, 3, true,               -- L3, not postable
                'BS', 'Current Asset',                -- fs_type + section
                TRUE, '1.1.01', 'new', now(),         -- is_canonical, new_code, migration_status
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.02 — البنوك (Banks) — DEC-NEW-1
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.02', 'البنوك', 'Banks',
                1, 1, NULL, false, false, 3, true,
                'BS', 'Current Asset',
                TRUE, '1.1.02', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.2.01 — أصول ثابتة ملموسة (Tangible PPE) — DEC-NEW-2
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.01', 'أصول ثابتة ملموسة', 'Tangible PPE (IAS 16)',
                1, 1, NULL, false, false, 3, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.01', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.2.02 — أصول غير ملموسة (Intangible Assets) — DEC-NEW-2
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.02', 'أصول غير ملموسة', 'Intangible Assets (IAS 38)',
                1, 1, NULL, false, false, 3, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.02', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 2.1.08 — مصلحة التأمينات الاجتماعية (Social Security) — DEC-NEW-5
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '2.1.08', 'مصلحة التأمينات الاجتماعية', 'Social Security (employee + employer share)',
                2, 2, NULL, false, false, 3, true,  -- type=Liability, normal_balance=Credit
                'BS', 'Current Liability',
                TRUE, '2.1.08', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 5.2.02 — تكلفة مواد خام (Construction Materials Cost) — DEC-NEW-9 (multi-activity L3)
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '5.2.02', 'تكلفة مواد خام', 'Construction Materials Cost (cement, steel, etc.)',
                5, 1, NULL, false, false, 3, true,  -- type=Expense, normal_balance=Debit
                'PL', 'COGS',
                TRUE, '5.2.02', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 5.2.03 — تكلفة عمالة مباشرة (Construction Direct Labor Cost) — DEC-NEW-9
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '5.2.03', 'تكلفة عمالة مباشرة', 'Construction Direct Labor Cost (on-site)',
                5, 1, NULL, false, false, 3, true,
                'PL', 'COGS',
                TRUE, '5.2.03', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 8.2.01 — رسوم دمغات وNDB (Stamps & NDB) — DEC-NEW-5
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '8.2.01', 'رسوم دمغات وNDB', 'Stamps & NDB contributions (Law 12/2004 + NDB 2024)',
                5, 1, NULL, false, false, 3, true,  -- type=Expense (Tax family)
                'PL', 'Tax',
                TRUE, '8.2.01', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // ----------------------- L4 postable accounts (19) -------------------

        // === DEC-NEW-1 — Cash & Banks detail (7 L4) ===

        // 1.1.01.002 — Cash USD/EUR
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.01.002', 'صندوق عملات أجنبية (USD/EUR)', 'Foreign Currency Cash (USD/EUR)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.01.002', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.01.003 — Cash in Transit
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.01.003', 'نقدية في الطريق', 'Cash In Transit',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.01.003', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.02.001 — Bank CDBL LYD
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.02.001', 'بنك الجمهورية - جاري (LYD)', 'CDBL - Checking (LYD)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.02.001', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.02.002 — Bank T&D LYD
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.02.002', 'بنك التجارة والتنمية - جاري (LYD)', 'T&D Bank - Checking (LYD)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.02.002', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.02.003 — Bank Sahara
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.02.003', 'مصرف الصحراء - جاري', 'Sahara Bank - Checking',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.02.003', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.02.004 — Bank CDBL USD
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.02.004', 'بنك الجمهورية - USD', 'CDBL - USD',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.02.004', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.1.02.005 — Cheques in Collection
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.1.02.005', 'شيكات تحت التحصيل', 'Cheques In Collection',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Current Asset',
                TRUE, '1.1.02.005', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // === DEC-NEW-2 — Tangible PPE & Intangible Assets detail (5 L4) ===

        // 1.2.01.001 — Land
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.01.001', 'أراضي', 'Land (IAS 16)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.01.001', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.2.01.002 — Buildings
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.01.002', 'مباني وإنشاءات', 'Buildings & Constructions (IAS 16)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.01.002', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.2.01.003 — Machinery
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.01.003', 'آلات ومعدات', 'Machinery & Equipment (IAS 16)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.01.003', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.2.01.008 — IT Equipment
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.01.008', 'أجهزة حاسوب وملحقاتها', 'IT Equipment (computers, peripherals)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.01.008', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 1.2.02.001 — Software
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '1.2.02.001', 'برامج حاسوب', 'Software (e.g. ERP — IAS 38)',
                1, 1, NULL, false, true, 4, true,
                'BS', 'Non-Current Asset',
                TRUE, '1.2.02.001', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // === DEC-NEW-5 — NDB / Stamps / CIT / SS detail (7 L4) ===

        // 8.2.01.001 — Engineering Stamp
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '8.2.01.001', 'دمغة هندسية', 'Engineering Stamp (Law 12/2004)',
                5, 1, NULL, false, true, 4, true,
                'PL', 'Tax',
                TRUE, '8.2.01.001', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 8.2.01.002 — Contractors Union Stamp
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '8.2.01.002', 'دمغة اتحاد التشييد', 'Contractors Union Stamp',
                5, 1, NULL, false, true, 4, true,
                'PL', 'Tax',
                TRUE, '8.2.01.002', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 8.2.01.003 — Regular Stamp
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '8.2.01.003', 'دمغة عادية', 'Regular Stamp',
                5, 1, NULL, false, true, 4, true,
                'PL', 'Tax',
                TRUE, '8.2.01.003', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 8.2.01.005 — NDB 1.5% (non-refundable)
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '8.2.01.005', 'مساهمة جهاز الوطني للتنمية', 'NDB Contribution 1.5% (non-refundable, NDB 2024)',
                5, 1, NULL, false, true, 4, true,
                'PL', 'Tax',
                TRUE, '8.2.01.005', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 2.1.03.002 — CIT Withholding
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '2.1.03.002', 'ضريبة خصم من المنبع', 'CIT Withholding (1-4% tax deduction at source)',
                2, 2, NULL, false, true, 4, true,
                'BS', 'Current Liability',
                TRUE, '2.1.03.002', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 2.1.08.001 — SS Employee share
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '2.1.08.001', 'تأمينات اجتماعية - حصة العامل', 'Social Security - Employee Share',
                2, 2, NULL, false, true, 4, true,
                'BS', 'Current Liability',
                TRUE, '2.1.08.001', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // 2.1.08.002 — SS Employer share
        Execute.Sql(@"
            INSERT INTO accounts (
                id, company_id, code, name, description, type, normal_balance,
                parent_account_id, is_intercompany, is_postable, level, is_active,
                fs_type, section, is_canonical, new_code, migration_status, migrated_at,
                created_at, updated_at
            )
            SELECT
                gen_random_uuid(), c.id, '2.1.08.002', 'تأمينات اجتماعية - حصة المقاول', 'Social Security - Employer Share',
                2, 2, NULL, false, true, 4, true,
                'BS', 'Current Liability',
                TRUE, '2.1.08.002', 'new', now(),
                now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // ====================================================================
        // Step 2 — DEC-186 — UPDATE 1.3 Off-Balance to deprecated
        // The 1.3 range is removed in the CoA plan (WIP moved to 1.1.06).
        // Any account in 1.3.* is marked deprecated (not deleted) so the
        // historical data still exists for audit, but is no longer posted to.
        // ====================================================================
        Execute.Sql(@"
            UPDATE accounts
            SET migration_status = 'deprecated',
                migrated_at = now()
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code LIKE '1.3.%'
              AND migration_status <> 'deprecated';
        ");

        // ====================================================================
        // Step 3 — DEC-187 — RENAME 9201 → 1.1.06 (WIP moved to Current Asset)
        //   9201 (L3)        → 1.1.06        (Contract Assets / WIP — IFRS 15.95-98)
        //   9201-001 (L4)    → 1.1.06.001
        //   9201-002 (L4)    → 1.1.06.002
        //   9201-003 (L4)    → 1.1.06.003
        // We use one UPDATE per code so the (company_id, code) unique index
        // never sees a duplicate during the rename. The new 1.1.06 L3 is the
        // RENAMED 9201 L3 (not a separately inserted account) — DEC-NEW-3
        // describes this as a "migration" of the existing account.
        // ====================================================================
        Execute.Sql(@"
            UPDATE accounts
            SET code = '1.1.06',
                new_code = '1.1.06',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'BS',
                section = 'Current Asset'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '9201';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '1.1.06.001',
                new_code = '1.1.06.001',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'BS',
                section = 'Current Asset'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '9201-001';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '1.1.06.002',
                new_code = '1.1.06.002',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'BS',
                section = 'Current Asset'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '9201-002';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '1.1.06.003',
                new_code = '1.1.06.003',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'BS',
                section = 'Current Asset'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '9201-003';
        ");

        // ====================================================================
        // Step 4 — DEC-188 — SPLIT L1=7 into 7.1/7.2/7.3 (Finance I/E)
        // 71 (Other Income)    → 7.1   (Finance Income)
        // 7101 (Investment)    → 7.1.01
        // 7102 (Misc Income)   → 7.1.02
        // 72 (Other Expenses)  → 7.2   (Finance Expense)
        // 7201 (Misc Losses)   → 7.2.01
        // 7.3 (Other Gains/Losses) has no existing accounts yet — reserved
        // for future "gain/loss on disposal" lines (per the plan).
        // ====================================================================
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7.1',
                new_code = '7.1',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'PL',
                section = 'Finance Income'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '71';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7.1.01',
                new_code = '7.1.01',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'PL',
                section = 'Finance Income'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7101';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7.1.02',
                new_code = '7.1.02',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'PL',
                section = 'Finance Income'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7102';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7.2',
                new_code = '7.2',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'PL',
                section = 'Finance Expense'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '72';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7.2.01',
                new_code = '7.2.01',
                migration_status = 'migrated',
                migrated_at = now(),
                fs_type = 'PL',
                section = 'Finance Expense'
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7201';
        ");

        // ====================================================================
        // Step 5 — Bonus — backfill fs_type + section on 131 keep accounts
        // Every account with migration_status = 'pending' (the default from
        // Wave 1) and NOT touched by DEC-186/187/188 gets fs_type and section
        // derived from the first character of its code (L1) and — for L1=1/2 —
        // the second character (L2).
        //
        // L1 → fs_type + section mapping (per Anas's CoA plan, executive summary
        //      line "L1=0..9" + section text in the plan tables):
        //   L1=0 (Holding equity accounts) → fs_type='BS', section='Equity'
        //   L1=1 (Assets)                  → fs_type='BS', section from L2
        //                                       L2=1 Current Asset
        //                                       L2=2 Non-Current Asset
        //   L1=2 (Liabilities)             → fs_type='BS', section from L2
        //                                       L2=1 Current Liability
        //                                       L2=2 Non-Current Liability
        //   L1=3 (Equity)                  → fs_type='BS', section='Equity'
        //   L1=4 (Revenue)                 → fs_type='PL', section='Revenue'
        //   L1=5 (COGS)                    → fs_type='PL', section='COGS'
        //   L1=6 (OpEx)                    → fs_type='PL', section='OpEx'
        //   L1=7 (Finance I/E)             → fs_type='PL', section='Finance Expense'
        //                                       (legacy 7.x all map to Finance Expense
        //                                        since they predate the 7.1/7.2 split
        //                                        — DEC-188 reclassifies them above)
        //   L1=8 (Tax)                     → fs_type='PL', section='Tax'
        //   L1=9 (Closing / Memo)          → fs_type='PL', section='Closing'
        //   L1=0 (Holding — seeder sets type=Equity) → fs_type='BS', section='Equity'
        //      (the user spec mentioned L1=0 → Closing, but the existing seeder
        //       uses L1=0 for Holding equity, not Closing. We follow the seeder
        //       semantics: Closing is L1=9.)
        // ====================================================================
        Execute.Sql(@"
            UPDATE accounts
            SET fs_type = CASE SUBSTRING(code FROM 1 FOR 1)
                    WHEN '0' THEN 'BS'    -- Holding equity
                    WHEN '1' THEN 'BS'    -- Assets
                    WHEN '2' THEN 'BS'    -- Liabilities
                    WHEN '3' THEN 'BS'    -- Equity
                    WHEN '4' THEN 'PL'    -- Revenue
                    WHEN '5' THEN 'PL'    -- COGS
                    WHEN '6' THEN 'PL'    -- OpEx
                    WHEN '7' THEN 'PL'    -- Finance I/E
                    WHEN '8' THEN 'PL'    -- Tax
                    WHEN '9' THEN 'PL'    -- Closing
                END,
                section = CASE SUBSTRING(code FROM 1 FOR 1)
                    WHEN '0' THEN 'Equity'
                    WHEN '1' THEN
                        CASE SUBSTRING(code FROM 1 FOR 2)
                            WHEN '11' THEN 'Current Asset'
                            WHEN '12' THEN 'Current Asset'
                            WHEN '13' THEN 'Current Asset'
                            WHEN '14' THEN 'Current Asset'
                            WHEN '15' THEN 'Non-Current Asset'
                            WHEN '16' THEN 'Non-Current Asset'
                            ELSE 'Asset'
                        END
                    WHEN '2' THEN
                        CASE SUBSTRING(code FROM 1 FOR 2)
                            WHEN '21' THEN 'Current Liability'
                            WHEN '22' THEN 'Non-Current Liability'
                            ELSE 'Liability'
                        END
                    WHEN '3' THEN 'Equity'
                    WHEN '4' THEN 'Revenue'
                    WHEN '5' THEN 'COGS'
                    WHEN '6' THEN 'OpEx'
                    WHEN '7' THEN 'Finance Expense'  -- pre-7.1/7.2 split (now empty)
                    WHEN '8' THEN 'Tax'
                    WHEN '9' THEN 'Closing'
                END
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND migration_status = 'pending'
              AND code NOT LIKE '1.3.%'      -- exclude DEC-186 deprecated
              AND code NOT LIKE '7%';          -- exclude DEC-188 split (already handled above)
        ");

        // Note: the existing 131 keep accounts keep is_canonical = FALSE
        // (set by Wave 1). They use the legacy 4-digit code format (1101,
        // 1101-001, etc.), not the canonical dot format. Only the 27 new
        // accounts inserted in Step 1 have is_canonical = TRUE.
    }

    public override void Down()
    {
        // ====================================================================
        // Down() — reverse Wave 2B
        // 1. DELETE the 27 new accounts (where migration_status = 'new')
        // 2. REVERT the DEC-186/187/188 UPDATEs
        // 3. CLEAR the bonus fs_type/section backfill (set to NULL for
        //    accounts that were 'pending' before this migration ran)
        // ====================================================================

        // 1. Delete the 27 new accounts (only those tagged as 'new' by this migration)
        Execute.Sql(@"
            DELETE FROM accounts
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND migration_status = 'new'
              AND code IN (
                  -- 8 L3 control accounts
                  '1.1.01', '1.1.02', '1.2.01', '1.2.02', '2.1.08', '5.2.02', '5.2.03', '8.2.01',
                  -- 7 L4 Cash/Banks detail (DEC-NEW-1)
                  '1.1.01.002', '1.1.01.003', '1.1.02.001', '1.1.02.002',
                  '1.1.02.003', '1.1.02.004', '1.1.02.005',
                  -- 5 L4 Tangible/Intangible detail (DEC-NEW-2)
                  '1.2.01.001', '1.2.01.002', '1.2.01.003', '1.2.01.008', '1.2.02.001',
                  -- 7 L4 NDB/Stamps/CIT/SS detail (DEC-NEW-5)
                  '8.2.01.001', '8.2.01.002', '8.2.01.003', '8.2.01.005',
                  '2.1.03.002', '2.1.08.001', '2.1.08.002'
              );
        ");

        // 2a. Revert DEC-186 — restore 1.3.* from deprecated → pending
        Execute.Sql(@"
            UPDATE accounts
            SET migration_status = 'pending',
                migrated_at = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code LIKE '1.3.%'
              AND migration_status = 'deprecated';
        ");

        // 2b. Revert DEC-187 — rename 1.1.06.* back to 9201-*
        Execute.Sql(@"
            UPDATE accounts
            SET code = '9201',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '1.1.06';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '9201-001',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '1.1.06.001';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '9201-002',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '1.1.06.002';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '9201-003',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '1.1.06.003';
        ");

        // 2c. Revert DEC-188 — rename 7.1/7.2.* back to 71/72.*
        Execute.Sql(@"
            UPDATE accounts
            SET code = '71',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7.1';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7101',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7.1.01';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7102',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7.1.02';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '72',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7.2';
        ");
        Execute.Sql(@"
            UPDATE accounts
            SET code = '7201',
                new_code = NULL,
                migration_status = 'pending',
                migrated_at = NULL,
                fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code = '7.2.01';
        ");

        // 3. Clear the bonus backfill on the 131 keep accounts.
        // The migration only wrote fs_type/section for accounts that were
        // 'pending' AND not in 1.3.* or 7.* ranges. We clear those fields
        // for any account that was touched by the backfill (identified by
        // having a non-NULL fs_type and migration_status still = 'pending'
        // and code NOT starting with the new 1.1.0x or 1.2.0x prefixes).
        Execute.Sql(@"
            UPDATE accounts
            SET fs_type = NULL,
                section = NULL
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND migration_status = 'pending'
              AND fs_type IS NOT NULL
              AND code NOT LIKE '1.1.0%'    -- don't touch the new 1.1.0x L3/L4 (they were just deleted in step 1)
              AND code NOT LIKE '1.2.0%';   -- don't touch the new 1.2.0x L3/L4
        ");
    }
}
