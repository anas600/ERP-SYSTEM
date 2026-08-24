using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 60 — Wave 1 (DEC-NEW-14 + DEC-NEW-15) — Foundation data seed.
///
/// <para><b>Why</b>: per Anas's CoA-Final-Proposal-2026-08-24, the holding company needs
/// a stable set of foundation data (cost centers + projects) before Wave 2 can use them.</para>
///
/// <para><b>DEC-NEW-14 — 4 cost centers</b> for the default holding company:</para>
/// <list type="bullet">
///   <item><c>CC-CONSTR</c> — قسم المقاولات (Construction Division)</item>
///   <item><c>CC-REST</c>  — قسم المطاعم (Restaurant Division)</item>
///   <item><c>CC-ADMIN</c> — الإدارة (Admin / Shared)</item>
///   <item><c>CC-WORKSHOP</c> — الورشة (Workshop, optional)</item>
/// </list>
///
/// <para><b>DEC-NEW-15 — 5 new projects</b> for the default holding company (3 existing
/// PRJ-2026-* projects from Sprint 58c stay untouched, total becomes 3 + 5 = 8):</para>
/// <list type="bullet">
///   <item><c>REST-2026-001</c> — مطعم الأسماك (NDB seafood contract) — Active</item>
///   <item><c>REST-2026-002</c> — خدمات الإعاشة (Catering contract) — Planning</item>
///   <item><c>ADMN-2026-001</c> — ترقية نظام ERP (ERP upgrade internal) — Active</item>
///   <item><c>TRNG-2026-001</c> — تدريب الموظفين (Staff training) — Planning</item>
///   <item><c>YRCL-2026-001</c> — إقفال السنة المالية (Year-end closing) — Planning</item>
/// </list>
///
/// <para><b>Idempotency</b>: every INSERT uses <c>ON CONFLICT (company_id, code) DO NOTHING</c>
/// (the UNIQUE constraint on both tables). Re-running this migration is a safe no-op.</para>
///
/// <para><b>Default company</b>: resolved via <c>companies.code = '000'</c> (the constitutional
/// holding marker per CONSTITUTION.md §3.2). For the <c>created_by</c> audit field on projects,
/// the migration picks the first active user; if no user exists, it falls back to a deterministic
/// placeholder UUID so the INSERT does not violate the NOT NULL constraint.</para>
///
/// <para><b>Down()</b>: removes the 4 cost centers and 5 projects by code. Existing data from
/// other waves (PRJ-2026-001/002/003 from Sprint 58c) is NOT touched.</para>
/// </summary>
[Migration(20260825_002)]
public class Sprint60_FoundationDataSeed : Migration
{
    public override void Up()
    {
        // ====================================================================
        // DEC-NEW-14 — 4 cost centers (idempotent via ON CONFLICT)
        // Type 2 = Department (per CostCenterType enum: Project=1, Department=2, ...)
        // ====================================================================
        Execute.Sql(@"
            INSERT INTO cost_centers (id, company_id, code, name, type, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'CC-CONSTR', 'قسم المقاولات', 2, true, now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO cost_centers (id, company_id, code, name, type, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'CC-REST', 'قسم المطاعم', 2, true, now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO cost_centers (id, company_id, code, name, type, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'CC-ADMIN', 'الإدارة', 2, true, now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO cost_centers (id, company_id, code, name, type, is_active, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'CC-WORKSHOP', 'الورشة', 2, true, now(), now()
            FROM companies c WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");

        // ====================================================================
        // DEC-NEW-15 — 5 new projects (idempotent via ON CONFLICT)
        // cost_center_id is looked up by (company_id, code) so we don't hardcode UUIDs.
        // status: 1=Planning, 2=Active (per ProjectStatus enum)
        // created_by: first active user; if none, fall back to a deterministic placeholder
        //             (matches the 'admin@alfajr.local' user from DefaultHoldingBootstrap).
        // ====================================================================
        Execute.Sql(@"
            INSERT INTO projects (id, company_id, code, name, cost_center_id, status, budget,
                                  start_date, end_date, is_active, created_by, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'REST-2026-001', 'مطعم الأسماك - عقد NDB',
                   cc.id, 2, 0, '2026-09-01', '2026-12-31', true,
                   COALESCE((SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1),
                            '00000000-0000-0000-0000-000000000002'::uuid),
                   now(), now()
            FROM companies c
            JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-REST'
            WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO projects (id, company_id, code, name, cost_center_id, status, budget,
                                  start_date, end_date, is_active, created_by, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'REST-2026-002', 'خدمات الإعاشة - عقد catering',
                   cc.id, 1, 0, '2026-09-15', '2027-03-31', true,
                   COALESCE((SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1),
                            '00000000-0000-0000-0000-000000000002'::uuid),
                   now(), now()
            FROM companies c
            JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-REST'
            WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO projects (id, company_id, code, name, cost_center_id, status, budget,
                                  start_date, end_date, is_active, created_by, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'ADMN-2026-001', 'ترقية نظام ERP - مشروع داخلي',
                   cc.id, 2, 0, '2026-09-01', '2026-11-30', true,
                   COALESCE((SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1),
                            '00000000-0000-0000-0000-000000000002'::uuid),
                   now(), now()
            FROM companies c
            JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-ADMIN'
            WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO projects (id, company_id, code, name, cost_center_id, status, budget,
                                  start_date, end_date, is_active, created_by, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'TRNG-2026-001', 'تدريب الموظفين - برنامج Q4',
                   cc.id, 1, 0, '2026-10-01', '2026-12-15', true,
                   COALESCE((SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1),
                            '00000000-0000-0000-0000-000000000002'::uuid),
                   now(), now()
            FROM companies c
            JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-ADMIN'
            WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
        Execute.Sql(@"
            INSERT INTO projects (id, company_id, code, name, cost_center_id, status, budget,
                                  start_date, end_date, is_active, created_by, created_at, updated_at)
            SELECT gen_random_uuid(), c.id, 'YRCL-2026-001', 'إقفال السنة المالية 2026',
                   cc.id, 1, 0, '2026-12-01', '2026-12-31', true,
                   COALESCE((SELECT id FROM users WHERE is_active = true ORDER BY created_at LIMIT 1),
                            '00000000-0000-0000-0000-000000000002'::uuid),
                   now(), now()
            FROM companies c
            JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-ADMIN'
            WHERE c.code = '000' AND c.is_group = true
            ON CONFLICT (company_id, code) DO NOTHING;
        ");
    }

    public override void Down()
    {
        // Remove only the 5 new projects (the 3 Sprint 58c projects PRJ-2026-* are not touched).
        Execute.Sql(@"
            DELETE FROM projects
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code IN ('REST-2026-001', 'REST-2026-002', 'ADMN-2026-001',
                           'TRNG-2026-001', 'YRCL-2026-001');
        ");

        // Remove the 4 new cost centers.
        Execute.Sql(@"
            DELETE FROM cost_centers
            WHERE company_id = (SELECT id FROM companies WHERE code = '000' AND is_group = true LIMIT 1)
              AND code IN ('CC-CONSTR', 'CC-REST', 'CC-ADMIN', 'CC-WORKSHOP');
        ");
    }
}
