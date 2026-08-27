'use client';

// Sprint 64 / DEC-226 — SubcontractorCard.
//
// Compact card used in the subcontractor list page. Shows master data
// (code, name, specialty, contact) + a trade pill. Click → navigates to the
// subcontractor detail page (handled by the parent via onClick / Link wrap).

import Link from 'next/link';
import { HardHat, Phone, Mail, Briefcase, User2 } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface SubcontractorCardProps {
  id: string;
  code: string;
  name: string;
  nameAr?: string | null;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  tradeSpecialty?: string | null;
  isActive: boolean;
  /** Optional project context — when provided the link goes to the project-scoped detail. */
  projectId?: string;
  className?: string;
}

export function SubcontractorCard({
  id,
  code,
  name,
  nameAr,
  contactPerson,
  phone,
  email,
  tradeSpecialty,
  isActive,
  projectId,
  className,
}: SubcontractorCardProps) {
  const href = projectId
    ? `/projects/${projectId}/subcontractors/${id}`
    : '#';

  return (
    <Link
      href={href}
      className={cn(
        'block rounded-2xl border border-gray-200 bg-white p-4 shadow-sm transition hover:border-brand-300 hover:shadow-md',
        !isActive && 'opacity-60',
        className,
      )}
    >
      <div className="flex items-start gap-3">
        <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-amber-500 to-amber-700 text-white shadow-sm">
          <HardHat className="h-5 w-5" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="inline-flex items-center rounded-md bg-slate-100 px-2 py-0.5 font-mono text-[11px] font-bold text-slate-700">
              {code}
            </span>
            {tradeSpecialty && (
              <span className="inline-flex items-center rounded-full bg-violet-50 px-2 py-0.5 text-[11px] font-bold text-violet-700 ring-1 ring-violet-200">
                {tradeSpecialty}
              </span>
            )}
            {!isActive && (
              <span className="inline-flex items-center rounded-full bg-rose-50 px-2 py-0.5 text-[11px] font-bold text-rose-700 ring-1 ring-rose-200">
                معطّل
              </span>
            )}
          </div>
          <p className="mt-1 truncate text-sm font-bold text-gray-900" title={name}>
            {name}
          </p>
          {nameAr && (
            <p className="truncate text-xs text-gray-500" dir="rtl" title={nameAr}>
              {nameAr}
            </p>
          )}
        </div>
      </div>

      <div className="mt-3 space-y-1.5 text-[11px] text-gray-500">
        {contactPerson && (
          <div className="flex items-center gap-1.5">
            <User2 className="h-3 w-3 text-gray-400" />
            <span className="truncate">{contactPerson}</span>
          </div>
        )}
        {phone && (
          <div className="flex items-center gap-1.5">
            <Phone className="h-3 w-3 text-gray-400" />
            <span className="tabular-nums">{phone}</span>
          </div>
        )}
        {email && (
          <div className="flex items-center gap-1.5">
            <Mail className="h-3 w-3 text-gray-400" />
            <span className="truncate">{email}</span>
          </div>
        )}
        {!contactPerson && !phone && !email && (
          <div className="flex items-center gap-1.5 text-gray-400">
            <Briefcase className="h-3 w-3" />
            <span>لا توجد بيانات اتصال</span>
          </div>
        )}
      </div>
    </Link>
  );
}

export default SubcontractorCard;
