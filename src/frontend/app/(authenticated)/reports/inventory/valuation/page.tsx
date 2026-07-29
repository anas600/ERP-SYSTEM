'use client';

import { useEffect, useCallback, useState } from 'react';
import { ArrowLeft, Warehouse, Box } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatCurrency } from '@/lib/utils';

export default function InventoryValuationPage() {
  const [report, setReport] = useState<Awaited<ReturnType<typeof reportsApi.inventoryValuation>> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);


  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await reportsApi.inventoryValuation();
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  }, []);
  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader
        title="📦 تقييم المخزون"
        description="Inventory Valuation — قيمة كل صنف × الكمية الحالية"
        actions={
          <Link href="/reports/inventory">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      {error && <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading || !report ? (
        <Card className="p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            <Card className="p-6 bg-blue-50">
              <div className="flex items-center gap-3 mb-2">
                <Box className="h-5 w-5 text-blue-600" />
                <span className="text-sm text-gray-500">عدد الأصناف</span>
              </div>
              <div className="text-3xl font-bold text-blue-700 font-mono">{report.count}</div>
            </Card>
            <Card className="p-6 bg-green-50 md:col-span-2">
              <div className="flex items-center gap-3 mb-2">
                <Warehouse className="h-5 w-5 text-green-600" />
                <span className="text-sm text-gray-500">إجمالي قيمة المخزون</span>
              </div>
              <div className="text-4xl font-bold text-green-700 font-mono">{formatCurrency(report.totalValue)}</div>
            </Card>
          </div>

          <Card className="p-0 overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">SKU</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الصنف</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">المستودع</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الكمية</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">متوسط التكلفة</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">القيمة الإجمالية</th>
                  </tr>
                </thead>
                <tbody>
                  {report.items.slice(0, 50).map((item) => (
                    <tr key={`${item.itemId}-${item.warehouseId}`} className="border-b border-gray-100 hover:bg-gray-50">
                      <td className="px-3 py-2 font-mono text-xs">{item.itemSku}</td>
                      <td className="px-3 py-2 font-medium">{item.itemName}</td>
                      <td className="px-3 py-2 text-gray-600 text-xs">{item.warehouseName}</td>
                      <td className="px-3 py-2 text-end font-mono">{item.quantityOnHand.toLocaleString(undefined, { maximumFractionDigits: 2 })}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs text-gray-600">{formatCurrency(item.averageCost)}</td>
                      <td className="px-3 py-2 text-end font-mono font-semibold">{formatCurrency(item.totalValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {report.items.length > 50 && (
              <div className="p-3 text-center text-sm text-gray-500 bg-gray-50">
                يعرض أول 50 صنف من أصل {report.items.length}
              </div>
            )}
          </Card>
        </>
      )}
    </div>
  );
}
