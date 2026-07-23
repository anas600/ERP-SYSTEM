'use client';

import { useEffect, useState } from 'react';
import { TrendingUp, FileText, BarChart3, DollarSign, ArrowLeft } from 'lucide-react';
import { PageHeader, Card, Badge, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import Link from 'next/link';

interface FinancialRow {
  accountCode: string;
  accountName: string;
  amount: number;
  category?: string;
}

interface FinancialReport {
  fromDate?: string;
  toDate?: string;
  rows: FinancialRow[];
  totals: Record<string, number>;
}

export default function FinancialReportsPage() {
  const { loading: authLoading } = useAuth();
  const [reportType, setReportType] = useState<'pl' | 'bs' | 'tb' | 'cf'>('pl');
  const [fromDate, setFromDate] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10));
  const [toDate, setToDate] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<FinancialReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, reportType]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const endpoint = reportType === 'pl' ? 'income-statement'
        : reportType === 'bs' ? 'balance-sheet'
        : reportType === 'tb' ? 'trial-balance'
        : 'cash-flow';
      const data = await api.get<FinancialReport>(`/api/reports/finance/${endpoint}`, {
        params: { fromDate, toDate }
      });
      setReport(data.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  };

  const reportNames = {
    pl: { title: 'قائمة الدخل', en: 'Profit & Loss Statement', icon: TrendingUp, color: 'green' },
    bs: { title: 'الميزانية العمومية', en: 'Balance Sheet', icon: BarChart3, color: 'blue' },
    tb: { title: 'ميزان المراجعة', en: 'Trial Balance', icon: FileText, color: 'purple' },
    cf: { title: 'التدفقات النقدية', en: 'Cash Flow', icon: DollarSign, color: 'orange' },
  };
  const current = reportNames[reportType];

  return (
    <div>
      <PageHeader
        title={`📊 ${current.title}`}
        description={current.en}
        actions={
          <Link href="/reports">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>
              العودة للتقارير
            </Button>
          </Link>
        }
      />

      {/* Report Type Tabs */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1 overflow-x-auto">
        {(Object.keys(reportNames) as Array<keyof typeof reportNames>).map((key) => {
          const r = reportNames[key];
          const Icon = r.icon;
          return (
            <button
              key={key}
              onClick={() => setReportType(key)}
              className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 whitespace-nowrap ${
                reportType === key ? `bg-${r.color}-500 text-white` : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              <Icon className="h-4 w-4" />
              {r.title}
            </button>
          );
        })}
      </div>

      {/* Date Filter */}
      <div className="bg-white rounded-xl shadow-sm p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">من تاريخ</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">إلى تاريخ</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div className="flex items-end">
            <Button onClick={load} variant="primary" disabled={loading}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : !report || !report.rows || report.rows.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          لا توجد بيانات للفترة المحددة.
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm p-6">
          <div className="mb-4 text-sm text-gray-500">
            الفترة: {formatDate(report.fromDate || fromDate)} - {formatDate(report.toDate || toDate)}
          </div>
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الكود</th>
                <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الحساب</th>
                {report.totals && Object.keys(report.totals).length > 0 && (
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">المبلغ</th>
                )}
              </tr>
            </thead>
            <tbody>
              {report.rows.map((row, i) => (
                <tr key={i} className="border-b border-gray-100 last:border-0">
                  <td className="px-3 py-2 font-mono text-xs text-gray-500">{row.accountCode}</td>
                  <td className="px-3 py-2 font-semibold">{row.accountName}</td>
                  {report.totals && Object.keys(report.totals).length > 0 && (
                    <td className="px-3 py-2 text-end font-mono">
                      {row.amount?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
            {report.totals && Object.keys(report.totals).length > 0 && (
              <tfoot className="bg-gray-50 border-t-2 border-gray-200">
                {Object.entries(report.totals).map(([k, v]) => (
                  <tr key={k}>
                    <td colSpan={2} className="px-3 py-2 text-end font-bold">{k}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold">
                      {v?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                  </tr>
                ))}
              </tfoot>
            )}
          </table>
        </div>
      )}
    </div>
  );
}
