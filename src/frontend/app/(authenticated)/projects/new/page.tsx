'use client';

// صفحة إنشاء مشروع جديد (Project) — Sprint 59 redesign (DEC-171)
//
// Modern layout:
//   1. PageHero with eyebrow + title + hint
//   2. Form card with grouped sections (basic info, schedule, finance)
//   3. Info banner about auto-CostCenter creation
//   4. Save / Cancel actions in card footer
//
// Backend: POST /api/projects. Sprint 32 P0 (DEC-113) auto-creates a CostCenter
// per project (code=CC-{projectCode}), so the form does NOT need a costCenterId.

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, Save, Briefcase, Calendar, Wallet, FileText,
  Building2, CheckCircle2,
} from 'lucide-react';
import {
  PageHero, SectionCard, StatusPill,
  Button, Input, Select, type SelectOption,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { authedFetch, getErrorMessage, type ProjectStatusName } from '@/lib/api';

// L120: BE serializes ProjectStatus as string. We send strings too.
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
  status: ProjectStatusName;
  budget: string;
  startDate: string;
  endDate: string;
}

export default function NewProjectPage() {
  const router = useRouter();
  useAuth();
  const [form, setForm] = useState<FormState>({
    code: '',
    name: '',
    description: '',
    status: 'Active',
    budget: '0',
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onChange = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const v = e.target.value;
    setForm((f) => ({ ...f, [k]: k === 'status' ? (v as ProjectStatusName) : v }));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const res = await authedFetch('/api/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          code: form.code,
          name: form.name,
          description: form.description || undefined,
          status: form.status,
          budget: Number(form.budget),
          startDate: new Date(form.startDate).toISOString(),
          endDate: form.endDate ? new Date(form.endDate).toISOString() : undefined,
          isActive: true,
        }),
      });
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل إنشاء المشروع');
      }
      router.push('/projects');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إنشاء المشروع. تأكد من البيانات.'));
      setSubmitting(false);
    }
  };

  const isActive = form.status === 'Active';
  const isCompleted = form.status === 'Completed';

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المشاريع"
        title="مشروع جديد"
        subtitle="أضف مشروعاً جديداً إلى النظام. سيتم إنشاء مركز تكلفة تلقائياً وربطه بحساب إيرادات/تكاليف المشروع."
        tone="violet"
        actions={
          <Link href="/projects">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة إلى المشاريع
            </Button>
          </Link>
        }
        highlight={
          isCompleted
            ? { label: 'الحالة', value: 'مكتمل' }
            : isActive
              ? { label: 'الحالة الافتراضية', value: 'نشط' }
              : undefined
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر إنشاء المشروع</p>
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
              tone={isActive ? 'green' : isCompleted ? 'blue' : 'slate'}
              label={isActive ? 'مشروع نشط' : isCompleted ? 'مشروع مكتمل' : 'مشروع مُعلّق'}
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
              placeholder="مثال: PRJ-001"
              hint="كود فريد — يُستخدم لإنشاء مركز التكلفة CC-{code}"
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
              placeholder="مثال: بناء مجمع سكني - طرابلس"
            />
          </div>

          <div className="mt-4">
            <Input
              label="الوصف"
              value={form.description}
              onChange={onChange('description')}
              placeholder="وصف تفصيلي للمشروع (اختياري)"
            />
          </div>
        </SectionCard>

        {/* Info banner about auto CostCenter */}
        <div className="flex items-start gap-3 rounded-2xl border border-blue-200 bg-blue-50 p-4">
          <div className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-xl bg-blue-100">
            <CheckCircle2 className="h-5 w-5 text-blue-600" />
          </div>
          <div className="flex-1">
            <p className="text-sm font-bold text-blue-900">مركز التكلفة التلقائي</p>
            <p className="mt-0.5 text-xs text-blue-800">
              سيُنشأ مركز تكلفة تلقائياً باسم{' '}
              <span className="rounded bg-white/60 px-1.5 py-0.5 font-mono font-semibold">
                CC-{form.code || '{كود المشروع}'}
              </span>{' '}
              من نوع <span className="font-mono">Project</span> لهذا المشروع.
            </p>
          </div>
        </div>

        {/* Section 2: Schedule + Budget */}
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
        </SectionCard>

        {/* Actions footer */}
        <div className="flex flex-col gap-3 rounded-2xl bg-white p-4 shadow-sm ring-1 ring-gray-200/70 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-xs text-gray-500">
            جميع الحقول المطلوبة يجب ملؤها قبل الحفظ. بعد الإنشاء يمكنك إضافة العقد والمستخلصات.
          </p>
          <div className="flex items-center gap-2">
            <Link href="/projects">
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
              {submitting ? 'جاري الحفظ…' : 'حفظ المشروع'}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
