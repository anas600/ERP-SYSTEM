'use client';

// صفحة الإجازات (Leaves) — قائمة + Bulk Approve/Reject + Status filter

import { useEffect, useMemo, useState } from 'react';
import { formatDate, formatTime } from '@/lib/utils';
import Link from 'next/link';
import { Check, CheckCheck, Filter, Plus, X, XCircle } from 'lucide-react';
import {
  Badge,
  Button,
  Card,
  ConfirmDialog,
  EmptyState,
  PageHeader,
  Select,
  SkeletonTable,
  useToast,
} from '@/components/ui';
import { Table, type TableColumn } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import {
  hrApi,
  LeaveRequest,
  LEAVE_TYPES,
  LEAVE_STATUSES,
  LEAVE_STATUS_VARIANTS,
  getErrorMessage,
} from '@/lib/api';

type StatusFilter = 'all' | '1' | '2' | '3';

export default function LeavesPage() {
  const toast = useToast();
  const { loading: authLoading, user } = useAuth();

  const [leaves, setLeaves] = useState<LeaveRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [actionId, setActionId] = useState<string | null>(null);
  const [bulkBusy, setBulkBusy] = useState(false);

  const [bulkConfirm, setBulkConfirm] = useState<'approve' | 'reject' | 'approveAllMonth' | null>(null);

  // هل المستخدم الحالي مدير (يستطيع Approve)؟
  const isManager = user?.roles?.some((r) => ['Admin', 'HRManager'].includes(r)) ?? false;

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await hrApi.listLeaves();
      setLeaves(data);
      // عند إعادة التحميل، أزل أي معرّفات لم تعد ظاهرة
      setSelectedIds((prev) => {
        const visible = new Set(data.map((l) => l.id));
        const next = new Set<string>();
        prev.forEach((id) => {
          if (visible.has(id)) next.add(id);
        });
        return next;
      });
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل طلبات الإجازات.'));
    } finally {
      setLoading(false);
    }
  };

  // التصفية حسب الحالة
  const filtered = useMemo(() => {
    if (statusFilter === 'all') return leaves;
    return leaves.filter((l) => String(l.status) === statusFilter);
  }, [leaves, statusFilter]);

  // الطلبات المعلّقة (status=1) بدون فلتر
  const pendingAll = useMemo(() => leaves.filter((l) => l.status === 1), [leaves]);

  // الطلبات المعلّقة ضمن الشهر الحالي
  const pendingThisMonth = useMemo(() => {
    const now = new Date();
    const y = now.getFullYear();
    const m = now.getMonth();
    return pendingAll.filter((l) => {
      const d = new Date(l.startDate);
      return d.getFullYear() === y && d.getMonth() === m;
    });
  }, [pendingAll]);

  // الطلبات المعلّقة المختارة (Bulk)
  const selectedPending = useMemo(
    () => filtered.filter((l) => selectedIds.has(l.id) && l.status === 1),
    [filtered, selectedIds]
  );

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSelectAllVisible = () => {
    const visiblePending = filtered.filter((l) => l.status === 1);
    const allSelected = visiblePending.every((l) => selectedIds.has(l.id));
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (allSelected) {
        visiblePending.forEach((l) => next.delete(l.id));
      } else {
        visiblePending.forEach((l) => next.add(l.id));
      }
      return next;
    });
  };

  const handleApprove = async (id: string) => {
    setActionId(id);
    try {
      await hrApi.approveLeave(id);
      toast.success('تمت الموافقة على الطلب.');
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشلت الموافقة.'));
    } finally {
      setActionId(null);
    }
  };

  const handleReject = async (id: string) => {
    setActionId(id);
    try {
      await hrApi.rejectLeave(id);
      toast.success('تم رفض الطلب.');
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل الرفض.'));
    } finally {
      setActionId(null);
    }
  };

  const executeBulk = async (
    ids: string[],
    action: 'approve' | 'reject'
  ) => {
    if (ids.length === 0) return;
    setBulkBusy(true);
    try {
      const results = await Promise.allSettled(
        ids.map((id) => (action === 'approve' ? hrApi.approveLeave(id) : hrApi.rejectLeave(id)))
      );
      const ok = results.filter((r) => r.status === 'fulfilled').length;
      const fail = results.length - ok;
      if (ok > 0) toast.success(`تمت العملية على ${ok} طلب.`);
      if (fail > 0) toast.error(`فشل ${fail} طلب.`);
      await load();
    } finally {
      setBulkBusy(false);
      setBulkConfirm(null);
    }
  };

  const visiblePending = filtered.filter((l) => l.status === 1);
  const allSelected =
    visiblePending.length > 0 && visiblePending.every((l) => selectedIds.has(l.id));

  const columns: TableColumn<LeaveRequest>[] = [
    {
      key: 'select',
      header: (
        <input
          type="checkbox"
          aria-label="تحديد الكل"
          checked={allSelected}
          onChange={toggleSelectAllVisible}
          disabled={visiblePending.length === 0}
          className="h-4 w-4 rounded border-gray-300"
        />
      ),
      render: (l) =>
        l.status === 1 ? (
          <input
            type="checkbox"
            aria-label={`تحديد طلب ${l.id}`}
            checked={selectedIds.has(l.id)}
            onChange={() => toggleSelect(l.id)}
            className="h-4 w-4 rounded border-gray-300"
          />
        ) : (
          <span className="text-gray-300">—</span>
        ),
      className: 'w-12',
    },
    {
      key: 'employee',
      header: 'الموظف',
      render: (l) =>
        l.employeeName || <span className="text-gray-400 text-xs">{l.employeeId}</span>,
    },
    {
      key: 'leaveType',
      header: 'النوع',
      render: (l) => <Badge variant="info">{LEAVE_TYPES[l.leaveType] || l.leaveType}</Badge>,
    },
    {
      key: 'period',
      header: 'الفترة',
      render: (l) => (
        <div>
          <p className="text-sm text-gray-800">
            {formatDate(l.startDate)} - {formatDate(l.endDate)}
          </p>
          <p className="text-xs text-gray-500">{l.totalDays} يوم</p>
        </div>
      ),
    },
    {
      key: 'reason',
      header: 'السبب',
      render: (l) =>
        l.reason ? (
          <span className="text-sm text-gray-700 max-w-[200px] truncate inline-block">
            {l.reason}
          </span>
        ) : (
          <span className="text-gray-400 text-xs">—</span>
        ),
    },
    {
      key: 'createdAt',
      header: 'تاريخ الطلب',
      render: (l) => (
        <div>
          <p className="text-xs text-gray-700">{formatDate(l.createdAt)}</p>
          <p className="text-[10px] text-gray-400">{formatTime(l.createdAt)}</p>
        </div>
      ),
    },
    {
      key: 'status',
      header: 'الحالة',
      render: (l) => (
        <Badge variant={LEAVE_STATUS_VARIANTS[l.status] || 'neutral'}>
          {LEAVE_STATUSES[l.status] || l.status}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: 'إجراءات',
      align: 'center',
      render: (l) =>
        isManager && l.status === 1 ? (
          <div className="flex items-center gap-1 justify-center">
            <button
              onClick={() => handleApprove(l.id)}
              disabled={actionId === l.id || bulkBusy}
              className="p-1.5 rounded-lg text-green-600 hover:bg-green-50 disabled:opacity-50"
              title="موافقة"
              aria-label="موافقة"
            >
              <Check className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleReject(l.id)}
              disabled={actionId === l.id || bulkBusy}
              className="p-1.5 rounded-lg text-danger-600 hover:bg-danger-50 disabled:opacity-50"
              title="رفض"
              aria-label="رفض"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        ) : (
          <span className="text-xs text-gray-400">—</span>
        ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="🌴 الإجازات"
        description="طلبات إجازات الموظفين"
        actions={
          <Link href="/hr/leaves/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              طلب إجازة
            </Button>
          </Link>
        }
      />

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
        <Card accent="yellow">
          <p className="text-xs text-gray-500">بانتظار الموافقة</p>
          <p className="text-2xl font-bold text-yellow-600 mt-1">{pendingAll.length}</p>
        </Card>
        <Card accent="green">
          <p className="text-xs text-gray-500">معتمدة</p>
          <p className="text-2xl font-bold text-green-600 mt-1">
            {leaves.filter((l) => l.status === 2).length}
          </p>
        </Card>
        <Card accent="red">
          <p className="text-xs text-gray-500">مرفوضة</p>
          <p className="text-2xl font-bold text-danger-600 mt-1">
            {leaves.filter((l) => l.status === 3).length}
          </p>
        </Card>
        <Card accent="blue">
          <p className="text-xs text-gray-500">إجمالي الطلبات</p>
          <p className="text-2xl font-bold text-blue-600 mt-1">{leaves.length}</p>
        </Card>
      </div>

      {/* Filters + Bulk Actions */}
      {isManager && (
        <div className="bg-white rounded-xl shadow-sm p-4 mb-4 flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <Filter className="h-4 w-4" />
            <span className="font-semibold">تصفية:</span>
          </div>
          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
            options={[
              { label: 'الكل', value: 'all' },
              { label: 'بانتظار الموافقة', value: '1' },
              { label: 'معتمدة', value: '2' },
              { label: 'مرفوضة', value: '3' },
            ]}
            containerClassName="w-48"
          />

          <div className="flex-1" />

          <Badge variant={selectedPending.length > 0 ? 'info' : 'neutral'}>
            {selectedPending.length > 0
              ? `${selectedPending.length} طلب محدد`
              : 'لم يتم تحديد شيء'}
          </Badge>

          <Button
            variant="primary"
            size="sm"
            disabled={selectedPending.length === 0 || bulkBusy}
            loading={bulkBusy}
            onClick={() => setBulkConfirm('approve')}
            iconLeft={<CheckCheck className="h-4 w-4" />}
          >
            موافقة المحدد
          </Button>
          <Button
            variant="danger"
            size="sm"
            disabled={selectedPending.length === 0 || bulkBusy}
            loading={bulkBusy}
            onClick={() => setBulkConfirm('reject')}
            iconLeft={<XCircle className="h-4 w-4" />}
          >
            رفض المحدد
          </Button>
          <Button
            variant="secondary"
            size="sm"
            disabled={pendingThisMonth.length === 0 || bulkBusy}
            loading={bulkBusy}
            onClick={() => setBulkConfirm('approveAllMonth')}
            iconLeft={<CheckCheck className="h-4 w-4" />}
          >
            موافقة معلّقة هذا الشهر ({pendingThisMonth.length})
          </Button>
        </div>
      )}

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={6} cols={7} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Filter className="h-12 w-12" />}
          title="لا توجد طلبات إجازات"
          description={
            statusFilter !== 'all'
              ? 'لا توجد طلبات تطابق التصفية الحالية.'
              : 'لم يتم تقديم أي طلب إجازة بعد.'
          }
        />
      ) : (
        <Table
          columns={columns}
          data={filtered}
          loading={false}
          rowKey={(l) => l.id}
          emptyMessage="لا توجد طلبات"
        />
      )}

      {!loading && filtered.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">
          {filtered.length} طلب {selectedPending.length > 0 ? `· ${selectedPending.length} محدد` : ''}
        </p>
      )}

      {/* Bulk confirmations */}
      <ConfirmDialog
        open={bulkConfirm === 'approve'}
        title="موافقة على الطلبات المحددة"
        message={
          <span>
            هل تريد الموافقة على <b>{selectedPending.length}</b> طلب إجازة؟ هذا الإجراء لا يمكن التراجع عنه.
          </span>
        }
        confirmLabel="موافقة"
        variant="primary"
        loading={bulkBusy}
        onConfirm={() => executeBulk(Array.from(selectedPending.map((l) => l.id)), 'approve')}
        onCancel={() => setBulkConfirm(null)}
      />
      <ConfirmDialog
        open={bulkConfirm === 'reject'}
        title="رفض الطلبات المحددة"
        message={
          <span>
            هل تريد رفض <b>{selectedPending.length}</b> طلب إجازة؟ هذا الإجراء لا يمكن التراجع عنه.
          </span>
        }
        confirmLabel="رفض"
        variant="danger"
        loading={bulkBusy}
        onConfirm={() => executeBulk(Array.from(selectedPending.map((l) => l.id)), 'reject')}
        onCancel={() => setBulkConfirm(null)}
      />
      <ConfirmDialog
        open={bulkConfirm === 'approveAllMonth'}
        title="موافقة على جميع الطلبات المعلّقة لهذا الشهر"
        message={
          <span>
            سيتم اعتماد <b>{pendingThisMonth.length}</b> طلب إجازة معلّق خلال الشهر الحالي. هل تريد المتابعة؟
          </span>
        }
        confirmLabel="موافقة الكل"
        variant="primary"
        loading={bulkBusy}
        onConfirm={() => executeBulk(pendingThisMonth.map((l) => l.id), 'approve')}
        onCancel={() => setBulkConfirm(null)}
      />
    </div>
  );
}
