'use client';

import { useEffect, useState } from 'react';
import { ArrowRight, TrendingUp } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function IncomeStatementPage() {
  const [from, setFrom] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10));
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.incomeStatement>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.incomeStatement(from, to);
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
        title="📈 قائمة الدخل"
        description="Income Statement — الإيرادات - المصروفات = صافي الدخل"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>العودة</Button>
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
        <Card className="p-6">
          <div className="mb-4 text-sm text-gray-500">
            الفترة: {formatDate(report.from)} - {formatDate(report.to)}
          </div>
          <div className="space-y-3">
            <Row label="الإيرادات" value={report.revenue} type="income" />
            <Row label="تكلفة المبيعات" value={-report.cogs} type="expense" />
            <Row label="إجمالي الربح" value={report.revenue - report.cogs} type="subtotal" />
            <Row label="المصروفات التشغيلية" value={-report.operatingExpenses} type="expense" />
            <Row label="إيرادات أخرى" value={report.otherIncome} type="income" />
            <Row label="مصروفات أخرى" value={-report.otherExpenses} type="expense" />
            <div className="border-t-2 border-gray-300 pt-3">
              <Row label="صافي الدخل" value={report.netIncome} type="total" />
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}

function Row({ label, value, type }: { label: string; value: number; type: 'income' | 'expense' | 'subtotal' | 'total' }) {
  const color = type === 'total' ? (value >= 0 ? 'text-green-700' : 'text-red-700') : 'text-gray-800';
  const bg = type === 'total' ? 'bg-blue-50' : type === 'subtotal' ? 'bg-gray-50' : '';
  return (
    <div className={`flex justify-between items-center px-4 py-2 rounded ${bg}`}>
      <span className={`font-${type === 'total' ? 'bold' : 'medium'}`}>{label}</span>
      <span className={`font-mono font-bold text-lg ${color}`}>{formatCurrency(value)}</span>
    </div>
  );
}
