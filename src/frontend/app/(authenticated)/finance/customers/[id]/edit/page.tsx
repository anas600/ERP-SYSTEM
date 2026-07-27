'use client';

// صفحة تعديل العميل (Customer) — form مُعبَّأ مسبقاً
//
// الحقول: code (readonly), name, nameEn, taxId, email, phone, address, creditLimit, paymentTermsDays, isActive
// (الحقول تطابق Customer DTO في api.ts — لا حقول غير معرَّفة كـ nameAr/taxNumber/paymentTerms/city/country/currency/notes)

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save } from 'lucide-react';
import { Button, Input, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { arApi, getErrorMessage } from '@/lib/api';
import { useToast } from '@/lib/useToast';

interface FormState {
  code: string;
  name: string;
  nameEn: string;
  taxId: string;
  email: string;
  phone: string;
  address: string;
  creditLimit: string;
  paymentTermsDays: string;
  isActive: boolean;
}

const EMPTY: FormState = {
  code: '',
  name: '',
  nameEn: '',
  taxId: '',
  email: '',
  phone: '',
  address: '',
  creditLimit: '',
  paymentTermsDays: '30',
  isActive: true,
};

export default function EditCustomerPage() {
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
        const data = await arApi.getCustomer(params.id);
        setForm({
          code: data.code || '',
          name: data.name || '',
          nameEn: data.nameEn || '',
          taxId: data.taxId || '',
          email: data.email || '',
          phone: data.phone || '',
          address: data.address || '',
          creditLimit: data.creditLimit !== undefined && data.creditLimit !== null ? String(data.creditLimit) : '',
          paymentTermsDays: String(data.paymentTermsDays ?? 30),
          isActive: data.isActive ?? true,
        });
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل تحميل العميل.'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [params.id]);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement>
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
      await arApi.updateCustomer(params.id, {
        code: form.code,
        name: form.name,
        nameEn: form.nameEn || undefined,
        taxId: form.taxId || undefined,
        email: form.email || undefined,
        phone: form.phone || undefined,
        address: form.address || undefined,
        creditLimit: form.creditLimit ? Number(form.creditLimit) : undefined,
        paymentTermsDays: Number(form.paymentTermsDays) || 30,
        isActive: form.isActive,
      });
      toast.success('تم حفظ التعديلات بنجاح.');
      router.push(`/finance/customers/${params.id}`);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل تحديث العميل.');
      setError(msg);
      toast.error(msg);
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="✏️ تعديل عميل" />
        <Card className="max-w-2xl">
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        </Card>
      </div>
    );
  }

  if (!form) {
    return (
      <div>
        <PageHeader title="✏️ تعديل عميل" />
        <Card className="max-w-2xl">
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error || 'العميل غير موجود.'}
          </div>
          <div className="mt-4">
            <Link href="/finance/customers">
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
        title="✏️ تعديل عميل"
        description={form.name}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'العملاء', href: '/finance/customers' },
          { label: form.code, href: `/finance/customers/${params.id}` },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href={`/finance/customers/${params.id}`}>
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
              إلغاء والعودة
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
            <Input
              label="كود العميل"
              value={form.code}
              readOnly
              hint="لا يمكن تغيير الكود (معرّف ثابت)"
            />
            <Input
              label="مدة السداد (أيام)"
              type="number"
              value={form.paymentTermsDays}
              onChange={onChange('paymentTermsDays')}
              min={0}
              max={365}
            />
          </div>

          <Input
            label="اسم العميل (بالعربية) *"
            value={form.name}
            onChange={onChange('name')}
            required
          />

          <Input
            label="اسم العميل (بالإنجليزية)"
            value={form.nameEn}
            onChange={onChange('nameEn')}
            placeholder="Alfajr Construction Co."
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
              value={form.taxId}
              onChange={onChange('taxId')}
              placeholder="اختياري"
            />
            <Input
              label="حد الائتمان (LYD)"
              type="number"
              value={form.creditLimit}
              onChange={onChange('creditLimit')}
              placeholder="0.0000"
              min={0}
              step="0.0001"
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
            <Link href={`/finance/customers/${params.id}`}>
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
