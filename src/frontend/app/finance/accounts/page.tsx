'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authApi, financeApi, Account, ACCOUNT_TYPES } from '@/lib/api';

export default function AccountsPage() {
  const router = useRouter();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<string>('');

  useEffect(() => {
    if (!authApi.isLoggedIn()) { router.push('/login'); return; }
    load();
  }, [router]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await financeApi.listAccounts();
      setAccounts(data);
    } catch (e: any) {
      setError(e?.response?.data?.detail || 'فشل التحميل');
    } finally { setLoading(false); }
  };

  const filtered = accounts.filter(a =>
    !filter || a.code.includes(filter) || a.name.includes(filter)
  );

  return (
    <div className="min-h-screen" dir="rtl">
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Link href="/dashboard" className="text-gray-500 hover:text-gray-700">← رجوع</Link>
            <h1 className="text-2xl font-bold text-gray-800">💰 Chart of Accounts</h1>
          </div>
          <input
            type="text"
            placeholder="🔍 بحث..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="border rounded-lg px-3 py-1.5 text-sm w-48"
          />
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        ) : filtered.length === 0 ? (
          <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
            لا توجد حسابات. الحسابات الافتراضية تُنشأ تلقائياً عند register.
            <br />
            <button onClick={load} className="mt-3 text-blue-600 hover:underline">إعادة المحاولة</button>
          </div>
        ) : (
          <div className="bg-white rounded-xl shadow-sm overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-600">الكود</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-600">الاسم</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-600">النوع</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-600">الرصيد الطبيعي</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-600">نشط</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((a) => (
                  <tr key={a.id} className="border-b hover:bg-gray-50">
                    <td className="px-4 py-3 font-mono text-sm text-blue-600">{a.code}</td>
                    <td className="px-4 py-3 text-sm">{a.name}</td>
                    <td className="px-4 py-3 text-sm">
                      <span className="bg-blue-50 text-blue-700 px-2 py-0.5 rounded text-xs">
                        {ACCOUNT_TYPES[a.type] || a.type}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm">
                      {a.normalBalance === 1 ? 'مدين' : 'دائن'}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      {a.isActive ? '✅' : '❌'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="bg-gray-50 px-4 py-2 text-xs text-gray-500">
              {filtered.length} حساب
            </div>
          </div>
        )}
      </main>
    </div>
  );
}
