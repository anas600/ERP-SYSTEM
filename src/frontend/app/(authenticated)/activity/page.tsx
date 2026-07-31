'use client';

// Sprint 3 — T2: Activity feed page.
// Reads recent user actions (LOGIN, LOGOUT, REGISTER, COMPANY_SWITCH, ...)
// from GET /api/activity/recent?limit=20 and renders a vertical timeline.
//
// Backend contract (T1, Backend Jimi — parallel branch):
//   GET /api/activity/recent?limit=20
//   → ActivityItem[]  (or { items: ActivityItem[] } — both accepted)
//
//   ActivityItem = {
//     id, userId, userName, action, entityType?, entityId?,
//     timestamp (ISO 8601), metadata?, ipAddress?
//   }
//
// Failure modes handled here:
//   1. Endpoint not yet wired (404) — show a friendly empty state, not a stack trace
//   2. Backend not reachable (5xx / network) — show error with retry
//   3. Empty array — bilingual empty state (Arabic + English)
//
// Sprint 3 hand-off: scope is the active company (X-Company-Id header on every
// request). The interceptor in lib/api.ts adds it automatically.

import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
  Activity as ActivityIcon,
  AlertCircle,
  LogIn,
  LogOut,
  UserPlus,
  RefreshCw,
  ArrowRightLeft,
  KeyRound,
  Globe,
  RefreshCcw,
} from 'lucide-react';
import { PageHeader, EmptyState } from '@/components/ui';
import { Skeleton } from '@/components/ui';
import { activityApi, getErrorMessage, type ActivityItem } from '@/lib/api';
import { useAuth } from '@/lib/useAuth';

// ============ Action metadata ============
// Maps backend action constants to icons, colors, and Arabic labels.
// Falls back to a neutral entry when the action is unknown (forward-compat —
// Backend may add new actions without a frontend release).

type Variant = 'info' | 'success' | 'warning' | 'danger' | 'neutral';

interface ActionMeta {
  icon: typeof LogIn;
  variant: Variant;
  label: string;     // Arabic
  labelEn: string;   // English
}

const ACTION_META: Record<string, ActionMeta> = {
  LOGIN_SUCCESS:   { icon: LogIn,        variant: 'success', label: 'تسجيل دخول',         labelEn: 'Login' },
  LOGIN_FAILED:    { icon: KeyRound,     variant: 'danger',  label: 'فشل تسجيل دخول',    labelEn: 'Login failed' },
  LOGIN:           { icon: LogIn,        variant: 'info',    label: 'تسجيل دخول',         labelEn: 'Login attempt' },
  LOGOUT:          { icon: LogOut,       variant: 'neutral', label: 'تسجيل خروج',         labelEn: 'Logout' },
  REFRESH:         { icon: RefreshCcw,   variant: 'info',    label: 'تجديد الجلسة',       labelEn: 'Session refresh' },
  REGISTER:        { icon: UserPlus,     variant: 'success', label: 'تسجيل مستخدم جديد', labelEn: 'New user registered' },
  PASSWORD_CHANGE: { icon: KeyRound,     variant: 'warning', label: 'تغيير كلمة المرور',  labelEn: 'Password changed' },
  COMPANY_SWITCH:  { icon: ArrowRightLeft, variant: 'info',  label: 'تبديل شركة',         labelEn: 'Switched company' },
};

const VARIANT_STYLES: Record<Variant, { dot: string; ring: string; badge: string }> = {
  info:    { dot: 'bg-blue-500',   ring: 'ring-blue-100',   badge: 'bg-blue-50 text-blue-700' },
  success: { dot: 'bg-green-500',  ring: 'ring-green-100',  badge: 'bg-green-50 text-green-700' },
  warning: { dot: 'bg-amber-500',  ring: 'ring-amber-100',  badge: 'bg-amber-50 text-amber-700' },
  danger:  { dot: 'bg-red-500',    ring: 'ring-red-100',    badge: 'bg-red-50 text-red-700' },
  neutral: { dot: 'bg-gray-400',   ring: 'ring-gray-100',   badge: 'bg-gray-100 text-gray-600' },
};

const DEFAULT_META: ActionMeta = {
  icon: ActivityIcon,
  variant: 'neutral',
  label: 'نشاط',
  labelEn: 'Activity',
};

// ============ Helpers ============

/**
 * تنسيق الوقت بالعربي: "منذ 5 دقائق"، "الآن"، "أمس"، ...
 * يطابق النمط المستخدم في NotificationBell.
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

/** تنسيق تاريخ مطلق للـ tooltip (en-GB لتجنّب التقويم الهجري). */
function formatAbsolute(iso: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleString('en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// ============ Page ============

export default function ActivityPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<ActivityItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await activityApi.recent(20);
      setItems(data);
    } catch (e: unknown) {
      // Sprint 3 T2: the endpoint may not be wired yet (Backend Jimi in
      // parallel). Surface a friendly error with retry — the empty state
      // below would also work, but the user should know *why* the page is
      // empty if it's actually a failure.
      setError(getErrorMessage(e, 'تعذّر تحميل سجل النشاط.'));
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="📊 سجل النشاط"
        description="آخر الإجراءات على النظام (تسجيل دخول، تبديل شركة، تغيير كلمة مرور، ...)"
        actions={
          <button
            type="button"
            onClick={() => void load()}
            disabled={loading}
            className="inline-flex items-center gap-2 px-3 h-9 rounded-lg bg-gray-100 hover:bg-gray-200 text-sm text-gray-700 disabled:opacity-50"
            aria-label="تحديث"
            title="تحديث"
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            <span>تحديث</span>
          </button>
        }
      />

      {error && (
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل سجل النشاط</p>
            <p className="text-sm mt-0.5">{error}</p>
            <p className="text-xs mt-1 text-red-600">
              ملاحظة: قد يكون الـ endpoint غير مُفعَّل بعد في هذه البيئة.
            </p>
          </div>
          <button
            type="button"
            onClick={() => void load()}
            disabled={loading}
            className="px-3 h-8 rounded bg-white border border-red-200 text-sm text-red-700 hover:bg-red-50 disabled:opacity-50"
          >
            إعادة المحاولة
          </button>
        </div>
      )}

      {loading ? (
        <ActivitySkeleton />
      ) : items.length === 0 ? (
        <EmptyState
          icon={<ActivityIcon className="h-12 w-12" />}
          title="لا توجد أنشطة حديثة"
          description="No recent activity — لم يتم تسجيل أي نشاط على النظام خلال الفترة الأخيرة."
        />
      ) : (
        <ActivityTimeline items={items} />
      )}
    </div>
  );
}

// ============ Timeline ============

interface TimelineProps {
  items: ActivityItem[];
}

function ActivityTimeline({ items }: TimelineProps) {
  return (
    <div
      className="bg-white rounded-xl shadow-sm border border-gray-100 p-4 sm:p-6"
      dir="rtl"
    >
      <ol
        className="relative"
        // Vertical line: 1px wide, positioned to align with the dot centers.
        // The dots are h-8 (32px), centered in a flex column, so the line sits
        // 1rem (16px) from the right edge in RTL.
      >
        {items.map((item, idx) => {
          const meta = ACTION_META[item.action] ?? DEFAULT_META;
          const styles = VARIANT_STYLES[meta.variant];
          const Icon = meta.icon;
          const isLast = idx === items.length - 1;
          return (
            <li
              key={item.id}
              className={`relative flex gap-3 sm:gap-4 ${isLast ? '' : 'pb-5'}`}
            >
              {/* Vertical line (skip on last item) */}
              {!isLast && (
                <span
                  className="absolute right-[15px] top-8 bottom-0 w-px bg-gray-200"
                  aria-hidden="true"
                />
              )}

              {/* Dot with icon */}
              <div className="flex-shrink-0 relative z-10">
                <div
                  className={`h-8 w-8 rounded-full ring-4 ${styles.ring} ${styles.badge} flex items-center justify-center`}
                >
                  <Icon className="h-4 w-4" />
                </div>
              </div>

              {/* Body */}
              <div className="flex-1 min-w-0 pt-0.5">
                <div className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
                  <p className="text-sm font-semibold text-gray-800">
                    {meta.label}
                  </p>
                  {item.userName && (
                    <span className="text-sm text-gray-700">
                      — <span className="font-medium">{item.userName}</span>
                    </span>
                  )}
                  <span
                    className={`text-[10px] px-1.5 py-0.5 rounded-full ${styles.badge} font-mono`}
                    title={meta.labelEn}
                  >
                    {item.action}
                  </span>
                </div>

                {/* Entity link (when present) */}
                {item.entityType && item.entityId && (
                  <p className="text-xs text-gray-600 mt-0.5">
                    <span className="text-gray-400">{item.entityType}:</span>{' '}
                    <Link
                      href={`/${item.entityType}/${item.entityId}`}
                      className="font-mono text-blue-600 hover:underline"
                    >
                      {item.entityId.slice(0, 8)}…
                    </Link>
                  </p>
                )}

                {/* Metadata preview — show 1–2 keys only, to keep the list scannable. */}
                {item.metadata && Object.keys(item.metadata).length > 0 && (
                  <MetadataPreview metadata={item.metadata} />
                )}

                <p
                  className="text-[11px] text-gray-400 mt-1"
                  title={formatAbsolute(item.timestamp)}
                >
                  {formatTimeAgo(item.timestamp)}
                  {item.ipAddress && (
                    <span className="inline-flex items-center gap-1 ms-3">
                      <Globe className="h-3 w-3" />
                      {item.ipAddress}
                    </span>
                  )}
                </p>
              </div>
            </li>
          );
        })}
      </ol>
    </div>
  );
}

// ============ Metadata preview ============
// Shows 1–2 keys from the metadata JSON. Avoids dumping noisy internal
// fields (token ids, full user agents) into the timeline. Forward-compat:
// unknown keys are rendered with a generic "{key}: {value}" format.

function MetadataPreview({ metadata }: { metadata: Record<string, unknown> }) {
  const displayKeys: (keyof typeof metadata)[] = [
    'success',
    'reason',
    'self_service',
    'from_company_id',
    'to_company_id',
  ];
  const shown = displayKeys
    .filter((k) => metadata[k] !== undefined && metadata[k] !== null)
    .slice(0, 2);

  if (shown.length === 0) return null;

  return (
    <p className="text-xs text-gray-500 mt-0.5">
      {shown.map((k) => (
        <span key={String(k)} className="me-3">
          <span className="text-gray-400">{String(k)}:</span>{' '}
          <span className="font-mono">{String(metadata[k])}</span>
        </span>
      ))}
    </p>
  );
}

// ============ Skeleton ============

function ActivitySkeleton() {
  return (
    <div
      className="bg-white rounded-xl shadow-sm border border-gray-100 p-4 sm:p-6"
      dir="rtl"
    >
      <ol>
        {Array.from({ length: 5 }).map((_, i) => {
          const isLast = i === 4;
          return (
            <li
              key={i}
              className={`relative flex gap-3 sm:gap-4 ${isLast ? '' : 'pb-5'}`}
            >
              {!isLast && (
                <span
                  className="absolute right-[15px] top-8 bottom-0 w-px bg-gray-100"
                  aria-hidden="true"
                />
              )}
              <Skeleton rounded width="w-8" height="h-8" />
              <div className="flex-1 min-w-0 space-y-2 pt-1">
                <Skeleton width="w-1/3" height="h-4" />
                <Skeleton width="w-2/3" height="h-3" />
                <Skeleton width="w-1/4" height="h-3" />
              </div>
            </li>
          );
        })}
      </ol>
    </div>
  );
}
