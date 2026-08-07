'use client';

// Sprint 49 — Sprint 38 (DEC-124): صفحة دفتر الأستاذ
// General Ledger per Account — كل الحركات على حساب معين بترتيب زمني + رصيد جاري
// Sprint 52: يقبل ?accountId=X من URL للدخول المباشر (drill-down من التقارير)

import { Suspense, useEffect, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { FileText, RefreshCw, AlertCircle, ArrowLeft } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { financeApi, Account, getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

function todayIso(): string { return new Date().toISOString().slice(0, 10); }
function firstOfYearIso(): string { const d = new Date(); return `${d.getFullYear()}-01-01`; }

function GeneralLedgerContent() {
  const searchParams = useSearchParams();
  const initialAccountId = searchParams.get('accountId') || '';
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [accountId, setAccountId] = useState<string>(initialAccountId);
  const [from, setFrom] = useState<string>(firstOfYearIso());
  const [to, setTo] = useState<string>(todayIso());
  const [report, setReport] = useState<any | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    financeApi.listAccounts().then(setAccounts).catch(() => setAccounts([]));
  }, []);

  // Sprint 52: Auto-load إذا تم تمرير accountId من URL
  useEffect(() => {
    if (initialAccountId && accounts.length > 0 && !report) {
      load();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialAccountId, accounts.length]);

  const load = async () => {
    if (!accountId) return;
    setLoading(true); setError(null);
    try {
      const r = await financeApi.getAccountLedger(accountId, from, to);
      setReport(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل دفتر الأستاذ.'));
    } finally { setLoading(false); }
  };

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="دفتر الأستاذ"
        description="الحركات المفصّلة على حساب معين بترتيب زمني مع رصيد جارٍ"
      />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col min-w-[240px]">
            <label className="text-xs text-gray-500 mb-1">الحساب</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">— اختر حساب —</option>
              {accounts.map((a) => (
                <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">من تاريخ</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">إلى تاريخ</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <Button variant="primary" onClick={load} disabled={!accountId}>عرض</Button>
          <Button variant="ghost" onClick={() => setReport(null)} iconLeft={<RefreshCw className="h-4 w-4" />}>مسح</Button>
        </div>
      </Card>

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2">
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" /><span>{error}</span>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        </div>
      ) : !report ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          اختر حساباً لعرض حركاته.
        </Card>
      ) : (
        <Card className="p-0 overflow-hidden">
          <div className="px-4 py-3 bg-gray-50 border-b">
            <h3 className="text-sm font-bold text-gray-800">{report.accountCode} — {report.accountName}</h3>
            <p className="text-xs text-gray-500">من {report.from || 'البداية'} إلى {report.to || 'اليوم'} • رصيد افتتاحي: {formatNumber(report.openingBalance)} LYD</p>
          </div>
          <table className="w-full text-sm" dir="rtl">
            <thead className="bg-white border-b border-gray-100">
              <tr>
                <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">التاريخ</th>
                <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">رقم القيد</th>
                <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">البيان</th>
                <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">مدين</th>
                <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">دائن</th>
                <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">رصيد جاري</th>
              </tr>
            </thead>
            <tbody>
              {(report.lines || []).map((l: any, i: number) => (
                <tr key={i} className="border-b border-gray-100">
                  <td className="px-3 py-2 text-xs">{l.entryDate?.slice(0, 10)}</td>
                  <td className="px-3 py-2 font-mono text-xs text-blue-600">{l.entryNumber}</td>
                  <td className="px-3 py-2 text-gray-800">{l.entryDescription}</td>
                  <td className="px-3 py-2 text-end font-mono text-blue-700">{l.debit > 0 ? formatNumber(l.debit) : '—'}</td>
                  <td className="px-3 py-2 text-end font-mono text-red-700">{l.credit > 0 ? formatNumber(l.credit) : '—'}</td>
                  <td className="px-3 py-2 text-end font-mono font-bold">{formatNumber(l.runningBalance)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot className="bg-gray-50">
              <tr>
                <td colSpan={3} className="px-3 py-2 text-start text-xs font-bold">الإجماليات</td>
                <td className="px-3 py-2 text-end font-mono font-bold text-blue-700">{formatNumber(report.totalDebit)}</td>
                <td className="px-3 py-2 text-end font-mono font-bold text-red-700">{formatNumber(report.totalCredit)}</td>
                <td className="px-3 py-2 text-end font-mono font-bold">رصيد ختامي: {formatNumber(report.closingBalance)}</td>
              </tr>
            </tfoot>
          </table>
        </Card>
      )}
    </div>
  );
}

export default function GeneralLedgerPage() {
  return (
    <Suspense fallback={
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل...</p>
      </div>
    }>
      <GeneralLedgerContent />
    </Suspense>
  );
}
