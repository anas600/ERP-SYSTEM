'use client';

// صفحة تعديل المورّد (Vendor) — form مُعبَّأ مسبقاً
//
// الحقول: name, email, phone, address, taxNumber, currency, paymentTerms, isActive
// (الحقول تطابق Vendor DTO في api.ts — لا حقول غير معرَّفة كـ code/nameAr/bankAccount/notes)

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { procurementApi, PAYMENT_TERMS, getErrorMessage } from '@/lib/api';
import { useToast } from '@/lib/useToast';

interface FormState {
  name: string;
  email: string;
  phone: string;
  address: string;
  taxNumber: string;
  currency: string;
  paymentTerms: string;
  isActive: boolean;
}

const CURRENCY_OPTIONS = [
  { label: 'دينار ليبي (LYD)', value: 'LYD' },
  { label: 'دولار أمريكي (USD)', value: 'USD' },
  { label: 'يورو (EUR)', value: 'EUR' },
  { label: 'جنيه مصري (EGP)', value: 'EGP' },
  { label: 'ريال سعودي (SAR)', value: 'SAR' },
  { label: 'درهم إماراتي (AED)', value: 'AED' },
];

const PAYMENT_TERMS_OPTIONS = Object.entries(PAYMENT_TERMS).map(([k, v]) => ({ label: v, value: k }));

export default function EditVendorPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const toast = useToast();
  const [form, setForm] = useState<FormState | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const data = await procurementApi.getVendor(params.id);
        setForm({
          name: data.name || '',
          email: data.email || '',
          phone: data.phone || '',
          address: data.address || '',
          taxNumber: data.taxNumber || '',
          currency: data.currency || 'LYD',
          paymentTerms: data.paymentTerms || 'Net30',
          isActive: data.isActive ?? true,
        });
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل تحميل المورّد.'));
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
      await procurementApi.updateVendor(params.id, {
        name: form.name,
        email: form.email || undefined,
        phone: form.phone || undefined,
        address: form.address || undefined,
        taxNumber: form.taxNumber || undefined,
        currency: form.currency,
        paymentTerms: form.paymentTerms,
        isActive: form.isActive,
      });
      toast.success('تم حفظ التعديلات بنجاح.');
      router.push('/procurement/vendors');
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل تحديث المورّد.');
      setError(msg);
      toast.error(msg);
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="✏️ تعديل مورّد" />
        <Card className="max-w-2xl">
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        </Card>
      </div>
    );
  }

  if (!form) {
    return (
      <div>
        <PageHeader title="✏️ تعديل مورّد" />
        <Card className="max-w-2xl">
          <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg text-sm">
            {error || 'المورّد غير موجود.'}
          </div>
          <div className="mt-4">
            <Link href="/procurement/vendors">
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
        title="✏️ تعديل مورّد"
        description={form.name}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'المشتريات', href: '/procurement/vendors' },
          { label: 'الموردين', href: '/procurement/vendors' },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href="/procurement/vendors">
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
          <Input
            label="اسم المورّد *"
            value={form.name}
            onChange={onChange('name')}
            required
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="البريد الإلكتروني"
              type="email"
              value={form.email}
              onChange={onChange('email')}
            />
            <Input
              label="الهاتف"
              type="tel"
              value={form.phone}
              onChange={onChange('phone')}
              placeholder="+218 91 234 5678"
            />
          </div>

          <Input
            label="العنوان"
            value={form.address}
            onChange={onChange('address')}
            placeholder="العنوان الكامل"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="الرقم الضريبي"
              value={form.taxNumber}
              onChange={onChange('taxNumber')}
              placeholder="اختياري"
            />
            <Select
              label="العملة"
              value={form.currency}
              onChange={onChange('currency')}
              options={CURRENCY_OPTIONS}
            />
          </div>

          <Select
            label="شروط الدفع"
            value={form.paymentTerms}
            onChange={onChange('paymentTerms')}
            options={PAYMENT_TERMS_OPTIONS}
          />

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
            <Link href="/procurement/vendors">
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
