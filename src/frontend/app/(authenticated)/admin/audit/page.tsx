'use client';

import { useEffect, useState } from 'react';
import { formatDate, formatTime } from '@/lib/utils';
import { useAuth } from '@/lib/useAuth';
import { Button, Table, Badge, PageHeader } from '@/components/ui';
import { api, getErrorMessage } from '@/lib/api';

interface AuditEntry {
  id: number;
  companyId?: string;
  entityType: string;
  entityId?: string;
  action: string;
  userId?: string;
  changes?: string;
  ipAddress?: string;
  createdAt: string;
}

interface AuditSummary {
  entityType: string;
  cnt: number;
}

const ACTION_VARIANTS: Record<string, string> = {
  CREATE: 'success',
  UPDATE: 'info',
  DELETE: 'danger',
  READ: 'neutral',
  APPROVE: 'success',
  POST: 'info',
  REVERSE: 'warning',
};

export default function AuditPage() {
  const { loading: authLoading, user } = useAuth();
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [summary, setSummary] = useState<AuditSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState({
    entityType: '',
    action: '',
    fromDate: '',
    toDate: '',
  });
  const [skip, setSkip] = useState(0);
  const take = 50;

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, skip]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const params: Record<string, unknown> = { skip, take };
      if (filters.entityType) params.entityType = filters.entityType;
      if (filters.action) params.action = filters.action;
      if (filters.fromDate) params.fromDate = filters.fromDate;
      if (filters.toDate) params.toDate = filters.toDate;
      const data = await api.get<AuditEntry[]>('/api/audit', { params });
      setEntries(data.data);
      const sum = await api.get<AuditSummary[]>('/api/audit/summary');
      setSummary(sum.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل سجل التدقيق.'));
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    setSkip(0);
    load();
  };

  return (
    <div>
      <PageHeader
        title="🛡️ سجل التدقيق"
        description="Audit Log — كل العمليات على النظام مسجلة"
        actions={
          <Button onClick={load} variant="secondary" disabled={loading}>
            تحديث
          </Button>
        }
      />

      {/* Summary */}
      {!loading && summary.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2 mb-4">
          {summary.slice(0, 6).map((s) => (
            <div key={s.entityType} className="bg-white rounded-lg shadow-sm p-3 text-center">
              <div className="text-2xl font-bold text-blue-600">{s.cnt}</div>
              <div className="text-xs text-gray-500 mt-1">{s.entityType}</div>
            </div>
          ))}
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <input
            type="text"
            placeholder="نوع الكيان (Vendor, JournalEntry...)"
            value={filters.entityType}
            onChange={(e) => setFilters({ ...filters, entityType: e.target.value })}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
          />
          <input
            type="text"
            placeholder="الإجراء (CREATE, UPDATE...)"
            value={filters.action}
            onChange={(e) => setFilters({ ...filters, action: e.target.value })}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
          />
          <input
            type="date"
            value={filters.fromDate}
            onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
          />
          <input
            type="date"
            value={filters.toDate}
            onChange={(e) => setFilters({ ...filters, toDate: e.target.value })}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm"
          />
        </div>
        <div className="mt-3 flex gap-2">
          <Button onClick={applyFilters} variant="primary" size="sm" disabled={loading}>
            تطبيق الفلاتر
          </Button>
          <Button onClick={() => { setFilters({ entityType: '', action: '', fromDate: '', toDate: '' }); setSkip(0); }} variant="secondary" size="sm">
            مسح
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4 text-red-700">
          {error}
        </div>
      )}

      <Table
        data={entries}
        loading={loading}
        rowKey={(e) => String(e.id)}
        columns={[
          {
            key: 'id',
            header: '#',
            render: (e) => <span className="text-xs text-gray-500 font-mono">{e.id}</span>,
            className: 'w-12',
          },
          {
            key: 'createdAt',
            header: 'التاريخ',
            render: (e) => (
              <div>
                <div className="text-sm">{formatDate(e.createdAt)}</div>
                <div className="text-xs text-gray-400">{formatTime(e.createdAt)}</div>
              </div>
            ),
            className: 'w-40',
          },
          {
            key: 'entityType',
            header: 'الكيان',
            render: (e) => <span className="font-mono text-xs">{e.entityType}</span>,
          },
          {
            key: 'action',
            header: 'الإجراء',
            render: (e) => (
              <Badge variant={(ACTION_VARIANTS[e.action] ?? 'neutral') as any}>
                {e.action}
              </Badge>
            ),
            className: 'w-24',
          },
          {
            key: 'userId',
            header: 'المستخدم',
            render: (e) => <span className="font-mono text-xs text-gray-500">{e.userId?.slice(0, 8) ?? '—'}</span>,
          },
          {
            key: 'ip',
            header: 'IP',
            render: (e) => <span className="font-mono text-xs">{e.ipAddress ?? '—'}</span>,
          },
          {
            key: 'changes',
            header: 'التغييرات',
            render: (e) => e.changes ? (
              <details className="text-xs">
                <summary className="cursor-pointer text-blue-600">عرض</summary>
                <pre className="mt-1 p-2 bg-gray-50 rounded text-xs overflow-x-auto max-w-md">{e.changes}</pre>
              </details>
            ) : <span className="text-gray-400">—</span>,
          },
        ]}
        emptyMessage="لا توجد سجلات تدقيق."
      />

      {/* Pagination */}
      {entries.length === take && (
        <div className="mt-4 flex gap-2 justify-center">
          <Button onClick={() => setSkip(Math.max(0, skip - take))} variant="secondary" size="sm" disabled={skip === 0}>
            السابق
          </Button>
          <span className="px-3 py-2 text-sm text-gray-600">
            {skip + 1} - {skip + entries.length}
          </span>
          <Button onClick={() => setSkip(skip + take)} variant="secondary" size="sm">
            التالي
          </Button>
        </div>
      )}
    </div>
  );
}
