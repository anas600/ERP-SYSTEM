'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, TrendingDown, TrendingUp } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency, formatPercent } from '@/lib/utils';

export default function BudgetVsActualPage() {
  const [from, setFrom] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10));
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.projectBudgetVsActual>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.projectBudgetVsActual(undefined, from, to);
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="📊 الميزانية مقابل الفعلي"
        description="Budget vs Actual — لكل المشاريع"
        actions={
          <Link href="/reports/projects">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">من تاريخ</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">إلى تاريخ</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div className="flex items-end">
            <Button onClick={load} variant="primary" disabled={loading}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </Card>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading || !report ? (
        <Card className="p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            <Card className="p-4 bg-blue-50">
              <div className="text-xs text-gray-500 mb-1">إجمالي الميزانية</div>
              <div className="text-2xl font-bold text-blue-700 font-mono">{formatCurrency(report.totalBudget)}</div>
            </Card>
            <Card className="p-4 bg-orange-50">
              <div className="text-xs text-gray-500 mb-1">إجمالي الفعلي</div>
              <div className="text-2xl font-bold text-orange-700 font-mono">{formatCurrency(report.totalActual)}</div>
            </Card>
            <Card className={`p-4 ${report.totalVariance >= 0 ? 'bg-green-50' : 'bg-red-50'}`}>
              <div className="text-xs text-gray-500 mb-1">الفرق (المتبقي)</div>
              <div className={`text-2xl font-bold font-mono ${report.totalVariance >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                {formatCurrency(report.totalVariance)}
              </div>
              <div className="text-xs mt-1">{formatPercent(report.totalVariancePercent)} من الميزانية</div>
            </Card>
          </div>

          <Card className="p-0 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">المشروع</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الميزانية</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الفعلي</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الفرق</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">% المستهلك</th>
                  </tr>
                </thead>
                <tbody>
                  {report.rows.map((row) => {
                    const pct = row.budget > 0 ? (row.actual / row.budget) * 100 : 0;
                    return (
                      <tr key={row.projectId} className="border-b border-gray-100 hover:bg-gray-50">
                        <td className="px-3 py-2 font-medium">{row.projectName}</td>
                        <td className="px-3 py-2 text-end font-mono">{formatCurrency(row.budget)}</td>
                        <td className="px-3 py-2 text-end font-mono">{formatCurrency(row.actual)}</td>
                        <td className="px-3 py-2 text-end font-mono">
                          <span className={row.variance >= 0 ? 'text-green-700' : 'text-red-700'}>
                            {row.variance >= 0 ? '↓ ' : '↑ '}{formatCurrency(Math.abs(row.variance))}
                          </span>
                        </td>
                        <td className="px-3 py-2 text-end">
                          <div className="flex items-center gap-2 justify-end">
                            {pct > 100 ? <TrendingUp className="h-3 w-3 text-red-600" /> : <TrendingDown className="h-3 w-3 text-green-600" />}
                            <span className={`font-mono text-xs font-semibold ${pct > 100 ? 'text-red-700' : pct > 80 ? 'text-orange-700' : 'text-green-700'}`}>
                              {formatPercent(pct)}
                            </span>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </Card>
        </>
      )}
    </div>
  );
}
