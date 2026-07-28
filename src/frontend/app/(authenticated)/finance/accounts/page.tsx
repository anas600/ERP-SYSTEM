'use client';

// صفحة دليل الحسابات (Chart of Accounts) — قائمة الحسابات مع فلترة

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Plus, Pencil, BookOpen } from 'lucide-react';
import { Input, Table, Badge, PageHeader, Button, EmptyState } from '@/components/ui';
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
          <div className="flex items-center gap-2">
            <Link href="/finance/accounts/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>حساب جديد</Button>
            </Link>
            <Input
              placeholder="🔍 بحث (كود/اسم)..."
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              containerClassName="w-64"
            />
          </div>
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
          {
            key: 'actions',
            header: '',
            render: (a) => (
              <Link href={`/finance/accounts/${a.id}/edit`}>
                <Button variant="ghost" size="sm" iconLeft={<Pencil className="h-3 w-3" />} />
              </Link>
            ),
          },
        ]}
        data={filtered}
        loading={loading}
        rowKey={(a) => a.id}
        emptyMessage={
          <EmptyState
            icon={<BookOpen className="h-12 w-12" />}
            title={{ ar: 'لا توجد حسابات', en: 'No accounts yet' }}
            description={{
              ar: 'دليل الحسابات فارغ. الحسابات الافتراضية تُنشأ تلقائياً عند تسجيل أول شركة.',
              en: 'The chart of accounts is empty. Default accounts are created automatically on first company registration.',
            }}
            action={
              <Button variant="primary" onClick={load}>
                إعادة المحاولة
              </Button>
            }
          />
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
