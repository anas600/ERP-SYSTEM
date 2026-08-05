'use client';

// صفحة تعديل المنتج (Item) — form مُعبَّأ مسبقاً
//
// الحقول: sku (readonly), name, description, categoryId, unitOfMeasureId, barcode, itemType,
//        costingMethod, averageCost, reorderLevel, reorderQuantity, isActive
// (الحقول تطابق Item DTO في api.ts — لا حقول غير معرَّفة كـ nameAr/costPrice/salePrice/taxRate/reorderPoint/preferredVendorId)

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { inventoryApi, getErrorMessage } from '@/lib/api';
import { useToast } from '@/lib/useToast';

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
  name: string;
  description: string;
  categoryId: string;
  unitOfMeasureId: string;
  barcode: string;
  itemType: string;
  costingMethod: string;
  averageCost: string;
  reorderLevel: string;
  reorderQuantity: string;
  isActive: boolean;
}

export default function EditItemPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const toast = useToast();
  const [form, setForm] = useState<FormState | null>(null);
  const [categories, setCategories] = useState<{ id: string; code: string; name: string; isActive: boolean }[]>([]);
  const [units, setUnits] = useState<{ id: string; code: string; name: string; isActive: boolean }[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [data, cats, uoms] = await Promise.all([
          inventoryApi.getItem(params.id),
          inventoryApi.listCategories().catch(() => []),
          inventoryApi.listUnitsOfMeasure().catch(() => []),
        ]);
        setForm({
          sku: data.sku || '',
          name: data.name || '',
          description: data.description || '',
          categoryId: data.categoryId || '',
          unitOfMeasureId: data.unitOfMeasureId || '',
          barcode: data.barcode || '',
          itemType: data.itemType || 'Stock',
          costingMethod: data.costingMethod || 'Average',
          averageCost: String(data.averageCost ?? 0),
          reorderLevel: String(data.reorderLevel ?? 0),
          reorderQuantity: String(data.reorderQuantity ?? 0),
          isActive: data.isActive ?? true,
        });
        setCategories(cats);
        setUnits(uoms);
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل تحميل المنتج.'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [params.id]);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    if (!form) return;
    setForm({ ...form, [k]: e.target.value });
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setError(null);
    setSubmitting(true);
    try {
      await inventoryApi.updateItem(params.id, {
        sku: form.sku,
        name: form.name,
        description: form.description || undefined,
        categoryId: form.categoryId || undefined,
        unitOfMeasureId: form.unitOfMeasureId,
        barcode: form.barcode || undefined,
        itemType: form.itemType,
        costingMethod: form.costingMethod,
        averageCost: Number(form.averageCost) || 0,
        reorderLevel: Number(form.reorderLevel) || 0,
        reorderQuantity: Number(form.reorderQuantity) || 0,
        isActive: form.isActive,
      });
      toast.success('تم حفظ التعديلات بنجاح.');
      router.push(`/inventory/items/${params.id}`);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل تحديث المنتج.');
      setError(msg);
      toast.error(msg);
      setSubmitting(false);
    }
  };

  const categoryOptions = [
    { label: '— بدون فئة —', value: '' },
    ...categories.filter((c) => c.isActive).map((c) => ({ label: `${c.code} — ${c.name}`, value: c.id })),
  ];

  const unitOptions = [
    { label: '— اختر وحدة —', value: '' },
    ...units.filter((u) => u.isActive).map((u) => ({ label: `${u.code} — ${u.name}`, value: u.id })),
  ];

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
          <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg text-sm">
            {error || 'المنتج غير موجود.'}
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
          { label: form.sku, href: `/inventory/items/${params.id}` },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href={`/inventory/items/${params.id}`}>
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
              إلغاء والعودة
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
              label="SKU"
              value={form.sku}
              readOnly
              hint="لا يمكن تغيير الكود"
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
          />

          <Input
            label="الوصف"
            value={form.description}
            onChange={onChange('description')}
            placeholder="وصف تفصيلي (اختياري)"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="الفئة"
              value={form.categoryId}
              onChange={onChange('categoryId')}
              options={categoryOptions}
            />
            <Select
              label="وحدة القياس *"
              value={form.unitOfMeasureId}
              onChange={onChange('unitOfMeasureId')}
              options={unitOptions}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="نوع المنتج"
              value={form.itemType}
              onChange={onChange('itemType')}
              options={ITEM_TYPES}
            />
            <Select
              label="طريقة التكلفة"
              value={form.costingMethod}
              onChange={onChange('costingMethod')}
              options={COSTING_METHODS}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input
              label="متوسط التكلفة"
              type="number"
              step="0.0001"
              value={form.averageCost}
              onChange={onChange('averageCost')}
              min={0}
            />
            <Input
              label="حد إعادة الطلب"
              type="number"
              value={form.reorderLevel}
              onChange={onChange('reorderLevel')}
              min={0}
            />
            <Input
              label="كمية إعادة الطلب"
              type="number"
              value={form.reorderQuantity}
              onChange={onChange('reorderQuantity')}
              min={0}
            />
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
            <Button
              type="submit"
              variant="primary"
              loading={submitting}
              iconLeft={<Save className="h-4 w-4" />}
            >
              حفظ التعديلات
            </Button>
            <Link href={`/inventory/items/${params.id}`}>
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
