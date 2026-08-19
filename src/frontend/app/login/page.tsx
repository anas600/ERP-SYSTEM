'use client';

// Sprint 39 (DEC-125) — Login page polish with design system overhaul
// - Brand gradient background
// - Glassmorphism card
// - Gradient logo + ERP-SYSTEM tagline
// - Form uses Input + Button components
// - Demo creds hint for quick dev login

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { LogIn, Mail, Lock, AlertCircle, Sparkles } from 'lucide-react';
import { Button, Input, useToast } from '@/components/ui';
import { authApi, getErrorMessage } from '@/lib/api';

export default function LoginPage() {
  const router = useRouter();
  const toast = useToast();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await authApi.login({ email, password });
      toast.success('تم تسجيل الدخول بنجاح');
      router.push('/dashboard');
    } catch (err: unknown) {
      const msg = getErrorMessage(err, 'فشل تسجيل الدخول - تحقق من البيانات');
      setError(msg);
      setLoading(false);
    }
  };

  return (
    <main
      className="min-h-screen flex items-center justify-center relative overflow-hidden bg-gradient-to-br from-brand-50 via-white to-brand-100 p-6"
      dir="rtl"
    >
      {/* Decorative blurred shapes for glassmorphism feel */}
      <div className="absolute top-0 right-0 w-72 h-72 bg-brand-400/20 rounded-full blur-3xl" aria-hidden="true" />
      <div className="absolute bottom-0 left-0 w-96 h-96 bg-brand-300/15 rounded-full blur-3xl" aria-hidden="true" />
      <div className="absolute top-1/3 left-1/4 w-48 h-48 bg-brand-500/10 rounded-full blur-2xl" aria-hidden="true" />

      <div className="relative w-full max-w-md">
        {/* Brand header */}
        <div className="text-center mb-6 animate-fade-in">
          <div className="inline-flex items-center justify-center h-16 w-16 rounded-2xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-soft-lg mb-4">
            <Sparkles className="h-8 w-8" />
          </div>
          <h1 className="text-3xl font-bold text-ink-800">ERP-SYSTEM</h1>
          <p className="text-sm text-ink-500 mt-1">نظام إدارة الشركات المتعددة</p>
        </div>

        {/* Card with glassmorphism */}
        <div className="bg-white/80 backdrop-blur-xl rounded-2xl shadow-soft-xl border border-white/60 p-8 animate-slide-up">
          <h2 className="text-lg font-bold text-ink-800 mb-1">تسجيل الدخول</h2>
          <p className="text-sm text-ink-500 mb-5">أدخل بياناتك للمتابعة</p>

          {error && (
            <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2 animate-slide-down">
              <AlertCircle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          <form onSubmit={onSubmit} className="space-y-4">
            <Input
              label="البريد الإلكتروني"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              iconLeft={<Mail className="h-4 w-4" />}
              required
              autoComplete="email"
              data-testid="email"
            />
            <Input
              label="كلمة المرور"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              iconLeft={<Lock className="h-4 w-4" />}
              required
              autoComplete="current-password"
              data-testid="password"
            />
            <Button
              type="submit"
              variant="primary"
              size="lg"
              loading={loading}
              iconLeft={<LogIn className="h-4 w-4" />}
              fullWidth
            >
              {loading ? 'جاري الدخول...' : 'دخول'}
            </Button>
          </form>

          <div className="mt-6 space-y-1.5 text-center text-sm">
            <p className="text-ink-600">
              ليس لديك حساب؟{' '}
              <Link href="/register" className="text-brand-600 font-semibold hover:underline">
                إنشاء حساب جديد
              </Link>
            </p>
            <p>
              <Link href="/login/forgot" className="text-brand-600 hover:underline">
                نسيت كلمة المرور؟
              </Link>
            </p>
            <p>
              <Link href="/" className="text-ink-500 hover:text-ink-700 hover:underline">
                ← العودة للرئيسية
              </Link>
            </p>
          </div>
        </div>

        <p className="text-center text-xs text-ink-400 mt-6">
          © {new Date().getFullYear()} ERP-SYSTEM · v1.0.12
        </p>
      </div>
    </main>
  );
}
