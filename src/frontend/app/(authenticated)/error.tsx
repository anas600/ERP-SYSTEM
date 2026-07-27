'use client';

// Global error boundary للصفحات المحمية (authenticated)
// يلتقط أي خطأ يحدث داخل شجرة الـ (authenticated) layout
// يظهر للمستخدم رسالة عربية ودية مع زرّي "إعادة المحاولة" و"العودة للرئيسية"

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { AlertTriangle, Home, RotateCw } from 'lucide-react';
import { Button, Card } from '@/components/ui';

export default function AuthenticatedError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const router = useRouter();

  useEffect(() => {
    // يمكن إرسال الخطأ لخدمة monitoring (Sentry مثلاً) في المستقبل
    console.error('Authenticated error boundary caught:', error);
  }, [error]);

  return (
    <div dir="rtl" className="py-8">
      <Card className="max-w-2xl mx-auto text-center py-10">
        <div className="inline-flex h-16 w-16 items-center justify-center rounded-full bg-red-50 text-red-600 mb-4">
          <AlertTriangle className="h-8 w-8" />
        </div>
        <h1 className="text-2xl font-bold text-gray-800 mb-2">حدث خطأ غير متوقع</h1>
        <p className="text-sm text-gray-500 mb-6 max-w-md mx-auto">
          عذراً، حدث خطأ أثناء تحميل هذه الصفحة. يمكنك إعادة المحاولة أو العودة للوحة التحكم.
        </p>

        {process.env.NODE_ENV !== 'production' && error.digest && (
          <div className="mb-6 mx-auto max-w-md">
            <p className="text-xs text-gray-400 mb-1">معرّف الخطأ (للمطوّر):</p>
            <p
              className="text-xs font-mono text-gray-500 bg-gray-50 border border-gray-200 rounded-md px-3 py-2 break-all"
              dir="ltr"
            >
              {error.digest}
            </p>
          </div>
        )}

        <div className="flex items-center justify-center gap-2 flex-wrap">
          <Button
            variant="primary"
            onClick={reset}
            iconLeft={<RotateCw className="h-4 w-4" />}
          >
            إعادة المحاولة
          </Button>
          <Button
            variant="secondary"
            onClick={() => router.push('/dashboard')}
            iconLeft={<Home className="h-4 w-4" />}
          >
            العودة للرئيسية
          </Button>
        </div>
      </Card>
    </div>
  );
}
