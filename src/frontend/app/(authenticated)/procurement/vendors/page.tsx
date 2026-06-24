'use client';

// صفحة قائمة الموردين (Vendors) — جدول + فلترة + زر Add

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Plus, Mail, Phone, MapPin } from 'lucide-react';
import { Button, Input, Table, Badge, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { procurementApi, Vendor, PAYMENT_TERMS, getErrorMessage } from '@/lib/api';

export default function VendorsPage() {
  const { loading: authLoading } = useAuth();
  const [vendors, setVendors] = useState<Vendor[]>([]);
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
      const data = await procurementApi.listVendors();
      setVendors(data);
    } catch (e: unknown) {
      // في الـ dev: الـ backend قد لا يكون جاهز — نعرض رسالة واضحة
      setError(
        getErrorMessage(e, 'تعذّر تحميل الموردين. تأكد أن الـ backend يعمل وأن endpoint /api/procurement/vendors جاهز.')
      );
    } finally {
      setLoading(false);
    }
  };

  const filtered = vendors.filter(
    (v) => !filter || v.name.toLowerCase().includes(filter.toLowerCase()) || (v.email || '').toLowerCase().includes(filter.toLowerCase())
  );

  return (
    <div>
      <PageHeader
        title="🚚 الموردين"
        description="قائمة الموردين المُسجَّلين في النظام"
        actions={
          <Link href="/procurement/vendors/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              مورّد جديد
            </Button>
          </Link>
        }
      />

      <div className="mb-4">
        <Input
          placeholder="🔍 بحث (اسم / بريد)..."
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
            key: 'name',
            header: 'الاسم',
            render: (v) => (
              <div>
                <p className="font-semibold text-gray-800">{v.name}</p>
                {v.taxNumber && <p className="text-xs text-gray-500 font-mono mt-0.5">TAX: {v.taxNumber}</p>}
              </div>
            ),
          },
          {
            key: 'contact',
            header: 'التواصل',
            render: (v) => (
              <div className="space-y-1 text-xs text-gray-600">
                {v.email && (
                  <div className="flex items-center gap-1">
                    <Mail className="h-3 w-3" />
                    <span>{v.email}</span>
                  </div>
                )}
                {v.phone && (
                  <div className="flex items-center gap-1">
                    <Phone className="h-3 w-3" />
                    <span dir="ltr">{v.phone}</span>
                  </div>
                )}
              </div>
            ),
          },
          {
            key: 'address',
            header: 'العنوان',
            render: (v) =>
              v.address ? (
                <div className="flex items-start gap-1 text-xs text-gray-600 max-w-[200px]">
                  <MapPin className="h-3 w-3 mt-0.5 flex-shrink-0" />
                  <span className="truncate">{v.address}</span>
                </div>
              ) : (
                <span className="text-xs text-gray-400">—</span>
              ),
          },
          {
            key: 'paymentTerms',
            header: 'شروط الدفع',
            render: (v) => <Badge variant="info">{PAYMENT_TERMS[v.paymentTerms] || v.paymentTerms}</Badge>,
          },
          {
            key: 'currency',
            header: 'العملة',
            align: 'center',
            render: (v) => <span className="font-mono text-sm">{v.currency}</span>,
          },
          {
            key: 'isActive',
            header: 'الحالة',
            align: 'center',
            render: (v) => (v.isActive ? <Badge variant="success">نشط</Badge> : <Badge variant="neutral">غير نشط</Badge>),
          },
        ]}
        data={filtered}
        loading={loading}
        rowKey={(v) => v.id}
        emptyMessage="لا يوجد موردين. ابدأ بإضافة مورّد جديد."
      />

      {!loading && filtered.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">{filtered.length} مورّد</p>
      )}
    </div>
  );
}
