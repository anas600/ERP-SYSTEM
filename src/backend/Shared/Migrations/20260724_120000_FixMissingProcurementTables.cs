using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 020 — Fix missing procurement tables (DEC-080 reconciliation).
///
/// السبب الجذري:
///   Migration 008 (20260623_120000_CreateProcurementTables) كان NoOp بعد DEC-080
///   يفترض أن DataTypeMigrator (JSON) ينشئ الجداول. لكن
///   appsettings.json:Database.JsonMigrationEnabled = false في Fresh Build Mode
///   → الـ JSON migrator ما يـ run → الجداول ما تنوجد
///   → Migration 016 (20260710_120000_AddMissingIndexes) يفشل بـ 42P01 على
///     CREATE INDEX ON vendor_bills
///
/// الإصلاح:
///   - إنشاء الجداول الناقصة (vendor_bills + vendor_bill_lines) بـ CREATE TABLE IF NOT EXISTS
///   - idempotent على deploys موجودة (الجدول لو موجود، skip)
///   - يـ run بعد migration 008 (اللي خلّى الجداول ناقصة) ومباشرة بعده في الـ sequence
/// </summary>
[Migration(20260724_120000)]
public class FixMissingProcurementTables : Migration
{
    public override void Up()
    {
        // ============== vendor_bills ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS vendor_bills (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                bill_number varchar(50) NOT NULL,
                goods_receipt_id uuid NOT NULL,
                vendor_id uuid NOT NULL,
                status varchar(20) NOT NULL DEFAULT 'Draft',
                bill_date timestamptz NOT NULL DEFAULT now(),
                due_date timestamptz,
                currency varchar(3) NOT NULL DEFAULT 'LYD',
                sub_total numeric(18,4) NOT NULL DEFAULT 0,
                tax_amount numeric(18,4) NOT NULL DEFAULT 0,
                total_amount numeric(18,4) NOT NULL DEFAULT 0,
                notes text,
                journal_entry_id uuid,
                posted_at timestamptz,
                created_at timestamptz NOT NULL DEFAULT now(),
                created_by uuid NOT NULL,
                updated_at timestamptz NOT NULL DEFAULT now(),
                updated_by uuid,
                deleted_at timestamptz
            );");

        Execute.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_vbs_gr') THEN
                    ALTER TABLE vendor_bills
                        ADD CONSTRAINT fk_vbs_gr FOREIGN KEY (goods_receipt_id)
                        REFERENCES goods_receipts(id) ON DELETE RESTRICT;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_vbs_vendor') THEN
                    ALTER TABLE vendor_bills
                        ADD CONSTRAINT fk_vbs_vendor FOREIGN KEY (vendor_id)
                        REFERENCES vendors(id) ON DELETE RESTRICT;
                END IF;
            END $$;");

        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbs_tenant_bill_number ON vendor_bills (tenant_id, bill_number);");
        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbs_tenant_gr ON vendor_bills (tenant_id, goods_receipt_id);");
        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbs_tenant_vendor ON vendor_bills (tenant_id, vendor_id);");
        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbs_tenant_status ON vendor_bills (tenant_id, status);");

        // ============== vendor_bill_lines ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS vendor_bill_lines (
                id uuid PRIMARY KEY,
                tenant_id uuid NOT NULL,
                vendor_id uuid NOT NULL,
                vendor_bill_id uuid NOT NULL,
                item_id uuid NOT NULL,
                quantity numeric(18,4) NOT NULL DEFAULT 0,
                unit_price numeric(18,4) NOT NULL DEFAULT 0,
                tax_rate numeric(5,2) NOT NULL DEFAULT 0,
                sub_total numeric(18,4) NOT NULL DEFAULT 0,
                line_order integer NOT NULL DEFAULT 0
            );");

        Execute.Sql(@"
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_vbl_vb') THEN
                    ALTER TABLE vendor_bill_lines
                        ADD CONSTRAINT fk_vbl_vb FOREIGN KEY (vendor_bill_id)
                        REFERENCES vendor_bills(id) ON DELETE CASCADE;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_vbl_vendor') THEN
                    ALTER TABLE vendor_bill_lines
                        ADD CONSTRAINT fk_vbl_vendor FOREIGN KEY (vendor_id)
                        REFERENCES vendors(id) ON DELETE RESTRICT;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_vbl_item') THEN
                    ALTER TABLE vendor_bill_lines
                        ADD CONSTRAINT fk_vbl_item FOREIGN KEY (item_id)
                        REFERENCES items(id) ON DELETE RESTRICT;
                END IF;
            END $$;");

        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbl_tenant_vb ON vendor_bill_lines (tenant_id, vendor_bill_id);");
        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbl_tenant_item ON vendor_bill_lines (tenant_id, item_id);");
        Execute.Sql("CREATE INDEX IF NOT EXISTS ix_vbl_vb_order ON vendor_bill_lines (vendor_bill_id, line_order);");
    }

    public override void Down()
    {
        Execute.Sql("DROP TABLE IF EXISTS vendor_bill_lines CASCADE;");
        Execute.Sql("DROP TABLE IF EXISTS vendor_bills CASCADE;");
    }
}
