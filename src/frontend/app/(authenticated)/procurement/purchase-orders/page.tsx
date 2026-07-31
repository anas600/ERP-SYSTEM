'use client';

// صفحة قائمة أوامر الشراء (Purchase Orders) — جدول مع status badges

import { useEffect, useState } from 'react';
import { formatDate, formatTime } from '@/lib/utils';
import Link from 'next/link';
import { Plus } from 'lucide-react';
import { Button, Table, Badge, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import {
  procurementApi,
  PurchaseOrder,
  PO_STATUSES,
  PO_STATUS_VARIANTS,
  getErrorMessage,
} from '@/lib/api';

export default function PurchaseOrdersPage() {
  const { loading: authLoading } = useAuth();
  const [pos, setPOs] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await procurementApi.listPOs();
      setPOs(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل أوامر الشراء.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="📄 أوامر الشراء"
        description="قائمة Purchase Orders (PO)"
        actions={
          <Link href="/procurement/purchase-orders/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              أمر شراء جديد
            </Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      <Table
        columns={[
          {
            key: 'poNumber',
            header: 'رقم الأمر',
            render: (p) => <span className="font-mono text-blue-600 font-semibold">{p.poNumber}</span>,
          },
          {
            key: 'vendor',
            header: 'المورّد',
            render: (p) => p.vendorName || <span className="text-gray-400 text-xs">{p.vendorId}</span>,
          },
          {
            key: 'orderDate',
            header: 'تاريخ الطلب',
            render: (p) => (
              <span className="text-sm text-gray-700">
                {formatDate(p.orderDate)}
              </span>
            ),
          },
          {
            key: 'expectedDate',
            header: 'تاريخ التوصيل المتوقع',
            render: (p) =>
              p.expectedDate ? (
                <span className="text-sm text-gray-700">
                  {formatDate(p.expectedDate)}
                </span>
              ) : (
                <span className="text-gray-400 text-xs">—</span>
              ),
          },
          {
            key: 'lines',
            header: 'عدد البنود',
            align: 'center',
            render: (p) => <Badge variant="neutral">{p.lines?.length || 0}</Badge>,
          },
          {
            key: 'total',
            header: 'الإجمالي',
            align: 'end',
            render: (p) => (
              <div className="text-end">
                <p className="font-bold text-gray-800">
                  {p.totalAmount?.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                </p>
                <p className="text-[10px] text-gray-500 font-mono">{p.currency}</p>
              </div>
            ),
          },
          {
            key: 'status',
            header: 'الحالة',
            render: (p) => (
              <Badge variant={PO_STATUS_VARIANTS[p.status] || 'neutral'}>
                {PO_STATUSES[p.status] || p.status}
              </Badge>
            ),
          },
        ]}
        data={pos}
        loading={loading}
        rowKey={(p) => p.id}
        emptyMessage="لا توجد أوامر شراء. أنشئ أول أمر شراء."
      />

      {!loading && pos.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">{pos.length} أمر شراء</p>
      )}
    </div>
  );
}


