'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, FileText, TrendingUp, TrendingDown } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatCurrency } from '@/lib/utils';

export default function CashFlowPage() {
  const [from, setFrom] = useState('2025-08-01');
  const [to, setTo] = useState('2026-07-26');
  const [report, setReport] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await reportsApi.cashFlow(from, to);
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally { setLoading(false); }
  };

  return (
    <div>
      <PageHeader
        title="💰 التدفقات النقدية"
        description="Cash Flow Statement — حركة النقدية خلال فترة"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">من تاريخ</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">إلى تاريخ</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div className="flex items-end">
            <Button onClick={load} variant="primary" disabled={loading}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </Card>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading ? (
        <Card className="p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </Card>
      ) : !report ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لا توجد بيانات في الفترة المحددة.
        </Card>
      ) : (
        <div className="space-y-3">
          <Card className="p-4">
            <h3 className="font-bold text-gray-800 mb-3 flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-green-600" />
              التدفقات الداخلة
            </h3>
            <div className="space-y-1">
              {(report.inflows || []).map((row: any, i: number) => (
                <div key={i} className="flex justify-between text-sm py-1 border-b border-gray-100 last:border-0">
                  <span className="text-gray-700">{row.category}</span>
                  <span className="font-mono text-green-700">{formatCurrency(row.amount)}</span>
                </div>
              ))}
              <div className="flex justify-between text-sm font-bold pt-2 border-t-2 border-gray-300">
                <span>إجمالي الداخل</span>
                <span className="font-mono text-green-700">{formatCurrency(report.totalInflows)}</span>
              </div>
            </div>
          </Card>

          <Card className="p-4">
            <h3 className="font-bold text-gray-800 mb-3 flex items-center gap-2">
              <TrendingDown className="h-5 w-5 text-red-600" />
              التدفقات الخارجة
            </h3>
            <div className="space-y-1">
              {(report.outflows || []).map((row: any, i: number) => (
                <div key={i} className="flex justify-between text-sm py-1 border-b border-gray-100 last:border-0">
                  <span className="text-gray-700">{row.category}</span>
                  <span className="font-mono text-red-700">{formatCurrency(row.amount)}</span>
                </div>
              ))}
              <div className="flex justify-between text-sm font-bold pt-2 border-t-2 border-gray-300">
                <span>إجمالي الخارج</span>
                <span className="font-mono text-red-700">{formatCurrency(report.totalOutflows)}</span>
              </div>
            </div>
          </Card>

          <Card className={`p-4 ${(report.netCashFlow || 0) >= 0 ? 'bg-green-50' : 'bg-red-50'}`}>
            <div className="flex justify-between items-center">
              <span className="font-bold text-lg">صافي التدفق النقدي</span>
              <span className={`font-mono font-bold text-2xl ${(report.netCashFlow || 0) >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                {formatCurrency(report.netCashFlow)}
              </span>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
