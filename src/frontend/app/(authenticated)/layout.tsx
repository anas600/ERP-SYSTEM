'use client';

// Layout لكل الصفحات المحمية — يلف المحتوى بـ AppShell
// الـ (authenticated) route group يخفي الـ prefix من الـ URL (نفس الـ paths)
// Sprint 9 T3 (FE Jimi 3): wraps children in <ErrorBoundary> for client-side
// render failures. Complementary to the route-level error.tsx files (which
// catch framework-level errors); the ErrorBoundary catches errors that
// escape the route boundary inside client components.
//
// SessionTimeoutModal is intentionally a SIBLING (outside the ErrorBoundary)
// so a crash inside the page tree doesn't disable the session-timeout safety
// net — the modal must keep working even if the page tree is broken.

import { ReactNode } from 'react';
import { AppShell } from '@/components/layout/AppShell';
import SessionTimeoutModal from '@/components/SessionTimeoutModal';
import { ErrorBoundary } from '@/components/ui/ErrorBoundary';

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
  return (
    <>
      <ErrorBoundary>
        <AppShell>{children}</AppShell>
      </ErrorBoundary>
      <SessionTimeoutModal />
    </>
  );
}
