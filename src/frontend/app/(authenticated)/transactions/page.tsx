'use client';

// Sprint 11 (T1) — Recent transactions hub page.
//
// Top-level "/transactions" page that lists the most-recent journal
// transactions in the new Sprint 11 DTO shape. Complements the existing
// /finance/journal-entries page (which lists full journal entries with
// multiple lines) by providing a single-line feed view focused on demo
// readability.
//
// Contract:
//   GET /api/transactions/recent?limit=N → TransactionDto[]
//
// The page degrades gracefully if the BE endpoint isn't wired yet.

import { useEffect, useMemo, useState } from 'react';
import {
  ArrowRightLeft,
  RefreshCw,
  AlertCircle,
  Filter,
  TrendingUp,
  TrendingDown,
  Calendar,
  Hash,
} from 'lucide-react';
import {
  Card,
  PageHeader,
  Button,
  Input,
  Select,
  EmptyState,
  Badge,
} from '@/components/ui';
import { Table, type TableColumn } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getRecentTransactions, getErrorMessage } from '@/lib/api';
import type { TransactionDto } from '@/lib/api-types';
import { formatDateTime, formatCurrency } from '@/lib/utils';

const LIMIT_OPTIONS = [
  { value: '25', label: '25 معاملة' },
  { value: '50', label: '50 معاملة' },
  { value: '100', label: '100 معاملة' },
  { value: '200', label: '200 معاملة' },
];

type SideFilter = 'all' | 'debit' | 'credit';

export default function TransactionsHubPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<TransactionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [sideFilter, setSideFilter] = useState<SideFilter>('all');
  const [limit, setLimit] = useState(50);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading, limit]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getRecentTransactions(limit);
      setItems(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل المعاملات.'));
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  // Filtered list (search + side filter)
  const filtered = useMemo(() => {
    return items.filter((t) => {
      if (sideFilter === 'debit' && !(Number(t.debit) > 0)) return false;
      if (sideFilter === 'credit' && !(Number(t.credit) > 0)) return false;
      if (search.trim()) {
        const q = search.trim().toLowerCase();
        return (
          (t.description ?? '').toLowerCase().includes(q) ||
          (t.accountName ?? '').toLowerCase().includes(q) ||
          (t.accountCode ?? '').toLowerCase().includes(q) ||
          (t.reference ?? '').toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [items, search, sideFilter]);

  // Summary stats
  const stats = useMemo(() => {
    let totalDebit = 0;
    let totalCredit = 0;
    let debitCount = 0;
    let creditCount = 0;
    for (const t of items) {
      const d = Number(t.debit) || 0;
      const c = Number(t.credit) || 0;
      if (d > 0) {
        totalDebit += d;
        debitCount++;
      }
      if (c > 0) {
        totalCredit += c;
        creditCount++;
      }
    }
    return { totalDebit, totalCredit, debitCount, creditCount, count: items.length };
  }, [items]);

  const columns: TableColumn<TransactionDto>[] = [
    {
      key: 'createdAt',
      header: 'التاريخ',
      render: (t) => (
        <div className="flex items-center gap-1 text-xs text-gray-700">
          <Calendar className="h-3 w-3 text-gray-400" />
          <span>{formatDateTime(t.createdAt)}</span>
        </div>
      ),
      className: 'w-40',
    },
    {
      key: 'account',
      header: 'الحساب',
      render: (t) => (
        <div>
          <div className="text-sm text-gray-800">{t.accountName || '—'}</div>
          {t.accountCode && (
            <div className="text-xs text-gray-500 font-mono flex items-center gap-1">
              <Hash className="h-2.5 w-2.5" /> {t.accountCode}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'description',
      header: 'الوصف',
      render: (t) => (
        <span className="text-sm text-gray-700 truncate block max-w-md">
          {t.description || '—'}
        </span>
      ),
    },
    {
      key: 'reference',
      header: 'المرجع',
      render: (t) =>
        t.reference ? (
          <span className="font-mono text-xs text-gray-600">{t.reference}</span>
        ) : (
          <span className="text-xs text-gray-400">—</span>
        ),
      className: 'w-28',
    },
    {
      key: 'debit',
      header: 'مدين',
      render: (t) =>
        Number(t.debit) > 0 ? (
          <span className="text-green-700 font-mono text-sm font-semibold">
            +{Number(t.debit).toLocaleString('en', { minimumFractionDigits: 2 })}
          </span>
        ) : (
          <span className="text-xs text-gray-400">—</span>
        ),
      className: 'w-28 text-left',
    },
    {
      key: 'credit',
      header: 'دائن',
      render: (t) =>
        Number(t.credit) > 0 ? (
          <span className="text-red-700 font-mono text-sm font-semibold">
            −{Number(t.credit).toLocaleString('en', { minimumFractionDigits: 2 })}
          </span>
        ) : (
          <span className="text-xs text-gray-400">—</span>
        ),
      className: 'w-28 text-left',
    },
  ];

  return (
    <div>
      <PageHeader
        title="🔁 المعاملات الأخيرة"
        description="Recent Transactions — آخر القيود في دفتر الأستاذ (Sprint 11 T1)"
        actions={
          <Button
            variant="secondary"
            onClick={load}
            disabled={loading}
            iconLeft={<RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />}
          >
            تحديث
          </Button>
        }
      />

      {/* Error banner */}
      {error && (
        <div
          className="bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل المعاملات</p>
            <p className="text-sm mt-0.5">{error}</p>
            <p className="text-xs mt-1 text-amber-700">
              ملاحظة: قد يكون الـ endpoint الجديد <code>/api/transactions/recent</code> غير مُفعَّل بعد.
            </p>
          </div>
          <Button variant="secondary" onClick={load} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {/* Summary stats */}
      {!loading && items.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
          <Card>
            <div className="flex items-center gap-2">
              <ArrowRightLeft className="h-4 w-4 text-blue-500" />
              <p className="text-xs text-gray-500">عدد المعاملات</p>
            </div>
            <p className="text-2xl font-bold text-blue-600 mt-1">{stats.count}</p>
          </Card>
          <Card>
            <div className="flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-green-500" />
              <p className="text-xs text-gray-500">إجمالي المدين</p>
            </div>
            <p className="text-lg font-bold text-green-700 mt-1 tabular-nums">
              {formatCurrency(stats.totalDebit)}
            </p>
            <p className="text-xs text-gray-400 mt-0.5">{stats.debitCount} عملية</p>
          </Card>
          <Card>
            <div className="flex items-center gap-2">
              <TrendingDown className="h-4 w-4 text-red-500" />
              <p className="text-xs text-gray-500">إجمالي الدائن</p>
            </div>
            <p className="text-lg font-bold text-red-700 mt-1 tabular-nums">
              {formatCurrency(stats.totalCredit)}
            </p>
            <p className="text-xs text-gray-400 mt-0.5">{stats.creditCount} عملية</p>
          </Card>
          <Card>
            <div className="flex items-center gap-2">
              <Badge variant={stats.totalDebit === stats.totalCredit ? 'success' : 'warning'}>
                {stats.totalDebit === stats.totalCredit ? 'متوازن' : 'غير متوازن'}
              </Badge>
              <p className="text-xs text-gray-500">الفرق</p>
            </div>
            <p className="text-lg font-bold text-gray-700 mt-1 tabular-nums">
              {formatCurrency(Math.abs(stats.totalDebit - stats.totalCredit))}
            </p>
          </Card>
        </div>
      )}

      {/* Filters */}
      <Card className="mb-4">
        <div className="flex items-center gap-2 mb-3">
          <Filter className="h-4 w-4 text-gray-500" />
          <span className="text-sm font-medium text-gray-700">فلاتر</span>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3" dir="rtl">
          <Input
            label="بحث"
            placeholder="وصف / حساب / مرجع..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            containerClassName="md:col-span-1"
          />
          <Select
            label="الجانب"
            value={sideFilter}
            onChange={(e) => setSideFilter(e.target.value as SideFilter)}
            options={[
              { value: 'all', label: 'الكل' },
              { value: 'debit', label: 'مدين فقط' },
              { value: 'credit', label: 'دائن فقط' },
            ]}
          />
          <Select
            label="عدد النتائج"
            value={String(limit)}
            onChange={(e) => setLimit(Number(e.target.value))}
            options={LIMIT_OPTIONS}
          />
        </div>
      </Card>

      {/* Table */}
      {loading ? (
        <Card>
          <div className="space-y-2">
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className="h-9 bg-gray-100 rounded animate-pulse" />
            ))}
          </div>
        </Card>
      ) : filtered.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ArrowRightLeft className="h-12 w-12" />}
            title="لا توجد معاملات"
            description={
              search
                ? `لا توجد معاملات تطابق "${search}".`
                : 'لم يتم تسجيل أي معاملة بعد.'
            }
          />
        </Card>
      ) : (
        <>
          <Table
            data={filtered}
            loading={false}
            rowKey={(t) => t.id}
            columns={columns}
            emptyMessage="لا توجد معاملات"
          />
          <div className="mt-2 text-xs text-gray-500 text-center">
            عرض {filtered.length} من {items.length} معاملة
          </div>
        </>
      )}
    </div>
  );
}
