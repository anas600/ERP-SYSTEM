'use client';

// صفحة إنشاء منتج جديد (Item) — Sprint 60 redesign
//
// Modern form layout:
//   1. PageHero with title + subtitle
//   2. SectionCard "المعلومات الأساسية" (sku, barcode, name, description)
//   3. SectionCard "التصنيف والقياس" (itemType, costingMethod, unitOfMeasureId SELECT)
//   4. SectionCard "التسعير وإعادة الطلب" (averageCost, reorderLevel, reorderQuantity)
//   5. Footer with save/cancel
//
// Backend: POST /api/inventory/items expects unitOfMeasureId as a Guid?
// (foreign key to units_of_measure.id). The dropdown is loaded from
// /api/inventory/uom and submits the selected Guid.

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, Save, Package, Tag, DollarSign, Boxes,
  Ruler, Barcode, FileText, Info, AlertTriangle,
} from 'lucide-react';
import {
  PageHero, SectionCard, StatusPill,
  Button, Input, Select, type SelectOption,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { authedFetch, getErrorMessage } from '@/lib/api';

const ITEM_TYPES: SelectOption[] = [
  { label: 'منتج (Stock)', value: 'Stock' },
  { label: 'خدمة (Service)', value: 'Service' },
  { label: 'منتج متوسط (Average)', value: 'Average' },
];

const COSTING_METHODS: SelectOption[] = [
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

interface UoM {
  id: string;
  code: string;
  name: string;
  symbol?: string;
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
  const [uoms, setUoms] = useState<UoM[]>([]);
  const [uomsLoading, setUomsLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load UoMs on mount
  useEffect(() => {
    const loadUoMs = async () => {
      try {
        const res = await authedFetch('/api/inventory/uom', { cache: 'no-store' });
        if (res.ok) {
          const data = await res.json();
          if (Array.isArray(data)) {
            setUoms(data);
            // Default selection: pcs (قطعة) if available
            const pcs = data.find((u: UoM) => u.code === 'pcs');
            if (pcs && !form.unitOfMeasureId) {
              setForm((f) => ({ ...f, unitOfMeasureId: pcs.id }));
            }
          }
        }
      } catch {
        // ignore — leave dropdown empty
      } finally {
        setUomsLoading(false);
      }
    };
    loadUoMs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
      // Send unitOfMeasureId only if selected (nullable in DTO)
      const payload: Record<string, unknown> = {
        sku: form.sku,
        barcode: form.barcode || undefined,
        name: form.name,
        description: form.description || undefined,
        itemType: form.itemType,
        costingMethod: form.costingMethod,
        averageCost: Number(form.averageCost),
        reorderLevel: Number(form.reorderLevel),
        reorderQuantity: Number(form.reorderQuantity),
        isActive: true,
      };
      if (form.unitOfMeasureId) {
        payload.unitOfMeasureId = form.unitOfMeasureId;
      }
      const res = await authedFetch('/api/inventory/items', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
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

  const uomOptions: SelectOption[] = [
    { label: '— بدون وحدة —', value: '' },
    ...uoms.map((u) => ({
      label: `${u.code} — ${u.name}${u.symbol && u.symbol !== u.code ? ` (${u.symbol})` : ''}`,
      value: u.id,
    })),
  ];

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المخزون"
        title="منتج جديد"
        subtitle="أضف صنفاً جديداً إلى المخزون. SKU فريد، متوسط التكلفة، وحدود إعادة الطلب."
        tone="emerald"
        actions={
          <Link href="/inventory/items">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة إلى المنتجات
            </Button>
          </Link>
        }
        highlight={
          form.sku
            ? { label: 'الكود', value: form.sku }
            : { label: 'الحالة', value: 'منتج جديد' }
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر إنشاء المنتج</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      <form onSubmit={onSubmit} className="space-y-6">
        {/* Section 1: Basic info */}
        <SectionCard
          title="المعلومات الأساسية"
          description="SKU، الباركود، اسم المنتج، والوصف"
          actions={
            <StatusPill tone="green" label="صنف جديد" showDot={false} />
          }
        >
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
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

          <div className="mt-4">
            <Input
              label="اسم المنتج *"
              value={form.name}
              onChange={onChange('name')}
              required
              placeholder="مثال: حديد تسليح 12mm"
            />
          </div>

          <div className="mt-4">
            <Input
              label="الوصف"
              value={form.description}
              onChange={onChange('description')}
              placeholder="وصف تفصيلي (اختياري)"
            />
          </div>
        </SectionCard>

        {/* Section 2: Type + Costing + Unit */}
        <SectionCard
          title="التصنيف والقياس"
          description="نوع المنتج، طريقة التكلفة، ووحدة القياس"
        >
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
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

          <div className="mt-4">
            <Select
              label="وحدة القياس *"
              value={form.unitOfMeasureId}
              onChange={onChange('unitOfMeasureId')}
              options={uomOptions}
              hint={
                uomsLoading
                  ? 'جاري تحميل الوحدات…'
                  : uoms.length === 0
                    ? 'لا توجد وحدات قياس معرّفة. أضف وحدة من /api/inventory/uom أولاً.'
                    : `${uoms.length} وحدة متاحة (kg, m, قطعة, ساعة, يوم...)`
              }
            />
          </div>
        </SectionCard>

        {/* Section 3: Cost + Reorder */}
        <SectionCard
          title="التسعير وإعادة الطلب"
          description="متوسط التكلفة، وحدود إعادة الطلب للمخزون"
        >
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <Input
              label="متوسط التكلفة (ل.د)"
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
              hint="عند الوصول لهذا الحد يظهر تحذير"
            />
            <Input
              label="كمية إعادة الطلب"
              type="number"
              value={form.reorderQuantity}
              onChange={onChange('reorderQuantity')}
            />
          </div>
        </SectionCard>

        {/* Actions footer */}
        <div className="flex flex-col gap-3 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-gray-200/70 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-xs text-gray-500">
            بعد الحفظ، يمكنك إضافة حركات المخزون وتسوية الكميات من صفحة المنتج.
          </p>
          <div className="flex items-center gap-2">
            <Link href="/inventory/items">
              <Button type="button" variant="ghost">
                إلغاء
              </Button>
            </Link>
            <Button
              type="submit"
              variant="primary"
              loading={submitting}
              iconLeft={<Save className="h-4 w-4" />}
            >
              {submitting ? 'جاري الحفظ…' : 'حفظ المنتج'}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
