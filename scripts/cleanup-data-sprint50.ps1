# Sprint 50 — تنظيف البيانات الموجودة قبل تطبيق Scenario الجديد
# يحذف: journal_lines, journal_entries, receipts, sales_invoices,
#        payment_allocations, payments, vendor_bills, goods_receipts, purchase_orders
# يحتفظ بـ: companies, accounts, customers, vendors, items, employees, projects, departments

$ErrorActionPreference = 'Stop'
$connStr = 'Host=localhost;Port=5432;Database=erp_system;Username=erp;Password=erp'

# استخدم Npgsql من مجلد dotnet للنزول إلى psql مع PowerShell
$psql = Get-Command psql -ErrorAction SilentlyContinue
if ($psql) {
    Write-Output '✅ Using psql'
} else {
    Write-Output '⚠️ psql not found, trying via Npgsql in dotnet-script style'
    # سأستخدم Npgsql من C# عبر dotnet script
}

# استعلام الحذف — ترتيب مهم (FK constraints)
$sql = @"
BEGIN;
TRUNCATE TABLE journal_lines RESTART IDENTITY CASCADE;
TRUNCATE TABLE journal_entries RESTART IDENTITY CASCADE;
TRUNCATE TABLE payment_allocations RESTART IDENTITY CASCADE;
TRUNCATE TABLE payments RESTART IDENTITY CASCADE;
TRUNCATE TABLE receipts RESTART IDENTITY CASCADE;
TRUNCATE TABLE sales_invoice_lines RESTART IDENTITY CASCADE;
TRUNCATE TABLE sales_invoices RESTART IDENTITY CASCADE;
TRUNCATE TABLE vendor_bill_lines RESTART IDENTITY CASCADE;
TRUNCATE TABLE vendor_bills RESTART IDENTITY CASCADE;
TRUNCATE TABLE goods_receipt_lines RESTART IDENTITY CASCADE;
TRUNCATE TABLE goods_receipts RESTART IDENTITY CASCADE;
TRUNCATE TABLE purchase_order_lines RESTART IDENTITY CASCADE;
TRUNCATE TABLE purchase_orders RESTART IDENTITY CASCADE;
TRUNCATE TABLE stock_movements RESTART IDENTITY CASCADE;
TRUNCATE TABLE stock_levels RESTART IDENTITY CASCADE;
TRUNCATE TABLE stock_reservations RESTART IDENTITY CASCADE;
TRUNCATE TABLE purchase_requests RESTART IDENTITY CASCADE;
COMMIT;
"@

# كتابة SQL إلى ملف مؤقت
$tmp = [System.IO.Path]::GetTempFileName() + '.sql'
[System.IO.File]::WriteAllText($tmp, $sql, [System.Text.Encoding]::UTF8)
Write-Output "📄 SQL written to: $tmp"
Write-Output ''
Write-Output '⚠️ This script does NOT execute psql — it only writes the SQL.'
Write-Output 'You need to run it manually with:'
Write-Output "  psql -h localhost -U erp -d erp_system -f $tmp"
Write-Output ''
Write-Output 'Or use the in-app cleanup: trigger the BE with the appropriate flag.'
