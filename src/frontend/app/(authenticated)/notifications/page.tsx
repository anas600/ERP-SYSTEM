'use client';

// صفحة الإشعارات (User Notifications)

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Bell, BellOff, CheckCheck, Filter, Eye, ArrowRight } from 'lucide-react';
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
  approval: 'warning',
  alert: 'danger',
};

export default function UserNotificationsPage() {
  const { loading: authLoading, user } = useAuth();
  const [items, setItems] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'unread' | 'read'>('all');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await api.get<Notification[] | { items?: Notification[] }>('/api/notifications');
      const list = Array.isArray(r.data) ? r.data : r.data.items || [];
      setItems(list);
    } catch (e: unknown) {
      // If endpoint not available, show empty
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  const markAsRead = async (id: string) => {
    try {
      await api.post(`/api/notifications/${id}/mark-read`);
      setItems((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث الإشعار.'));
    }
  };

  const markAllAsRead = async () => {
    try {
      const unread = items.filter((n) => !n.isRead);
      await Promise.all(unread.map((n) => api.post(`/api/notifications/${n.id}/mark-read`)));
      setItems((prev) => prev.map((n) => ({ ...n, isRead: true })));
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث الإشعارات.'));
    }
  };

  const filtered = items.filter((n) =>
    filter === 'all' ? true : filter === 'unread' ? !n.isRead : n.isRead
  );
  const unreadCount = items.filter((n) => !n.isRead).length;

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل...</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="🔔 الإشعارات"
        description={`إشعاراتك في النظام (${unreadCount} غير مقروء)`}
        actions={
          unreadCount > 0 ? (
            <Button variant="primary" onClick={markAllAsRead} iconLeft={<CheckCheck className="h-4 w-4" />}>
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
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1">
        {[
          { id: 'all', label: 'الكل', count: items.length, icon: Filter },
          { id: 'unread', label: 'غير مقروء', count: unreadCount, icon: Bell },
          { id: 'read', label: 'مقروء', count: items.length - unreadCount, icon: CheckCheck },
        ].map((tab) => {
          const Icon = tab.icon;
          const active = filter === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setFilter(tab.id as any)}
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

      {/* Notifications list */}
      {filtered.length === 0 ? (
        <Card className="p-12 text-center text-gray-500">
          <BellOff className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          <p>لا توجد إشعارات {filter === 'unread' ? 'غير مقروءة' : ''}.</p>
        </Card>
      ) : (
        <div className="space-y-2">
          {filtered.map((n) => (
            <Card
              key={n.id}
              className={`p-4 ${!n.isRead ? 'border-l-4 border-l-blue-500 bg-blue-50/30' : ''}`}
            >
              <div className="flex items-start gap-3">
                <div className={`h-9 w-9 rounded-lg flex items-center justify-center flex-shrink-0 ${
                  n.isRead ? 'bg-gray-100 text-gray-400' : 'bg-blue-100 text-blue-600'
                }`}>
                  <Bell className="h-4 w-4" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <h3 className={`text-sm font-semibold ${n.isRead ? 'text-gray-700' : 'text-gray-900'}`}>
                          {n.title}
                        </h3>
                        <Badge variant={TYPE_VARIANTS[n.type] || 'neutral'} size="sm">
                          {n.type}
                        </Badge>
                        {!n.isRead && (
                          <span className="inline-block h-2 w-2 rounded-full bg-blue-500" title="غير مقروء" />
                        )}
                      </div>
                      <p className={`text-sm mt-1 ${n.isRead ? 'text-gray-500' : 'text-gray-700'}`}>
                        {n.message}
                      </p>
                      <p className="text-xs text-gray-400 mt-1">
                        {formatDate(n.createdAt)} {formatTime(n.createdAt)}
                      </p>
                    </div>

                    <div className="flex items-center gap-1 flex-shrink-0">
                      {!n.isRead && (
                        <button
                          onClick={() => markAsRead(n.id)}
                          className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded"
                          title="تحديد كمقروء"
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
                          <ArrowRight className="h-4 w-4" />
                        </Link>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
