'use client';

import { useEffect, useState } from 'react';
import { User, Mail, Shield, Calendar, CheckCircle2, XCircle, KeyRound } from 'lucide-react';
import { Card, Badge, PageHeader, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

interface UserWithRoles {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  twoFactorEnabled: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
  roles: string[];
}

interface RoleInfo {
  id: string;
  name: string;
  description?: string;
}

interface UsersResponse {
  items: UserWithRoles[];
  total: number;
  skip: number;
  take: number;
}

const ROLE_COLORS: Record<string, 'success' | 'info' | 'warning' | 'danger' | 'neutral'> = {
  Admin: 'danger',
  Accountant: 'info',
  ProjectManager: 'warning',
  Viewer: 'neutral',
};

export default function UsersAdminPage() {
  const { loading: authLoading, user: currentUser } = useAuth();
  const [items, setItems] = useState<UserWithRoles[]>([]);
  const [total, setTotal] = useState(0);
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('all');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [usersRes, rolesRes] = await Promise.all([
        api.get<UsersResponse>('/api/users', { params: { take: 100 } }),
        api.get<RoleInfo[]>('/api/users/roles'),
      ]);
      setItems(usersRes.data.items);
      setTotal(usersRes.data.total);
      setRoles(rolesRes.data);
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

  return (
    <div>
      <PageHeader
        title="👥 إدارة المستخدمين"
        description="Users Admin — عرض وتعيين أدوار وتعطيل المستخدمين"
        actions={
          <Button onClick={load} variant="secondary" disabled={loading}>
            تحديث
          </Button>
        }
      />

      {/* Stats */}
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

      {/* Filters */}
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
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          لا يوجد مستخدمين مطابقين.
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
                  <Button variant="ghost" size="sm">تعديل</Button>
                  <Button variant="ghost" size="sm">الأدوار</Button>
                  {u.isActive ? (
                    <Button variant="ghost" size="sm">تعطيل</Button>
                  ) : (
                    <Button variant="ghost" size="sm">تفعيل</Button>
                  )}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
