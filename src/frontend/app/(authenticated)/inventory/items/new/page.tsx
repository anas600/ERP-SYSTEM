'use client';

// صفحة إنشاء منتج جديد (Item) — form

import { useState } from 'react';
import { useRouter } from 'next/navigation';
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
}

export default function NewItemPage() {
  const router = useRouter();
  useAuth();
  const [form, setForm] = useState<FormState>({
    sku: '',
    barcode: '',
    name: '',
    description: '',
    itemType: 'Stock',
    costingMethod: 'Average',
    unitOfMeasureId: '',
    averageCost: '0',
    reorderLevel: '0',
    reorderQuantity: '0',
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    setForm((f) => ({ ...f, [k]: e.target.value }));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const res = await fetch('/api/inventory/items', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...form,
          averageCost: Number(form.averageCost),
          reorderLevel: Number(form.reorderLevel),
          reorderQuantity: Number(form.reorderQuantity),
          isActive: true,
        }),
      });
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل إنشاء المنتج');
      }
      router.push('/inventory/items');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إنشاء المنتج. تأكد من البيانات وأن الـ backend يدعم الـ endpoint.'));
      setSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="➕ منتج جديد"
        description="أضف صنفاً جديداً إلى المخزون"
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'المخزون', href: '/inventory/items' },
          { label: 'المنتجات', href: '/inventory/items' },
          { label: 'جديد' },
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
          <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="SKU *"
              value={form.sku}
              onChange={onChange('sku')}
              required
              placeholder="مثال: ITEM-001"
            />
            <Input
              label="الباركود"
              value={form.barcode}
              onChange={onChange('barcode')}
              placeholder="اختياري"
            />
          </div>

          <Input
            label="اسم المنتج *"
            value={form.name}
            onChange={onChange('name')}
            required
            placeholder="مثال: حديد تسليح 12mm"
          />

          <Input
            label="الوصف"
            value={form.description}
            onChange={onChange('description')}
            placeholder="وصف تفصيلي (اختياري)"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="نوع المنتج *"
              value={form.itemType}
              onChange={onChange('itemType')}
              options={ITEM_TYPES}
            />
            <Select
              label="طريقة التكلفة *"
              value={form.costingMethod}
              onChange={onChange('costingMethod')}
              options={COSTING_METHODS}
            />
          </div>

          <Input
            label="وحدة القياس *"
            value={form.unitOfMeasureId}
            onChange={onChange('unitOfMeasureId')}
            required
            placeholder="مثال: kg, m, pcs"
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input
              label="متوسط التكلفة"
              type="number"
              step="0.01"
              value={form.averageCost}
              onChange={onChange('averageCost')}
            />
            <Input
              label="حد إعادة الطلب"
              type="number"
              value={form.reorderLevel}
              onChange={onChange('reorderLevel')}
            />
            <Input
              label="كمية إعادة الطلب"
              type="number"
              value={form.reorderQuantity}
              onChange={onChange('reorderQuantity')}
            />
          </div>

          <div className="flex items-center gap-2 pt-3 border-t">
            <Button
              type="submit"
              variant="primary"
              loading={submitting}
              iconLeft={<Save className="h-4 w-4" />}
            >
              حفظ المنتج
            </Button>
            <Link href="/inventory/items">
              <Button type="button" variant="ghost">
                إلغاء
              </Button>
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
}
