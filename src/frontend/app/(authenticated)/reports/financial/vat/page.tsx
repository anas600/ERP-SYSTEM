'use client';

import { useEffect, useState } from 'react';
import { ArrowRight, FileText, TrendingUp } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency, formatPercent } from '@/lib/utils';

export default function VatReportPage() {
  const [from, setFrom] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10));
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.vat>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.vat(from, to);
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
        title="🧾 تقرير ضريبة القيمة المضافة"
        description={`VAT Report — ليبيا ${formatPercent(0.15)} — المستحقة = المخرجات - المدخلات`}
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
        <>
          <div className="mb-4 text-sm text-gray-500">
            الفترة: {formatDate(report.from)} - {formatDate(report.to)}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card className="p-6 bg-green-50">
              <div className="text-sm text-gray-500 mb-2">ضريبة المخرجات (مبيعات)</div>
              <div className="text-2xl font-bold text-green-700 font-mono">{formatCurrency(report.outputVat)}</div>
              <div className="text-xs text-gray-500 mt-1">من مبيعات: {formatCurrency(report.totalSales)}</div>
            </Card>
            <Card className="p-6 bg-orange-50">
              <div className="text-sm text-gray-500 block mb-2">ضريبة المدخلات (مشتريات)</div>
              <div className="text-2xl font-bold text-orange-700 font-mono">{formatCurrency(report.inputVat)}</div>
              <div className="text-xs text-gray-500 mt-1">من مشتريات: {formatCurrency(report.totalPurchases)}</div>
            </Card>
            <Card className={`p-6 ${report.netVatPayable >= 0 ? 'bg-red-50' : 'bg-blue-50'}`}>
              <div className="text-sm text-gray-500 mb-2">
                {report.netVatPayable >= 0 ? 'مستحقة الدفع' : 'رصيد دائن (لصالحنا)'}
              </div>
              <div className={`text-2xl font-bold font-mono ${report.netVatPayable >= 0 ? 'text-red-700' : 'text-blue-700'}`}>
                {formatCurrency(Math.abs(report.netVatPayable))}
              </div>
              <div className="text-xs text-gray-500 mt-1">
                بمعدل {formatPercent(report.vatRate)}
              </div>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
