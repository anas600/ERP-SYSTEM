'use client';

// سجل التدقيق (Audit Log) — مع filters + CSV export + SkeletonTable + EmptyState

import { useEffect, useMemo, useState } from 'react';
import { Download, Filter, RefreshCw, Shield } from 'lucide-react';
import {
  Badge,
  Button,
  EmptyState,
  PageHeader,
  Select,
  SkeletonTable,
  useToast,
} from '@/components/ui';
import { Table, type TableColumn } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage, identityApi, type AdminUser } from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

interface AuditEntry {
  id: number;
  companyId?: string | null;
  entityType: string;
  entityId?: string | null;
  action: string;
  userId?: string | null;
  changes?: string | null;
  ipAddress?: string | null;
  createdAt: string;
}

interface AuditSummary {
  entityType: string;
  cnt: number;
}

const ACTION_VARIANTS: Record<string, 'success' | 'info' | 'danger' | 'warning' | 'neutral'> = {
  CREATE: 'success',
  UPDATE: 'info',
  DELETE: 'danger',
  READ: 'neutral',
  APPROVE: 'success',
  REJECT: 'danger',
  POST: 'info',
  REVERSE: 'warning',
  CANCEL: 'danger',
};

const ACTION_LABELS: Record<string, string> = {
  CREATE: 'إنشاء',
  UPDATE: 'تعديل',
  DELETE: 'حذف',
  READ: 'قراءة',
  APPROVE: 'موافقة',
  REJECT: 'رفض',
  POST: 'ترحيل',
  REVERSE: 'عكس',
  CANCEL: 'إلغاء',
};

const TAKE = 50;

function toCsv(rows: AuditEntry[], users: AdminUser[]): string {
  const userById = new Map(users.map((u) => [u.id, u]));
  const header = [
    'id',
    'createdAt',
    'userEmail',
    'action',
    'entityType',
    'entityId',
    'ipAddress',
    'changes',
  ];
  const escape = (v: unknown): string => {
    if (v == null) return '';
    const s = String(v);
    if (s.includes(',') || s.includes('"') || s.includes('\n')) {
      return `"${s.replace(/"/g, '""')}"`;
    }
    return s;
  };
  const body = rows.map((r) => [
    r.id,
    r.createdAt,
    userById.get(r.userId ?? '')?.email ?? r.userId ?? '',
    r.action,
    r.entityType,
    r.entityId ?? '',
    r.ipAddress ?? '',
    r.changes ?? '',
  ].map(escape).join(','));
  return [header.join(','), ...body].join('\n');
}

function downloadCsv(filename: string, csv: string) {
  // BOM في بداية الملف حتى Excel يفتح UTF-8 بشكل صحيح
  const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

export default function AuditPage() {
  const { loading: authLoading } = useAuth();
  const toast = useToast();

  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [summary, setSummary] = useState<AuditSummary[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // الفلاتر
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [userId, setUserId] = useState('');
  const [action, setAction] = useState('');
  const [entityType, setEntityType] = useState('');

  const [skip, setSkip] = useState(0);

  useEffect(() => {
    if (authLoading) return;
    void loadUsers();
    void load({ skip: 0 });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const loadUsers = async () => {
    try {
      const res = await identityApi.listUsers(0, 200);
      setUsers(res.items);
    } catch {
      // users list is optional — if it fails we just show user ids
      setUsers([]);
    }
  };

  const load = async (opts?: { skip?: number; silent?: boolean }) => {
    const nextSkip = opts?.skip ?? skip;
    if (!opts?.silent) setLoading(true);
    setError(null);
    try {
      const params: Record<string, string | number> = { skip: nextSkip, take: TAKE };
      if (from) params.fromDate = from;
      if (to) params.toDate = to;
      if (userId) params.userId = userId;
      if (action) params.action = action;
      if (entityType) params.entityType = entityType;

      const [listRes, sumRes] = await Promise.all([
        api.get<AuditEntry[]>('/api/audit', { params }),
        // ملخص فقط في الـ load الأول — يقلل الطلبات
        nextSkip === 0
          ? api.get<AuditSummary[]>('/api/audit/summary').catch(() => ({ data: [] as AuditSummary[] }))
          : Promise.resolve({ data: [] as AuditSummary[] }),
      ]);
      setEntries(listRes.data);
      if (nextSkip === 0) setSummary(sumRes.data);
      setSkip(nextSkip);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل سجل التدقيق.'));
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = () => {
    void load({ skip: 0 });
  };

  const clearFilters = () => {
    setFrom('');
    setTo('');
    setUserId('');
    setAction('');
    setEntityType('');
    void load({ skip: 0 });
  };

  const onExport = () => {
    if (entries.length === 0) {
      toast.info('لا توجد بيانات للتصدير.');
      return;
    }
    const csv = toCsv(entries, users);
    const ts = new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-');
    downloadCsv(`audit-log-${ts}.csv`, csv);
    toast.success(`تم تصدير ${entries.length} سجل.`);
  };

  const userById = useMemo(
    () => new Map(users.map((u) => [u.id, u])),
    [users]
  );

  const hasMore = entries.length === TAKE;

  const columns: TableColumn<AuditEntry>[] = [
    {
      key: 'id',
      header: '#',
      render: (e) => <span className="text-xs text-gray-500 font-mono">#{e.id}</span>,
      className: 'w-16',
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
      key: 'user',
      header: 'المستخدم',
      render: (e) => {
        const u = e.userId ? userById.get(e.userId) : null;
        if (u) {
          return (
            <div>
              <div className="text-sm text-gray-800">{u.fullName || u.email}</div>
              <div className="text-xs text-gray-400">{u.email}</div>
            </div>
          );
        }
        return <span className="font-mono text-xs text-gray-400">{e.userId?.slice(0, 8) ?? '—'}</span>;
      },
    },
    {
      key: 'action',
      header: 'الإجراء',
      render: (e) => (
        <Badge variant={ACTION_VARIANTS[e.action] ?? 'neutral'}>
          {ACTION_LABELS[e.action] ?? e.action}
        </Badge>
      ),
      className: 'w-24',
    },
    {
      key: 'entityType',
      header: 'الكيان',
      render: (e) => <span className="font-mono text-xs">{e.entityType}</span>,
    },
    {
      key: 'entityId',
      header: 'معرّف الكيان',
      render: (e) => (
        <span className="font-mono text-xs text-gray-500" title={e.entityId ?? ''}>
          {e.entityId ? e.entityId.slice(0, 8) + '…' : '—'}
        </span>
      ),
    },
    {
      key: 'ip',
      header: 'IP',
      render: (e) => <span className="font-mono text-xs">{e.ipAddress ?? '—'}</span>,
      className: 'w-32',
    },
    {
      key: 'changes',
      header: 'التفاصيل',
      render: (e) =>
        e.changes ? (
          <details className="text-xs">
            <summary className="cursor-pointer text-blue-600">عرض</summary>
            <pre className="mt-1 p-2 bg-gray-50 rounded text-xs overflow-x-auto max-w-md font-mono whitespace-pre-wrap break-all">
              {e.changes}
            </pre>
          </details>
        ) : (
          <span className="text-gray-400">—</span>
        ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="🛡️ سجل التدقيق"
        description="Audit Log — كل العمليات على النظام مسجلة"
        actions={
          <div className="flex items-center gap-2">
            <Button onClick={() => load({ silent: true })} variant="secondary" disabled={loading} iconLeft={<RefreshCw className="h-4 w-4" />}>
              تحديث
            </Button>
            <Button onClick={onExport} variant="primary" disabled={loading || entries.length === 0} iconLeft={<Download className="h-4 w-4" />}>
              تصدير CSV
            </Button>
          </div>
        }
      />

      {/* Summary cards */}
      {!loading && summary.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2 mb-4">
          {summary.slice(0, 6).map((s) => (
            <div key={s.entityType} className="bg-white rounded-lg shadow-sm p-3 text-center">
              <div className="text-2xl font-bold text-blue-600">{s.cnt}</div>
              <div className="text-xs text-gray-500 mt-1 truncate" title={s.entityType}>{s.entityType}</div>
            </div>
          ))}
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-xl shadow-sm p-4 mb-4">
        <div className="flex items-center gap-2 text-sm text-gray-600 mb-3">
          <Filter className="h-4 w-4" />
          <span className="font-semibold">الفلاتر</span>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-5 gap-3">
          <div>
            <label className="block text-xs text-gray-500 mb-1">من تاريخ</label>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-500 mb-1">إلى تاريخ</label>
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <Select
            label="المستخدم"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
            options={[
              { label: '— الكل —', value: '' },
              ...users.map((u) => ({ label: u.email, value: u.id })),
            ]}
            containerClassName="md:col-span-1"
          />
          <Select
            label="الإجراء"
            value={action}
            onChange={(e) => setAction(e.target.value)}
            options={[
              { label: '— الكل —', value: '' },
              { label: 'CREATE', value: 'CREATE' },
              { label: 'UPDATE', value: 'UPDATE' },
              { label: 'DELETE', value: 'DELETE' },
              { label: 'APPROVE', value: 'APPROVE' },
              { label: 'REJECT', value: 'REJECT' },
              { label: 'POST', value: 'POST' },
              { label: 'CANCEL', value: 'CANCEL' },
            ]}
          />
          <div>
            <label className="block text-xs text-gray-500 mb-1">نوع الكيان</label>
            <input
              type="text"
              placeholder="مثال: Vendor, JournalEntry"
              value={entityType}
              onChange={(e) => setEntityType(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
        </div>
        <div className="mt-3 flex gap-2">
          <Button onClick={applyFilters} variant="primary" size="sm" disabled={loading} iconLeft={<Filter className="h-3 w-3" />}>
            تطبيق الفلاتر
          </Button>
          <Button onClick={clearFilters} variant="secondary" size="sm" disabled={loading}>
            مسح
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4 text-red-700 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={6} cols={6} />
      ) : entries.length === 0 ? (
        <EmptyState
          icon={<Shield className="h-12 w-12" />}
          title="لا توجد سجلات"
          description="لم يتم العثور على سجلات تدقيق تطابق الفلاتر."
        />
      ) : (
        <>
          <Table
            data={entries}
            loading={false}
            rowKey={(e) => String(e.id)}
            columns={columns}
            emptyMessage="لا توجد سجلات"
          />

          {/* Pagination */}
          <div className="mt-4 flex items-center justify-center gap-2">
            <Button
              onClick={() => load({ skip: Math.max(0, skip - TAKE) })}
              variant="secondary"
              size="sm"
              disabled={loading || skip === 0}
            >
              السابق
            </Button>
            <span className="px-3 py-2 text-sm text-gray-600">
              {skip + 1} - {skip + entries.length}
            </span>
            <Button onClick={() => load({ skip: skip + TAKE })} variant="secondary" size="sm" disabled={loading || !hasMore}>
              التالي
            </Button>
          </div>
        </>
      )}
    </div>
  );
}
