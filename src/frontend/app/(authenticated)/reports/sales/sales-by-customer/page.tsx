'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, TrendingUp } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function SalesByCustomerPage() {
  const [from, setFrom] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().slice(0, 10));
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10));
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.salesByCustomer>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.salesByCustomer(from, to);
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
        title="👥 المبيعات حسب العميل"
        description="Sales by Customer"
        actions={
          <Link href="/reports/sales">
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
        <Card className="p-0 overflow-hidden">
          <div className="px-4 py-3 border-b border-gray-200 bg-gray-50 flex justify-between items-center text-sm">
            <span>📅 {formatDate(report.from)} - {formatDate(report.to)}</span>
            <span className="font-bold text-blue-700">إجمالي: {formatCurrency(report.grandTotal)}</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الكود</th>
                  <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">العميل</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">عدد الفواتير</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">إجمالي</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">المدفوع</th>
                  <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">المستحق</th>
                </tr>
              </thead>
              <tbody>
                {report.rows.map((row) => (
                  <tr key={row.customerId} className="border-b border-gray-100 hover:bg-gray-50">
                    <td className="px-3 py-2 font-mono text-xs">{row.customerCode}</td>
                    <td className="px-3 py-2 font-medium">{row.customerName}</td>
                    <td className="px-3 py-2 text-end font-mono text-xs">{row.invoiceCount}</td>
                    <td className="px-3 py-2 text-end font-mono font-semibold">{formatCurrency(row.totalAmount)}</td>
                    <td className="px-3 py-2 text-end font-mono text-green-700">{formatCurrency(row.paidAmount)}</td>
                    <td className="px-3 py-2 text-end font-mono text-orange-700">{formatCurrency(row.outstanding)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
