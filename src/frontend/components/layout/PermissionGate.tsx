'use client';

// Sprint 63 (DEC-218) — PermissionGate.
//
// Inline permission gate: shows `children` only if the current user has the
// given permission code. Renders `fallback` (default: null) otherwise.
//
// L19 / DEC-095: no userId is sent from the FE — the BE reads it from the
// JWT (the api.ts interceptor already attaches the Bearer token).
//
// Security note: the BE is the real authority. The Gate is purely UX — it
// hides buttons the user can't use, but the BE's [RequirePermission]
// attribute (Sprint 63 Wave 2A) is what actually rejects the request.

import { ReactNode } from 'react';
import { usePermissions } from '@/hooks/usePermissions';

export interface PermissionGateProps {
  /** Permission code the user must hold (e.g. "projects.create"). */
  permission: string;
  /** What to show when the user has the permission. */
  children: ReactNode;
  /** What to show otherwise (default: render nothing). */
  fallback?: ReactNode;
  /**
   * If true, render `fallback` while the initial fetch is in flight
   * (default: false → render nothing, to avoid button flicker).
   */
  showFallbackOnLoading?: boolean;
}

/**
 * Render `children` only if the current user holds the given permission.
 *
 * The `admin.all` wildcard (seeded on the Admin role) is honoured
 * automatically by `usePermissions().hasPermission`.
 */
export function PermissionGate({
  permission,
  children,
  fallback = null,
  showFallbackOnLoading = false,
}: PermissionGateProps) {
  const { hasPermission, loading } = usePermissions();

  if (loading) {
    return <>{showFallbackOnLoading ? fallback : null}</>;
  }

  if (!hasPermission(permission)) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
