'use client';

// Sprint 49 — إعادة توجيه لصفحة ميزان المراجعة الموجودة
// (الصفحة الكاملة في /finance/trial-balance — تحتوي UI غني)

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export default function TrialBalanceRedirect() {
  const router = useRouter();
  useEffect(() => { router.replace('/finance/trial-balance'); }, [router]);
  return (
    <div className="text-center py-12 text-gray-500">
      <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
      <p className="mt-3 text-sm">جاري التحويل إلى ميزان المراجعة...</p>
    </div>
  );
}
