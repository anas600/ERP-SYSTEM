'use client';

import { useEffect, useCallback, useState } from 'react';
import { ArrowLeft, FileText } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function TrialBalancePage() {
  const [asOf, setAsOf] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.trialBalance>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);


  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.trialBalance(asOf);
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  }, [asOf]);
  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader
        title="📊 ميزان المراجعة"
        description="Trial Balance — رصيد كل حساب في تاريخ محدد"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">كما في تاريخ</label>
            <input type="date" value={asOf} onChange={(e) => setAsOf(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div className="flex items-end">
            <Button onClick={load} variant="primary" disabled={loading}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </Card>

      {error && <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading ? (
        <Card className="p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </Card>
      ) : !report || !report.rows || report.rows.length === 0 ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لا توجد بيانات في التاريخ المحدد.
        </Card>
      ) : (
        <Card className="p-0 overflow-hidden">
          <div className="px-4 py-3 border-b border-gray-200 bg-gray-50 flex justify-between items-center">
            <div className="text-sm">
              <span className="font-semibold">التاريخ:</span> {formatDate(report.asOfDate)} ·
              <span className="font-semibold mr-2">عدد الحسابات:</span> {report.rows.length}
            </div>
            <div className="text-sm">
              {report.isBalanced ? (
                <span className="text-green-600 font-semibold">✓ متوازن</span>
              ) : (
                <span className="text-red-600 font-semibold">✗ غير متوازن ({formatCurrency(report.totalDebit - report.totalCredit)})</span>
              )}
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الكود</th>
                  <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الحساب</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">مدين</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">دائن</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الصافي</th>
                </tr>
              </thead>
              <tbody>
                {report.rows.map((row) => (
                  <tr key={row.accountId} className="border-b border-gray-100 hover:bg-gray-50">
                    <td className="px-3 py-2 font-mono text-xs">{row.accountCode}</td>
                    <td className="px-3 py-2 font-medium">{row.accountName}</td>
                    <td className="px-3 py-2 text-end font-mono">{row.debit > 0 ? formatCurrency(row.debit) : '-'}</td>
                    <td className="px-3 py-2 text-end font-mono">{row.credit > 0 ? formatCurrency(row.credit) : '-'}</td>
                    <td className="px-3 py-2 text-end font-mono font-semibold">
                      {formatCurrency(Math.abs(row.debit - row.credit))}
                      <span className="text-xs text-gray-500 mr-1">
                        {row.debit > row.credit ? 'مدين' : 'دائن'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-100 border-t-2 border-gray-300">
                <tr className="font-bold">
                  <td colSpan={2} className="px-3 py-3 text-end">الإجمالي</td>
                  <td className="px-3 py-3 text-end font-mono text-blue-700">{formatCurrency(report.totalDebit)}</td>
                  <td className="px-3 py-3 text-end font-mono text-blue-700">{formatCurrency(report.totalCredit)}</td>
                  <td className="px-3 py-3 text-end font-mono text-blue-700">{formatCurrency(Math.abs(report.totalDebit - report.totalCredit))}</td>
                </tr>
              </tfoot>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
