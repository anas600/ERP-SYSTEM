'use client';

import { useEffect, useState } from 'react';
import { ArrowRight, BarChart3 } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function BalanceSheetPage() {
  const [asOf, setAsOf] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.balanceSheet>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.balanceSheet(asOf);
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
        title="⚖️ الميزانية العمومية"
        description="Balance Sheet — الأصول = الالتزامات + حقوق الملكية"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>العودة</Button>
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

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading || !report ? (
        <Card className="p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </Card>
      ) : (
        <>
          <div className="mb-4 flex items-center justify-between text-sm text-gray-500">
            <span>📅 {formatDate(report.asOfDate)}</span>
            <span className={report.isBalanced ? 'text-green-600 font-semibold' : 'text-red-600 font-semibold'}>
              {report.isBalanced ? '✓ متوازنة' : `✗ فرق: ${formatCurrency(report.totalAssets - report.totalLiabilities - report.totalEquity)}`}
            </span>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card className="p-6 bg-blue-50">
              <div className="text-sm text-gray-500 mb-2">إجمالي الأصول</div>
              <div className="text-3xl font-bold text-blue-700 font-mono">{formatCurrency(report.totalAssets)}</div>
            </Card>
            <Card className="p-6 bg-red-50">
              <div className="text-sm text-gray-500 mb-2">إجمالي الالتزامات</div>
              <div className="text-3xl font-bold text-red-700 font-mono">{formatCurrency(report.totalLiabilities)}</div>
            </Card>
            <Card className="p-6 bg-green-50">
              <div className="text-sm text-gray-500 mb-2">حقوق الملكية</div>
              <div className="text-3xl font-bold text-green-700 font-mono">{formatCurrency(report.totalEquity)}</div>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
