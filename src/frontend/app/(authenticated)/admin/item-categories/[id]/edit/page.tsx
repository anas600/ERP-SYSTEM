'use client';

// تعديل فئة أصناف (Item Category Edit) — has PUT backend support

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save } from 'lucide-react';
import { Button, Input, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getErrorMessage, inventoryApi } from '@/lib/api';

interface ItemCategory {
  id: string;
  code: string;
  name: string;
  description?: string;
  parentId?: string;
  isActive: boolean;
}

interface FormState {
  name: string;
  description: string;
  parentId: string;
  isActive: boolean;
}

export default function EditCategoryPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const [form, setForm] = useState<FormState | null>(null);
  const [parents, setParents] = useState<ItemCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        if (!params.id) return;
        // Sprint 40 (L67): use inventoryApi (auto-JWT) instead of raw fetch
        const data = (await inventoryApi.listCategories()) as unknown as ItemCategory[];
        const current = data.find((c) => c.id === params.id);
        if (!current) throw new Error('الفئة غير موجودة');
        setForm({
          name: current.name,
          description: current.description || '',
          parentId: current.parentId || '',
          isActive: current.isActive ?? true,
        });
        setParents(data.filter((c) => c.id !== params.id));
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [params.id]);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => setForm((f) => (f ? { ...f, [k]: e.target.value } : f));

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setError(null);
    setSubmitting(true);
    try {
      // Sprint 40 (L67): use inventoryApi.updateCategory (auto-JWT) instead of raw fetch
      await inventoryApi.updateCategory(params.id!, {
        code: '',  // code is immutable on update (BE will keep existing)
        name: form.name,
        parentId: form.parentId || undefined,
        isActive: form.isActive,
      });
      router.push('/admin/item-categories');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحديث.'));
      setSubmitting(false);
    }
  };

  if (loading) return <div><PageHeader title="فئة" /><Card><div className="text-center py-12 text-gray-500">جاري التحميل...</div></Card></div>;
  if (!form) return <div><PageHeader title="فئة" /><Card><div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg text-sm">{error || 'غير موجود'}</div><div className="mt-4"><Link href="/admin/item-categories"><Button variant="ghost">رجوع</Button></Link></div></Card></div>;

  return (
    <div>
      <PageHeader
        title="✏️ تعديل فئة"
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'الفئات', href: '/admin/item-categories' },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href="/admin/item-categories">
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>رجوع</Button>
          </Link>
        }
      />

      <Card className="max-w-2xl">
        {error && <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>}

        <form onSubmit={onSubmit} className="space-y-4">
          <Input label="الاسم *" value={form.name} onChange={onChange('name')} required />
          <Input label="الوصف" value={form.description} onChange={onChange('description')} />

          <div>
            <label className="block text-sm text-gray-500 mb-1">الفئة الأب</label>
            <select
              value={form.parentId}
              onChange={onChange('parentId')}
              className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
            >
              <option value="">— لا يوجد —</option>
              {parents.map((p) => (
                <option key={p.id} value={p.id}>{p.code} - {p.name}</option>
              ))}
            </select>
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
            <Button type="submit" variant="primary" loading={submitting} iconLeft={<Save className="h-4 w-4" />}>حفظ التعديلات</Button>
            <Link href="/admin/item-categories">
              <Button type="button" variant="ghost">إلغاء</Button>
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
}
