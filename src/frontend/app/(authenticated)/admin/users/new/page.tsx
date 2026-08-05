'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowRight, Save, UserPlus, Eye, EyeOff } from 'lucide-react';
import { Button, Card, PageHeader } from '@/components/ui';
import { useToast } from '@/lib/useToast';
import { identityApi, getErrorMessage } from '@/lib/api';

interface RoleItem { id: string; name: string; description?: string; }
interface CompanyItem { id: string; name: string; code: string; isHolding?: boolean; }

export default function NewUserPage() {
  const router = useRouter();
  const toast = useToast();
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [showPwd, setShowPwd] = useState(false);
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [defaultCompanyId, setDefaultCompanyId] = useState<string>('');
  const [roles, setRoles] = useState<RoleItem[]>([]);
  const [companies, setCompanies] = useState<CompanyItem[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    Promise.all([identityApi.listRoles(), fetch('/api/companies').then(r => r.json()).catch(() => [])])
      .then(([r, c]) => {
        setRoles(r);
        setCompanies(Array.isArray(c) ? c : []);
      })
      .catch(() => {});
  }, []);

  const validate = (): boolean => {
    const e: Record<string, string> = {};
    if (!email.trim()) e.email = 'الإيميل مطلوب';
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) e.email = 'صيغة الإيميل غير صحيحة';
    if (!fullName.trim()) e.fullName = 'الاسم الكامل مطلوب';
    if (password.length < 8) e.password = 'كلمة المرور يجب أن تكون 8 أحرف على الأقل';
    if (password !== confirm) e.confirm = 'كلمة المرور وتأكيدها غير متطابقتين';
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const onSubmit = async (ev: React.FormEvent) => {
    ev.preventDefault();
    if (!validate()) return;
    setSubmitting(true);
    try {
      const body: { email: string; fullName: string; password: string; isActive: boolean; roleIds?: string[]; defaultCompanyId?: string } = {
        email: email.trim(),
        fullName: fullName.trim(),
        password,
        isActive,
      };
      if (roleIds.length > 0) body.roleIds = roleIds;
      if (defaultCompanyId) body.defaultCompanyId = defaultCompanyId;
      await identityApi.createUser(body as any);
      toast.success(`تم إنشاء المستخدم ${fullName} بنجاح`);
      router.push('/admin/users');
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل إنشاء المستخدم.'));
    } finally {
      setSubmitting(false);
    }
  };

  const toggleRole = (id: string) => {
    setRoleIds((prev) => (prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]));
  };

  return (
    <div>
      <PageHeader
        title="مستخدم جديد"
        description="إضافة مستخدم جديد للنظام"
        actions={
          <Button variant="secondary" onClick={() => router.push('/admin/users')}>
            <ArrowRight className="h-4 w-4 inline-block ml-1" />
            العودة للقائمة
          </Button>
        }
      />

      <form onSubmit={onSubmit}>
        <Card>
          <div className="space-y-4" dir="rtl">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  الاسم الكامل <span className="text-danger-500">*</span>
                </label>
                <input
                  type="text"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                  placeholder="مثال: أحمد محمد"
                />
                {errors.fullName && <p className="text-xs text-danger-600 mt-1">{errors.fullName}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  الإيميل <span className="text-danger-500">*</span>
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                  placeholder="user@example.com"
                  dir="ltr"
                />
                {errors.email && <p className="text-xs text-danger-600 mt-1">{errors.email}</p>}
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  كلمة المرور <span className="text-danger-500">*</span>
                </label>
                <div className="relative">
                  <input
                    type={showPwd ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg text-sm"
                    placeholder="8 أحرف على الأقل"
                    dir="ltr"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPwd((s) => !s)}
                    className="absolute left-2 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700"
                    tabIndex={-1}
                  >
                    {showPwd ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {errors.password && <p className="text-xs text-danger-600 mt-1">{errors.password}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  تأكيد كلمة المرور <span className="text-danger-500">*</span>
                </label>
                <input
                  type={showPwd ? 'text' : 'password'}
                  value={confirm}
                  onChange={(e) => setConfirm(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
                  dir="ltr"
                />
                {errors.confirm && <p className="text-xs text-danger-600 mt-1">{errors.confirm}</p>}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">الأدوار</label>
              <div className="flex flex-wrap gap-2">
                {roles.length === 0 ? (
                  <span className="text-sm text-gray-400">جاري التحميل...</span>
                ) : (
                  roles.map((r) => {
                    const selected = roleIds.includes(r.id);
                    return (
                      <button
                        type="button"
                        key={r.id}
                        onClick={() => toggleRole(r.id)}
                        className={`px-3 py-1.5 rounded-lg text-sm border transition-colors ${
                          selected
                            ? 'bg-blue-600 text-white border-blue-600'
                            : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400'
                        }`}
                      >
                        {r.name}
                      </button>
                    );
                  })
                )}
              </div>
              <p className="text-xs text-gray-500 mt-1">يمكنك اختيار أكثر من دور.</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                الشركة الافتراضية
              </label>
              <select
                value={defaultCompanyId}
                onChange={(e) => setDefaultCompanyId(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
              >
                <option value="">— لا شيء —</option>
                {companies.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.code} — {c.name} {c.isHolding ? '(Holding)' : ''}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="rounded"
                />
                <span>فعّال فوراً</span>
              </label>
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t">
              <Button
                type="button"
                variant="secondary"
                onClick={() => router.push('/admin/users')}
                disabled={submitting}
              >
                إلغاء
              </Button>
              <Button type="submit" variant="primary" disabled={submitting}>
                {submitting ? (
                  'جاري الحفظ...'
                ) : (
                  <>
                    <Save className="h-4 w-4 inline-block ml-1" />
                    حفظ
                  </>
                )}
              </Button>
            </div>
          </div>
        </Card>
      </form>
    </div>
  );
}
