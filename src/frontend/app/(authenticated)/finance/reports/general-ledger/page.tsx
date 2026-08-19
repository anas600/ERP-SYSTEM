'use client';

// Sprint 52b (DEC-137) — صفحة دفتر الأستاذ لحساب معين.
// Sprint 38 كانت عندها صفحة، لكن Route /finance/reports/general-ledger غير موجود
// (كان redirect لـ /finance/ledger). الآن صفحة كاملة في /finance/reports/general-ledger
// مع: account picker + date range + ledger lines + running balance + طباعة.

import { useEffect, useMemo, useState, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  Calendar,
  RefreshCw,
  Printer,
  ArrowLeft,
  FileText,
  ChevronDown,
  ChevronLeft,
} from 'lucide-react';
import { PageHeader, Card, Button, Input, useToast } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { financeApi, getErrorMessage, type LedgerLine, type Account } from '@/lib/api';
import { cn } from '@/lib/utils';

function todayIso(): string { return new Date().toISOString().slice(0, 10); }
function firstOfYearIso(): string {
  const d = new Date();
  return `${d.getFullYear() - 1}-01-01`;
}

function GeneralLedgerContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const initialAccountId = searchParams.get('accountId') ?? '';
  const { success, error: showError } = useToast();

  const [accounts, setAccounts] = useState<Account[]>([]);
  const [accountId, setAccountId] = useState<string>(initialAccountId);
  const [from, setFrom] = useState<string>(firstOfYearIso());
  const [to, setTo] = useState<string>(todayIso());
  const [lines, setLines] = useState<LedgerLine[]>([]);
  const [loading, setLoading] = useState(false);
  const [loaded, setLoaded] = useState(false);

  // Load accounts
  useEffect(() => {
    let cancelled = false;
    financeApi.listAccounts()
      .then((rows) => { if (!cancelled) setAccounts(rows); })
      .catch((e) => { if (!cancelled) showError('فشل تحميل الحسابات: ' + getErrorMessage(e)); });
    return () => { cancelled = true; };
  }, [showError]);

  // Auto-load when accountId is set via URL
  useEffect(() => {
    if (initialAccountId && !loaded) {
      load(initialAccountId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialAccountId]);

  const selectedAccount = useMemo(() => accounts.find((a) => a.id === accountId) ?? null, [accounts, accountId]);

  const totals = useMemo(() => {
    const dr = lines.reduce((s, l) => s + Number(l.debit || 0), 0);
    const cr = lines.reduce((s, l) => s + Number(l.credit || 0), 0);
    return { dr, cr, diff: dr - cr };
  }, [lines]);

  const load = async (idOverride?: string) => {
    const id = idOverride ?? accountId;
    if (!id) {
      showError('اختر حساب أولاً');
      return;
    }
    setLoading(true);
    setLoaded(true);
    try {
      const data = await financeApi.getAccountLedger(id, from || undefined, to || undefined);
      setLines(data);
    } catch (e) {
      showError('فشل تحميل دفتر الأستاذ: ' + getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  const onPrint = () => {
    window.print();
  };

  return (
    <div className="space-y-6" dir="rtl">
      <PageHeader
        title="دفتر الأستاذ"
        description="حركات حساب معين بترتيب زمني مع رصيد جاري"
        actions={
          <div className="flex items-center gap-3">
            <Link
              href="/dashboard"
              className="inline-flex items-center gap-2 text-sm text-primary-600 hover:text-primary-700"
            >
              <ArrowLeft className="w-4 h-4 rotate-180" />
              العودة للوحة التحكم
            </Link>
            <Button variant="secondary" onClick={onPrint} iconLeft={<Printer className="h-4 w-4" />}>
              طباعة
            </Button>
          </div>
        }
      />

      {/* Filters */}
      <Card className="p-4">
        <div className="flex items-end gap-3 flex-wrap">
          <div className="flex flex-col flex-1 min-w-[240px]">
            <label className="text-xs text-gray-500 mb-1">الحساب</label>
            <select
              value={accountId}
              onChange={(e) => setAccountId(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 min-h-[38px]"
            >
              <option value="">— اختر حساب —</option>
              {accounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.code} — {a.name}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">من تاريخ</label>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">إلى تاريخ</label>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
          <Button variant="primary" onClick={() => load()} iconLeft={<Calendar className="h-4 w-4" />}>
            تطبيق
          </Button>
          <Button variant="ghost" onClick={() => load()} iconLeft={<RefreshCw className="h-4 w-4" />}>
            إعادة
          </Button>
        </div>
      </Card>

      {/* Account summary card */}
      {selectedAccount && (
        <Card className="p-4 bg-gradient-to-l from-blue-50 to-white">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-xs text-gray-500">الحساب المختار</div>
              <div className="text-lg font-bold text-gray-800">
                <span className="font-mono text-blue-600 ml-2">{selectedAccount.code}</span>
                {selectedAccount.name}
              </div>
            </div>
            <div className="flex items-center gap-6 text-sm">
              <div className="text-center">
                <div className="text-xs text-gray-500">عدد الحركات</div>
                <div className="font-mono font-bold text-lg">{lines.length}</div>
              </div>
              <div className="text-center">
                <div className="text-xs text-gray-500">إجمالي المدين</div>
                <div className="font-mono font-bold text-lg text-emerald-700">
                  {totals.dr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </div>
              </div>
              <div className="text-center">
                <div className="text-xs text-gray-500">إجمالي الدائن</div>
                <div className="font-mono font-bold text-lg text-red-700">
                  {totals.cr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </div>
              </div>
              <div className="text-center">
                <div className="text-xs text-gray-500">الرصيد النهائي</div>
                <div className={cn(
                  'font-mono font-bold text-lg',
                  lines.length > 0 && lines[lines.length - 1].runningBalance >= 0 ? 'text-emerald-700' : 'text-red-700'
                )}>
                  {(lines.length > 0 ? lines[lines.length - 1].runningBalance : 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </div>
              </div>
            </div>
          </div>
        </Card>
      )}

      {/* Ledger table */}
      <Card className="p-0 overflow-hidden">
        {loading ? (
          <div className="text-center py-12 text-gray-500">
            <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
            <p className="mt-3 text-sm">جاري تحميل الحركات…</p>
          </div>
        ) : !accountId ? (
          <div className="px-4 py-12 text-center text-gray-400">
            <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
            <p>اختر حسابًا من القائمة لعرض حركاته</p>
          </div>
        ) : lines.length === 0 ? (
          <div className="px-4 py-12 text-center text-gray-400">
            <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
            <p>لا توجد حركات على هذا الحساب في الفترة المحددة</p>
            <p className="text-xs mt-2">جرّب توسيع النطاق الزمني أو اختر حسابًا آخر</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm" dir="rtl">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-start px-3 py-2 font-semibold text-gray-600 text-xs">التاريخ</th>
                  <th className="text-start px-3 py-2 font-semibold text-gray-600 text-xs">رقم القيد</th>
                  <th className="text-start px-3 py-2 font-semibold text-gray-600 text-xs">البيان</th>
                  <th className="text-end px-3 py-2 font-semibold text-gray-600 text-xs">مدين</th>
                  <th className="text-end px-3 py-2 font-semibold text-gray-600 text-xs">دائن</th>
                  <th className="text-end px-3 py-2 font-semibold text-gray-600 text-xs">الرصيد</th>
                </tr>
              </thead>
              <tbody>
                {lines.map((l, i) => (
                  <tr key={l.journalEntryId + i} className="border-b border-gray-100 hover:bg-blue-50/30">
                    <td className="px-3 py-2 text-gray-700 whitespace-nowrap text-xs">
                      {new Date(l.entryDate).toLocaleDateString('en-GB')}
                    </td>
                    <td className="px-3 py-2 font-mono text-xs text-blue-700 whitespace-nowrap">
                      {l.entryNumber}
                    </td>
                    <td className="px-3 py-2 text-gray-800">
                      {l.description}
                      {l.reference && (
                        <span className="text-xs text-gray-400 mr-2">({l.reference})</span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-end font-mono text-emerald-700">
                      {l.debit > 0 ? l.debit.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '—'}
                    </td>
                    <td className="px-3 py-2 text-end font-mono text-red-700">
                      {l.credit > 0 ? l.credit.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '—'}
                    </td>
                    <td className={cn(
                      'px-3 py-2 text-end font-mono font-bold',
                      l.runningBalance >= 0 ? 'text-gray-800' : 'text-red-700'
                    )}>
                      {l.runningBalance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-gray-50 border-t-2 border-gray-300">
                <tr>
                  <td colSpan={3} className="px-3 py-2 text-start font-semibold text-gray-700">
                    الإجمالي
                  </td>
                  <td className="px-3 py-2 text-end font-mono font-bold text-emerald-700">
                    {totals.dr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td className="px-3 py-2 text-end font-mono font-bold text-red-700">
                    {totals.cr.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td className={cn(
                    'px-3 py-2 text-end font-mono font-bold',
                    lines[lines.length - 1].runningBalance >= 0 ? 'text-gray-800' : 'text-red-700'
                  )}>
                    {lines[lines.length - 1].runningBalance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </Card>

      {selectedAccount && lines.length > 0 && (
        <div className="text-xs text-gray-500 text-center">
          تم عرض {lines.length} حركة للحساب {selectedAccount.code} — {selectedAccount.name}
        </div>
      )}
    </div>
  );
}

export default function GeneralLedgerPage() {
  return (
    <Suspense fallback={
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل…</p>
      </div>
    }>
      <GeneralLedgerContent />
    </Suspense>
  );
}
