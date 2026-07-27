'use client';

// صفحة تعديل الموظف (Employee) — form مُعبَّأ مسبقاً
//
// الحقول: employeeNumber (readonly), fullName, email, phone, nationalId, departmentId, jobTitle,
//        hireDate, baseSalary, isActive
// (الحقول تطابق Employee DTO في api.ts — لا حقول غير معرَّفة كـ fullNameAr/managerId؛
//  basicSalary في الـ spec = baseSalary في الـ DTO الفعلي)

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader, useToast } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { hrApi, Department, getErrorMessage } from '@/lib/api';

interface FormState {
  employeeNumber: string;
  fullName: string;
  email: string;
  phone: string;
  nationalId: string;
  departmentId: string;
  jobTitle: string;
  hireDate: string;
  baseSalary: string;
  isActive: boolean;
}

export default function EditEmployeePage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const toast = useToast();
  const [form, setForm] = useState<FormState | null>(null);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [data, depts] = await Promise.all([
          hrApi.getEmployee(params.id),
          hrApi.listDepartments().catch(() => [] as Department[]),
        ]);
        setForm({
          employeeNumber: data.employeeNumber || '',
          fullName: data.fullName || '',
          email: data.email || '',
          phone: data.phone || '',
          nationalId: data.nationalId || '',
          departmentId: data.departmentId || '',
          jobTitle: data.jobTitle || '',
          hireDate: data.hireDate ? data.hireDate.split('T')[0] : '',
          baseSalary: String(data.baseSalary ?? 0),
          isActive: data.isActive ?? true,
        });
        setDepartments(depts);
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل تحميل الموظف.'));
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
    setForm({ ...form, [k]: k === 'baseSalary' ? v : v });
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;
    setError(null);
    if (!form.fullName || !form.email) {
      const msg = 'الاسم الكامل والبريد الإلكتروني مطلوبان.';
      setError(msg);
      toast.error(msg);
      return;
    }
    setSubmitting(true);
    try {
      await hrApi.updateEmployee(params.id, {
        employeeNumber: form.employeeNumber,
        fullName: form.fullName,
        email: form.email,
        phone: form.phone || undefined,
        nationalId: form.nationalId || undefined,
        departmentId: form.departmentId || undefined,
        jobTitle: form.jobTitle || undefined,
        hireDate: form.hireDate ? new Date(form.hireDate).toISOString() : new Date().toISOString(),
        baseSalary: Number(form.baseSalary) || 0,
        isActive: form.isActive,
      });
      toast.success('تم حفظ التعديلات بنجاح.');
      router.push(`/hr/employees/${params.id}`);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل تحديث الموظف.');
      setError(msg);
      toast.error(msg);
      setSubmitting(false);
    }
  };

  const deptOptions = [
    { label: '— بدون قسم —', value: '' },
    ...departments.filter((d) => d.isActive).map((d) => ({ label: d.name, value: d.id })),
  ];

  if (loading) {
    return (
      <div>
        <PageHeader title="✏️ تعديل موظف" />
        <Card className="max-w-2xl">
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        </Card>
      </div>
    );
  }

  if (!form) {
    return (
      <div>
        <PageHeader title="✏️ تعديل موظف" />
        <Card className="max-w-2xl">
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
            {error || 'الموظف غير موجود.'}
          </div>
          <div className="mt-4">
            <Link href="/hr/employees">
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
        title="✏️ تعديل موظف"
        description={form.fullName}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'الموظفين', href: '/hr/employees' },
          { label: form.employeeNumber, href: `/hr/employees/${params.id}` },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href={`/hr/employees/${params.id}`}>
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
          <Input
            label="الرقم الوظيفي"
            value={form.employeeNumber}
            readOnly
            hint="لا يمكن تغيير الرقم الوظيفي"
          />

          <Input
            label="الاسم الكامل *"
            value={form.fullName}
            onChange={onChange('fullName')}
            required
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="البريد الإلكتروني *"
              type="email"
              value={form.email}
              onChange={onChange('email')}
              required
            />
            <Input
              label="الهاتف"
              type="tel"
              value={form.phone}
              onChange={onChange('phone')}
              placeholder="+218 ..."
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="الرقم الوطني"
              value={form.nationalId}
              onChange={onChange('nationalId')}
              placeholder="اختياري"
            />
            <Input
              label="المسمى الوظيفي"
              value={form.jobTitle}
              onChange={onChange('jobTitle')}
              placeholder="مثال: محاسب"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="القسم"
              value={form.departmentId}
              onChange={onChange('departmentId')}
              options={deptOptions}
            />
            <Input
              type="date"
              label="تاريخ التعيين *"
              value={form.hireDate}
              onChange={onChange('hireDate')}
              required
            />
          </div>

          <Input
            label="الراتب الأساسي"
            type="number"
            min={0}
            step={0.01}
            value={form.baseSalary}
            onChange={onChange('baseSalary')}
            hint="للعرض فقط في هذه المرحلة (لا payroll)"
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
            <Link href={`/hr/employees/${params.id}`}>
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
