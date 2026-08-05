'use client';

// تعديل قاعدة ترحيل (Posting Rule Edit)
// ملاحظة: الـ backend حالياً لا يدعم PUT لقواعد الترحيل — الزر يظهر رسالة خطأ ودية.
// نحتفظ بالـ UI لأنه من المتطلبات ونضيف fallback ملائم.

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save, Trash2 } from 'lucide-react';
import { Button, Card, Input, PageHeader, Select } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getErrorMessage, financeApi } from '@/lib/api';
import { useToast } from '@/lib/useToast';

interface PostingRule {
  id: string;
  name: string;
  description?: string | null;
  eventType: number;
  isActive: boolean;
  templateJson: string;
  createdAt: string;
}

const EVENT_LABELS: Record<number, string> = {
  1: 'استلام مخزون (StockReceived)',
  2: 'صرف مخزون (StockIssued)',
  3: 'إنشاء فاتورة (InvoiceCreated)',
  4: 'استلام دفعة (PaymentReceived)',
};

const EVENT_OPTIONS = Object.entries(EVENT_LABELS).map(([value, label]) => ({
  label,
  value: Number(value),
}));

interface FormState {
  name: string;
  description: string;
  eventType: number;
  isActive: boolean;
  templateJson: string;
}

export default function EditPostingRulePage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const toast = useToast();
  useAuth();

  const [form, setForm] = useState<FormState | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        if (!params.id) return;
        // Sprint 40 (L67): use financeApi.listPostingRules (auto-JWT) instead of raw fetch
        const list = (await financeApi.listPostingRules()) as unknown as PostingRule[];
        const found = list.find((x) => x.id === params.id);
        if (!found) throw new Error('القاعدة غير موجودة');
        setForm({
          name: found.name,
          description: found.description ?? '',
          eventType: found.eventType,
          isActive: found.isActive,
          templateJson: found.templateJson,
        });
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, [params.id]);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const v = e.target.value;
    setForm((f) => (f ? { ...f, [k]: k === 'eventType' ? Number(v) : v } : f));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setError(null);

    try {
      JSON.parse(form.templateJson);
    } catch {
      setError('قالب JSON غير صالح.');
      return;
    }

    setSubmitting(true);
    try {
      // Sprint 40 (L67): use financeApi.updatePostingRule (auto-JWT) instead of raw fetch
      await financeApi.updatePostingRule(params.id!, {
        name: form.name,
        description: form.description || undefined,
        eventType: form.eventType,
        isActive: form.isActive,
        templateJson: form.templateJson,
      });
      toast.success('تم تحديث القاعدة.');
      router.push('/admin/posting-rules');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحديث.'));
      setSubmitting(false);
    }
  };

  const onDelete = async () => {
    if (!form) return;
    if (!confirm('هل أنت متأكد من حذف هذه القاعدة؟')) return;
    setSubmitting(true);
    try {
      // Sprint 40 (L67): use financeApi.deletePostingRule (auto-JWT) for posting rules
      await financeApi.deletePostingRule(params.id!);
      toast.success('تم حذف القاعدة.');
      router.push('/admin/posting-rules');
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل الحذف.'));
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="قاعدة" />
        <Card>
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        </Card>
      </div>
    );
  }

  if (!form) {
    return (
      <div>
        <PageHeader title="قاعدة" />
        <Card>
          <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg text-sm">
            {error || 'غير موجود'}
          </div>
          <div className="mt-4">
            <Link href="/admin/posting-rules">
              <Button variant="ghost">رجوع</Button>
            </Link>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="✏️ تعديل قاعدة ترحيل"
        description={form.name}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'قواعد الترحيل', href: '/admin/posting-rules' },
          { label: 'تعديل' },
        ]}
        actions={
          <div className="flex items-center gap-2">
            <Button variant="danger" onClick={onDelete} disabled={submitting} iconLeft={<Trash2 className="h-4 w-4" />}>
              حذف
            </Button>
            <Link href="/admin/posting-rules">
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
                رجوع
              </Button>
            </Link>
          </div>
        }
      />

      <Card className="max-w-3xl">
        {error && (
          <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={onSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input label="اسم القاعدة *" value={form.name} onChange={onChange('name')} required />
            <Select
              label="نوع الحدث *"
              value={String(form.eventType)}
              onChange={onChange('eventType')}
              options={EVENT_OPTIONS}
            />
          </div>
          <Input
            label="الوصف"
            value={form.description}
            onChange={onChange('description')}
            placeholder="اختياري"
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

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">قالب JSON *</label>
            <textarea
              value={form.templateJson}
              onChange={onChange('templateJson')}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-xs font-mono"
              rows={14}
              required
            />
            <p className="text-xs text-gray-500 mt-1">
              💡 المتغيرات المتاحة: {'{amount}'}, {'{reference}'}.
            </p>
          </div>

          <div className="flex items-center gap-2 pt-3 border-t">
            <Button
              type="submit"
              variant="primary"
              loading={submitting}
              iconLeft={<Save className="h-4 w-4" />}
            >
              حفظ التعديلات
            </Button>
            <Link href="/admin/posting-rules">
              <Button type="button" variant="ghost" disabled={submitting}>
                إلغاء
              </Button>
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
}
