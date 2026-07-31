'use client';

// NotificationBell (Cycle 8 / DEC-073) — topbar dropdown for in-app
// notifications. Replaces the static Bell link in AppShell.tsx.
//
// What it does:
//   1. Renders a Bell icon with a red badge showing the unread count
//      (or empty if there are no unread notifications)
//   2. Clicking the Bell opens a dropdown with the 50 most recent unread
//      notifications
//   3. Each item shows: title, message, time-ago, and a "mark read" action
//   4. Mark-read is optimistic (item disappears immediately)
//   5. Outside click closes the dropdown
//
// State management:
//   - Uses useNotifications() from lib/notifications.ts (polls every 30s)
//   - No external state-management lib (project pattern: React + axios)
//
// Phase 6: the user_id comes from the JWT. The bell shows notifications
// for the CURRENT user (filtered server-side), and follows the active
// X-Company-Id (notifications are also company-scoped server-side).

import { useEffect, useRef, useState } from 'react';
import Link from 'next/link';
import { Bell, Check, RefreshCw } from 'lucide-react';
import { useNotifications } from '@/lib/notifications';
import { cn } from '@/lib/utils';

// ============ Helpers ============

/**
 * تنسيق الوقت بالعربي: "منذ 5 دقائق"، "الآن"، "أمس"، ...
 * استخدام Intl.RelativeTimeFormat مع locale = ar.
 */
function formatTimeAgo(iso: string): string {
  if (!iso) return '';
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '';
  const diffSec = Math.floor((Date.now() - then) / 1000);
  if (diffSec < 60) return 'الآن';
  const rtf = new Intl.RelativeTimeFormat('ar', { numeric: 'auto' });
  if (diffSec < 3600) return rtf.format(-Math.floor(diffSec / 60), 'minute');
  if (diffSec < 86400) return rtf.format(-Math.floor(diffSec / 3600), 'hour');
  if (diffSec < 604800) return rtf.format(-Math.floor(diffSec / 86400), 'day');
  return rtf.format(-Math.floor(diffSec / 604800), 'week');
}

// ============ Component ============

export function NotificationBell() {
  const { items, unreadCount, loading, refresh, markRead } = useNotifications();
  const [open, setOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close on outside click (same pattern as CompanySwitcher)
  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  return (
    <div ref={dropdownRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="relative p-2 rounded-lg hover:bg-gray-100 text-gray-600 hover:text-gray-800"
        aria-label="الإشعارات"
        aria-haspopup="menu"
        aria-expanded={open}
        title="إشعاراتي"
      >
        <Bell className="h-5 w-5" />
        {unreadCount > 0 && (
          <span
            data-testid="notification-badge"
            className={cn(
              'absolute top-0.5 right-0.5 min-w-[1.125rem] h-[1.125rem] px-1 rounded-full',
              'bg-red-600 text-white text-[10px] font-bold flex items-center justify-center',
              'ring-2 ring-white'
            )}
          >
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div
          role="menu"
          className="absolute left-0 mt-2 w-80 max-h-96 overflow-y-auto bg-white rounded-lg shadow-lg border border-gray-100 z-30"
        >
          {/* Header */}
          <div className="flex items-center justify-between px-3 py-2 border-b border-gray-100">
            <p className="text-sm font-semibold text-gray-800">
              الإشعارات
              {unreadCount > 0 && (
                <span className="text-xs text-gray-500 font-normal ms-2">({unreadCount} غير مقروء)</span>
              )}
            </p>
            <button
              type="button"
              onClick={refresh}
              className="p-1 rounded hover:bg-gray-50 text-gray-500"
              aria-label="تحديث"
              title="تحديث"
            >
              <RefreshCw className={cn('h-4 w-4', loading && 'animate-spin')} />
            </button>
          </div>

          {/* Body */}
          {loading && items.length === 0 ? (
            <div className="px-3 py-8 text-center text-sm text-gray-500">
              جارٍ التحميل…
            </div>
          ) : items.length === 0 ? (
            <div className="px-3 py-8 text-center text-sm text-gray-500">
              لا توجد إشعارات غير مقروءة
            </div>
          ) : (
            <ul className="divide-y divide-gray-100">
              {items.map((n) => (
                <li
                  key={n.id}
                  className="px-3 py-2 hover:bg-gray-50 group"
                  data-testid="notification-item"
                >
                  <div className="flex items-start gap-2">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-800 truncate">{n.title}</p>
                      <p className="text-xs text-gray-600 line-clamp-2 mt-0.5">{n.message}</p>
                      <p className="text-[10px] text-gray-400 mt-1">{formatTimeAgo(n.createdAt)}</p>
                    </div>
                    <button
                      type="button"
                      onClick={() => markRead(n.id)}
                      className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-blue-50 text-blue-600 flex-shrink-0 transition-opacity"
                      aria-label="تحديد كمقروء"
                      title="تحديد كمقروء"
                    >
                      <Check className="h-4 w-4" />
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {/* Footer */}
          <div className="px-3 py-2 border-t border-gray-100 bg-gray-50">
            <Link
              href="/admin/notifications"
              onClick={() => setOpen(false)}
              className="text-xs text-blue-600 hover:underline"
            >
              عرض كل الإشعارات
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
