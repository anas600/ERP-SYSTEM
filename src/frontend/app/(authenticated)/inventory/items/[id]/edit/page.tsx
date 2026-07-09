'use client';

// صفحة تعديل المنتج (Item) — form

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getErrorMessage } from '@/lib/api';

const ITEM_TYPES = [
  { label: 'منتج (Stock)', value: 'Stock' },
  { label: 'خدمة (Service)', value: 'Service' },
  { label: 'منتج متوسط (Average)', value: 'Average' },
];

const COSTING_METHODS = [
  { label: 'متوسط التكلفة (Average)', value: 'Average' },
  { label: 'FIFO', value: 'FIFO' },
  { label: 'LIFO', value: 'LIFO' },
];

interface FormState {
  sku: string;
  barcode: string;
  name: string;
  description: string;
  itemType: string;
  costingMethod: string;
  unitOfMeasureId: string;
  averageCost: string;
  reorderLevel: string;
  reorderQuantity: string;
  isActive: boolean;
}

export default function EditItemPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const [form, setForm] = useState<FormState | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const res = await fetch(`/api/inventory/items/${params.id}`);
        if (!res.ok) throw new Error('فشل تحميل المنتج');
        const data = await res.json();
        setForm({
          sku: data.sku || '',
          barcode: data.barcode || '',
          name: data.name || '',
          description: data.description || '',
          itemType: data.itemType || 'Stock',
          costingMethod: data.costingMethod || 'Average',
          unitOfMeasureId: data.unitOfMeasureId || '',
          averageCost: String(data.averageCost ?? 0),
          reorderLevel: String(data.reorderLevel ?? 0),
          reorderQuantity: String(data.reorderQuantity ?? 0),
          isActive: data.isActive ?? true,
        });
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل تحميل المنتج'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [params.id]);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    setForm((f) => (f ? { ...f, [k]: e.target.value } : f));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setError(null);
    setSubmitting(true);
    try {
      const res = await fetch(`/api/inventory/items/${params.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...form,
          averageCost: Number(form.averageCost),
          reorderLevel: Number(form.reorderLevel),
          reorderQuantity: Number(form.reorderQuantity),
        }),
      });
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل تحديث المنتج');
      }
      router.push('/inventory/items');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث المنتج.'));
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="✏️ تعديل منتج" />
        <Card className="max-w-2xl">
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        </Card>
      </div>
    );
  }

  if (!form) {
    return (
      <div>
        <PageHeader title="✏️ تعديل منتج" />
        <Card className="max-w-2xl">
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error || 'المنتج غير موجود'}
          </div>
          <div className="mt-4">
            <Link href="/inventory/items">
              <Button variant="ghost">رجوع للقائمة</Button>
            </Link>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="✏️ تعديل منتج"
        description={form.name}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'المخزون', href: '/inventory/items' },
          { label: 'المنتجات', href: '/inventory/items' },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href="/inventory/items">
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
              رجوع للقائمة
            </Button>
          </Link>
        }
      />

      <Card className="max-w-2xl">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input label="SKU *" value={form.sku} onChange={onChange('sku')} required />
            <Input label="الباركود" value={form.barcode} onChange={onChange('barcode')} />
          </div>

          <Input label="اسم المنتج *" value={form.name} onChange={onChange('name')} required />

          <Input label="الوصف" value={form.description} onChange={onChange('description')} />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select label="نوع المنتج" value={form.itemType} onChange={onChange('itemType')} options={ITEM_TYPES} />
            <Select label="طريقة التكلفة" value={form.costingMethod} onChange={onChange('costingMethod')} options={COSTING_METHODS} />
          </div>

          <Input label="وحدة القياس *" value={form.unitOfMeasureId} onChange={onChange('unitOfMeasureId')} required />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input label="متوسط التكلفة" type="number" step="0.01" value={form.averageCost} onChange={onChange('averageCost')} />
            <Input label="حد إعادة الطلب" type="number" value={form.reorderLevel} onChange={onChange('reorderLevel')} />
            <Input label="كمية إعادة الطلب" type="number" value={form.reorderQuantity} onChange={onChange('reorderQuantity')} />
          </div>

          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              className="rounded border-gray-300"
            />
            <span>فعّال</span>
          </label>

          <div className="flex items-center gap-2 pt-3 border-t">
            <Button type="submit" variant="primary" loading={submitting} iconLeft={<Save className="h-4 w-4" />}>
              حفظ التعديلات
            </Button>
            <Link href="/inventory/items">
              <Button type="button" variant="ghost">إلغاء</Button>
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
}
