'use client';

// Phase 6.3: <CompanySwitcher /> — dropdown of the user's companies.
// Lets the user pick which company the X-Company-Id header will route to.
// - Loads /api/auth/me/companies on mount (refreshes the list).
// - Persists the active company id in localStorage (currentCompanyId).
// - On change: updates localStorage + reloads the current view so the
//   new company context takes effect across all loaded data.
//
// Sprint 9 T3 (FE Jimi 3): visual feedback during the switch — the trigger
// button shows a spinner and is disabled while `router.refresh()` is in
// flight, so the user knows the page is reloading instead of seeing a
// frozen UI between "click" and "data reload".

import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Building2, Check, ChevronDown, Loader2 } from 'lucide-react';
import { authApi, GetUserCompaniesResponse, UserCompanyInfo } from '@/lib/api';
import { cn } from '@/lib/utils';

interface CompanySwitcherProps {
  className?: string;
}

export function CompanySwitcher({ className }: CompanySwitcherProps) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [companies, setCompanies] = useState<UserCompanyInfo[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  // Sprint 9 T3 — "switching" state: true while router.refresh() is in flight.
  const [switching, setSwitching] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Load on mount + when dropdown opens (so we always have fresh data).
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const res = await authApi.getUserCompanies();
        if (cancelled) return;
        setCompanies(res.companies);
        // Honor the user's previously-selected company if it still exists;
        // otherwise fall back to the default.
        const stored = authApi.getCurrentCompanyId();
        const valid = stored && res.companies.some((c) => c.companyId === stored);
        const next = valid ? stored! : res.defaultCompanyId;
        if (next !== authApi.getCurrentCompanyId()) {
          authApi.setCurrentCompanyId(next);
        }
        setActiveId(next);
      } catch (e) {
        // Silent — the switcher just stays empty. 401 will be caught upstream.
        if (!cancelled) setCompanies([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  // Close on outside click
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

  // Sprint 9 T3 — when router.refresh() finishes, clear the switching flag.
  // Next.js doesn't expose a promise from router.refresh(), so we use a
  // microtask + short delay heuristic. The spinner is purely cosmetic —
  // the actual refresh is driven by the framework.
  useEffect(() => {
    if (!switching) return;
    const t = setTimeout(() => setSwitching(false), 600);
    return () => clearTimeout(t);
  }, [switching]);

  const active = companies.find((c) => c.companyId === activeId) ?? null;

  if (loading) {
    return (
      <div className={cn('h-9 w-40 bg-gray-100 rounded-lg animate-pulse', className)} aria-label="جاري التحميل…" />
    );
  }

  // No companies? Don't render (shouldn't happen post-6.1c — every user
  // has at least the Holding via user_companies).
  if (companies.length === 0) return null;

  return (
    <div ref={dropdownRef} className={cn('relative', className)}>
      <button
        type="button"
        onClick={() => !switching && setOpen((v) => !v)}
        disabled={switching}
        className={cn(
          'flex items-center gap-2 h-9 px-3 rounded-lg border border-gray-200 bg-white text-sm transition-colors',
          switching ? 'opacity-70 cursor-wait' : 'hover:bg-gray-50'
        )}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-busy={switching}
        aria-label={switching ? 'جاري التبديل...' : 'تبديل الشركة'}
      >
        {switching ? (
          <Loader2 className="h-4 w-4 text-blue-500 animate-spin" />
        ) : (
          <Building2 className="h-4 w-4 text-gray-400" />
        )}
        <div className="text-right min-w-0">
          <p className="text-sm font-medium text-gray-800 truncate leading-tight">
            {switching ? 'جاري التبديل...' : active?.name ?? 'شركة غير محددة'}
          </p>
          {active && !switching && (
            <p className="text-[10px] text-gray-500 leading-tight">
              {active.isHolding ? 'الشركة القابضة' : active.code}
            </p>
          )}
        </div>
        <ChevronDown
          className={cn(
            'h-4 w-4 text-gray-400 transition-transform',
            open && !switching && 'rotate-180',
            switching && 'opacity-30'
          )}
        />
      </button>

      {open && !switching && (
        <ul
          role="listbox"
          className="absolute left-0 mt-2 w-64 max-h-80 overflow-y-auto bg-white rounded-lg shadow-lg border border-gray-100 py-1 z-30"
        >
          {companies.map((c) => {
            const isActive = c.companyId === activeId;
            return (
              <li key={c.companyId}>
                <button
                  type="button"
                  onClick={() => {
                    if (c.companyId === activeId) {
                      setOpen(false);
                      return;
                    }
                    authApi.setCurrentCompanyId(c.companyId);
                    setActiveId(c.companyId);
                    setOpen(false);
                    // Reload the current route so server-side / context-derived
                    // data reloads against the new company.
                    setSwitching(true);
                    router.refresh();
                  }}
                  className={cn(
                    'w-full text-right flex items-start gap-2 px-3 py-2 text-sm hover:bg-gray-50',
                    isActive && 'bg-blue-50'
                  )}
                  role="option"
                  aria-selected={isActive}
                >
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-gray-800 truncate">{c.name}</p>
                    <p className="text-[10px] text-gray-500 truncate">
                      {c.code} {c.isHolding ? '· القابضة' : ''} {c.isDefault ? '· الافتراضية' : ''}
                    </p>
                  </div>
                  {isActive && <Check className="h-4 w-4 text-blue-600 flex-shrink-0 mt-0.5" />}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
