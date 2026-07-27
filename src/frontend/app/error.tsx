'use client';

// Root error boundary — يلتقط الأخطاء التي تحدث خارج (authenticated)
// مثال: خطأ في layout الجذر أو في صفحة login/register

import { useEffect } from 'react';

export default function RootError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // يمكن إرسال الخطأ لخدمة monitoring (Sentry مثلاً) في المستقبل
    console.error('Root error boundary caught:', error);
  }, [error]);

  return (
    <div
      dir="rtl"
      className="min-h-screen flex items-center justify-center bg-gray-50 p-6"
    >
      <div className="max-w-md w-full bg-white rounded-2xl shadow-md border border-gray-100 p-8 text-center">
        <div className="text-6xl mb-3">⚠️</div>
        <h1 className="text-xl font-bold text-gray-800 mb-2">حدث خطأ غير متوقع</h1>
        <p className="text-sm text-gray-500 mb-5">
          عذراً، حدث خطأ في التطبيق. حاول مرة أخرى.
        </p>
        {process.env.NODE_ENV !== 'production' && error.digest && (
          <p className="text-xs font-mono text-gray-400 mb-4 break-all" dir="ltr">
            digest: {error.digest}
          </p>
        )}
        <button
          onClick={reset}
          className="inline-flex items-center justify-center h-10 px-4 text-sm font-semibold rounded-lg bg-blue-600 text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-blue-500"
        >
          إعادة المحاولة
        </button>
      </div>
    </div>
  );
}
