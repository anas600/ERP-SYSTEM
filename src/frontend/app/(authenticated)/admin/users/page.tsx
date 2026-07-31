'use client';

// Sprint 2 — T9: Paginated + filterable users admin page.
//   Table view: email, full_name, is_active, roles, last_login_at
//   Filter:    company (uses T4 ?company_id=), role, search
//   Pagination: skip + take (default 50, max 100)
//
// Replaces the Phase 6.1b card-based list with a clean table + filters. The
// existing actions (Edit, Reset Password, Activate/Deactivate) are still
// available via per-row buttons.
//
// Backend contract (T4):
//   GET /api/users?company_id=&skip=&take=
//
// The role names are enriched on the frontend via a single
// `identityApi.listRoles()` call (cached) + N×`getUser(id)` for roleIds —
// this matches the Phase 6.2 design.

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  User,
  Mail,
  Shield,
  Calendar,
  CheckCircle2,
  XCircle,
  KeyRound,
  Power,
  Edit,
  ChevronLeft,
  ChevronRight,
  RefreshCw,
  AlertCircle,
  UserPlus,
  Filter,
} from 'lucide-react';
import {
  Badge,
  Button,
  Card,
  EmptyState,
  Modal,
  PageHeader,
  Select,
  SkeletonTable,
} from '@/components/ui';
import { Table, type TableColumn } from '@/components/ui';
import { useToast } from '@/lib/useToast';
import { useAuth } from '@/lib/useAuth';
import {
  companiesApi,
  identityApi,
  usersApi,
  getErrorMessage,
  type AdminUser,
  type Company,
  type RoleItem,
} from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

const DEFAULT_TAKE = 50;
const MAX_TAKE = 100;
const TAKE_OPTIONS = [25, 50, 100];

const ROLE_COLORS: Record<string, 'success' | 'info' | 'warning' | 'danger' | 'neutral'> = {
  Admin: 'danger',
  Accountant: 'info',
  ProjectManager: 'warning',
  Viewer: 'neutral',
};

interface UserWithRoles extends AdminUser {
  roles: string[];
  roleIds: string[];
}

export default function UsersAdminPage() {
  const router = useRouter();
  const toast = useToast();
  const { loading: authLoading, user: currentUser } = useAuth();

  // Data
  const [items, setItems] = useState<UserWithRoles[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Pagination — T4 uses skip/take
  const [skip, setSkip] = useState(0);
  const [take, setTake] = useState(DEFAULT_TAKE);

  // Filters — T9 explicit
  const [companyFilter, setCompanyFilter] = useState<string>(''); // '' = all
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');

  // For the company dropdown
  const [companies, setCompanies] = useState<Company[]>([]);
  const [companiesLoading, setCompaniesLoading] = useState(false);

  // For the role filter and per-row rendering
  const [roles, setRoles] = useState<RoleItem[]>([]);

  // Modals
  const [pwdUser, setPwdUser] = useState<UserWithRoles | null>(null);
  const [newPwd, setNewPwd] = useState('');
  const [busy, setBusy] = useState(false);

  // initial load
  useEffect(() => {
    if (authLoading) return;
    void loadCompanies();
    void loadRoles();
  }, [authLoading]);

  // main load — fires on any filter or pagination change
  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading, skip, take, companyFilter]);

  const loadCompanies = async () => {
    setCompaniesLoading(true);
    try {
      const res = await companiesApi.list({ pageSize: 100, includeInactive: true });
      setCompanies(res.items);
    } catch {
      // Companies dropdown is optional — silently skip
      setCompanies([]);
    } finally {
      setCompaniesLoading(false);
    }
  };

  const loadRoles = async () => {
    try {
      const r = await identityApi.listRoles();
      setRoles(r);
    } catch {
      setRoles([]);
    }
  };

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      // T4: GET /api/users?company_id=&skip=&take=
      const res = await usersApi.list({
        companyId: companyFilter || undefined,
        skip,
        take,
      });
      setTotal(res.total);

      // enrich with role names — N+1 on first page only, then roles cached
      const enriched: UserWithRoles[] = await Promise.all(
        res.items.map(async (u) => {
          try {
            const detail = await identityApi.getUser(u.id);
            const roleNames = detail.roleIds
              .map((rid) => roles.find((r) => r.id === rid)?.name)
              .filter((n): n is string => Boolean(n));
            return { ...u, roles: roleNames, roleIds: detail.roleIds };
          } catch {
            return { ...u, roles: [], roleIds: [] };
          }
        })
      );
      setItems(enriched);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل قائمة المستخدمين.'));
      setItems([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  // Client-side filters (search + role) — الـ backend filter للـ company فقط
  const filtered = useMemo(() => {
    return items.filter((u) => {
      if (search) {
        const q = search.toLowerCase();
        if (
          !u.email.toLowerCase().includes(q) &&
          !(u.fullName || '').toLowerCase().includes(q)
        ) {
          return false;
        }
      }
      if (roleFilter !== 'all' && !u.roles.includes(roleFilter)) return false;
      return true;
    });
  }, [items, search, roleFilter]);

  const totalPages = Math.max(1, Math.ceil(total / take));
  const hasPrev = skip > 0;
  const hasNext = skip + take < total;

  const onApplySearch = () => {
    setSkip(0);
    setSearch(searchInput.trim());
  };

  const onClearFilters = () => {
    setSearchInput('');
    setSearch('');
    setRoleFilter('all');
    setCompanyFilter('');
    setSkip(0);
  };

  const onChangeCompany = (val: string) => {
    setCompanyFilter(val);
    setSkip(0);
  };

  const onChangeTake = (val: number) => {
    setTake(Math.min(MAX_TAKE, val));
    setSkip(0);
  };

  const toggleActive = async (u: UserWithRoles) => {
    const action = u.isActive ? 'تعطيل' : 'تفعيل';
    if (!confirm(`هل تريد ${action} المستخدم ${u.fullName || u.email}؟`)) return;
    setBusy(true);
    try {
      if (u.isActive) {
        await identityApi.deactivateUser(u.id);
        toast.success(`تم تعطيل ${u.fullName || u.email}`);
      } else {
        await identityApi.updateUser(u.id, { isActive: true });
        toast.success(`تم تفعيل ${u.fullName || u.email}`);
      }
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, `فشل ${action} المستخدم.`));
    } finally {
      setBusy(false);
    }
  };

  const submitResetPassword = async () => {
    if (!pwdUser) return;
    if (newPwd.length < 8) {
      toast.error('كلمة المرور يجب أن تكون 8 أحرف على الأقل.');
      return;
    }
    setBusy(true);
    try {
      await identityApi.resetPassword(pwdUser.id, newPwd);
      toast.success(`تم تغيير كلمة المرور لـ ${pwdUser.fullName || pwdUser.email}`);
      setPwdUser(null);
      setNewPwd('');
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل تغيير كلمة المرور.'));
    } finally {
      setBusy(false);
    }
  };

  // Columns
  const columns: TableColumn<UserWithRoles>[] = [
    {
      key: 'fullName',
      header: 'الاسم',
      render: (u) => (
        <div>
          <div className="flex items-center gap-2">
            <User className="h-3 w-3 text-gray-400" />
            <span className="font-medium text-gray-800">
              {u.fullName || '—'}
            </span>
            {u.id === currentUser?.id && (
              <Badge variant="info" size="sm">
                أنت
              </Badge>
            )}
          </div>
        </div>
      ),
    },
    {
      key: 'email',
      header: 'الإيميل',
      render: (u) => (
        <div className="flex items-center gap-1 text-sm text-gray-700" dir="ltr">
          <Mail className="h-3 w-3 text-gray-400" />
          <span>{u.email}</span>
        </div>
      ),
    },
    {
      key: 'isActive',
      header: 'الحالة',
      render: (u) =>
        u.isActive ? (
          <Badge variant="success">
            <CheckCircle2 className="h-3 w-3 inline-block ml-1" />
            فعّال
          </Badge>
        ) : (
          <Badge variant="neutral">
            <XCircle className="h-3 w-3 inline-block ml-1" />
            معطّل
          </Badge>
        ),
      className: 'w-24',
    },
    {
      key: 'roles',
      header: 'الأدوار',
      render: (u) =>
        u.roles.length === 0 ? (
          <span className="text-xs text-gray-400">—</span>
        ) : (
          <div className="flex flex-wrap gap-1">
            {u.roles.map((r) => (
              <Badge key={r} variant={ROLE_COLORS[r] ?? 'neutral'} size="sm">
                <Shield className="h-3 w-3 inline-block ml-1" />
                {r}
              </Badge>
            ))}
          </div>
        ),
    },
    {
      key: 'lastLoginAt',
      header: 'آخر دخول',
      render: (u) =>
        u.lastLoginAt ? (
          <div>
            <div className="text-sm text-gray-700">{formatDate(u.lastLoginAt)}</div>
            <div className="text-xs text-gray-400">{formatTime(u.lastLoginAt)}</div>
          </div>
        ) : (
          <span className="text-xs text-gray-400">لم يسجل دخول</span>
        ),
      className: 'w-32',
    },
    {
      key: 'actions',
      header: '',
      render: (u) => (
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.push(`/admin/users/${u.id}`)}
            iconLeft={<Edit className="h-3 w-3" />}
            title="تعديل"
          >
            عرض
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setPwdUser(u);
              setNewPwd('');
            }}
            disabled={busy}
            iconLeft={<KeyRound className="h-3 w-3" />}
            title="إعادة تعيين كلمة المرور"
          >
            <span className="sr-only">إعادة تعيين كلمة المرور</span>
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => toggleActive(u)}
            disabled={busy}
            iconLeft={<Power className="h-3 w-3" />}
            title={u.isActive ? 'تعطيل' : 'تفعيل'}
          >
            <span className="sr-only">{u.isActive ? 'تعطيل' : 'تفعيل'}</span>
          </Button>
        </div>
      ),
      className: 'w-40',
    },
  ];

  return (
    <div>
      <PageHeader
        title="👥 إدارة المستخدمين"
        description="Users Admin — عرض وتعيين أدوار وتعطيل المستخدمين"
        actions={
          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              onClick={load}
              disabled={loading}
              iconLeft={<RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />}
            >
              تحديث
            </Button>
            <Button
              variant="primary"
              onClick={() => router.push('/admin/users/new')}
              iconLeft={<UserPlus className="h-4 w-4" />}
            >
              مستخدم جديد
            </Button>
          </div>
        }
      />

      {/* Filters — T9 explicit (company_id), plus existing search/role */}
      <Card className="mb-4">
        <div className="flex items-center gap-2 text-sm text-gray-600 mb-3">
          <Filter className="h-4 w-4" />
          <span className="font-semibold">الفلاتر</span>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3" dir="rtl">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">الشركة</label>
            <select
              value={companyFilter}
              onChange={(e) => onChangeCompany(e.target.value)}
              disabled={companiesLoading}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            >
              <option value="">جميع الشركات</option>
              {companies.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.code} — {c.name} {c.isHolding ? '(Holding)' : ''}
                </option>
              ))}
            </select>
            <p className="text-xs text-gray-500 mt-1">
              {companiesLoading
                ? 'جاري تحميل الشركات...'
                : companyFilter
                  ? 'مفلتر بالشركة المحددة'
                  : 'بدون فلتر — يعرض كل المستخدمين'}
            </p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              بحث بالاسم أو الإيميل
            </label>
            <input
              type="text"
              placeholder="ابحث..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') onApplySearch();
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <Select
            label="الدور"
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
            options={[
              { label: 'جميع الأدوار', value: 'all' },
              ...roles.map((r) => ({ label: r.name, value: r.name })),
            ]}
          />
        </div>
        <div className="flex gap-2 mt-3 pt-3 border-t border-gray-100">
          <Button onClick={onApplySearch} variant="primary" size="sm">
            تطبيق البحث
          </Button>
          <Button onClick={onClearFilters} variant="secondary" size="sm">
            مسح كل الفلاتر
          </Button>
        </div>
      </Card>

      {error && (
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل المستخدمين</p>
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
        <SkeletonTable rows={Math.min(take, 8)} cols={5} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<User className="h-12 w-12" />}
          title="لا يوجد مستخدمين"
          description={
            companyFilter || search || roleFilter !== 'all'
              ? 'لا توجد نتائج تطابق الفلاتر المحددة.'
              : 'أضف أول مستخدم للنظام.'
          }
          action={
            <Button
              variant="primary"
              onClick={() => router.push('/admin/users/new')}
              iconLeft={<UserPlus className="h-4 w-4" />}
            >
              مستخدم جديد
            </Button>
          }
        />
      ) : (
        <>
          <div className="mb-2 text-sm text-gray-500">
            عرض <span className="font-bold text-gray-700">{filtered.length}</span> مستخدم
            {total !== filtered.length && (
              <>
                {' '}
                (من أصل <span className="font-bold text-gray-700">{total}</span>)
              </>
            )}
          </div>
          <Table
            data={filtered}
            loading={false}
            rowKey={(u) => u.id}
            columns={columns}
            emptyMessage="لا يوجد مستخدمين"
          />

          {/* Pagination — skip + take controls */}
          <div className="mt-4 flex items-center justify-between gap-2 flex-wrap">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <span>عدد النتائج في الصفحة:</span>
              <select
                value={take}
                onChange={(e) => onChangeTake(Number(e.target.value))}
                className="px-2 py-1 border border-gray-300 rounded text-sm"
                disabled={loading}
              >
                {TAKE_OPTIONS.map((n) => (
                  <option key={n} value={n}>
                    {n}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-1">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSkip(0)}
                disabled={!hasPrev || loading}
              >
                الأولى
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSkip(Math.max(0, skip - take))}
                disabled={!hasPrev || loading}
                iconLeft={<ChevronRight className="h-4 w-4" />}
              >
                السابق
              </Button>
              <span className="px-3 py-2 text-sm text-gray-600">
                {total > 0 ? Math.floor(skip / take) + 1 : 0} / {totalPages}
              </span>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSkip(skip + take)}
                disabled={!hasNext || loading}
                iconRight={<ChevronLeft className="h-4 w-4" />}
              >
                التالي
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSkip(Math.max(0, (totalPages - 1) * take))}
                disabled={!hasNext || loading}
              >
                الأخيرة
              </Button>
            </div>
          </div>
        </>
      )}

      {/* Reset Password Modal */}
      <Modal
        open={!!pwdUser}
        onClose={() => {
          if (!busy) {
            setPwdUser(null);
            setNewPwd('');
          }
        }}
        title="إعادة تعيين كلمة المرور"
      >
        {pwdUser && (
          <div className="space-y-3" dir="rtl">
            <p className="text-sm text-gray-600">
              المستخدم:{' '}
              <span className="font-bold">{pwdUser.fullName || pwdUser.email}</span>
            </p>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                كلمة المرور الجديدة (8 أحرف على الأقل)
              </label>
              <input
                type="password"
                value={newPwd}
                onChange={(e) => setNewPwd(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                placeholder="••••••••"
                disabled={busy}
                autoFocus
                dir="ltr"
              />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button
                variant="secondary"
                onClick={() => {
                  setPwdUser(null);
                  setNewPwd('');
                }}
                disabled={busy}
              >
                إلغاء
              </Button>
              <Button
                variant="primary"
                onClick={submitResetPassword}
                disabled={busy || newPwd.length < 8}
                loading={busy}
              >
                {busy ? 'جاري...' : 'حفظ'}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
