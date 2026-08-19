'use client';

// صفحة فواتير المبيعات (Sales Invoices) — جدول + فلترة بـ (العميل/الحالة/الفترة)

import { useEffect, useState, useMemo } from 'react';
import Link from 'next/link';
import { Plus, FileText } from 'lucide-react';
import { Button, Input, Select, Table, Badge, PageHeader, Card, EmptyState, SkeletonTable } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { arApi, SalesInvoice, Customer, SALES_INVOICE_STATUSES, SALES_INVOICE_STATUS_VARIANTS, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

export default function SalesInvoicesPage() {
  const { loading: authLoading } = useAuth();
  const [invoices, setInvoices] = useState<SalesInvoice[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterCustomer, setFilterCustomer] = useState<string>('');
  const [filterStatus, setFilterStatus] = useState<string>('');
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [data, customersData] = await Promise.all([
        arApi.listInvoices(),
        arApi.listCustomers(),
      ]);
      setInvoices(data);
      setCustomers(customersData);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الفواتير.'));
    } finally {
      setLoading(false);
    }
  };

  const filtered = useMemo(() => {
    return invoices.filter((inv) => {
      if (filterCustomer && inv.customerId !== filterCustomer) return false;
      if (filterStatus && inv.status !== Number(filterStatus)) return false;
      if (search) {
        const q = search.toLowerCase();
        if (!inv.invoiceNumber.toLowerCase().includes(q) && !(inv.customerName || '').toLowerCase().includes(q))
          return false;
      }
      return true;
    });
  }, [invoices, filterCustomer, filterStatus, search]);

  const customerOptions = useMemo(() => [
    { value: '', label: 'كل العملاء' },
    ...customers.map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` })),
  ], [customers]);

  const statusOptions = useMemo(() => [
    { value: '', label: 'كل الحالات' },
    ...Object.entries(SALES_INVOICE_STATUSES).map(([k, v]) => ({ value: k, label: v })),
  ], []);

  return (
    <div>
      <PageHeader
        title="📄 فواتير المبيعات"
        description="قائمة فواتير المبيعات المُسجَّلة"
        actions={
          <Link href="/finance/sales-invoices/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              فاتورة جديدة
            </Button>
          </Link>
        }
      />

      <Card className="mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <Input
            placeholder="🔍 بحث (رقم/عميل)..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            value={filterCustomer}
            onChange={(e) => setFilterCustomer(e.target.value)}
            options={customerOptions}
            placeholder="اختر العميل"
          />
          <Select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            options={statusOptions}
            placeholder="اختر الحالة"
          />
        </div>
      </Card>

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={5} cols={4} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<FileText className="h-12 w-12" />}
          title="لا توجد فواتير مبيعات"
          description={invoices.length === 0 ? 'ابدأ بإنشاء أول فاتورة مبيعات للعملاء.' : 'لا توجد فواتير تطابق الفلاتر الحالية.'}
          action={
            invoices.length === 0 ? (
              <Link href="/finance/sales-invoices/new">
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  فاتورة جديدة
                </Button>
              </Link>
            ) : (
              <Button variant="secondary" onClick={() => { setFilterCustomer(''); setFilterStatus(''); setSearch(''); }}>
                مسح الفلاتر
              </Button>
            )
          }
        />
      ) : (
        <>
          <Table
            columns={[
              {
                key: 'invoiceNumber',
                header: 'رقم الفاتورة',
                render: (inv) => (
                  <Link href={`/finance/sales-invoices/${inv.id}`} className="text-blue-600 hover:underline font-mono font-semibold">
                    {inv.invoiceNumber}
                  </Link>
                ),
              },
              {
                key: 'customer',
                header: 'العميل',
                render: (inv) => <span className="font-semibold text-gray-800">{inv.customerName || '—'}</span>,
              },
              {
                key: 'invoiceDate',
                header: 'التاريخ',
                render: (inv) => <span className="text-sm text-gray-600">{formatDate(inv.invoiceDate)}</span>,
              },
              {
                key: 'dueDate',
                header: 'الاستحقاق',
                render: (inv) => <span className="text-sm text-gray-600">{formatDate(inv.dueDate)}</span>,
              },
              {
                key: 'total',
                header: 'الإجمالي',
                align: 'end',
                render: (inv) => <span className="font-mono font-semibold">{formatNumber(inv.totalAmount)}</span>,
              },
              {
                key: 'outstanding',
                header: 'المتبقي',
                align: 'end',
                render: (inv) => (
                  <span className={`font-mono font-semibold ${inv.outstanding > 0 ? 'text-red-600' : 'text-green-600'}`}>
                    {formatNumber(inv.outstanding)}
                  </span>
                ),
              },
              {
                key: 'status',
                header: 'الحالة',
                align: 'center',
                render: (inv) => (
                  <Badge variant={SALES_INVOICE_STATUS_VARIANTS[inv.status] || 'neutral'}>
                    {SALES_INVOICE_STATUSES[inv.status] || '—'}
                  </Badge>
                ),
              },
            ]}
            data={filtered}
            rowKey={(inv) => inv.id}
            emptyMessage="لا توجد فواتير. ابدأ بإنشاء فاتورة جديدة."
          />
          <p className="mt-3 text-xs text-gray-500 text-start">
            {filtered.length} فاتورة • إجمالي: <span className="font-mono font-semibold">{formatNumber(filtered.reduce((s, i) => s + i.totalAmount, 0))}</span>
          </p>
        </>
      )}
    </div>
  );
}
