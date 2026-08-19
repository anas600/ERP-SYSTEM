'use client';

// صفحة الملف الشخصي — عرض بيانات المستخدم الحالي

import { useEffect, useState } from 'react';
import { Card, Badge, PageHeader, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, authApi, getErrorMessage, UserInfo } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { User, Mail, Shield, Building2, Calendar, Hash, Briefcase } from 'lucide-react';

export default function ProfilePage() {
  const { user: authUser, loading: authLoading } = useAuth();
  const [me, setMe] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await authApi.me();
      setMe(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الملف الشخصي.'));
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل...</p>
      </div>
    );
  }

  const user = me || authUser;
  if (!user) {
    return (
      <div className="text-center py-12 text-gray-500">لم يتم العثور على بيانات المستخدم.</div>
    );
  }

  return (
    <div>
      <PageHeader title="👤 الملف الشخصي" description="معلوماتك الشخصية في النظام" />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Profile Card */}
        <Card className="p-6 lg:col-span-1">
          <div className="text-center">
            <div className="h-24 w-24 mx-auto rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 text-white flex items-center justify-center text-3xl font-bold shadow-lg">
              {user.fullName
                ?.split(' ')
                .map((s) => s[0])
                .filter(Boolean)
                .slice(0, 2)
                .join('') || '؟'}
            </div>
            <h2 className="mt-4 text-xl font-bold text-gray-800">{user.fullName}</h2>
            <p className="text-sm text-gray-500 mt-1">{user.email}</p>

            <div className="mt-4 flex flex-wrap gap-2 justify-center">
              {user.roles?.map((role) => (
                <Badge key={role} variant="info">{role}</Badge>
              ))}
            </div>

            <div className="mt-6 pt-6 border-t border-gray-200 text-start">
              <a
                href="/profile/change-password"
                className="text-sm text-blue-600 hover:underline flex items-center gap-1"
              >
                تغيير كلمة المرور ←
              </a>
            </div>
          </div>
        </Card>

        {/* Details Card */}
        <Card className="p-6 lg:col-span-2">
          <h3 className="text-lg font-bold text-gray-800 mb-4">المعلومات</h3>
          <div className="space-y-4">
            <DetailRow icon={User} label="الاسم الكامل" value={user.fullName} />
            <DetailRow icon={Mail} label="البريد الإلكتروني" value={user.email} />
            <DetailRow icon={Hash} label="معرّف المستخدم" value={user.id} mono />
            <DetailRow
              icon={Shield}
              label="الأدوار"
              value={user.roles?.join(', ') || '—'}
            />
            <DetailRow
              icon={Building2}
              label="الشركة الافتراضية"
              value={user.companies?.find((c) => c.isDefault)?.name || user.defaultCompanyId}
            />
          </div>

          {user.companies && user.companies.length > 0 && (
            <div className="mt-6 pt-6 border-t border-gray-200">
              <h4 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                <Briefcase className="h-4 w-4" />
                الشركات المتاحة ({user.companies.length})
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {user.companies.map((c) => (
                  <div
                    key={c.companyId}
                    className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                  >
                    <div>
                      <p className="text-sm font-medium text-gray-800">{c.name}</p>
                      <p className="text-xs text-gray-500">{c.code}</p>
                    </div>
                    {c.isDefault && <Badge variant="success">افتراضي</Badge>}
                    {c.isHolding && <Badge variant="info">Holding</Badge>}
                  </div>
                ))}
              </div>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}

function DetailRow({
  icon: Icon,
  label,
  value,
  mono,
}: {
  icon: any;
  label: string;
  value: string;
  mono?: boolean;
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="h-9 w-9 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center flex-shrink-0">
        <Icon className="h-4 w-4" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-gray-500">{label}</p>
        <p className={`text-sm font-medium text-gray-800 break-all ${mono ? 'font-mono text-xs' : ''}`}>
          {value}
        </p>
      </div>
    </div>
  );
}
