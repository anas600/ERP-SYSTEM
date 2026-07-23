'use client';

// صفحة قائمة دورات الرواتب (Payroll Runs) — جدول + زر "دورة جديدة"
// الحالات: Draft → Processing → Posted | Cancelled

import { useEffect, useState } from 'react';
import { formatDate } from '@/lib/utils';
import Link from 'next/link';
import { Plus, Calendar, FileText, PlayCircle, CheckCircle2 } from 'lucide-react';
import { Button, Table, Badge, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import {
  hrApi,
  PayrollRun,
  PAYROLL_RUN_STATUSES,
  PAYROLL_RUN_STATUS_VARIANTS,
  getErrorMessage,
} from '@/lib/api';

export default function PayrollListPage() {
  const { loading: authLoading } = useAuth();
  const [runs, setRuns] = useState<PayrollRun[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, statusFilter]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const params = statusFilter ? { status: Number(statusFilter) } : undefined;
      const data = await hrApi.payroll.listPayrollRuns(params);
      setRuns(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل دورات الرواتب.'));
    } finally {
      setLoading(false);
    }
  };

  
  const formatMoney = (n: number) => n?.toLocaleString(undefined, { minimumFractionDigits: 2 }) || '0.00';

  return (
    <div>
      <PageHeader
        title="💰 Payroll"
        description="دورات الرواتب — إنشاء / معالجة / ترحيل"
        actions={
          <Link href="/hr/payroll/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              دورة جديدة
            </Button>
          </Link>
        }
      />

      {/* فلتر حسب الحالة */}
      <div className="mb-4 flex items-center gap-2 flex-wrap">
        <span className="text-sm text-gray-600">الحالة:</span>
        <button
          onClick={() => setStatusFilter('')}
          className={`text-xs px-3 py-1 rounded-full ${
            !statusFilter ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
          }`}
        >
          الكل
        </button>
        {Object.entries(PAYROLL_RUN_STATUSES).map(([k, v]) => (
          <button
            key={k}
            onClick={() => setStatusFilter(k)}
            className={`text-xs px-3 py-1 rounded-full ${
              statusFilter === k ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            {v}
          </button>
        ))}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      <Table
        columns={[
          {
            key: 'period',
            header: 'الفترة',
            render: (r) => (
              <div>
                <p className="font-semibold text-gray-800 flex items-center gap-1">
                  <Calendar className="h-3 w-3 text-gray-400" />
                  {formatDate(r.periodStart)} → {formatDate(r.periodEnd)}
                </p>
                <p className="text-[10px] text-gray-400 font-mono mt-0.5">{r.id.slice(0, 8)}</p>
              </div>
            ),
          },
          {
            key: 'status',
            header: 'الحالة',
            align: 'center',
            render: (r) => (
              <Badge variant={PAYROLL_RUN_STATUS_VARIANTS[r.status] || 'neutral'}>
                {r.status === 2 && <PlayCircle className="h-3 w-3 ml-1" />}
                {r.status === 3 && <CheckCircle2 className="h-3 w-3 ml-1" />}
                {PAYROLL_RUN_STATUSES[r.status] || r.status}
              </Badge>
            ),
          },
          {
            key: 'items',
            header: 'Payslips',
            align: 'center',
            render: (r) => (
              <span className="text-sm text-gray-700">
                {r.itemsCount ?? 0} <span className="text-xs text-gray-500">قسيمة</span>
              </span>
            ),
          },
          {
            key: 'totalGross',
            header: 'إجمالي Gross',
            align: 'end',
            render: (r) => (
              <span className="font-mono text-sm text-gray-700">{formatMoney(r.totalGross)}</span>
            ),
          },
          {
            key: 'totalNet',
            header: 'إجمالي Net',
            align: 'end',
            render: (r) => (
              <span className="font-mono text-sm font-bold text-green-700">{formatMoney(r.totalNet)}</span>
            ),
          },
          {
            key: 'createdAt',
            header: 'تاريخ الإنشاء',
            render: (r) => <span className="text-xs text-gray-600">{formatDate(r.createdAt)}</span>,
          },
          {
            key: 'notes',
            header: 'ملاحظات',
            render: (r) =>
              r.notes ? (
                <span className="text-xs text-gray-600 max-w-[180px] truncate inline-block">{r.notes}</span>
              ) : (
                <span className="text-gray-400 text-xs">—</span>
              ),
          },
          {
            key: 'actions',
            header: '',
            align: 'center',
            render: (r) => (
              <Link href={`/hr/payroll/${r.id}`}>
                <Button variant="ghost" size="sm" iconLeft={<FileText className="h-4 w-4" />}>
                  عرض
                </Button>
              </Link>
            ),
          },
        ]}
        data={runs}
        loading={loading}
        rowKey={(r) => r.id}
        emptyMessage="لا توجد دورات رواتب. أنشئ أول دورة."
      />

      {!loading && runs.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">{runs.length} دورة</p>
      )}
    </div>
  );
}