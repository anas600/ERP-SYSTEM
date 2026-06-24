'use client';

// Layout لكل الصفحات المحمية — يلف المحتوى بـ AppShell
// الـ (authenticated) route group يخفي الـ prefix من الـ URL (نفس الـ paths)

import { ReactNode } from 'react';
import { AppShell } from '@/components/layout/AppShell';

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
