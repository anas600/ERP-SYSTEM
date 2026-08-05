'use client';

// صفحة تغيير كلمة المرور (self-service)

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { Lock, ArrowRight, Eye, EyeOff, Check } from 'lucide-react';
import { Card, Button, PageHeader, Input } from '@/components/ui';
import { identityApi, getErrorMessage } from '@/lib/api';

export default function ChangePasswordPage() {
  const router = useRouter();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (!currentPassword || !newPassword || !confirmPassword) {
      setError('جميع الحقول مطلوبة.');
      return;
    }
    if (newPassword.length < 8) {
      setError('كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setError('كلمتا المرور غير متطابقتين.');
      return;
    }
    if (currentPassword === newPassword) {
      setError('كلمة المرور الجديدة يجب أن تكون مختلفة عن الحالية.');
      return;
    }

    setLoading(true);
    try {
      await identityApi.changePassword({ currentPassword, newPassword });
      setSuccess(true);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setTimeout(() => router.push('/profile'), 2000);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تغيير كلمة المرور.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="🔑 تغيير كلمة المرور"
        description="تحديث كلمة المرور الخاصة بحسابك"
        actions={
          <Link href="/profile">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة
            </Button>
          </Link>
        }
      />

      <div className="max-w-md mx-auto">
        <Card className="p-6">
          {error && (
            <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
              {error}
            </div>
          )}

          {success && (
            <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-center gap-2">
              <Check className="h-4 w-4" />
              <span>تم تغيير كلمة المرور بنجاح! سيتم توجيهك للملف الشخصي...</span>
            </div>
          )}

          <form onSubmit={onSubmit} className="space-y-4">
            <PasswordField
              label="كلمة المرور الحالية"
              value={currentPassword}
              onChange={setCurrentPassword}
              show={showCurrent}
              onToggleShow={() => setShowCurrent((v) => !v)}
              autoFocus
            />

            <PasswordField
              label="كلمة المرور الجديدة"
              value={newPassword}
              onChange={setNewPassword}
              show={showNew}
              onToggleShow={() => setShowNew((v) => !v)}
              hint="8 أحرف على الأقل"
            />

            <PasswordField
              label="تأكيد كلمة المرور الجديدة"
              value={confirmPassword}
              onChange={setConfirmPassword}
              show={showConfirm}
              onToggleShow={() => setShowConfirm((v) => !v)}
            />

            <div className="pt-2 flex gap-2">
              <Button type="submit" variant="primary" disabled={loading} className="flex-1">
                {loading ? 'جاري الحفظ...' : 'تغيير كلمة المرور'}
              </Button>
              <Link href="/profile">
                <Button type="button" variant="secondary">
                  إلغاء
                </Button>
              </Link>
            </div>
          </form>
        </Card>

        <div className="mt-4 text-xs text-gray-500 text-center">
          💡 نصيحة: استخدم كلمة مرور قوية تحتوي على أحرف وأرقام ورموز
        </div>
      </div>
    </div>
  );
}

function PasswordField({
  label,
  value,
  onChange,
  show,
  onToggleShow,
  hint,
  autoFocus,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  show: boolean;
  onToggleShow: () => void;
  hint?: string;
  autoFocus?: boolean;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      <div className="relative">
        <input
          type={show ? 'text' : 'password'}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          autoFocus={autoFocus}
          required
          className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
        <button
          type="button"
          onClick={onToggleShow}
          className="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 p-1"
          aria-label={show ? 'إخفاء' : 'إظهار'}
        >
          {show ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
        </button>
      </div>
      {hint && <p className="text-xs text-gray-500 mt-1">{hint}</p>}
    </div>
  );
}
