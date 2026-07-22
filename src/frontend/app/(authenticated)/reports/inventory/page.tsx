'use client';

import { useEffect, useState } from 'react';
import { Package, AlertTriangle, TrendingDown, Calendar, ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Badge, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';

type ReportType = 'valuation' | 'low-stock' | 'movements' | 'aging';

interface InventoryRow {
  itemId?: string;
  itemCode?: string;
  itemName?: string;
  warehouseName?: string;
  quantity?: number;
  unitCost?: number;
  totalValue?: number;
  reorderPoint?: number;
  movementType?: string;
  movementDate?: string;
  ageDays?: number;
}

export default function InventoryReportsPage() {
  const { loading: authLoading } = useAuth();
  const [reportType, setReportType] = useState<ReportType>('valuation');
  const [rows, setRows] = useState<InventoryRow[]>([]);
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
      const data = await api.get<InventoryRow[] | { items: InventoryRow[] }>(`/api/reports/inventory/${reportType}`);
      const items = Array.isArray(data.data) ? data.data : (data.data as any).items || [];
      setRows(items);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  };

  const reportMeta: Record<ReportType, { title: string; en: string; icon: any; color: string }> = {
    'valuation': { title: 'تقييم المخزون', en: 'Inventory Valuation', icon: Package, color: 'blue' },
    'low-stock': { title: 'تنبيهات نقص المخزون', en: 'Low Stock Alerts', icon: AlertTriangle, color: 'red' },
    'movements': { title: 'حركات المخزون', en: 'Stock Movements', icon: TrendingDown, color: 'orange' },
    'aging': { title: 'تقادم المخزون', en: 'Stock Aging', icon: Calendar, color: 'purple' },
  };
  const current = reportMeta[reportType];

  const totalValue = rows.reduce((sum, r) => sum + (r.totalValue || 0), 0);

  return (
    <div>
      <PageHeader
        title={`📦 ${current.title}`}
        description={current.en}
        actions={
          <Link href="/reports">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>
              العودة للتقارير
            </Button>
          </Link>
        }
      />

      {/* Tabs */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1 overflow-x-auto">
        {(Object.keys(reportMeta) as ReportType[]).map((key) => {
          const r = reportMeta[key];
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

      {/* Stats */}
      {reportType === 'valuation' && rows.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-4">
          <Card accent="blue">
            <div className="text-sm text-gray-500">عدد الأصناف</div>
            <div className="text-2xl font-bold text-blue-600 mt-1">{rows.length}</div>
          </Card>
          <Card accent="green">
            <div className="text-sm text-gray-500">إجمالي القيمة</div>
            <div className="text-2xl font-bold text-green-600 mt-1">
              {totalValue.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
          </Card>
          <Card accent="yellow">
            <div className="text-sm text-gray-500">متوسط قيمة الصنف</div>
            <div className="text-2xl font-bold text-orange-600 mt-1">
              {(totalValue / Math.max(rows.length, 1)).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
          </Card>
        </div>
      )}

      {reportType === 'low-stock' && rows.length > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-4 mb-4">
          <div className="flex items-center gap-2 text-red-700">
            <AlertTriangle className="h-5 w-5" />
            <span className="font-semibold">تنبيه: {rows.length} صنف تحت حد الطلب</span>
          </div>
        </div>
      )}

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
      ) : rows.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          لا توجد بيانات.
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm p-4">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                {reportType === 'movements' ? (
                  <>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">التاريخ</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">النوع</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الصنف</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">المخزن</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الكمية</th>
                  </>
                ) : reportType === 'aging' ? (
                  <>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الصنف</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">المخزن</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">العمر (أيام)</th>
                  </>
                ) : (
                  <>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الكود</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الصنف</th>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">المخزن</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الكمية</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">القيمة</th>
                    {reportType === 'low-stock' && <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">حد الطلب</th>}
                  </>
                )}
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={i} className="border-b border-gray-100 last:border-0">
                  {reportType === 'movements' ? (
                    <>
                      <td className="px-3 py-2 text-xs">{r.movementDate ? formatDate(r.movementDate) : '—'}</td>
                      <td className="px-3 py-2"><Badge variant="info">{r.movementType}</Badge></td>
                      <td className="px-3 py-2">{r.itemName ?? r.itemCode ?? '—'}</td>
                      <td className="px-3 py-2">{r.warehouseName ?? '—'}</td>
                      <td className="px-3 py-2 text-end font-mono">{r.quantity?.toLocaleString() ?? '—'}</td>
                    </>
                  ) : reportType === 'aging' ? (
                    <>
                      <td className="px-3 py-2">{r.itemName ?? r.itemCode ?? '—'}</td>
                      <td className="px-3 py-2">{r.warehouseName ?? '—'}</td>
                      <td className="px-3 py-2 text-end font-mono">{r.ageDays ?? '—'}</td>
                    </>
                  ) : (
                    <>
                      <td className="px-3 py-2 font-mono text-xs">{r.itemCode ?? '—'}</td>
                      <td className="px-3 py-2 font-semibold">{r.itemName ?? '—'}</td>
                      <td className="px-3 py-2">{r.warehouseName ?? '—'}</td>
                      <td className="px-3 py-2 text-end font-mono">{r.quantity?.toLocaleString() ?? '—'}</td>
                      <td className="px-3 py-2 text-end font-mono font-semibold">
                        {r.totalValue?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                      {reportType === 'low-stock' && (
                        <td className="px-3 py-2 text-end font-mono text-red-600">{r.reorderPoint?.toLocaleString() ?? '—'}</td>
                      )}
                    </>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
