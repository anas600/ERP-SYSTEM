'use client';

// صفحة قائمة العملاء (Customers) — جدول + فلترة + زر Add

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Plus, Mail, Phone, MapPin, CreditCard } from 'lucide-react';
import { Button, Input, Table, Badge, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { arApi, Customer, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

export default function CustomersPage() {
  const { loading: authLoading } = useAuth();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await arApi.listCustomers();
      setCustomers(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل العملاء. تأكد أن الـ backend يعمل وأن endpoint /api/ar/customers جاهز.'));
    } finally {
      setLoading(false);
    }
  };

  const filtered = customers.filter(
    (c) => !filter || c.name.toLowerCase().includes(filter.toLowerCase()) || (c.code || '').toLowerCase().includes(filter.toLowerCase())
  );

  return (
    <div>
      <PageHeader
        title="👥 العملاء"
        description="قائمة العملاء المُسجَّلين في النظام"
        actions={
          <Link href="/finance/customers/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              عميل جديد
            </Button>
          </Link>
        }
      />

      <div className="mb-4">
        <Input
          placeholder="🔍 بحث (اسم / كود)..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          containerClassName="max-w-md"
        />
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      <Table
        columns={[
          {
            key: 'code',
            header: 'الكود',
            render: (c) => <span className="font-mono text-sm">{c.code}</span>,
          },
          {
            key: 'name',
            header: 'الاسم',
            render: (c) => (
              <div>
                <p className="font-semibold text-gray-800">{c.name}</p>
                {c.nameEn && <p className="text-xs text-gray-500">{c.nameEn}</p>}
                {c.taxId && <p className="text-xs text-gray-500 font-mono mt-0.5">TAX: {c.taxId}</p>}
              </div>
            ),
          },
          {
            key: 'contact',
            header: 'التواصل',
            render: (c) => (
              <div className="space-y-1 text-xs text-gray-600">
                {c.email && (
                  <div className="flex items-center gap-1">
                    <Mail className="h-3 w-3" />
                    <span>{c.email}</span>
                  </div>
                )}
                {c.phone && (
                  <div className="flex items-center gap-1">
                    <Phone className="h-3 w-3" />
                    <span dir="ltr">{c.phone}</span>
                  </div>
                )}
              </div>
            ),
          },
          {
            key: 'address',
            header: 'العنوان',
            render: (c) =>
              c.address ? (
                <div className="flex items-start gap-1 text-xs text-gray-600 max-w-[200px]">
                  <MapPin className="h-3 w-3 mt-0.5 flex-shrink-0" />
                  <span className="truncate">{c.address}</span>
                </div>
              ) : (
                <span className="text-xs text-gray-400">—</span>
              ),
          },
          {
            key: 'creditLimit',
            header: 'حد الائتمان',
            align: 'end',
            render: (c) =>
              c.creditLimit ? (
                <div className="flex items-center gap-1 text-xs">
                  <CreditCard className="h-3 w-3 text-gray-400" />
                  <span className="font-mono">{formatNumber(c.creditLimit)}</span>
                </div>
              ) : (
                <span className="text-xs text-gray-400">—</span>
              ),
          },
          {
            key: 'paymentTerms',
            header: 'شروط الدفع',
            align: 'center',
            render: (c) => <Badge variant="info">{c.paymentTermsDays} يوم</Badge>,
          },
          {
            key: 'isActive',
            header: 'الحالة',
            align: 'center',
            render: (c) => (c.isActive ? <Badge variant="success">نشط</Badge> : <Badge variant="neutral">غير نشط</Badge>),
          },
        ]}
        data={filtered}
        loading={loading}
        rowKey={(c) => c.id}
        emptyMessage="لا يوجد عملاء. ابدأ بإضافة عميل جديد."
      />

      {!loading && filtered.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">{filtered.length} عميل</p>
      )}
    </div>
  );
}
