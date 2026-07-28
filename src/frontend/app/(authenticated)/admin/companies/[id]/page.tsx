'use client';

// Sprint 2 — T8: Company detail + edit page.
//   Route: /admin/companies/[id]
//   Show:  name, code, legal_name, base_currency, is_holding, is_active,
//          parent_company_id, created_at
//   Edit:  inline form (PUT /api/companies/{id} via companiesApi.update)
//
// الـ edit form يعرض نفس حقول الـ Create Modal في T7 (مع disabled للـ code —
// لا نسمح بتغيير الـ primary key بعد الإنشاء). الـ backend (T1, T3, T8) يُحدَّد
// شكله في الـ parallel branch.
//
// الـ activating/deactivating عبر `companiesApi.setActive` — يستعمل
// PUT /api/companies/{id}/activate.

import { useCallback, useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import {
  ArrowRight,
  Save,
  Building2,
  Hash,
  MapPin,
  Calendar,
  Power,
  CheckCircle2,
  XCircle,
  AlertCircle,
  RefreshCw,
  Edit,
  X,
} from 'lucide-react';
import {
  Badge,
  Button,
  Card,
  Input,
  PageHeader,
  Select,
  SkeletonTable,
} from '@/components/ui';
import { useToast } from '@/lib/useToast';
import { useAuth } from '@/lib/useAuth';
import {
  companiesApi,
  getErrorMessage,
  type Company,
  type UpdateCompanyRequest,
} from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

const CURRENCY_OPTIONS = [
  { label: 'دينار ليبي (LYD)', value: 'LYD' },
  { label: 'دولار أمريكي (USD)', value: 'USD' },
  { label: 'يورو (EUR)', value: 'EUR' },
  { label: 'جنيه مصري (EGP)', value: 'EGP' },
  { label: 'درهم إماراتي (AED)', value: 'AED' },
];

interface EditState {
  name: string;
  legalName: string;
  taxNumber: string;
  baseCurrency: string;
  country: string;
  isHolding: boolean;
  isActive: boolean;
  parentCompanyId: string;
}

export default function CompanyDetailPage() {
  const params = useParams();
  const id = params?.id as string;
  const router = useRouter();
  const toast = useToast();
  const { loading: authLoading } = useAuth();

  const [company, setCompany] = useState<Company | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // edit mode
  const [editing, setEditing] = useState(false);
  const [edit, setEdit] = useState<EditState | null>(null);
  const [saving, setSaving] = useState(false);
  const [editErrors, setEditErrors] = useState<Record<string, string>>({});

  // parent companies for the dropdown
  const [parentOptions, setParentOptions] = useState<
    { label: string; value: string; isHolding?: boolean }[]
  >([]);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const [c, list] = await Promise.all([
        companiesApi.get(id),
        // Parent dropdown — كل الشركات الأخرى (نستبعد الشركة الحالية).
        companiesApi.list({ pageSize: 100, includeInactive: true }).catch(() => null),
      ]);
      setCompany(c);
      if (list) {
        setParentOptions(
          list.items
            .filter((p) => p.id !== id)
            .map((p) => ({
              label: `${p.code} — ${p.name}${p.isHolding ? ' (Holding)' : ''}`,
              value: p.id,
              isHolding: p.isHolding,
            }))
        );
      }
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل بيانات الشركة.'));
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    if (authLoading) return;
    void load();
  }, [authLoading, load]);

  const startEdit = () => {
    if (!company) return;
    setEdit({
      name: company.name,
      legalName: company.legalName ?? '',
      taxNumber: company.taxNumber ?? '',
      baseCurrency: company.baseCurrency,
      country: company.country ?? '',
      isHolding: company.isHolding,
      isActive: company.isActive,
      parentCompanyId: company.parentCompanyId ?? '',
    });
    setEditErrors({});
    setEditing(true);
  };

  const cancelEdit = () => {
    setEditing(false);
    setEdit(null);
    setEditErrors({});
  };

  const validate = (state: EditState): boolean => {
    const e: Record<string, string> = {};
    if (!state.name.trim()) e.name = 'الاسم مطلوب';
    if (!state.baseCurrency.trim()) e.baseCurrency = 'العملة مطلوبة';
    setEditErrors(e);
    return Object.keys(e).length === 0;
  };

  const save = async () => {
    if (!company || !edit) return;
    if (!validate(edit)) return;
    setSaving(true);
    try {
      const body: UpdateCompanyRequest = {
        name: edit.name.trim(),
        legalName: edit.legalName.trim() || undefined,
        taxNumber: edit.taxNumber.trim() || undefined,
        baseCurrency: edit.baseCurrency.trim().toUpperCase(),
        country: edit.country.trim() || undefined,
        isHolding: edit.isHolding,
        isActive: edit.isActive,
        parentCompanyId: edit.parentCompanyId || null,
      };
      const updated = await companiesApi.update(company.id, body);
      setCompany(updated);
      setEditing(false);
      setEdit(null);
      toast.success(`تم حفظ تغييرات "${updated.name}"`);
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل حفظ التغييرات.'));
    } finally {
      setSaving(false);
    }
  };

  const toggleActive = async () => {
    if (!company) return;
    const next = !company.isActive;
    const verb = next ? 'تفعيل' : 'تعطيل';
    if (!confirm(`هل تريد ${verb} الشركة "${company.name}"؟`)) return;
    try {
      await companiesApi.setActive(company.id, next);
      setCompany({ ...company, isActive: next });
      toast.success(`تم ${verb} الشركة "${company.name}"`);
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, `فشل ${verb} الشركة.`));
    }
  };

  if (authLoading || loading) {
    return (
      <div>
        <PageHeader title="تحميل..." />
        <div className="bg-white rounded-xl shadow-sm p-4">
          <SkeletonTable rows={3} cols={3} />
        </div>
      </div>
    );
  }

  if (error && !company) {
    return (
      <div>
        <PageHeader
          title="خطأ"
          description="تعذّر تحميل الشركة"
          actions={
            <Button variant="secondary" onClick={() => router.push('/admin/companies')}>
              <ArrowRight className="h-4 w-4 inline-block ml-1" />
              العودة
            </Button>
          }
        />
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل بيانات الشركة</p>
            <p className="text-sm mt-0.5">{error}</p>
          </div>
          <Button variant="secondary" onClick={load}>
            <RefreshCw className="h-4 w-4 inline-block ml-1" />
            إعادة المحاولة
          </Button>
        </div>
      </div>
    );
  }

  if (!company) return null;

  return (
    <div>
      <PageHeader
        title={
          <span className="flex items-center gap-2">
            <Building2 className="h-6 w-6 text-gray-500" />
            {company.name}
          </span>
        }
        description={`تفاصيل الشركة — ${company.code}`}
        breadcrumb={[
          { label: 'إدارة الشركات', href: '/admin/companies' },
          { label: company.code },
        ]}
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => router.push('/admin/companies')}>
              <ArrowRight className="h-4 w-4 inline-block ml-1" />
              العودة
            </Button>
            {!editing ? (
              <Button variant="primary" onClick={startEdit} iconLeft={<Edit className="h-4 w-4" />}>
                تعديل
              </Button>
            ) : (
              <>
                <Button variant="secondary" onClick={cancelEdit} disabled={saving}>
                  <X className="h-4 w-4 inline-block ml-1" />
                  إلغاء
                </Button>
                <Button
                  variant="primary"
                  onClick={save}
                  disabled={saving}
                  loading={saving}
                  iconLeft={<Save className="h-4 w-4" />}
                >
                  {saving ? 'جاري...' : 'حفظ'}
                </Button>
              </>
            )}
          </div>
        }
      />

      {error && (
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <p className="text-sm flex-1">{error}</p>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Main info */}
        <div className="lg:col-span-2 space-y-4">
          <Card title="المعلومات الأساسية" description="تفاصيل الشركة المسجلة في النظام">
            {editing && edit ? (
              <div className="space-y-4" dir="rtl">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      الرمز (Code)
                    </label>
                    <Input value={company.code} disabled dir="ltr" hint="لا يمكن تغيير الرمز بعد الإنشاء" />
                  </div>
                  <Input
                    label="الاسم *"
                    value={edit.name}
                    onChange={(e) => setEdit({ ...edit, name: e.target.value })}
                    error={editErrors.name}
                  />
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Input
                    label="الاسم القانوني"
                    value={edit.legalName}
                    onChange={(e) => setEdit({ ...edit, legalName: e.target.value })}
                  />
                  <Input
                    label="الرقم الضريبي"
                    value={edit.taxNumber}
                    onChange={(e) => setEdit({ ...edit, taxNumber: e.target.value })}
                    dir="ltr"
                  />
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Select
                    label="العملة الأساسية *"
                    value={edit.baseCurrency}
                    onChange={(e) => setEdit({ ...edit, baseCurrency: e.target.value })}
                    options={CURRENCY_OPTIONS}
                    error={editErrors.baseCurrency}
                  />
                  <Input
                    label="البلد"
                    value={edit.country}
                    onChange={(e) => setEdit({ ...edit, country: e.target.value })}
                  />
                </div>
                {!edit.isHolding && parentOptions.length > 0 && (
                  <Select
                    label="الشركة الأم"
                    value={edit.parentCompanyId}
                    onChange={(e) => setEdit({ ...edit, parentCompanyId: e.target.value })}
                    options={[
                      { label: '— لا شيء (مستقلة) —', value: '' },
                      ...parentOptions,
                    ]}
                    hint="اختر الشركة القابضة لهذه الشركة الفرعية"
                  />
                )}
                <div className="flex flex-wrap items-center gap-4 pt-2 border-t border-gray-100">
                  <label className="flex items-center gap-2 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={edit.isHolding}
                      onChange={(e) => setEdit({ ...edit, isHolding: e.target.checked })}
                      className="rounded"
                    />
                    <span>شركة قابضة</span>
                  </label>
                  <label className="flex items-center gap-2 text-sm text-gray-700">
                    <input
                      type="checkbox"
                      checked={edit.isActive}
                      onChange={(e) => setEdit({ ...edit, isActive: e.target.checked })}
                      className="rounded"
                    />
                    <span>فعّالة</span>
                  </label>
                </div>
              </div>
            ) : (
              <dl className="space-y-3" dir="rtl">
                <Row label="الاسم">
                  <span className="font-bold text-gray-800">{company.name}</span>
                </Row>
                <Row label="الرمز">
                  <span className="font-mono text-sm text-gray-700">{company.code}</span>
                </Row>
                <Row label="الاسم القانوني">
                  {company.legalName ? (
                    <span className="text-gray-800">{company.legalName}</span>
                  ) : (
                    <span className="text-gray-400">—</span>
                  )}
                </Row>
                <Row label="الرقم الضريبي">
                  {company.taxNumber ? (
                    <span className="font-mono text-sm text-gray-700" dir="ltr">
                      {company.taxNumber}
                    </span>
                  ) : (
                    <span className="text-gray-400">—</span>
                  )}
                </Row>
                <Row label="العملة الأساسية">
                  <span className="font-mono text-sm text-gray-700">{company.baseCurrency}</span>
                </Row>
                <Row label="البلد">
                  {company.country ? (
                    <span className="flex items-center gap-1 text-gray-800">
                      <MapPin className="h-3 w-3 text-gray-500" />
                      {company.country}
                    </span>
                  ) : (
                    <span className="text-gray-400">—</span>
                  )}
                </Row>
                <Row label="النوع">
                  {company.isHolding ? (
                    <Badge variant="info">شركة قابضة</Badge>
                  ) : (
                    <Badge variant="neutral">شركة فرعية</Badge>
                  )}
                </Row>
                <Row label="الحالة">
                  {company.isActive ? (
                    <Badge variant="success">
                      <CheckCircle2 className="h-3 w-3 inline-block ml-1" />
                      فعّالة
                    </Badge>
                  ) : (
                    <Badge variant="neutral">
                      <XCircle className="h-3 w-3 inline-block ml-1" />
                      معطّلة
                    </Badge>
                  )}
                </Row>
                <Row label="الشركة الأم">
                  {company.parentCompanyId ? (
                    <button
                      onClick={() => router.push(`/admin/companies/${company.parentCompanyId}`)}
                      className="text-blue-600 hover:underline text-sm"
                    >
                      {company.parentCompanyName ||
                        `${company.parentCompanyId.slice(0, 8)}…`}
                    </button>
                  ) : (
                    <span className="text-gray-400">—</span>
                  )}
                </Row>
              </dl>
            )}
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <Card title="معرّف النظام">
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-1 text-gray-500">
                <Hash className="h-3 w-3" />
                <span className="text-xs">ID</span>
              </div>
              <div className="font-mono text-xs text-gray-700 break-all" dir="ltr">
                {company.id}
              </div>
            </div>
          </Card>

          <Card title="التواريخ">
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-1 text-gray-500">
                <Calendar className="h-3 w-3" />
                <span>تاريخ الإنشاء</span>
              </div>
              <div className="text-gray-800">
                {formatDate(company.createdAt)}{' '}
                <span className="text-gray-500 text-xs">
                  {formatTime(company.createdAt)}
                </span>
              </div>
              {company.updatedAt && (
                <>
                  <div className="flex items-center gap-1 text-gray-500 pt-2 border-t border-gray-100 mt-2">
                    <Calendar className="h-3 w-3" />
                    <span>آخر تحديث</span>
                  </div>
                  <div className="text-gray-800">
                    {formatDate(company.updatedAt)}{' '}
                    <span className="text-gray-500 text-xs">
                      {formatTime(company.updatedAt)}
                    </span>
                  </div>
                </>
              )}
            </div>
          </Card>

          <Card title="إجراءات سريعة">
            <div className="space-y-2">
              <Button
                variant={company.isActive ? 'outline' : 'primary'}
                fullWidth
                onClick={toggleActive}
                disabled={editing}
                iconLeft={<Power className="h-4 w-4" />}
              >
                {company.isActive ? 'تعطيل الشركة' : 'تفعيل الشركة'}
              </Button>
              <p className="text-xs text-gray-500">
                {company.isActive
                  ? 'تعطيل الشركة يمنع تسجيل دخول المستخدمين المرتبطين بها.'
                  : 'تفعيل الشركة يسمح بربط المستخدمين الجدد بها.'}
              </p>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}

// Helper row component for the read-only display
function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-3 py-1.5 border-b border-gray-50 last:border-0">
      <dt className="text-sm text-gray-500 shrink-0">{label}</dt>
      <dd className="text-sm text-end flex-1">{children}</dd>
    </div>
  );
}
