'use client';

// Notification store (Cycle 8 / DEC-073) — frontend polling + state.
//
// Why a custom hook (not Zustand/Redux): the project doesn't use external
// state-management libs (see package.json — only React + axios). Following
// the useToast pattern (lib/useToast.ts): plain React state + a small
// helper module for shared logic.
//
// What this provides:
//   - useNotifications(): polls /api/inventory/notifications/unread every 30s
//   - returns { items, unreadCount, loading, refresh, markRead }
//
// Polling lifecycle:
//   - Starts on first mount (when user is logged in)
//   - Pauses on logout (token cleared) — call refresh() to re-start
//   - Stops on unmount
//
// Why 30s: the hand-off specified it. Cheap on the backend (single SELECT
// with the unread index). User can manually refresh via the "refresh" button.

import { useEffect, useRef, useState, useCallback } from 'react';
import { authApi, inventoryApi, type Notification } from '@/lib/api';

const POLL_INTERVAL_MS = 30_000; // 30 seconds per cycle 8 hand-off T4

export interface UseNotificationsResult {
  /** Recent unread notifications (max 50). */
  items: Notification[];
  /** Total count of unread notifications (matches the badge number). */
  unreadCount: number;
  /** True during the first fetch. */
  loading: boolean;
  /** Manually trigger a refresh (e.g. user clicks the refresh button). */
  refresh: () => Promise<void>;
  /** Mark a single notification as read. Optimistic — calls backend then refreshes. */
  markRead: (id: string) => Promise<void>;
  /** True if the polling timer is active. */
  isPolling: boolean;
}

/**
 * Hook that polls the notifications API every 30s. Returns the unread
 * notifications + count + helpers. The polling starts on mount and stops
 * on unmount.
 */
export function useNotifications(): UseNotificationsResult {
  const [items, setItems] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [isPolling, setIsPolling] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchUnread = useCallback(async () => {
    // Skip if not authenticated (the api.ts interceptor would 401, but
    // a guard here is cheaper and avoids the error toast).
    if (!authApi.isLoggedIn()) {
      setItems([]);
      setUnreadCount(0);
      setLoading(false);
      return;
    }
    try {
      const res = await inventoryApi.getUnreadNotifications();
      setItems(res.items ?? []);
      setUnreadCount(res.count ?? 0);
    } catch {
      // Silent — keep the last known state. A failed poll should not
      // surface as an error to the user (we'd be spamming them every 30s).
    } finally {
      setLoading(false);
    }
  }, []);

  const refresh = useCallback(async () => {
    setLoading(true);
    await fetchUnread();
  }, [fetchUnread]);

  const markRead = useCallback(async (id: string) => {
    // Optimistic update: remove from the list immediately.
    setItems((prev) => prev.filter((n) => n.id !== id));
    setUnreadCount((c) => Math.max(0, c - 1));
    try {
      await inventoryApi.markNotificationRead(id);
    } catch {
      // On failure, re-fetch to restore correct state.
      await fetchUnread();
    }
  }, [fetchUnread]);

  useEffect(() => {
    // Initial fetch
    fetchUnread();

    // Start polling
    setIsPolling(true);
    intervalRef.current = setInterval(fetchUnread, POLL_INTERVAL_MS);

    return () => {
      setIsPolling(false);
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [fetchUnread]);

  return { items, unreadCount, loading, refresh, markRead, isPolling };
}
