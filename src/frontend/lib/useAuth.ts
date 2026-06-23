'use client';

// Hook موحد للتحقق من الـ authentication
// يعيد redirect إلى /login إن لم يكن المستخدم مسجلاً دخوله

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { authApi, UserInfo } from '@/lib/api';

export interface UseAuthResult {
  /** الـ user الحالي (null أثناء التحميل أو غير مسجل) */
  user: UserInfo | null;
  /** هل الصفحة لا تزال تتحقق من الـ session؟ */
  loading: boolean;
}

export function useAuth(): UseAuthResult {
  const router = useRouter();
  const [user, setUser] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!authApi.isLoggedIn()) {
      router.push('/login');
      setLoading(false);
      return;
    }
    setUser(authApi.getUser());
    setLoading(false);
  }, [router]);

  return { user, loading };
}
