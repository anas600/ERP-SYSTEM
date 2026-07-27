'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { User, Mail, Shield, Calendar, CheckCircle2, XCircle, KeyRound, UserPlus, Edit, Power } from 'lucide-react';
import { Card, Badge, PageHeader, Button, Modal, EmptyState, SkeletonTable } from '@/components/ui';
import { useToast } from '@/lib/useToast';
import { useAuth } from '@/lib/useAuth';
import { identityApi, AdminUser, getErrorMessage } from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

interface UserWithRoles extends AdminUser {
  roles: string[];
  roleIds?: string[];
  companies?: { companyId: string; companyName: string; isDefault: boolean; isHolding: boolean }[];
}

interface RoleInfo {
  id: string;
  name: string;
  description?: string;
}

const ROLE_COLORS: Record<string, 'success' | 'info' | 'warning' | 'danger' | 'neutral'> = {
  Admin: 'danger',
  Accountant: 'info',
  ProjectManager: 'warning',
  Viewer: 'neutral',
};

export default function UsersAdminPage() {
  const router = useRouter();
  const toast = useToast();
  const { loading: authLoading, user: currentUser } = useAuth();
  const [items, setItems] = useState<UserWithRoles[]>([]);
  const [total, setTotal] = useState(0);
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');

  // Modals
  const [pwdUser, setPwdUser] = useState<UserWithRoles | null>(null);
  const [newPwd, setNewPwd] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [usersRes, rolesRes] = await Promise.all([
        identityApi.listUsers(0, 100),
        identityApi.listRoles(),
      ]);
      const detailedItems = await Promise.all(
        usersRes.items.map(async (u) => {
          try {
            const detail = await identityApi.getUser(u.id);
            const roleNames = detail.roleIds.map(rid => rolesRes.find(r => r.id === rid)?.name || 'Unknown');
            return { ...u, roles: roleNames, roleIds: detail.roleIds, companies: detail.companies };
          } catch {
            return { ...u, roles: [] as string[] };
          }
        })
      );
      setItems(detailedItems);
      setTotal(usersRes.count);
      setRoles(rolesRes);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل.'));
    } finally {
      setLoading(false);
    }
  };

  const filtered = items.filter((u) => {
    if (search) {
      const q = search.toLowerCase();
      if (!u.email.toLowerCase().includes(q) && !u.fullName.toLowerCase().includes(q)) {
        return false;
      }
    }
    if (roleFilter !== 'all' && !u.roles.includes(roleFilter)) return false;
    return true;
  });

  const activeCount = items.filter((u) => u.isActive).length;
  const adminCount = items.filter((u) => u.roles.includes('Admin')).length;

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

  return (
    <div>
      <PageHeader
        title="👥 إدارة المستخدمين"
        description="Users Admin — عرض وتعيين أدوار وتعطيل المستخدمين"
        actions={
          <div className="flex gap-2">
            <Button onClick={load} variant="secondary" disabled={loading}>
              تحديث
            </Button>
            <Button onClick={() => router.push('/admin/users/new')} variant="primary">
              <UserPlus className="h-4 w-4 inline-block ml-1" />
              مستخدم جديد
            </Button>
          </div>
        }
      />

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">إجمالي</div>
          <div className="text-2xl font-bold text-blue-600 mt-1">{total}</div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">فعّال</div>
          <div className="text-2xl font-bold text-green-600 mt-1">{activeCount}</div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">Admin</div>
          <div className="text-2xl font-bold text-red-600 mt-1">{adminCount}</div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">2FA مفعّل</div>
          <div className="text-2xl font-bold text-purple-600 mt-1">
            {items.filter((u) => u.twoFactorEnabled).length}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <input
            type="text"
            placeholder="بحث بالاسم أو الإيميل..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
          />
          <select
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
          >
            <option value="all">جميع الأدوار</option>
            {roles.map((r) => (
              <option key={r.id} value={r.name}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-white rounded-xl shadow-sm p-4">
          <SkeletonTable rows={5} cols={4} />
        </div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-4">
          <EmptyState
            icon={<User className="h-12 w-12 text-gray-300" />}
            title="لا يوجد مستخدمين"
            description="أضف أول مستخدم للنظام."
            action={
              <Button onClick={() => router.push('/admin/users/new')} variant="primary">
                <UserPlus className="h-4 w-4 inline-block ml-1" />
                مستخدم جديد
              </Button>
            }
          />
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map((u) => (
            <Card key={u.id} accent={u.isActive ? 'blue' : 'gray'}>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <User className="h-4 w-4 text-gray-500" />
                    <h3 className="font-bold text-gray-800">{u.fullName || '—'}</h3>
                    {u.id === currentUser?.id && <Badge variant="info">أنت</Badge>}
                    {u.isActive ? (
                      <Badge variant="success">
                        <CheckCircle2 className="h-3 w-3 inline-block ml-1" />
                        فعّال
                      </Badge>
                    ) : (
                      <Badge variant="neutral">
                        <XCircle className="h-3 w-3 inline-block ml-1" />
                        معطّل
                      </Badge>
                    )}
                    {u.twoFactorEnabled && (
                      <Badge variant="warning">
                        <KeyRound className="h-3 w-3 inline-block ml-1" />
                        2FA
                      </Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-1 text-sm text-gray-600 mt-1">
                    <Mail className="h-3 w-3" />
                    {u.email}
                  </div>
                  <div className="mt-3 flex flex-wrap items-center gap-2">
                    <Shield className="h-3 w-3 text-gray-500" />
                    {u.roles.length === 0 ? (
                      <span className="text-sm text-gray-400">لا يوجد أدوار</span>
                    ) : (
                      u.roles.map((r) => (
                        <Badge key={r} variant={ROLE_COLORS[r] ?? 'neutral'}>
                          {r}
                        </Badge>
                      ))
                    )}
                  </div>
                  <div className="mt-2 flex items-center gap-3 text-xs text-gray-400">
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      {formatDate(u.createdAt)}
                    </span>
                    {u.lastLoginAt && (
                      <span>آخر دخول: {formatDate(u.lastLoginAt)} {formatTime(u.lastLoginAt)}</span>
                    )}
                  </div>
                </div>
                <div className="flex flex-col gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => router.push(`/admin/users/${u.id}`)}
                  >
                    <Edit className="h-3 w-3 inline-block ml-1" />
                    تعديل
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => { setPwdUser(u); setNewPwd(''); }}
                    disabled={busy}
                  >
                    <KeyRound className="h-3 w-3 inline-block ml-1" />
                    إعادة تعيين كلمة المرور
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => toggleActive(u)}
                    disabled={busy}
                  >
                    <Power className="h-3 w-3 inline-block ml-1" />
                    {u.isActive ? 'تعطيل' : 'تفعيل'}
                  </Button>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Reset Password Modal */}
      <Modal
        open={!!pwdUser}
        onClose={() => { if (!busy) { setPwdUser(null); setNewPwd(''); } }}
        title="إعادة تعيين كلمة المرور"
      >
        {pwdUser && (
          <div className="space-y-3" dir="rtl">
            <p className="text-sm text-gray-600">
              المستخدم: <span className="font-bold">{pwdUser.fullName || pwdUser.email}</span>
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
              />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button
                variant="secondary"
                onClick={() => { setPwdUser(null); setNewPwd(''); }}
                disabled={busy}
              >
                إلغاء
              </Button>
              <Button
                variant="primary"
                onClick={submitResetPassword}
                disabled={busy || newPwd.length < 8}
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
