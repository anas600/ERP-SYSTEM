'use client';

// صفحة إنشاء أمر شراء جديد (Purchase Order) — form مع lines

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save, Plus as PlusIcon, Trash2 } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import {
  procurementApi,
  inventoryApi,
  Vendor,
  Item,
  PurchaseOrder,
  PurchaseOrderLine,
  getErrorMessage,
} from '@/lib/api';

interface LineDraft {
  itemId: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
}

export default function NewPurchaseOrderPage() {
  const router = useRouter();
  useAuth();
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [vendorId, setVendorId] = useState('');
  const [orderDate, setOrderDate] = useState(new Date().toISOString().slice(0, 10));
  const [expectedDate, setExpectedDate] = useState('');
  const [currency, setCurrency] = useState('LYD');
  const [notes, setNotes] = useState('');
  const [lines, setLines] = useState<LineDraft[]>([{ itemId: '', quantity: 1, unitPrice: 0, taxRate: 0 }]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadingLookups, setLoadingLookups] = useState(true);

  useEffect(() => {
    loadLookups();
  }, []);

  const loadLookups = async () => {
    setLoadingLookups(true);
    try {
      const [v, i] = await Promise.allSettled([procurementApi.listVendors(), inventoryApi.listItems()]);
      if (v.status === 'fulfilled') setVendors(v.value);
      if (i.status === 'fulfilled') setItems(i.value);
    } finally {
      setLoadingLookups(false);
    }
  };

  const vendorOptions = vendors.map((v) => ({ label: v.name, value: v.id }));
  const itemOptions = items.map((i) => ({ label: `${i.sku} — ${i.name}`, value: i.id }));

  const updateLine = (idx: number, key: keyof LineDraft, value: string | number) => {
    setLines((prev) =>
      prev.map((l, i) => (i === idx ? { ...l, [key]: typeof value === 'string' ? (key === 'itemId' ? value : Number(value)) : value } : l))
    );
  };

  const addLine = () => setLines((prev) => [...prev, { itemId: '', quantity: 1, unitPrice: 0, taxRate: 0 }]);
  const removeLine = (idx: number) => setLines((prev) => prev.filter((_, i) => i !== idx));

  const subTotal = lines.reduce((sum, l) => sum + l.quantity * l.unitPrice, 0);
  const taxTotal = lines.reduce((sum, l) => sum + l.quantity * l.unitPrice * (l.taxRate / 100), 0);
  const grandTotal = subTotal + taxTotal;

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!vendorId) {
      setError('يجب اختيار المورّد.');
      return;
    }
    if (lines.length === 0 || lines.some((l) => !l.itemId || l.quantity <= 0)) {
      setError('كل بند يجب أن يحتوي على صنف وكمية صحيحة.');
      return;
    }

    setSubmitting(true);
    try {
      // تنسيق الـ lines بالشكل المتوقع من الـ backend
      const linesDto: PurchaseOrderLine[] = lines.map((l, i) => ({
        id: `temp-${i}`,
        itemId: l.itemId,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        taxRate: l.taxRate,
        subTotal: l.quantity * l.unitPrice,
      }));
      await procurementApi.createPO({
        vendorId,
        orderDate,
        expectedDate: expectedDate || undefined,
        currency,
        notes: notes || undefined,
        lines: linesDto,
      } as Partial<PurchaseOrder> & { vendorId: string; lines: PurchaseOrderLine[] });
      router.push('/procurement/purchase-orders');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إنشاء أمر الشراء.'));
      setSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="➕ أمر شراء جديد"
        description="أنشئ Purchase Order جديد"
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'أوامر الشراء', href: '/procurement/purchase-orders' },
          { label: 'جديد' },
        ]}
        actions={
          <Link href="/procurement/purchase-orders">
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
              رجوع
            </Button>
          </Link>
        }
      />

      <form onSubmit={onSubmit} className="space-y-4 max-w-4xl">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">{error}</div>
        )}

        <Card title="معلومات الأمر">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="المورّد *"
              value={vendorId}
              onChange={(e) => setVendorId(e.target.value)}
              options={vendorOptions}
              placeholder={loadingLookups ? 'جاري التحميل...' : 'اختر المورّد'}
              required
            />
            <Input
              label="العملة"
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              maxLength={3}
            />
            <Input
              type="date"
              label="تاريخ الطلب"
              value={orderDate}
              onChange={(e) => setOrderDate(e.target.value)}
              required
            />
            <Input
              type="date"
              label="تاريخ التوصيل المتوقع"
              value={expectedDate}
              onChange={(e) => setExpectedDate(e.target.value)}
            />
          </div>
          <div className="mt-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">ملاحظات</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
            />
          </div>
        </Card>

        <Card
          title="بنود الأمر (Lines)"
          actions={
            <Button
              type="button"
              variant="outline"
              size="sm"
              iconLeft={<PlusIcon className="h-3 w-3" />}
              onClick={addLine}
            >
              إضافة بند
            </Button>
          }
        >
          <div className="space-y-3">
            {lines.map((line, idx) => (
              <div
                key={idx}
                className="grid grid-cols-12 gap-2 items-end p-3 bg-gray-50 rounded-lg"
              >
                <div className="col-span-12 md:col-span-5">
                  <Select
                    label={idx === 0 ? 'الصنف' : undefined}
                    value={line.itemId}
                    onChange={(e) => updateLine(idx, 'itemId', e.target.value)}
                    options={itemOptions}
                    placeholder="اختر صنف"
                  />
                </div>
                <div className="col-span-4 md:col-span-2">
                  <Input
                    label={idx === 0 ? 'الكمية' : undefined}
                    type="number"
                    min={0.01}
                    step={0.01}
                    value={line.quantity}
                    onChange={(e) => updateLine(idx, 'quantity', e.target.value)}
                  />
                </div>
                <div className="col-span-4 md:col-span-2">
                  <Input
                    label={idx === 0 ? 'سعر الوحدة' : undefined}
                    type="number"
                    min={0}
                    step={0.01}
                    value={line.unitPrice}
                    onChange={(e) => updateLine(idx, 'unitPrice', e.target.value)}
                  />
                </div>
                <div className="col-span-3 md:col-span-2">
                  <Input
                    label={idx === 0 ? 'الضريبة %' : undefined}
                    type="number"
                    min={0}
                    step={0.1}
                    value={line.taxRate}
                    onChange={(e) => updateLine(idx, 'taxRate', e.target.value)}
                  />
                </div>
                <div className="col-span-1 flex justify-end">
                  <button
                    type="button"
                    onClick={() => removeLine(idx)}
                    disabled={lines.length === 1}
                    className="text-red-500 hover:text-red-700 p-1 disabled:opacity-30"
                    aria-label="حذف البند"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-4 pt-4 border-t flex justify-end">
            <div className="text-end text-sm space-y-1 w-64">
              <div className="flex justify-between">
                <span className="text-gray-500">المجموع الفرعي:</span>
                <span className="font-mono font-semibold">
                  {subTotal.toFixed(2)} {currency}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">الضريبة:</span>
                <span className="font-mono font-semibold">
                  {taxTotal.toFixed(2)} {currency}
                </span>
              </div>
              <div className="flex justify-between text-base font-bold pt-1 border-t">
                <span>الإجمالي:</span>
                <span className="text-blue-700">
                  {grandTotal.toFixed(2)} {currency}
                </span>
              </div>
            </div>
          </div>
        </Card>

        <div className="flex items-center gap-2">
          <Button
            type="submit"
            variant="primary"
            loading={submitting}
            iconLeft={<Save className="h-4 w-4" />}
          >
            حفظ الأمر
          </Button>
          <Link href="/procurement/purchase-orders">
            <Button type="button" variant="ghost">
              إلغاء
            </Button>
          </Link>
        </div>
      </form>
    </div>
  );
}
