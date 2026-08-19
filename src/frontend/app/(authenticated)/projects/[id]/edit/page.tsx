'use client';

// صفحة تعديل المشروع (Project) — Sprint 59 redesign (DEC-172)
//
// Modern layout, same as /new but pre-populated. Uses projectsApi.updateProject
// (which we just added in DEC-172 to fix the bare-fetch 401 bug).
//
// Backend: PUT /api/projects/{id}.

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, Save, Briefcase, Calendar, Wallet, CheckCircle2,
  Building2,
} from 'lucide-react';
import {
  PageHero, SectionCard, StatusPill,
  Button, Input, Select, type SelectOption,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { projectsApi, getErrorMessage, type Project, type ProjectStatusName } from '@/lib/api';

// L120: BE serializes ProjectStatus as string. We send strings back too.
const PROJECT_STATUSES: SelectOption[] = [
  { label: 'تخطيط (Planning)', value: 'Planning' },
  { label: 'نشط (Active)', value: 'Active' },
  { label: 'معلّق (OnHold)', value: 'OnHold' },
  { label: 'مكتمل (Completed)', value: 'Completed' },
  { label: 'ملغي (Cancelled)', value: 'Cancelled' },
];

interface FormState {
  code: string;
  name: string;
  description: string;
  costCenterId: string;
  status: ProjectStatusName;
  budget: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export default function EditProjectPage() {
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
        const data = await projectsApi.getProject(params.id);
        setForm({
          code: data.code || '',
          name: data.name || '',
          description: data.description || '',
          costCenterId: data.costCenterId || '',
          status: data.status ?? 'Active',
          budget: String(data.budget ?? 0),
          startDate: data.startDate ? data.startDate.split('T')[0] : '',
          endDate: data.endDate ? data.endDate.split('T')[0] : '',
          isActive: data.isActive ?? true,
        });
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل تحميل المشروع'));
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
    const v = e.target.value;
    setForm({ ...form, [k]: k === 'status' ? (v as ProjectStatusName) : v });
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setError(null);
    setSubmitting(true);
    try {
      await projectsApi.updateProject(params.id, {
        code: form.code,
        name: form.name,
        description: form.description || undefined,
        costCenterId: form.costCenterId,
        status: form.status,
        budget: Number(form.budget),
        startDate: form.startDate ? new Date(form.startDate).toISOString() : undefined as unknown as string,
        endDate: form.endDate ? new Date(form.endDate).toISOString() : undefined,
        isActive: form.isActive,
      });
      router.push(`/projects/${params.id}`);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث المشروع.'));
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <PageHero
          eyebrow="إدارة المشاريع"
          title="تعديل مشروع"
          tone="violet"
          highlight={{ label: 'الحالة', value: '...' }}
        />
        <SectionCard title="جاري التحميل">
          <div className="py-12 text-center text-gray-500">جاري تحميل بيانات المشروع...</div>
        </SectionCard>
      </div>
    );
  }

  if (!form) {
    return (
      <div className="space-y-6">
        <PageHero
          eyebrow="إدارة المشاريع"
          title="تعديل مشروع"
          tone="violet"
          actions={
            <Link href="/projects">
              <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
                العودة إلى المشاريع
              </Button>
            </Link>
          }
        />
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-6 text-rose-700">
          <p className="font-semibold">تعذّر تحميل المشروع</p>
          <p className="mt-1 text-sm">{error || 'المشروع غير موجود'}</p>
        </div>
      </div>
    );
  }

  const isActive = form.status === 'Active';
  const isCompleted = form.status === 'Completed';
  const isCancelled = form.status === 'Cancelled';

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المشاريع"
        title={`تعديل: ${form.name}`}
        subtitle={`الكود: ${form.code} — مركز التكلفة: CC-${form.code}`}
        tone="violet"
        actions={
          <>
            <Link href={`/projects/${params.id}`}>
              <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
                العودة للتفاصيل
              </Button>
            </Link>
          </>
        }
        highlight={
          isActive
            ? { label: 'الحالة الحالية', value: 'نشط' }
            : isCompleted
              ? { label: 'الحالة الحالية', value: 'مكتمل' }
              : isCancelled
                ? { label: 'الحالة الحالية', value: 'ملغي' }
                : { label: 'الحالة الحالية', value: 'معلّق / تخطيط' }
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحديث المشروع</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      <form onSubmit={onSubmit} className="space-y-6">
        {/* Section 1: Basic info */}
        <SectionCard
          title="المعلومات الأساسية"
          description="كود المشروع، الاسم، الوصف، والحالة"
          actions={
            <StatusPill
              tone={isActive ? 'green' : isCompleted ? 'blue' : isCancelled ? 'red' : 'slate'}
              label={
                isActive ? 'نشط' : isCompleted ? 'مكتمل' : isCancelled ? 'ملغي' :
                form.status === 'OnHold' ? 'معلق' : 'تخطيط'
              }
              showDot={false}
            />
          }
        >
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <Input
              label="كود المشروع *"
              value={form.code}
              onChange={onChange('code')}
              required
            />
            <Select
              label="الحالة"
              value={String(form.status)}
              onChange={onChange('status')}
              options={PROJECT_STATUSES}
            />
          </div>

          <div className="mt-4">
            <Input
              label="اسم المشروع *"
              value={form.name}
              onChange={onChange('name')}
              required
            />
          </div>

          <div className="mt-4">
            <Input
              label="الوصف"
              value={form.description}
              onChange={onChange('description')}
            />
          </div>
        </SectionCard>

        {/* Section 2: Cost center (read-only info) */}
        <div className="flex items-start gap-3 rounded-2xl border border-blue-200 bg-blue-50 p-4">
          <div className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-xl bg-blue-100">
            <Building2 className="h-5 w-5 text-blue-600" />
          </div>
          <div className="flex-1">
            <p className="text-sm font-bold text-blue-900">مركز التكلفة المرتبط</p>
            <p className="mt-0.5 text-xs text-blue-800">
              <span className="rounded bg-white/60 px-1.5 py-0.5 font-mono font-semibold">
                {form.costCenterId || '— (لم يُربط بعد)'}
              </span>
              <span className="ms-2">من نوع <span className="font-mono">Project</span>، ينشأ تلقائياً عند إنشاء المشروع.</span>
            </p>
          </div>
        </div>

        {/* Section 3: Schedule + Budget */}
        <SectionCard
          title="الجدول الزمني والميزانية"
          description="تاريخ البداية والنهاية، وقيمة الميزانية المخصصة"
        >
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <Input
              label="الميزانية (ل.د)"
              type="number"
              step="0.01"
              value={form.budget}
              onChange={onChange('budget')}
            />
            <Input
              label="تاريخ البداية *"
              type="date"
              value={form.startDate}
              onChange={onChange('startDate')}
              required
            />
            <Input
              label="تاريخ النهاية"
              type="date"
              value={form.endDate}
              onChange={onChange('endDate')}
            />
          </div>

          <div className="mt-4 flex items-center gap-2">
            <input
              type="checkbox"
              id="isActive"
              checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              className="h-4 w-4 rounded border-gray-300 text-violet-600 focus:ring-violet-500"
            />
            <label htmlFor="isActive" className="text-sm text-gray-700">
              المشروع <strong>فعّال</strong> (إيقاف النشاط يخفي المشروع من القوائم النشطة)
            </label>
          </div>
        </SectionCard>

        {/* Actions footer */}
        <div className="flex flex-col gap-3 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-gray-200/70 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-xs text-gray-500">
            التغييرات تُحفظ على مستوى الجدول الرئيسي فقط. العقد والمستخلصات تُدار من تبويبات التفاصيل.
          </p>
          <div className="flex items-center gap-2">
            <Link href={`/projects/${params.id}`}>
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
              {submitting ? 'جاري الحفظ…' : 'حفظ التعديلات'}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
