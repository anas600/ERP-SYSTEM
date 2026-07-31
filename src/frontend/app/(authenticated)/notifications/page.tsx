'use client';

// صفحة الإشعارات (User Notifications) — محدّثة: URL الصحيح + per-notification hide + bulk mark-as-read

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Bell, BellOff, CheckCheck, Eye, Filter, Trash2 } from 'lucide-react';
import {
  Badge,
  Button,
  Card,
  EmptyState,
  PageHeader,
  SkeletonTable,
  useToast,
} from '@/components/ui';
import { Table, type TableColumn } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

interface Notification {
  id: string;
  type: string;
  title: string;
  message: string;
  referenceType?: string | null;
  referenceId?: string | null;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
}

const TYPE_VARIANTS: Record<string, 'info' | 'success' | 'warning' | 'danger' | 'neutral'> = {
  info: 'info',
  success: 'success',
  warning: 'warning',
  error: 'danger',
  system: 'neutral',
  approval: 'warning',
  alert: 'danger',
  LowStock: 'warning',
  JournalPosted: 'success',
  HighVariance: 'danger',
  Payroll: 'info',
};

type Filter = 'all' | 'unread' | 'read';

// ملاحظة: الـ backend لا يوفّر DELETE endpoint للإشعارات — نعتمد hide محلي.
const STORAGE_KEY = 'hiddenNotifications';

function loadHidden(): Set<string> {
  if (typeof window === 'undefined') return new Set();
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return new Set();
    return new Set(JSON.parse(raw) as string[]);
  } catch {
    return new Set();
  }
}

function saveHidden(ids: Set<string>) {
  if (typeof window === 'undefined') return;
  localStorage.setItem(STORAGE_KEY, JSON.stringify(Array.from(ids)));
}

export default function UserNotificationsPage() {
  const { loading: authLoading } = useAuth();
  const toast = useToast();

  const [items, setItems] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<Filter>('all');
  const [hidden, setHidden] = useState<Set<string>>(new Set());

  useEffect(() => {
    setHidden(loadHidden());
  }, []);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      // الإصل: الـ endpoint الصحيح هو /api/inventory/notifications
      const r = await api.get<Notification[] | { items?: Notification[] }>(
        '/api/inventory/notifications'
      );
      const list = Array.isArray(r.data) ? r.data : r.data.items ?? [];
      setItems(list);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الإشعارات.'));
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (id: string) => {
    try {
      await api.post(`/api/inventory/notifications/${id}/mark-read`);
      setItems((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل تحديث الإشعار.'));
    }
  };

  const markAllAsRead = async () => {
    const unread = items.filter((n) => !n.isRead);
    if (unread.length === 0) return;
    try {
      await Promise.all(
        unread.map((n) => api.post(`/api/inventory/notifications/${n.id}/mark-read`))
      );
      setItems((prev) => prev.map((n) => ({ ...n, isRead: true })));
      toast.success(`تم تحديد ${unread.length} إشعار كمقروء.`);
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل تحديث الإشعارات.'));
    }
  };

  // إخفاء محلي (بما أنّ الـ backend لا يوفّر DELETE)
  const hideNotification = (id: string) => {
    setHidden((prev) => {
      const next = new Set(prev);
      next.add(id);
      saveHidden(next);
      return next;
    });
    toast.info('تم إخفاء الإشعار.');
  };

  const visibleItems = items.filter((n) => !hidden.has(n.id));
  const filtered = visibleItems.filter((n) =>
    filter === 'all' ? true : filter === 'unread' ? !n.isRead : n.isRead
  );
  const unreadCount = visibleItems.filter((n) => !n.isRead).length;
  const readCount = visibleItems.length - unreadCount;

  const columns: TableColumn<Notification>[] = [
    {
      key: 'icon',
      header: '',
      render: (n) => (
        <div
          className={`h-8 w-8 rounded-lg flex items-center justify-center flex-shrink-0 ${
            n.isRead ? 'bg-gray-100 text-gray-400' : 'bg-blue-100 text-blue-600'
          }`}
        >
          <Bell className="h-4 w-4" />
        </div>
      ),
      className: 'w-12',
    },
    {
      key: 'title',
      header: 'العنوان',
      render: (n) => (
        <div>
          <div className="flex items-center gap-2 flex-wrap">
            <h3 className={`text-sm font-semibold ${n.isRead ? 'text-gray-700' : 'text-gray-900'}`}>
              {n.title}
            </h3>
            <Badge variant={TYPE_VARIANTS[n.type] || 'neutral'} size="sm">
              {n.type}
            </Badge>
            {!n.isRead && (
              <span
                className="inline-block h-2 w-2 rounded-full bg-blue-500"
                title="غير مقروء"
              />
            )}
          </div>
          <p className={`text-sm mt-1 ${n.isRead ? 'text-gray-500' : 'text-gray-700'}`}>
            {n.message}
          </p>
        </div>
      ),
    },
    {
      key: 'createdAt',
      header: 'التاريخ',
      render: (n) => (
        <div>
          <div className="text-xs text-gray-700">{formatDate(n.createdAt)}</div>
          <div className="text-[10px] text-gray-400">{formatTime(n.createdAt)}</div>
        </div>
      ),
      className: 'w-28',
    },
    {
      key: 'actions',
      header: 'إجراءات',
      align: 'center',
      render: (n) => (
        <div className="flex items-center gap-1 justify-center">
          {!n.isRead && (
            <button
              onClick={() => markAsRead(n.id)}
              className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded"
              title="تحديد كمقروء"
              aria-label="تحديد كمقروء"
            >
              <Eye className="h-4 w-4" />
            </button>
          )}
          {n.referenceId && n.referenceType && (
            <Link
              href={`/${n.referenceType}/${n.referenceId}`}
              className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded"
              title="عرض"
            >
              <span className="text-xs underline">فتح</span>
            </Link>
          )}
          <button
            onClick={() => hideNotification(n.id)}
            className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded"
            title="إخفاء"
            aria-label="إخفاء"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="🔔 الإشعارات"
        description={`إشعاراتك في النظام (${unreadCount} غير مقروء)`}
        actions={
          unreadCount > 0 ? (
            <Button
              variant="primary"
              onClick={markAllAsRead}
              iconLeft={<CheckCheck className="h-4 w-4" />}
            >
              تحديد الكل كمقروء
            </Button>
          ) : undefined
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {/* Filter tabs */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1 flex-wrap">
        {[
          { id: 'all' as Filter, label: 'الكل', count: visibleItems.length, icon: Filter },
          { id: 'unread' as Filter, label: 'غير مقروء', count: unreadCount, icon: Bell },
          { id: 'read' as Filter, label: 'مقروء', count: readCount, icon: CheckCheck },
        ].map((tab) => {
          const Icon = tab.icon;
          const active = filter === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setFilter(tab.id)}
              className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 ${
                active ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              <Icon className="h-4 w-4" />
              {tab.label} ({tab.count})
            </button>
          );
        })}
      </div>

      {loading ? (
        <SkeletonTable rows={5} cols={4} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<BellOff className="h-12 w-12" />}
          title="لا توجد إشعارات"
          description={
            filter === 'unread'
              ? 'لا توجد إشعارات غير مقروءة.'
              : filter === 'read'
              ? 'لا توجد إشعارات مقروءة.'
              : 'لا توجد إشعارات حالياً.'
          }
        />
      ) : (
        <Table
          data={filtered}
          columns={columns}
          rowKey={(n) => n.id}
          emptyMessage="لا توجد إشعارات"
        />
      )}

      <Card className="mt-4 bg-blue-50/40 border-blue-100">
        <p className="text-xs text-gray-600">
          💡 <b>ملاحظة:</b> إخفاء الإشعار يكون محلياً في المتصفح فقط — الـ backend لا يدعم حذف الإشعارات حالياً.
        </p>
      </Card>
    </div>
  );
}
