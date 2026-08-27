// Sprint 63 (DEC-218) — usePermissions hook.
//
// One-shot fetch of GET /api/me/permissions. Exposes a `hasPermission(code)`
// helper that also honours the `admin.all` wildcard (the Admin bypass token).
//
// L19 / DEC-095: no userId is sent from the FE — the BE reads it from the
// JWT (the api.ts interceptor already attaches the Bearer token).

import { useCallback, useEffect, useState } from 'react';
import { ADMIN_ALL_PERMISSION, fetchMyPermissions } from '../lib/api/permissions';

export interface UsePermissionsResult {
  /** The raw set of permission codes the user holds. */
  permissions: Set<string>;
  /** True while the initial fetch is in flight. */
  loading: boolean;
  /** Non-null if the fetch failed. */
  error: Error | null;
  /**
   * Returns true if the user holds `<code>` OR holds the wildcard
   * `admin.all` (which the BE seeds onto the Admin role).
   *
   * Frontend-only UX gate — the BE is the real authority (see
   * `[RequirePermission]` attribute, DEC-215/216).
   */
  hasPermission: (code: string) => boolean;
  /** Manually re-trigger the fetch (e.g. after a role change). */
  refetch: () => Promise<void>;
}

/**
 * React hook: subscribe the current component to the user's permissions.
 *
 * `hasPermission` is stable (memoised) so it can be used in useEffect
 * dependency arrays without retriggering.
 */
export function usePermissions(): UsePermissionsResult {
  const [permissions, setPermissions] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const list = await fetchMyPermissions();
      setPermissions(new Set(list));
    } catch (e) {
      setError(e instanceof Error ? e : new Error(String(e)));
      setPermissions(new Set());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const hasPermission = useCallback(
    (code: string) => permissions.has(code) || permissions.has(ADMIN_ALL_PERMISSION),
    [permissions],
  );

  return { permissions, loading, error, hasPermission, refetch: load };
}
