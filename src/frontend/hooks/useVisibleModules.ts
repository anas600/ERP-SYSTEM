// Sprint 63 (DEC-217) — useVisibleModules hook.
//
// One-shot fetch of GET /api/me/visible-modules. Returns the sorted list of
// module names the current user can see.
//
// L19 / DEC-095: no userId is sent from the FE — the BE reads it from the
// JWT (the api.ts interceptor already attaches the Bearer token).

import { useEffect, useState } from 'react';
import { fetchVisibleModules } from '../lib/api/module-visibility';
import type { ModuleCode } from '../lib/api-types';

export interface UseVisibleModulesResult {
  /** Sorted, deduped list of module names the user can see. */
  modules: ModuleCode[];
  /** True while the initial fetch is in flight. */
  loading: boolean;
  /** Non-null if the fetch failed. */
  error: Error | null;
  /** Manually re-trigger the fetch (e.g. after a role change). */
  refetch: () => Promise<void>;
}

/**
 * React hook: subscribe the current component to the user's visible modules.
 *
 * The fetch runs once on mount. Components that mount many times share the
 * underlying HTTP cache (axios instance) but each one keeps its own loading
 * state — that's fine, the request itself is fast.
 */
export function useVisibleModules(): UseVisibleModulesResult {
  const [modules, setModules] = useState<ModuleCode[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchVisibleModules();
      setModules(data);
    } catch (e) {
      // On error, render the sidebar with an empty module list (so the
      // worst case is "nothing visible" — the BE 403 is a separate concern).
      setError(e instanceof Error ? e : new Error(String(e)));
      setModules([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return { modules, loading, error, refetch: load };
}
