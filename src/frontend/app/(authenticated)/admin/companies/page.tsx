'use client';

// Sprint 2 — T7: Paginated companies admin page.
// Replaces the Phase 6.1b version (Card-based list). The new design is
// table-based with explicit pagination controls (page + pageSize) and a
// "Create Company" button that opens a modal form posting to T3.
//
// Backend contract (T1, T3):
//   GET  /api/companies?page=N&pageSize=20&includeInactive=true
//   POST /api/companies
//   GET  /api/companies/{id}             (T8 detail view)
//   PUT  /api/companies/{id}             (T8 edit)
//
// Frontend-first errors (soft rule #9): كل خطأ في الواجهة يعرض رسالة ودودة
// بالعربي + الـ underlying message بالإنجليزي (getErrorMessage). الـ backend
// errors تستعمل رسائل "فشل..." كـ fallback.

import { useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  Plus,
  Pencil,
  Building2,
  ChevronLeft,
  ChevronRight,
  RefreshCw,
  AlertCircle,
} from 'lucide-react';
import {
  Badge,
  Button,
  Card,
  EmptyState,
  Input,
  Modal,
  PageHeader,
  Select,
  SkeletonTable,
} from '@/components/ui';
import { Table, type TableColumn } from '@/components/ui';
import { useToast } from '@/lib/useToast';
import { useAuth } from '@/lib/useAuth';
import { companiesApi, getErrorMessage, type Company, type CreateCompanyRequest } from '@/lib/api';
import { formatDate } from '@/lib/utils';

const DEFAULT_PAGE_SIZE = 20;
const MAX_PAGE_SIZE = 100;
const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

interface FormState {
  code: string;
  name: string;
  legalName: string;
  taxNumber: string;
  baseCurrency: string;
  country: string;
  isHolding: boolean;
  isActive: boolean;
  parentCompanyId: string;
}

const EMPTY_FORM: FormState = {
  code: '',
  name: '',
  legalName: '',
  taxNumber: '',
  baseCurrency: 'LYD',
  country: '',
  isHolding: false,
  isActive: true,
  parentCompanyId: '',
};

const CURRENCY_OPTIONS = [
  { label: 'دينار ليبي (LYD)', value: 'LYD' },
  { label: 'دولار أمريكي (USD)', value: 'USD' },
  { label: 'يورو (EUR)', value: 'EUR' },
  { label: 'جنيه مصري (EGP)', value: 'EGP' },
  { label: 'درهم إماراتي (AED)', value: 'AED' },
];

export default function CompaniesAdminPage() {
  const router = useRouter();
  const toast = useToast();
  const { loading: authLoading } = useAuth();

  // Data
  const [items, setItems] = useState<Company[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Pagination
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  // Search / filter
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [includeInactive, setIncludeInactive] = useState(true);

  // Create modal
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading, page, pageSize, includeInactive, search]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await companiesApi.list({
        page,
        pageSize: Math.min(pageSize, MAX_PAGE_SIZE),
        includeInactive,
        search: search || undefined,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل قائمة الشركات.'));
      setItems([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  // Derived stats — مفيدة للـ summary
  const stats = useMemo(() => {
    const all = items;
    const active = all.filter((c) => c.isActive).length;
    const holding = all.filter((c) => c.isHolding).length;
    return { total: all.length, active, holding, inactive: all.length - active };
  }, [items]);

  // Parent lookup — لعرض اسم الشركة الأم في الـ table بدون N+1 calls
  const parentById = useMemo(() => {
    const m = new Map<string, string>();
    for (const c of items) {
      if (c.parentCompanyId) m.set(c.parentCompanyId, c.parentCompanyName || c.name);
    }
    return m;
  }, [items]);

  // خيارات الشركات الأم في الـ create form (لاستبعاد القابضة نفسها)
  const parentOptions = useMemo(() => {
    return items
      .filter((c) => !c.isHolding)
      .map((c) => ({ label: `${c.code} — ${c.name}`, value: c.id }));
  }, [items]);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const hasPrev = page > 1;
  const hasNext = page < totalPages;

  const onApplySearch = () => {
    setPage(1);
    setSearch(searchInput.trim());
  };

  const onClearSearch = () => {
    setSearchInput('');
    setSearch('');
    setPage(1);
  };

  const openCreate = () => {
    setForm({ ...EMPTY_FORM, parentCompanyId: parentOptions[0]?.value ?? '' });
    setFormErrors({});
    setShowCreate(true);
  };

  const validateForm = (): boolean => {
    const e: Record<string, string> = {};
    if (!form.code.trim()) e.code = 'الرمز مطلوب';
    else if (!/^[A-Za-z0-9_-]{2,16}$/.test(form.code.trim())) e.code = 'الرمز 2-16 حرفاً (أحرف/أرقام/_/-)';
    if (!form.name.trim()) e.name = 'الاسم مطلوب';
    if (!form.baseCurrency.trim()) e.baseCurrency = 'العملة مطلوبة';
    setFormErrors(e);
    return Object.keys(e).length === 0;
  };

  const submitCreate = async () => {
    if (!validateForm()) return;
    setSubmitting(true);
    try {
      const body: CreateCompanyRequest = {
        code: form.code.trim().toUpperCase(),
        name: form.name.trim(),
        legalName: form.legalName.trim() || undefined,
        taxNumber: form.taxNumber.trim() || undefined,
        baseCurrency: form.baseCurrency.trim().toUpperCase(),
        country: form.country.trim() || undefined,
        isHolding: form.isHolding,
        isActive: form.isActive,
        parentCompanyId: form.parentCompanyId || null,
      };
      const created = await companiesApi.create(body);
      toast.success(`تم إنشاء الشركة "${created.name}" بنجاح`);
      setShowCreate(false);
      setPage(1);
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل إنشاء الشركة.'));
    } finally {
      setSubmitting(false);
    }
  };

  // Columns — Table component
  const columns: TableColumn<Company>[] = [
    {
      key: 'name',
      header: 'الاسم',
      render: (c) => (
        <div>
          <div className="font-medium text-gray-800">{c.name}</div>
          {c.legalName && c.legalName !== c.name && (
            <div className="text-xs text-gray-500">{c.legalName}</div>
          )}
        </div>
      ),
    },
    {
      key: 'code',
      header: 'الرمز',
      render: (c) => <span className="font-mono text-xs text-gray-700">{c.code}</span>,
      className: 'w-28',
    },
    {
      key: 'isHolding',
      header: 'قابضة؟',
      render: (c) =>
        c.isHolding ? (
          <Badge variant="info">نعم</Badge>
        ) : (
          <span className="text-xs text-gray-400">لا</span>
        ),
      className: 'w-20',
    },
    {
      key: 'isActive',
      header: 'الحالة',
      render: (c) =>
        c.isActive ? (
          <Badge variant="success">فعّالة</Badge>
        ) : (
          <Badge variant="neutral">معطّلة</Badge>
        ),
      className: 'w-24',
    },
    {
      key: 'parent',
      header: 'الشركة الأم',
      render: (c) => {
        if (c.isHolding) return <span className="text-xs text-gray-400">—</span>;
        if (!c.parentCompanyId) return <span className="text-xs text-gray-400">—</span>;
        const name = parentById.get(c.parentCompanyId) || c.parentCompanyId.slice(0, 8);
        return <span className="text-sm text-gray-700">{name}</span>;
      },
    },
    {
      key: 'baseCurrency',
      header: 'العملة',
      render: (c) => <span className="font-mono text-xs text-gray-600">{c.baseCurrency}</span>,
      className: 'w-20',
    },
    {
      key: 'createdAt',
      header: 'تاريخ الإنشاء',
      render: (c) => <span className="text-xs text-gray-500">{formatDate(c.createdAt)}</span>,
      className: 'w-32',
    },
    {
      key: 'actions',
      header: '',
      render: (c) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => router.push(`/admin/companies/${c.id}`)}
          iconLeft={<Pencil className="h-3 w-3" />}
        >
          عرض
        </Button>
      ),
      className: 'w-24',
    },
  ];

  return (
    <div>
      <PageHeader
        title="🏢 إدارة الشركات"
        description="Companies Admin — عرض وإنشاء وتعديل وتعطيل الشركات"
        actions={
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              onClick={() => load()}
              disabled={loading}
              iconLeft={<RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />}
            >
              تحديث
            </Button>
            <Button
              variant="primary"
              onClick={openCreate}
              iconLeft={<Plus className="h-4 w-4" />}
            >
              شركة جديدة
            </Button>
          </div>
        }
      />

      {/* Stats — تعرض ما في الصفحة الحالية (للـ quick view) */}
      {!loading && items.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
          <div className="bg-white rounded-xl shadow-sm p-4">
            <div className="text-sm text-gray-500">إجمالي في الصفحة</div>
            <div className="text-2xl font-bold text-blue-600 mt-1">{stats.total}</div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-4">
            <div className="text-sm text-gray-500">فعّالة</div>
            <div className="text-2xl font-bold text-green-600 mt-1">{stats.active}</div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-4">
            <div className="text-sm text-gray-500">قابضة</div>
            <div className="text-2xl font-bold text-purple-600 mt-1">{stats.holding}</div>
          </div>
          <div className="bg-white rounded-xl shadow-sm p-4">
            <div className="text-sm text-gray-500">إجمالي الكل</div>
            <div className="text-2xl font-bold text-gray-700 mt-1 tabular-nums">{total}</div>
          </div>
        </div>
      )}

      {/* Filters + search */}
      <Card className="mb-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3" dir="rtl">
          <Input
            label="بحث بالاسم أو الرمز"
            placeholder="مثال: MFA أو Trade"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') onApplySearch();
            }}
            containerClassName="md:col-span-2"
          />
          <Select
            label="حجم الصفحة"
            value={String(pageSize)}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPage(1);
            }}
            options={PAGE_SIZE_OPTIONS.map((n) => ({ label: `${n} / صفحة`, value: n }))}
          />
          <Select
            label="المعطّلة"
            value={includeInactive ? 'yes' : 'no'}
            onChange={(e) => {
              setIncludeInactive(e.target.value === 'yes');
              setPage(1);
            }}
            options={[
              { label: 'إظهار الكل', value: 'yes' },
              { label: 'الفعّالة فقط', value: 'no' },
            ]}
          />
        </div>
        <div className="flex gap-2 mt-3 pt-3 border-t border-gray-100">
          <Button onClick={onApplySearch} variant="primary" size="sm">
            تطبيق البحث
          </Button>
          {search && (
            <Button onClick={onClearSearch} variant="secondary" size="sm">
              مسح البحث
            </Button>
          )}
        </div>
      </Card>

      {error && (
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل قائمة الشركات</p>
            <p className="text-sm mt-0.5">{error}</p>
            <p className="text-xs mt-1 text-red-600">
              ملاحظة: في وضع التطوير قد يكون الـ endpoint غير مُفعَّل بعد.
            </p>
          </div>
          <Button variant="secondary" onClick={load} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={Math.min(pageSize, 8)} cols={5} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={<Building2 className="h-12 w-12" />}
          title="لا توجد شركات"
          description={
            search
              ? `لا توجد شركات تطابق "${search}".`
              : 'لم يتم تسجيل أي شركة بعد. أنشئ أول شركة من زر "شركة جديدة".'
          }
          action={
            <Button variant="primary" onClick={openCreate} iconLeft={<Plus className="h-4 w-4" />}>
              شركة جديدة
            </Button>
          }
        />
      ) : (
        <>
          <Table
            data={items}
            loading={false}
            rowKey={(c) => c.id}
            columns={columns}
            emptyMessage="لا توجد شركات"
          />

          {/* Pagination controls */}
          {totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between gap-2 flex-wrap">
              <div className="text-sm text-gray-600">
                صفحة {page} من {totalPages} — عرض{' '}
                <span className="font-semibold">{(page - 1) * pageSize + 1}</span>-
                <span className="font-semibold">
                  {Math.min(page * pageSize, total)}
                </span>{' '}
                من <span className="font-semibold">{total}</span>
              </div>
              <div className="flex items-center gap-1">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage(1)}
                  disabled={!hasPrev || loading}
                >
                  الأولى
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={!hasPrev || loading}
                  iconLeft={<ChevronRight className="h-4 w-4" />}
                >
                  السابق
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={!hasNext || loading}
                  iconRight={<ChevronLeft className="h-4 w-4" />}
                >
                  التالي
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setPage(totalPages)}
                  disabled={!hasNext || loading}
                >
                  الأخيرة
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {/* Create Company Modal (T3) */}
      <Modal
        open={showCreate}
        onClose={() => {
          if (!submitting) setShowCreate(false);
        }}
        title="شركة جديدة"
        description="إضافة شركة جديدة إلى القابضة"
        size="lg"
      >
        <div className="space-y-4" dir="rtl">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="الرمز (Code) *"
              placeholder="مثال: MFATECH"
              value={form.code}
              onChange={(e) => setForm((f) => ({ ...f, code: e.target.value.toUpperCase() }))}
              error={formErrors.code}
              hint="2-16 حرفاً (أحرف إنجليزية/أرقام/_/-)"
              dir="ltr"
              maxLength={16}
            />
            <Input
              label="الاسم (Name) *"
              placeholder="مثال: MFA Technology"
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
              error={formErrors.name}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="الاسم القانوني"
              placeholder="مثال: شركة MFA للتقنيات"
              value={form.legalName}
              onChange={(e) => setForm((f) => ({ ...f, legalName: e.target.value }))}
            />
            <Input
              label="الرقم الضريبي"
              placeholder="اختياري"
              value={form.taxNumber}
              onChange={(e) => setForm((f) => ({ ...f, taxNumber: e.target.value }))}
              dir="ltr"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="العملة الأساسية *"
              value={form.baseCurrency}
              onChange={(e) => setForm((f) => ({ ...f, baseCurrency: e.target.value }))}
              options={CURRENCY_OPTIONS}
              error={formErrors.baseCurrency}
            />
            <Input
              label="البلد"
              placeholder="مثال: ليبيا"
              value={form.country}
              onChange={(e) => setForm((f) => ({ ...f, country: e.target.value }))}
            />
          </div>

          {!form.isHolding && parentOptions.length > 0 && (
            <Select
              label="الشركة الأم (للشركات الفرعية)"
              value={form.parentCompanyId}
              onChange={(e) => setForm((f) => ({ ...f, parentCompanyId: e.target.value }))}
              options={[
                { label: '— لا شيء (شركة مستقلة) —', value: '' },
                ...parentOptions,
              ]}
              hint="اتركها فارغة لو الشركة مستقلة أو قابضة فرعية مباشرة"
            />
          )}

          <div className="flex flex-wrap items-center gap-4 pt-2 border-t border-gray-100">
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={form.isHolding}
                onChange={(e) => setForm((f) => ({ ...f, isHolding: e.target.checked }))}
                className="rounded"
              />
              <span>شركة قابضة</span>
            </label>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                className="rounded"
              />
              <span>فعّالة</span>
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              variant="secondary"
              onClick={() => setShowCreate(false)}
              disabled={submitting}
            >
              إلغاء
            </Button>
            <Button
              variant="primary"
              onClick={submitCreate}
              disabled={submitting}
              loading={submitting}
            >
              {submitting ? 'جاري الإنشاء...' : 'إنشاء الشركة'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
