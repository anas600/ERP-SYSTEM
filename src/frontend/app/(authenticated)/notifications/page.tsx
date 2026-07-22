'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Bell, BellOff, CheckCheck, Filter, Eye } from 'lucide-react';
import { PageHeader, Card, Badge, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate, formatTime } from '@/lib/utils';

interface Notification {
  id: string;
  type: string;
  title: string;
  message: string;
  referenceType?: string;
  referenceId?: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
}

const TYPE_VARIANTS: Record<string, 'info' | 'success' | 'warning' | 'danger' | 'neutral'> = {
  info: 'info',
  success: 'success',
  warning: 'warning',
  error: 'danger',
  system: 'neutral',
};

export default function NotificationsPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'unread'>('all');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, filter]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const unreadOnly = filter === 'unread';
      const data = await api.get<Notification[]>('/api/inventory/notifications', {
        params: { unreadOnly, take: 100 }
      });
      setItems(data.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل الإشعارات.'));
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (id: string) => {
    try {
      await api.post(`/api/inventory/notifications/${id}/read`, {});
      // Refresh
      load();
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث الإشعار.'));
    }
  };

  const markAllAsRead = async () => {
    try {
      // Bulk mark as read - need a backend endpoint or loop
      for (const item of items.filter(i => !i.isRead)) {
        await api.post(`/api/inventory/notifications/${item.id}/read`, {});
      }
      load();
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث الإشعارات.'));
    }
  };

  const unreadCount = items.filter(i => !i.isRead).length;

  return (
    <div>
      <PageHeader
        title="🔔 الإشعارات"
        description={`Notifications — ${unreadCount} غير مقروء`}
        actions={
          <>
            {unreadCount > 0 && (
              <Button onClick={markAllAsRead} variant="primary" size="sm" iconLeft={<CheckCheck className="h-4 w-4" />}>
                تعليم الكل كمقروء
              </Button>
            )}
            <Button onClick={load} variant="secondary" size="sm">تحديث</Button>
          </>
        }
      />

      {/* Filter */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1">
        <button
          onClick={() => setFilter('all')}
          className={`px-4 py-2 rounded-lg text-sm font-medium ${
            filter === 'all' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'
          }`}
        >
          الكل ({items.length})
        </button>
        <button
          onClick={() => setFilter('unread')}
          className={`px-4 py-2 rounded-lg text-sm font-medium ${
            filter === 'unread' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'
          }`}
        >
          غير المقروء ({unreadCount})
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : items.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          <BellOff className="h-12 w-12 mx-auto mb-2 text-gray-400" />
          <p>{filter === 'unread' ? 'لا توجد إشعارات غير مقروءة' : 'لا توجد إشعارات'}</p>
        </div>
      ) : (
        <div className="space-y-2">
          {items.map((n) => (
            <Card key={n.id} accent={n.isRead ? 'gray' : 'blue'}>
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className={`font-bold ${n.isRead ? 'text-gray-700' : 'text-gray-900'}`}>{n.title}</h3>
                    <Badge variant={TYPE_VARIANTS[n.type] ?? 'neutral'}>{n.type}</Badge>
                    {!n.isRead && <Badge variant="info">جديد</Badge>}
                  </div>
                  <p className={`text-sm ${n.isRead ? 'text-gray-500' : 'text-gray-700'}`}>{n.message}</p>
                  <div className="mt-2 flex items-center gap-3 text-xs text-gray-400">
                    <span>{formatDate(n.createdAt)} {formatTime(n.createdAt)}</span>
                    {n.readAt && <span>• مقروء {formatDate(n.readAt)}</span>}
                  </div>
                </div>
                <div className="flex flex-col gap-1">
                  {!n.isRead && (
                    <Button onClick={() => markAsRead(n.id)} variant="ghost" size="sm" iconLeft={<Eye className="h-3 w-3" />}>
                      تعليم كمقروء
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
