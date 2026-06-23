'use client';

// صفحة دليل الحسابات (Chart of Accounts) — قائمة الحسابات مع فلترة

import { useEffect, useState } from 'react';
import { Input, Table, Badge, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { financeApi, Account, ACCOUNT_TYPES } from '@/lib/api';

export default function AccountsPage() {
  const { loading: authLoading } = useAuth();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<string>('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await financeApi.listAccounts();
      setAccounts(data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { detail?: string } } };
      setError(err?.response?.data?.detail || 'فشل التحميل');
    } finally {
      setLoading(false);
    }
  };

  const filtered = accounts.filter(
    (a) => !filter || a.code.includes(filter) || a.name.includes(filter)
  );

  return (
    <div>
      <PageHeader
        title="💰 دليل الحسابات"
        description="شجرة الحسابات المحاسبية الأساسية"
        actions={
          <Input
            placeholder="🔍 بحث (كود/اسم)..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            containerClassName="w-64"
          />
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      <Table
        columns={[
          {
            key: 'code',
            header: 'الكود',
            render: (a) => <span className="font-mono text-blue-600">{a.code}</span>,
          },
          { key: 'name', header: 'الاسم', render: (a) => a.name },
          {
            key: 'type',
            header: 'النوع',
            render: (a) => (
              <Badge variant="info">{ACCOUNT_TYPES[a.type] || a.type}</Badge>
            ),
          },
          {
            key: 'normalBalance',
            header: 'الرصيد الطبيعي',
            render: (a) => (a.normalBalance === 1 ? 'مدين' : 'دائن'),
          },
          {
            key: 'isActive',
            header: 'نشط',
            align: 'center',
            render: (a) => (a.isActive ? '✅' : '❌'),
          },
        ]}
        data={filtered}
        loading={loading}
        rowKey={(a) => a.id}
        emptyMessage={
          <div>
            لا توجد حسابات. الحسابات الافتراضية تُنشأ تلقائياً عند الـ Register.
            <br />
            <button onClick={load} className="mt-3 text-blue-600 hover:underline">
              إعادة المحاولة
            </button>
          </div>
        }
      />

      {!loading && filtered.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">
          {filtered.length} حساب
        </p>
      )}
    </div>
  );
}
