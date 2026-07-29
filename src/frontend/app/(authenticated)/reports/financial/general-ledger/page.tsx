'use client';

import { useEffect, useState, useCallback } from 'react';
import { ArrowLeft, FileText } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, financeApi, Account, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function GeneralLedgerPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [accountId, setAccountId] = useState<string>('');
  const [from, setFrom] = useState('2025-08-01');
  const [to, setTo] = useState('2026-07-26');
  const [report, setReport] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    financeApi.listAccounts().then((a) => {
      setAccounts(a);
      setAccountId((current) => (current ? current : a[0]?.id ?? ''));
    });
  }, []);

  const load = useCallback(async () => {
    if (!accountId) return;
    setLoading(true); setError(null);
    try {
      const data = await reportsApi.generalLedger(accountId, from, to);
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally { setLoading(false); }
  }, [accountId, from, to]);

  useEffect(() => { if (accountId) load(); }, [accountId, load]);

  return (
    <div>
      <PageHeader
        title="📒 دفتر الأستاذ العام"
        description="General Ledger — حركات حساب محدد خلال فترة"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div className="md:col-span-2">
            <label className="text-xs text-gray-500 block mb-1">الحساب</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm">
              <option value="">اختر حساب</option>
              {accounts.map((a) => <option key={a.id} value={a.id}>{a.code} - {a.name}</option>)}
            </select>
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">من تاريخ</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">إلى تاريخ</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div className="md:col-span-4 flex justify-end">
            <Button onClick={load} variant="primary" disabled={loading || !accountId}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </Card>

      {error && <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading ? (
        <Card className="p-12 text-center text-gray-500">جاري التحميل...</Card>
      ) : !report ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          اختر حساب لعرض حركاته.
        </Card>
      ) : (
        <Card className="p-0 overflow-hidden">
          <div className="px-4 py-3 border-b border-gray-200 bg-gray-50 flex justify-between">
            <div className="text-sm">
              <span className="font-semibold">الحساب:</span> {report.accountCode} - {report.accountName}
            </div>
            <div className="text-sm">
              <span className="font-semibold">رصيد الإغلاق:</span>{' '}
              <span className={`font-mono font-bold ${(report.closingBalance || 0) >= 0 ? 'text-blue-700' : 'text-red-700'}`}>
                {formatCurrency(report.closingBalance)}
              </span>
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">التاريخ</th>
                  <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">البيان</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">مدين</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">دائن</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الرصيد</th>
                </tr>
              </thead>
              <tbody>
                <tr className="bg-gray-50 font-semibold">
                  <td colSpan={4} className="px-3 py-2">رصيد أول المدة</td>
                  <td className="px-3 py-2 text-end font-mono">{formatCurrency(report.openingBalance)}</td>
                </tr>
                {(report.lines || []).map((line: any, i: number) => (
                  <tr key={i} className="border-b border-gray-100 hover:bg-gray-50">
                    <td className="px-3 py-2 text-xs">{formatDate(line.date)}</td>
                    <td className="px-3 py-2">{line.description}</td>
                    <td className="px-3 py-2 text-end font-mono">{line.debit > 0 ? formatCurrency(line.debit) : '-'}</td>
                    <td className="px-3 py-2 text-end font-mono">{line.credit > 0 ? formatCurrency(line.credit) : '-'}</td>
                    <td className="px-3 py-2 text-end font-mono font-semibold">{formatCurrency(line.balance)}</td>
                  </tr>
                ))}
                <tr className="bg-gray-100 font-bold">
                  <td colSpan={2} className="px-3 py-3 text-end">الإجمالي</td>
                  <td className="px-3 py-3 text-end font-mono text-blue-700">{formatCurrency(report.totalDebit)}</td>
                  <td className="px-3 py-3 text-end font-mono text-blue-700">{formatCurrency(report.totalCredit)}</td>
                  <td className="px-3 py-3 text-end font-mono"></td>
                </tr>
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
